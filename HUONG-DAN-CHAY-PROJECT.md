# Cập nhật Organic và Khám phá 3D — 15/10/2026

Xem [hướng dẫn bản mới](ECOSORT_GAME/Docs/ORGANIC-EXPLORATION.md): 36 vật phẩm theo bối cảnh, bốn nhóm rác, điều khiển nhân vật bằng WASD/E và mô hình AI bốn lớp. Backend Docker cần `docker compose up --build -d` trong thư mục `backend`; backend Python cần khởi động lại. `/health` phải có `Organic`. Source chứa checkpoint đã huấn luyện, không cần train lại để chơi.

# Hướng dẫn chạy và chia sẻ EcoSort Quest

## Bổ sung học tập ngày 15/10/2026

Menu → **Học tập / AI / Thống kê**: Học có hướng dẫn, Kiểm tra, Ôn câu sai, ba bối cảnh và nhiệm vụ AI. Xem [hướng dẫn chi tiết](ECOSORT_GAME/Docs/LEARNING-UPGRADE.md). Học không cần mở khóa chiến dịch. Offline dùng được học/kiểm tra/ôn; nhiệm vụ AI thật cần Online và FastAPI.

Cập nhật source rồi dừng/chạy lại FastAPI; Docker chạy `docker compose up -d --build` trong backend. API `/learning/sessions` lưu lịch sử trong learning.json ở backend/volume. Sáu feed Adafruit giữ nguyên; thống kê chi tiết không nằm trên Dashboard hiện tại. Client giữ lượt chưa gửi để thử lại từ Kết quả học tập. Báo cáo Word 13/10 là mốc trước nâng cấp này.

Cập nhật ngày 13/10/2026. Bản Windows / Unity Editor có 18 mẫu rác, sổ tay phân loại, băng chuyền chuyển động và kéo thả vào thùng. Thử thách AR hiện là mô phỏng 3D trên PC. Repo gốc có README và .gitignore để đưa source lên GitHub; không cần installer.

## 1. Project gồm những gì?

| Thành phần | Vai trò |
|---|---|
| Unity | Game, kéo thả, chụp/nạp ảnh, hiển thị tiến trình |
| FastAPI | Backend nhận yêu cầu từ Unity |
| Uvicorn | Chương trình chạy ứng dụng FastAPI |
| MobileNetV2 | Model nhận diện Plastic, Metal, Paper |
| Adafruit IO | Lưu và xem dữ liệu cloud qua feed/Dashboard |
| Docker Compose | Cách chạy backend trong container, thay cho chạy Python trực tiếp |

Luồng: Unity → FastAPI → model AI / Adafruit IO → kết quả trả về Unity.
Chỉ chọn **một** cách chạy backend: Python hoặc Docker. Không chạy cả hai cùng cổng 8000.

## 2. Gửi project cho thành viên khác

Tạo một thư mục mới, ví dụ `EcoSort-Share`, rồi **chép** các thành phần sau vào đó. Không xóa các thư mục gốc đang sử dụng.

```text
EcoSort-Share/
  HUONG-DAN-CHAY-PROJECT.md
  HOAN-THIEN-WINDOWS.md
  ECOSORT_GAME/
    Assets/             (giữ toàn bộ file .meta)
    Packages/
    ProjectSettings/
    Docs/
    README-EcoSort.md
  backend/
    app/                (bao gồm model và metadata)
    tests/
    requirements.txt
    requirements-dev.txt
    Dockerfile
    compose.yaml
    .dockerignore
    .gitignore
    .env.example
    README.md
    EVIDENCE-CHECKLIST.md
```

Trong `backend/app`, bỏ các thư mục `__pycache__` khi chép. Kiểm tra vẫn có:

- `backend/app/ai_model/artifacts/model.keras`
- `backend/app/ai_model/artifacts/metadata.json`

Không gửi `.env`, `.venv`, dữ liệu tiến trình trong `backend/data`, Unity `Library`, `Temp`, `Logs`, `UserSettings`, `.plastic`, `.git`, `.vs` hoặc file `.csproj`/`.slnx` sinh tự động. Log có thể chứa thông tin đăng nhập hoặc cấu hình máy. Unity tự tạo lại cache và project IDE trên máy nhận.

Không cần Dataset/Kaggle để chạy game. Chỉ gửi thêm `ai-model` và tài liệu nghiên cứu nếu nhóm cần huấn luyện hoặc làm báo cáo. Có thể kèm vài ảnh JPG/PNG rác không chứa thông tin cá nhân để thử Scan.

