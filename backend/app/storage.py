from __future__ import annotations

import json
import threading
from datetime import datetime, timezone
from pathlib import Path
from typing import Protocol

from .schemas import ArResultSubmission, Progress, ScoreSubmission


class Store(Protocol):
    mode: str
    def get_progress(self, player_id: str) -> Progress: ...
    def put_progress(self, progress: Progress) -> Progress: ...
    def add_score(self, score: ScoreSubmission) -> None: ...
    def add_ar_result(self, result: ArResultSubmission) -> None: ...
    def close(self) -> None: ...


def default_progress(player_id: str) -> Progress:
    return Progress(player_id=player_id, unlocked_level=1, best_score=0,
                    ar_completed=0, campaign_completed=False,
                    updated_at=datetime.now(timezone.utc))


class LocalJsonStore:
    """Small persistent store for a single local demo; replace with AIO for shared cloud state."""
    mode = "local"

    def __init__(self, path: Path):
        self.path = path
        self.path.parent.mkdir(parents=True, exist_ok=True)
        self.lock = threading.RLock()
        if not self.path.exists():
            self._write({"progress": {}, "scores": [], "ar_results": []})

    def _read(self) -> dict:
        try:
            return json.loads(self.path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as exc:
            raise RuntimeError("Local data file is unreadable") from exc

    def _write(self, value: dict) -> None:
        self.path.parent.mkdir(parents=True, exist_ok=True)
        temporary = self.path.with_suffix(self.path.suffix + ".tmp")
        temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")
        temporary.replace(self.path)

    def get_progress(self, player_id: str) -> Progress:
        with self.lock:
            raw = self._read()["progress"].get(player_id)
            return Progress.model_validate(raw) if raw else default_progress(player_id)

    def put_progress(self, progress: Progress) -> Progress:
        with self.lock:
            db = self._read()
            previous = db["progress"].get(progress.player_id)
            if previous:
                old = Progress.model_validate(previous)
                progress = progress.model_copy(update={
                    "unlocked_level": max(old.unlocked_level, progress.unlocked_level),
                    "best_score": max(old.best_score, progress.best_score),
                    "ar_completed": max(old.ar_completed, progress.ar_completed),
                    "campaign_completed": old.campaign_completed or progress.campaign_completed,
                    "updated_at": datetime.now(timezone.utc),
                })
            db["progress"][progress.player_id] = progress.model_dump(mode="json")
            self._write(db)
            return progress

    def add_score(self, score: ScoreSubmission) -> None:
        with self.lock:
            db = self._read()
            db["scores"].append({**score.model_dump(), "created_at": datetime.now(timezone.utc).isoformat()})
            self._write(db)

    def add_ar_result(self, result: ArResultSubmission) -> None:
        with self.lock:
            db = self._read()
            db["ar_results"].append({**result.model_dump(), "created_at": datetime.now(timezone.utc).isoformat()})
            self._write(db)

    def close(self) -> None:
        return None

