# Kiểm thử ảnh điện thoại — chưa thực hiện

Chưa có tập ảnh điện thoại độc lập được cung cấp; không dùng ảnh test hiện có hay ảnh tổng hợp để thay thế bằng chứng này.

1. Chụp ít nhất 20 vật phẩm mới mỗi lớp Plastic/Metal/Paper, nhiều nền, góc, ánh sáng và khoảng cách; một vật phẩm chính mỗi ảnh. Ghi thiết bị, điều kiện, vật phẩm và session ID. Vật phẩm/phiên chụp khác hoàn toàn dữ liệu đã train/validation/test.
2. Gán nhãn thủ công, kiểm tra nhãn bởi người thứ hai nếu có thể. Lưu `phone-test/plastic`, `phone-test/metal`, `phone-test/paper`. Giữ bản ảnh gốc, bao gồm EXIF; ưu tiên JPEG vì Unity hiện upload JPEG.
3. Chốt model/ngưỡng trước đánh giá. Từ thư mục `ai-model`, chạy:

```powershell
& .\.venv\Scripts\python.exe evaluate.py --external-dataset ..\phone-test --reports reports\phone-test
```

Lệnh từ chối ảnh trùng pixel với dataset gốc, nhưng không phát hiện mọi ảnh chụp lại cùng vật phẩm. Cần xác thực object/session bằng nhật ký thu thập. Báo cáo được lưu riêng, không ghi đè kết quả test nội bộ.

4. Báo cáo accuracy/macro F1 toàn bộ ảnh, từng lớp, confusion matrix, coverage và accepted accuracy. Lưu lỗi cùng điều kiện chụp; kiểm tra ảnh từ điện thoại qua Unity/API khi backend sẵn sàng.
5. Chụp thêm vật phẩm ngoài ba lớp, cảnh trống, nhiều vật phẩm để khảo sát khả năng từ chối. Không đưa chúng vào thư mục ba lớp với nhãn sai. Softmax confidence không phải bộ phát hiện OOD; cần bộ nhãn unknown riêng và quy trình đánh giá bổ sung trước khi khẳng định khả năng này.

Nếu dùng ảnh điện thoại để cải thiện model, chuyển chúng thành train/validation theo nhóm vật phẩm/session và thu thập một tập test mới; không tiếp tục gọi tập đã dùng để điều chỉnh là test độc lập.
