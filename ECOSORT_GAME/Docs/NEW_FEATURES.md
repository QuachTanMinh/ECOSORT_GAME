# Tên người chơi, hành trình khu vực và bảng kết quả

Cập nhật ngày 12/11/2026. Các chức năng này bổ sung cho bản demo môn học; chín nhiệm vụ Nhà ở, Trường học và Công viên cùng ba địa điểm thực hành nay nằm trong một hành trình.

## Nhập tên và mở môi trường

1. Mở `Assets/_EcoSortQuest/Scenes/MainMenu.unity`, nhấn Play. Lần đầu, nhập tên gọi rồi chọn **Lưu tên gọi**, hoặc chọn **Chơi khách**. Tên dài tối đa 32 ký tự, hỗ trợ tiếng Việt, chữ số, dấu cách, gạch ngang và gạch dưới.
2. Chọn **Đổi tên** trong menu để thay tên. Tên gọi lưu trên máy; mã người chơi và lịch sử cũ được giữ nguyên. Đây không phải tài khoản đăng nhập.
3. Chọn **Hành trình dọn dẹp các khu vực**. Trang 1/2 có chín nhiệm vụ theo tuyến ở Nhà ở, Trường học và Công viên. Chọn **Địa điểm mới →** để sang trang 2/2, gồm **Bệnh viện**, **Siêu thị** và **Bãi biển**.
4. Mỗi địa điểm mới có ba nhiệm vụ mở theo thứ tự: 6, 10 rồi 18 vật phẩm. Di chuyển và nhặt vật phẩm theo hướng dẫn trong game; quan sát tình trạng, chuẩn bị hoặc tách vật liệu khi cần, rồi đưa vào một trong bốn thùng. Nhiệm vụ cuối tối đa 180 điểm.

Khu tiếp đón bệnh viện chỉ có rác sinh hoạt sạch trong bộ vật phẩm hiện tại. Game không hướng dẫn trẻ thu gom kim tiêm hay rác y tế. Ba môi trường dùng lại kiến thức bốn nhóm Nhựa, Kim loại, Giấy và Hữu cơ; không bổ sung lớp nhận diện AI. Thành tích phục hồi của từng môi trường lưu riêng và không mở khóa hành trình chính.

## Chơi lại từ đầu

Nút **Chơi lại từ đầu** ở cuối menu chính mở một hộp xác nhận. Khi đồng ý, game xóa trên máy: điểm chiến dịch, sao, huy hiệu, sổ tay, lịch sử học và tiến trình của tất cả nhiệm vụ; sau đó quay lại menu để chơi từ đầu. Tên gọi và thiết lập hiển thị/kết nối vẫn giữ. Dữ liệu điểm đã gửi lên bảng kết quả trực tuyến không bị xóa bởi nút này.

`PlayerManager`, `PlayerNamePrompt` và `MainMenuButtons` đã được gắn vào MainMenu. Giao diện dùng Canvas sẵn có, không tạo thêm Canvas chồng nhau. Không cần prefab UI riêng cho bản demo này. Xem [hướng dẫn hành trình](AREA-JOURNEY.md) và [biên bản kiểm chứng ngày 12/11](JOURNEY-VALIDATION-20261112.md).

## Xem bảng kết quả

Khởi động dịch vụ, sau đó mở [Bảng kết quả trên máy này](http://127.0.0.1:8000/dashboard). Tệp `ECOSORT_GAME/Dashboard.html` cũng dẫn tới địa chỉ này. Trong game, nút **Bảng kết quả** ở menu chính mở trang đã lọc theo mã người chơi hiện tại.

- Muốn gửi điểm thật: vào phần thiết lập kết nối của game, chọn chế độ kết nối và đúng địa chỉ dịch vụ trước khi chơi. Chế độ chơi trên máy không gửi dữ liệu lên Adafruit.
- Dịch vụ dùng cấu hình `backend/.env` hiện có. Khóa Adafruit chỉ đặt tại đây; không điền vào HTML, Inspector hay mã nguồn, không đưa tệp này lên GitHub.
- Nguồn dữ liệu điểm là feed `game-score`, có thêm tiền tố nhóm nếu `AIO_FEED_PREFIX` được cấu hình. Không dùng feed tự đặt `player_score`.
- Trang tự cập nhật mỗi 5 giây, hiển thị điểm, số câu đúng, tên gọi và môi trường. Tên có thể trùng; dùng mã người chơi để lọc chính xác.
- Bảng đọc tối đa 100 bản ghi mới nhất của nguồn rồi mới lọc người chơi. Vì vậy đây là cửa sổ dữ liệu gần đây, không phải thống kê toàn bộ lịch sử. Biểu đồ điểm cũng không chứng minh hiệu quả giáo dục.
- Trên máy khác, mở địa chỉ dịch vụ đang chạy kèm `/dashboard`. Cần cập nhật cả Unity và dịch vụ để có các trường tên/môi trường mới.

## Gửi điểm mẫu có chủ đích

1. Mở `Assets/_EcoSortQuest/Scenes/DashboardDebug.unity`. Đây là scene kiểm tra riêng, không phải màn chơi của trẻ.
2. Chọn đối tượng **Manual dashboard test** và kiểm tra `Backend Url`, mặc định `http://127.0.0.1:8000`.
3. Nhấn Play. Scene chưa gửi gì ở bước này.
4. Mở menu của thành phần `AdafruitTestSender` trong Inspector, chọn **Gửi điểm kiểm tra qua backend**.
5. Mở `/dashboard?player_id=debug-dashboard-20261101`. Điểm mặc định là 40, tên **Kiểm thử Dashboard**, số câu đúng là 4. Đây là dữ liệu mẫu để kiểm tra đường truyền, không phải kết quả học thật.

Không cần nhập khóa Adafruit vào thành phần này. Khi lỗi kết nối, xem trạng thái trong Console và kiểm tra dịch vụ đang chạy; điểm không được tự gửi lại ngầm.

## Kiểm chứng bản cập nhật

Xem [biên bản kiểm chứng ngày 01/11](MENU-VALIDATION-20261101.md) và ảnh trong `Docs/Evidence/Menu-20261101`. Kiểm thử tự động bao gồm nhập/đổi tên, dùng chung Canvas, ba cảnh có vật phẩm không chồng nội thất, hoàn thành lượt chơi và đọc lại điểm qua dịch vụ. Việc kiểm thử chơi tự động không thay thế buổi quan sát trẻ em sử dụng game.
