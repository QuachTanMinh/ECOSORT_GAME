# Kiểm thử Unity Client

## Bản nâng cấp gameplay ngày 13/10/2026

- **EditMode: 20/20 passed**, `TestResults/EditMode-20261013.xml`: luật cũ và danh mục 18 vật phẩm, bộ chọn vật phẩm theo nhóm, vòng lại băng chuyền, collider của tất cả mô hình.
- **PlayMode: 8/8 passed**, `TestResults/PlayMode-20261013.xml`: campaign 30 lần kéo thả đúng, thử thách sai rồi đúng, progress mock đọc lại, click/thả ngoài không chấm điểm, băng chuyền mang rác, pause dừng rác và vạch băng chuyền, timeout, nạp ảnh và lỗi REST thiếu URL.
- Có test menu kết nối: từ chối URL không hợp lệ, chuyển Online/ngoại tuyến và giữ lựa chọn xuyên scene. Test không gửi HTTP ra mạng.
- Test tự cấu hình mock trước Start của mỗi scene, không thay ApiSettings.asset, không gửi lên cloud. Key tiến trình, cấu hình kết nối và trạng thái phiên được lưu/khôi phục.
- Đã mở và kiểm tra ảnh gameplay 1600×900, menu, sổ tay và ba ảnh mô hình `08-catalog-*.png`. Các ảnh danh mục là fixture trưng bày sáu mô hình, game thực tế xuất hiện từng vật phẩm.
- Fixture đồ họa chạy lại riêng **1/1 passed**, `TestResults/Visual-20261013.xml`, sau khi chú thích ảnh danh mục; build Windows x64 thành công, log `Logs/EcoSort-Upgrade-WindowsBuild.log` (log không đưa lên Git). Build không chứng minh đã playtest bản standalone.
- HUD gọn hơn, camera gameplay trực diện, menu/sổ tay rộng hơn. Build Windows mặc định 1600×900 và cho phép resize; vẫn cần playtest thủ công ở các kích thước màn hình của người nhận.

Các mục phía dưới là lịch sử ngày 12/10, không mô tả bản gameplay mới. AR vẫn mô phỏng PC; kiểm thử mock không thay thế nghiệm thu REST/cloud/device.

## Kiểm tra tự động

EditMode: score không âm, đúng/sai, ngưỡng pass 6/7/10, không chấm điểm sau khi kết thúc, timeout, mở khóa level/campaign, merge progress không lùi và từ chối prediction không hợp lệ.

PlayMode: chuột ảo đi qua PointerInput → raycast vật phẩm/thùng, hoàn thành 30 câu của campaign, Scan mock, AR simulation chọn sai rồi đúng, huy hiệu, mock progress đọc lại; pause dừng timer; timeout kết thúc; REST chưa cấu hình trả lỗi.

Kết quả chạy ngày **12/10/2026** bằng Unity **6000.6.2f1** trên Windows:

| Bộ kiểm thử | Kết quả | Minh chứng |
|---|---|---|
| EditMode | **16/16 Passed** | `TestResults/EditMode.xml` |
| PlayMode | **4/4 Passed** | `TestResults/PlayMode.xml` |
| Visual QA | Đã mở và kiểm tra 6 ảnh, 1600×900 | `Evidence/01-main-menu.png` đến `06-progress.png` |

PlayMode sử dụng chuột ảo qua Input System và raycast thực. Bộ test cho phép input khi Game View không có focus trong batch rồi khôi phục setting; không thay cấu hình input của project. Có kiểm tra nạp ảnh PNG, JPEG encoding, đường dẫn sai và đọc lại dữ liệu mock bằng service riêng. Ảnh được render bằng URP SingleCameraRequest để có đầy đủ geometry và UI trong batch.

Build Windows x64 đã hoàn thành với exit code 0. Đầu ra ở `Builds/Windows/`, log `Logs/EcoSort-WindowsBuild.log`. Build thành công không thay thế kiểm thử giao diện tương tác trong bản standalone.

Chưa kiểm thử thủ công webcam/quyền camera, ảnh EXIF, Android/cảm ứng, AR thật hoặc backend/Adafruit IO thật. REST được kiểm tra ở tình huống chưa cấu hình URL; chưa chứng minh response từ server thật. Kết quả XML thuộc lần chạy cụ thể, không thay thế playtest trên máy/thiết bị mục tiêu.

## Checklist thủ công

| ID | Thao tác | Kỳ vọng |
|---|---|---|
| M01 | Mở MainMenu và Play Level 1 | Đủ scene 3D, HUD, ba thùng có nhãn |
| M02 | Chọn đúng/sai, sai khi score=0 | +10/−5, không âm |
| M03 | Click thùng trước vật phẩm | Hướng dẫn chọn vật phẩm, không tính điểm |
| M04 | Click liên tiếp khi chuyển vật phẩm | Chỉ tính một lần |
| M05 | Hoàn thành 6/10 hoặc 7/10 đúng | 6 không qua; 7 qua và mở level |
| M06 | ESC, chờ, ESC lần nữa | Timer và tương tác dừng rồi tiếp tục |
| M07 | Hết giờ Level 2/3 | Hiển thị kết quả, không tính câu chưa trả lời |
| M08 | Chơi lại hoặc về menu khi pause | Lượt mới sạch; timeScale trở về 1 |
| M09 | Thoát Play, Play lại | Điểm cao và level đã mở vẫn còn |
| M10 | Game View 1280×720, 1920×1080, 1024×768 | Nội dung đọc được, nút không chồng; 16:9 là mục tiêu chính |
| M11 | Scan mock từng Plastic/Metal/Paper | Kết quả khớp nhãn fixture, có cảnh báo mock |
| M12 | Webcam mở/chụp, chuyển scene | Có preview, chụp được; camera được giải phóng |
| M13 | Nạp JPG/PNG; đường dẫn sai/file không phải ảnh | Hiển thị ảnh hoặc lỗi rõ ràng, không crash |
| M14 | AR simulation click sàn, chọn sai rồi đúng | Sai không cộng; đúng hoàn thành +20 một lần trong nhiệm vụ |
| M15 | Progress mock upload/download | Đọc lại đúng tiến trình, ghi rõ mock/local |
| M16 | REST URL sai/server không chạy | Có timeout/lỗi; progress cục bộ còn |
| M17 | Backend thật health/predict/score/progress/ar-results | Chạy đúng contract, kiểm tra dữ liệu phía server |
| M18 | Android AR và cảm ứng | Chưa nằm trong lần kiểm thử PC này |

M12/M17/M18 cần webcam/backend/phần cứng tương ứng; không đánh dấu pass khi chưa kiểm tra. Console không được có lỗi runtime trong từng kịch bản. Ghi ngày, thiết bị, resolution, ảnh và Pass/Fail khi chạy thủ công.

