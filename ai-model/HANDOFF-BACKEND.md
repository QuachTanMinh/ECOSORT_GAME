# Bàn giao AI cho thành viên 3

Chép thư mục module thành `backend/ai_model/` (đổi dấu gạch ngang thành gạch dưới). Tối thiểu giữ `__init__.py`, `inference.py`, `data.py`, `artifacts/model.keras`, `artifacts/metadata.json`. Cài dependency AI trong môi trường backend. Khi working directory là `backend`, dùng:

```python
from ai_model import predict_image

result = predict_image(image_bytes)
# dict: class_name (str), confidence (float), accepted (bool)
```

Không chuẩn hóa/resize trước khi gọi hàm. Hàm nhận bytes file gốc JPEG/PNG/WEBP, tối đa 4 MiB, ảnh tối đa 20 triệu pixel; áp dụng EXIF transpose, RGB, resize bilinear 224×224. Model đã chứa Rescaling về [-1,1]. Thứ tự lớp cố định `Plastic, Metal, Paper`, trùng hợp đồng Unity. `confidence` là xác suất softmax lớn nhất; `accepted = confidence >= 0.75` mặc định. Khi từ chối vẫn trả nhãn dự đoán và confidence để UI hiển thị; backend không tự đổi nhãn thành `Unknown`.

Biến môi trường tùy chọn (đặt trước lần gọi đầu):

- `ECOSORT_AI_ARTIFACTS`: thư mục chứa model và metadata nếu đặt ngoài package.
- `ECOSORT_AI_THRESHOLD`: ngưỡng [0,1], mặc định lấy metadata. Thay ngưỡng phải thống nhất UI và đánh giá lại, không gọi đây là ngưỡng đã kiểm chứng của bản model bàn giao.

Model được lazy-load một lần mỗi process; khóa bảo vệ load và inference. Hàm là đồng bộ: FastAPI async handler nên đưa inference vào threadpool, tránh chặn event loop. Warmup trước readiness bằng ảnh mẫu hợp lệ; đo RAM/latency trong container rồi quyết định số worker vì mỗi process nạp model riêng.

Xử lý lỗi ở lớp API:

| Tình huống | Exception module | HTTP đề xuất |
|---|---|---|
| Bytes rỗng, ảnh lỗi/không hỗ trợ/quá lớn | ValueError | 400 hoặc 413 nếu backend bắt giới hạn upload |
| Thiếu model, checksum sai, mapping/dimension sai | RuntimeError | 503, ghi log nội bộ |
| Lỗi runtime khác khi load/inference | Exception từ runtime | 503/500, ghi log nội bộ |

API cần chặn kích thước upload trước khi đọc toàn bộ vào RAM. Unity gửi multipart field `file` đến `/predict`; module này chỉ cung cấp inference, không triển khai endpoint. Giữ response JSON đúng ba trường trên. Không trả traceback/path nội bộ cho client. Kiểm tra checksum khi triển khai bằng metadata đi kèm, không tự tạo metadata mới cho model khác.

Kiểm thử tích hợp còn cần: JPEG từ Unity, ảnh EXIF portrait, confidence dưới ngưỡng, bytes lỗi, backend restart/warmup, timeout, tải đồng thời, và Unity → API → AI → AR trên Android. Lần chạy hiện tại chỉ xác minh inference trực tiếp trên Windows CPU, chưa chạy Docker hoặc HTTP.
