"""Standalone Kaggle notebook source. Build notebook with build_t4x2_notebook.py.

Cells marked # %% execute in order. No project checkout, API key or pip upgrade needed.
"""
# %% Configuration
from pathlib import Path
import os
os.environ.setdefault("KERAS_BACKEND", "tensorflow")
os.environ.setdefault("TF_CPP_MIN_LOG_LEVEL", "2")

MODE = "expanded12"  # expanded12 OR game4; game4 preserves current serving label order.
DATASET_ROOT = None  # e.g. /kaggle/input/garbage-classification/garbage_classification
RUN_NAME = "ecosort-efficientnetv2b0-12-v1"  # Change for every independent experiment.
BACKBONE = "EfficientNetV2B0"  # Also supported: EfficientNetV2B2, MobileNetV2
SEED = 42
PER_GPU_BATCH = 16  # Global batch = this value * replicas; lower to 8 for OOM.
HEAD_EPOCHS = 10
FINE_EPOCHS = 30
UNFREEZE_LAST = 80
HEAD_LR = 1e-3
FINE_LR = 1e-5
MIN_THRESHOLD = .75
TARGET_ACCEPTED_ACCURACY = .97  # Target, NEVER a guaranteed result.
MIN_ACCEPTED_VALIDATION = 50
REQUIRE_GPU = True
USE_MIXED_PRECISION = True
NEAR_HASH_DISTANCE = 4
# Optional CSV: path,group_id. Paths relative to DATASET_ROOT; same object/session
# must share group_id. Without it only visual duplicates are grouped, not object IDs.
GROUPS_CSV = None
RUN_TEST = False  # Deliberate gate: lock configuration on validation first.

# %% Environment / actual two GPU strategy
import csv, hashlib, io, json, platform, subprocess, sys, time, zipfile
from collections import Counter, defaultdict
import numpy as np
import pandas as pd
from PIL import Image, ImageOps
import matplotlib.pyplot as plt
from sklearn.metrics import classification_report, confusion_matrix, f1_score, accuracy_score
import tensorflow as tf

def write_json(path, value):
    Path(path).write_text(json.dumps(value, indent=2, ensure_ascii=False, default=str), encoding="utf-8")

tf.keras.utils.set_random_seed(SEED)
gpus = tf.config.list_physical_devices("GPU")
if REQUIRE_GPU and not gpus:
    raise RuntimeError("No GPU. Kaggle Settings > Accelerator > GPU T4 x2, then restart session.")
for gpu in gpus:
    tf.config.experimental.set_memory_growth(gpu, True)
strategy = tf.distribute.MirroredStrategy() if gpus else tf.distribute.OneDeviceStrategy("/cpu:0")
tf.keras.mixed_precision.set_global_policy("mixed_float16" if gpus and USE_MIXED_PRECISION else "float32")
BATCH = PER_GPU_BATCH * strategy.num_replicas_in_sync
CLASSES = (["Plastic", "Metal", "Paper", "Organic"] if MODE == "game4" else
           ["Plastic", "Metal", "Paper", "Organic", "Cardboard", "GreenGlass",
            "BrownGlass", "WhiteGlass", "Clothes", "Shoes", "Batteries", "Trash"])
if MODE not in ("game4", "expanded12"):
    raise ValueError("MODE must be game4 or expanded12")
FOLDER_LABEL = dict(zip(["plastic", "metal", "paper", "biological", "cardboard", "green-glass",
    "brown-glass", "white-glass", "clothes", "shoes", "battery", "trash"],
    ["Plastic", "Metal", "Paper", "Organic", "Cardboard", "GreenGlass", "BrownGlass",
     "WhiteGlass", "Clothes", "Shoes", "Batteries", "Trash"]))
FOLDER_LABEL.update({"organic": "Organic", "batteries": "Batteries"})
RUN = Path("/kaggle/working") / RUN_NAME
RUN.mkdir(parents=True, exist_ok=False)
REPORTS = RUN / "reports"; REPORTS.mkdir()
ARTIFACTS = RUN / "artifacts"; ARTIFACTS.mkdir()
CHECKPOINTS = RUN / "checkpoints"; CHECKPOINTS.mkdir()
config = {k: globals()[k] for k in ["MODE", "RUN_NAME", "BACKBONE", "SEED", "PER_GPU_BATCH",
    "HEAD_EPOCHS", "FINE_EPOCHS", "UNFREEZE_LAST", "HEAD_LR", "FINE_LR", "MIN_THRESHOLD",
    "TARGET_ACCEPTED_ACCURACY", "MIN_ACCEPTED_VALIDATION", "NEAR_HASH_DISTANCE", "GROUPS_CSV"]}
