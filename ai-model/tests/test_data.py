import io

import numpy as np
import pytest
from PIL import Image

from data import MAX_BYTES, decode_image
from prepare_dataset import assign_splits, make_groups


def encoded(mode="RGB", size=(30, 20)):
    image = Image.new(mode, size)
    buffer = io.BytesIO(); image.save(buffer, format="PNG"); return buffer.getvalue()


@pytest.mark.parametrize("mode", ["RGB", "RGBA", "L"])
def test_image_conversion_is_consistent_rgb_uint8(mode):
    array = decode_image(encoded(mode))
    assert array.shape == (224, 224, 3)
    assert array.dtype == np.uint8


@pytest.mark.parametrize("content", [b"", b"not-an-image", None, "string", b"x" * (MAX_BYTES + 1)],
                         ids=["empty", "corrupt", "none", "wrong-type", "oversized"])
def test_invalid_input_rejected(content):
    with pytest.raises(ValueError):
        decode_image(content)


def test_exact_duplicate_with_conflicting_labels_fails():
    records = [{"path": "a", "label": 0, "sha256_pixels": "same", "dhash": 1},
               {"path": "b", "label": 1, "sha256_pixels": "same", "dhash": 1}]
    with pytest.raises(ValueError, match="Conflicting"):
        make_groups(records)


def test_near_duplicate_group_stays_in_one_split():
    records = []
    for label in range(3):
        for index in range(30):
            records.append({"path": f"{label}/{index}", "label": label, "sha256_pixels": f"{label}/{index}",
                            "dhash": int.from_bytes(f"{label}-{index:08d}".encode(), "little")})
    # Explicit pairs model a known session and are honored even when hashes differ.
    explicit = {f"{label}/{index}": f"{label}-{index // 2}" for label in range(3) for index in range(30)}
    groups = make_groups(records, near_distance=0, explicit=explicit)
    rows = assign_splits(groups, 42)
    for group_id in {row["group_id"] for row in rows}:
        assert len({row["split"] for row in rows if row["group_id"] == group_id}) == 1
    assert len(rows) == 90
    assert rows == assign_splits(groups, 42)


def test_real_manifest_is_disjoint_and_reproducible():
    import csv
    from pathlib import Path
    path = Path(__file__).parents[1] / "reports/splits.csv"
    with path.open(encoding="utf-8", newline="") as stream:
        rows = list(csv.DictReader(stream))
    assert len({row["path"] for row in rows}) == len(rows)
    for key in ("group_id", "sha256_pixels"):
        seen = {}
        for row in rows:
            assert key not in row or seen.setdefault(row[key], row["split"]) == row["split"]
    assert {row["split"] for row in rows} == {"train", "validation", "test"}
