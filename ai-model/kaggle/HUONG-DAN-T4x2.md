# Train ECOSORT bằng GPU T4 ×2 trên Kaggle

Notebook mới: **EcoSort_Kaggle_T4x2_Advanced.ipynb**. Notebook cũ vẫn giữ nguyên.
Source dễ sửa: `train_t4x2.py`; sinh lại notebook bằng `python build_t4x2_notebook.py`.

## 1. Chọn mục tiêu

| Chế độ | Nhãn | Đưa vào game hiện tại |
|---|---|---|
| `expanded12` (mặc định) | Plastic, Metal, Paper, Organic, Cardboard, GreenGlass, BrownGlass, WhiteGlass, Clothes, Shoes, Batteries, Trash | Chưa: cần mở rộng API, enum, thùng rác, nội dung hướng dẫn và kiểm thử |
| `game4` | Plastic, Metal, Paper, Organic, đúng thứ tự hiện tại | Hợp đồng nhãn phù hợp; vẫn phải kiểm tra CPU/backend và ảnh thực tế trước khi thay |

Nhận biết nhiều **nhóm** không đồng nghĩa nhận biết chính xác từng loại vật (chai/lon/túi),
hoặc phát hiện nhiều vật trong một ảnh. Muốn bounding box cần dữ liệu gán nhãn và pipeline object detection riêng.
Cardboard được tách riêng trong 12 nhóm; không tự gộp sang Paper trong chế độ bốn nhóm.
Phân loại chất liệu chưa đưa ra quy tắc xử lý an toàn riêng cho pin/thủy tinh hay rác hỗn hợp.

## 2. Thao tác trên Kaggle

