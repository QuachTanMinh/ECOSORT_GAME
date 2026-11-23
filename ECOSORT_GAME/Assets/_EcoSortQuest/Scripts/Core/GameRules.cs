using System;
using UnityEngine;

namespace EcoSortQuest
{
    public enum WasteType {
        Plastic, Metal, Paper, Organic,
        Cardboard, GreenGlass, BrownGlass, WhiteGlass,
        Clothes, Shoes, Batteries, Trash
    }
    public enum ClientScreen { MainMenu, Game, Scan, ARChallenge, Progress }

    public static class WasteInfo
    {
        public static string Label(WasteType type) => type switch {
            WasteType.Plastic    => "PLASTIC / NHỰA",
            WasteType.Metal      => "METAL / KIM LOẠI",
            WasteType.Paper      => "PAPER / GIẤY",
            WasteType.Organic    => "ORGANIC / HỮU CƠ",
            WasteType.Cardboard  => "CARDBOARD / CARTON",
            WasteType.GreenGlass => "THỦY TINH XANH LÁ",
            WasteType.BrownGlass => "THỦY TINH NÂU",
            WasteType.WhiteGlass => "THỦY TINH TRONG",
            WasteType.Clothes    => "QUẦN ÁO / VẢI",
            WasteType.Shoes      => "GIÀY DÉP",
            WasteType.Batteries  => "PIN / ẮC QUY",
            WasteType.Trash      => "RÁC THẢI CHUNG",
            _                    => type.ToString().ToUpperInvariant()
        };
        public static string Tip(WasteType type) => type switch {
            WasteType.Plastic    => "Chai nhựa thuộc nhóm Plastic.",
            WasteType.Metal      => "Lon kim loại thuộc nhóm Metal.",
            WasteType.Paper      => "Giấy và báo thuộc nhóm Paper.",
            WasteType.Organic    => "Thức ăn thừa, vỏ quả và lá cây thuộc Hữu cơ.",
            WasteType.Cardboard  => "Carton sạch, khô: ép phẳng trước khi thu gom.",
            WasteType.GreenGlass => "Làm rỗng chai thủy tinh xanh; tách màu nếu nơi thu gom yêu cầu.",
            WasteType.BrownGlass => "Chai bia nâu: làm rỗng và thu gom riêng thủy tinh màu.",
            WasteType.WhiteGlass => "Thủy tinh trong: làm sạch và bỏ nắp kim loại trước.",
            WasteType.Clothes    => "Ưu tiên tái sử dụng hoặc mang đến điểm thu gom quần áo.",
            WasteType.Shoes      => "Liên hệ điểm thu gom giày cũ; đừng vứt vào thùng thường.",
            WasteType.Batteries  => "NGUY HẠI — bỏ riêng tại điểm thu gom pin, không trộn chung.",
            WasteType.Trash      => "Rác thải chung: tự kiểm tra và theo hướng dẫn địa phương.",
            _                    => ""
        };
        public static Color Color(WasteType type) => type switch {
            WasteType.Plastic    => new Color(.12f, .66f, .88f),
            WasteType.Metal      => new Color(.95f, .62f, .18f),
            WasteType.Paper      => new Color(.36f, .76f, .48f),
            WasteType.Organic    => new Color(.66f, .40f, .22f),
            WasteType.Cardboard  => new Color(.82f, .64f, .30f),
            WasteType.GreenGlass => new Color(.20f, .68f, .32f),
            WasteType.BrownGlass => new Color(.55f, .32f, .10f),
            WasteType.WhiteGlass => new Color(.80f, .90f, .95f),
            WasteType.Clothes    => new Color(.62f, .40f, .78f),
            WasteType.Shoes      => new Color(.45f, .28f, .18f),
            WasteType.Batteries  => new Color(.90f, .78f, .08f),
            WasteType.Trash      => new Color(.55f, .55f, .55f),
            _                    => Color.gray
        };
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
