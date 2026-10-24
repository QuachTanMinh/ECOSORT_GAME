# Nhận diện 12 nhóm AI — 24/10/2026

## Cách sử dụng

Chạy backend rồi vào **Kết nối backend**, chọn Online/FastAPI với địa chỉ `http://127.0.0.1:8000`. Mở màn hình Scan, nạp ảnh một vật thể và bấm nhận diện. Nút **12 nhóm AI** mở sổ tay hướng dẫn.

Model EfficientNetV2B0 nhận đúng thứ tự: Plastic, Metal, Paper, Organic, Cardboard, GreenGlass, BrownGlass, WhiteGlass, Clothes, Shoes, Batteries, Trash. Input RGB 224×224, giá trị [0,255], tiền xử lý nằm trong model; ngưỡng chấp nhận 0.75. SHA-256: `c94553614878308bc219487c2910de23ff2dd5a686737014cdc96a540e18814f`.

Bốn nhóm Plastic/Metal/Paper/Organic tiếp tục dùng thử thách hiện có. Tám nhóm còn lại hiển thị kết quả và hướng dẫn riêng, chưa có vật phẩm/thùng 3D tương ứng. Cardboard là nhãn AI riêng; không tự ép vào Paper. Pin không bị gán vào thùng Metal. Kết quả bị từ chối sẽ xóa kết quả và khả năng bắt đầu thử thách từ lần quét trước. Trash là một lớp của bộ dữ liệu, không phải bộ phát hiện mọi vật lạ.

`POST /predict` giữ ba trường class_name/confidence/accepted. `GET /recognition/classes` cung cấp tên tiếng Việt, hướng dẫn và game_supported. Catalog backend và Unity giống nhau, được kiểm tra tự động.

## Đã kiểm chứng trên máy này

| Hạng mục | Kết quả |
|---|---|
| Backend | 14/14 kiểm thử đạt |
| Unity EditMode | 44/44 đạt |
| Unity PlayMode | 19/19 đạt, không bỏ qua |
| Chạy lại riêng luồng REST/ảnh trên UI | 1/1 đạt |
| Windows build | Thành công, 108532389 byte theo log build |
| Backend cổng 8000 | Model mới, 12 lớp, Adafruit IO |
| Cloud ghi và đọc lại | Organic, confidence 0.996063768863678, khớp feed lúc 18:03 ngày 24/10/2026 |

Ảnh kiểm thử REST sử dụng ảnh validation đã có trong dataset; đây là kiểm chứng tích hợp, không phải phép đo độ chính xác trên ảnh mới. Ảnh Pin và sổ tay dùng phản hồi giả lập, có nhãn MOCK. Xem các ảnh `recognition-*-20261024.png` trong Evidence và XML `Recognition12-*` trong TestResults.

Kaggle báo cáo test accuracy 97.06%, macro F1 95.88%; nhóm được chấp nhận có accuracy 98.52% với coverage 96.44%. Các số này thuộc tập test của lần train, không bảo đảm kết quả ngoài thực tế. Chưa nghiệm thu ảnh độc lập cho đủ 12 nhóm, AR hay mobile. Windows build đã biên dịch, chưa kiểm thử chơi thủ công bản executable trong đợt này.

Docker CLI không có trên máy hiện tại nên chưa build/chạy lại image cho model 12 lớp. Dịch vụ hiện dùng Python. Không dùng bằng chứng Docker cũ để kết luận image mới đã chạy.

## Chạy lại và khôi phục

Trên máy hiện tại, từ thư mục gốc repo:

```powershell
cd backend
..\ai-model\.venv\Scripts\python.exe -m uvicorn app.main:app --env-file .env --host 127.0.0.1 --port 8000
```

Trên máy mới, tạo môi trường Python 3.12 và cài backend/requirements.txt theo README. Giữ thông tin Adafruit trong backend/.env, không đưa khóa lên Git.

Bản bốn lớp trước khi thay được giữ tại `backend/data/model-backups/20261024-four-class/` (cục bộ, không nằm trên Git). Muốn quay lại, dừng server, đặt `ECOSORT_AI_ARTIFACTS` tới đường dẫn tuyệt đối của thư mục này rồi khởi động lại server. Kiểm tra /health xác nhận bốn lớp và hash cũ. Không thay đổi enum WasteType, lịch sử học tập hay thành tích đã lưu.

Báo cáo Word ngày 23/10 là ảnh chụp trạng thái trước tích hợp này; chưa được cập nhật trong đợt này.

## Cơ sở hướng dẫn phân loại

Quy tắc thu gom tùy địa phương; nhận diện vật liệu không xác định được mọi chất bẩn hay hóa chất. Tham khảo [EPA: vật liệu tái chế phổ biến](https://www.epa.gov/recycle/how-do-i-recycle-common-recyclables) và [EPA: pin gia dụng](https://www.epa.gov/recycle/used-household-batteries). Với quần áo/giày, ưu tiên tái sử dụng và hỏi điểm thu gom phù hợp.
