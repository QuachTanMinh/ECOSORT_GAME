from __future__ import annotations

from datetime import datetime
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field
from .recognition import RecognitionLabel


class StrictModel(BaseModel):
    model_config = ConfigDict(extra="forbid")


class Prediction(StrictModel):
    class_name: RecognitionLabel
    confidence: float = Field(ge=0, le=1, allow_inf_nan=False)
    accepted: bool


class Progress(StrictModel):
    player_id: str = Field(min_length=1, max_length=64, pattern=r"^[A-Za-z0-9_-]+$")
    unlocked_level: int = Field(ge=1, le=3)
    best_score: int = Field(ge=0)
    ar_completed: int = Field(ge=0)
    campaign_completed: bool
    updated_at: datetime


class ScoreSubmission(StrictModel):
    player_id: str = Field(min_length=1, max_length=64, pattern=r"^[A-Za-z0-9_-]+$")
    level: int = Field(ge=1, le=3)
    score: int = Field(ge=0, le=1_000_000)
    correct_count: int = Field(ge=0, le=1_000)


class ArResultSubmission(StrictModel):
    player_id: str = Field(min_length=1, max_length=64, pattern=r"^[A-Za-z0-9_-]+$")
    predicted_class: Literal["Plastic", "Metal", "Paper", "Organic"]
    confidence: float = Field(ge=0, le=1, allow_inf_nan=False)
    player_choice: Literal["Plastic", "Metal", "Paper", "Organic"]
    is_correct: bool

