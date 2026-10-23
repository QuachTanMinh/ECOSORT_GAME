# Cập nhật mô hình bốn lớp — 15/10/2026

Checkpoint phục vụ hiện tại nhận Plastic, Metal, Paper, Organic; `/health` trả về bốn lớp. Schema nhận Organic trong `/predict`, kết quả AR và lịch sử học tập. Lịch sử có thêm mode `Explore`. Rebuild Docker với `docker compose up --build -d`, hoặc khởi động lại Uvicorn; không chỉ restart image cũ. Xem [tài liệu Organic / 3D](../ECOSORT_GAME/Docs/ORGANIC-EXPLORATION.md). Các thông số ba lớp ở phần lịch sử bên dưới không mô tả checkpoint mới.

# EcoSort Quest AR backend

## Learning extension 2026-10-15

`POST /learning/sessions` accepts session_id, player_id, mode, context, created_at, level, total, score, expired, answers. Each answer: item_id, expected, choice, correct, seconds. Modes: Campaign/Learn/Test/Review; contexts: Home/School/Park; materials: Plastic/Metal/Paper.

Retries with same ID/content do not duplicate; changed content under same player/ID returns 409. Invalid material, correctness, score or answer count returns 422. `GET /learning/{player_id}` returns recent 100 sessions and test-only aggregate counts including unanswered. Storage is learning.json beside ECOSORT_DATA_FILE, also when progress uses Adafruit. Docker volume retains this file. No new feed required. Routes remain anonymous, for a single backend instance in a trusted local demo. Unity retains an outbox and retries from Learning results. Restart/rebuild backend after updating. See [learning guide](../ECOSORT_GAME/Docs/LEARNING-UPGRADE.md).

FastAPI service for the Unity Windows client. It loads the Kaggle-trained model once at startup, accepts Unity's multipart upload, and exposes progress, score, and AR routes. Local JSON persistence is the default. Adafruit IO is an optional cloud adapter enabled with environment variables.

## Model contract

The bundled checkpoint is `app/ai_model/artifacts/model.keras`. Its SHA-256 is `5f97707c552ac491e66f720c1b9bca1eab2325622ea5328e7f9cad05eabbde38`, matching `metadata.json`. Input is RGB, resized to 224×224 with Pillow bilinear after EXIF orientation; the model itself applies `Rescaling(1/127.5, offset=-1)`. Output order is `Plastic`, `Metal`, `Paper`; confidence threshold is 0.75. The service returns Unity's prediction object: `{ "class_name": "Plastic", "confidence": 0.92, "accepted": true }`.

The 224 held-out predictions from Kaggle were replayed through this checkpoint and preprocessing on the local dataset. All class labels and accepted/rejected decisions matched; maximum confidence difference was `0.00000352`. This verifies the serving model against saved Kaggle results, not new phone-camera images.

## Run on Windows

Install Python 3.12, then in PowerShell:

```powershell
cd E:\Document_Collage\CNM\CK\backend
py -3.12 -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
pip install -r requirements.txt
pip install -r requirements-dev.txt
python -m uvicorn app.main:app --host 127.0.0.1 --port 8000
```

