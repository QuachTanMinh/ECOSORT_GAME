"""MobileNetV2 transfer learning; validation-only model/threshold selection."""
from __future__ import annotations

import argparse
import csv
import hashlib
import json
import os
import platform
import time
from pathlib import Path

os.environ.setdefault("TF_CPP_MIN_LOG_LEVEL", "2")
os.environ.setdefault("TF_NUM_INTRAOP_THREADS", "4")
os.environ.setdefault("TF_NUM_INTEROP_THREADS", "2")
os.environ.setdefault("KERAS_HOME", str(Path(__file__).parent / ".keras"))
os.environ.setdefault("MPLCONFIGDIR", str(Path(__file__).parent / ".matplotlib"))

import numpy as np
import tensorflow as tf
from sklearn.metrics import accuracy_score

from data import CLASS_NAMES, IMAGE_SIZE, load_rows


def calibrate_threshold(probabilities, labels, minimum=.75, target_precision=.90):
    confidence = probabilities.max(axis=1)
    correct = probabilities.argmax(axis=1) == labels
    candidates = []
    for threshold in np.arange(minimum, .991, .01):
        selected = confidence >= threshold
        count = int(selected.sum())
        precision = float(correct[selected].mean()) if count else None
        candidates.append({"threshold": round(float(threshold), 2), "accepted": count,
                           "coverage": count / len(labels), "accepted_accuracy": precision})
    valid = [row for row in candidates if row["accepted"] >= 20 and row["accepted_accuracy"] >= target_precision]
    chosen = max(valid, key=lambda row: (row["coverage"], -row["threshold"])) if valid else candidates[0]
    return {"threshold": chosen["threshold"], "selection_split": "validation", "target_accepted_accuracy": target_precision,
            "target_met": bool(valid), "chosen": chosen, "candidates": candidates,
            "warning": "Confidence gating is not an out-of-distribution detector; only known-class validation images were available."}


