"""Download the public biological subset for local research, preserving attribution.

Raw images are not committed: contents retain their original authors' copyright.
"""
import hashlib
import json
import shutil
import zipfile
from pathlib import Path
import requests

root = Path(__file__).resolve().parents[1]
cache = root / "Dataset" / "organic-source"
cache.mkdir(parents=True, exist_ok=True)
archive = cache / "garbage-classification.zip"
url = "https://www.kaggle.com/api/v1/datasets/download/mostafaabla/garbage-classification"
if not archive.exists():
    with requests.get(url, stream=True, timeout=120) as response:
        response.raise_for_status()
        with archive.with_suffix('.part').open('wb') as stream:
            for chunk in response.iter_content(1024 * 1024):
                stream.write(chunk)
    archive.with_suffix('.part').replace(archive)
destination = root / 'Dataset' / 'dataset-four-classes'
destination.mkdir(exist_ok=True)
for category in ('plastic', 'metal', 'paper'):
    shutil.copytree(root / 'Dataset' / 'dataset-plastic-metal-paper' / category,
                    destination / category, dirs_exist_ok=True)
organic = destination / 'organic'
organic.mkdir(exist_ok=True)
count = 0
with zipfile.ZipFile(archive) as source:
    for member in source.infolist():
        path = Path(member.filename)
        if 'biological' in path.parts and path.suffix.lower() in ('.jpg', '.jpeg', '.png'):
            (organic / path.name).write_bytes(source.read(member))
            count += 1
if count < 100:
    raise RuntimeError('Unexpected biological subset: review archive before training')
provenance = dict(source='https://www.kaggle.com/datasets/mostafaabla/garbage-classification',
    author='Mostafa Mohamed', version=1, original_class='biological', target_class='Organic',
    license='Database: Open Database, Contents: Copyright Original Authors',
    images=count, archive_sha256=hashlib.sha256(archive.read_bytes()).hexdigest(),
    limitation='Labels inherited from public dataset; source/background shift versus original three classes. Raw images excluded from Git.')
(cache / 'provenance.json').write_text(json.dumps(provenance, indent=2), encoding='utf-8')
print(json.dumps(provenance), flush=True)