config.update(classes=CLASSES, global_batch=BATCH, replicas=strategy.num_replicas_in_sync,
              require_gpu=REQUIRE_GPU, mixed_precision_policy=tf.keras.mixed_precision.global_policy().name)
write_json(RUN / "config.json", config)
write_json(REPORTS / "environment.json", {"python": sys.version, "tensorflow": tf.__version__,
    "keras": tf.keras.__version__, "platform": platform.platform(), "gpu_devices": [str(x) for x in gpus],
    "gpu_details": [tf.config.experimental.get_device_details(g) for g in gpus],
    "build": tf.sysconfig.get_build_info(), "replicas": strategy.num_replicas_in_sync})
(RUN / "requirements-runtime.txt").write_text(subprocess.check_output(
    [sys.executable, "-m", "pip", "freeze"], text=True), encoding="utf-8")
print("GPUs:", gpus, "replicas:", strategy.num_replicas_in_sync, "global batch:", BATCH)
if len(gpus) != 2:
    print("NOTE: requested T4 x2 but actual GPU count is", len(gpus), "; recorded above.")

# %% Data audit / duplicate groups / independent split
def discover_root(base):
    candidates = []
    for folder in [base, *sorted(p for p in base.rglob("*") if p.is_dir())]:
        labels = {FOLDER_LABEL.get(p.name.lower()) for p in folder.iterdir() if p.is_dir()}
        if set(CLASSES) <= labels:
            candidates.append(folder)
    if len(candidates) != 1:
        raise ValueError(f"Expected ONE dataset root, found {candidates}. Set DATASET_ROOT explicitly.")
    return candidates[0]

root = Path(DATASET_ROOT) if DATASET_ROOT else discover_root(Path("/kaggle/input"))
config["dataset_root"] = str(root); write_json(RUN / "config.json", config)
IMAGE_SIZE = (224, 224)  # Keep existing FastAPI input dimensions.

def read_rgb(path, resize=True):
    with Image.open(path) as im:
        if im.format not in ("JPEG", "PNG", "WEBP") or im.width * im.height > 20_000_000:
            raise ValueError("Unsupported format or >20 megapixels")
        rgb = ImageOps.exif_transpose(im).convert("RGB")
        if resize:
            rgb = rgb.resize(IMAGE_SIZE, Image.Resampling.BILINEAR)
        return np.asarray(rgb, dtype=np.uint8)

class UnionFind:
    def __init__(self, n): self.p = list(range(n))
    def find(self, x):
        while self.p[x] != x:
            self.p[x] = self.p[self.p[x]]; x = self.p[x]
        return x
    def union(self, a, b): self.p[self.find(a)] = self.find(b)

class BKTree:
    """64-bit dHash Hamming neighbors; avoids a quadratic all-pairs scan."""
    def __init__(self): self.root = None
    def add(self, value, index):
        if self.root is None: self.root = (value, index, {}); return
        node = self.root
        while True:
            d = (value ^ node[0]).bit_count()
            if d not in node[2]: node[2][d] = (value, index, {}); return
            node = node[2][d]
    def query(self, value, radius):
        found, stack = [], [self.root] if self.root else []
        while stack:
            v, index, children = stack.pop(); d = (value ^ v).bit_count()
            if d <= radius: found.append(index)
            stack.extend(node for edge, node in children.items() if d-radius <= edge <= d+radius)
        return found