1. Mở [Kaggle Code](https://www.kaggle.com/code), tạo notebook mới, dùng **Import Notebook / Upload** để chọn file `.ipynb` trên.
2. Chọn **Add Input / Add Data** → tìm **Garbage Classification (12 classes)** của **Mostafa Mohamed**, slug `mostafaabla/garbage-classification`.
   [Đường dẫn dataset](https://www.kaggle.com/datasets/mostafaabla/garbage-classification).
   Kiểm tra điều khoản/license trên trang dữ liệu trước khi phân phối lại ảnh; ZIP kết quả không chứa ảnh gốc.
3. Trong Settings/Session options: chọn **GPU T4 x2**, bật **Internet** để tải ImageNet weights.
   Loại GPU, quota và yêu cầu xác minh tài khoản tùy trạng thái Kaggle thực tế.
4. Cell cấu hình: giữ `MODE='expanded12'` nếu muốn mở rộng, đặt `RUN_NAME` riêng.
   Nếu muốn cải thiện ngay bốn nhóm, đổi `MODE='game4'` và tên run có `4class`.
5. Chạy lần lượt tới cell export/validation. Console cần hiện **replicas: 2**, global batch **32** với mặc định16/GPU.
   Notebook ghi môi trường thực tế vào ZIP, không giả định có hai GPU nếu chỉ thấy một.
6. Sau khi chốt tham số trên validation, thêm một cell `RUN_TEST = True`, chạy nó rồi chạy **Held-out evaluation** và **Package**.
   Để lưu một lần chạy nền đầy đủ: đặt `RUN_TEST=True` ngay từ đầu, chọn **Save Version → Save & Run All**.
   Không điều chỉnh model/ngưỡng theo test; nếu đã xem test nhiều lần cần bộ kiểm tra độc lập mới.
7. Tải `RUN_NAME-handoff.zip` trong Output. Lưu notebook version và phiên bản dataset cùng kết quả.

Không cần nhập tài khoản hoặc API key vào source. Không cài lại CUDA/TensorFlow trong image Kaggle.

## 3. Pipeline và bằng chứng

- EfficientNetV2B0 ImageNet mặc định; có thể thử B2 ở run riêng hoặc MobileNetV2 làm đối chứng.
- `MirroredStrategy` chia batch qua các GPU thực sự có; mixed precision khi train, softmax float32.
- Audit ảnh, loại bản pixel trùng trong nhóm, gom dHash gần nhau trước khi chia khoảng70/15/15 **theo nhóm**.
  Tỉ lệ ảnh thực tế có thể lệch vì nhóm không cùng kích thước. Nhóm có nhãn mâu thuẫn được cách ly.
- `GROUPS_CSV` tùy chọn (`path,group_id`) giúp giữ cùng vật/phiên chụp trong một split; dHash không thay được ID vật.
- Augmentation chỉ ở train, cân bằng trọng số lớp, warm-up rồi fine-tune80 layer cuối, giữ BatchNorm cố định.
- Chọn checkpoint bằng val_loss, chọn threshold bằng validation; báo rõ target chưa đạt thay vì sửa điểm.
- Xuất model float32, reload an toàn, so sánh đầu ra trước/sau trên một probe validation.
- Test cuối báo accuracy toàn bộ ảnh, macro-F1, từng lớp, confusion matrix và coverage/accepted accuracy.

Model hiện tại ghi nhận96,77% trên372 ảnh bốn nhóm trong `reports-organic/metrics.json`.
**Không so sánh trực tiếp** con số đó với test12 nhóm hoặc split mới. Muốn chứng minh cải thiện:
huấn luyện MobileNetV2 và EfficientNetV2 trên cùng manifest/same task, chọn bằng validation,
đánh giá một lần trên bộ ảnh thực tế độc lập chưa dùng phát triển. Không có bảo đảm99% hoặc100%.

## 4. Kết quả tải về và tích hợp

ZIP có `artifacts/model.keras`, `artifacts/metadata.json`, checksum, `reports/splits.csv`, audit,
history/đồ thị, calibration, dự đoán/metrics test nếu đã chạy, cấu hình, môi trường,
`predict_standalone.py`, `requirements-serving.txt`, `deployment.json`.
Checkpoint từng stage nằm trong Output run nhưng không đóng vào ZIP để tiết kiệm dung lượng.

`deployment.json.test_completed` phải là `true` trước khi xem là đã đánh giá đầy đủ.
Không chép đè model đang phục vụ ngay. Giữ model/metadata cùng một lần train, kiểm tra SHA256,
khả năng load trên TensorFlow/Keras Windows và chạy `/predict` với ảnh thật trước.
Với12 nhóm: chỉ dùng helper độc lập để thử; backend hiện tại chủ động từ chối mapping12 lớp.
Notebook này không tự sửa backend, Unity hoặc cloud đang chạy.

## 5. Lỗi thường gặp

| Hiện tượng | Xử lý |
|---|---|
| No GPU / replicas1 | Chọn T4x2 và restart session; kiểm tra quota thực tế |
| NCCL / collective error | Restart session; thử một GPU để chẩn đoán, ghi nhận không phải run2GPU |
| OOM | Giảm `PER_GPU_BATCH=8`, đổi RUN_NAME và chạy session mới; B0 trước B2 |
| Không tải ImageNet weights | Bật Internet; nếu không được, cần attach weights đúng kiến trúc và chỉnh source, không âm thầm dùng random weights |
| Expected ONE dataset root | In cây thư mục Input và đặt DATASET_ROOT đúng thư mục cha các lớp |
| FileExistsError ở RUN | Run trước được bảo vệ; đổi RUN_NAME, không xóa kết quả cũ |
| Target NOT met | Đọc coverage/từng lớp, bổ sung ảnh lỗi và train lại; không gọi đó là đạt97% |
| Test NOT run | Đặt RUN_TEST=True rồi chạy riêng cell test và package |

Nguồn: [TensorFlow distributed training](https://www.tensorflow.org/guide/keras/distributed_training),
[mixed precision](https://www.tensorflow.org/guide/mixed_precision),
[EfficientNetV2](https://keras.io/api/applications/efficientnet_v2/efficientnet_v2_models/).

Kiểm tra cục bộ23/10/2026: notebook đúng schema, tất cả8 cell code hợp lệ;
smoke CPU chạy trọn warm-up → fine-tune → calibration → export/reload → test → ZIP
với MobileNetV2 và ảnh giả; kiểm tra phát hiện duplicate/nhãn mâu thuẫn và chia nhóm cũng đạt.
EfficientNetV2B0 đã kiểm tra riêng đầu ra12 lớp, input0..255 và export/reload an toàn bằng trọng số ngẫu nhiên.
Đây là kiểm tra đường đi code, không phải độ chính xác trên dữ liệu rác hoặc bằng chứng chạy nhiều GPU.
Chưa có kết quả train GPU Kaggle của notebook mới.

Để chạy lại kiểm tra dev: cài `requirements-notebook-check.txt` vào môi trường riêng,
chạy `python check_t4x2_notebook.py --smoke --backbone-check`. Không chạy lệnh này thay cho train Kaggle.
