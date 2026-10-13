# Hợp đồng tích hợp Unity và FastAPI

Đây là contract client đề xuất để thành viên backend đối chiếu. Chưa được xác nhận với server thật. Unity dùng `IEcoSortService`, không trực tiếp gọi Adafruit IO.

## Quy ước

- Base URL HTTP(S) không có dấu `/` cuối cũng được. HTTPS cho Internet.
- Request JSON dùng UTF-8, tên field snake_case. Response lỗi trả HTTP 4xx/5xx.
- `player_id` là mã demo cục bộ, không phải token xác thực.
- Mặc định timeout 15 giây, ảnh upload ≤4 MB.
- Các POST/PUT thành công chấp nhận HTTP 2xx, body không bắt buộc. GET progress cần JSON đúng schema.
- Ngưỡng confidence 0.75 là cấu hình tạm thời cần hiệu chỉnh validation.

| Method | Path | Request | Response thành công |
|---|---|---|---|
| GET | `/health` | Không body | 2xx, text/JSON trạng thái |
| POST | `/predict` | multipart/form-data, field **file**, filename capture.jpg, image/jpeg | Prediction JSON |
| GET | `/progress/{player_id}` | Không body | PlayerProgress JSON |
| PUT | `/progress/{player_id}` | PlayerProgress JSON | 2xx |
| POST | `/scores` | ScoreRequest JSON | 2xx |
| POST | `/ar-results` | ArResultRequest JSON | 2xx |

Prediction:

```json
{"class_name":"Plastic","confidence":0.92,"accepted":true}
```

Nhãn hợp lệ: Plastic, Metal, Paper (client chấp nhận không phân biệt hoa thường). confidence phải hữu hạn, trong [0,1], đạt ngưỡng cấu hình; accepted=false luôn từ chối. Ảnh lạ không được giả định sẽ bị phát hiện tin cậy chỉ bằng confidence.

PlayerProgress:

```json
{
  "player_id":"demo-example",
  "unlocked_level":3,
  "best_score":100,
  "ar_completed":1,
  "campaign_completed":true,
  "updated_at":"2026-10-12T00:00:00.0000000Z"
}
```

`campaign_completed` cần thiết để phân biệt đã mở Level 3 với đã vượt Level 3. `ar_completed` là tổng số thử thách hoàn thành; trong bản PC hiện tại là thử thách mô phỏng. Hai field này là bổ sung client đề xuất; cần backend xác nhận trước tích hợp. Huy hiệu yêu cầu campaign_completed=true và ar_completed>0. Điểm nhiệm vụ AR được suy ra bằng 20 × ar_completed; best_score là điểm cao nhất của lượt sorting, không gộp điểm AR.

GET progress trả trực tiếp object, không bọc trong `data`. Khi chưa có người chơi, đề nghị trả object mặc định (unlocked_level=1), hoặc thống nhất thêm xử lý 404 với client. Client kiểm tra player_id phải đúng ID yêu cầu. Merge hiện dùng max cho level/score/ar_completed và OR cho campaign_completed, phù hợp một người chơi demo; chưa hỗ trợ reset, conflict nhiều thiết bị hoặc save có phiên bản.

ScoreRequest:

```json
{"player_id":"demo-example","level":1,"score":85,"correct_count":9}
```

ArResultRequest:

```json
{"player_id":"demo-example","predicted_class":"Plastic","confidence":0.92,"player_choice":"Plastic","is_correct":true}
```

Hiện chỉ gửi kết quả AR khi hoàn thành đúng; chọn sai cho phép thử lại ngay trên client. Server không nên xem client score hoặc player_id là dữ liệu đã được xác thực. Chưa gửi ảnh về score/progress endpoint.

## Trước buổi tích hợp thật

1. Xác nhận tên field multipart (`file`), response wrapper và các field tiến trình.
2. Xác nhận server chấp nhận dữ liệu mô phỏng trong môi trường dev; không lẫn với số liệu nghiệm thu thật.
3. Kiểm tra `/health`, `/predict` bằng ảnh ngoài training, trường hợp accepted=false và lỗi server.
4. Kiểm tra POST score → PUT progress → GET progress và dữ liệu xuất hiện trên Adafruit IO Dashboard.
5. Ngắt mạng, kiểm tra timeout và lưu cục bộ. Khôi phục mạng rồi thử đồng bộ lại.
6. Nếu cần gửi lại lịch sử, bổ sung request ID và idempotency với backend trước khi tạo hàng đợi retry.