records, rejected = [], []
for folder in sorted(root.iterdir()):
    label = FOLDER_LABEL.get(folder.name.lower())
    if not folder.is_dir() or label not in CLASSES: continue
    for path in sorted(folder.rglob("*")):
        if not path.is_file() or path.suffix.lower() not in (".jpg", ".jpeg", ".png", ".webp"): continue
        try:
            if path.stat().st_size > 4 * 1024 * 1024: raise ValueError("Exceeds current serving 4MB limit")
            rgb = read_rgb(path, resize=False)
            digest = hashlib.sha256(str(rgb.shape).encode() + rgb.tobytes()).hexdigest()
            gray = np.asarray(Image.fromarray(rgb).convert("L").resize((9, 8), Image.Resampling.LANCZOS))
            dh = sum(int(v) << i for i, v in enumerate((gray[:, 1:] > gray[:, :-1]).ravel()))
            records.append({"path": path.relative_to(root).as_posix(), "label": CLASSES.index(label),
                            "class_name": label, "sha256_pixels": digest, "dhash": dh})
        except (OSError, ValueError, Image.DecompressionBombError) as exc:
            rejected.append({"path": str(path), "error": str(exc)})
if not records: raise RuntimeError("No readable training images")
uf = UnionFind(len(records)); tree = BKTree(); exact = {}; manual = {}
if GROUPS_CSV:
    with open(GROUPS_CSV, encoding="utf-8-sig", newline="") as stream:
        for row in csv.DictReader(stream):
            if not row.get("group_id"): raise ValueError("Empty group_id")
            manual[row["path"].replace("\\", "/")] = row["group_id"]
    unknown = set(manual) - {r["path"] for r in records}
    if unknown: raise ValueError(f"Unknown paths in GROUPS_CSV: {sorted(unknown)[:5]}")
manual_first = {}
for i, r in enumerate(records):
    if r["sha256_pixels"] in exact: uf.union(i, exact[r["sha256_pixels"]])
    exact[r["sha256_pixels"]] = i
    for neighbor in tree.query(r["dhash"], NEAR_HASH_DISTANCE): uf.union(i, neighbor)
    tree.add(r["dhash"], i)
    group = manual.get(r["path"])
    if group is not None:
        if group in manual_first: uf.union(i, manual_first[group])
        manual_first[group] = i
groups = defaultdict(list)
for i, record in enumerate(records):
    record["group_id"] = str(uf.find(i))
    groups[uf.find(i)].append(record)
# Quarantine groups containing conflicting material labels, do not silently relabel them.
conflicts = [g for g in groups.values() if len({r["label"] for r in g}) > 1]
write_json(REPORTS / "conflicting_groups.json", conflicts)
clean = [g for g in groups.values() if len({r["label"] for r in g}) == 1]
rng = np.random.default_rng(SEED); rows = []; duplicate_removed = 0
for label in range(len(CLASSES)):
    bucket = [g for g in clean if g[0]["label"] == label]
    rng.shuffle(bucket)
    if len(bucket) < 10: raise ValueError(f"Too few independent groups for {CLASSES[label]}: {len(bucket)}")
    nval = max(1, round(.15 * len(bucket))); ntest = nval
    for k, group in enumerate(bucket):
        split = "validation" if k < nval else "test" if k < nval + ntest else "train"
        seen_pixels = set()
        for r in group:
            if r["sha256_pixels"] in seen_pixels:
                duplicate_removed += 1; continue
            seen_pixels.add(r["sha256_pixels"])
            rows.append({**r, "split": split})
df = pd.DataFrame(rows)
assert df.groupby("group_id")["split"].nunique().max() == 1
assert df.groupby("sha256_pixels")["split"].nunique().max() == 1
df.to_csv(REPORTS / "splits.csv", index=False)
counts = pd.crosstab(df.class_name, df.split).reindex(CLASSES)
print(counts); counts.to_csv(REPORTS / "split_distribution.csv")
counts.plot.bar(figsize=(12, 4)); plt.tight_layout(); plt.savefig(REPORTS / "split_distribution.png"); plt.show()
write_json(REPORTS / "data_audit.json", {"readable": len(records), "retained": len(rows),
    "exact_duplicates_removed": duplicate_removed, "conflict_groups_quarantined": len(conflicts),
    "rejected": rejected, "group_count": len(clean), "manual_grouping": bool(GROUPS_CSV),
    "limitation": "dHash is heuristic; without object/session IDs different photos of one object can still leak.",
    "dataset_source": "https://www.kaggle.com/datasets/mostafaabla/garbage-classification",
    "dataset_version": "Record attached Kaggle dataset version in the saved notebook version."})
manifest_sha = hashlib.sha256((REPORTS / "splits.csv").read_bytes()).hexdigest()