The first start loads and verifies the 10 MB model. Open [http://127.0.0.1:8000/docs](http://127.0.0.1:8000/docs) for Swagger and [http://127.0.0.1:8000/health](http://127.0.0.1:8000/health) for readiness. Local data persists in `backend/data/ecosort.json`; this mode does not write to Adafruit IO.

Run tests from another PowerShell window:

```powershell
cd E:\Document_Collage\CNM\CK\backend
.\.venv\Scripts\Activate.ps1
python -m pytest -q
```

Tests load the real model, exercise every Unity route and image validation, verify monotonic progress merging, and test AIO authentication/error handling through a local mocked HTTP transport. They do not contact Adafruit.

## API contract

| Method and path | Unity request | Response |
|---|---|---|
| `GET /health` | None | Model readiness, hash, classes, threshold, and storage mode |
| `POST /predict` | Multipart field `file`; JPEG/PNG/WEBP; max 4 MiB | `class_name`, `confidence`, `accepted`; invalid image 400; oversize image 413 |
| `GET /progress/{player_id}` | Unity-generated ID | Raw progress; a new player starts at level 1 |
| `PUT /progress/{player_id}` | Unity `PlayerProgress` JSON | Raw merged progress; IDs in path and body must match |
| `POST /scores` | `{player_id, level, score, correct_count}` | 201 and `{accepted:true}` once persisted |
| `POST /ar-results` | `{player_id, predicted_class, confidence, player_choice, is_correct}` | 201 and `{accepted:true}` once persisted |

Progress fields match Unity `PlayerProgress`: `player_id`, `unlocked_level`, `best_score`, `ar_completed` (integer count), `campaign_completed` (boolean), and `updated_at` (ISO-8601 string). Level, best score, and AR count cannot decrease; campaign completion stays true once earned. Player IDs accept letters, digits, `_`, and `-`.

## Adafruit IO cloud mode

Grouped feeds: if the feed list shows keys such as `ecosort.game-score`, set
`AIO_FEED_PREFIX=ecosort` in the local `.env`. Leave it empty for default-group
keys such as `game-score`. After changing configuration or backend/model files,
run `docker compose up -d --build`; restarting an old image does not update its code.
The 23/10/2026 cloud incident was caused by grouped feeds being addressed without
their prefix, plus an outdated running image. See `ECOSORT_GAME/Docs/CLOUD-REPAIR-20261023.md`.

The adapter authenticates with `X-AIO-Key` and uses a finite timeout. Keep the key in a local `.env` file; never put it in Unity, source control, screenshots, or chat. Start from the sample:

```powershell
Copy-Item .env.example .env
notepad .env
```

Set `ECOSORT_STORAGE=adafruit_io`, your Adafruit username, and your account key in `.env`. Create these feeds in the Adafruit IO UI before starting the backend:

- `game-score`: JSON event per round (`player_id`, `level`, `score`, `correct_count`, timestamp).
- `current-level`: integer unlocked level for a dashboard chart.
- `player-progress`: JSON snapshot of all Unity progress fields. This extra feed is required because the suggested event feeds do not retain every field needed by `GET /progress/{player_id}`. The initial release supports one demo player.
- `detected-waste`: JSON inference event (`class_name`, `confidence`, `accepted`, timestamp).
- `ai-confidence`: numeric confidence for a chart.
- `ar-challenge-result`: JSON AR event with player ID, predicted class, choice, correctness, and confidence.

The JSON feeds preserve the full Unity score/progress/AR event, but Adafruit IO does not interpret these serialized JSON values for dashboard chart blocks. Use `current-level` and `ai-confidence` for numeric charts; inspect the latest raw JSON event through the feed/API. Keep event values under 1 KB per point when feed history is enabled. The service does not create feeds or claim cloud success when the account/feed is unavailable. AIO 401/403/404/429/5xx and network/timeouts return HTTP 503 with a safe diagnostic; keys and vendor response bodies are not logged or returned. Progress and prediction each publish two feed points and are not atomic. Scans and game actions are user-triggered, never per camera frame. Adafruit's free tier documents a 30 data-point-per-minute limit, so avoid rapid retries. See the [HTTP API reference](https://io.adafruit.com/api/docs/) and [JSON feed cookbook note](https://io.adafruit.com/api/docs/cookbook.html).

Once configured, check `/health` reports `storage_mode: "adafruit_io"`, then send one scan, score, progress update, and AR result. Verify the records in the actual feeds/dashboard. Local mode proves the API flow only; it is not cloud evidence.

## Docker Desktop on Windows 11

Install and start Docker Desktop with Linux containers. From `backend` run:

```powershell
docker --version
docker compose version
docker compose build
docker compose up -d
docker compose ps
docker compose logs --tail 100 ecosort-api
Invoke-RestMethod http://127.0.0.1:8000/health
```

The Python 3.12 container runs as a non-root user, includes the verified model, exposes port 8000, and stores local JSON in a named volume. A successful Dockerfile edit is not evidence that a container ran; the commands above must be executed where Docker Desktop is available.

For cloud mode, keep `.env` beside `compose.yaml`, fill in the AIO settings, then run `docker compose up -d --build`. The key is passed as an environment variable and is not copied into the image.

## Unity integration on Windows

1. Start the backend and confirm `/health` returns `status: "ok"`.
2. In Unity select `Assets/_EcoSortQuest/ScriptableObjects/ApiSettings.asset`. Set Mode to `Rest`, Base URL to `http://127.0.0.1:8000`, and minimum confidence to `0.75`.
3. Run the UI health check and scan an image. Unity uploads a JPEG as multipart field `file`; the API returns the three prediction fields above.
4. Play a round and check score/progress succeed. Exercise AR and verify `ar_completed` remains an integer count.
5. For a cloud demonstration, enable AIO mode in the backend environment, restart the backend, repeat one scan/round/AR result, and inspect the actual feeds.

The loopback URL works when Unity and the backend run on the same PC. For another device, use the PC's LAN address and allow inbound port 8000 in Windows Firewall. The current Windows game build simulates AR recognition; this backend does not make it a mobile AR build.

## Evidence and acceptance

See [EVIDENCE-CHECKLIST.md](EVIDENCE-CHECKLIST.md) for verified work and remaining local steps. Capture actual Swagger, Unity REST gameplay, Docker, and Adafruit feed screenshots/logs only after running those components.
