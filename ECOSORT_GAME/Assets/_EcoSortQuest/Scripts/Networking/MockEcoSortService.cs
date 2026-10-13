using System;
using System.Collections;
using UnityEngine;

namespace EcoSortQuest
{
    // Explicit integration fixture. Never claimed to be inference or a remote cloud.
    public sealed class MockEcoSortService : IEcoSortService
    {
        readonly float latency;
        public bool IsMock => true;
        public MockEcoSortService(float latency) { this.latency = latency; }
        IEnumerator Delay() { yield return new WaitForSecondsRealtime(latency); }
        public IEnumerator Health(Action<string> success, Action<string> failure)
        { yield return Delay(); success("Mock service hoạt động — không phải backend thật."); }
        public IEnumerator Predict(byte[] image, WasteType mockClass, Action<Prediction> success, Action<string> failure)
        { yield return Delay(); success(new Prediction { class_name = mockClass.ToString(), confidence = .92f, accepted = true }); }
        public IEnumerator GetProgress(string id, Action<PlayerProgress> success, Action<string> failure)
        {
            yield return Delay();
            try { success(JsonUtility.FromJson<PlayerProgress>(PlayerPrefs.GetString("EcoSort.MockCloud." + id,
                JsonUtility.ToJson(new PlayerProgress { player_id = id })))); }
            catch (Exception e) { failure("Mock save lỗi: " + e.Message); }
        }
        public IEnumerator PutProgress(PlayerProgress progress, Action success, Action<string> failure)
        {
            yield return Delay();
            PlayerPrefs.SetString("EcoSort.MockCloud." + progress.player_id, JsonUtility.ToJson(progress));
            PlayerPrefs.Save(); success();
        }
        public IEnumerator PostScore(ScoreRequest score, Action success, Action<string> failure)
        { yield return Delay(); success(); }
        public IEnumerator PostArResult(ArResultRequest result, Action success, Action<string> failure)
        { yield return Delay(); success(); }
    }
}