# %% Streaming input / training-only augmentation
augmentation = tf.keras.Sequential([
    tf.keras.layers.RandomFlip("horizontal", seed=SEED),
    tf.keras.layers.RandomRotation(.08, seed=SEED+1),
    tf.keras.layers.RandomZoom(.15, seed=SEED+2),
    tf.keras.layers.RandomTranslation(.08, .08, seed=SEED+3),
    tf.keras.layers.RandomContrast(.15, seed=SEED+4)], name="train_augmentation")

def py_decode(path):
    return read_rgb(path.numpy().decode("utf-8"))

def decode(path, label):
    image = tf.py_function(py_decode, [path], Tout=tf.uint8)
    image.set_shape((224, 224, 3))
    return tf.cast(image, tf.float32), label

def dataset(split, training=False):
    subset = df[df.split == split]
    ds = tf.data.Dataset.from_tensor_slices((
        np.array([str(root / p) for p in subset.path]), subset.label.to_numpy(dtype=np.int32)))
    if training: ds = ds.shuffle(len(subset), seed=SEED, reshuffle_each_iteration=True)
    ds = ds.map(decode, num_parallel_calls=tf.data.AUTOTUNE)
    ds = ds.batch(BATCH, drop_remainder=training)
    if training:
        ds = ds.map(lambda x, y: (tf.clip_by_value(augmentation(x, training=True), 0., 255.), y),
                    num_parallel_calls=tf.data.AUTOTUNE)
    options = tf.data.Options()
    options.experimental_distribute.auto_shard_policy = tf.data.experimental.AutoShardPolicy.DATA
    return ds.with_options(options).prefetch(tf.data.AUTOTUNE)

train_ds, val_ds = dataset("train", True), dataset("validation")
if len(df[df.split == "train"]) < BATCH: raise ValueError("Global batch larger than training set")
freq = np.bincount(df[df.split == "train"].label, minlength=len(CLASSES))
class_weights = {i: min(5., float(freq.sum() / (len(CLASSES) * n))) for i, n in enumerate(freq)}

# %% Model / warm-up / fine-tuning with both GPUs
def build_model(weights="imagenet"):
    inputs = tf.keras.Input((224, 224, 3), dtype="float32", name="rgb_0_255")
    if BACKBONE == "MobileNetV2":
        backbone = tf.keras.applications.MobileNetV2(include_top=False, weights=weights, input_shape=(224,224,3))
        x = tf.keras.layers.Rescaling(1/127.5, offset=-1)(inputs)
    elif BACKBONE in ("EfficientNetV2B0", "EfficientNetV2B2"):
        backbone = getattr(tf.keras.applications, BACKBONE)(include_top=False, weights=weights,
            input_shape=(224,224,3), include_preprocessing=True)
        x = inputs  # EfficientNetV2 includes scaling: never divide by 255 again.
    else: raise ValueError("Unsupported BACKBONE")
    backbone.trainable = False
    x = backbone(x, training=False)  # Freeze BatchNorm statistics during fine-tuning too.
    x = tf.keras.layers.GlobalAveragePooling2D()(x)
    x = tf.keras.layers.Dropout(.35)(x)
    outputs = tf.keras.layers.Dense(len(CLASSES), activation="softmax", dtype="float32", name="class_probabilities")(x)
    return tf.keras.Model(inputs, outputs, name="ecosort_classifier"), backbone

def compile_model(model, lr):
    model.compile(optimizer=tf.keras.optimizers.Adam(learning_rate=lr),
        loss="sparse_categorical_crossentropy", metrics=["accuracy"], jit_compile=False)

def callbacks(stage, lr):
    return [tf.keras.callbacks.ModelCheckpoint(str(CHECKPOINTS / f"{stage}.keras"),
                monitor="val_loss", save_best_only=True),
            tf.keras.callbacks.EarlyStopping(monitor="val_loss", patience=6, restore_best_weights=True),
            tf.keras.callbacks.ReduceLROnPlateau(monitor="val_loss", factor=.5, patience=2, min_lr=lr/100),
            tf.keras.callbacks.CSVLogger(str(REPORTS / f"{stage}_epochs.csv")),
            tf.keras.callbacks.TerminateOnNaN()]

