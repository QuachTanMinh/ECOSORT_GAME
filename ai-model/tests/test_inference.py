import csv
import io
import json
from pathlib import Path

import numpy as np
import pytest
from PIL import Image

from data import CLASS_NAMES, decode_image
from inference import Predictor, predict_image

BASE = Path(__file__).parents[1]


@pytest.fixture(scope="module")
def predictor():
    return Predictor(BASE / "artifacts")


def test_missing_model_reports_actionable_error(tmp_path):
    with pytest.raises(RuntimeError, match="artifacts missing"):
        Predictor(tmp_path)


def test_invalid_bytes_fail_before_loading_global_model():
    with pytest.raises(ValueError):
        predict_image(b"corrupted")


def test_real_model_contract_and_preprocessing_agree(predictor):
    with (BASE / "reports/splits.csv").open(encoding="utf-8", newline="") as stream:
        rows = list(csv.DictReader(stream))
    root = BASE.parent / "Dataset/dataset-plastic-metal-paper"
    for name in CLASS_NAMES:
        row = next(row for row in rows if row["split"] == "test" and row["class_name"] == name)
        content = (root / row["path"]).read_bytes()
        result = predictor.predict(content)
        assert set(result) == {"class_name", "confidence", "accepted"}
        assert result["class_name"] in CLASS_NAMES
        assert 0 <= result["confidence"] <= 1
        assert type(result["accepted"]) is bool
        expected = np.asarray(predictor.model(decode_image(content).astype(np.float32)[None], training=False))[0]
        assert result["class_name"] == CLASS_NAMES[int(expected.argmax())]
        assert result["confidence"] == pytest.approx(float(expected.max()), abs=1e-6)
        assert result["accepted"] == (result["confidence"] >= predictor.threshold)
        json.dumps(result)  # Builtin JSON types required by FastAPI/Unity contract.


def test_global_predict_image_uses_real_model():
    with (BASE / "reports/splits.csv").open(encoding="utf-8", newline="") as stream:
        row = next(row for row in csv.DictReader(stream) if row["split"] == "test")
    content = (BASE.parent / "Dataset/dataset-plastic-metal-paper" / row["path"]).read_bytes()
    assert predict_image(content)["class_name"] in CLASS_NAMES


def test_confidence_rejection_is_not_hardcoded(predictor):
    image = Image.new("RGB", (50, 50), color=(120, 120, 120))
    stream = io.BytesIO(); image.save(stream, format="JPEG")
    original = predictor.threshold
    try:
        predictor.threshold = 1.0
        result = predictor.predict(stream.getvalue())
        assert result["accepted"] == (result["confidence"] >= 1.0)
        predictor.threshold = 0
        assert predictor.predict(stream.getvalue())["accepted"] is True
    finally:
        predictor.threshold = original


def test_tampered_model_rejected_before_tensorflow_load(tmp_path):
    (tmp_path / "model.keras").write_bytes(b"tampered")
    (tmp_path / "metadata.json").write_bytes((BASE / "artifacts/metadata.json").read_bytes())
    with pytest.raises(RuntimeError, match="checksum"):
        Predictor(tmp_path)
