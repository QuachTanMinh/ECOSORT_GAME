# Phòng thí nghiệm AI và học từ lịch sử

## Cách dùng trên PC

Từ menu chọn **Phân loại bằng ảnh**. Nạp ảnh JPG/PNG bằng đường dẫn tuyệt đối hoặc mở webcam rồi chụp. Sau khi có ảnh, tự chọn một trong 12 nhóm hoặc **Chưa biết / cần kiểm tra**, sau đó gửi AI. Đổi ảnh sẽ yêu cầu tự dự đoán lại.

Chế độ Offline trả nhãn mock để thử giao diện, không phân tích nội dung ảnh. Có nút **Đổi ví dụ** độc lập với lựa chọn của người học để thử tình huống bất đồng. Online dùng cấu hình FastAPI và model hiện có.

Kết quả hiển thị nhãn, độ tin cậy và hướng dẫn đối chiếu. Khi nhãn không hợp lệ hoặc dưới ngưỡng, không kết luận vật liệu; gợi ý chụp một vật rõ, đủ sáng, nền đơn giản và kiểm tra ký hiệu/vật liệu. Độ tin cậy không phải bằng chứng đáp án đúng và không phát hiện được mọi ảnh ngoài dữ liệu huấn luyện.

Nút **Xác nhận kiến thức** gồm câu hiểu giới hạn AI và câu phân loại một vật phẩm có đáp án từ `WasteCatalog`. Câu thứ hai nói rõ đây là vật mẫu trong bài học, không chấm ảnh vừa tải. Người học có thể hoàn thành dù AI không chắc hoặc khác dự đoán. Trả lời sai nhận giải thích và được thử lại. **Luyện 3D mẫu** chỉ mở sau xác nhận, dùng nhãn của vật mẫu bài học. **Thí nghiệm mới** xóa đầu vào, buộc dự đoán lại.

## Thống kê và quy tắc gợi ý

**Kết quả học tập** có ba trang phân tích và trang **Thành tích** để xem điểm, cấp độ và huy hiệu:

- Tiến bộ: tỷ lệ kiểm tra gần nhất từng bối cảnh; chênh lệch so với lượt trước cùng bối cảnh; ba cặp vật liệu thường nhầm.
- Kỹ năng vật liệu: số đúng/tổng đáp án cho từng vật liệu ở Nhà ở, Trường học, Công viên; chênh lệch giữa hai lượt gần nhất có câu về vật liệu đó.
- Danh sách ôn: vật phẩm có lần trả lời gần nhất sai, lỗi mới nhất đứng trước. Trả lời đúng lại xóa lỗi. Hiển thị tối đa năm vật phẩm nhưng toàn bộ danh sách được giữ để luyện.

Chỉ lượt `Test` tính tỷ lệ kiểm tra. Tỷ lệ toàn lượt lấy tổng câu được giao làm mẫu số, gồm cả câu bỏ trống. Tỷ lệ từng vật liệu tính trên các đáp án đã ghi nhận vì lịch sử cũ không lưu vật liệu của câu bỏ trống. Các lượt không có câu về vật liệu đó không tham gia so sánh kỹ năng đó. Chênh lệch mang tính mô tả; các bộ câu khác nhau. Chưa suy ra kỹ năng chuẩn bị hoặc tách vật liệu từ số liệu phân loại.

Gợi ý là quy tắc xác định được, hiển thị lý do và có nút bắt đầu:

1. Còn lỗi chưa sửa: ôn lỗi gần nhất, chọn bối cảnh có vật phẩm đó.
2. Chưa có kiểm tra tại bối cảnh đang chọn: học có hướng dẫn.
3. Kiểm tra gần nhất dưới 70%: học lại có giải thích.
4. Đạt ít nhất 70% và không còn lỗi: thử kiểm tra bối cảnh kế tiếp.

Khi chọn ôn trực tiếp trong bối cảnh không có lỗi phù hợp, game chuyển sang bối cảnh chứa lỗi gần nhất. Không âm thầm mở bộ luyện chung.

## Dữ liệu và kiểm chứng

Lịch sử hiện có giữ nguyên khóa PlayerPrefs phiên bản 1. Bổ sung `knowledge_confirmed` cho quan sát AI; bản ghi cũ mặc định false. Quan sát AI và xác nhận kiến thức lưu cục bộ, tách khỏi `LearningSession` và tỷ lệ kiểm tra. Không lưu ảnh vào lịch sử. Cơ chế gửi các lượt học đang chờ tới backend giữ nguyên; quan sát AI chưa có API đồng bộ riêng.

Các bài kiểm thử EditMode kiểm tra mẫu số có câu bỏ trống, không trộn luyện có gợi ý, so sánh cùng bối cảnh, ưu tiên lỗi gần nhất và chuyển đúng bối cảnh. PlayMode kiểm tra chọn trước khi dự đoán, không dùng sự đồng thuận AI làm điều kiện qua bài, câu xác nhận, đặt lại đầu vào và các trang thống kê. Kết quả mock không chứng minh AI thật, webcam vật lý hoặc AR trên thiết bị hoạt động.

## Hoàn thiện gameplay và đồ họa sau bước 6

- Viền vàng quanh thùng dưới con trỏ lúc kéo; áp dụng cho cả thùng đúng lẫn sai. Cùng phép dò vật lý dùng cho xem trước và thả thật; tạm dừng hoặc hủy kéo sẽ tắt viền. Trong góc nhìn trực giao, đưa vật đang kéo ra trước thùng để không bị che khuất, giữ nguyên vị trí dưới con trỏ và kích thước hiển thị.
- Thanh tiến độ theo số câu đã trả lời, không tiết lộ đáp án trong chế độ kiểm tra. Đồng hồ đổi màu khi còn không quá 15 giây.
- Phép dò vùng thả dùng bộ đệm tái sử dụng; trường hợp quá nhiều collider có đường dự phòng đầy đủ.
- Các hình khối runtime cùng màu dùng chung material, bật khả năng instancing; giải phóng palette khi rời cảnh. Công cụ tạo scene cũng xử lý đúng material được chia sẻ. Đây là thay đổi cấu trúc tài nguyên, chưa có phép đo FPS trước/sau để định lượng tăng tốc.
- Thống kê có thẻ bối cảnh, thanh tỷ lệ và thẻ nhầm lẫn; tên vật liệu tiếng Việt ngắn giúp các ô kỹ năng dễ đọc.

`EcoSort/Build Windows (current scenes)` biên dịch từ các scene đã lưu, tránh tạo lại scene/prefab khi chỉ cập nhật code. `EcoSort/Build Windows` vẫn có hành vi tạo lại dữ liệu authoring như trước.
