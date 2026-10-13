# Trạng thái thành viên 2

| Công việc trong phân công | Trạng thái và bằng chứng |
|---|---|
| Môi trường Python | Đã tạo `.venv`; phiên bản ở `requirements-lock.txt` |
| Dataset ba lớp | Đã dùng 1.486 ảnh có sẵn; nguồn gốc chưa xác thực với upstream, xem `DATASET.md` |
| Gán nhãn/làm sạch | Nhãn thư mục, audit ảnh và trùng lặp; chưa duyệt thủ công từng nhãn |
| Train/Validation/Test | Đã cố định manifest 1.038/224/224; heuristic nhóm dHash, chưa có object ID gốc |
| MobileNetV2 và train head | Đã train ImageNet backbone frozen, head 10 epoch |
| Fine-tune nếu cần | Có mã tùy chọn; chưa chạy, baseline đáp ứng mục tiêu nội bộ |
| Accuracy/Precision/Recall/F1 | Đã đánh giá test giữ riêng: `reports/EVALUATION.md` |
| Confusion Matrix | `reports/confusion_matrix.png` |
| Export model | `artifacts/model.keras` và metadata/checksum |
| Inference | Hàm đúng contract, kiểm thử model thật |
| Kiểm thử ảnh điện thoại | **Chưa thực hiện**; quy trình ở `PHONE-TEST.md` |
| Bàn giao backend | Có hướng dẫn/archive; FastAPI demo đã smoke-test HTTP qua model thật; chưa chạy Unity end-to-end hoặc tích hợp backend chính |

Module AI sẵn sàng để thành viên 3 tích hợp. Chưa đánh dấu toàn bộ công việc thành viên 2 nghiệm thu xong vì còn kiểm thử thực tế trên điện thoại và tích hợp chung.
