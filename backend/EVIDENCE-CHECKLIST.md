# Phase acceptance and evidence checklist

## Implemented and locally verified

- [x] Phase 0: inspected the Unity REST client/DTOs, Python/TensorFlow environment, Kaggle handoff, and Docker availability.
- [x] Phase 1: FastAPI health, schemas, real-image route, request validation, and tests.
- [x] Phase 2: checked model hash, preprocessing, class order, and threshold against Kaggle metadata; loads once at startup. Replayed all 224 saved held-out predictions: zero class/acceptance mismatches; maximum confidence difference 0.00000352.
- [x] Phase 3 implementation: AIO REST adapter, auth header, timeout/error mapping, feed mapping, safe sample environment, and mock transport tests.
- [x] Phase 4 files: Python 3.12 Dockerfile, Compose service, model in image context, volume, environment configuration, and health check.
- [x] Phase 5 contract: request/response fields match Unity `RestEcoSortService` and `PlayerProgress`, including integer `ar_completed`.
- [x] Phase 6: API, local run, cloud configuration, Docker and Unity integration instructions.
- [x] Automated tests: 6 passed in the existing Python 3.12 environment, including actual model inference and mocked AIO requests.
- [x] Live local HTTP smoke check: `/health`, real `/predict`, GET/PUT/read-back progress, `/scores`, and `/ar-results` returned expected 200/201 statuses.

## Must be run on the user's PC/account

- [ ] Install/start Docker Desktop, then run `docker compose build`, `docker compose up -d`, `docker compose ps`, and `/health`.
- [ ] Run Unity Editor in Rest mode against `http://127.0.0.1:8000`; exercise scan, score/progress, and AR result.
- [ ] Create the listed Adafruit feeds; add the key privately to `.env`; exercise read/write and capture the actual feed/dashboard values.
- [ ] Capture real screenshots/logs and a short demo video for the project report.

At inspection, this machine had no Docker CLI/Desktop command. No Adafruit credentials are configured, and a key should not be shared in chat. Therefore a Docker build/run, Unity Editor round, and live cloud write are **not claimed as completed**. The service defaults to persistent local JSON mode.

