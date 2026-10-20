# Trạng thái triển khai Unity Client

## Cập nhật 13/10/2026 — bản source chuẩn bị GitHub

Đã thay thao tác chọn bằng kéo thả, thêm 18 mẫu rác (6 mỗi nhóm) có mô hình/collider/bài học riêng, sổ tay phân loại, băng chuyền mang vật phẩm chạy vòng lại, độ khó theo level, feedback có thời gian đọc và timer dừng trong feedback. HUD gọn, menu/sổ tay rộng hơn, camera gameplay dễ quan sát. Có màn cấu hình backend trong game; standalone mặc định ngoại tuyến, Editor giữ cấu hình asset trừ khi người dùng chọn chế độ trong phiên.

Xác minh: 20 EditMode + 8 PlayMode passed, đã kiểm tra ảnh đồ họa. Adafruit có dữ liệu thật và Dashboard đã được quan sát ở phiên làm việc trước; không có kiểm thử cloud mới trong lượt nâng cấp gameplay. Các bảng lịch sử dưới đây thuộc phiên bản ban đầu.

Phạm vi ban đầu ưu tiên Windows PC/Unity Editor. Phạm vi mới bổ sung ARKit cho iOS và ARCore cho Android; kiểm thử trên thiết bị thật vẫn đang chờ.

| Phase | Nội dung đã viết | Giới hạn |
|---|---|---|
| 1 | MainMenu, scene 3D, primitive prefab, chọn vật phẩm/thùng, score, vòng 10 vật phẩm | Băng chuyền trang trí, vật phẩm đứng tại vị trí chọn; không mô phỏng vật lý kéo thả |
| 2 | Ba LevelConfig, timer 75/50 giây, đúng/sai, pass 7/10, pause/resume, kết quả, unlock, local save | Giá trị thời gian cần playtest với người dùng |
| 3 | IEcoSortService, mock, REST adapter, webcam/file image, preview, resize JPEG, loading/error, confidence gate | Backend thật chưa có URL; mock không nhận diện hình ảnh |
| 4 | Scene thử thách, prediction → item, tương tác, thưởng 20, badge; đã thêm AR Foundation, plane hit-test và provider ARKit/ARCore | Chưa build/cài lên iPhone hoặc điện thoại Android thật; cần kiểm tra tracking, placement, camera và drag/drop trên thiết bị |
| 5 | Kết nối score/progress/AR service, giao diện progress, setup tool, tests, tài liệu và ảnh kiểm thử | Cloud/AI thật cần thành viên 2–3; không thể nghiệm thu tích hợp thật bằng mock |

Trạng thái kiểm thử thực tế được ghi riêng trong TESTING.md và XML. Việc viết code hoặc sinh scene không tự động chứng minh tính năng đã hoạt động.

Lần xác minh cuối ngày 12/10/2026: **16 EditMode + 4 PlayMode passed**, 6 ảnh minh chứng đã kiểm tra, build Windows x64 thành công. UI/gameplay/mock flow đã được kiểm thử tự động trong Editor. Webcam, REST backend thật, Adafruit IO thật và AR/Android chưa được kiểm thử.

Giữ Unity version và package manifest. Không xóa scene/asset template. Setup chỉ tạo asset còn thiếu. Mã tạo riêng trong namespace và assembly EcoSortQuest.

## Các bước còn phụ thuộc bên ngoài

- Cung cấp FastAPI URL và xác nhận contract để chạy REST integration.
- Cung cấp model thật và ảnh thử nghiệm để đánh giá Scan.
- Triển khai Adafruit IO và xác nhận yêu cầu IO Cloud với giảng viên.
- Hoàn tất acceptance AR bằng build iOS cloud và APK Android, sau đó chạy thử trên iPhone 11 Pro Max và thiết bị Android được ARCore hỗ trợ.
- Kiểm thử cảm ứng và xuất APK khi chuyển sang giai đoạn mobile.
- Playtest UI, webcam và hiệu năng trên máy người phát triển.

