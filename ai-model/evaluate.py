"""Evaluate a frozen model on the held-out test manifest or independent labeled phone photos."""
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
os.environ.setdefault("MPLCONFIGDIR", str(Path(__file__).parent / ".matplotlib"))

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
import tensorflow as tf
from sklearn.metrics import accuracy_score, classification_report, confusion_matrix, ConfusionMatrixDisplay, f1_score

from data import CLASS_NAMES, FOLDERS, image_fingerprints, load_rows
from inference import Predictor


def evaluate(dataset: Path, manifest: Path, artifacts: Path, reports: Path, external: Path | None = None):
    predictor = Predictor(artifacts)
    metadata = predictor.metadata
    if hashlib.sha256(manifest.read_bytes()).hexdigest() != metadata["split_manifest_sha256"]:
        raise ValueError("Manifest differs from the one used to train this model")
    with manifest.open(encoding="utf-8", newline="") as stream:
        all_rows = list(csv.DictReader(stream))
    if external:
        known = {row["sha256_pixels"] for row in all_rows}
        rows = []
        for label, folder in enumerate(FOLDERS):
            for path in sorted((external / folder).glob("*")):
                if not path.is_file() or path.suffix.lower() not in (".png", ".jpg", ".jpeg", ".webp"):
                    continue
                digest, _, _ = image_fingerprints(path)
                if digest in known:
                    raise ValueError(f"External photo duplicates original dataset: {path}")
                rows.append({"path": path.relative_to(external).as_posix(), "label": label})
        if not rows or set(int(row["label"]) for row in rows) != {0, 1, 2}:
            raise ValueError("External evaluation requires labeled plastic/, metal/ and paper/ images")
        dataset = external
    else:
        rows = [row for row in all_rows if row["split"] == "test"]
    images, labels = load_rows(rows, dataset)
    probabilities = np.concatenate([np.asarray(predictor.model(images[i:i + 32], training=False))
                                    for i in range(0, len(images), 32)])
    predicted = probabilities.argmax(axis=1)
    confidence = probabilities.max(axis=1)
    accepted = confidence >= predictor.threshold
    report = classification_report(labels, predicted, labels=list(range(len(CLASS_NAMES))), target_names=CLASS_NAMES,
                                   output_dict=True, zero_division=0)
    matrix = confusion_matrix(labels, predicted, labels=list(range(len(CLASS_NAMES))))
    latencies = []
    predictor.predict((dataset / rows[0]["path"]).read_bytes())  # Warmup, not measured.
    for row in rows[:30]:
        content = (dataset / row["path"]).read_bytes()
        start = time.perf_counter(); predictor.predict(content)
        latencies.append((time.perf_counter() - start) * 1000)
    accuracy = float(accuracy_score(labels, predicted)); macro_f1 = float(f1_score(labels, predicted, average="macro"))
    metrics = {"evaluation_set": "external_photos" if external else "held_out_test", "images": len(rows),
               "accuracy": accuracy, "macro_f1": macro_f1, "macro_precision": report["macro avg"]["precision"],
               "macro_recall": report["macro avg"]["recall"], "class_names": CLASS_NAMES,
               "confusion_matrix": matrix.tolist(), "threshold": predictor.threshold,
               "accepted_count": int(accepted.sum()), "coverage": float(accepted.mean()),
               "accepted_accuracy": float((predicted[accepted] == labels[accepted]).mean()) if accepted.any() else None,
               "all_images_metrics_include_rejected": True, "target_accuracy_85_met": accuracy >= .85,
               "target_macro_f1_080_met": macro_f1 >= .80,
               "latency_warm_median_ms": float(np.median(latencies)), "latency_warm_p95_ms": float(np.percentile(latencies, 95)),
               "latency_samples": len(latencies), "latency_includes": "decode/EXIF/RGB/resize/model; excludes disk IO and HTTP",
               "platform": platform.platform(), "tensorflow": tf.__version__, "gpu_devices": [d.name for d in tf.config.list_physical_devices("GPU")],
               "model_sha256": metadata["model_sha256"], "split_manifest_sha256": metadata["split_manifest_sha256"]}
    reports.mkdir(parents=True, exist_ok=True)
    from evaluation_curves import diagnostic_curves
    curve_summary = diagnostic_curves(labels, probabilities, reports, predictor.threshold,
                                      "external_photos" if external else "test")
    metrics["macro_roc_auc_ovr"] = curve_summary["macro_roc_auc_ovr"]
    metrics["macro_average_precision"] = curve_summary["macro_average_precision"]
    metrics["ece_10_equal_width_bins"] = curve_summary["ece_10_equal_width_bins"]
    (reports / "metrics.json").write_text(json.dumps(metrics, indent=2), encoding="utf-8")
    (reports / "classification_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    with (reports / "predictions.csv").open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=["path", "true_class", "predicted_class", "confidence", "accepted", "correct"])
        writer.writeheader()
        for row, truth, prediction, score, accept in zip(rows, labels, predicted, confidence, accepted):
            writer.writerow({"path": row["path"], "true_class": CLASS_NAMES[truth], "predicted_class": CLASS_NAMES[prediction],
                             "confidence": float(score), "accepted": bool(accept), "correct": bool(truth == prediction)})
    fig, ax = plt.subplots(figsize=(6, 5))
    ConfusionMatrixDisplay(matrix, display_labels=CLASS_NAMES).plot(ax=ax, cmap="Greens", colorbar=False)
    ax.set_title(f"EcoSort MobileNetV2 / {metrics['evaluation_set']} (n={len(rows)})")
    fig.tight_layout(); fig.savefig(reports / "confusion_matrix.png", dpi=180); plt.close(fig)
    errors = np.flatnonzero(labels != predicted)[:12]
    if len(errors):
        fig, axes = plt.subplots(int(np.ceil(len(errors) / 4)), 4, figsize=(12, 3 * int(np.ceil(len(errors) / 4))), squeeze=False)
        for ax in axes.flat:
            ax.axis("off")
        for ax, i in zip(axes.flat, errors):
            ax.imshow(images[i]); ax.set_title(f"True: {CLASS_NAMES[labels[i]]} / Pred: {CLASS_NAMES[predicted[i]]}\n{confidence[i]:.1%}", fontsize=10)
        fig.suptitle("First misclassified test images (not training examples)"); fig.tight_layout()
        fig.savefig(reports / "misclassified_examples.png", dpi=140); plt.close(fig)
    per_class = "\n".join(f"| {name} | {report[name]['precision']:.3f} | {report[name]['recall']:.3f} | {report[name]['f1-score']:.3f} | {int(report[name]['support'])} |" for name in CLASS_NAMES)
    content = f"""# Báo cáo đánh giá EcoSort MobileNetV2

Model: MobileNetV2 pretrained ImageNet + Global Average Pooling + Dense 128 + Dropout + Softmax 3 lớp.
Tập đánh giá: **{metrics['evaluation_set']}**, **{len(rows)} ảnh**. Train/validation/test được cố định trước huấn luyện; chọn checkpoint và ngưỡng bằng validation, không dùng test để chọn model.

| Chỉ số trên toàn bộ ảnh | Kết quả |
|---|---:|
| Accuracy | {accuracy:.2%} |
| Macro Precision | {metrics['macro_precision']:.4f} |
| Macro Recall | {metrics['macro_recall']:.4f} |
| Macro F1 | {macro_f1:.4f} |
| Mục tiêu nội bộ Accuracy ≥85% | {'Đạt' if accuracy >= .85 else 'Chưa đạt'} |
| Mục tiêu nội bộ Macro F1 ≥0.80 | {'Đạt' if macro_f1 >= .80 else 'Chưa đạt'} |

Các mục tiêu này không phải điểm chấm hoặc bảo đảm kết quả ngoài thực tế.

| Lớp | Precision | Recall | F1 | Số ảnh |
|---|---:|---:|---:|---:|
{per_class}

![Confusion matrix](confusion_matrix.png)

Ngưỡng confidence chọn từ validation: **{predictor.threshold:.2f}**. Coverage trên tập đánh giá: **{metrics['coverage']:.2%}**; accepted accuracy: **{metrics['accepted_accuracy']}**. Accuracy/Macro F1 phía trên tính cả ảnh bị từ chối, không chỉ tính ảnh confidence cao. Chỉ số accepted accuracy là độ chính xác phân loại trong các ảnh được nhận, không phải calibration hoặc OOD accuracy.

Thời gian inference sau warmup, bao gồm giải mã/resize và model, không gồm HTTP/đọc file: median **{metrics['latency_warm_median_ms']:.1f} ms**, p95 **{metrics['latency_warm_p95_ms']:.1f} ms**, {len(latencies)} ảnh. TensorFlow {tf.__version__}; GPU thấy bởi TensorFlow: {metrics['gpu_devices']}.

## Giới hạn

- Dataset hiện tại có nền và điều kiện chụp hẹp. Kết quả này chưa chứng minh chất lượng trên ảnh điện thoại người dùng.
- Không có object/session ID gốc; nhóm ảnh gần nhau bằng dHash chỉ giảm rủi ro rò rỉ, không bảo đảm object-disjoint.
- Chỉ nhận diện một vật phẩm chính thuộc Plastic/Metal/Paper/Organic; không object detection hoặc phân loại nhiều vật phẩm.
- Confidence cao không bảo đảm ảnh thuộc bốn lớp. Chưa có tập unknown/OOD để đánh giá khả năng từ chối vật phẩm lạ.
- Chưa kiểm thử Unity → FastAPI → model thật hoặc ảnh chụp điện thoại độc lập.

Model SHA256: `{metadata['model_sha256']}`.
Manifest SHA256: `{metadata['split_manifest_sha256']}`.
Chi tiết từng ảnh: `predictions.csv`; số liệu máy đọc: `metrics.json`, `classification_report.json`.

## Đồ thị bổ sung

- `roc_pr_curves.png`: ROC và Precision–Recall one-vs-rest, AUC/AP từng lớp.
- `confusion_matrix_normalized.png`: tỉ lệ theo nhãn thật.
- `reliability_confidence.png`: reliability diagram và phân bố confidence (ECE chẩn đoán, không phải bảo đảm calibration/OOD).
- `threshold_risk_coverage.png`: coverage, accepted accuracy và selective risk theo ngưỡng; chỉ chẩn đoán test, không dùng chọn lại ngưỡng.
- `curve_metrics.json`, `probabilities.npz`: dữ liệu tái tạo đồ thị.
"""
    (reports / "EVALUATION.md").write_text(content, encoding="utf-8")
    print(json.dumps(metrics, indent=2), flush=True)
    return metrics


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    base = Path(__file__).parent
    parser.add_argument("--dataset", type=Path, default=base.parent / "Dataset/dataset-four-classes")
    parser.add_argument("--manifest", type=Path, default=base / "reports-organic/splits.csv")
    parser.add_argument("--artifacts", type=Path, default=base / "artifacts-organic")
    parser.add_argument("--reports", type=Path, default=base / "reports-organic")
    parser.add_argument("--external-dataset", type=Path)
    args = parser.parse_args()
    if args.external_dataset and args.reports == base / "reports-organic":
        parser.error("Use a separate --reports directory for external evaluation to preserve the held-out test report")
    evaluate(args.dataset, args.manifest, args.artifacts, args.reports, args.external_dataset)
