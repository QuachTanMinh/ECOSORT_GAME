# 🚀 Hướng dẫn Clone và Chạy Dự án ECOSORT_GAME trên Máy Mới từ A-Z

Tài liệu này hướng dẫn chi tiết từng bước thiết lập và chạy toàn bộ dự án **ECOSORT_GAME** (bao gồm Unity Client và Backend AI 12 lớp) trên một máy tính mới sau khi clone từ GitHub.

---

## 📁 1. Cấu trúc thư mục dự án

```text
CK/
├── ECOSORT_GAME/              # Source Unity game (Unity 6 / 6000.6.2f1)
│   ├── Assets/                # Code C#, Prefabs, Scenes, Materials, Audio
│   ├── Builds/Windows/        # Bản build Standalone chạy ngay không cần Unity
│   ├── Docs/                  # Toàn bộ tài liệu kỹ thuật, playtest, checklist
│   └── run-benchmark.ps1      # Script đo FPS và hiệu năng chuẩn hóa
├── backend/                   # FastAPI Server AI nhận diện 12 lớp & Adafruit IO
│   ├── app/                   # Mã nguồn API & Model EfficientNetV2B0
│   ├── tools/                 # Script kiểm thử Docker & Cloud
│   ├── compose.yaml           # Cấu hình Docker Compose
│   ├── Dockerfile             # Dockerfile build backend
│   └── requirements.txt       # Danh sách thư viện Python
├── ai-model/                  # Mã nguồn huấn luyện và đánh giá mô hình AI
├── BaoCao/                    # Báo cáo đồ án cuối kỳ
└── HUONG-DAN-CLONE-VA-CHAY-MAY-MOI.md
```

---

## 🛠️ 2. Yêu cầu phần mềm trên máy mới

| Thành phần | Yêu cầu | Mục đích |
|---|---|---|
| **Git** | Bản mới nhất | Clone và đồng bộ mã nguồn |
| **Unity Hub & Unity Editor** | `6000.6.2f1` (Unity 6) | Mở, chỉnh sửa và build game Unity |
| **Python** *(Nếu chạy local)* | Python `3.12` (64-bit) | Chạy FastAPI backend và AI |
| **Docker Desktop** *(Tùy chọn)* | Docker ≥ 24, Compose ≥ 2.24 | Chạy backend trong container không cần cài Python |

---

## 📥 3. Bước 1: Clone dự án về máy

Mở **PowerShell** hoặc **Terminal** và chạy:

```powershell
git clone <URL_REPO_CUA_BAN>
cd CK
```

---

## 🖥️ 4. Bước 2: Khởi động Backend AI (Chọn 1 trong 2 cách)

Backend cung cấp API nhận diện ảnh rác 12 lớp (`POST /predict`), lưu trữ điểm số (`/scores`, `/ar-results`) và đồng bộ tiến trình học tập (`/progress`, `/learning`).

### 👉 Cách A: Chạy trực tiếp bằng Python (Khuyên dùng - Nhanh & Nhẹ)

1. **Di chuyển vào thư mục backend:**
   ```powershell
   cd backend
   ```

2. **Tạo và kích hoạt môi trường ảo Python 3.12:**
   ```powershell
   py -3.12 -m venv .venv
   .\.venv\Scripts\activate
   ```
   *(Nếu dùng Linux/macOS: `python3.12 -m venv .venv && source .venv/bin/activate`)*

3. **Cài đặt thư viện:**
   ```powershell
   python -m pip install --upgrade pip
   pip install -r requirements.txt
   ```

4. **Tạo file cấu hình `.env`:**
   ```powershell
   Copy-Item .env.example .env
   ```
   > [!NOTE]
   > Mặc định file `.env` đã thiết lập `ECOSORT_STORAGE=local`. Bạn **không cần tài khoản Adafruit IO** để chạy thử nghiệm trên máy. Nếu muốn kết nối Adafruit IO thật, điền thêm `AIO_USERNAME` và `AIO_KEY` vào file `.env`.

5. **Khởi động server:**
   ```powershell
   python -m uvicorn app.main:app --host 127.0.0.1 --port 8000 --reload
   ```

