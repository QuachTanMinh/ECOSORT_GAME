"""Detailed learning history stays in the backend volume, separately from AIO snapshots."""
from __future__ import annotations

import json
import threading
from pathlib import Path
from typing import Literal
from datetime import datetime
from pydantic import Field, model_validator
from .schemas import StrictModel

Material = Literal["Plastic", "Metal", "Paper", "Organic"]
ITEMS = {
    "Plastic": {"water-bottle", "detergent-bottle", "plastic-cup", "food-container", "plastic-cap", "shampoo-bottle", "plastic-spoon", "yogurt-pot", "plant-pot"},
    "Metal": {"drink-can", "food-can", "metal-lid", "aluminium-tray", "steel-cup", "metal-box", "metal-spoon", "bottle-crown", "foil-ball"},
    "Paper": {"newspaper", "cardboard", "paper-bag", "notebook", "envelope", "paper-tube", "worksheet", "paper-box", "flyer"},
    "Organic": {"banana-peel", "apple-core", "orange-peel", "vegetable-scraps", "bread-scrap", "eggshell", "coffee-grounds", "watermelon-rind", "fallen-leaf"},
}

class LearningAnswer(StrictModel):
    item_id: str
    expected: Material
    choice: Material
    correct: bool
    seconds: float = Field(ge=0, le=86400, allow_inf_nan=False)

    @model_validator(mode="after")
    def check_answer(self):
        if self.item_id not in ITEMS[self.expected] or self.correct != (self.expected == self.choice):
            raise ValueError("Item material and correctness must agree")
        return self

class LearningSession(StrictModel):
    session_id: str = Field(pattern=r"^[a-f0-9]{32}$")
    player_id: str = Field(min_length=1, max_length=64, pattern=r"^[A-Za-z0-9_-]+$")
    mode: Literal["Campaign", "Learn", "Test", "Review", "Explore"]
    context: Literal["Home", "School", "Park"]
    created_at: datetime
    level: int = Field(ge=1, le=3)
    total: int = Field(ge=1, le=30)
    score: int = Field(ge=0, le=300)
    expired: bool
    answers: list[LearningAnswer] = Field(max_length=30)

    @model_validator(mode="after")
    def check_round(self):
        if len(self.answers) > self.total or self.expired != (len(self.answers) < self.total):
            raise ValueError("Answer count and expiration do not agree")
        score = 0
        for a in self.answers:
            score = max(0, score + (10 if a.correct else -5))
        if score != self.score:
            raise ValueError("Score does not agree with answers")
        return self

class LearningStore:
    def __init__(self, path: Path):
        self.path = path
        self.lock = threading.RLock()

    def read(self):
        if not self.path.exists():
            return {}
        return json.loads(self.path.read_text(encoding="utf-8"))

    def add(self, session: LearningSession):
        value = session.model_dump(mode="json")
        with self.lock:
            data = self.read()
            key = session.player_id + ":" + session.session_id
            if key in data:
                if data[key] != value:
                    raise ValueError("session_id already belongs to a different payload")
                return False
            data[key] = value
            self.path.parent.mkdir(parents=True, exist_ok=True)
            temporary = self.path.with_suffix(".tmp")
            temporary.write_text(json.dumps(data, ensure_ascii=False), encoding="utf-8")
            temporary.replace(self.path)
            return True

    def history(self, player_id: str):
        with self.lock:
            sessions = [s for s in self.read().values() if s["player_id"] == player_id]
        sessions.sort(key=lambda s: s["created_at"])
        tests = [s for s in sessions if s["mode"] == "Test"]
        groups = {}
        for material in ITEMS:
            answers = [a for s in tests for a in s["answers"] if a["expected"] == material]
            groups[material] = {"answered": len(answers), "correct": sum(a["correct"] for a in answers)}
        return {"sessions": sessions[-100:], "total_sessions": len(sessions), "test_sessions": len(tests),
                "test_total_questions": sum(s["total"] for s in tests),
                "test_unanswered": sum(s["total"] - len(s["answers"]) for s in tests),
                "test_groups": groups, "storage": "backend_local"}