Nén thư mục `EcoSort-Share` thành ZIP bằng File Explorer, rồi gửi qua Drive/USB/kênh nhóm. Người nhận phải giải nén trước khi chạy. Chưa có gói ZIP tự động được tạo bởi hướng dẫn này.

Nếu chỉ gửi bản chơi: build lại sau thay đổi code, gửi **toàn bộ** `ECOSORT_GAME/Builds/Windows`, không chỉ `.exe`. Bản đó vẫn cần backend đang chạy tại URL đã cấu hình lúc build. Không dùng bản build cũ để kiểm tra tính năng kéo thả mới.

## 3. Chuẩn bị máy nhận

- Unity Hub và Unity Editor **6000.6.2f1** để mở/sửa source game; mở thư mục `ECOSORT_GAME` bằng Hub.
- Nếu chạy backend Python: Python **3.12, 64-bit** và Internet để cài dependencies.
- Nếu chạy backend Docker: Docker Desktop chạy Linux containers và Docker Compose hỗ trợ `env_file.required` (Compose 2.24 trở lên).
- Adafruit chỉ cần khi demo cloud; chạy local không cần tài khoản hoặc key.

Các lệnh dưới đây chạy bằng PowerShell. Đổi đường dẫn ví dụ theo vị trí giải nén trên máy nhận.

## 4. Cách A — chạy FastAPI bằng Python

### Lần đầu trên máy mới

```powershell
cd "D:\EcoSort-Share\backend"
py -3.12 -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
```

Không chép `.venv` từ máy khác. Các lệnh dùng trực tiếp Python trong `.venv`, không cần Activate.ps1 và không cần đổi Execution Policy.

Nếu chưa có `.env`, tạo từ mẫu:

```powershell
Copy-Item .env.example .env
notepad .env
```

Chỉ sao chép mẫu khi **chưa có** `.env`; không ghi đè cấu hình Adafruit đã hoạt động. Để thử local, giữ `ECOSORT_STORAGE=local`.

### Mỗi lần chạy

```powershell
cd "D:\EcoSort-Share\backend"
.\.venv\Scripts\python.exe -m uvicorn app.main:app --env-file .env --host 127.0.0.1 --port 8000
```

Trên máy gốc, đường dẫn là `E:\Document_Collage\CNM\CK\backend`.
Giữ cửa sổ PowerShell này mở. Lần đầu cần chờ model nạp xong và dòng `Application startup complete`. Dừng bằng Ctrl+C.

**Lưu ý:** backend đọc biến môi trường. Tham số `--env-file .env` giúp Uvicorn nạp file `.env`; chỉ tạo file rồi chạy lệnh không có tham số này có thể khiến backend vẫn ở local.

Mở trình duyệt:

- Health: <http://127.0.0.1:8000/health>
- Swagger thử API: <http://127.0.0.1:8000/docs>

Health cần có `status: "ok"`, `ai_ready: true`. `storage_mode` cho biết local hay adafruit_io. Health thành công chứng minh API/model sẵn sàng, chưa chứng minh các feed cloud đọc/ghi được.

## 5. Cách B — chạy backend bằng Docker

Mở Docker Desktop, chờ engine sẵn sàng. Dừng backend Python nếu đang chiếm cổng 8000.

```powershell
cd "D:\EcoSort-Share\backend"
docker --version
docker compose version
docker compose build
docker compose up -d
docker compose ps
docker compose logs --tail 50 ecosort-api
Invoke-RestMethod http://127.0.0.1:8000/health
```

Compose tự đọc `.env` bên cạnh `compose.yaml`; không có `.env` thì mặc định local. Unity vẫn kết nối `http://127.0.0.1:8000` như cách A. Docker chỉ chạy backend, không chạy Unity.

Các lệnh thường dùng:

```powershell
docker compose logs -f ecosort-api
docker compose stop
docker compose up -d
```

Sau khi sửa code/model hoặc dependencies, chạy `docker compose up -d --build`. Sau khi sửa `.env`, chạy `docker compose up -d --force-recreate` để container nhận cấu hình mới. Dữ liệu local lưu trong named volume; không dùng `down -v` nếu muốn giữ dữ liệu.

## 6. Kết nối Unity với backend

Trong MainMenu có **Kết nối backend**: chọn Chơi ngoại tuyến để thử campaign/sổ tay không cần server; chọn Online và nhập URL để nhận diện thật. Bản standalone mặc định ngoại tuyến, lưu lựa chọn kết nối trên máy người chơi. Editor giữ ApiSettings.asset ở đầu phiên; lựa chọn trong game áp dụng xuyên scene trong phiên đó. Không nhập AIO Key vào ô URL.

