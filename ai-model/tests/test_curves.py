import ast
import json
from pathlib import Path

import numpy as np
import pytest
from evaluation_curves import diagnostic_curves, training_curves


def test_curve_metrics_perfect_classifier(tmp_path):
    labels = np.array([0, 1, 2, 0, 1, 2])
    probabilities = np.eye(3)[labels] * .9 + (1 - np.eye(3)[labels]) * .05
    summary = diagnostic_curves(labels, probabilities, tmp_path, .75)
    assert summary["macro_roc_auc_ovr"] == pytest.approx(1)
    assert summary["macro_average_precision"] == pytest.approx(1)
    assert summary["ece_10_equal_width_bins"] == pytest.approx(.1)
    assert summary["multiclass_brier_score_sum_over_classes"] == pytest.approx(.015)
    assert len(list(tmp_path.glob("*.png"))) == 4
    assert summary["threshold_diagnostics_not_for_test_selection"][-1]["accepted_accuracy"] is None
    saved = np.load(tmp_path / "probabilities.npz")
    np.testing.assert_array_equal(saved["labels"], labels)


def test_curve_metrics_reject_missing_class(tmp_path):
    with pytest.raises(ValueError):
        diagnostic_curves(np.array([0, 0]), np.array([[.8, .1, .1]] * 2), tmp_path, .75)


def test_notebook_is_self_contained_and_parseable():
    root = Path(__file__).parents[1]
    notebook = json.loads((root / "kaggle/EcoSort_Kaggle_GPU.ipynb").read_text(encoding="utf-8"))
    code = ["".join(c["source"]) for c in notebook["cells"] if c["cell_type"] == "code"]
    for source in code:
        ast.parse(source)
    embedded = next(source for source in code if "SOURCES = " in source)
    tree = ast.parse(embedded)
    assignment = next(node for node in tree.body if isinstance(node, ast.Assign)
                      and any(isinstance(t, ast.Name) and t.id == "SOURCES" for t in node.targets))
    sources = ast.literal_eval(assignment.value)
    for name, source in sources.items():
        assert source == (root / name).read_text(encoding="utf-8"), f"Regenerate notebook after editing {name}"
    # Check the serving requirements string contains real newlines when evaluated.
    export = next(source for source in code if "requirements-serving.txt" in source)
    tree = ast.parse(export)
    calls = [node for node in ast.walk(tree) if isinstance(node, ast.Call)
             and isinstance(node.func, ast.Attribute) and node.func.attr == "write_text"]
    expression = next(node.args[0] for node in calls if isinstance(node.args[0], ast.JoinedStr))
    class Version:
        __version__ = "2.20.0"
        keras = type("Keras", (), {"__version__": "3.15.1"})
    value = eval(compile(ast.Expression(expression), "requirements", "eval"), {"tf": Version})
    assert len(value.splitlines()) == 4


def test_training_curves_two_phases(tmp_path):
    h = {"loss": [.4, .2], "val_loss": [.5, .3], "accuracy": [.8, .9], "val_accuracy": [.7, .8], "learning_rate": [.001, .0005]}
    training_curves({"head": h, "fine_tune": h}, tmp_path)
    assert (tmp_path / "training_curves.png").stat().st_size > 1000
