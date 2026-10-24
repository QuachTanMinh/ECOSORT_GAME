# Hành trình ba khu vực — 24/10/2026

57/57 EditMode, 20/20 PlayMode, không bỏ qua; gồm luồng chơi đủ chín nhiệm vụ và các ca REST hiện có trên backend local riêng. [Thiết kế, cách chơi, phạm vi lưu và bằng chứng](AREA-JOURNEY.md).

# Kiểm chứng mới nhất — 24/10/2026

Bản nhận diện 12 lớp: backend 14/14, EditMode 44/44, PlayMode 19/19, REST UI chạy lại 1/1; Windows build thành công. Đã ghi/đọc Adafruit thật. Docker image mới chưa chạy. Xem [phạm vi và bằng chứng](AI12-INTEGRATION.md). Các mục bên dưới giữ kết quả lịch sử.

# Kiểm thử Unity Client

## Nhiệm vụ phục hồi và chuẩn bị rác — 23/10/2026

- EditMode: **26/26 passed**, `TestResults/Restoration-EditMode-20261023.xml`. Có ca mốc phục hồi, điều kiện xử lý theo vật phẩm, sao chỉ cấp khi dọn đủ và lưu thưởng không lặp / không hạ kỷ lục.
- PlayMode hồi quy: **16 passed, 0 failed, 1 skipped**, `TestResults/Restoration-PlayMode-20261023.xml`. Bài REST yêu cầu backend test riêng nên được bỏ qua; các test dùng mock cô lập, không ghi thành tích vào Adafruit.
- Sau khi đưa nút nhiệm vụ lên menu chính: kiểm thử trọn luồng riêng **1/1 passed**, `TestResults/Restoration-Focused-20261023.xml`. Từ menu → nhận nhiệm vụ → di chuyển / pause → chuẩn bị sai / đúng → sai thùng / sửa lại → tách cốc và túi giấy → thu gom riêng → đủ 12 → lưu 2 sao đúng một lần → ngắm cảnh / mở lại kết quả.
- Các ảnh `restoration-main-menu`, `restoration-briefing`, `restoration-preparation`, `restoration-separated-materials`, `restoration-finished-world` đã được xem để kiểm tra bố cục và chữ.
- Windows x64 build thành công, **108525429 bytes**, `Logs/Restoration-WindowsBuild-20261023.log`, exit code 0. Đã mở bản standalone thật, đóng hướng dẫn ban đầu và bấm nút Phục hồi công viên; bảng nhận nhiệm vụ hiển thị đúng. Ảnh `Evidence/restoration-windows-20261023.png`. Chưa chơi lại toàn bộ 12 vật phẩm bằng tay trong standalone.
- Fixture cũ phụ thuộc `SampleScene` đã bị bỏ khỏi build profile. Đã thay bằng scene trống có tên duy nhất và khôi phục PlayerPrefs trước thao tác dọn scene, kể cả khi test bị bỏ qua. Lượt lỗi đầu để lại player thử; đã khôi phục player thật `demo-fb98729b88e9` từ snapshot local có sẵn (ngày 13/10), giữ nguyên lịch sử học tập; bản sao phục hồi ở `backend/data/player-local-recovery-20261023.json`. Không đưa thành tích AR mô phỏng trong snapshot này lên cloud.
- AR/mobile, camera mới và chất lượng model AI không nằm trong phạm vi thay đổi này. Kỷ lục phục hồi lưu trên máy; lịch sử Explore gửi qua API học tập hiện có.

Hướng dẫn: [RESTORATION-GAMEPLAY](RESTORATION-GAMEPLAY.md).

## HUD gọn — 23/10/2026

- Campaign và Khám phá 3D dùng header theo cụm sát mép, HUD nhỏ, thẻ hướng dẫn góc dưới; nền HUD trong nhẹ, panel trang trí không chặn raycast.
- Điểm và thời gian campaign hiển thị một dòng, footer giảm chiều cao trong màn chơi.
- Đã nạp lại script và chạy level 1 trong Unity Editor; quan sát bố cục, nhấn Pause và Tiếp tục thành công. Ảnh: `Evidence/compact-hud-20261023.png`.
- Chưa chạy lại bộ kiểm thử tự động, chưa build Windows mới và chưa kiểm tra đầy đủ mọi tỉ lệ màn hình. Ảnh minh chứng thuộc phiên Editor đang chạy Online; không hoàn thành thêm một lượt để tạo điểm cloud.

## Organic và Khám phá 3D — 15/10/2026

- EditMode: **23 passed, 0 failed**, `TestResults/Organic-EditMode-20261015.xml`.
- PlayMode: **16 passed, 0 failed, 1 skipped**, `TestResults/Organic-PlayMode-20261015.xml`; bỏ qua bài Unity–backend trực tiếp do không cấu hình server test riêng. Các ca đạt kiểm tra di chuyển bàn phím, pause, giới hạn khoảng cách nhặt/bỏ, sửa sai không tính điểm lặp, đặt xuống/nhặt lại, dọn đủ 12 vật và lưu lịch sử Explore. Bộ kéo/thả, âm thanh, cài đặt và chiến dịch cũ cũng đạt.
- Backend: **10 passed**, gồm schema Organic/Explore, checkpoint bốn đầu ra và đối chiếu API với inference trực tiếp trên 10 ảnh Organic test đầu tiên (kể cả ảnh model đoán sai).
- Công cụ AI: **27 passed, 1 skipped**; bỏ qua smoke GPU opt-in. Huấn luyện thực tế chạy CPU; 372 ảnh held-out được đánh giá riêng trong `ai-model/reports-organic/metrics.json`.
- Đã xem ảnh `Evidence/exploration-park-start.png`, `exploration-result.png` và `02-gameplay.png`: nhân vật, bốn thùng, HUD và màn kết quả hiển thị rõ ở 1600×900.
- Windows build thành công: **106173462 bytes**, exit code 0, log `Logs/Organic-Windows-Build.log`; đầu ra `Builds/Windows/`.

