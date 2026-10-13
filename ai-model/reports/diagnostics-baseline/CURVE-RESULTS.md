# Đồ thị chẩn đoán trên baseline Windows hiện có

Đây là biểu đồ đánh giá **cùng model baseline đã train trước đó**, trên đúng held-out test 224 ảnh. Không phải model Kaggle hay một lần train lại. Test này đã được xem trong baseline, không phải test độc lập mới cho các thay đổi tiếp theo.

- Macro ROC-AUC one-vs-rest: **0,9832**.
- Macro average precision one-vs-rest: **0,9679**.
- Top-label ECE, 10 bin bằng chiều rộng: **0,0494**.
- Ngưỡng confidence lấy từ validation: **0,75**.

Xem `roc_pr_curves.png`, `confusion_matrix_normalized.png`, `reliability_confidence.png`, `threshold_risk_coverage.png`, cùng xác suất lưu trong `probabilities.npz` và các điểm curve trong `curve_metrics.json`. Các bin nhỏ có nhiễu; ECE không xác nhận calibration trên điện thoại. Confidence và accepted accuracy không chứng minh OOD rejection.
