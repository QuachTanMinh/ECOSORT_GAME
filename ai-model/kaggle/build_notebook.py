"""Build a self-contained Kaggle notebook from the checked-in source modules."""
import ast
import json
from pathlib import Path

BASE = Path(__file__).resolve().parents[1]
cells = []


def markdown(text):
    cells.append({"cell_type": "markdown", "metadata": {}, "source": text.splitlines(keepends=True)})


def code(text):
    ast.parse(text)
    cells.append({"cell_type": "code", "metadata": {}, "source": text.splitlines(keepends=True), "execution_count": None, "outputs": []})


markdown("""# ECOSORT QUEST AR — Kaggle GPU Training

Pipeline độc lập: audit/chia nhóm → head → fine-tune → chọn checkpoint/ngưỡng bằng validation → test → export cho backend.

**Trước Run All:** thêm dataset chứa `plastic/`, `metal/`, `paper/`, `organic/` bằng Add Input; Settings → Accelerator → GPU; bật Internet để tải trọng số ImageNet. Không cài lại TensorFlow/CUDA bằng pip. Notebook giữ phiên bản runtime thực tế trong output. Mặc định dùng một GPU (T4 x2 vẫn chỉ dùng GPU đầu), float32 để dễ chạy backend CPU.

Notebook chứa toàn bộ source, không cần upload code riêng. Chỉ dùng 3 lớp trên nếu input có thêm lớp. Không tự giải nén zip: upload bộ ảnh để Kaggle giải nén thành thư mục, hoặc giải nén an toàn trước. Không ghi vào `/kaggle/input`.

Đây là notebook chưa chạy trên Kaggle trong phiên làm việc hiện tại. Chạy trên GPU rồi tải bằng chứng thực tế; không coi số liệu baseline Windows là kết quả Kaggle.
""")
code('''from pathlib import Path
import os, sys, json, subprocess, importlib.metadata
os.environ["TF_CPP_MIN_LOG_LEVEL"] = "2"
os.environ["KERAS_HOME"] = "/kaggle/working/.keras"
os.environ["MPLCONFIGDIR"] = "/kaggle/working/.matplotlib"
import tensorflow as tf
print("Python:", sys.version)
print("TensorFlow:", tf.__version__, "Keras:", tf.keras.__version__)
print("GPU:", tf.config.list_physical_devices("GPU"))
assert tf.config.list_physical_devices("GPU"), "Enable GPU in Settings > Accelerator, then restart session."
assert int(tf.__version__.split(".")[0]) >= 2 and tuple(map(int, tf.__version__.split(".")[:2])) >= (2, 16), "Need modern TensorFlow/Keras .keras support."
for package in ("Pillow", "numpy", "scikit-learn", "matplotlib"):
    print(package, importlib.metadata.version(package))
result = subprocess.run(["nvidia-smi"], capture_output=True, text=True)
print(result.stdout or result.stderr)
''')
markdown("""## 1. Cấu hình — sửa trước khi chạy

Đặt `DATASET_ROOT` là thư mục **cha của ba thư mục lớp**, không phải đường dẫn zip. Để `None` nếu chỉ có đúng một bộ dữ liệu phù hợp trong Input. Chọn `RUN_NAME` mới cho mỗi lần train, không ghi đè model cũ. `GROUPS_CSV` tùy chọn có cột `path,group_id` cho vật phẩm/phiên chụp. Dataset hiện tại thiếu ID gốc nên dHash chỉ giảm rò rỉ, không chứng minh object-disjoint.
""")
code('''DATASET_ROOT = None  # Ví dụ: Path("/kaggle/input/your-dataset/dataset-four-classes")
GROUPS_CSV = None
RUN_NAME = "ecosort_gpu_v1"
CONFIG = {
    "seed": 42, "batch_size": 32,
    "head_epochs": 25, "fine_tune_epochs": 15,
    "head_lr": 1e-3, "fine_tune_lr": 1e-5, "unfreeze_last": 30,
    "threshold_minimum": 0.75, "target_accepted_accuracy": 0.90,
    "require_gpu": True, "weights": "imagenet",
}
assert RUN_NAME and Path(RUN_NAME).name == RUN_NAME, "Use a simple run folder name"
RUN = Path("/kaggle/working") / RUN_NAME
assert not RUN.exists(), "Run already exists. Choose a new RUN_NAME or restart a clean session."
if DATASET_ROOT is None:
    candidates = sorted({p.parent for p in Path("/kaggle/input").rglob("plastic")
                         if p.is_dir() and all((p.parent / c).is_dir() for c in ("plastic", "metal", "paper", "organic"))})
    print("Dataset candidates:", candidates)
    assert len(candidates) == 1, "Set DATASET_ROOT explicitly to one dataset root."
    DATASET_ROOT = candidates[0]
DATASET_ROOT = Path(DATASET_ROOT)
assert all((DATASET_ROOT / c).is_dir() for c in ("plastic", "metal", "paper", "organic"))
print("Input:", DATASET_ROOT, "Output:", RUN)
''')
markdown("## 2. Source tự chứa — không cần sửa cell này")
sources = {name: (BASE / name).read_text(encoding="utf-8") for name in
           ("data.py", "prepare_dataset.py", "train.py", "gpu_train.py", "evaluation_curves.py", "inference.py", "evaluate.py", "__init__.py")}
