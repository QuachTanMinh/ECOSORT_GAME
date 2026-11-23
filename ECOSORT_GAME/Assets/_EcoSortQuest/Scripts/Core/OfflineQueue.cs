using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoSortQuest
{
    /// <summary>
    /// Hàng đợi bền vững cho ScoreRequest và ArResultRequest khi mất kết nối mạng.
    /// Dữ liệu được lưu trong PlayerPrefs dưới dạng JSON để không bị mất khi đóng game.
    /// Khi có mạng, hàm Flush() sẽ tự động gửi lại toàn bộ dữ liệu đang chờ.
    /// </summary>
    public sealed class OfflineQueue
    {
        const string ScoresKey = "EcoSortQuest.OfflineQueue.Scores.v1";
        const string ArResultsKey = "EcoSortQuest.OfflineQueue.ArResults.v1";
        const int MaxEntries = 100;

        [Serializable]
        private sealed class ScoreListWrapper
        {
            public List<ScoreRequest> items = new List<ScoreRequest>();
        }

        [Serializable]
        private sealed class ArResultListWrapper
        {
            public List<ArResultRequest> items = new List<ArResultRequest>();
        }

        public int PendingScoreCount
        {
            get
            {
                var list = LoadScores();
                return list.items.Count;
            }
        }

        public int PendingArCount
        {
            get
            {
                var list = LoadArResults();
                return list.items.Count;
            }
        }

        public int TotalPending => PendingScoreCount + PendingArCount;

        ScoreListWrapper LoadScores()
        {
            string raw = PlayerPrefs.GetString(ScoresKey, "");
            if (string.IsNullOrEmpty(raw)) return new ScoreListWrapper();
            try
            {
                var w = JsonUtility.FromJson<ScoreListWrapper>(raw);
                return w ?? new ScoreListWrapper();
            }
            catch
            {
                return new ScoreListWrapper();
            }
        }

        void SaveScores(ScoreListWrapper wrapper)
        {
            if (wrapper.items.Count > MaxEntries)
            {
                wrapper.items.RemoveRange(0, wrapper.items.Count - MaxEntries);
            }
            PlayerPrefs.SetString(ScoresKey, JsonUtility.ToJson(wrapper));
            PlayerPrefs.Save();
        }

        ArResultListWrapper LoadArResults()
        {
            string raw = PlayerPrefs.GetString(ArResultsKey, "");
            if (string.IsNullOrEmpty(raw)) return new ArResultListWrapper();
            try
            {
                var w = JsonUtility.FromJson<ArResultListWrapper>(raw);
                return w ?? new ArResultListWrapper();
            }
            catch
            {
                return new ArResultListWrapper();
            }
        }

        void SaveArResults(ArResultListWrapper wrapper)
        {
            if (wrapper.items.Count > MaxEntries)
            {
                wrapper.items.RemoveRange(0, wrapper.items.Count - MaxEntries);
            }
            PlayerPrefs.SetString(ArResultsKey, JsonUtility.ToJson(wrapper));
            PlayerPrefs.Save();
        }

        public void EnqueueScore(ScoreRequest score)
        {
            if (score == null) return;
            var list = LoadScores();
            list.items.Add(score);
            SaveScores(list);
            Debug.Log($"[OfflineQueue] Đã lưu score vào hàng đợi ngoại tuyến ({list.items.Count} đang chờ).");
        }

        public void EnqueueArResult(ArResultRequest result)
        {
            if (result == null) return;
            var list = LoadArResults();
            list.items.Add(result);
            SaveArResults(list);
            Debug.Log($"[OfflineQueue] Đã lưu AR result vào hàng đợi ngoại tuyến ({list.items.Count} đang chờ).");
        }

        /// <summary>
        /// Gửi lại tất cả scores và AR results đang chờ.
        /// </summary>
        public IEnumerator Flush(IEcoSortService service, Action<string> onStatus)
        {
            if (service == null || service.IsMock) yield break;

            var scores = LoadScores();
            int sentScores = 0;
            while (scores.items.Count > 0)
            {
                var current = scores.items[0];
                bool ok = false;
                string error = "";

                yield return service.PostScore(current, () => ok = true, e => error = e);

                if (ok)
                {
                    sentScores++;
                    scores.items.RemoveAt(0);
                    SaveScores(scores);
                }
                else
                {
                    onStatus?.Invoke($"Gửi lại điểm tạm dừng: {error} ({scores.items.Count} còn lại)");
                    yield break;
                }
            }

            var arResults = LoadArResults();
            int sentAr = 0;
            while (arResults.items.Count > 0)
            {
                var current = arResults.items[0];
                bool ok = false;
                string error = "";

                yield return service.PostArResult(current, () => ok = true, e => error = e);

                if (ok)
                {
                    sentAr++;
                    arResults.items.RemoveAt(0);
                    SaveArResults(arResults);
                }
                else
                {
                    onStatus?.Invoke($"Gửi lại AR tạm dừng: {error} ({arResults.items.Count} còn lại)");
                    yield break;
                }
            }

            if (sentScores > 0 || sentAr > 0)
            {
                onStatus?.Invoke($"Đã đồng bộ ngoại tuyến: {sentScores} điểm, {sentAr} thử thách AR.");
            }
        }
    }
}
