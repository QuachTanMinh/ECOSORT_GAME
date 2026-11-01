import json
import httpx
from fastapi.testclient import TestClient
from app.main import app
from app.adafruit import AdafruitIOStore


def test_dashboard_uses_backend_snapshot_and_filters_stable_id(monkeypatch, tmp_path):
    monkeypatch.setenv("ECOSORT_STORAGE", "local")
    monkeypatch.setenv("ECOSORT_DATA_FILE", str(tmp_path / "scores.json"))
    with TestClient(app) as client:
        page=client.get('/dashboard')
        assert page.status_code==200 and 'fetch(\'/dashboard/data' in page.text
        assert 'X-AIO-Key' not in page.text and 'cdn.jsdelivr' not in page.text
        for player,score in [('one',80),('two',50)]:
            response=client.post('/scores',json={'player_id':player,'level':1,'score':score,'correct_count':8,
                                               'display_name':'Bạn Mây','environment':'Beach'})
            assert response.status_code==201
        data=client.get('/dashboard/data',params={'player_id':'one'}).json()
        assert data['returned']==1 and data['scores'][0]['score']==80
        assert data['scores'][0]['display_name']=='Bạn Mây' and data['scores'][0]['environment']=='Beach'
        assert client.get('/dashboard/data',params={'player_id':'<script>'}).status_code==422
        assert client.post('/scores',json={'player_id':'one','level':1,'score':1,'correct_count':1,'display_name':'<img>'}).status_code==422


def test_cloud_dashboard_parses_json_feed_values_and_skips_legacy_scalars():
    paths=[]
    def handler(request):
        paths.append(request.url.path)
        return httpx.Response(200,json=[{'value':'42'}, {'value':'broken'},
            {'value':json.dumps({'player_id':'one','level':1,'score':60,'correct_count':6,'created_at':'2026-11-01T01:00:00Z'})}])
    store=AdafruitIOStore('demo','not-real','https://io.adafruit.com/api/v2',feed_prefix='ecosort',transport=httpx.MockTransport(handler))
    try:
        scores=store.recent_scores()
        assert len(scores)==1 and scores[0]['score']==60 and scores[0]['display_name']==''
        assert paths==['/api/v2/demo/feeds/ecosort.game-score/data']
    finally:store.close()
