from __future__ import annotations

import json
from datetime import datetime, timezone
from urllib.parse import quote

import httpx

from .schemas import ArResultSubmission, Progress, ScoreSubmission
from .storage import Store, default_progress


FEEDS = {
    "game_score": "game-score",
    "current_level": "current-level",
    "player_progress": "player-progress",
    "detected_waste": "detected-waste",
    "ai_confidence": "ai-confidence",
    "ar_result": "ar-challenge-result",
}


class AdafruitIOError(RuntimeError):
    pass


class AdafruitIOStore(Store):
    mode = "adafruit_io"

    def __init__(self, username: str, key: str, base_url: str, timeout: float = 8.0,
                 transport: httpx.BaseTransport | None = None):
        self.username = username
        self.base_url = base_url.rstrip("/")
        self.client = httpx.Client(
            base_url=self.base_url,
            headers={"X-AIO-Key": key, "Content-Type": "application/json"},
            timeout=timeout,
            transport=transport,
        )

    def _path(self, feed: str, suffix: str = "") -> str:
        return f"/{quote(self.username, safe='')}/feeds/{quote(feed, safe='')}{suffix}"

    def _request(self, method: str, path: str, **kwargs) -> httpx.Response:
        try:
            response = self.client.request(method, path, **kwargs)
        except httpx.HTTPError as exc:
            raise AdafruitIOError("Adafruit IO could not be reached; check network and service status") from exc
        if response.status_code >= 400:
            # Do not return vendor response bodies; they can include account details.
            raise AdafruitIOError(f"Adafruit IO returned HTTP {response.status_code}; check feed names and account limits")
        return response

    def _publish(self, feed: str, value) -> None:
        encoded = value if isinstance(value, str) else json.dumps(value, separators=(",", ":"), ensure_ascii=False)
        self._request("POST", self._path(feed, "/data"), json={"value": encoded})

    def _latest(self, feed: str):
        response = self._request("GET", self._path(feed, "/data"), params={"limit": 1})
        entries = response.json()
        if not entries:
            return None
        return entries[0].get("value")

    @staticmethod
    def _decode_value(value):
        if value is None:
            return None
        if not isinstance(value, str):
            return value
        try:
            return json.loads(value)
        except json.JSONDecodeError:
            return value

    def get_progress(self, player_id: str) -> Progress:
        raw = self._decode_value(self._latest(FEEDS["player_progress"]))
        if not isinstance(raw, dict) or raw.get("player_id") != player_id:
            return default_progress(player_id)
        return Progress.model_validate(raw)

    def put_progress(self, progress: Progress) -> Progress:
        previous = self.get_progress(progress.player_id)
        merged = progress.model_copy(update={
            "unlocked_level": max(previous.unlocked_level, progress.unlocked_level),
            "best_score": max(previous.best_score, progress.best_score),
            "ar_completed": max(previous.ar_completed, progress.ar_completed),
            "campaign_completed": previous.campaign_completed or progress.campaign_completed,
            "updated_at": datetime.now(timezone.utc),
        })
        snapshot = merged.model_dump(mode="json")
        # These two feeds are separate AIO writes and cannot be made transactional.
        self._publish(FEEDS["player_progress"], snapshot)
        self._publish(FEEDS["current_level"], merged.unlocked_level)
        return merged

    def add_score(self, score: ScoreSubmission) -> None:
        self._publish(FEEDS["game_score"], {**score.model_dump(), "created_at": datetime.now(timezone.utc).isoformat()})

    def add_ar_result(self, result: ArResultSubmission) -> None:
        self._publish(FEEDS["ar_result"], {**result.model_dump(), "created_at": datetime.now(timezone.utc).isoformat()})

    def record_prediction(self, prediction: dict) -> None:
        event = {**prediction, "created_at": datetime.now(timezone.utc).isoformat()}
        self._publish(FEEDS["detected_waste"], event)
        self._publish(FEEDS["ai_confidence"], prediction["confidence"])

    def close(self) -> None:
        self.client.close()

