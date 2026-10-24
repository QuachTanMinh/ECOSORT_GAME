"""Recognition vocabulary is independent of the four campaign materials."""
import json
from pathlib import Path
from typing import Literal

RecognitionLabel = Literal['Plastic', 'Metal', 'Paper', 'Organic', 'Cardboard', 'GreenGlass',
                           'BrownGlass', 'WhiteGlass', 'Clothes', 'Shoes', 'Batteries', 'Trash']
CATALOG = json.loads(Path(__file__).with_name('recognition_catalog.json').read_text(encoding='utf-8'))['classes']
MODEL_CLASS_NAMES = tuple(row['class_name'] for row in CATALOG)

def recognition_catalog():
    return {'classes': CATALOG, 'note': 'Quy định tiếp nhận phụ thuộc đơn vị thu gom tại địa phương.'}