started = time.perf_counter()
with strategy.scope():
    model, backbone = build_model()
    compile_model(model, HEAD_LR)
model.summary()
histories = {}
histories["head"] = model.fit(train_ds, validation_data=val_ds, epochs=HEAD_EPOCHS,
    class_weight=class_weights, callbacks=callbacks("head", HEAD_LR), verbose=2).history
head_loss = float(model.evaluate(val_ds, verbose=0)[0])
if not np.isfinite(head_loss) or not (CHECKPOINTS / "head.keras").exists():
    raise RuntimeError("Warm-up produced invalid loss or no checkpoint; inspect training logs")
fine_loss = None; selected = "head"
if FINE_EPOCHS > 0:
    with strategy.scope():
        backbone.trainable = True
        for layer in backbone.layers: layer.trainable = False
        for layer in backbone.layers[-UNFREEZE_LAST:]:
            if not isinstance(layer, tf.keras.layers.BatchNormalization): layer.trainable = True
        compile_model(model, FINE_LR)
    histories["fine_tune"] = model.fit(train_ds, validation_data=val_ds, epochs=FINE_EPOCHS,
        class_weight=class_weights, callbacks=callbacks("fine_tune", FINE_LR), verbose=2).history
    fine_loss = float(model.evaluate(val_ds, verbose=0)[0])
    if np.isfinite(fine_loss) and fine_loss < head_loss: selected = "fine_tune"
write_json(REPORTS / "training_history.json", histories)
fig, ax = plt.subplots(1, 2, figsize=(12, 4))
for col, metric in enumerate(("loss", "accuracy")):
    for prefix in ("", "val_"):
        values = [v for h in histories.values() for v in h[prefix+metric]]
        ax[col].plot(range(1, len(values)+1), values, label=prefix+metric)
    ax[col].axvline(len(histories["head"]["loss"]), color="gray", linestyle="--")
    ax[col].legend(); ax[col].set_xlabel("Epoch")
plt.tight_layout(); plt.savefig(REPORTS / "training_curves.png"); plt.show()
print("Selected checkpoint:", selected, "head val loss:", head_loss, "fine val loss:", fine_loss)

# %% CPU-friendly float32 export / validation threshold (NOT test)
# Rebuild in float32 rather than retaining GPU mixed_float16 in the deployed model.
with strategy.scope():
    selected_model = tf.keras.models.load_model(CHECKPOINTS / f"{selected}.keras", compile=False, safe_mode=True)
tf.keras.mixed_precision.set_global_policy("float32")
with tf.device("/CPU:0"):
    serving_model, _ = build_model(weights=None)
    serving_model.set_weights(selected_model.get_weights())
    serving_model.save(ARTIFACTS / "model.keras")
    exported = tf.keras.models.load_model(ARTIFACTS / "model.keras", compile=False, safe_mode=True)

def predict_rows(model, split):
    # CPU serving path, Pillow/EXIF/bilinear preprocessing matches existing API.
    part = df[df.split == split]
    probabilities = []
    with tf.device("/CPU:0"):
        for start in range(0, len(part), 32):
            batch = np.stack([read_rgb(root / p) for p in part.path.iloc[start:start+32]]).astype(np.float32)
            probabilities.append(np.asarray(model(batch, training=False)))
    return np.concatenate(probabilities), part.label.to_numpy(dtype=int)

val_p, val_y = predict_rows(exported, "validation")
assert val_p.shape == (len(val_y), len(CLASSES)) and np.isfinite(val_p).all()
np.testing.assert_allclose(val_p.sum(axis=1), 1., atol=1e-4)
probe = np.stack([read_rgb(root / p) for p in df[df.split == "validation"].path.iloc[:8]]).astype(np.float32)
before = np.asarray(selected_model(probe, training=False)); after = np.asarray(exported(probe, training=False))
export_max_abs = float(np.max(np.abs(before-after)))
agreement = float(np.mean(before.argmax(1) == after.argmax(1)))
write_json(REPORTS / "export_parity.json", {"max_abs_probability_difference": export_max_abs,
    "top1_agreement": agreement, "images": len(probe), "note": "Mixed vs float32, small validation probe only"})
if agreement < 1 or export_max_abs > .03: raise RuntimeError("Export parity failed; inspect before deployment")

