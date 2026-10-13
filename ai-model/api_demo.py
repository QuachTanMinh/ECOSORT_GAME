"""Local AI bridge for Unity Scan only; member 3 owns the complete backend."""
from contextlib import asynccontextmanager
import io
import logging

from fastapi import FastAPI, File, HTTPException, UploadFile
from PIL import Image
from starlette.concurrency import run_in_threadpool

from data import MAX_BYTES
from inference import Predictor

logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app):
    predictor = await run_in_threadpool(Predictor)
    # Warmup exercises decode and actual exported model, not only file presence.
    buffer = io.BytesIO(); Image.new("RGB", (224, 224)).save(buffer, format="JPEG")
    await run_in_threadpool(predictor.predict, buffer.getvalue())
    app.state.predictor = predictor
    yield
    app.state.predictor = None


app = FastAPI(title="EcoSort AI local integration demo", lifespan=lifespan)


@app.get("/health")
async def health():
    return {"status": "ready", "threshold": app.state.predictor.threshold,
            "model_sha256": app.state.predictor.metadata["model_sha256"]}


@app.post("/predict")
async def predict(file: UploadFile = File(...)):
    try:
        content = await file.read(MAX_BYTES + 1)
        if len(content) > MAX_BYTES:
            raise HTTPException(413, "Image exceeds 4 MiB")
        try:
            return await run_in_threadpool(app.state.predictor.predict, content)
        except ValueError as exc:
            raise HTTPException(400, str(exc)) from exc
        except Exception as exc:
            logger.exception("AI inference failed")
            raise HTTPException(503, "AI inference unavailable") from exc
    finally:
        await file.close()
