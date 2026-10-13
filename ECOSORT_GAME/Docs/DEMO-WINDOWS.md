# Kịch bản nghiệm thu EcoSort Quest Windows

## Gameplay và giáo dục

1. MainMenu → Kết nối backend → Chơi ngoại tuyến.
2. Mở Sổ tay 18 vật phẩm; chuyển Nhựa/Kim loại/Giấy, giải thích ví dụ tái sử dụng và làm sạch trước thu gom.
3. Chơi Level 1: quan sát rác và vạch băng chuyền chuyển động; kéo thả đúng/sai. Đọc phản hồi kiến thức. Thả ngoài không tính câu.
4. ESC pause: rác, băng chuyền và timer dừng. Tiếp tục và hoàn thành ít nhất 7/10 câu.
5. Hoàn thành Level 2 và 3; mẫu vật phẩm đa dạng hơn, tốc độ băng chuyền tăng, Level 2/3 có timer 75/50 giây.
6. Xem tiến trình; thoát rồi mở lại để kiểm tra bản lưu cục bộ.

## AI và cloud thật

1. Chạy backend Python hoặc Docker theo hướng dẫn gốc; health báo model sẵn sàng. Cấu hình Adafruit riêng trong `.env`, nạp bằng `--env-file` nếu chạy Python.
2. MainMenu → Kết nối backend → URL FastAPI → Online. Tiến trình → Kiểm tra API.
3. Chơi một lượt, đợi gửi hoàn tất; kiểm tra điểm và progress trên feed thật.
4. Sau khi mở thử thách, nạp/chụp ảnh một vật phẩm, Gửi ảnh đến AI. Quan sát nhãn và confidence; kết quả không được chấp nhận không mở thử thách.
5. Mở thử thách mô phỏng, đặt thùng, kéo thả sai rồi đúng. Quan sát +20 một lần và progress đọc lại.
6. Mở Dashboard EcoSort Quest trên Adafruit, đối chiếu bản ghi và timestamp mới. Nhật ký JSON giữ đầy đủ điểm/nhãn/thử thách; Gauge và Line Chart dùng confidence số.

Thử thách PC là mô phỏng 3D, không chứng minh tracking AR trên điện thoại. Thao tác ở ngoại tuyến dùng mock, không phải AI/cloud thật. Test tự động cũng dùng mock, xem XML và TESTING.md để phân biệt bằng chứng.

Bản Windows cần toàn bộ thư mục `Builds/Windows`, không chỉ exe. Source để clone gồm Assets, Packages, ProjectSettings và backend/model; không gửi Library, .venv hoặc .env.