def wilson_lower(correct, n, z=1.96):
    if not n: return 0.
    p = correct / n
    return (p + z*z/(2*n) - z*np.sqrt(p*(1-p)/n + z*z/(4*n*n))) / (1 + z*z/n)

calibration = []
for threshold in np.arange(MIN_THRESHOLD, 1., .005):
    mask = val_p.max(1) >= threshold; n = int(mask.sum())
    correct = int((val_p.argmax(1)[mask] == val_y[mask]).sum())
    calibration.append({"threshold": float(threshold), "accepted": n,
        "coverage": n/len(val_y), "accepted_accuracy": correct/n if n else None,
        "wilson95_lower": wilson_lower(correct, n)})
eligible = [r for r in calibration if r["accepted"] >= MIN_ACCEPTED_VALIDATION
            and r["wilson95_lower"] >= TARGET_ACCEPTED_ACCURACY]
chosen = max(eligible, key=lambda r: r["coverage"]) if eligible else calibration[0]
threshold = chosen["threshold"]
write_json(REPORTS / "threshold_calibration.json", {"selected": chosen, "target_met": bool(eligible),
    "target": TARGET_ACCEPTED_ACCURACY, "selection_set": "validation", "grid": calibration,
    "caveat": "Wilson bound is descriptive; threshold search is adaptive, not an independent guarantee."})
np.savez_compressed(REPORTS / "validation_predictions.npz", labels=val_y, probabilities=val_p)
print("Validation accuracy:", accuracy_score(val_y, val_p.argmax(1)), "threshold:", threshold,
      "target met:", bool(eligible))
if not eligible: print("Target NOT met; threshold remains minimum. Do not claim 97% accepted accuracy.")
metadata = {"class_names": CLASSES, "input_shape": [224,224,3], "input_dtype": "float32",
    "input_range": [0,255], "preprocessing_in_model": "Rescaling(1/127.5, offset=-1)" if BACKBONE == "MobileNetV2"
        else "EfficientNetV2 built-in preprocessing (RGB 0..255)",
    "resize": "Pillow bilinear, direct resize, EXIF transpose, RGB", "threshold": threshold,
    "threshold_target_met_on_validation": bool(eligible), "mode": MODE,
    "compatible_with_current_ecosort_label_contract": MODE == "game4",
    "deployment_note": "Requires Windows CPU/backend acceptance before replacing production artifacts."
        if MODE == "game4" else "Research only: current backend/Unity reject 12 labels. Extend enums/UI/API first.",
    "model_sha256": hashlib.sha256((ARTIFACTS / "model.keras").read_bytes()).hexdigest(),
    "split_manifest_sha256": manifest_sha, "seed": SEED, "backbone": BACKBONE,
    "tensorflow": tf.__version__, "keras": tf.keras.__version__, "python": platform.python_version(),
    "validation_accuracy": float(accuracy_score(val_y, val_p.argmax(1))), "selected_stage": selected,
    "head_validation_loss": head_loss, "fine_tune_validation_loss": fine_loss,
    "epochs_run": {k: len(h["loss"]) for k,h in histories.items()},
    "training_and_export_seconds": time.perf_counter()-started, "export_precision": "float32",
    "selection": "validation only; test not used for checkpoint or threshold selection"}
write_json(ARTIFACTS / "metadata.json", metadata)

