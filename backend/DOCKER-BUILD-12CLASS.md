# DOCKER BUILD – Model 12 Lớp (ECOSORT_GAME)

> Tài liệu này hướng dẫn **build Docker image** cho backend ECOSORT với model EfficientNetV2B0 12 lớp,
> cũng như cách chạy không có Docker, cách rollback, và troubleshooting.

---

## Thông tin model

| Thuộc tính | Giá trị |
|---|---|
| Backbone | EfficientNetV2B0 |
| Mode | `expanded12` |
| Classes | 12 |
| SHA-256 | `c94553614878308bc219487c2910de23ff2dd5a686737014cdc96a540e18814f` |
| Validation Accuracy | 97.94% |
| Threshold mặc định | 0.75 |
| TensorFlow | 2.20.0 |

12 class names theo thứ tự:
```
Plastic, Metal, Paper, Organic, Cardboard, GreenGlass,
BrownGlass, WhiteGlass, Clothes, Shoes, Batteries, Trash
```

---

## 1. Chuẩn bị

### 1.1. Yêu cầu hệ thống

- Docker Desktop (Windows/Mac) hoặc Docker Engine (Linux) ≥ 24
- RAM trống ≥ 2 GB khi build (TensorFlow ~ 620 MB)
- Disk trống ≥ 3 GB
- File model **phải tồn tại** trước khi build:

```
backend/app/ai_model/artifacts/model.keras     ← file model (~90-200 MB)
backend/app/ai_model/artifacts/metadata.json   ← đã có sẵn
```

> [!IMPORTANT]
> `model.keras` **không được commit** vào git vì kích thước lớn.
> Copy file này thủ công hoặc tải từ nơi lưu trữ nội bộ trước khi build.

### 1.2. Kiểm tra Docker

```powershell
docker version
docker info
```

Nếu lệnh không tìm thấy → xem Mục 5 (chạy không có Docker).

---

## 2. Build Docker Image

### 2.1. Build chuẩn (tải TF từ internet, ~ 620 MB)

Chạy từ thư mục `backend/`:

```powershell
cd e:\Document_Collage\CNM\CK\backend

docker build -t ecosort-api:12class .
```

Quá trình build sẽ mất **5–15 phút** tuỳ tốc độ mạng.

### 2.2. Build nhanh – tái dùng runtime cục bộ

Nếu đã có image cũ (`ecosort-api:before-20261030`) với Python + TensorFlow đã cài sẵn,
dùng `--build-arg` để Docker bỏ qua bước `pip install`:

```powershell
docker build `
  --build-arg BASE_IMAGE=ecosort-api:before-20261030 `
  -t ecosort-api:12class `
  .
```

> [!TIP]
> `ecosort-api:before-20261030` là image 4 lớp từ ngày 30/10/2026 còn tồn tại cục bộ.
> Dùng `docker images` để xem danh sách image hiện có.

Lệnh tương đương trên Linux/Mac:
```bash
docker build \
  --build-arg BASE_IMAGE=ecosort-api:before-20261030 \
  -t ecosort-api:12class \
  .
```

### 2.3. Build và chạy luôn với Docker Compose

```powershell
cd e:\Document_Collage\CNM\CK\backend

# Build + start
docker compose up --build -d

# Xem log
docker compose logs -f
```

---

## 3. Chạy image

### 3.1. Chạy bằng Docker Compose (khuyến nghị)

```powershell
docker compose up -d
```

API sẽ lắng nghe tại `http://localhost:8000`.

### 3.2. Chạy bằng lệnh `docker run` thủ công

```powershell
docker run -d `
  --name ecosort-api `
  -p 8000:8000 `
  -v ecosort-data:/app/data `
  -e ECOSORT_STORAGE=local `
  ecosort-api:12class
```

---

## 4. Verify image dùng đúng model 12 lớp

### 4.1. Kiểm tra `/health`

```powershell
# Chờ container sẵn sàng (~30s sau khi start)
Start-Sleep -Seconds 30

Invoke-RestMethod http://localhost:8000/health | ConvertTo-Json -Depth 5
```

Kết quả mong đợi phải có:
```json
{
  "class_names": [
    "Plastic","Metal","Paper","Organic","Cardboard","GreenGlass",
    "BrownGlass","WhiteGlass","Clothes","Shoes","Batteries","Trash"
  ]
}
```

### 4.2. Dùng script verify tự động

```powershell
cd e:\Document_Collage\CNM\CK\backend
python tools\verify_docker_12class.py
```

Script sẽ in `PASS`/`FAIL` cho từng mục (xem Mục 7 để biết thêm).

### 4.3. Curl (nếu có)

```powershell
curl http://localhost:8000/health
```

---

## 5. Chạy không có Docker (uvicorn trực tiếp)

Dùng khi môi trường không có Docker hoặc debug nhanh.

### 5.1. Cài dependencies

```powershell
cd e:\Document_Collage\CNM\CK\backend

