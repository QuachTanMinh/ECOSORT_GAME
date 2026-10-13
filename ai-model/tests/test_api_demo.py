from pathlib import Path

import pytest
pytest.importorskip("fastapi")
pytest.importorskip("httpx")
from fastapi.testclient import TestClient
from api_demo import app
from data import MAX_BYTES


@pytest.fixture(scope="module")
def client():
    with TestClient(app) as instance:
        yield instance


def test_ready_health_exposes_loaded_model(client):
    response = client.get("/health")
    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "ready"
    assert body["model_sha256"] == app.state.predictor.metadata["model_sha256"]
    assert body["threshold"] == app.state.predictor.threshold


def test_http_multipart_returns_real_inference(client):
    root = Path(__file__).parents[1].parent / "Dataset/dataset-plastic-metal-paper"
    image = next((root / "plastic").glob("*.jpg")).read_bytes()
    response = client.post("/predict", files={"file": ("capture.jpg", image, "image/jpeg")})
    assert response.status_code == 200
    result = response.json()
    direct = app.state.predictor.predict(image)
    assert result.keys() == {"class_name", "confidence", "accepted"}
    assert result["class_name"] == direct["class_name"]
    assert result["confidence"] == pytest.approx(direct["confidence"])
    assert result["accepted"] == direct["accepted"]


def test_http_invalid_image(client):
    assert client.post("/predict", files={"file": ("bad.jpg", b"invalid", "image/jpeg")}).status_code == 400


def test_http_oversized_image(client):
    assert client.post("/predict", files={"file": ("large.jpg", b"x" * (MAX_BYTES + 1), "image/jpeg")}).status_code == 413


def test_http_requires_file_field(client):
    assert client.post("/predict", files={"image": ("bad.jpg", b"invalid", "image/jpeg")}).status_code == 422
