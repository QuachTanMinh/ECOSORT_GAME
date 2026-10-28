using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EcoSortQuest
{
    public enum LearningMode { Campaign, Learn, Test, Review, Explore }
    public enum LearningContext { Home, School, Park }
    [Serializable] public sealed class LearningAnswer
    {
        public string item_id, expected, choice;
        public bool correct;
        public float seconds;
    }
    [Serializable] public sealed class LearningSession
    {
        public string session_id = Guid.NewGuid().ToString("N"), player_id, mode, context, created_at;
        public int level, total, score;
        public bool expired;
        public LearningAnswer[] answers;
    }
    [Serializable] public sealed class AiMissionRecord
    {
        public string created_at, target, choice, predicted, knowledge_item_id;
        public float confidence;
        public bool accepted, mock, agrees;
        public bool knowledge_confirmed;
    }
    [Serializable] public sealed class LearningHistory
    {
        public List<LearningSession> sessions = new List<LearningSession>();
        public List<AiMissionRecord> missions = new List<AiMissionRecord>();
        public List<string> pending = new List<string>();
    }
    public static class LearningStore
    {
        public static string Key(string id) => "EcoSortQuest.Learning.v1." + id;
        public static LearningHistory Load(string id)
        {
            LearningHistory h = null;
            try { h = JsonUtility.FromJson<LearningHistory>(PlayerPrefs.GetString(Key(id), "")); }
            catch (Exception) { Debug.LogWarning("Không đọc được lịch sử học tập; không thay đổi tiến trình chiến dịch."); }
            h = h ?? new LearningHistory();
            h.sessions = h.sessions ?? new List<LearningSession>(); h.missions = h.missions ?? new List<AiMissionRecord>();
            h.pending = h.pending ?? new List<string>(); return h;
        }
        public static void Save(string id, LearningHistory history)
        { PlayerPrefs.SetString(Key(id), JsonUtility.ToJson(history)); PlayerPrefs.Save(); }
        public static void Add(LearningHistory h, LearningSession session)
        {
            if (h.sessions.Any(s => s.session_id == session.session_id)) return;
            h.sessions.Add(session); h.pending.Add(session.session_id);
            // Keep unsent records until explicitly acknowledged; cap acknowledged history only.
            while (h.sessions.Count > 100)
            {
                int index = h.sessions.FindIndex(s => !h.pending.Contains(s.session_id));
                if (index < 0) break; h.sessions.RemoveAt(index);
            }
        }
        public static List<WasteDefinition> Mistakes(LearningHistory h)
        {
            // Latest response per item: a successful review resolves an earlier mistake.
            var wrong = new List<string>();
            foreach (var s in h.sessions)
                foreach (var a in s.answers ?? Array.Empty<LearningAnswer>())
                { wrong.Remove(a.item_id); if (!a.correct) wrong.Add(a.item_id); }
            wrong.Reverse();
            return wrong.Select(id => WasteCatalog.All.FirstOrDefault(d => d.Id == id)).Where(d => d != null).ToList();
        }
        public static string Summary(LearningHistory h)
        {
            var tests = h.sessions.Where(s => s.mode == LearningMode.Test.ToString()).ToArray();
            var lines = new List<string> { "KIỂM TRA ĐỘC LẬP: " + tests.Length + " lượt (không gộp luyện tập có gợi ý)" };
            foreach (WasteType type in Enum.GetValues(typeof(WasteType)))
            {
                var a = tests.SelectMany(s => s.answers ?? Array.Empty<LearningAnswer>()).Where(x => x.expected == type.ToString()).ToArray();
                int right = a.Count(x => x.correct);
                lines.Add(WasteInfo.Label(type) + ": " + right + "/" + a.Length + (a.Length == 0 ? " • chưa có dữ liệu" : " • " + (100f * right / a.Length).ToString("F0") + "%"));
            }
            if (tests.Length > 0)
            {
                Func<LearningSession, string> rate = s => LearningInsights.Rate(s).ToString("F0") + "%";
                lines.Add("Lượt kiểm tra đầu: " + rate(tests[0]) + " • Gần nhất: " + rate(tests[tests.Length - 1]) + " (câu chưa trả lời tính vào tổng)");
            }
            lines.Add("Cần ôn lại: " + Mistakes(h).Count + " vật phẩm • Chờ lưu trực tuyến: " + h.pending.Count);
            var real = h.missions.Where(m => !m.mock).ToArray();
            lines.Add("Quan sát AI thật: " + real.Length + " lần • Đồng thuận lựa chọn: " + real.Count(m => m.agrees) + " (không phải độ chính xác của nhận diện)");
            return string.Join("\n", lines);
        }
    }
    public static class LearningDeck
    {
        public static WasteDefinition[] Build(LearningContext context, int count, int seed, List<WasteDefinition> review = null)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            var pool = review != null && review.Count > 0 ? review.Where(d => d.AppearsIn(context)).ToArray() :
                WasteCatalog.All.Where(d => d.AppearsIn(context)).ToArray();
            if (pool.Length == 0) pool = WasteCatalog.All.Where(d => d.AppearsIn(context)).ToArray();
            var random = new System.Random(seed); var result = new WasteDefinition[count];
            var groups=pool.GroupBy(d=>d.Type).Select(g=>g.OrderBy(_=>random.Next()).ToArray()).ToArray();
            for(int i=0;i<count;i++) {var group=groups[i%groups.Length];result[i]=group[(i/groups.Length)%group.Length];}
            for(int i=count-1;i>0;i--){int j=random.Next(i+1);var value=result[i];result[i]=result[j];result[j]=value;}
            return result;
        }
    }
}
