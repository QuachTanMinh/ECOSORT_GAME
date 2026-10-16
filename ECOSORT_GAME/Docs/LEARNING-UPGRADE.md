# Nâng cấp học tập ngày 15 tháng 10 năm 2026

**Cài đặt mới:** icon bánh răng ở Header mở menu riêng gồm Âm thanh, Hình ảnh, Lối chơi. Các mục âm lượng/tốc độ đã được gom vào đây; nút Cài đặt ở hub là lối vào cùng menu. Mở khi đang chơi sẽ dừng timer/băng chuyền, đóng hoặc ESC khôi phục trạng thái trước đó. Kéo rác có tiếng nhấc, thả vào thùng có tiếng thả và phản hồi đúng/sai riêng (kiểm tra chỉ có âm thao tác).

**Cập nhật bối cảnh và âm thanh:** mặt sàn và camera rộng hơn, sân/nhà/trường/công viên có đường đi, vườn cây, ghế, cửa sổ và hồ nước. Có nhạc nền nhẹ cùng hiệu ứng Kenney CC0. Vào Âm thanh / tốc độ → Nghe thử để bật âm game và bỏ mute của Unity Editor. Xem [nguồn âm và chẩn đoán](AUDIO-CREDITS.md).

MainMenu → **Học tập / AI / Thống kê**. Chiến dịch ba level vẫn mở bằng nút Level. Học/kiểm tra/ôn không mở khóa hay cộng điểm cao nhất của chiến dịch.

## Chế độ và bối cảnh

- **Học có hướng dẫn**: 10 câu, không giới hạn giờ, gợi ý vật liệu và giải thích ngay, âm đúng/sai.
- **Kiểm tra**: 10 câu, 75 giây thao tác; ẩn tên chứa chất liệu, gợi ý, điểm và đúng/sai trong lượt. Xem đáp án ở cuối. Timer dừng khi pause và chuyển câu.
- **Ôn câu sai**: lấy vật phẩm sai chưa được giải quyết. Lần trả lời đúng sau đó gỡ vật phẩm khỏi danh sách. Ít hơn 10 vật phẩm thì lặp để đủ lượt; không coi đây là kiểm tra độc lập.

**Nhà ở / Trường học / Công viên** có trang trí 3D và pool vật phẩm khác nhau (12 mẫu/bối cảnh, 4 mỗi nhóm; lượt 10 câu không lặp trước khi hết pool). Là ba môi trường quanh khu phân loại chung, chưa phải bản đồ di chuyển tự do. Vật phẩm sạch, vật liệu chính; thu gom thực tế tùy địa phương.

## Thống kê

Lịch sử theo player_id tại PlayerPrefs key `EcoSortQuest.Learning.v1.<id>`. Lưu chế độ, bối cảnh, timestamp UTC, tổng câu, điểm, hết giờ và từng đáp án (item_id, expected, choice, correct, seconds). Thời gian trả lời loại pause/phản hồi. Về menu giữa lượt không lưu như lượt hoàn thành.

UI tách kiểm tra khỏi học có gợi ý. Tỷ lệ nhóm tính câu đã trả lời; so sánh lượt đầu/gần nhất tính cả câu chưa trả lời vào tổng. Không suy ra hiệu quả giáo dục chỉ từ điểm. Hiển thị 4 lượt gần nhất; giữ tối đa 100 lượt đã gửi trên client, không tự xóa lượt chưa gửi. Máy mới chưa tự tải lịch sử backend vào UI.

## Nhiệm vụ AI

Mở ngay từ Học tập, không cần vượt Level 3. Ba mục tiêu: Nhựa → Kim loại → Giấy. Tìm vật thật → nạp/chụp ảnh → tự chọn vật liệu → gửi ảnh → đối chiếu model. Qua bước khi ảnh được chấp nhận và nhãn AI/lựa chọn đều khớp mục tiêu. Bước tiếp theo xóa ảnh và kết quả để yêu cầu đầu vào mới với backend thật.

