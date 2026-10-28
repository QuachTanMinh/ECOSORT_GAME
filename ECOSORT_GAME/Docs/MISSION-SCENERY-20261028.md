# Cảnh quan nhiệm vụ và bản demo môn học — 28/10/2026

Bản demo dành cho hoạt động học phân loại rác của trẻ em trên Windows. Vào **Hành trình 3 khu vực**, chọn nhiệm vụ đã mở, nhận hướng dẫn rồi khám phá. Các nhiệm vụ vẫn mở tuần tự và giữ tiến trình cũ.

| Khu vực | Cảnh riêng |
|---|---|
| Nhà ở | Góc bếp có tủ, chậu rửa, bếp nấu, tủ lạnh và bàn ăn; góc học tập trong phòng ngủ có giường, bàn học, tủ đồ và kệ sách; phòng thu gom có máy giặt, chậu rửa và kệ chứa. |
| Trường học | Lớp học có bàn ghế, bảng và bục giảng; căng tin có quầy phục vụ và bàn ăn; sân trường có ba dãy lớp học hai tầng, cây xanh, ghế, cột cờ và sân hoạt động. |
| Công viên | Lối đi có ghế, đèn và hồ nền; bãi dã ngoại có bàn nghỉ, thảm và mái che; vườn cộng đồng có luống rau, nhà kho và nơi ủ phân. |

Sàn, tường và đồ nội thất có va chạm. Tường phía camera có thể ẩn phần hiển thị để quan sát nhân vật nhưng giữ va chạm. Cảnh trong nhà có thùng nhỏ hơn và phần thưởng trang trí phù hợp phòng. Vị trí nhân vật, rác và thùng dùng chung một khai báo bố cục trong `AreaSceneryLayout`; camera có giới hạn điểm theo dõi trong khu vực.

## Kiểm chứng

- EditMode: **59 đạt, 0 lỗi**.
- PlayMode: **24 đạt, 0 lỗi, 2 bỏ qua**. Hai bài bỏ qua cần dịch vụ riêng; không tính là đã đạt.
- Python: **14 đạt, 0 lỗi**; chạy qua TestClient và mô hình hiện có. Kiểm thử kênh Adafruit dùng giả lập vận chuyển, không phải nghiệm thu cloud thật.
- Kiểm tra cảnh duyệt cả chín nhiệm vụ, đối chiếu điểm xuất hiện nhân vật, tìm đường trên lưới theo kích thước va chạm nhân vật và kiểm tra vùng đứng tương tác tại từng vật phẩm, từng thùng. Luồng hành trình tự động hoàn thành cả chín nhiệm vụ và nạp lại tiến trình.
- Build Windows thành công, thoát mã 0; dung lượng do Unity báo **108.600.421 bytes**. DLL `EcoSortQuest.Runtime.dll` được cập nhật sau lần sửa cuối. Tóm tắt và mã SHA-256 trong [verification.json](Evidence/Scenery-20261028/verification.json).

Ảnh [góc bếp](Evidence/Scenery-20261028/scenery-home-kitchen-play.png), [phòng ngủ](Evidence/Scenery-20261028/scenery-home-study-play.png), [lớp học](Evidence/Scenery-20261028/scenery-school-class-play.png), [sân trường](Evidence/Scenery-20261028/scenery-school-yard-overview.png), [dã ngoại](Evidence/Scenery-20261028/scenery-park-picnic-play.png), [vườn cộng đồng](Evidence/Scenery-20261028/scenery-park-restore-play.png) được chụp từ PlayMode. Thư mục có đủ ảnh chơi và góc tổng quan của cả chín địa điểm.

## Bàn giao

- Bản chạy trên máy: `Builds/Windows-UI/EcoSortQuest.exe`; khi chép sang máy khác phải chép cả thư mục build. Build bị loại khỏi Git để tránh đưa tệp biên dịch vào mã nguồn.
- [Báo cáo môn học hoàn chỉnh](../../BaoCao/Bao-cao-do-an-EcoSort-Quest-hoan-chinh-20261028.docx): 23 trang, sáu chương; giữ thông tin nhóm, giảng viên và hình thức trang bìa của bản gốc. Mục lục đã được cập nhật bằng Word, toàn bộ trang đã được xuất và kiểm tra hình thức.

Kiểm tra tự động và biên dịch chưa thay thế việc chơi thử executable trên máy mang đi báo cáo. Phiên bản này chưa nghiệm thu lại Docker, cloud thật, webcam vật lý hoặc AR trên điện thoại, chưa đo FPS trước/sau và chưa thực nghiệm giáo dục với trẻ em. Phạm vi kết luận là bản demo chức năng của đồ án.
