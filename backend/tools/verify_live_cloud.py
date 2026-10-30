"""Explicitly verify current-model inference and AIO read/write; preserves existing progress values.

Run from backend with the configured local .env. Raw progress backup stays under data/.
No credentials are printed. Predictions are real model outputs, not synthetic labels.
"""
import json
import logging
import os
import sys
from datetime import datetime, timezone
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))

def main():
    from dotenv import load_dotenv
    from fastapi.testclient import TestClient
    from app.main import app
    from app.adafruit import FEEDS
    logging.getLogger('httpx').setLevel(logging.WARNING)
    root=Path(__file__).resolve().parents[2]
    load_dotenv(root/'backend/.env',override=True)
    os.environ['ECOSORT_STORAGE']='adafruit_io'
    os.environ['ECOSORT_AI_ARTIFACTS']=str(root/'backend/app/ai_model/artifacts')
    directory=root/'backend/data/acceptance-20261030';directory.mkdir(parents=True,exist_ok=True)
    result={'checked_at':datetime.now(timezone.utc).isoformat(),'transport':'FastAPI TestClient with live Adafruit HTTP'}
    with TestClient(app) as client:
        health=client.get('/health');health.raise_for_status()
        result['model_sha256']=health.json()['model_sha256'];result['class_count']=len(health.json()['class_names'])
        store=app.state.store
        before=store._decode_value(store._latest(FEEDS['player_progress']))
        (directory/'progress-backup.json').write_text(json.dumps(before,ensure_ascii=False,indent=2),encoding='utf-8')
        if isinstance(before,dict) and 'player_id' in before:
            from app.schemas import Progress
            existing=Progress.model_validate(before)
            saved=client.put('/progress/'+existing.player_id,json=existing.model_dump(mode='json'));saved.raise_for_status()
            actual=client.get('/progress/'+existing.player_id);actual.raise_for_status()
            for field in ('player_id','best_score','unlocked_level','ar_completed','campaign_completed'):
                assert actual.json()[field]==saved.json()[field]==before[field]
            result['progress_round_trip']='passed; original values preserved; updated_at refreshed'
        else:result['progress_round_trip']='not run; no existing snapshot to preserve'
        image=root/'img-test/test3.jpg'
        response=client.post('/predict',files={'file':('organic-example.jpg',image.read_bytes(),'image/jpeg')});response.raise_for_status()
        predicted=response.json();readback=store._decode_value(store._latest(FEEDS['detected_waste']))
        assert readback['class_name']==predicted['class_name'] and readback['accepted']==predicted['accepted']
        assert abs(float(store._latest(FEEDS['ai_confidence']))-predicted['confidence'])<1e-6
        result['prediction_round_trip']={'response':predicted,'cloud_readback_matches':True,'photo':'existing img-test/test3.jpg; integration sample, not an independent accuracy benchmark'}
        score={'player_id':'acceptance-20261030','level':1,'score':10,'correct_count':1}
        posted=client.post('/scores',json=score);posted.raise_for_status()
        recorded=store._decode_value(store._latest(FEEDS['game_score']))
        assert all(recorded[k]==v for k,v in score.items());result['score_round_trip']='passed; explicitly labelled acceptance record'
    output=root/'ECOSORT_GAME/Logs/Demo-Cloud-20261030.json'
    output.write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(result,ensure_ascii=False,indent=2))

if __name__=='__main__':main()
