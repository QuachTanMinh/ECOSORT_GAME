"""Offline structural and synthetic CPU smoke checks; does not measure model quality."""
import ast
import json
import os
from pathlib import Path
import re
import tempfile
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent

def check_structure():
    notebook = json.loads((ROOT / "EcoSort_Kaggle_T4x2_Advanced.ipynb").read_text(encoding="utf-8"))
    import nbformat
    nbformat.validate(nbformat.from_dict(notebook))
    codes = ["".join(c["source"]) for c in notebook["cells"] if c["cell_type"] == "code"]
    assert len(codes) == 8
    for code in codes: ast.parse(code)
    print("Notebook schema and all 8 code cells: OK")

def smoke():
    os.environ["TF_NUM_INTEROP_THREADS"] = "2"
    os.environ["TF_NUM_INTRAOP_THREADS"] = "2"
    os.environ["MPLBACKEND"] = "Agg"
    source = (ROOT / "train_t4x2.py").read_text(encoding="utf-8")
    chunks = [c.split("\n", 1)[1] for c in re.split(r"^# %% ", source, flags=re.MULTILINE)[1:]]
    # Synthetic random pictures test wiring only, never ecological/accuracy claims.
    workspace = ROOT.parents[1] / "report-work" / "kaggle-smoke"
    workspace.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(dir=workspace) as folder:
        temp = Path(folder); dataset = temp / "input"; dataset.mkdir()
        rng = np.random.default_rng(920)
        for cls in ("plastic", "metal", "paper", "biological"):
            target = dataset / cls; target.mkdir()
            for i in range(12):
                Image.fromarray(rng.integers(0,256,(96,96,3),dtype=np.uint8)).save(target / f"{i}.png")
        (dataset / "plastic" / "duplicate.png").write_bytes((dataset / "plastic" / "0.png").read_bytes())
        (dataset / "metal" / "conflict.png").write_bytes((dataset / "plastic" / "1.png").read_bytes())
        scope = {"__name__": "__smoke__"}
        for i, code in enumerate(chunks):
            code = code.replace('Path("/kaggle/working")', f'Path({str(temp)!r})')
            code = code.replace('def build_model(weights="imagenet"):', 'def build_model(weights=None):')
            exec(compile(code, f"notebook-cell-{i+1}", "exec"), scope)
            if i == 0:
                scope.update(MODE="game4", DATASET_ROOT=str(dataset), RUN_NAME="synthetic-only",
                    BACKBONE="MobileNetV2", PER_GPU_BATCH=4, HEAD_EPOCHS=1, FINE_EPOCHS=1,
                    UNFREEZE_LAST=5, REQUIRE_GPU=False, USE_MIXED_PRECISION=False,
                    RUN_TEST=True, NEAR_HASH_DISTANCE=0, MIN_ACCEPTED_VALIDATION=2)
            print(f"Smoke cell {i+1}: OK", flush=True)
        archive = scope["archive"]
        assert archive.is_file()
        metrics = json.loads((scope["REPORTS"] / "metrics.json").read_text())
        assert metrics["images"] == 8
        audit = json.loads((scope["REPORTS"] / "data_audit.json").read_text())
        assert audit["exact_duplicates_removed"] == 1
        assert audit["conflict_groups_quarantined"] == 1
        assert scope["metadata"]["compatible_with_current_ecosort_label_contract"]
        print("Synthetic CPU: warm-up/fine-tune/export/reload/calibration/test/ZIP OK")

def check_backbone():
    os.environ["TF_NUM_INTEROP_THREADS"] = "2"
    os.environ["TF_NUM_INTRAOP_THREADS"] = "2"
    import tensorflow as tf
    source = ast.parse((ROOT / "train_t4x2.py").read_text(encoding="utf-8"))
    node = next(n for n in source.body if isinstance(n, ast.FunctionDef) and n.name == "build_model")
    scope = {"tf": tf, "BACKBONE": "EfficientNetV2B0", "CLASSES": list(range(12))}
    exec(compile(ast.Module(body=[node], type_ignores=[]), "model-builder", "exec"), scope)
    model, _ = scope["build_model"](weights=None)
    x = np.random.default_rng(1).uniform(0,255,(1,224,224,3)).astype(np.float32)
    expected = np.asarray(model(x, training=False))
    with tempfile.TemporaryDirectory() as tmp:
        target = Path(tmp) / "model.keras"; model.save(target)
        loaded = tf.keras.models.load_model(target, compile=False, safe_mode=True)
        actual = np.asarray(loaded(x, training=False))
    assert actual.shape == (1,12)
    np.testing.assert_allclose(actual.sum(1), 1., atol=1e-5)
    np.testing.assert_allclose(actual, expected, atol=1e-6)
    print("EfficientNetV2B0: 12-class 0..255 input / safe export-reload parity OK (random weights only)")

if __name__ == "__main__":
    import sys
    check_structure()
    if "--smoke" in sys.argv: smoke()
    if "--backbone-check" in sys.argv: check_backbone()
