"""Generate the portable Kaggle notebook from reviewable Python source."""
import ast
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "train_t4x2.py"
INTRO = """# ECOSORT — Kaggle T4 ×2: mở rộng 12 nhóm rác

Notebook tự chứa, không cần clone repo hoặc cung cấp API key.

1. **Add Input**: [Garbage Classification (12 classes)](https://www.kaggle.com/datasets/mostafaabla/garbage-classification).
2. **Settings**: GPU **T4 x2**, Internet **On** để tải ImageNet weights. Không nâng cấp TensorFlow/CUDA bằng pip.
3. Chọn `MODE='expanded12'` (nghiên cứu 12 nhóm) hoặc `MODE='game4'` (giữ bốn nhãn game hiện tại).
4. Đổi `RUN_NAME` cho mỗi thử nghiệm. Chạy đến hết cell **CPU-friendly export / validation**.
5. Sau khi chốt cấu hình bằng validation, tạo cell `RUN_TEST = True` rồi chạy cell **Held-out evaluation** một lần.
6. Chạy cell **Package**, tải ZIP từ Output. Muốn chạy tự động một lần đầy đủ bằng Save Version → Run All,
   đặt `RUN_TEST=True` trước khi bắt đầu và không chỉnh tham số theo kết quả test.

**Mục tiêu, không phải kết quả đã đạt:** cải thiện khả năng tổng quát hóa với EfficientNetV2,
augmentation và fine-tuning. Hai GPU tăng năng lực tính toán, không tự bảo đảm accuracy cao hơn.

**Giới hạn:** đây là phân loại một ảnh/một vật chính, không có bounding box hoặc phát hiện nhiều vật.
12 nhãn mở rộng chưa tương thích backend/Unity hiện tại; KHÔNG thay model đang chạy bằng bản 12 nhãn.
`game4` giữ Plastic/Metal/Paper/Organic, không gộp cardboard vào Paper trong lần này để tránh thay định nghĩa nhãn.
Ngưỡng confidence không phải cơ chế phát hiện vật lạ. Cần bộ ảnh thực tế độc lập và đối chứng cùng split.
Ảnh công khai có thể đã xuất hiện trong lần đánh giá trước; split mới không tự biến thành benchmark độc lập.

Nguồn kỹ thuật: [TensorFlow nhiều GPU](https://www.tensorflow.org/guide/keras/distributed_training),
[mixed precision](https://www.tensorflow.org/guide/mixed_precision),
[EfficientNetV2 preprocessing](https://keras.io/api/applications/efficientnet_v2/efficientnet_v2_models/).
"""

def build():
    source = SOURCE.read_text(encoding="utf-8")
    ast.parse(source)
    cells = [{"cell_type": "markdown", "metadata": {}, "source": INTRO.splitlines(keepends=True)}]
    for chunk in re.split(r"^# %% ", source, flags=re.MULTILINE)[1:]:
        title, code = chunk.split("\n", 1)
        ast.parse(code)
        cells.append({"cell_type": "markdown", "metadata": {}, "source": ["## " + title]})
        cells.append({"cell_type": "code", "metadata": {}, "source": code.splitlines(keepends=True),
                      "execution_count": None, "outputs": []})
    notebook = {"nbformat": 4, "nbformat_minor": 5, "cells": cells,
        "metadata": {"kernelspec": {"display_name": "Python 3", "language": "python", "name": "python3"},
            "language_info": {"name": "python", "version": "3.12"},
            "ecosort_source_sha256": hashlib.sha256(source.encode()).hexdigest()}}
    for i, cell in enumerate(cells):
        cell["id"] = f"ecosort-{i:03d}"
    output = ROOT / "EcoSort_Kaggle_T4x2_Advanced.ipynb"
    output.write_text(json.dumps(notebook, ensure_ascii=False, indent=1), encoding="utf-8")
    print(output)

if __name__ == "__main__": build()
