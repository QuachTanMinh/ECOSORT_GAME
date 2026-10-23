# Nhiệm vụ phục hồi khu vực — 23/10/2026

## Cách vào chơi

Menu → PHỤC HỒI CÔNG VIÊN → Nhận nhiệm vụ.

Để đổi bối cảnh: Menu → Học tập / AI / Thống kê → chọn Nhà ở, Trường học hoặc Công viên → Khám phá 3D → Nhận nhiệm vụ.

Vòng chơi: nhận mục tiêu → đi tìm và nhặt rác → quan sát tình trạng → chuẩn bị / tách vật liệu nếu cần → bỏ vào thùng → sửa sai → phục hồi cảnh quan.

- WASD / phím mũi tên: đi; Shift: chạy; chuột phải: xoay camera.
- E: nhặt hoặc bỏ vật đang cầm vào thùng gần nhất; Q: đặt xuống.
- R hoặc nút Quan sát / xử lý: mở tình trạng và chọn cách chuẩn bị.
- ESC đóng bảng xử lý trước; trong lúc chơi dùng để tạm dừng.

## Ba mốc phục hồi

1. Dọn 4 vật phẩm: hồi sinh dải cây xanh.
2. Dọn 8 vật phẩm: vườn hoa xuất hiện.
3. Dọn đủ 12 vật phẩm: thêm chỗ ngồi và biển khu vực đã phục hồi.

Nhấn Ngắm thành quả ở cuối nhiệm vụ để đi lại trong khu vực; Xem kết quả để mở lại bảng tổng kết. Chơi lại bắt đầu một lượt dọn mới, giữ kỷ lục sao đã đạt.

## Quyết định xử lý rác

Tình trạng được mô tả riêng cho nhiệm vụ, không suy ra chỉ từ nhãn AI hoặc hình học vật phẩm:

- Chai / lon / hộp được mô tả còn nước hoặc phần dư: làm rỗng trước khi bỏ thùng.
- Hộp giấy được mô tả sạch, khô và cồng kềnh: làm phẳng, giữ khô.
- Vật phẩm đã tách riêng và sẵn sàng: không cần thao tác thêm.
- Một cốc nhựa được đặt trong túi giấy sạch: tách hai phần trước, làm rỗng cốc, rồi thu gom riêng cả cốc và túi giấy. Túi giấy đã tách xuất hiện cạnh chân người chơi và vẫn thuộc 12 vật phẩm cần dọn.

Đây là các tình huống giáo dục giới hạn trong game; cách tiếp nhận rác thực tế phụ thuộc đơn vị thu gom. Không mô phỏng pin, hóa chất hoặc các thao tác xử lý chất nguy hại.

## Chấm điểm và sửa sai

- Chỉ lần chọn thùng đầu tiên của mỗi vật phẩm mới tính điểm và ghi đáp án: đúng +10, sai −5, điểm không âm.
- Sai thùng: vật phẩm vẫn trên tay, có giải thích, được thử lại; sửa đúng giúp dọn khu vực nhưng không cộng thêm điểm cho cùng vật phẩm.
- Chọn cách chuẩn bị sai: được giải thích và thử lại, không tính là đáp án phân loại và không cộng điểm.
- Tách hai thành phần không tự cộng số rác đã dọn; mỗi phần phải được bỏ đúng thùng riêng.
- 1 sao: dọn đủ 12; 2 sao: dọn đủ và đúng lần đầu ít nhất 9; 3 sao: dọn đủ, đúng lần đầu cả 12 và không chọn sai cách chuẩn bị.

## Lưu dữ liệu

Kỷ lục sao và số lần hoàn thành lưu riêng theo player / bối cảnh trong PlayerPrefs. Không thay đổi khóa mở campaign hoặc thành tích AR.

Lịch sử đáp án vẫn dùng mode `Explore`, hợp đồng `/learning/sessions` hiện có và hàng đợi gửi lại của Unity. Chi tiết nằm trong volume backend; nhiệm vụ này không bổ sung feed điểm hoặc feed sao Adafruit. Lượt đang chơi chưa có cơ chế tiếp tục giữa chừng khi thoát scene; kỷ lục chỉ được cấp lúc dọn đủ.
