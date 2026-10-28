# Làm mới menu và HUD — 28/10/2026

## Thay đổi

- Chữ xanh đen trên bảng sáng; HUD dùng nền xanh đen gần đục, chữ trắng, chữ phụ xám sáng và điểm nhấn xanh ngọc. Nút chính xanh đậm/chữ trắng, nút phụ nền xanh xám/chữ đậm. Nút và bảng bo góc nhẹ.
- Menu chính có tiêu đề tiếng Việt, một nút hành trình nổi bật và các nhóm lựa chọn gọn hơn. Thu hẹp bảng bên trái, điều chỉnh camera để dành thêm chỗ cho cảnh 3D.
- Tên bối cảnh, điểm, số câu và thời gian tách thành các ô nhỏ. Bảng lời nhắc tự co theo nội dung, giới hạn chiều rộng 43% màn hình, giữ cỡ chữ và tăng chiều cao khi giải thích dài. Bỏ dải nền ngang rộng ở chân màn hình.
- HUD khám phá 3D cũng tự co theo nội dung; hướng dẫn thao tác và nút xử lý tách sang hai phía.
- Hộp tạm dừng giảm từ 40% × 37% xuống 29% × 33% màn hình, thêm nền làm tối phía sau. Bảng cài đặt giảm chiều rộng từ 74% xuống 60%; bảng kết quả và hướng dẫn cũng được thu gọn.

`UiContentFit` cập nhật kích thước khi chữ hoặc chiều rộng vùng chứa thay đổi. Nền HUD không bắt chuột nên không cản thao tác kéo/thả. Sprite bo góc được dựng bằng code, không cần ảnh hoặc font tải ngoài.

## Xem hình

- [Menu chính, 1920 × 1000](Evidence/UI-20261028/ui-main-menu-wide.png)
- [HUD gameplay](Evidence/UI-20261028/ui-gameplay.png)
- [Lời giải thích dài, 1280 × 720](Evidence/UI-20261028/ui-gameplay-long-feedback-720p.png)
- [Tạm dừng](Evidence/UI-20261028/ui-pause.png)
- [Cài đặt](Evidence/UI-20261028/ui-settings.png)
- [Khám phá 3D](Evidence/UI-20261028/ui-exploration.png)

Ảnh giải thích dài là dữ liệu kiểm tra bố cục được chèn trong test, không phải nội dung lời giải của vật phẩm đang hiển thị. Các ảnh khác dùng luồng game thông thường trong PlayMode.

## Bản Windows

Bản giao diện mới nằm tại `Builds/Windows-UI/EcoSortQuest.exe` để tránh ghi đè executable cũ đang mở. Đóng bản cũ rồi chạy executable trong thư mục này. Giữ nguyên cả thư mục cùng các DLL và dữ liệu đi kèm.


## Kết quả kiểm chứng

- EditMode: **59/59 đạt** — [XML](../Logs/UI-EditMode-20261028.xml).
- PlayMode toàn bộ: **22 đạt, 0 lỗi, 2 bỏ qua** do cần backend riêng — [XML](../Logs/UI-PlayMode-20261028.xml).
- Kiểm tra ảnh cuối sau khi chuyển nút Thoát lên thanh điều hướng: **2/2 đạt** — [XML](../Logs/UI-Preview-20261028.xml). Đây là kiểm tra lại, không cộng vào số bài độc lập.
- Đã xem ảnh menu/HUD/hộp thoại ở 1280×720, 1600×900 và 1920×1000. Kiểm tra tự động xác nhận bảng mở rộng khi chữ dài, không cắt nội dung và hộp tạm dừng không vượt vùng đã giới hạn.
- Build Windows thành công: **108,574,005 bytes**, log thoát mã 0 và DLL gameplay đã cập nhật — [log](../Logs/UI-WindowsBuild-20261028.log). Chưa chạy chiến dịch thủ công trong executable mới.