1. Chạy backend theo A hoặc B; xác nhận health.
2. Unity Hub → Add project from disk → chọn `ECOSORT_GAME`; mở bằng 6000.6.2f1 và chờ import.
3. Trong Project, chọn `Assets/_EcoSortQuest/ScriptableObjects/ApiSettings.asset`.
4. Đặt Mode = **Rest**, Base Url = `http://127.0.0.1:8000`, Minimum Confidence = `0.75`. Base URL không thêm `/predict` hoặc `/docs`.
5. Mở `Assets/_EcoSortQuest/Scenes/MainMenu.unity`, bấm Play.
6. Vào Tiến trình → Kiểm tra API; phải nhận phản hồi backend.
7. Chơi ba level: giữ chuột trái trên rác, kéo và thả vào thùng. Đúng +10, sai −5, không âm; cần ít nhất 7/10 đúng để qua. Level 2/3 có timer; ESC tạm dừng.
8. Sau Level 3, vào Scan. Nạp JPG/PNG bằng đường dẫn tuyệt đối hoặc mở webcam/chụp, rồi Gửi ảnh đến AI.
9. Kết quả hợp lệ và đủ confidence mới mở thử thách mô phỏng. Kéo thả đúng để hoàn thành +20 một lần trong nhiệm vụ.
10. Đợi yêu cầu gửi hoàn tất trước khi chuyển màn; vào Tiến trình để đồng bộ/tải lại và đối chiếu cloud.

Mode Mock chỉ dùng kiểm thử UI, không phân tích ảnh và không gửi cloud. Nhận diện AI tự nó không cộng điểm. AR trên PC là mô phỏng, không phải camera tracking ARCore.

## 7. Bật Adafruit IO

Mỗi máy có `.env` riêng. Thành viên muốn chạy độc lập nên dùng tài khoản/feed riêng; cấu hình hiện tại lưu snapshot gần nhất cho **một demo player**, không phải database nhiều người chơi. Nhiều máy ghi cùng tài khoản có thể thay snapshot của nhau.

Trong `.env` của backend, cấu hình riêng trên máy:

```dotenv
ECOSORT_STORAGE=adafruit_io
AIO_USERNAME=ten_tai_khoan_cua_ban
AIO_KEY=nhap_key_that_tren_may_cua_ban
```

Giữ các biến còn lại theo `.env.example`. Không gửi key qua chat, không đặt vào Unity/APK, không đưa vào ZIP/Git hoặc ảnh minh chứng.

Tạo đúng sáu feed key sau trên Adafruit IO (tên hiển thị có thể khác, **key phải khớp**):

| Feed key | Nội dung |
|---|---|
| `game-score` | JSON điểm, level, số câu đúng và player_id |
| `current-level` | Số level đã mở |
| `player-progress` | JSON tiến trình đầy đủ, dùng để đọc lại |
| `detected-waste` | JSON nhãn, confidence và accepted |
| `ai-confidence` | Confidence dạng số từ 0 đến 1 |
| `ar-challenge-result` | JSON kết quả thử thách mô phỏng |

Khởi động lại Python với `--env-file .env`, hoặc recreate container. Health cần báo `storage_mode: "adafruit_io"`. Thực hiện scan, hoàn thành lượt và đồng bộ tiến trình, rồi mở feed kiểm tra bản ghi/thời gian thật. Không spam nút; gói Basic hiện giới hạn 30 data points/phút, một thao tác có thể ghi nhiều feed.

## 8. Xem Dashboard