def train(dataset: Path, manifest: Path, artifacts: Path, reports: Path, epochs=35, augmented_views=2,
          fine_tune_epochs=0, seed=42, batch_size=32):
    tf.keras.utils.set_random_seed(seed)
    artifacts.mkdir(parents=True, exist_ok=True); reports.mkdir(parents=True, exist_ok=True)
    if (artifacts / "model.keras").exists():
        raise FileExistsError("Model already exists. Choose a new --artifacts directory to preserve the previous run.")
    with manifest.open(encoding="utf-8", newline="") as stream:
        rows = list(csv.DictReader(stream))
    split_rows = {split: [row for row in rows if row["split"] == split] for split in ("train", "validation", "test")}
    groups = {split: {row["group_id"] for row in data} for split, data in split_rows.items()}
    if groups["train"] & groups["validation"] or groups["train"] & groups["test"] or groups["validation"] & groups["test"]:
        raise ValueError("Group leakage in split manifest")
    # Do not load or inspect test images during training/model selection.
    x_train, y_train = load_rows(split_rows["train"], dataset)
    x_val, y_val = load_rows(split_rows["validation"], dataset)
    start = time.perf_counter()
    backbone = tf.keras.applications.MobileNetV2(input_shape=(*IMAGE_SIZE, 3), include_top=False, weights="imagenet")
    backbone.trainable = False
    raw = tf.keras.Input(shape=(*IMAGE_SIZE, 3), name="rgb_0_255")
    scaled = tf.keras.layers.Rescaling(1 / 127.5, offset=-1, name="mobilenet_preprocessing")(raw)
    features = backbone(scaled, training=False)
    pooled = tf.keras.layers.GlobalAveragePooling2D()(features)
    encoder = tf.keras.Model(raw, pooled, name="imagenet_encoder")
    feature_input = tf.keras.Input(shape=(1280,))
    hidden = tf.keras.layers.Dense(128, activation="relu")(feature_input)
    hidden = tf.keras.layers.Dropout(.35)(hidden)
    output = tf.keras.layers.Dense(3, activation="softmax", name="class_probabilities")(hidden)
    head = tf.keras.Model(feature_input, output, name="classification_head")
    head.compile(optimizer=tf.keras.optimizers.Adam(1e-3), loss="sparse_categorical_crossentropy", metrics=["accuracy"])
    augmentation = tf.keras.Sequential([
        tf.keras.layers.RandomFlip("horizontal", seed=seed),
        tf.keras.layers.RandomRotation(.06, fill_mode="reflect", seed=seed + 1),
        tf.keras.layers.RandomZoom(.12, seed=seed + 2),
        tf.keras.layers.RandomContrast(.12, seed=seed + 3),
    ], name="train_only_augmentation")

    def encode(images, augment=False):
        batches = []
        for offset in range(0, len(images), batch_size):
            batch = tf.cast(images[offset:offset + batch_size], tf.float32)
            if augment:
                batch = tf.clip_by_value(augmentation(batch, training=True), 0, 255)
            batches.append(encoder(batch, training=False).numpy())
            if offset % (batch_size * 8) == 0:
                print(f"Extract features: {min(offset + batch_size, len(images))}/{len(images)} augment={augment}", flush=True)
        return np.concatenate(batches)

    # Cache embeddings in RAM, not a train/validation/test mixing cache on disk.
    train_features = [encode(x_train)]
    for _ in range(augmented_views):
        train_features.append(encode(x_train, augment=True))
    train_features = np.concatenate(train_features)
    train_labels = np.tile(y_train, augmented_views + 1)
    val_features = encode(x_val)
    counts = np.bincount(y_train, minlength=3)
    class_weights = {i: float(len(y_train) / (3 * count)) for i, count in enumerate(counts)}
    train_ds = tf.data.Dataset.from_tensor_slices((train_features, train_labels)).shuffle(len(train_labels), seed=seed).batch(batch_size)
    val_ds = tf.data.Dataset.from_tensor_slices((val_features, y_val)).batch(batch_size)
    history = head.fit(train_ds, validation_data=val_ds, epochs=epochs, class_weight=class_weights,
        callbacks=[tf.keras.callbacks.EarlyStopping(monitor="val_loss", patience=7, restore_best_weights=True),
                   tf.keras.callbacks.ReduceLROnPlateau(monitor="val_loss", patience=3, factor=.5)], verbose=2)
    model = tf.keras.Model(raw, head(encoder(raw)), name="ecosort_mobilenetv2")
    fine_history = None
    baseline_val_loss = float(head.evaluate(val_ds, verbose=0)[0])
    if fine_tune_epochs > 0:
        baseline_weights = model.get_weights()
        encoder.trainable = True; backbone.trainable = True
        for layer in backbone.layers[:-30]:
            layer.trainable = False
        for layer in backbone.layers:
            if isinstance(layer, tf.keras.layers.BatchNormalization):
                layer.trainable = False
        model.compile(optimizer=tf.keras.optimizers.Adam(1e-5), loss="sparse_categorical_crossentropy", metrics=["accuracy"])
        fine_train = tf.data.Dataset.from_tensor_slices((x_train, y_train)).shuffle(len(y_train), seed=seed).batch(batch_size).map(
            lambda x, y: (tf.clip_by_value(augmentation(tf.cast(x, tf.float32), training=True), 0, 255), y))
        fine_val = tf.data.Dataset.from_tensor_slices((x_val, y_val)).batch(batch_size)
        fine_history = model.fit(fine_train, validation_data=fine_val, epochs=fine_tune_epochs, class_weight=class_weights,
            callbacks=[tf.keras.callbacks.EarlyStopping(monitor="val_loss", patience=3, restore_best_weights=True)], verbose=2).history
        if float(model.evaluate(fine_val, verbose=0)[0]) >= baseline_val_loss:
            model.set_weights(baseline_weights)
            print("Fine tuning did not improve validation loss; restored baseline weights.", flush=True)
    val_probabilities = np.concatenate([model(x_val[i:i + batch_size], training=False).numpy()
                                        for i in range(0, len(x_val), batch_size)])
    calibration = calibrate_threshold(val_probabilities, y_val)
    (reports / "threshold_calibration.json").write_text(json.dumps(calibration, indent=2), encoding="utf-8")
    model.save(artifacts / "model.keras")
    metadata = {"class_names": CLASS_NAMES, "input_shape": [224, 224, 3], "input_dtype": "float32",
                "input_range": [0, 255], "preprocessing_in_model": "Rescaling(1/127.5, offset=-1)",
                "resize": "Pillow bilinear, direct resize, EXIF transpose, RGB", "threshold": calibration["threshold"],
                "threshold_target_met_on_validation": calibration["target_met"], "seed": seed,
                "split_manifest_sha256": hashlib.sha256(manifest.read_bytes()).hexdigest(),
                "model_sha256": hashlib.sha256((artifacts / "model.keras").read_bytes()).hexdigest(),
                "tensorflow": tf.__version__, "keras": tf.keras.__version__, "python": platform.python_version(),
                "epochs_requested": epochs, "epochs_run": len(history.history["loss"]),
                "augmented_train_views": augmented_views, "fine_tune_epochs_requested": fine_tune_epochs,
                "training_seconds": round(time.perf_counter() - start, 2),
                "validation_accuracy": float(accuracy_score(y_val, val_probabilities.argmax(axis=1))),
                "selection": "best validation loss; test set not loaded by train.py"}
    (artifacts / "metadata.json").write_text(json.dumps(metadata, indent=2), encoding="utf-8")
    (reports / "training_history.json").write_text(json.dumps({"head": history.history, "fine_tune": fine_history}, indent=2), encoding="utf-8")
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    fig, axes = plt.subplots(1, 2, figsize=(10, 4))
    for ax, metric in zip(axes, ("loss", "accuracy")):
        ax.plot(history.history[metric], label="Train (augmented features)")
        ax.plot(history.history["val_" + metric], label="Validation"); ax.set_title(metric.title()); ax.set_xlabel("Epoch"); ax.legend()
    fig.tight_layout(); fig.savefig(reports / "learning_curves.png", dpi=160); plt.close(fig)
    print(json.dumps(metadata, indent=2), flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    base = Path(__file__).parent
    parser.add_argument("--dataset", type=Path, default=base.parent / "Dataset/dataset-plastic-metal-paper")
    parser.add_argument("--manifest", type=Path, default=base / "reports/splits.csv")
    parser.add_argument("--artifacts", type=Path, default=base / "artifacts")
    parser.add_argument("--reports", type=Path, default=base / "reports")
    parser.add_argument("--epochs", type=int, default=35)
    parser.add_argument("--augmented-views", type=int, default=2)
    parser.add_argument("--fine-tune-epochs", type=int, default=0)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--batch-size", type=int, default=32)
    args = parser.parse_args()
    train(args.dataset, args.manifest, args.artifacts, args.reports, args.epochs, args.augmented_views,
          args.fine_tune_epochs, args.seed, args.batch_size)
