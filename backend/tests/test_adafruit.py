import json

import httpx

from app.adafruit import AdafruitIOError, AdafruitIOStore, FEEDS
from datetime import datetime, timezone

from app.schemas import ArResultSubmission, Progress


def test_adafruit_uses_secret_header_and_json_feed_values():
    seen = []

    def handler(request: httpx.Request):
        seen.append(request)
        return httpx.Response(201, json={"id": 1})

    store = AdafruitIOStore("eco-user", "private-key", "https://io.adafruit.com/api/v2",
                            transport=httpx.MockTransport(handler))
    store.add_ar_result(ArResultSubmission(player_id="demo-1", predicted_class="Paper",
                                            confidence=0.88, player_choice="Paper", is_correct=True))
    request = seen[0]
    assert request.url.path == f"/api/v2/eco-user/feeds/{FEEDS['ar_result']}/data"
    assert request.headers["X-AIO-Key"] == "private-key"
    assert "private-key" not in str(request.url)
    payload = json.loads(request.content)
    event = json.loads(payload["value"])
    assert event["player_id"] == "demo-1"
    assert event["is_correct"] is True
    store.close()


def test_adafruit_errors_do_not_forward_vendor_body():
    def handler(_request: httpx.Request):
        return httpx.Response(429, text="secret account detail")

    store = AdafruitIOStore("eco-user", "private-key", "https://io.adafruit.com/api/v2",
                            transport=httpx.MockTransport(handler))
    try:
        store.add_ar_result(ArResultSubmission(player_id="demo-1", predicted_class="Paper",
                                                confidence=0.88, player_choice="Paper", is_correct=True))
        assert False, "Expected cloud request failure"
    except AdafruitIOError as exc:
        assert "429" in str(exc)
        assert "secret" not in str(exc)
    finally:
        store.close()


def test_adafruit_progress_reads_snapshot_and_publishes_level_and_snapshot():
    seen = []

    def handler(request: httpx.Request):
        seen.append(request)
        if request.method == "GET":
            return httpx.Response(200, json=[])
        return httpx.Response(201, json={"id": len(seen)})

    store = AdafruitIOStore("eco-user", "private-key", "https://io.adafruit.com/api/v2",
                            transport=httpx.MockTransport(handler))
    progress = Progress(player_id="demo-1", unlocked_level=2, best_score=150, ar_completed=1,
                        campaign_completed=False, updated_at=datetime.now(timezone.utc))
    saved = store.put_progress(progress)
    assert saved.unlocked_level == 2
    assert len(seen) == 3  # Read snapshot, then publish progress snapshot and chart value.
    assert seen[0].method == "GET"
    assert seen[1].url.path.endswith(f"/feeds/{FEEDS['player_progress']}/data")
    assert json.loads(json.loads(seen[1].content)["value"])["ar_completed"] == 1
    assert seen[2].url.path.endswith(f"/feeds/{FEEDS['current_level']}/data")
    assert json.loads(seen[2].content)["value"] == "2"
    store.close()

