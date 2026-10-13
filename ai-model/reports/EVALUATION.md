# Báo cáo đánh giá EcoSort MobileNetV2

Model: MobileNetV2 pretrained ImageNet + Global Average Pooling + Dense 128 + Dropout + Softmax 3 lớp.
Tập đánh giá: **held_out_test**, **224 ảnh**. Train/validation/test được cố định trước huấn luyện; chọn checkpoint và ngưỡng bằng validation, không dùng test để chọn model.

| Chỉ số trên toàn bộ ảnh | Kết quả |
|---|---:|
| Accuracy | 90.62% |
| Macro Precision | 0.9095 |
| Macro Recall | 0.8993 |
| Macro F1 | 0.9033 |
| Mục tiêu nội bộ Accuracy ≥85% | Đạt |
| Mục tiêu nội bộ Macro F1 ≥0.80 | Đạt |

Các mục tiêu này không phải điểm chấm hoặc bảo đảm kết quả ngoài thực tế.

| Lớp | Precision | Recall | F1 | Số ảnh |
|---|---:|---:|---:|---:|
| Plastic | 0.877 | 0.877 | 0.877 | 73 |
| Metal | 0.946 | 0.855 | 0.898 | 62 |
| Paper | 0.905 | 0.966 | 0.935 | 89 |

![Confusion matrix](confusion_matrix.png)

Ngưỡng confidence chọn từ validation: **0.75**. Coverage trên tập đánh giá: **93.75%**; accepted accuracy: **0.9380952380952381**. Accuracy/Macro F1 phía trên tính cả ảnh bị từ chối, không chỉ tính ảnh confidence cao. Chỉ số accepted accuracy là độ chính xác phân loại trong các ảnh được nhận, không phải calibration hoặc OOD accuracy.

Thời gian inference sau warmup, bao gồm giải mã/resize và model, không gồm HTTP/đọc file: median **120.6 ms**, p95 **144.1 ms**, 30 ảnh. TensorFlow 2.20.0; GPU thấy bởi TensorFlow: [].

## Giới hạn

- Dataset hiện tại có nền và điều kiện chụp hẹp. Kết quả này chưa chứng minh chất lượng trên ảnh điện thoại người dùng.
- Không có object/session ID gốc; nhóm ảnh gần nhau bằng dHash chỉ giảm rủi ro rò rỉ, không bảo đảm object-disjoint.
- Chỉ nhận diện một vật phẩm chính thuộc Plastic/Metal/Paper; không object detection hoặc phân loại nhiều vật phẩm.
- Confidence cao không bảo đảm ảnh thuộc ba lớp. Chưa có tập unknown/OOD để đánh giá khả năng từ chối vật phẩm lạ.
- Chưa kiểm thử Unity → FastAPI → model thật hoặc ảnh chụp điện thoại độc lập.

Model SHA256: `b0346d1dc428bba255705bacb451e9c6635fffc8d8a0e1a679fe838e828a5f6c`.
Manifest SHA256: `eaf7e344281f308241e22d68b5c6bf41e0cec98752e2734cbd185ac9abf52adc`.
Chi tiết từng ảnh: `predictions.csv`; số liệu máy đọc: `metrics.json`, `classification_report.json`.
