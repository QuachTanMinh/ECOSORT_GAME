# Chạy lại EcoSort trên Kaggle GPU

> **Bản mới T4 ×2 / 4 hoặc12 nhóm:** dùng `EcoSort_Kaggle_T4x2_Advanced.ipynb`
> và đọc [HUONG-DAN-T4x2.md](HUONG-DAN-T4x2.md).
> Phần dưới mô tả notebook cũ; không áp dụng giới hạn chỉ dùng GPU đầu cho bản mới.

File cần import: **`EcoSort_Kaggle_GPU.ipynb`**, notebook tự chứa toàn bộ source. Không cần chép từng cell hoặc upload code riêng.

## Chuẩn bị

1. Tạo Kaggle Dataset riêng từ thư mục ảnh `Dataset/dataset-plastic-metal-paper`. Sau khi upload/giải nén phải thấy `plastic/`, `metal/`, `paper/`; nếu upload toàn TrashNet, notebook chỉ dùng ba lớp này.
2. Tạo Notebook và dùng chức năng Import/Upload Notebook để mở file `.ipynb` này. Add Input dataset vừa tạo.
3. Trong Settings chọn Accelerator → GPU (loại đang khả dụng), bật Internet để tải ImageNet weights. Notebook sẽ dừng nếu TensorFlow không thấy GPU. Không cài lại TensorFlow/CUDA vào image Kaggle bằng lệnh pip của môi trường Windows.
4. Sửa `DATASET_ROOT` ở cell cấu hình thành đường dẫn thư mục cha của ba lớp trong Input, hoặc giữ `None` nếu chỉ có một bộ phù hợp. Đặt `RUN_NAME` mới. Nếu có ID vật phẩm/phiên chụp, cung cấp `GROUPS_CSV`.
5. Chạy từ trên xuống. Đọc train/validation trước khi chạy cell test. Khi cấu hình đã chốt, chạy test và export. Save Version với lần chạy hoàn chỉnh, tải `ecosort-kaggle-handoff.zip` trong Output của run.

Kaggle thay đổi image theo thời gian; notebook kiểm tra package và lưu môi trường thực tế trong `requirements-runtime.txt`, `reports/environment.json`. GPU/quota tùy tài khoản; không bảo đảm loại GPU hoặc thời gian chạy cụ thể. Mặc định chỉ dùng GPU đầu, kể cả khi chọn T4 x2. [Tài liệu Kaggle](https://www.kaggle.com/docs/notebooks).

## Pipeline

Audit ảnh → nhóm pixel/dHash hoặc object/session → chia 70/15/15 cố định → RGB/EXIF/resize 224 → augmentation train → MobileNetV2 ImageNet frozen → train head → unfreeze 30 layer cuối với LR 1e-5, cố định BatchNorm → chọn checkpoint giữa hai phase bằng val_loss → chọn ngưỡng bằng validation → export `.keras` và checksum → reload qua inference backend → test giữ riêng → zip.

Đặt `fine_tune_epochs=0` để chạy baseline. Giảm `batch_size` nếu OOM; tạo run mới để tránh ghi đè. Model float32 giúp thuận tiện phục vụ CPU, không cần mixed precision để dùng GPU. Kết quả GPU không nhất thiết giống từng byte hoặc tốt hơn baseline Windows. Quy trình BatchNorm/fine-tune theo [TensorFlow transfer learning](https://www.tensorflow.org/guide/keras/transfer_learning).

## Biểu đồ và cách đọc

| Tệp | Mục đích |
|---|---|
| `split_distribution.png` | Số ảnh mỗi lớp theo train/validation/test |
| `training_curves.png` | Loss, Accuracy và Learning Rate cho cả head/fine-tune; vạch chia phase |
| `confusion_matrix.png` | Số lượng dự đoán đúng/sai theo lớp |
| `confusion_matrix_normalized.png` | Tỉ lệ theo hàng nhãn thật, dễ nhìn recall lớp ít ảnh |
| `roc_pr_curves.png` | ROC/AUC và Precision–Recall/AP one-vs-rest từng lớp |
| `reliability_confidence.png` | Top-label reliability, ECE 10 bin và confidence của ảnh đúng/sai |
| `threshold_risk_coverage.png` | Coverage/accepted accuracy theo ngưỡng và risk–coverage |
| `misclassified_examples.png` | Tối đa 12 ảnh sai để xem lỗi cụ thể; chỉ xuất nếu có lỗi |

Đồ thị validation nằm trong `reports/validation/`; đồ thị test nằm trực tiếp trong `reports/`. `curve_metrics.json` lưu AUC/AP/ECE/Brier và các điểm đồ thị; `probabilities.npz` lưu labels và xác suất để tái tạo. Brier ở đây là trung bình tổng bình phương sai số qua ba lớp, không chia thêm cho số lớp.

Train có augmentation/dropout nên train/validation không cùng điều kiện đo. Learning rate được ghi cuối epoch, có thể là LR vừa giảm cho epoch tiếp theo. Curve không phải statistical learning curve theo kích thước dataset; chưa có cross-validation hoặc khoảng tin cậy theo vật phẩm/session.

**Không chọn lại model/ngưỡng theo đồ thị test.** Test gốc đã được xem trong baseline Windows trước đó; cùng split dùng được để đối chiếu nhưng không là thử nghiệm độc lập hoàn toàn mới. Cần ảnh điện thoại từ vật phẩm/phiên chụp mới để xác minh chất lượng thực địa. Softmax confidence/ECE không chứng minh khả năng nhận diện vật lạ.

## Sau khi train

Tải zip và giữ nguyên `model.keras` + `metadata.json` cùng lần train. Dùng source inference trong chính zip, kiểm thử CPU/backend trước thay model đang dùng. Không sao chép toàn bộ runtime Kaggle vào Windows; `requirements-serving.txt` chỉ chứa dependency serving chính, vẫn phải kiểm tra tương thích hệ điều hành. Xem `../INTEGRATION-UNITY-AI.md` để nối vào Unity.

Notebook được sinh từ source trong workspace bằng `python kaggle/build_notebook.py`. Nếu sửa module, chạy lại script để notebook chứa source mới. Chưa chạy trên GPU Kaggle trong phiên triển khai này; smoke test CPU chỉ xác minh đường đi hai phase/export/evaluation, không xác nhận tốc độ hoặc accuracy GPU.
