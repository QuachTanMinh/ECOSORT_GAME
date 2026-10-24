from fastapi.testclient import TestClient
from app.main import app
from app.schemas import Prediction
from app.ai_model.inference import Predictor
import pytest
import csv
from pathlib import Path

def test_organic_exploration_history_and_contract(monkeypatch,tmp_path):
    monkeypatch.setenv('ECOSORT_STORAGE','local')
    monkeypatch.setenv('ECOSORT_DATA_FILE',str(tmp_path/'data.json'))
    payload=dict(session_id='b'*32,player_id='organic-player',mode='Explore',context='Park',
        created_at='2026-10-15T00:00:00Z',level=1,total=1,score=10,expired=False,
        answers=[dict(item_id='banana-peel',expected='Organic',choice='Organic',correct=True,seconds=12)])
    with TestClient(app) as client:
        assert client.post('/learning/sessions',json=payload).status_code==201
        result=client.get('/learning/organic-player').json()
        assert result['total_sessions']==1 and result['test_sessions']==0
        assert 'Organic' in result['test_groups']
        payload['answers'][0]['expected']='Plastic'
        assert client.post('/learning/sessions',json=payload).status_code==422
    assert Prediction(class_name='Organic',confidence=.9,accepted=True).accepted

def test_serving_model_has_twelve_outputs():
    predictor=Predictor()
    assert predictor.classes==('Plastic','Metal','Paper','Organic','Cardboard','GreenGlass',
                               'BrownGlass','WhiteGlass','Clothes','Shoes','Batteries','Trash')
    assert predictor.model.output_shape[-1]==12

def test_real_organic_image_over_api(monkeypatch,tmp_path):
    root=Path(__file__).resolve().parents[2]
    manifest=root/'ai-model/reports-organic/splits.csv'
    dataset=root/'Dataset/dataset-four-classes'
    if not manifest.exists() or not dataset.exists():
        pytest.skip('Local held-out images are not distributed in source repository')
    with manifest.open(encoding='utf-8',newline='') as stream:
        candidates=[r for r in csv.DictReader(stream) if r['split']=='test' and r['class_name']=='Organic']
    monkeypatch.setenv('ECOSORT_STORAGE','local')
    monkeypatch.setenv('ECOSORT_DATA_FILE',str(tmp_path/'data.json'))
    # API transport must match direct inference, including genuine model mistakes.
    # Use the first ten held-out rows, not a cherry-picked successful prediction.
    with TestClient(app) as client:
        outputs=[]
        for row in candidates[:10]:
            image=dataset/row['path'];content=image.read_bytes()
            expected=app.state.predictor.predict(content)
            response=client.post('/predict',files={'file':(image.name,content,'image/jpeg')})
            assert response.status_code==200,response.text
            assert response.json()==expected
            outputs.append(response.json()['class_name'])
        assert 'Organic' in outputs
