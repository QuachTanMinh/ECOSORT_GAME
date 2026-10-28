using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoSortQuest
{
    public sealed class LearningRecommendation
    {
        public LearningMode Mode;
        public LearningContext Context;
        public string Reason;
    }

    // Derived from verified catalog answers only. AI observations never enter these metrics.
    public static class LearningInsights
    {
        public static float Rate(LearningSession session) => session.total > 0 ?
            100f * (session.answers ?? Array.Empty<LearningAnswer>()).Count(a => a.correct) / session.total : 0;
        public static string ContextName(LearningContext context) => context == LearningContext.Home ? "Nhà ở" :
            context == LearningContext.School ? "Trường học" : "Công viên";
        public static LearningSession[] Tests(LearningHistory h, LearningContext context) => h.sessions
            .Where(s => s.mode == "Test" && s.context == context.ToString() && s.total > 0).ToArray();

        public static LearningRecommendation Recommend(LearningHistory h, LearningContext selected)
        {
            var mistakes = LearningStore.Mistakes(h);
            if (mistakes.Count > 0)
            {
                var context = mistakes[0].AppearsIn(selected) ? selected : mistakes[0].Contexts[0];
                return new LearningRecommendation { Mode = LearningMode.Review, Context = context,
                    Reason = "Ôn lỗi gần nhất: " + mistakes[0].Name + ". Đúng lại vật phẩm sẽ xóa lỗi đang chờ ôn." };
            }
            var tests = Tests(h, selected);
            if (tests.Length == 0) return new LearningRecommendation { Mode = LearningMode.Learn, Context = selected,
                Reason = "Chưa có kiểm tra ở " + ContextName(selected) + ". Học có hướng dẫn để làm quen trước." };
            if (Rate(tests.Last()) < 70) return new LearningRecommendation { Mode = LearningMode.Learn, Context = selected,
                Reason = "Kiểm tra gần nhất ở bối cảnh này dưới 70%. Luyện có giải thích trước khi kiểm tra lại." };
            var next = (LearningContext)(((int)selected + 1) % 3);
            return new LearningRecommendation { Mode = LearningMode.Test, Context = next,
                Reason = "Kiểm tra gần nhất đạt ít nhất 70%, không còn lỗi chờ ôn. Thử vận dụng ở " + ContextName(next) + "." };
        }

        public static string Confusions(LearningHistory h)
        {
            var errors = h.sessions.Where(s => s.mode == "Test")
                .SelectMany(s => s.answers ?? Array.Empty<LearningAnswer>()).Where(a => !a.correct)
                .GroupBy(a => new { a.expected, a.choice }).OrderByDescending(g => g.Count()).Take(3);
            var lines = errors.Select(g => Label(g.Key.expected) + " → " + Label(g.Key.choice) + ": " + g.Count() + " lần").ToArray();
            return lines.Length == 0 ? "Chưa có nhầm lẫn trong kiểm tra." : string.Join("\n", lines);
        }
        static string Label(string name) => Enum.TryParse<WasteType>(name, out var type) ? MaterialName(type) : name;
        static string MaterialName(WasteType type) => type==WasteType.Plastic?"Nhựa":type==WasteType.Metal?"Kim loại":type==WasteType.Paper?"Giấy":"Hữu cơ";

        public static string ContextProgress(LearningHistory h, LearningContext context)
        {
            var tests = Tests(h, context);
            if (tests.Length == 0) return ContextName(context) + ": chưa kiểm tra";
            var last = tests.Last();
            string result = ContextName(context) + ": " + Rate(last).ToString("F0") + "% • " + tests.Length + " lượt";
            if (tests.Length > 1)
            {
                float delta = Rate(last) - Rate(tests[tests.Length - 2]);
                result += " • " + (delta > 0 ? "+" : "") + delta.ToString("F0") + " điểm %";
            }
            return result;
        }
        public static string MaterialProgress(LearningHistory h, LearningContext context)
        {
            var tests = Tests(h, context);
            return string.Join("\n", Enum.GetValues(typeof(WasteType)).Cast<WasteType>().Select(type =>
            {
                var answers = tests.SelectMany(s => s.answers ?? Array.Empty<LearningAnswer>())
                    .Where(a => a.expected == type.ToString()).ToArray();
                var attempts=tests.Select(s=>(s.answers??Array.Empty<LearningAnswer>()).Where(a=>a.expected==type.ToString()).ToArray()).Where(a=>a.Length>0).ToArray();
                string trend="";
                if(attempts.Length>1)
                {
                    var last=attempts[attempts.Length-1];var previous=attempts[attempts.Length-2];
                    float delta=100f*last.Count(a=>a.correct)/last.Length-100f*previous.Count(a=>a.correct)/previous.Length;
                    trend="\nGần nhất: "+(delta>0?"+":"")+delta.ToString("F0")+" điểm %";
                }
                return MaterialName(type) + ": " + answers.Count(a => a.correct) + "/" + answers.Length +
                    (answers.Length == 0 ? " • chưa có dữ liệu" : " • " + (100f * answers.Count(a => a.correct) / answers.Length).ToString("F0") + "%")+trend;
            }));
        }
    }
}
