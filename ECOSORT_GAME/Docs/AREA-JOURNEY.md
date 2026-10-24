# Hành trình ba khu vực và phần thưởng — 24/10/2026

## Mở và chơi

Mở bản Windows hoặc MainMenu trong Unity. Chọn **HÀNH TRÌNH 3 KHU VỰC →**. Người chơi mới bắt đầu ở Nhà ở; chọn thẻ khu vực để xem ba nhiệm vụ và điều kiện mở khóa. Các chế độ luyện tập/chiến dịch cũ vẫn có đường vào riêng.

WASD di chuyển, Shift chạy, giữ chuột phải xoay góc nhìn. E nhặt/bỏ thùng, Q đặt xuống, R quan sát và chọn cách chuẩn bị. H hỏi gợi ý cho vật đang cầm. Không có giới hạn thời gian trong cả chín nhiệm vụ; tốc độ không quyết định sao. Tên vật phẩm và thao tác quan sát không bị tính là gợi ý; H khi đang cầm vật sẽ tăng số lần gợi ý.

## Khu vực mẫu: Nhà ở

Ba lượt ngắn, mỗi lượt bốn vật phẩm cố định, đặt gần nhau; người mới có thể quan sát, chuẩn bị và sửa sai thoải mái.

| Nhiệm vụ | Kỹ năng | Phần thưởng |
|---|---|---|
| Sau bữa sáng | Làm rỗng chai/lon, làm phẳng hộp giấy, tách thực phẩm | Chậu cây |
| Góc học tập gọn gàng | Giấy khô/carton, phân biệt giấy nhôm | Hoa |
| Hai vật liệu, hai nơi | Tách túi giấy khỏi cốc, làm rỗng cốc, thu gom từng phần | Ghế và mở Trường học |

## Trường học và Công viên

Trường học gồm **Đội trực nhật — Lớp học**, **Sau giờ ăn — Căng tin**, **Sân trường xanh — Sân trường**. Mỗi nhiệm vụ có câu hỏi tình huống trước khi bắt đầu, đồ vật và bảng khu vực tương ứng. Câu trả lời sai có giải thích và được chọn lại; không thể bỏ qua câu hỏi. Hai lượt đầu bốn vật phẩm, lượt sân trường tám vật phẩm. Đây là các góc hoạt động trong cảnh 3D hiện có, chưa phải ba bản đồ nội thất lớn độc lập.

Công viên gồm **Lối đi an toàn**, **Sau buổi dã ngoại**, **Trả lại góc xanh**, với 8/8/12 vật phẩm. Vị trí tìm kiếm trải rộng theo tuyến cố định, khác sân nhỏ ở Nhà ở; có tách vật liệu, làm rỗng vật chứa và thu gom thực phẩm. Không đổi ngẫu nhiên bộ vật phẩm giữa các lần chơi nhiệm vụ. Các tuyến nằm trong ranh giới đi bộ, tránh luống cây/công trình có va chạm.

## Sao và mở khóa

- 1 sao: dọn đủ vật phẩm, kể cả phải sửa sai.
- 2 sao: đúng lần đầu ít nhất 75%, dùng gợi ý không quá hai lần.
- 3 sao: đúng lần đầu ít nhất 90%, không sai chuẩn bị hoặc câu hỏi, không dùng gợi ý.

Một đáp án sai không ngăn hoàn thành: giữ vật phẩm và chọn lại thùng. Điểm/độ chính xác chỉ ghi lần phân loại đầu. Tách túi/cốc không tự tăng số vật đã dọn. Đúng vật liệu nhưng sai chuẩn bị hoặc câu hỏi vẫn chưa đủ điều kiện ba sao.

Hoàn thành một nhiệm vụ mở nhiệm vụ kế tiếp; xong ba nhiệm vụ Nhà ở mở Trường học, xong Trường học mở Công viên. Không yêu cầu ba sao để đi tiếp. Chơi lại giữ số sao/độ chính xác tốt nhất, không ghi thưởng lặp cho cùng phiên hoàn thành. Thành tích chiến dịch/phục hồi cũ được giữ nguyên; hành trình mới có bộ nhiệm vụ và lưu tiến trình riêng.

## Phần thưởng, huy hiệu và sổ tay

Hoàn thành lần lượt ba nhiệm vụ trong một khu vực mở cây, hoa, ghế. Chúng xuất hiện khi nhận thưởng và được dựng lại khi quay về khu vực; màn **Phần thưởng / huy hiệu** cho bật/tắt từng loại. Đồ trang trí là các khối 3D được dựng bằng code, chưa có bộ mỹ thuật riêng hay cửa hàng.

Huy hiệu **Nhận biết bốn vật liệu** cần phân loại đúng lần đầu đủ bốn loại qua hành trình; **Chăm chỉ ôn tập** cần hoàn thành trọn một lượt Review đúng tất cả; **Tiến bộ từng ngày** cần chơi lại một nhiệm vụ và cải thiện sao hoặc số đáp án đúng; **Người giữ góc xanh** cần hoàn thành nhiệm vụ Công viên cuối.

Sổ tay có 36 chỗ; nhặt vật trong khám phá hoặc gặp vật trên băng chuyền sẽ mở trang tương ứng, lưu ngay cả khi chưa hoàn tất lượt. Các trang chưa gặp giữ trạng thái chưa khám phá. Không suy diễn vật đã gặp trong các bản cũ nếu lịch sử không đủ bằng chứng.

## Lưu và đồng bộ

Tiến trình nhiệm vụ, sao, huy hiệu, trang sổ tay và lựa chọn trang trí nằm trong PlayerPrefs với khóa `EcoSortQuest.AreaJourney.v1.<player_id>`. Lưu theo người chơi trên máy; chưa có đồng bộ hành trình/trang trí giữa thiết bị. Không xóa khóa lưu gốc hay thay schema PlayerProgress hiện tại.

Lịch sử từng đáp án vẫn được gửi qua `/learning/sessions` khi Online; không tạo thành tích AR hoặc ghi thêm feed Adafruit giả. Phần điểm chiến dịch/cloud vẫn đi theo luồng cũ. Không đồng nhất lịch sử đáp án đã đồng bộ với tiến trình nhiệm vụ đã đồng bộ.

## Kiểm chứng

57/57 EditMode và 20/20 PlayMode đã đạt, không bỏ qua. Sau tinh chỉnh UI/góc bắt đầu, ca đủ chín nhiệm vụ chạy lại riêng đạt 1/1. Windows build mới thành công (108555221 byte theo BuildReport); chưa chạy nghiệm thu thủ công executable trong đợt này. Ca PlayMode mới đi qua đủ chín nhiệm vụ: khóa/mở, câu hỏi bắt buộc, nhặt/chuẩn bị/tách/bỏ thùng, tính sao có gợi ý, lưu và tải lại, phần thưởng và trang sổ tay. Các ca REST học tập và model 12 lớp hiện có cũng chạy với backend kiểm thử local riêng. Bộ EditMode kiểm tra biên tính sao, thứ tự mở khóa, chống ghi lặp, giữ kỷ lục, badge cải thiện/ôn tập và lựa chọn trang trí.

Xem `TestResults/Areas-*.xml` và các ảnh `Evidence/areas-*-20261024.png`. Các ảnh này thuộc người chơi kiểm thử, không phải thành tích thật của bạn. Không dùng kết quả trên để tuyên bố đã nghiệm thu AR/mobile, hoặc đã nghiên cứu hiệu quả giáo dục với người dùng thật.
