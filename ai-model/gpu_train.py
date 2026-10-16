"""GPU transfer learning for Kaggle; outputs compatible with existing inference.py."""
from __future__ import annotations

import csv
import hashlib
import json
import os
import platform
import subprocess
import sys
import time
from pathlib import Path

os.environ.setdefault("TF_CPP_MIN_LOG_LEVEL", "2")
import numpy as np
import tensorflow as tf
from data import CLASS_NAMES, load_rows, image_fingerprints
from train import calibrate_threshold
from evaluation_curves import diagnostic_curves, training_curves, split_distribution


DEFAULT_CONFIG = {"seed": 42, "batch_size": 32, "head_epochs": 25, "fine_tune_epochs": 15,
                  "head_lr": 1e-3, "fine_tune_lr": 1e-5, "unfreeze_last": 30,
                  "require_gpu": True, "weights": "imagenet", "threshold_minimum": .75,
                  "target_accepted_accuracy": .90}


class LearningRateLog(tf.keras.callbacks.Callback):
    def on_epoch_end(self, epoch, logs=None):
        if logs is not None:
            logs["learning_rate"] = float(tf.keras.backend.get_value(self.model.optimizer.learning_rate))


def train_gpu(dataset, run, config=None):
    dataset, run = Path(dataset), Path(run)
    cfg = {**DEFAULT_CONFIG, **(config or {})}
    if cfg["head_epochs"] < 1 or cfg["fine_tune_epochs"] < 0 or cfg["batch_size"] < 1 or cfg["unfreeze_last"] < 1:
        raise ValueError("Invalid epoch/batch/unfreeze configuration")
    gpus = tf.config.list_physical_devices("GPU")
    if cfg["require_gpu"] and not gpus:
        raise RuntimeError("No TensorFlow GPU detected. Enable Kaggle Settings > Accelerator > GPU and restart session.")
    for gpu in gpus:
        try:
            tf.config.experimental.set_memory_growth(gpu, True)
        except RuntimeError:
            pass  # Notebook may have initialized devices already.
    # Float32 keeps the exported model suitable for the CPU backend too.
    # Kaggle automatically places supported operations on the first GPU.
    tf.keras.mixed_precision.set_global_policy("float32")
    tf.keras.utils.set_random_seed(cfg["seed"])
    reports, artifacts = run / "reports", run / "artifacts"
    manifest = reports / "splits.csv"
    if (artifacts / "model.keras").exists() or (run / "checkpoints").exists():
        raise FileExistsError("Run already trained/started; choose a NEW run directory to preserve evidence")
    with manifest.open(encoding="utf-8", newline="") as stream:
        rows = list(csv.DictReader(stream))
    sets = {name: [r for r in rows if r["split"] == name] for name in ("train", "validation", "test")}
    if any({int(r["label"]) for r in part} != set(range(len(CLASS_NAMES))) for part in sets.values()):
        raise ValueError("Every split must contain all configured classes")
    if len({r["path"] for r in rows}) != len(rows):
        raise ValueError("Repeated path in manifest")
    for key in ("group_id", "sha256_pixels"):
        seen = {}
        for r in rows:
            if seen.setdefault(r[key], r["split"]) != r["split"]:
                raise ValueError(f"Split leakage: {key}")
    # Audit hashes, not test predictions; prevents silently training with changed files.
    for r in rows:
        if image_fingerprints(dataset / r["path"])[0] != r["sha256_pixels"]:
            raise ValueError(f"Image changed since split preparation: {r['path']}")
    artifacts.mkdir(parents=True); (run / "checkpoints").mkdir()
    (run / "config.json").write_text(json.dumps(cfg, indent=2), encoding="utf-8")
    (run / "requirements-runtime.txt").write_text(subprocess.check_output([sys.executable, "-m", "pip", "freeze"], text=True), encoding="utf-8")
    (reports / "environment.json").write_text(json.dumps({"python": platform.python_version(), "tensorflow": tf.__version__,
        "keras": tf.keras.__version__, "gpus": [g.name for g in gpus], "platform": platform.platform(),
        "tensorflow_build": tf.sysconfig.get_build_info()}, indent=2, default=str), encoding="utf-8")
    split_distribution(rows, reports)
    x_train, y_train = load_rows(sets["train"], dataset)
    x_val, y_val = load_rows(sets["validation"], dataset)
    augmentation = tf.keras.Sequential([
        tf.keras.layers.RandomFlip("horizontal", seed=cfg["seed"]),
        tf.keras.layers.RandomRotation(.06, seed=cfg["seed"] + 1),
        tf.keras.layers.RandomZoom(.12, seed=cfg["seed"] + 2),
        tf.keras.layers.RandomContrast(.12, seed=cfg["seed"] + 3)], name="train_only_augmentation")
    train_ds = tf.data.Dataset.from_tensor_slices((x_train, y_train)).shuffle(len(y_train), seed=cfg["seed"])
    train_ds = train_ds.batch(cfg["batch_size"]).map(
        lambda x, y: (tf.clip_by_value(augmentation(tf.cast(x, tf.float32), training=True), 0, 255), y),
        num_parallel_calls=1).prefetch(tf.data.AUTOTUNE)
    val_ds = tf.data.Dataset.from_tensor_slices((x_val, y_val)).batch(cfg["batch_size"]).prefetch(tf.data.AUTOTUNE)
    backbone = tf.keras.applications.MobileNetV2(include_top=False, weights=cfg["weights"], input_shape=(224, 224, 3))
    backbone.trainable = False
    inputs = tf.keras.Input((224, 224, 3), name="rgb_0_255")
    x = tf.keras.layers.Rescaling(1 / 127.5, offset=-1, name="mobilenet_preprocessing")(inputs)
    x = backbone(x, training=False)  # BN statistics remain fixed in BOTH stages.
    x = tf.keras.layers.GlobalAveragePooling2D()(x)
    x = tf.keras.layers.Dense(128, activation="relu")(x)
    x = tf.keras.layers.Dropout(.35)(x)
    outputs = tf.keras.layers.Dense(len(CLASS_NAMES), activation="softmax", dtype="float32", name="class_probabilities")(x)
    model = tf.keras.Model(inputs, outputs, name="ecosort_mobilenetv2")
    counts = np.bincount(y_train, minlength=len(CLASS_NAMES))
    class_weights = {i: float(len(y_train) / (len(CLASS_NAMES) * counts[i])) for i in range(len(CLASS_NAMES))}
    histories = {}; start = time.perf_counter()

    def fit_phase(phase, epochs, lr, patience):
        model.compile(optimizer=tf.keras.optimizers.Adam(lr), loss="sparse_categorical_crossentropy", metrics=["accuracy"])
        path = run / "checkpoints" / f"{phase}.keras"
        history = model.fit(train_ds, validation_data=val_ds, epochs=epochs, class_weight=class_weights, shuffle=False,
            callbacks=[tf.keras.callbacks.ModelCheckpoint(path, monitor="val_loss", save_best_only=True),
                       tf.keras.callbacks.EarlyStopping(monitor="val_loss", patience=patience, restore_best_weights=True),
                       tf.keras.callbacks.ReduceLROnPlateau(monitor="val_loss", patience=2, factor=.5, min_lr=lr / 100),
                       LearningRateLog(), tf.keras.callbacks.CSVLogger(run / "reports" / f"{phase}_epochs.csv")], verbose=2)
        histories[phase] = {k: [float(v) for v in values] for k, values in history.history.items()}
        return float(model.evaluate(val_ds, verbose=0)[0])

    head_loss = fit_phase("head", cfg["head_epochs"], cfg["head_lr"], 5)
    selected = "head"; fine_loss = None
    if cfg["fine_tune_epochs"]:
        backbone.trainable = True
        for layer in backbone.layers:
            layer.trainable = False
        for layer in backbone.layers[-cfg["unfreeze_last"]:]:
            if not isinstance(layer, tf.keras.layers.BatchNormalization):
                layer.trainable = True
        fine_loss = fit_phase("fine_tune", cfg["fine_tune_epochs"], cfg["fine_tune_lr"], 4)
        if fine_loss < head_loss:
            selected = "fine_tune"
    # Explicitly reload selected checkpoint; failed fine-tuning never replaces the head baseline.
    model = tf.keras.models.load_model(run / "checkpoints" / f"{selected}.keras", compile=False, safe_mode=True)
    val_probabilities = model.predict(val_ds, verbose=0)
    calibration = calibrate_threshold(val_probabilities, y_val, cfg["threshold_minimum"], cfg["target_accepted_accuracy"])
    (reports / "threshold_calibration.json").write_text(json.dumps(calibration, indent=2), encoding="utf-8")
    diagnostic_curves(y_val, val_probabilities, reports / "validation", calibration["threshold"], "validation")
    (reports / "training_history.json").write_text(json.dumps(histories, indent=2), encoding="utf-8")
    training_curves(histories, reports)
    model.save(artifacts / "model.keras")
    metadata = {"class_names": CLASS_NAMES, "input_shape": [224, 224, 3], "input_dtype": "float32", "input_range": [0, 255],
        "preprocessing_in_model": "Rescaling(1/127.5, offset=-1)", "resize": "Pillow bilinear, direct resize, EXIF transpose, RGB",
        "threshold": calibration["threshold"], "threshold_target_met_on_validation": calibration["target_met"],
        "model_sha256": hashlib.sha256((artifacts / "model.keras").read_bytes()).hexdigest(),
        "split_manifest_sha256": hashlib.sha256(manifest.read_bytes()).hexdigest(), "seed": cfg["seed"],
        "tensorflow": tf.__version__, "keras": tf.keras.__version__, "python": platform.python_version(),
        "selected_stage": selected, "head_validation_loss": head_loss, "fine_tune_validation_loss": fine_loss,
        "validation_accuracy": float((val_probabilities.argmax(1) == y_val).mean()),
        "epochs_run": {k: len(v["loss"]) for k, v in histories.items()}, "training_seconds": time.perf_counter() - start,
        "selection": "Lowest validation loss between stage checkpoints; train_gpu does not load test tensors",
        "gpu_devices": [g.name for g in gpus], "precision_policy": "float32"}
    (artifacts / "metadata.json").write_text(json.dumps(metadata, indent=2), encoding="utf-8")
    # Reload the EXACT exported artifact through the backend interface before test evaluation.
    from inference import Predictor
    serving = Predictor(artifacts)
    sample = (dataset / sets["validation"][0]["path"]).read_bytes()
    response = serving.predict(sample)
    (reports / "inference_smoke.json").write_text(json.dumps(response, indent=2), encoding="utf-8")
    print(json.dumps(metadata, indent=2)); return metadata
