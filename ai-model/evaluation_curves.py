"""Diagnostic curves for a fixed multiclass classifier; never select thresholds on test."""
from pathlib import Path
import json

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
from sklearn.metrics import (roc_curve, roc_auc_score, precision_recall_curve,
                             average_precision_score, confusion_matrix, ConfusionMatrixDisplay)
from data import CLASS_NAMES


def diagnostic_curves(labels, probabilities, output, threshold, split="test"):
    labels, probabilities = np.asarray(labels), np.asarray(probabilities)
    if probabilities.shape != (len(labels), 3) or set(labels.tolist()) != {0, 1, 2}:
        raise ValueError("Curves require all three labeled classes and Nx3 probabilities")
    if not np.isfinite(probabilities).all() or np.any(probabilities < 0) or not np.allclose(probabilities.sum(1), 1, atol=1e-4):
        raise ValueError("Invalid probabilities")
    output = Path(output); output.mkdir(parents=True, exist_ok=True)
    truth = np.eye(3)[labels]
    predicted = probabilities.argmax(1)
    confidence = probabilities.max(1)
    correct = predicted == labels
    summary = {"split": split, "threshold_from_validation": float(threshold), "classes": {}}

    def save(fig, name):
        fig.tight_layout(); fig.savefig(output / name, dpi=170); plt.close(fig)

    fig, axes = plt.subplots(1, 2, figsize=(12, 5))
    for i, name in enumerate(CLASS_NAMES):
        fpr, tpr, _ = roc_curve(truth[:, i], probabilities[:, i])
        precision, recall, _ = precision_recall_curve(truth[:, i], probabilities[:, i])
        auc = float(roc_auc_score(truth[:, i], probabilities[:, i]))
        ap = float(average_precision_score(truth[:, i], probabilities[:, i]))
        summary["classes"][name] = {"roc_auc_ovr": auc, "average_precision": ap,
                                   "fpr": fpr.tolist(), "tpr": tpr.tolist(),
                                   "precision": precision.tolist(), "recall": recall.tolist()}
        axes[0].plot(fpr, tpr, label=f"{name} AUC={auc:.3f}")
        axes[1].plot(recall, precision, label=f"{name} AP={ap:.3f}")
        axes[1].axhline(truth[:, i].mean(), linestyle=":", alpha=.25)
    summary["macro_roc_auc_ovr"] = float(np.mean([v["roc_auc_ovr"] for v in summary["classes"].values()]))
    summary["macro_average_precision"] = float(np.mean([v["average_precision"] for v in summary["classes"].values()]))
    axes[0].plot([0, 1], [0, 1], "k--", alpha=.5)
    axes[0].set(xlabel="False positive rate", ylabel="True positive rate", title=f"One-vs-rest ROC ({split})")
    axes[1].set(xlabel="Recall", ylabel="Precision", title=f"One-vs-rest Precision-Recall ({split})")
    for ax in axes:
        ax.set_xlim(0, 1); ax.set_ylim(0, 1.02); ax.legend(); ax.grid(alpha=.2)
    save(fig, "roc_pr_curves.png")

    fig, ax = plt.subplots(figsize=(6, 5))
    ConfusionMatrixDisplay(confusion_matrix(labels, predicted, labels=[0, 1, 2], normalize="true"),
                           display_labels=CLASS_NAMES).plot(ax=ax, cmap="Blues", values_format=".2f", colorbar=False)
    ax.set_title(f"Confusion matrix normalized by true class ({split})")
    save(fig, "confusion_matrix_normalized.png")

    bins = []
    for lower, upper in zip(np.linspace(0, 1, 11)[:-1], np.linspace(0, 1, 11)[1:]):
        mask = (confidence >= lower) & ((confidence < upper) if upper < 1 else (confidence <= upper))
        if mask.any():
            bins.append({"lower": float(lower), "upper": float(upper), "count": int(mask.sum()),
                         "confidence": float(confidence[mask].mean()), "accuracy": float(correct[mask].mean())})
    summary["top_label_calibration_bins"] = bins
    summary["ece_10_equal_width_bins"] = float(sum(b["count"] / len(labels) * abs(b["confidence"] - b["accuracy"]) for b in bins))
    summary["multiclass_brier_score_sum_over_classes"] = float(np.mean(np.sum((probabilities - truth) ** 2, axis=1)))
    fig, axes = plt.subplots(1, 2, figsize=(12, 5))
    axes[0].plot([0, 1], [0, 1], "k--", label="Ideal")
    axes[0].plot([b["confidence"] for b in bins], [b["accuracy"] for b in bins], "o-", label="Top-label bins")
    axes[0].set(xlabel="Mean confidence", ylabel="Observed accuracy", xlim=(0, 1), ylim=(0, 1.02),
                title=f"Reliability ({split}), ECE={summary['ece_10_equal_width_bins']:.3f}")
    axes[0].legend()
    for mask, name in ((correct, "Correct"), (~correct, "Incorrect")):
        axes[1].hist(confidence[mask], bins=np.linspace(0, 1, 21), alpha=.6, label=name)
    axes[1].axvline(threshold, color="black", linestyle="--", label=f"Validation threshold={threshold:.2f}")
    axes[1].set(xlabel="Maximum softmax probability", ylabel="Images", title="Confidence distribution")
    axes[1].legend()
    save(fig, "reliability_confidence.png")

    points = []
    for value in sorted(set(np.linspace(0, 1, 101).tolist() + [float(threshold)])):
        mask = confidence >= value
        points.append({"threshold": value, "accepted": int(mask.sum()), "coverage": float(mask.mean()),
                       "accepted_accuracy": float(correct[mask].mean()) if mask.any() else None})
    summary["threshold_diagnostics_not_for_test_selection"] = points
    fig, axes = plt.subplots(1, 2, figsize=(12, 5))
    axes[0].plot([p["threshold"] for p in points], [p["coverage"] for p in points], label="Coverage")
    axes[0].plot([p["threshold"] for p in points], [np.nan if p["accepted_accuracy"] is None else p["accepted_accuracy"] for p in points], label="Accepted accuracy")
    axes[0].axvline(threshold, linestyle="--", color="black", label="Chosen on validation")
    axes[0].set(xlabel="Confidence threshold", ylabel="Fraction", ylim=(0, 1.02), title=f"Threshold diagnostics ({split})")
    axes[0].legend()
    valid = sorted([p for p in points if p["accepted_accuracy"] is not None], key=lambda p: p["coverage"])
    axes[1].plot([p["coverage"] for p in valid], [1 - p["accepted_accuracy"] for p in valid], "o-", markersize=2)
    axes[1].set(xlabel="Coverage", ylabel="Selective risk (1 - accepted accuracy)", title=f"Risk-coverage ({split})", xlim=(0, 1), ylim=(0, 1))
    save(fig, "threshold_risk_coverage.png")
    summary["limitations"] = "Known-class diagnostic curves only. Confidence/ECE do not establish OOD rejection; no test-based threshold selection. Small bins may be noisy."
    (output / "curve_metrics.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    np.savez_compressed(output / "probabilities.npz", labels=labels, probabilities=probabilities)
    return summary


def training_curves(histories, output):
    output = Path(output); output.mkdir(parents=True, exist_ok=True)
    fig, axes = plt.subplots(1, 3, figsize=(16, 4))
    offset = 0
    for phase, history in histories.items():
        count = len(history["loss"]); epochs = np.arange(offset + 1, offset + count + 1)
        for ax, metric in zip(axes[:2], ("loss", "accuracy")):
            ax.plot(epochs, history[metric], label=f"{phase}: train (augmentation)")
            ax.plot(epochs, history["val_" + metric], label=f"{phase}: validation")
            ax.set_title(metric.title()); ax.set_xlabel("Epoch"); ax.legend(fontsize=7)
        axes[2].plot(epochs, history.get("learning_rate", [np.nan] * count), label=phase)
        if offset:
            for ax in axes:
                ax.axvline(offset + .5, linestyle="--", color="grey")
        offset += count
    axes[2].set(title="Learning rate", xlabel="Epoch", yscale="log"); axes[2].legend()
    for ax in axes:
        ax.grid(alpha=.2)
    fig.tight_layout(); fig.savefig(output / "training_curves.png", dpi=170); plt.close(fig)


def split_distribution(rows, output):
    from collections import Counter
    fig, ax = plt.subplots(figsize=(8, 4))
    x = np.arange(3)
    for i, split in enumerate(("train", "validation", "test")):
        counts = Counter(int(row["label"]) for row in rows if row["split"] == split)
        bars = ax.bar(x + (i - 1) * .25, [counts[j] for j in range(3)], .25, label=split)
        ax.bar_label(bars, fontsize=8)
    ax.set_xticks(x, CLASS_NAMES); ax.set(ylabel="Images", title="Dataset distribution by split"); ax.legend()
    fig.tight_layout(); fig.savefig(Path(output) / "split_distribution.png", dpi=170); plt.close(fig)
