"""Evaluate labelled photos without training or publishing them to cloud storage.

Run from backend: python tools/evaluate_photos.py manifest.csv --output data/photo-review
CSV columns: path,label,object_id,condition. Paths are relative to the manifest directory.
Labels must be assigned from material inspection, never from the model's prediction.
"""
from __future__ import annotations
import argparse
import csv
import hashlib
import json
import sys
import time
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from app.recognition import MODEL_CLASS_NAMES

def read_manifest(path: Path, forbidden: list[Path] = ()) -> list[dict]:
    root = path.resolve().parent
    with path.open(encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        if not {"path", "label", "object_id", "condition"}.issubset(reader.fieldnames or []):
            raise ValueError("Manifest needs path,label,object_id,condition")
        rows = list(reader)
    if not rows:
        raise ValueError("Manifest has no photos")
    hashes = set()
    object_labels = {}
    for row in rows:
        photo = (root / row["path"]).resolve()
        if not photo.is_relative_to(root) or any(photo.is_relative_to(p.resolve()) for p in forbidden):
            raise ValueError("Photos must stay in the review folder, outside forbidden dataset folders")
        if row["label"] not in MODEL_CLASS_NAMES or not row["object_id"].strip() or not row["condition"].strip():
            raise ValueError("Every photo needs a known material, object ID and capture condition")
        previous = object_labels.setdefault(row["object_id"], row["label"])
        if previous != row["label"]:
            raise ValueError("One physical object cannot have conflicting material labels")
        digest = hashlib.sha256(photo.read_bytes()).hexdigest()
        if digest in hashes:
            raise ValueError("Duplicate image bytes in the review set")
        hashes.add(digest)
        row.update(file=photo, sha256=digest)
    return rows

def summarize(rows: list[dict], classes=MODEL_CLASS_NAMES) -> dict:
    accepted = [r for r in rows if r["accepted"]]
    correct = lambda entries: sum(r["label"] == r["predicted"] for r in entries)
    return {
        "photos": len(rows), "physical_objects": len({r["object_id"] for r in rows}),
        "accuracy": correct(rows) / len(rows), "coverage": len(accepted) / len(rows),
        "accepted_accuracy": correct(accepted) / len(accepted) if accepted else None,
        "per_class": {c: {"photos": sum(r["label"] == c for r in rows),
                           "correct": sum(r["label"] == c == r["predicted"] for r in rows)} for c in classes},
        "missing_classes": [c for c in classes if not any(r["label"] == c for r in rows)],
        "confusions": dict(Counter(r["label"] + " -> " + r["predicted"] for r in rows if r["label"] != r["predicted"])),
        "conditions": dict(Counter(r["condition"] for r in rows)),
        "limitation": "Object IDs and provenance are human supplied. No proof of independence from training data without an external audit. Repeated views of one object are not independent objects.",
    }

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--forbid-dir", type=Path, action="append", default=[])
    args = parser.parse_args()
    rows = read_manifest(args.manifest, args.forbid_dir)
    from app.ai_model.inference import Predictor
    predictor = Predictor()
    # Warm up separately; measured latency below excludes first-call tracing.
    predictor.predict(rows[0]["file"].read_bytes())
    for row in rows:
        start = time.perf_counter()
        result = predictor.predict(row["file"].read_bytes())
        row.update(predicted=result["class_name"], confidence=result["confidence"], accepted=result["accepted"],
                   latency_ms=round((time.perf_counter() - start) * 1000, 2))
    args.output.mkdir(parents=True, exist_ok=True)
    fields = ["path", "label", "object_id", "condition", "sha256", "predicted", "confidence", "accepted", "latency_ms"]
    with (args.output / "predictions.csv").open("w", encoding="utf-8-sig", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore"); writer.writeheader(); writer.writerows(rows)
    report = summarize(rows)
    report.update(model_sha256=predictor.metadata["model_sha256"], threshold=predictor.threshold)
    (args.output / "summary.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(report, indent=2, ensure_ascii=False))

if __name__ == "__main__":
    main()