# %% Held-out evaluation — run ONLY after validation settings are locked
if RUN_TEST:
    # Separate marker prevents accidental repeated test inspection for hyperparameter tuning.
    marker = REPORTS / "test_started.json"
    if marker.exists(): raise RuntimeError("Test already started for this run. Do not tune against test.")
    write_json(marker, {"time": time.time(), "model_sha256": metadata["model_sha256"]})
    test_p, test_y = predict_rows(exported, "test")
    test_pred = test_p.argmax(1); mask = test_p.max(1) >= threshold
    report = classification_report(test_y, test_pred, labels=list(range(len(CLASSES))),
                                   target_names=CLASSES, output_dict=True, zero_division=0)
    metrics = {"evaluation_set": "held_out_test", "images": len(test_y),
        "accuracy": float(accuracy_score(test_y, test_pred)), "macro_f1": float(f1_score(test_y, test_pred, average="macro")),
        "threshold": threshold, "accepted_count": int(mask.sum()), "coverage": float(mask.mean()),
        "accepted_accuracy": float((test_pred[mask] == test_y[mask]).mean()) if mask.any() else None,
        "class_names": CLASSES, "per_class": report, "model_sha256": metadata["model_sha256"],
        "comparison_note": "New 12-class/new-split results are NOT directly comparable with old 4-class 372-image score."}
    write_json(REPORTS / "metrics.json", metrics)
    np.savez_compressed(REPORTS / "test_predictions.npz", labels=test_y, probabilities=test_p)
    pd.DataFrame({"path": df[df.split == "test"].path, "true": test_y, "predicted": test_pred,
                  "confidence": test_p.max(1), "accepted": mask}).to_csv(REPORTS / "test_predictions.csv", index=False)
    cm = confusion_matrix(test_y, test_pred, labels=list(range(len(CLASSES))))
    fig, ax = plt.subplots(figsize=(11, 9)); image = ax.imshow(cm, cmap="Blues")
    ax.set_xticks(range(len(CLASSES)), CLASSES, rotation=60, ha="right")
    ax.set_yticks(range(len(CLASSES)), CLASSES); ax.set_xlabel("Predicted"); ax.set_ylabel("True")
    for i in range(len(CLASSES)):
        for j in range(len(CLASSES)): ax.text(j, i, str(cm[i,j]), ha="center", va="center", fontsize=8)
    fig.colorbar(image, ax=ax); plt.tight_layout(); plt.savefig(REPORTS / "confusion_matrix.png"); plt.show()
    print(json.dumps(metrics, indent=2))
else:
    print("Test NOT run. Review validation; then set RUN_TEST=True in a new cell and rerun ONLY this cell.")

# %% Package / portable inference helper / download
inference_source = '''import hashlib, io, json
from pathlib import Path
import numpy as np
from PIL import Image, ImageOps
import tensorflow as tf

class Predictor:
    def __init__(self, artifacts):
        artifacts = Path(artifacts)
        self.metadata = json.loads((artifacts / "metadata.json").read_text(encoding="utf-8"))
        path = artifacts / "model.keras"
        if hashlib.sha256(path.read_bytes()).hexdigest() != self.metadata["model_sha256"]:
            raise ValueError("Model checksum mismatch")
        self.model = tf.keras.models.load_model(path, compile=False, safe_mode=True)
    def predict(self, image_bytes):
        if not image_bytes or len(image_bytes) > 4*1024*1024: raise ValueError("Image must be <=4MB")
        with Image.open(io.BytesIO(image_bytes)) as im:
            if im.format not in ("JPEG", "PNG", "WEBP") or im.width*im.height > 20_000_000:
                raise ValueError("Unsupported image")
            rgb = ImageOps.exif_transpose(im).convert("RGB").resize((224,224), Image.Resampling.BILINEAR)
            x = np.asarray(rgb, dtype=np.float32)[None]
        p = np.asarray(self.model(x, training=False))[0]
        if not np.isfinite(p).all() or not np.isclose(p.sum(), 1, atol=1e-4): raise RuntimeError("Invalid model output")
        i = int(p.argmax()); confidence = float(p[i])
        return {"class_name": self.metadata["class_names"][i], "confidence": confidence,
                "accepted": confidence >= self.metadata["threshold"]}
'''
(RUN / "predict_standalone.py").write_text(inference_source, encoding="utf-8")
(RUN / "requirements-serving.txt").write_text(
    f"tensorflow=={tf.__version__}\nnumpy=={np.__version__}\nPillow=={Image.__version__}\n", encoding="utf-8")
write_json(RUN / "deployment.json", {"mode": MODE, "current_game_labels_compatible": MODE == "game4",
    "test_completed": (REPORTS / "metrics.json").exists(), "production_model_replaced": False,
    "next": "Download zip, verify checksum, test exported model on target CPU and real photos before deployment."})
archive = RUN.parent / f"{RUN_NAME}-handoff.zip"
with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED) as z:
    for file in sorted(RUN.rglob("*")):
        if file.is_file() and "checkpoints" not in file.relative_to(RUN).parts:
            z.write(file, file.relative_to(RUN))
from IPython.display import FileLink, display
display(FileLink(str(archive)))
print("Saved", archive, "Test completed:", (REPORTS / "metrics.json").exists())