Mock ghi riêng, không cộng số lần AI thật. “Đồng thuận” không phải nhãn chuẩn/accuracy; cần kiểm tra vật liệu thực. Accepted=false hoặc sai mục tiêu không qua bước. Không cộng điểm/huy hiệu AR. Kết quả nhiệm vụ lưu client, không lưu ảnh gốc. `/predict` vẫn ghi feed hiện có khi bật Adafruit. AR mô phỏng cũ giữ từ Real-World Challenge.

## Trải nghiệm

Học tập → Âm thanh / tốc độ: bật/tắt âm, tốc độ luyện tập 0,6× / 1,0× / 1,4×. Kiểm tra giữ tốc độ chuẩn để so sánh. Cấu hình lưu trên máy. Âm tổng hợp runtime, không cần asset ngoài; cần nghe thực tế trên loa để nghiệm thu. Cách chơi, pause, hủy kéo khi mất focus vẫn có.

## API mới và đồng bộ

- `POST /learning/sessions`: JSON LearningSession; trả 201 `{accepted, created}`. Gửi lại cùng session_id/nội dung không tạo trùng; cùng ID khác nội dung trả 409.
- `GET /learning/{player_id}`: 100 lượt gần nhất và thống kê kiểm tra trên toàn lịch sử backend.

Lịch sử chi tiết nằm trong **learning.json trên backend local/volume**, kể cả khi progress dùng Adafruit. Không cần thêm feed. Backend kiểm tra vật liệu theo item_id, choice/correct, tính lại điểm theo thứ tự và kiểm tra tổng câu/hết giờ. Chưa có đăng nhập/chống gian lận; JSON dùng một instance, không nhiều worker.

Online tự gửi lượt hoàn thành. Lỗi giữ outbox, vào Kết quả học tập → Gửi các lượt còn chờ để thử lại. ID ổn định tránh trùng khi server đã lưu nhưng mất response. Outbox này chỉ áp dụng LearningSession mới, không áp dụng điểm/AR cũ. Cập nhật backend trước client; backend cũ trả 404, lượt vẫn lưu local.

## Nghiệm thu

1. Học ở ba bối cảnh, thử đúng/sai, pause và thả ngoài.
2. Kiểm tra không lộ gợi ý; hết giờ lưu đúng các câu đã trả lời.
3. Xem đáp án và ôn câu sai; không mở level chiến dịch.
4. Chạy backend mới, POST/GET lịch sử, gửi lại cùng ID không tạo trùng.
5. Nhiệm vụ AI dùng ảnh ngoài training; thử ảnh sai mục tiêu, accepted=false, mất mạng. Mock không thay minh chứng thật.
6. Nghe âm và kiểm tra giao diện trên màn hình người dùng.

Hướng dẫn thao tác tự hiện lần đầu ở menu; có thể mở lại bằng Cách chơi. Phản hồi có đổi màu chữ và hiệu ứng nhấn thùng khi học; chế độ kiểm tra không phát âm/hiệu ứng tiết lộ đúng sai. Thống kê chỉ bắt đầu từ các lượt sau nâng cấp, không suy ra từng đáp án từ điểm tổng của bản cũ.

Kết quả đợt này ở TestResults/Learning-*-20261015.xml. Báo cáo Word 13/10 giữ như mốc cũ, cần bổ sung sau khi nghiệm thu bản mới. Ca REST integration yêu cầu biến môi trường ECOSORT_LEARNING_TEST_URL trỏ tới backend riêng trên 127.0.0.1; nếu không cấu hình, Test Runner bỏ qua riêng ca này để không gọi nhầm backend/cloud người dùng.

Xác minh cuối: **23/23 EditMode, 12/12 PlayMode, 7/7 backend đạt; build Windows thành công**. Ca REST dùng backend local riêng và đã dừng sau kiểm tra. Webcam, ảnh mới, âm thanh loa và Docker/cloud vẫn cần nghiệm thu trực tiếp.
