"""Acceptance against a local isolated Docker service, including persistent learning data."""
import argparse
import io
import json
import sys
import time
import uuid
from datetime import datetime, timezone
from pathlib import Path
import httpx

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--url',default='http://127.0.0.1:8010')
    parser.add_argument('--read-only',action='store_true')
    parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args()
    expected=json.loads((Path(__file__).resolve().parents[1]/'app/ai_model/artifacts/metadata.json').read_text())
    with httpx.Client(base_url=args.url,timeout=30) as c:
        for attempt in range(45):
            try:
                health=c.get('/health');health.raise_for_status();break
            except (httpx.HTTPError, OSError):
                if attempt==44:raise
                time.sleep(2)
        h=health.json();assert h['model_sha256']==expected['model_sha256'] and len(h['class_names'])==12
        assert h['storage_mode']=='local','Use an isolated local-storage acceptance container'
        player='docker-acceptance-20261030'
        if not args.read_only:
            root=Path(__file__).resolve().parents[2]
            prediction=c.post('/predict',files={'file':('organic.jpg',(root/'img-test/test3.jpg').read_bytes(),'image/jpeg')})
            prediction.raise_for_status();assert set(prediction.json())=={'class_name','confidence','accepted'}
            bad=c.post('/predict',files={'file':('bad.jpg',b'not an image','image/jpeg')});assert bad.status_code==400
            p=c.get('/progress/'+player).json();p.update(best_score=30,unlocked_level=2)
            saved=c.put('/progress/'+player,json=p);saved.raise_for_status()
            session={'session_id':'d030'+('0'*28),'player_id':player,'mode':'Test','context':'Home',
                     'created_at':'2026-10-30T01:00:00Z','level':1,'total':10,'score':10,'expired':True,
                     'answers':[{'item_id':'water-bottle','expected':'Plastic','choice':'Plastic','correct':True,'seconds':3}]}
            for _ in range(2):c.post('/learning/sessions',json=session).raise_for_status()
        progress=c.get('/progress/'+player);progress.raise_for_status();assert progress.json()['best_score']==30
        history=c.get('/learning/'+player);history.raise_for_status();assert history.json()['total_sessions']==1
        result={'checked_at':datetime.now(timezone.utc).isoformat(),'storage':'local Docker volume','model_sha256':h['model_sha256'],
                'class_count':len(h['class_names']),'progress_restored':True,'learning_sessions_after_duplicate_upload':1,
                'restart_read_only_check':args.read_only}
        if not args.read_only:result['prediction']=prediction.json();result['invalid_image_status']=bad.status_code
    args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result,indent=2))

if __name__=='__main__':main()
