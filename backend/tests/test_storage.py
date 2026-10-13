from datetime import datetime, timezone

from app.schemas import Progress
from app.storage import LocalJsonStore


def test_progress_merge_is_monotonic_and_persists(tmp_path):
    store = LocalJsonStore(tmp_path / "data.json")
    first = Progress(player_id="p1", unlocked_level=3, best_score=900, ar_completed=2,
                     campaign_completed=True, updated_at=datetime.now(timezone.utc))
    store.put_progress(first)
    stale = first.model_copy(update={"unlocked_level": 1, "best_score": 10,
                                     "ar_completed": 0, "campaign_completed": False})
    merged = store.put_progress(stale)
    assert merged.unlocked_level == 3
    assert merged.best_score == 900
    assert merged.ar_completed == 2 and merged.campaign_completed
    assert store.get_progress("p1") == merged

