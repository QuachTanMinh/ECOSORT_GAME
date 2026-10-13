# Liên kết thành viên 1 (Unity) và thành viên 2 (AI)

Luồng dự án hiện tại: **Unity chụp/nạp ảnh → HTTP FastAPI `/predict` → Python nạp `.keras` → JSON → Unity hiển thị và chuyển sang thử thách**. Kaggle dùng để train, không làm API server cho game. Thành viên 3 triển khai backend đầy đủ và cloud; có API demo riêng để kiểm tra Scan trước khi backend hoàn tất.

## 1. Chốt artifact sau Kaggle

Tải `ecosort-kaggle-handoff.zip`, giải nén ra thư mục mới, ví dụ `E:\Document_Collage\CNM\CK\kaggle-result\ai_model`. Giữ cả `artifacts/model.keras` và `artifacts/metadata.json` từ cùng một lần chạy. Không ghi đè baseline Windows khi chưa xác minh model mới.

Kiểm tra class order `Plastic, Metal, Paper`, input `[224,224,3]`, threshold và phiên bản TensorFlow/Keras trong metadata. Model nhận RGB float32 0–255, chuẩn hóa nằm trong model; backend gọi inference với bytes ảnh gốc, không preprocess lần hai. Đọc báo cáo test và hoàn thành ảnh điện thoại độc lập theo `PHONE-TEST.md`.

## 2. Kiểm thử AI riêng trên máy backend

Mở PowerShell tại `E:\Document_Collage\CNM\CK`. Môi trường `.venv` hiện có đã chạy baseline TensorFlow 2.20/Keras 3.15. Nếu model Kaggle dùng phiên bản khác, tạo **môi trường serving riêng**, cài `requirements-serving.txt` trong zip và xác minh model load được trên hệ điều hành đích. Lệnh dưới dùng venv hiện tại làm ví dụ khi tương thích:

```powershell
$env:ECOSORT_AI_ARTIFACTS = 'E:\Document_Collage\CNM\CK\kaggle-result\ai_model\artifacts'
& .\ai-model\.venv\Scripts\python.exe .\kaggle-result\ai_model\inference.py 'E:\duong-dan\anh-dien-thoai.jpg'
```

Đường dẫn ảnh phải tồn tại. Nếu chưa train Kaggle, bỏ biến môi trường và dùng `ai-model/inference.py` với baseline hiện có. Lỗi load/checksum phải được xử lý trước bước HTTP. Metadata/hash xác minh cặp artifact; không sửa hash để bỏ qua lỗi.

## 3. Chạy cầu nối HTTP để thử Scan ngay

Đã có `ai-model/api_demo.py` với `/health` và `/predict`, model thật, warmup khi startup, inference trong threadpool, giới hạn ảnh 4 MiB, trả lỗi ảnh 400/quá lớn 413. Demo chỉ phục vụ tích hợp Scan; chưa có scores/progress/ar-results/cloud của thành viên 3.

```powershell
# Chạy tại E:\Document_Collage\CNM\CK
& .\ai-model\.venv\Scripts\python.exe -m pip install -r .\ai-model\requirements-demo.txt
# Dùng artifact Kaggle nếu đã kiểm tra tương thích; bỏ dòng này để dùng baseline.
$env:ECOSORT_AI_ARTIFACTS = 'E:\Document_Collage\CNM\CK\kaggle-result\ai_model\artifacts'
& .\ai-model\.venv\Scripts\python.exe -m uvicorn api_demo:app --app-dir .\ai-model --host 127.0.0.1 --port 8000 --workers 1
```

Với môi trường Kaggle khác phiên bản, requirements-demo hiện tham chiếu baseline TensorFlow 2.20; không dùng lệnh cài này để hạ model runtime. Trong môi trường serving mới, cài `requirements-serving.txt` của zip rồi cài riêng `fastapi uvicorn python-multipart httpx`, dùng Python của môi trường đó để chạy uvicorn. API demo dùng implementation Predictor hiện tại tương thích contract; backend chính nên dùng nguyên package inference trong zip.

Mở `http://127.0.0.1:8000/docs`, gọi GET `/health`, rồi POST `/predict` chọn ảnh ở field **file**. Hoặc PowerShell cửa sổ khác:

```powershell
curl.exe http://127.0.0.1:8000/health
curl.exe -X POST http://127.0.0.1:8000/predict -F 'file=@E:\duong-dan\anh-dien-thoai.jpg'
```

Response ví dụ về schema (không phải kết quả cam kết cho ảnh của bạn):

```json
{"class_name":"Plastic","confidence":0.92,"accepted":true}
```

Không bọc object trong `data`, không đổi label thành số, không nhân confidence lên thang 100. `accepted=false` vẫn trả class_name dự đoán; Unity phải yêu cầu chụp lại. `/health` trả threshold và model hash để đối chiếu deployment.

## 4. Chuyển Unity sang AI thật

Trong Unity, mở asset:

