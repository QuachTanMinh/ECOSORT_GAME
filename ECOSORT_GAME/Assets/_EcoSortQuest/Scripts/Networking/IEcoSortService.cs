using System;
using System.Collections;

namespace EcoSortQuest
{
    [Serializable] public sealed class Prediction
    {
        public string class_name;
        public float confidence;
        public bool accepted;
        public bool TryRecognize(float threshold, out RecognitionCategory category)
        {
            category = null;
            return accepted && !float.IsNaN(threshold) && !float.IsInfinity(threshold) && threshold >= 0 && threshold <= 1 &&
                !float.IsNaN(confidence) && !float.IsInfinity(confidence) && confidence >= 0 && confidence <= 1 &&
                confidence >= threshold && RecognitionInfo.TryGet(class_name, out category);
        }
        public bool TryAccept(float threshold, out WasteType type)
        {
            type = default;
            return TryRecognize(threshold, out var category) && category.game_supported &&
                Enum.TryParse(class_name, true, out type) && Enum.IsDefined(typeof(WasteType), type) &&
                string.Equals(class_name, type.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
    [Serializable] public sealed class ScoreRequest
    {
        public string player_id;
        public int level, score, correct_count;
    }
    [Serializable] public sealed class ArResultRequest
    {
        public string player_id, predicted_class, player_choice;
        public float confidence;
        public bool is_correct;
    }
    public interface IEcoSortService
    {
        bool IsMock { get; }
        IEnumerator Health(Action<string> success, Action<string> failure);
        IEnumerator Predict(byte[] image, WasteType mockClass, Action<Prediction> success, Action<string> failure);
        IEnumerator GetProgress(string id, Action<PlayerProgress> success, Action<string> failure);
        IEnumerator PutProgress(PlayerProgress progress, Action success, Action<string> failure);
        IEnumerator PostScore(ScoreRequest score, Action success, Action<string> failure);
        IEnumerator PostArResult(ArResultRequest result, Action success, Action<string> failure);
    }
}
