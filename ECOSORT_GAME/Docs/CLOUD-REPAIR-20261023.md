# Sửa đồng bộ Adafruit IO — 23/10/2026

## Nguyên nhân đã xác nhận

- Feed tài khoản hiện dùng `ecosort.game-score`, `ecosort.player-progress` và các key có tiền tố `ecosort.`.
- Container cũ gọi `game-score`/`player-progress`, nhận HTTP 404 từ Adafruit và trả 503 cho Unity.
- Log 05:54 UTC ghi nhận POST /scores và PUT /progress thất bại; POST /learning/sessions trả 404 vì image cũ.
- /health của image cũ chỉ có Plastic/Metal/Paper, khác metadata bốn lớp trong mã nguồn hiện tại.
- Khối dashboard cũ không còn liên kết feed (`block_feeds` rỗng). Không xác định được ai hoặc thao tác nào trước đó làm thay đổi feed/dashboard.

## Bản sửa

- Backend hỗ trợ `AIO_FEED_PREFIX`; `.env` máy này đặt `ecosort`, Compose truyền biến vào container.
- Giữ tương thích feed nhóm mặc định khi prefix rỗng.
- Dashboard tách đồng hồ level, confidence (0–1, ngưỡng 0.75), lịch sử game/progress và lịch sử AI/AR trong 24 giờ.
- Backup cấu hình dashboard và lượt chơi gốc lưu trong `backend/data/` (được Git bỏ qua).
- Lượt chơi cần khôi phục: session `acc91f234f5a4493b650080773c7b8c9`, level 1, 100 điểm, 10/10 đúng, thời gian gốc 05:54:07 UTC.
- Kiểm thử backend: 11 passed, 1 cảnh báo thư viện TestClient; có ca kiểm tra đường dẫn feed nhóm.

## Kết quả nghiệm thu thực tế

- Container mới đã build và chạy thành công; `/health` trả đủ Plastic/Metal/Paper/Organic và `storage_mode=adafruit_io`.
- Gửi lại bản ghi level 1 qua `/scores`: accepted; `/progress`: level 2, best_score 100; `/learning/sessions`: accepted, created.
- Đọc trực tiếp Adafruit xác nhận `ecosort.game-score` có score 100/correct_count 10, `ecosort.current-level=2`, `ecosort.player-progress` khớp.
- Thời gian ghi cloud là thời gian khôi phục (06:09 UTC); lịch sử học tập giữ thời gian chơi gốc 05:54 UTC.
- AI/AR không được tạo sự kiện giả; ba feed liên quan vẫn trống vì chưa thực hiện scan/AR mới trong lần sửa này.
- Chưa chơi thêm một màn Unity sau sửa; đã kiểm chứng đường backend Docker → cloud bằng chính bản ghi Unity bị lỗi.

## Khi chơi các lần tiếp theo

1. Docker Desktop đang chạy; trong backend chạy `docker compose up -d --build` khi cập nhật source/model.
2. `/health` phải có `storage_mode: adafruit_io` và đủ bốn lớp.
3. Game chọn Kết nối backend → Online, URL `http://127.0.0.1:8000` trên máy này.
4. Chơi campaign, chờ đồng bộ trước khi đổi scene; kiểm tra feed Game Score và Player Progress.

AI/AR chưa có sự kiện sẽ trống; không dùng giá trị mặc định 0 của gauge làm bằng chứng có lần nhận diện. Lịch sử học tập chi tiết lưu tại backend volume, không phải một feed cloud riêng.
