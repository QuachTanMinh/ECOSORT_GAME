from __future__ import annotations

from io import BytesIO

from fastapi.testclient import TestClient
from PIL import Image

from app.main import app


def png_bytes() -> bytes:
    buffer = BytesIO()
    Image.new("RGB", (240, 180), (30, 180, 70)).save(buffer, format="PNG")
    return buffer.getvalue()


def test_unity_routes_and_real_model(monkeypatch, tmp_path):
    monkeypatch.setenv("ECOSORT_STORAGE", "local")
    monkeypatch.setenv("ECOSORT_DATA_FILE", str(tmp_path / "ecosort.json"))
    monkeypatch.delenv("ECOSORT_AI_THRESHOLD", raising=False)
    with TestClient(app) as client:
        health = client.get("/health")
        assert health.status_code == 200
        assert health.json()["class_names"] == ["Plastic", "Metal", "Paper", "Organic"]
        assert health.json()["confidence_threshold"] == 0.75

        prediction = client.post("/predict", files={"file": ("capture.jpg", png_bytes(), "image/jpeg")})
        assert prediction.status_code == 200, prediction.text
        assert set(prediction.json()) == {"class_name", "confidence", "accepted"}
        assert prediction.json()["class_name"] in {"Plastic", "Metal", "Paper", "Organic"}
        assert 0 <= prediction.json()["confidence"] <= 1

        initial = client.get("/progress/demo-player-1")
        assert initial.status_code == 200
        assert initial.json()["player_id"] == "demo-player-1"
        assert initial.json()["unlocked_level"] == 1
        assert set(initial.json()) == {
            "player_id", "unlocked_level", "best_score", "ar_completed",
            "campaign_completed", "updated_at",
        }

        body = {**initial.json(), "unlocked_level": 2, "best_score": 250, "ar_completed": 1}
        saved = client.put("/progress/demo-player-1", json=body)
        assert saved.status_code == 200, saved.text
        assert saved.json()["ar_completed"] == 1
        assert client.get("/progress/demo-player-1").json()["best_score"] == 250
        # Unity sends the same player ID in both path and body; reject mismatches.
        mismatch = client.put("/progress/demo-player-1", json={**body, "player_id": "other"})
        assert mismatch.status_code == 400

        score = client.post("/scores", json={
            "player_id": "demo-player-1", "level": 1, "score": 250, "correct_count": 8,
        })
        assert score.status_code == 201
        ar = client.post("/ar-results", json={
            "player_id": "demo-player-1", "predicted_class": "Plastic", "confidence": 0.9,
            "player_choice": "Plastic", "is_correct": True,
        })
        assert ar.status_code == 201


def test_prediction_rejects_invalid_and_oversized_image(monkeypatch, tmp_path):
    monkeypatch.setenv("ECOSORT_STORAGE", "local")
    monkeypatch.setenv("ECOSORT_DATA_FILE", str(tmp_path / "ecosort.json"))
    monkeypatch.setenv("ECOSORT_MAX_UPLOAD_BYTES", "1024")
    with TestClient(app) as client:
        invalid = client.post("/predict", files={"file": ("not-an-image.jpg", b"not an image", "image/jpeg")})
        assert invalid.status_code == 400
        oversized = client.post("/predict", files={"file": ("large.png", b"x" * 1025, "image/png")})
        assert oversized.status_code == 413

