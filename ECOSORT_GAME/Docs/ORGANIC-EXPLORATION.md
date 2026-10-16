# Bốn nhóm rác và Khám phá 3D — 15/10/2026

## Cách chơi

Từ MainMenu → **Học tập / AI / Thống kê** → chọn **Nhà ở / Trường học / Công viên** → **Khám phá 3D**. Không cần chạy lại công cụ Setup scene. Chế độ này chạy bằng GameScene hiện có.

- WASD hoặc phím mũi tên: đi; giữ Shift để chạy.
- Giữ chuột phải và kéo: xoay camera quanh nhân vật.
- E: nhặt vật phẩm gần nhân vật; khi cầm rác, đến gần thùng rồi nhấn E để phân loại.
- Q: đặt vật phẩm xuống chân nhân vật.
- ESC hoặc bánh răng: tạm dừng / cài đặt.

Mỗi lượt có 12 vật phẩm, ba vật phẩm mỗi nhóm. Nhân vật chỉ cầm một vật phẩm. Bỏ sai thùng giữ rác trên tay và chỉ ra nhóm đúng; người chơi phải sửa trước khi dọn xong. Điểm chỉ ghi nhận lần phân loại đầu tiên của mỗi vật phẩm: đúng +10, sai −5, không âm. Nhặt lại hoặc thử lại không cộng/trừ điểm thêm. Kết thúc khi đã bỏ đúng cả 12 vật phẩm. Lịch sử `Explore` lưu riêng, không gộp vào thống kê kiểm tra độc lập và không mở khóa chiến dịch.

## Danh mục và bối cảnh

36 vật phẩm, gồm 9 Nhựa, 9 Kim loại, 9 Giấy, 9 Hữu cơ. Mỗi vật phẩm có danh sách bối cảnh cụ thể trong `WasteCatalog.cs`; chọn theo danh sách này thay vì số mẫu mô hình. Lượt học/kiểm tra/3D lấy rác phù hợp bối cảnh và phân bố các nhóm cân bằng. Chiến dịch Level 1/2/3 tương ứng Nhà ở/Trường học/Công viên. Ôn tập chỉ chọn các lỗi phù hợp bối cảnh; nếu không có sẽ dùng danh mục bối cảnh đó.

Ví dụ: chai nước giặt, chai dầu gội, bã cà phê ở Nhà ở; bài tập và vở ở Nhà ở/Trường học; tờ rơi ở Trường học/Công viên. Chai nước, lon nước, vỏ chuối, lõi táo có thể xuất hiện ở nhiều nơi. Đây là danh mục phục vụ bài học, không khẳng định vật phẩm chỉ tồn tại ở một nơi.

Hữu cơ gồm vỏ chuối, lõi táo, vỏ cam, rau củ thừa, mẩu bánh mì, vỏ trứng, bã cà phê, vỏ dưa hấu và lá rụng. Màu thùng là quy ước của game; việc thu gom thực tế cần theo địa phương. Mô hình AI chỉ phân nhóm `Organic`, không nhận diện riêng từng vật phẩm hoặc chứng minh vật đó phù hợp mọi quy trình ủ phân.

## AI Organic đã huấn luyện

Checkpoint phục vụ backend: `backend/app/ai_model/artifacts/model.keras` và `metadata.json`. Thứ tự đầu ra cố định: **Plastic, Metal, Paper, Organic**. Backend giữ tương thích checkpoint ba lớp nếu cần hoàn nguyên, nhưng checkpoint được bàn giao hiện tại có bốn đầu ra. `/health` phải trả về đủ bốn tên lớp.

Huấn luyện MobileNetV2 với trọng số ImageNet, giữ backbone cố định, huấn luyện đầu phân loại mới trên cả bốn lớp. Augmentation chỉ áp dụng train; chọn checkpoint và ngưỡng bằng validation. Không chỉ đổi tên nhãn trong metadata.

| Tập | Plastic | Metal | Paper | Organic | Tổng |
|---|---:|---:|---:|---:|---:|
| Train | 336 | 286 | 416 | 689 | 1727 |
| Validation | 73 | 62 | 89 | 148 | 372 |
| Test | 73 | 62 | 89 | 148 | 372 |

Kết quả test giữ riêng: accuracy **96,77%**, macro F1 **95,99%**; Organic đúng **147/148** (precision 100%, recall 99,32%). Ngưỡng confidence 0,75; 359/372 ảnh được chấp nhận, accuracy trong nhóm chấp nhận 98,05%. Xem toàn bộ dự đoán, confusion matrix và số liệu tại `ai-model/reports-organic/`. Không lấy test để chỉnh lại ngưỡng.

985 ảnh Organic lấy từ lớp biological của [Garbage Classification — Mostafa Mohamed](https://www.kaggle.com/datasets/mostafaabla/garbage-classification), version 1. Metadata nguồn ghi “Database: Open Database, Contents: Copyright Original Authors”; ảnh thô không được đưa vào Git. Provenance và hash archive nằm ở `ai-model/reports-organic/organic-provenance.json`. Ba nhóm cũ giữ dữ liệu cục bộ trước đó; xem `ai-model/DATASET.md` về nguồn gốc chưa đối chiếu của chúng.

Giới hạn: ảnh Organic và ba nhóm cũ có thể khác nguồn/nền chụp; kết quả chưa chứng minh độ chính xác ngoài thực tế. Chia nhóm dHash giảm ảnh gần trùng, không chứng minh khác vật thể/phiên chụp. Chưa đánh giá ảnh lạ, nhiều vật cùng lúc, ảnh điện thoại độc lập hoặc từng loại lá/bã/vỏ cụ thể. Confidence cao không phải bảo đảm đúng.

## Chạy backend mới

Nếu dùng Docker, từ thư mục `backend` chạy `docker compose up --build -d` để image chứa model và mã mới; chỉ restart container cũ sẽ không cập nhật model nằm trong image. Kiểm tra `http://localhost:8000/health` có Organic. Nếu chạy Python, khởi động lại Uvicorn sau khi cập nhật source. Không cần thay AIO key; trường nhãn AI có thêm giá trị Organic. Kết quả chi tiết Explore vẫn lưu backend local như các lượt học khác.

## Tái huấn luyện

Từ thư mục gốc, cài `ai-model/requirements-ai.txt` trong môi trường Python. Dữ liệu ba nhóm cũ cần được cung cấp ở `Dataset/dataset-plastic-metal-paper`; ảnh thô không kèm repository.

```powershell
python ai-model/prepare_organic_source.py
python ai-model/prepare_dataset.py --dataset Dataset/dataset-four-classes --output ai-model/reports-new
python ai-model/train.py --dataset Dataset/dataset-four-classes --manifest ai-model/reports-new/splits.csv --reports ai-model/reports-new --artifacts ai-model/artifacts-new
python ai-model/evaluate.py --dataset Dataset/dataset-four-classes --manifest ai-model/reports-new/splits.csv --reports ai-model/reports-new --artifacts ai-model/artifacts-new
```

Script từ chối ghi đè checkpoint đã có. Đánh giá rồi mới chép cả model và metadata cùng phiên bản sang backend. Notebook Kaggle đã cập nhật bốn thư mục đầu vào; lần huấn luyện này chạy CPU cục bộ, không phải một lần chạy Kaggle GPU. Các báo cáo trong `ai-model/reports` là lịch sử ba lớp; báo cáo mới ở `reports-organic`.