# (Khuyến nghị) Tạo virtual environment
python -m venv .venv
.venv\Scripts\Activate.ps1

pip install -r requirements.txt
```

### 5.2. Copy `.env`

```powershell
Copy-Item .env.example .env
# Chỉnh sửa .env nếu cần (AIO_KEY, storage, v.v.)
```

### 5.3. Khởi động server

```powershell
uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```

> [!NOTE]
> Biến môi trường `ECOSORT_DATA_FILE` mặc định là `data/ecosort.json` (đường dẫn tương đối từ thư mục chạy).
> Thư mục `data/` sẽ được tạo tự động.

### 5.4. Verify sau khi start

```powershell
Invoke-RestMethod http://localhost:8000/health
```

---

## 6. Rollback về model 4 lớp

### 6.1. Dùng lại image cũ (nếu còn tồn tại cục bộ)

```powershell
# Liệt kê backup images
docker images | Select-String "ecosort"

# Stop image hiện tại
docker compose down

# Chạy image 4 lớp cũ
docker run -d `
  --name ecosort-api `
  -p 8000:8000 `
  -v ecosort-data:/app/data `
  ecosort-api:before-20261024
```

### 6.2. Rollback artifacts về 4 lớp

```powershell
# Backup artifacts 12 lớp trước
Copy-Item app\ai_model\artifacts app\ai_model\artifacts_12class -Recurse

# Restore artifacts 4 lớp (cần có bản backup)
Copy-Item app\ai_model\artifacts_4class\* app\ai_model\artifacts\ -Force
```

Sau đó rebuild:

```powershell
docker compose up --build -d
```

---

## 7. Troubleshooting

### ❌ Build lỗi – TensorFlow 620 MB timeout hoặc mạng chậm

```
Retrying (Retry(total=4, ...)) after connection broken ...
```

**Giải pháp:**

```powershell
# Dùng Docker BuildKit với retry
$env:DOCKER_BUILDKIT = "1"
docker build --network=host -t ecosort-api:12class .

# Hoặc dùng base image có sẵn TF (xem Mục 2.2)
docker build --build-arg BASE_IMAGE=ecosort-api:before-20261030 -t ecosort-api:12class .
```

### ❌ Build lỗi – model.keras không tồn tại

```
RuntimeError: AI artifacts missing: need model.keras and metadata.json
```

**Giải pháp:** Đảm bảo file `app/ai_model/artifacts/model.keras` tồn tại trước khi build.
File này không được commit vào git – phải copy thủ công.

### ❌ Port 8000 đã được sử dụng

```
Error: bind: address already in use
```

**Giải pháp:**

```powershell
# Tìm process đang dùng port 8000
netstat -ano | Select-String ":8000"

# Kill process (thay <PID> bằng PID thực)
Stop-Process -Id <PID> -Force

# Hoặc đổi port trong compose.yaml:
# ports:
#   - "8001:8000"
```

### ❌ Volume data không persist

Nếu dữ liệu mất sau `docker compose down`:

```powershell
# Kiểm tra volume
docker volume ls | Select-String "ecosort"
docker volume inspect ecosort-data

# Đảm bảo compose.yaml có volume mount (đã cấu hình sẵn):
# volumes:
#   - ecosort-data:/app/data
```

> [!CAUTION]
> `docker compose down -v` sẽ **xóa volume** và mất toàn bộ dữ liệu game.
> Chỉ dùng `-v` khi muốn reset hoàn toàn.

### ❌ /health trả về class_names chỉ có 4 phần tử

Container đang chạy image cũ hoặc artifacts 4 lớp.

```powershell
# Kiểm tra image nào đang chạy
docker inspect ecosort-api --format "{{.Config.Image}}"

# Rebuild với image mới
docker compose down
docker compose up --build -d
```

### ❌ Checksum model không khớp

```
RuntimeError: Model checksum does not match metadata
```

File `model.keras` bị hỏng hoặc copy sai phiên bản.
SHA-256 đúng: `c94553614878308bc219487c2910de23ff2dd5a686737014cdc96a540e18814f`

```powershell
# Verify checksum trên Windows
Get-FileHash app\ai_model\artifacts\model.keras -Algorithm SHA256
```

---

## 8. Tham khảo nhanh

| Lệnh | Mục đích |
|---|---|
| `docker compose up -d` | Start container (dùng image đã build) |
| `docker compose up --build -d` | Rebuild và start |
| `docker compose down` | Stop và xóa container (giữ volume) |
| `docker compose logs -f` | Xem log realtime |
| `docker images \| grep ecosort` | Liệt kê images |
| `python tools\verify_docker_12class.py` | Verify 12 lớp |