6. **Kiểm tra hoạt động:**
   Mở trình duyệt truy cập:
   - **Kiểm tra sức khỏe & 12 lớp AI:** [http://127.0.0.1:8000/health](http://127.0.0.1:8000/health)  
     *(Phải thấy `"status": "ok"` và đủ 12 nhóm rác)*
   - **Bảng theo dõi điểm trực tiếp:** [http://127.0.0.1:8000/dashboard](http://127.0.0.1:8000/dashboard)
   - **Tài liệu API Swagger:** [http://127.0.0.1:8000/docs](http://127.0.0.1:8000/docs)

---

### 👉 Cách B: Chạy bằng Docker Compose (Không cần cài Python/TensorFlow)

1. **Bật Docker Desktop trên máy.**
2. **Chạy lệnh build và khởi động:**
   ```powershell
   cd backend
   docker compose up -d --build
   ```
3. **Kiểm tra trạng thái container:**
   ```powershell
   docker compose ps
   python tools/verify_docker_12class.py
   ```
   *(Xem thêm chi tiết tại file [backend/DOCKER-BUILD-12CLASS.md](backend/DOCKER-BUILD-12CLASS.md))*

---

## 🎮 5. Bước 3: Mở và Chạy Game Unity

### Cách 1: Chạy ngay không cần cài Unity Editor (Bản Standalone .exe)

Nếu máy bạn chỉ muốn test game hoặc chấm điểm nhanh:
1. Vào thư mục `ECOSORT_GAME/Builds/Windows/`
2. Chạy file `ECOSORT_GAME.exe`
3. Game sẽ khởi chạy ở độ phân giải 1600×900 (có thể bấm Alt+Enter hoặc vào Cài đặt để bật Toàn màn hình).

### Cách 2: Mở trong Unity Editor để phát triển / debug

1. Mở **Unity Hub**.
2. Chọn **Projects** → Bấm nút **Add** (hoặc dấu mũi tên cạnh Add → **Add project from disk**).
3. Chọn thư mục `ECOSORT_GAME` trong project.
4. Chọn đúng Editor Version: **`6000.6.2f1` (Unity 6)**.
5. Chờ Unity giải nén Package và nạp tài nguyên (lần đầu có thể mất 3-5 phút).
6. Trong cửa sổ **Project**, mở Scene chính:  
   `Assets/_EcoSortQuest/Scenes/MainMenu.unity`
7. Bấm nút **Play (▶)** ở thanh công cụ trên cùng.

---

## ⚙️ 6. Bước 4: Thiết lập kết nối trong Game

1. Tại **Menu chính** của game, bấm vào biểu tượng bánh răng **Cài đặt** (hoặc nút **Kết nối backend**).
2. Chuyển sang tab **Kết nối**:
   - **Chơi Online (có AI & lưu điểm):** Nhập URL backend: `http://127.0.0.1:8000` rồi chọn **Đã kết nối / Online**.
   - **Chơi Ngoại tuyến:** Chọn **Chơi trên máy (Offline)**. Toàn bộ gameplay, phân loại rác, tính điểm đều hoạt động bình thường mà không cần mạng. Khi có mạng trở lại, hàng đợi `OfflineQueue` sẽ tự động đồng bộ điểm lên máy chủ.

---

## 🧪 7. Hướng dẫn thử nghiệm các tính năng chính

| Tính năng | Thao tác trên màn hình | Kết quả mong đợi |
|---|---|---|
| **Chiến dịch phân loại** | Bấm **CHIẾN DỊCH PHÂN LOẠI** → Chọn Cấp 1, 2 hoặc 3 | Kéo rác vào thùng. Đúng +10, Sai −5. Đạt 7/10 câu để qua màn. |
| **Hành trình dọn dẹp** | Bấm **HÀNH TRÌNH DỌN DẸP KHU VỰC** | Khám phá 3D góc nhìn thứ ba tại Nhà ở, Trường học, Công viên, Bệnh viện, Siêu thị, Bãi biển. |
| **Phân loại bằng ảnh** | Bấm **PHÂN LOẠI BẰNG ẢNH** → Mở webcam hoặc Nạp file ảnh | AI nhận diện 12 nhóm rác. Bấm **Xác nhận kiến thức** để vào thử thách kéo thả 3D với đúng thùng rác tương ứng. |
| **Chỉnh thời gian lượt chơi** | Vào **Cài đặt** → tab **Lối chơi** → Mục **THỜI GIAN MỖI LƯỢT** | Chọn: *Ngắn (40s/30s)*, *Mặc định (75s/50s)* hoặc *Dài (120s/90s)*. |
| **Đánh giá Trước & Sau** | Vào **Kết quả học tập** → tab **Trước và sau** | Làm bài test 10 câu trước khi học, sau đó chơi game và làm lại test sau để đo mức độ tiến bộ. |
| **Đo FPS Benchmark** | Chạy lệnh `.\run-benchmark.ps1 -Scene campaign` từ PowerShell | Game tự chạy toàn màn hình trong 20s và xuất bảng FPS trung bình, Median Frame-time (≥ 60 FPS là ĐẠT). |

---

## ❓ 8. Các lỗi thường gặp và cách xử lý (Troubleshooting)

### 1. Webcam không hiện hình ảnh preview:
- Vào **Windows Settings** → **Privacy & Security** → **Camera** → Bật quyền **Let desktop apps access your camera**.
- Trong game, bấm nút **Đổi camera** để chuyển sang thiết bị webcam phù hợp.

### 2. Cổng 8000 bị trùng (Port already in use):
- Kiểm tra tiến trình đang chiếm cổng:
  ```powershell
  Get-Process -Id (Get-NetTCPConnection -LocalPort 8000).OwningProcess
  ```
- Hoặc đổi cổng chạy uvicorn sang cổng khác (ví dụ 8080):
  ```powershell
  python -m uvicorn app.main:app --host 127.0.0.1 --port 8080
  ```
  Sau đó vào game đổi URL kết nối thành `http://127.0.0.1:8080`.

### 3. Unity báo lỗi missing packages hoặc sai phiên bản:
- Đảm bảo mở đúng phiên bản Unity Editor `6000.6.2f1`.
- Nếu Package Manager bị lỗi cache, xóa thư mục `ECOSORT_GAME/Library` và khởi động lại Unity.