Dashboard đã tạo và kiểm tra trực tiếp trên Opera ngày 13/10/2026:
[EcoSort Quest - Game Results](https://io.adafruit.com/minhquach2910/dashboards/ecosort-quest-game-results).

Gồm Gauge confidence (ngưỡng 0.75), level đã mở, Stream bốn feed JSON với lịch sử 24 giờ và Line Chart confidence với Raw Data Only. Đã thấy level 3, các bản ghi điểm/AI/tiến trình/thử thách và đường biểu đồ từ dữ liệu có sẵn. Gauge làm tròn giá trị; xem Stream/feed để đọc confidence đầy đủ. Khối level dạng Text có thể ghi dữ liệu khi nhập rồi Enter, vì vậy chỉ xem, không sửa giá trị tại Dashboard.

Ảnh minh chứng: `ECOSORT_GAME/Docs/Evidence/adafruit-dashboard-20261013.png`. Đây là dữ liệu cloud đã có; lượt tạo Dashboard không sinh thêm điểm hoặc thử thách giả.

Dashboard dành cho project nằm trong tài khoản Adafruit đã cấu hình. Người xem cần quyền truy cập tài khoản/dashboard tương ứng; file source không chứa phiên đăng nhập của người gửi.

Với cấu trúc feed hiện có: dùng Gauge/Line Chart cho `ai-confidence` (0–1), hiển thị số cho `current-level` (1–3), và Stream để xem các feed JSON. Không gắn `game-score` dạng JSON trực tiếp vào biểu đồ số và coi đó là điểm: dashboard không tự tách trường `score`.

Khi demo: mở Unity và Dashboard cạnh nhau, thực hiện một thao tác, đợi đồng bộ rồi quan sát bản ghi mới. Giá trị có sẵn là dữ liệu lịch sử, không tự chứng minh lần thao tác vừa thực hiện.

## 9. Hai máy dùng chung backend trong LAN

Cách đơn giản nhất là mỗi thành viên tự chạy backend trên máy mình. Nếu dùng chung: backend Python chạy với `--host 0.0.0.0` thay cho 127.0.0.1; Docker đã bind cổng 8000 ra host theo Compose.

Trên máy backend, dùng `ipconfig` lấy IPv4 của mạng LAN, ví dụ `192.168.1.10`. Máy Unity đặt Base Url = `http://192.168.1.10:8000` và thử mở `/health` trước. Hai máy cùng mạng; cho phép cổng 8000 trên mạng Private trong Firewall khi cần. Không dùng `localhost` để trỏ sang máy khác, không nhập `0.0.0.0` vào Unity, không mở port router ra Internet. API hiện dùng ID ẩn danh, chưa có đăng nhập/xác thực.

## 10. Lỗi thường gặp

| Hiện tượng | Cách kiểm tra |
|---|---|
| Không tìm thấy `app` | Chạy lệnh từ thư mục `backend` |
| `py` không tồn tại | Cài Python 3.12 và Python Launcher; kiểm tra lại terminal |
| Activate.ps1 bị chặn | Dùng trực tiếp `.venv\Scripts\python.exe` như hướng dẫn |
| Cổng 8000 đã dùng | Dừng backend Python/container đang chạy trùng |
| Health vẫn local | Kiểm tra `.env`, `--env-file`, biến môi trường cũ trong terminal; restart/recreate đúng tiến trình |
| HTTP 503 khi ghi cloud | Kiểm tra tài khoản/key tại máy, feed key, mạng và giới hạn; không đăng log chứa key |
| Unity lỗi kết nối | Thử health trên cùng máy Unity; kiểm tra URL, Rest, backend và Firewall LAN |
| AI từ chối ảnh | Chụp một vật phẩm rõ, đủ sáng, nền đơn giản; không hạ threshold chỉ để ép nhận |
| Thiếu model | Giữ model.keras và metadata.json trong artifacts; không chỉ chép code Python |
| Unity vẫn click chọn | Chạy source mới và chờ compile; bản exe cũ cần build lại |
| Docker daemon không chạy | Mở Docker Desktop, chờ engine; kiểm tra Linux containers/WSL theo hướng dẫn Docker |
| Compose báo lỗi `required` | Cập nhật Docker Compose, cấu hình cần hỗ trợ env_file.required |

## 11. Kiểm tra trước khi bàn giao

- [ ] Gói chia sẻ có Assets/Packages/ProjectSettings, backend app và model.
- [ ] Không có `.env`, `.venv`, cache, log hoặc bí mật trong gói.
- [ ] Máy nhận tạo môi trường riêng và mở được health.
- [ ] Unity Rest kiểm tra API thành công; kéo thả được, thả ngoài không chấm điểm.
- [ ] Scan ảnh thật trả kết quả, thử thách mô phỏng hoàn thành và progress đọc lại.
- [ ] Cloud mode có bản ghi mới trong feed/Dashboard.
- [ ] Nếu gửi bản exe: build mới, gửi đầy đủ thư mục và thử standalone.

Backend đã chạy lại 6/6 test thành công ngày 13/10/2026. Test cloud adapter dùng transport mô phỏng, không thay cho nghiệm thu cloud. Dữ liệu sáu feed thực tế đã được quan sát trong tài khoản ngày 13/10. Gameplay nâng cấp đã chạy 20/20 EditMode và 7/8 PlayMode test thành công, có kiểm tra ảnh giao diện. Docker có cấu hình, tài liệu này không tự chứng minh container đã build/run trên máy nhận.
