from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class Settings:
    storage: str = "local"
    data_file: Path = Path("data/ecosort.json")
    aio_username: str = ""
    aio_key: str = ""
    aio_base_url: str = "https://io.adafruit.com/api/v2"
    aio_timeout_seconds: float = 8.0
    aio_feed_prefix: str = ""
    ai_threshold: float | None = None
    max_upload_bytes: int = 4 * 1024 * 1024

    @classmethod
    def from_env(cls) -> "Settings":
        threshold = os.getenv("ECOSORT_AI_THRESHOLD", "").strip()
        return cls(
            storage=os.getenv("ECOSORT_STORAGE", "local").strip().lower(),
            data_file=Path(os.getenv("ECOSORT_DATA_FILE", "data/ecosort.json")),
            aio_username=os.getenv("AIO_USERNAME", "").strip(),
            aio_key=os.getenv("AIO_KEY", "").strip(),
            aio_base_url=os.getenv("AIO_BASE_URL", "https://io.adafruit.com/api/v2").rstrip("/"),
            aio_timeout_seconds=float(os.getenv("AIO_TIMEOUT_SECONDS", "8")),
            aio_feed_prefix=os.getenv("AIO_FEED_PREFIX", "").strip(),
            ai_threshold=float(threshold) if threshold else None,
            max_upload_bytes=int(os.getenv("ECOSORT_MAX_UPLOAD_BYTES", str(4 * 1024 * 1024))),
        )

    def validate(self) -> None:
        if self.storage not in {"local", "adafruit_io"}:
            raise ValueError("ECOSORT_STORAGE must be 'local' or 'adafruit_io'")
        if self.storage == "adafruit_io" and (not self.aio_username or not self.aio_key):
            raise ValueError("AIO_USERNAME and AIO_KEY are required when ECOSORT_STORAGE=adafruit_io")
        if self.max_upload_bytes < 1024:
            raise ValueError("ECOSORT_MAX_UPLOAD_BYTES must be at least 1024")
        if self.ai_threshold is not None and not 0 <= self.ai_threshold <= 1:
            raise ValueError("ECOSORT_AI_THRESHOLD must be between 0 and 1")