code('''CODE = Path("/kaggle/working/ecosort_code")
CODE.mkdir(exist_ok=True)
SOURCES = ''' + repr(sources) + '''
for name, source in SOURCES.items():
    (CODE / name).write_text(source, encoding="utf-8")
sys.path.insert(0, str(CODE))
print("Source modules written:", list(SOURCES))
''')
markdown("""## 3. Audit và chia Train/Validation/Test

Giữ toàn bộ nhóm trùng/gần trùng trong một split, seed 42, mục tiêu 70/15/15. Dừng khi ảnh lỗi hoặc nhãn xung đột. Augmentation chỉ dùng train. Tránh đổi seed/split để tìm điểm test tốt hơn. Tập test cục bộ đã được xem ở lần baseline trước; chạy lại cùng split giúp so sánh, nhưng không biến nó thành một test độc lập mới.
""")
code('''from prepare_dataset import prepare
audit = prepare(DATASET_ROOT, RUN / "reports", seed=CONFIG["seed"], near_distance=4,
                groups_csv=Path(GROUPS_CSV) if GROUPS_CSV else None)
print(json.dumps(audit["split_counts"], indent=2))
''')
markdown("""## 4. Train head và fine-tune GPU

EarlyStopping, ReduceLROnPlateau, best checkpoint mỗi giai đoạn; fine-tune 30 layer cuối, BatchNorm cố định. Chọn checkpoint có **validation loss thấp nhất** giữa hai giai đoạn. Nếu fine-tune kém hơn, tự khôi phục head. Input raw RGB 0–255, chuẩn hóa [-1,1] nằm trong model; export không chứa augmentation. Không tải tensor test trong bước này.

Nếu OOM: tạo run mới và giảm batch 32 → 16 → 8. Không cần bật mixed precision để dùng GPU. Không bảo đảm fine-tune sẽ tốt hơn baseline.
""")
code('''from gpu_train import train_gpu
metadata = train_gpu(DATASET_ROOT, RUN, CONFIG)
print("Selected stage:", metadata["selected_stage"])
print("Validation accuracy:", metadata["validation_accuracy"])
print("Threshold:", metadata["threshold"])
''')
markdown("""## 5. Xem validation và đường học trước khi test

Loss/Accuracy cho cả hai phase, Learning Rate, phân bố dữ liệu; đồ thị validation nằm trong `reports/validation`. Train có augmentation/dropout, validation không có, nên hai đường không hoàn toàn cùng điều kiện đo. Nếu cần chỉnh cấu hình, chỉ dựa train/validation và chạy run mới **trước khi mở cell test**.
""")
code('''from IPython.display import display, Image
for name in ("split_distribution.png", "training_curves.png"):
    display(Image(filename=str(RUN / "reports" / name)))
for name in ("roc_pr_curves.png", "reliability_confidence.png", "threshold_risk_coverage.png"):
    display(Image(filename=str(RUN / "reports/validation" / name)))
print((RUN / "reports/threshold_calibration.json").read_text())
''')
markdown("""## 6. Test giữ riêng — chỉ chạy sau khi chốt cấu hình

Tải lại model `.keras` qua chính `Predictor` dùng cho backend, xác minh checksum và manifest. Báo cáo Accuracy/Precision/Recall/F1 trên **mọi ảnh**, kể cả ảnh rejected. ROC/PR là one-vs-rest; PR có baseline theo tỉ lệ lớp. Reliability/ECE và confidence chỉ chẩn đoán, không chứng minh unknown/OOD rejection. Risk–coverage trên test không được dùng chọn lại ngưỡng.
""")
code('''from evaluate import evaluate
metrics = evaluate(DATASET_ROOT, RUN / "reports/splits.csv", RUN / "artifacts", RUN / "reports")
print((RUN / "reports/EVALUATION.md").read_text())
for name in ("confusion_matrix.png", "confusion_matrix_normalized.png", "roc_pr_curves.png",
             "reliability_confidence.png", "threshold_risk_coverage.png", "misclassified_examples.png"):
    if (RUN / "reports" / name).exists():
        display(Image(filename=str(RUN / "reports" / name)))
''')
markdown("""## 7. Đóng gói và tải kết quả

Output gồm model + metadata cùng lần train, source inference, requirements runtime, cấu hình, manifest và các báo cáo. Không có dataset, checkpoint trung gian hoặc CUDA driver trong zip. `requirements-runtime.txt` là ghi nhận Kaggle, **không cài mù toàn bộ lên backend Windows**; cài TensorFlow/Keras tương thích theo phiên bản metadata rồi kiểm thử môi trường đích.

Sau cell này chọn Save Version → Save & Run All để lưu kết quả của lần chạy hoàn chỉnh, rồi tải zip ở Output. Bảo đảm RUN_NAME mới nếu chạy lại trong session hiện tại. Huấn luyện không đồng nghĩa kiểm thử điện thoại; cần bước trong hướng dẫn tích hợp.
""")
code('''import shutil, hashlib, zipfile
EXPORT = RUN / "export" / "ai_model"
EXPORT.mkdir(parents=True)
for name in ("__init__.py", "inference.py", "data.py"):
    shutil.copy2(CODE / name, EXPORT / name)
shutil.copytree(RUN / "artifacts", EXPORT / "artifacts")
shutil.copytree(RUN / "reports", EXPORT / "reports")
shutil.copytree(CODE, EXPORT / "training_source", ignore=shutil.ignore_patterns("__pycache__"))
for name in ("config.json", "requirements-runtime.txt"):
    shutil.copy2(RUN / name, EXPORT / name)
(EXPORT / "requirements-serving.txt").write_text(
    f"tensorflow=={tf.__version__}\\nkeras=={tf.keras.__version__}\\nPillow>=11,<13\\nnumpy>=1.26,<3\\n", encoding="utf-8")
checksums = {p.relative_to(EXPORT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
             for p in EXPORT.rglob("*") if p.is_file()}
(EXPORT / "SHA256SUMS.json").write_text(json.dumps(checksums, indent=2))
archive = shutil.make_archive(str(RUN / "ecosort-kaggle-handoff"), "zip", RUN / "export")
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    for name, digest in checksums.items():
        assert hashlib.sha256(z.read("ai_model/" + name)).hexdigest() == digest
print("Download:", archive)
print("SHA256:", hashlib.sha256(Path(archive).read_bytes()).hexdigest())
from IPython.display import FileLink
display(FileLink(archive))
''')
markdown("""## Đọc kết quả và bước tiếp theo

- Loss train giảm nhưng val tăng: dấu hiệu overfit; checkpoint sớm đã được giữ, không chọn epoch cuối chỉ vì train accuracy cao.
- ROC AUC/AP cao không thay thế accuracy/F1 tại quyết định argmax. Confusion matrix cho biết cặp lớp hay nhầm.
- ECE thấp trên dataset nhỏ không đảm bảo calibration trên điện thoại. Coverage giảm có thể tăng accepted accuracy nhưng cũng từ chối nhiều ảnh hơn.
- Chưa có khoảng tin cậy theo vật phẩm/session vì thiếu ID gốc; không khẳng định độ chính xác thực địa.
- Download zip → thành viên 3 nạp package `ai_model` trong FastAPI `/predict` → Unity đổi ApiSettings sang Rest. Xem `INTEGRATION-UNITY-AI.md` trong workspace.

Nguồn: [Kaggle Notebooks](https://www.kaggle.com/docs/notebooks), [TensorFlow transfer learning](https://www.tensorflow.org/guide/keras/transfer_learning), [MobileNetV2](https://www.tensorflow.org/api_docs/python/tf/keras/applications/MobileNetV2).
""")

notebook = {"cells": cells, "metadata": {"kernelspec": {"display_name": "Python 3", "language": "python", "name": "python3"},
             "language_info": {"name": "python"}}, "nbformat": 4, "nbformat_minor": 5}
for index, cell in enumerate(cells):
    cell["id"] = f"ecosort-{index:02d}"
target = Path(__file__).with_name("EcoSort_Kaggle_GPU.ipynb")
target.write_text(json.dumps(notebook, ensure_ascii=False, indent=1), encoding="utf-8")
print(f"Generated {target}: {len(cells)} cells; all code cells parsed")
