from pathlib import Path
import pytest
from tools.evaluate_photos import read_manifest, summarize

def test_review_requires_labels_and_no_duplicate_bytes(tmp_path):
    (tmp_path / "a.jpg").write_bytes(b"same")
    (tmp_path / "b.jpg").write_bytes(b"same")
    manifest=tmp_path / "photos.csv"
    manifest.write_text("path,label,object_id,condition\na.jpg,Metal,can1,daylight\nb.jpg,Metal,can1,dark\n")
    with pytest.raises(ValueError,match="Duplicate"):read_manifest(manifest)
    manifest.write_text("path,label,object_id,condition\n../outside.jpg,Metal,can1,daylight\n")
    with pytest.raises(ValueError,match="stay"):read_manifest(manifest)

def test_review_separates_coverage_and_accepted_accuracy():
    rows=[dict(label="Metal",predicted="Metal",accepted=True,object_id="one",condition="light"),
          dict(label="Paper",predicted="Metal",accepted=False,object_id="two",condition="dark")]
    report=summarize(rows)
    assert report["accuracy"]==.5 and report["coverage"]==.5
    assert report["accepted_accuracy"]==1 and "Plastic" in report["missing_classes"]
    assert report["physical_objects"]==2
