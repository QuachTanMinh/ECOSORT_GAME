# Kiểm thử module AI

Ngày chạy: 2026-10-12, Windows 11, Python 3.12.14, TensorFlow 2.20.0, CPU. Lệnh từ thư mục `ai-model`:

```powershell
& .\.venv\Scripts\python.exe -m pytest tests -q --tb=short --junitxml=reports/tests.xml
```

Kết quả cuối: **18 passed**. Bằng chứng máy đọc: `tests.xml`; console: `tests.log`.

| Phạm vi | Đã kiểm tra |
|---|---|
| Tiền xử lý | RGB/RGBA/grayscale về RGB uint8 224×224 |
| Dữ liệu lỗi | Rỗng, bytes lỗi, sai kiểu, quá giới hạn upload |
| Split | Xung đột nhãn ở ảnh trùng bị từ chối; nhóm phiên chụp không chia sang nhiều split; manifest thật không trùng đường dẫn/nhóm/hash giữa split |
| Model thật | Nạp `.keras`, đúng lớp và kiểu JSON, confidence hợp lệ, inference khớp tiền xử lý trực tiếp |
| Ngưỡng | Ngưỡng 0 và 1 thay đổi accepted theo confidence thật |
| Artifacts | Thiếu model và checksum sai được phát hiện |
| Backend import | Package `ai_model` dùng import tương đối đúng |

Các test với ảnh tổng hợp chỉ xác minh hành vi kỹ thuật, không được tính vào chỉ số chất lượng phân loại. Accuracy/F1 dựa trên 224 ảnh thật trong test giữ riêng, ở `EVALUATION.md`.

Chưa kiểm thử: ảnh điện thoại độc lập, ảnh unknown/OOD để đo khả năng từ chối, HTTP/FastAPI, Docker/Linux, Unity/Android end-to-end, tải nhiều người dùng. Quy trình và bàn giao ở `PHONE-TEST.md`, `HANDOFF-BACKEND.md`.
