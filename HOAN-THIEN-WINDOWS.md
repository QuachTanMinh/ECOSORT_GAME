# Hoàn thiện bản Windows / Unity Editor

Cập nhật sau nâng cấp gameplay: có 18 vật phẩm, sổ tay, băng chuyền chạy và UI cấu hình backend. 20 EditMode + 8 PlayMode và fixture đồ họa 1/1 đã pass; Windows x64 đã build lại thành công ngày 13/10/2026. Xem README và HUONG-DAN-CHAY-PROJECT.md ở thư mục gốc để clone/chạy. Các ghi chú "cần chạy lại" phía dưới là lịch sử trước lần nghiệm thu này.

Phạm vi được người dùng chọn ngày 13/10/2026: Windows/Unity Editor trước.
Người dùng xác nhận đã hoàn tất giai đoạn 5 Adafruit IO. Đây là xác nhận của người dùng; lượt kiểm tra này không gửi thêm dữ liệu lên tài khoản cloud.

## Cấu hình hiện tại

- Unity 6000.6.2f1; mở `ECOSORT_GAME`.
- `Assets/_EcoSortQuest/ScriptableObjects/ApiSettings.asset` đang ở Rest, URL `http://127.0.0.1:8000`, confidence 0.75.
- Backend dùng model thật; bí mật Adafruit chỉ nằm ở cấu hình backend.
- Thử thách là mô phỏng 3D trên PC, chưa phải AR trên thiết bị.

## Nghiệm thu luồng thật

1. Chạy backend bằng môi trường hiện có; không chép đè `.env` đã cấu hình.
2. Mở `/health`, kiểm tra model sẵn sàng và storage mode phù hợp cấu hình Adafruit.
3. Mở MainMenu, Play; vào Tiến trình và bấm Kiểm tra API.
4. Chơi đủ ba level; thử đúng/sai, pause và hết giờ. Đợi gửi kết quả hoàn tất trước khi chuyển màn.
5. Mở Scan; nạp JPG/PNG hoặc chụp webcam. Gửi ảnh đến AI và kiểm tra nhãn/confidence; ảnh bị từ chối không được mở thử thách.
6. Mở thử thách mô phỏng, đặt thùng, chọn sai rồi đúng. Đúng nhận 20 điểm một lần; đợi gửi hoàn tất.
7. Mở Tiến trình, đồng bộ rồi tải lại; đối chiếu Player ID và các giá trị với feed thực tế.
8. Thoát Play rồi mở lại để kiểm tra dữ liệu cục bộ. Thử backend tắt để kiểm tra báo lỗi và giữ tiến trình.
9. Kiểm tra UI ở 1280×720 và 1920×1080, Console không có lỗi runtime.
10. Sau khi nghiệm thu Editor, build lại Windows bằng `EcoSortQuest.Editor.ProjectSetup.BuildWindows` hoặc Build Settings. Kiểm tra lại bản standalone; khi nộp gửi toàn bộ thư mục `Builds/Windows`.

## Điều chỉnh ngày 13/10

Client chặn chuyển scene trong lúc gửi điểm/kết quả thử thách hoặc thao tác API, tránh hủy coroutine khi người chơi bấm chuyển màn quá nhanh. Khi lỗi/timeout kết thúc, người chơi có thể chuyển màn và thử đồng bộ tiến trình tại trang Tiến trình. Chưa có hàng đợi gửi lại lịch sử score/AR.

## Minh chứng cần lưu

## Thao tác kéo thả

Giữ chuột trái trên rác, kéo tới thùng rồi thả chuột. Đúng/sai được chấm lúc thả vào thùng. Thả ra ngoài hoặc lên UI trả rác về vị trí ban đầu, không tính câu. Pause hủy thao tác kéo. Áp dụng cho GameScene và thử thách mô phỏng. Test PlayMode đã được cập nhật sang giữ/kéo/thả và bổ sung kiểm tra click/thả ngoài không chấm điểm; cần chạy lại trong Unity để nghiệm thu phiên bản này.

Ảnh hoặc video: health, gameplay, kết quả AI thật, hoàn thành thử thách mô phỏng, progress đọc lại và feed Adafruit. Bản Windows cũ cần build lại sau thay đổi code. Kết quả test ngày 12/10 là bằng chứng của phiên bản cũ, không tự động xác minh thay đổi mới.

