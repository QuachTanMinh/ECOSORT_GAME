"""Package source, model and evidence, excluding environments, caches and dataset."""
from pathlib import Path
import hashlib
import json
import zipfile


def package():
    base = Path(__file__).resolve().parent
    files = [p for p in base.iterdir() if p.is_file() and p.suffix in (".py", ".md", ".txt")]
    for folder in ("artifacts-organic", "reports-organic", "tests", "kaggle"):
        files.extend(p for p in (base / folder).rglob("*") if p.is_file() and p.suffix in
                     (".py", ".keras", ".json", ".csv", ".png", ".md", ".ipynb", ".npz"))
    files = sorted(files)
    output = base / "handoff"
    output.mkdir(exist_ok=True)
    checksums = {p.relative_to(base).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in files}
    archive = output / "ecosort-ai-handoff.zip"
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as stream:
        for p in files:
            stream.write(p, "ai_model/" + p.relative_to(base).as_posix())
        stream.writestr("ai_model/SHA256SUMS.json", json.dumps(checksums, indent=2))
    with zipfile.ZipFile(archive) as stream:
        assert stream.testzip() is None
        for name, digest in checksums.items():
            assert hashlib.sha256(stream.read("ai_model/" + name)).hexdigest() == digest
    print(json.dumps({"archive": str(archive), "bytes": archive.stat().st_size,
                      "files": len(files), "sha256": hashlib.sha256(archive.read_bytes()).hexdigest()}))


if __name__ == "__main__":
    package()
