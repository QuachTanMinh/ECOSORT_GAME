using System;
using UnityEngine;

namespace EcoSortQuest
{
    public enum WasteType { Plastic, Metal, Paper, Organic }
    public enum ClientScreen { MainMenu, Game, Scan, ARChallenge, Progress }

    public static class WasteInfo
    {
        public static string Label(WasteType type) => type == WasteType.Plastic ? "PLASTIC / NHỰA" :
            type == WasteType.Metal ? "METAL / KIM LOẠI" : type == WasteType.Paper ? "PAPER / GIẤY" : "ORGANIC / HỮU CƠ";
        public static string Tip(WasteType type) => type == WasteType.Plastic ? "Chai nhựa thuộc nhóm Plastic." :
            type == WasteType.Metal ? "Lon kim loại thuộc nhóm Metal." : type == WasteType.Paper ? "Giấy và báo thuộc nhóm Paper." : "Thức ăn thừa, vỏ quả và lá cây thuộc Hữu cơ.";
        public static Color Color(WasteType type) => type == WasteType.Plastic ? new Color(.12f, .66f, .88f) :
            type == WasteType.Metal ? new Color(.95f, .62f, .18f) : type == WasteType.Paper ? new Color(.36f, .76f, .48f) : new Color(.66f, .40f, .22f);
    }

    // Pure round state; shared by mouse/touch interaction and independent of scene/UI.
    public sealed class SortingRound
    {
        public int Score { get; private set; }
        public int Correct { get; private set; }
        public int Answered { get; private set; }
        public int Total { get; }
        public int Required { get; }
        public bool Finished { get; private set; }
        public bool Passed => Finished && Correct >= Required;
        public event Action Changed;
        public SortingRound(int total = 10, int required = 7)
        {
            if (total < 1 || required < 1 || required > total) throw new ArgumentOutOfRangeException();
            Total = total; Required = required;
        }
        public bool Submit(WasteType item, WasteType bin)
        {
            if (Finished) return false;
            bool correct = item == bin;
            Score = Math.Max(0, Score + (correct ? 10 : -5));
            if (correct) Correct++;
            Answered++;
            if (Answered >= Total) Finished = true;
            Changed?.Invoke();
            return correct;
        }
        public void Expire() { if (Finished) return; Finished = true; Changed?.Invoke(); }
    }

    [Serializable]
    public class PlayerProgress
    {
        public string player_id = "";
        public int unlocked_level = 1;
        public int best_score;
        public int ar_completed;
        public bool campaign_completed;
        public string updated_at = "";
        public bool ChallengeUnlocked => campaign_completed;
        public bool HasBadge => campaign_completed && ar_completed > 0;
        public void RecordRound(int level, SortingRound round)
        {
            best_score = Math.Max(best_score, round.Score);
            if (round.Passed)
            {
                unlocked_level = Math.Max(unlocked_level, Math.Min(3, level + 1));
                if (level == 3) campaign_completed = true;
            }
            Touch();
        }
        public void Merge(PlayerProgress other)
        {
            if (other == null) return;
            unlocked_level = Math.Max(unlocked_level, Math.Max(1, Math.Min(3, other.unlocked_level)));
            best_score = Math.Max(best_score, other.best_score);
            ar_completed = Math.Max(ar_completed, other.ar_completed);
            campaign_completed |= other.campaign_completed;
            Touch();
        }
        public void Touch() => updated_at = DateTime.UtcNow.ToString("O");
    }

    public static class LocalProgress
    {
        const string Key = "EcoSortQuest.LocalProgress.v1";
        public static PlayerProgress Load()
        {
            PlayerProgress data = null;
            try { data = JsonUtility.FromJson<PlayerProgress>(PlayerPrefs.GetString(Key, "")); }
            catch (Exception e) { Debug.LogWarning("EcoSort: local save could not be read: " + e.Message); }
            if (data == null) data = new PlayerProgress();
            if (string.IsNullOrEmpty(data.player_id)) data.player_id = "demo-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            data.unlocked_level = Mathf.Clamp(data.unlocked_level, 1, 3);
            data.best_score = Math.Max(0, data.best_score);
            data.ar_completed = Math.Max(0, data.ar_completed);
            return data;
        }
        public static void Save(PlayerProgress data)
        { data.Touch(); PlayerPrefs.SetString(Key, JsonUtility.ToJson(data)); PlayerPrefs.Save(); }
    }
}
