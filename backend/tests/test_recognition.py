import json
from pathlib import Path
import pytest
from pydantic import ValidationError
from fastapi.testclient import TestClient
from app.main import app
from app.recognition import CATALOG, MODEL_CLASS_NAMES
from app.schemas import Prediction, ArResultSubmission
from app.ai_model.inference import Predictor
from test_api import png_bytes

def test_catalog_shared_with_unity_and_no_unsafe_mapping():
    root=Path(__file__).resolve().parents[2]
    unity=json.loads((root/'ECOSORT_GAME/Assets/_EcoSortQuest/Resources/RecognitionCatalog.json').read_text(encoding='utf-8'))
    assert unity['classes']==CATALOG
    assert len(MODEL_CLASS_NAMES)==len(set(MODEL_CLASS_NAMES))==12
    assert {r['class_name'] for r in CATALOG if r['game_supported']}=={'Plastic','Metal','Paper','Organic'}
    assert all(r['display_name'] and r['guidance'] for r in CATALOG)
    assert not next(r for r in CATALOG if r['class_name']=='Batteries')['game_supported']
    with pytest.raises(ValidationError):
        Prediction(class_name='Unknown',confidence=.9,accepted=True)
    # Extra recognition labels do not silently become four-bin gameplay choices.
    with pytest.raises(ValidationError):
        ArResultSubmission(player_id='test',predicted_class='Batteries',confidence=.9,
                           player_choice='Metal',is_correct=True)

def test_all_recognition_labels_over_api_without_cloud(monkeypatch,tmp_path):
    monkeypatch.setenv('ECOSORT_STORAGE','local')
    monkeypatch.setenv('ECOSORT_DATA_FILE',str(tmp_path/'state.json'))
    with TestClient(app) as client:
        assert len(client.get('/recognition/classes').json()['classes'])==12
        for label in MODEL_CLASS_NAMES:
            # Contract fixture only; this does not claim model accuracy for generated images.
            monkeypatch.setattr(app.state.predictor,'predict',lambda data,name=label:
                                dict(class_name=name,confidence=.9,accepted=True))
            response=client.post('/predict',files={'file':('fixture.png',png_bytes(),'image/png')})
            assert response.status_code==200 and response.json()['class_name']==label
        monkeypatch.setattr(app.state.predictor,'predict',lambda data:
                            dict(class_name='Batteries',confidence=.4,accepted=False))
        assert client.post('/predict',files={'file':('fixture.png',png_bytes(),'image/png')}).json()['accepted'] is False

def test_artifact_rejects_checksum_and_label_order_changes(tmp_path):
    import shutil
    artifacts=Path(__file__).resolve().parents[1]/'app/ai_model/artifacts'
    shutil.copy2(artifacts/'model.keras',tmp_path/'model.keras')
    meta=json.loads((artifacts/'metadata.json').read_text())
    meta['model_sha256']='0'*64
    (tmp_path/'metadata.json').write_text(json.dumps(meta))
    with pytest.raises(RuntimeError,match='checksum'): Predictor(tmp_path)
    meta['class_names'][0],meta['class_names'][1]=meta['class_names'][1],meta['class_names'][0]
    (tmp_path/'metadata.json').write_text(json.dumps(meta))
    with pytest.raises(RuntimeError,match='mapping'): Predictor(tmp_path)
