import copy
from fastapi.testclient import TestClient
from app.main import app

def test_learning_history_idempotency_and_validation(monkeypatch, tmp_path):
    monkeypatch.setenv("ECOSORT_STORAGE", "local")
    monkeypatch.setenv("ECOSORT_DATA_FILE", str(tmp_path / "ecosort.json"))
    payload = {"session_id": "a" * 32, "player_id": "learner", "mode": "Test", "context": "School",
               "created_at": "2026-10-15T00:00:00Z", "level": 1, "total": 10, "score": 5,
               "expired": True, "answers": [
                   {"item_id": "water-bottle", "expected": "Plastic", "choice": "Plastic", "correct": True, "seconds": 3.2},
                   {"item_id": "drink-can", "expected": "Metal", "choice": "Paper", "correct": False, "seconds": 4.1}]}
    with TestClient(app) as client:
        assert client.post("/learning/sessions", json=payload).json()["created"] is True
        assert client.post("/learning/sessions", json=payload).json()["created"] is False
        conflict = copy.deepcopy(payload); conflict["context"] = "Park"
        assert client.post("/learning/sessions", json=conflict).status_code == 409
        invalid = copy.deepcopy(payload); invalid["answers"][0]["expected"] = "Paper"
        assert client.post("/learning/sessions", json=invalid).status_code == 422
        invalid = copy.deepcopy(payload); invalid["score"] = 100
        assert client.post("/learning/sessions", json=invalid).status_code == 422
        stats = client.get("/learning/learner").json()
        assert stats["total_sessions"] == 1 and stats["test_unanswered"] == 8
        assert stats["test_groups"]["Plastic"] == {"answered": 1, "correct": 1}
        assert stats["test_groups"]["Metal"] == {"answered": 1, "correct": 0}
        assert client.get("/learning/other").json()["total_sessions"] == 0
    # Persist across backend process lifespans.
    with TestClient(app) as client:
        assert client.get("/learning/learner").json()["total_sessions"] == 1