`Assets/_EcoSortQuest/ScriptableObjects/ApiSettings.asset`

Thiết lập Inspector:

| Trường | Giá trị |
|---|---|
| Mode | **Rest** |
| Base Url | `http://127.0.0.1:8000` khi Unity Editor và API trên cùng PC |
| Minimum Confidence | **Giống threshold trong metadata và `/health`**, không mặc định giữ 0.75 nếu Kaggle chọn ngưỡng khác |
| Timeout Seconds | 15, tăng nếu warmup/CPU cần thời gian sau đo thực tế |
| Max Upload Bytes | 4194304 |

Hiện asset đang Mode=Mock, URL trống, MinimumConfidence=0.75. Không chỉ đổi URL mà quên Mode. Thoát Play rồi vào lại sau thay cấu hình vì service được tạo trong `EcoSortApp.Start()`.

Mở `ScanScene` hoặc đi từ Main Menu đến Scan → nạp đường dẫn ảnh JPG/PNG hoặc mở webcam/chụp → **Gửi ảnh đến AI**. Unity hiện resize cạnh dài tối đa 1024, encode JPEG chất lượng 85 và gửi multipart field `file`. Khi accepted=true và confidence đạt ngưỡng phía client, nút thử thách được mở; class_name được ánh xạ sang Plastic/Metal/Paper.

Các file đã có: `Scripts/Networking/RestEcoSortService.cs`, `Scripts/Core/EcoSortApp.Scan.cs`. Không cần nhúng TensorFlow vào Unity trong kiến trúc này. AR hiện vẫn là mô phỏng PC; nối AI thật không biến nó thành AR Foundation/ARCore thật.

## 5. Khi chuyển sang Android

Điện thoại không dùng `127.0.0.1` của PC. Chạy API với `--host 0.0.0.0`, lấy IPv4 LAN của PC bằng `ipconfig`, đặt Base Url ví dụ `http://192.168.1.10:8000`; điện thoại và PC cùng LAN, firewall cho phép cổng 8000. `0.0.0.0` chỉ là địa chỉ listen, không điền vào Unity. HTTP cleartext trên Android có thể bị chặn bởi Player Settings; chỉ trong mạng thử nghiệm mới bật cho phép HTTP downloads, còn khi demo công khai triển khai HTTPS.

Mở `/health` từ browser trên điện thoại trước. Nếu không vào được thì xử lý mạng/firewall trước sửa AI. HTTP LAN có thể cần cấu hình allow insecure HTTP trong Android/Unity; khi triển khai Internet dùng HTTPS. Đây là checklist triển khai cần xác minh trên thiết bị, chưa có APK/AR thật được kiểm thử ở bước này.

## 6. Bàn giao sang backend chính của thành viên 3

Chép package `ai_model` từ zip vào backend, cài dependency serving và dùng `from ai_model import predict_image`. `/predict` đọc field `file`, chuyển bytes vào inference, trả nguyên ba trường; đưa inference đồng bộ vào threadpool. Thống nhất threshold với Unity; chuyển Base Url từ demo sang backend chính.

Thành viên 3 bổ sung các endpoint trong `ECOSORT_GAME/Docs/API-CONTRACT.md`: `/progress/{player_id}`, `/scores`, `/ar-results`, Adafruit IO và Docker. Demo sẽ trả 404 cho những endpoint này; chỉ kiểm tra AI Scan/hiển thị bằng demo, không nghiệm thu cloud/tiến trình bằng nó. Kiểm tra pipeline lại trong container/Linux vì model chạy được Windows không chứng minh deployment chạy được.

## 7. Bằng chứng cần lưu

| Test | Kết quả mong đợi |
|---|---|
| `/health` sau restart | ready, model hash/ngưỡng đúng artifact |
| Ảnh Plastic/Metal/Paper mới | JSON hợp lệ, ghi nhận nhãn thật và dự đoán, không yêu cầu mọi ảnh đều đúng |
| Ảnh confidence thấp | accepted=false; Unity không mở thử thách |
| Bytes lỗi / file >4 MiB | 400 / 413, UI báo lỗi |
| Tắt server / ngắt mạng | Unity báo lỗi hoặc timeout, không giả trả mock thành công |
| Ảnh từ Android | API nhận đúng hướng ảnh, JSON về UI, nhãn sang thử thách đúng mapping |

Lưu ảnh gốc, JSON, metadata/model hash, ảnh màn hình/log và video từ chụp → gọi API → nhận kết quả → mở thử thách. Đánh giá điện thoại bằng tập vật phẩm/session riêng; không dùng ảnh vừa bổ sung train để chứng minh nhận diện ngoài dữ liệu.

Nguồn triển khai: [FastAPI UploadFile](https://fastapi.tiangolo.com/tutorial/request-files/), [FastAPI lifespan](https://fastapi.tiangolo.com/advanced/events/), [Starlette threadpool](https://www.starlette.io/threadpool/).