Chưa kiểm thử độc lập ảnh điện thoại, Docker/Adafruit mới, thiết bị AR hoặc playtest thủ công bản standalone. Các kết quả cũ bên dưới thuộc lần chạy trước.

## Thanh âm lượng 15/10/2026

Audio-Volume-20261015.xml: **2/2 passed**. Kiểm tra ba slider thay đổi source ngay, tổng 50% × nhạc 40% = 20%, tổng 50% × hiệu ứng 80% = 40%; giữ giá trị sau tải lại scene, clamp về 0–100%, clip/routing/Nghe thử không hồi quy. Ảnh audio-volume-controls.png đã được xem. Test khôi phục mức âm người dùng sau khi chạy.

## Bối cảnh rộng và âm thanh 15/10/2026

Environment-Audio-20261015.xml: **12 passed, 0 failed, 1 ignored** (REST integration không chạy lại vì không cấu hình backend riêng). Kiểm thử bao gồm kéo thả, ba bối cảnh, model/âm thanh được nạp, sample nhạc không rỗng, nguồn 2D, bật/tắt âm. Audio-Preview-20261015.xml: **1/1 passed** sau bổ sung bỏ Editor mute bằng nút Nghe thử. Đã xem ảnh ba bối cảnh mới và audio-settings. Test khôi phục các cấu hình âm bị thay đổi, không tự sửa Windows mixer.

Hiệu ứng Kenney CC0 có license trong Resources/Audio; nhạc nền được tổng hợp runtime. Chưa khẳng định loa/tai nghe của người dùng nghe được chỉ từ kiểm thử tự động. Xem AUDIO-CREDITS.md để thử trực tiếp và kiểm tra thiết bị đầu ra nếu còn im.

Build Windows đợt bối cảnh/âm thanh thành công: 106.141.238 bytes, Environment-Audio-Build.log kết thúc return code 0. Bản build chưa được nghe thử trên loa của người dùng.

## Nâng cấp học tập 15/10/2026

- EditMode: **23/23 đạt**, Learning-EditMode-20261015.xml. Bổ sung danh mục bối cảnh, ôn câu sai theo đáp án gần nhất, thống kê tách kiểm tra và chống trùng outbox.
- PlayMode: **12/12 đạt**, Learning-PlayMode-20261015.xml. Bao gồm 8 ca cũ, 3 ca học/kiểm tra/bối cảnh/nhiệm vụ mock và 1 ca Unity REST → FastAPI local thật ở 127.0.0.1:18015. Gửi lại cùng lượt hai lần, GET trả một lượt và 9 câu chưa trả lời. Backend riêng chỉ dùng local, đã dừng sau kiểm tra.
- Backend: **7/7 đạt** ngày 15/10, pytest gồm model thật, schema lịch sử, idempotency, conflict, phân tách player và lưu qua lần khởi động lại. Có một deprecation warning của TestClient/httpx, không có test thất bại.
- Đã xem các ảnh learning-hub, learning-statistics, learning-test-school, learning-answer-review, learning-context-Home/School/Park và learning-ai-mission-mock trong Evidence. Ảnh nhiệm vụ được ghi rõ mock.
- Windows x64: build thành công, **106.120.554 bytes**, Learning-WindowsBuild.log kết thúc return code 0. Chưa thực hiện playtest standalone trên máy nhận.

Các test không nghiệm thu webcam/loa thực tế, ảnh điện thoại mới, Docker trên máy nhận hay luồng ghi Adafruit mới. Lịch sử chi tiết nằm ở backend local; Dashboard cloud cũ không có thống kê chi tiết mới. Chạy bộ PlayMode trong Editor không có ECOSORT_LEARNING_TEST_URL sẽ bỏ qua riêng ca REST integration. Các kết quả bên dưới là mốc lịch sử trước nâng cấp.

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

## Kiểm chứng menu cài đặt và âm kéo/thả — 15/10/2026

PlayMode: **15 passed, 0 failed, 1 skipped** trong `TestResults/Settings-Sound-20261015.xml`. Bài REST integration bỏ qua vì lần này không cấu hình backend kiểm thử. Các bài đạt gồm chuỗi pickup/drop/correct hoặc retry, kiểm tra không tiết lộ đáp án qua âm trong chế độ kiểm tra, mở ba tab cài đặt và khôi phục trạng thái đang chơi hoặc đang tạm dừng.

Đã xem ba ảnh 1600×900: `Evidence/settings-audio-tab.png`, `Evidence/settings-visual-tab.png`, `Evidence/settings-gameplay-tab.png`. Windows build thành công, 106155590 bytes, exit code 0; log `Logs/Settings-Sound-Build.log`. Chưa kiểm tra nghe trực tiếp qua loa/tai nghe hoặc thao tác toàn màn hình trong bản standalone.

