# EcoSort Quest AR — Thành viên 2: AI

Module AI thật đã huấn luyện: MobileNetV2 ImageNet, classification head ba lớp theo thứ tự **Plastic, Metal, Paper**. Model xuất nhận RGB 224×224 với giá trị 0–255; chuẩn hóa nằm trong model. Hàm `predict_image(image_bytes: bytes) -> dict` tự giải mã, xử lý EXIF, chuyển RGB và resize ảnh.

**Train lại bằng Kaggle GPU:** mở [notebook tự chứa](kaggle/EcoSort_Kaggle_GPU.ipynb) và [hướng dẫn Kaggle](kaggle/README-KAGGLE.md). Để nối với thành viên 1, đọc [hướng dẫn Unity–AI](INTEGRATION-UNITY-AI.md); có `api_demo.py` để thử AI Scan bằng model thật trước backend đầy đủ.

## Kết quả đã chạy

Dataset có 1.486 ảnh; train 1.038, validation 224, test 224. Huấn luyện head dừng sớm sau 10 epoch, giữ checkpoint theo validation loss. Validation accuracy 95,09%. Không fine-tune backbone trong lần chạy này; mã nguồn có tùy chọn fine-tune khi cần ở lần thử nghiệm mới.

Test giữ riêng: accuracy **90,625%**, macro precision **0,9095**, macro recall **0,8993**, macro F1 **0,9033**. Ngưỡng 0,75 được chọn trên validation. Test coverage 93,75%; accuracy trong số ảnh được nhận 93,81%. Đọc [báo cáo đánh giá](reports/EVALUATION.md) để xem kết quả từng lớp và giới hạn.

Các đồ thị bổ sung cho baseline này nằm trong [thư mục chẩn đoán](reports/diagnostics-baseline/CURVE-RESULTS.md). ROC-AUC macro one-vs-rest 0,9832, macro AP 0,9679, ECE 10 bin 0,0494; đây là chẩn đoán cùng test baseline, không xác minh ảnh điện thoại.

Đã có model `.keras`, mã huấn luyện, manifest chia dữ liệu, kiểm thử và hướng dẫn bàn giao. API demo qua HTTP đã được kiểm thử bằng model thật. **Chưa chạy Unity gọi API demo, chưa kiểm thử trên điện thoại, và chưa tích hợp backend chính của thành viên 3.** Không dùng kết quả test nội bộ để khẳng định chất lượng ngoài thực tế.

## Chạy trên máy hiện tại

Mở PowerShell tại thư mục `ai-model`. Môi trường `.venv` đã cài trên máy này, không cần activate:

```powershell
& .\.venv\Scripts\python.exe inference.py ..\Dataset\dataset-plastic-metal-paper\plastic\plastic1.jpg
& .\.venv\Scripts\python.exe -m pytest tests -q
```

Nếu tên ảnh khác, thay đối số bằng đường dẫn ảnh có thật. Kết quả gồm `class_name`, `confidence`, `accepted`. API demo để nối Unity tạm thời nằm ở `api_demo.py`; hướng dẫn chạy ở `INTEGRATION-UNITY-AI.md`.

Máy khác cần Python 3.12 và môi trường mới; không sao chép `.venv` giữa các máy:

```powershell
py -3.12 -m venv .venv
& .\.venv\Scripts\python.exe -m pip install -r requirements-lock.txt
```

`requirements-lock.txt` lưu phiên bản thực tế của lần chạy trên Windows CPU; `requirements-ai.txt` chứa dependency chính cho inference và pipeline. Khi chuyển sang Linux/Docker cần cài và kiểm thử lại trong môi trường đích. Lần train đầu cần Internet để tải trọng số ImageNet; inference dùng model đã xuất nên không cần tải trọng số.

## Tái tạo pipeline

Giữ dataset gốc ở thư mục anh em `../Dataset/dataset-plastic-metal-paper`, theo [DATASET.md](DATASET.md). Bộ model hiện tại được bảo toàn; huấn luyện lại vào thư mục lần chạy mới:

```powershell
& .\.venv\Scripts\python.exe prepare_dataset.py --output runs\new\reports
& .\.venv\Scripts\python.exe train.py --manifest runs\new\reports\splits.csv --artifacts runs\new\artifacts --reports runs\new\reports
& .\.venv\Scripts\python.exe evaluate.py --manifest runs\new\reports\splits.csv --artifacts runs\new\artifacts --reports runs\new\reports
```

Seed 42 giúp tái tạo cách chia dữ liệu; không bảo đảm trọng số giống từng byte giữa phần cứng/phiên bản khác nhau. `train.py` không đọc ảnh test. Chỉ đánh giá test khi đã chốt model/ngưỡng bằng validation; không tinh chỉnh theo kết quả test. Fine-tune tùy chọn bằng `--fine-tune-epochs 5` trong một lần train mới, chỉ giữ nếu validation loss cải thiện.

## Tệp bàn giao

- `artifacts/model.keras`, `artifacts/metadata.json`: model, mapping lớp, ngưỡng, checksum.
- `inference.py`, `data.py`, `__init__.py`: suy luận và tiền xử lý cho backend.
- `prepare_dataset.py`, `train.py`, `evaluate.py`: audit, chia nhóm, train, đánh giá.
- `reports/`: audit, split manifest, lịch sử train, biểu đồ, dự đoán từng ảnh, metrics và báo cáo test.
- `tests/`: kiểm tra dữ liệu, phân chia và inference với model thật.
- [HANDOFF-BACKEND.md](HANDOFF-BACKEND.md): hướng dẫn thành viên 3.
- [PHONE-TEST.md](PHONE-TEST.md): quy trình kiểm thử điện thoại còn cần thực hiện.

`handoff/ecosort-ai-handoff.zip` chứa module dưới tên package `ai_model`, có model và báo cáo nhưng không chứa `.venv`, cache hoặc dataset gốc. Tái đóng gói bằng `python package_handoff.py`; archive có `SHA256SUMS.json` và script xác minh nội dung sau khi tạo.

## Tài liệu kỹ thuật tham chiếu

[MobileNetV2 và tiền xử lý](https://www.tensorflow.org/api_docs/python/tf/keras/applications/MobileNetV2), [cài TensorFlow](https://www.tensorflow.org/install/pip), [TrashNet upstream](https://github.com/garythung/trashnet). Nguồn gốc dataset cục bộ chưa được xác thực byte-for-byte với upstream.
