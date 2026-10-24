from __future__ import annotations

import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI, File, HTTPException, Request, UploadFile
from fastapi.responses import JSONResponse
from starlette.concurrency import run_in_threadpool

from .adafruit import AdafruitIOError, AdafruitIOStore
from .ai_model.inference import Predictor
from .config import Settings
from .schemas import ArResultSubmission, Prediction, Progress, ScoreSubmission
from .storage import LocalJsonStore, Store, default_progress
from .learning import LearningSession, LearningStore
from .recognition import recognition_catalog

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s %(message)s")
logger = logging.getLogger("ecosort.api")


@asynccontextmanager
async def lifespan(app: FastAPI):
    settings = Settings.from_env()
    settings.validate()
    try:
        predictor = Predictor(threshold=settings.ai_threshold)
    except Exception as exc:
        logger.exception("Could not load verified EcoSort model")
        raise RuntimeError("AI model could not be loaded; verify backend/app/ai_model/artifacts") from exc
    if settings.storage == "adafruit_io":
        store: Store = AdafruitIOStore(settings.aio_username, settings.aio_key,
                                       settings.aio_base_url, settings.aio_timeout_seconds,
                                       feed_prefix=settings.aio_feed_prefix)
    else:
        store = LocalJsonStore(settings.data_file)
    app.state.settings = settings
    app.state.predictor = predictor
    app.state.store = store
    app.state.learning = LearningStore(settings.data_file.parent / "learning.json")
    try:
        yield
    finally:
        store.close()


app = FastAPI(title="EcoSort Quest API", version="1.0.0", lifespan=lifespan)


@app.post("/learning/sessions", status_code=201)
def post_learning(session: LearningSession, request: Request):
    try:
        created = request.app.state.learning.add(session)
    except ValueError as exc:
        raise HTTPException(status_code=409, detail=str(exc)) from exc
    return {"accepted": True, "created": created}


@app.get("/learning/{player_id}")
def get_learning(player_id: str, request: Request):
    if not player_id or len(player_id) > 64 or not all(c.isalnum() or c in "-_" for c in player_id):
        raise HTTPException(status_code=422, detail="Invalid player_id")
    return request.app.state.learning.history(player_id)


@app.exception_handler(AdafruitIOError)
async def aio_error_handler(_: Request, exc: AdafruitIOError):
    logger.warning("Adafruit IO request failed: %s", exc)
    return JSONResponse(status_code=503, content={"detail": str(exc)})


@app.get("/health")
def health(request: Request):
    predictor: Predictor = request.app.state.predictor
    settings: Settings = request.app.state.settings
    return {"status": "ok", "ai_ready": True, "storage_mode": settings.storage,
            "class_names": list(predictor.classes), "confidence_threshold": predictor.threshold,
            "model_sha256": predictor.metadata["model_sha256"]}


@app.get('/recognition/classes')
def get_recognition_classes():
    return recognition_catalog()


@app.post("/predict", response_model=Prediction)
async def predict(request: Request, file: UploadFile = File(...)):
    settings: Settings = request.app.state.settings
    image_bytes = await file.read(settings.max_upload_bytes + 1)
    await file.close()
    if len(image_bytes) > settings.max_upload_bytes:
        raise HTTPException(status_code=413, detail="Image exceeds the 4 MB upload limit")
    try:
        result = await run_in_threadpool(request.app.state.predictor.predict, image_bytes)
        response = Prediction.model_validate(result)
        store = request.app.state.store
        if isinstance(store, AdafruitIOStore):
            await run_in_threadpool(store.record_prediction, response.model_dump())
        return response
    except (ValueError, OSError) as exc:
        raise HTTPException(status_code=400, detail="Invalid or unsupported image") from exc
    except AdafruitIOError:
        raise
    except Exception as exc:
        logger.exception("Inference failed")
        raise HTTPException(status_code=500, detail="Image inference failed") from exc


@app.get("/progress/{player_id}", response_model=Progress)
def get_progress(player_id: str, request: Request):
    if not player_id or len(player_id) > 64 or not all(c.isalnum() or c in "-_" for c in player_id):
        raise HTTPException(status_code=422, detail="Invalid player_id")
    return request.app.state.store.get_progress(player_id)


@app.put("/progress/{player_id}", response_model=Progress)
def put_progress(player_id: str, progress: Progress, request: Request):
    if player_id != progress.player_id:
        raise HTTPException(status_code=400, detail="player_id in path and body must match")
    return request.app.state.store.put_progress(progress)


@app.post("/scores", status_code=201)
def post_score(score: ScoreSubmission, request: Request):
    request.app.state.store.add_score(score)
    return {"accepted": True}


@app.post("/ar-results", status_code=201)
def post_ar_result(result: ArResultSubmission, request: Request):
    request.app.state.store.add_ar_result(result)
    return {"accepted": True}

