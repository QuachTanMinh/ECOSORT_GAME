"""Shared RGB/EXIF/resize rules for training, evaluation and inference."""
from __future__ import annotations

import hashlib
import io
import warnings
from pathlib import Path

import numpy as np
from PIL import Image, ImageOps, UnidentifiedImageError

CLASS_NAMES = ("Plastic", "Metal", "Paper", "Organic")  # Serialized output order; never alphabetical.
FOLDERS = ("plastic", "metal", "paper", "organic")
IMAGE_SIZE = (224, 224)
MAX_BYTES = 4 * 1024 * 1024
MAX_PIXELS = 20_000_000


def decode_image(image_bytes: bytes, size: tuple[int, int] = IMAGE_SIZE) -> np.ndarray:
    if not isinstance(image_bytes, bytes) or not image_bytes:
        raise ValueError("image_bytes must be nonempty bytes")
    if len(image_bytes) > MAX_BYTES:
        raise ValueError("Image exceeds 4 MB upload limit")
    try:
        with warnings.catch_warnings():
            warnings.simplefilter("error", Image.DecompressionBombWarning)
            with Image.open(io.BytesIO(image_bytes)) as source:
                if source.format not in ("JPEG", "PNG", "WEBP"):
                    raise ValueError("Only JPEG, PNG and WEBP are supported")
                if source.width * source.height > MAX_PIXELS:
                    raise ValueError("Image exceeds pixel limit")
                source.load()
                rgb = ImageOps.exif_transpose(source).convert("RGB")
                return np.asarray(rgb.resize(size, Image.Resampling.BILINEAR), dtype=np.uint8)
    except (UnidentifiedImageError, OSError, Image.DecompressionBombError, Image.DecompressionBombWarning) as exc:
        raise ValueError("Invalid or unsafe image") from exc


def image_fingerprints(path: Path) -> tuple[str, int, tuple[int, int]]:
    with Image.open(path) as source:
        source.load()
        rgb = ImageOps.exif_transpose(source).convert("RGB")
        digest = hashlib.sha256(str(rgb.size).encode() + rgb.tobytes()).hexdigest()
        gray = np.asarray(rgb.convert("L").resize((9, 8), Image.Resampling.LANCZOS))
        bits = (gray[:, 1:] > gray[:, :-1]).flatten()
        dhash = sum(int(value) << i for i, value in enumerate(bits))
        return digest, dhash, rgb.size


def load_rows(rows: list[dict], dataset_root: Path) -> tuple[np.ndarray, np.ndarray]:
    if not rows:
        raise ValueError("Empty split")
    images = np.stack([decode_image((dataset_root / row["path"]).read_bytes()) for row in rows])
    labels = np.asarray([int(row["label"]) for row in rows], dtype=np.int64)
    return images, labels
