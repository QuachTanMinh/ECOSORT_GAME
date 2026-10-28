# Menu theo từng chức năng — 28/10/2026

Menu chính có các lối vào riêng:

- **Hành trình 3 khu vực:** nhiệm vụ khám phá và phục hồi khu vực.
- **Học có hướng dẫn:** chọn bối cảnh, học 10 câu với gợi ý và giải thích.
- **Kiểm tra:** chọn bối cảnh, làm 10 câu trong 75 giây, xem đáp án cuối lượt.
- **Ôn câu sai:** chọn bối cảnh để luyện các lỗi chưa sửa.
- **Phân loại bằng ảnh:** lối vào duy nhất trên menu cho trải nghiệm tự dự đoán, đối chiếu kết quả và xác nhận kiến thức. Không yêu cầu mở cấp độ 3, vì chức năng học bằng ảnh trước đây vốn đã có thể truy cập qua mục học tập.
- **Thử thách phân loại:** chọn một trong ba cấp độ băng chuyền, giữ điều kiện mở khóa và điểm hiện có.
- **Kết quả học tập:** gộp thống kê và tiến trình. Bốn trang: Tiến bộ, Kỹ năng vật liệu, Danh sách ôn, Thành tích. Chỉ có một nút lưu trực tuyến cho cả lịch sử học và tiến trình; khôi phục tiến trình là thao tác riêng.
- **Sổ tay khám phá** và **Cách chơi:** tra cứu vật phẩm và hướng dẫn thao tác.

Không còn nút tổng hợp “Học tập / AI / Thống kê”, hai lối vào trùng nhau của nhận diện ảnh, hay nút Tiến trình tách riêng trên menu. Chọn học, kiểm tra hoặc ôn chỉ mở bảng chọn bối cảnh của đúng hoạt động đó.

## Ngôn ngữ hiển thị

- “Mock” → **Ví dụ có sẵn**, **Đổi ví dụ**, **Xem kết quả ví dụ**. Luôn nói rõ ví dụ không phân tích ảnh của người chơi.
- “Confidence” → **Độ tin cậy**.
- “API / backend / FastAPI” không hiển thị trong nút và thông báo kết nối cho người chơi. Dùng **Kết nối**, **Bật kết nối**, **Lưu kết quả trực tuyến** và lỗi bằng tiếng Việt.
- Cấu hình kết nối nằm tại bánh răng **Cài đặt → Kết nối → Thiết lập kết nối**. Địa chỉ dịch vụ vẫn cần do người cài đặt game cung cấp; không tự bật nhận diện thật khi chưa có kết nối.
- Tên lớp, khóa lưu, dữ liệu lịch sử và đường dẫn giao tiếp nội bộ giữ nguyên để tương thích dữ liệu cũ.

## Ảnh kiểm tra

- [Menu chính](Evidence/Menu-20261028/menu-separated-functions.png)
- [Chọn bối cảnh kiểm tra](Evidence/Menu-20261028/menu-test-context.png)
- [Thành tích trong kết quả học tập](Evidence/Menu-20261028/menu-achievements.png)
- [Thiết lập kết nối](Evidence/Menu-20261028/menu-connection.png)
- [Xem ví dụ phân loại ảnh](Evidence/Menu-20261028/menu-photo-example.png)


## Kiểm chứng và bản chạy

- 59/59 bài kiểm thử logic đạt.
- Bộ tương tác đầy đủ: 23 đạt, 0 lỗi; 2 bài cần dịch vụ riêng được bỏ qua.
- Kiểm tra lại 3 luồng bị ảnh hưởng sau khi gộp nút lưu và thêm nền hộp thoại: 3/3 đạt. Đây là kiểm tra lại, không cộng thành số bài độc lập.
- [Tóm tắt từng bài kiểm thử](Evidence/Menu-20261028/test-results.json).
- Build Windows thành công: 108,576,229 bytes; log có dòng kích thước bản build, thoát mã 0 và DLL gameplay đã cập nhật.
- Chạy `Builds/Windows-UI/EcoSortQuest.exe` sau khi đóng bản game cũ. Thư mục bản chạy và log chi tiết được giữ cục bộ theo quy tắc của dự án; không đưa lên Git.

Đã xem ảnh menu chính, chọn bối cảnh, trang thành tích, kết nối và ví dụ nhận diện. Kiểm thử dùng dữ liệu mô phỏng cho dịch vụ; không coi đó là kiểm chứng nhận diện thật hoặc đồng bộ mạng thật. Chưa chơi chiến dịch thủ công trong executable mới.
