"""Backend handoff: predict_image(image_bytes: bytes) -> dict; no FastAPI dependency."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import threading
from pathlib import Path

import numpy as np

if __package__:
    from .data import CLASS_NAMES, decode_image
else:
    from data import CLASS_NAMES, decode_image


class Predictor:
    def __init__(self, artifacts: Path | str | None = None, threshold: float | None = None):
        root = Path(artifacts or os.environ.get("ECOSORT_AI_ARTIFACTS", Path(__file__).parent / "artifacts-organic"))
        model_path, metadata_path = root / "model.keras", root / "metadata.json"
        if not model_path.is_file() or not metadata_path.is_file():
            raise RuntimeError("AI artifacts missing: need model.keras and metadata.json")
        self.metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
        self.classes = tuple(self.metadata["class_names"])
        if self.classes not in (CLASS_NAMES, CLASS_NAMES[:3]) or self.metadata["input_shape"] != [224, 224, 3]:
            raise RuntimeError("Unsupported model class mapping or input shape")
        if hashlib.sha256(model_path.read_bytes()).hexdigest() != self.metadata["model_sha256"]:
            raise RuntimeError("Model checksum does not match metadata")
        self.threshold = float(threshold if threshold is not None else os.environ.get("ECOSORT_AI_THRESHOLD", self.metadata["threshold"]))
        if not np.isfinite(self.threshold) or not 0 <= self.threshold <= 1:
            raise ValueError("Threshold must be finite and between 0 and 1")
        os.environ.setdefault("TF_CPP_MIN_LOG_LEVEL", "2")
        import tensorflow as tf
        self.model = tf.keras.models.load_model(model_path, compile=False, safe_mode=True)
        if tuple(self.model.input_shape[1:]) != (224, 224, 3) or self.model.output_shape[-1] != len(self.classes):
            raise RuntimeError("Model dimensions do not match metadata")
        self.lock = threading.Lock()

    def predict(self, image_bytes: bytes) -> dict:
        image = decode_image(image_bytes).astype(np.float32)[None, ...]
        # Rescaling is inside the exported model: do not call preprocess_input again.
        with self.lock:
            probabilities = np.asarray(self.model(image, training=False))[0]
        if probabilities.shape != (len(self.classes),) or not np.all(np.isfinite(probabilities)) or np.any(probabilities < 0) or not np.isclose(probabilities.sum(), 1, atol=1e-4):
            raise RuntimeError("Model returned invalid probabilities")
        index = int(probabilities.argmax())
        confidence = float(probabilities[index])
        return {"class_name": self.classes[index], "confidence": confidence, "accepted": bool(confidence >= self.threshold)}


_predictor = None
_load_lock = threading.Lock()


def predict_image(image_bytes: bytes) -> dict:
    # Validate before loading a potentially large model, also on a cold backend request.
    decode_image(image_bytes)
    global _predictor
    if _predictor is None:
        with _load_lock:
            if _predictor is None:
                _predictor = Predictor()
    return _predictor.predict(image_bytes)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("--artifacts", type=Path)
    parser.add_argument("--threshold", type=float)
    args = parser.parse_args()
    try:
        print(json.dumps(Predictor(args.artifacts, args.threshold).predict(args.image.read_bytes()), ensure_ascii=False))
    except (ValueError, RuntimeError, OSError) as error:
        parser.exit(1, str(error) + "\n")
