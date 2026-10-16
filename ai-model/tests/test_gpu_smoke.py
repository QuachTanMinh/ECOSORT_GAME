"""Optional CPU plumbing check, NOT Kaggle GPU or model quality validation."""
import csv
import os
from pathlib import Path

import pytest


@pytest.mark.skipif(os.environ.get("ECOSORT_GPU_SMOKE") != "1", reason="Opt-in two-stage CPU smoke test")
def test_two_stage_export_and_evaluation(tmp_path, monkeypatch):
    import gpu_train
    from evaluate import evaluate
    base = Path(__file__).parents[1]
    dataset = base.parent / "Dataset/dataset-four-classes"
    with (base / "reports-organic/splits.csv").open(newline="", encoding="utf-8") as stream:
        original = list(csv.DictReader(stream))
    chosen = []
    for label in range(4):
        samples = [r for r in original if int(r["label"]) == label][:4]
        for row, split in zip(samples, ("train", "train", "validation", "test")):
            chosen.append({**row, "split": split, "group_id": row["path"]})
    reports = tmp_path / "reports"; reports.mkdir()
    with (reports / "splits.csv").open("w", newline="", encoding="utf-8") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(chosen[0])); writer.writeheader(); writer.writerows(chosen)
    reads = []
    actual = gpu_train.load_rows
    def recorded(rows, root):
        reads.extend(r["split"] for r in rows)
        return actual(rows, root)
    monkeypatch.setattr(gpu_train, "load_rows", recorded)
    metadata = gpu_train.train_gpu(dataset, tmp_path, {"require_gpu": False, "weights": None,
        "head_epochs": 1, "fine_tune_epochs": 1, "batch_size": 2})
    assert set(reads) == {"train", "validation"}
    assert metadata["selected_stage"] == ("fine_tune" if metadata["fine_tune_validation_loss"] < metadata["head_validation_loss"] else "head")
    assert metadata["epochs_run"] == {"head": 1, "fine_tune": 1}
    metrics = evaluate(dataset, reports / "splits.csv", tmp_path / "artifacts", reports)
    assert metrics["images"] == 4
    assert (reports / "training_curves.png").is_file()
    assert (reports / "roc_pr_curves.png").is_file()
    assert (reports / "inference_smoke.json").is_file()
