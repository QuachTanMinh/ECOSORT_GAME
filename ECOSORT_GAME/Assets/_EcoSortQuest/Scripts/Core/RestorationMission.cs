using System;
using UnityEngine;

namespace EcoSortQuest
{
    public enum PreparationAction { EmptyContents, FlattenDryPaper, Ready, SeparateSleeve }

    // Conditions belong to this mission scenario, not to the model's material label.
    public static class PreparationRules
    {
        public static PreparationAction Required(string id)
        {
            switch (id)
            {
                case "water-bottle": case "plastic-cup": case "drink-can":
                case "food-can": case "food-container": case "yogurt-pot":
                    return PreparationAction.EmptyContents;
                case "cardboard": case "paper-box": return PreparationAction.FlattenDryPaper;
                default: return PreparationAction.Ready;
            }
        }
        public static string Condition(string id) => Required(id) == PreparationAction.EmptyContents
            ? "Tình huống: vật chứa vẫn còn nước hoặc phần thức ăn thừa. Cần làm rỗng trước khi thu gom."
            : Required(id) == PreparationAction.FlattenDryPaper
            ? "Tình huống: hộp giấy sạch, khô nhưng còn cồng kềnh. Hãy chuẩn bị để tiết kiệm chỗ thu gom."
            : "Tình huống: vật phẩm đã được tách riêng, sạch hoặc sẵn sàng thu gom. Không cần xử lý thêm.";
        public static string Label(PreparationAction action) => action == PreparationAction.EmptyContents
            ? "Làm rỗng phần còn lại" : action == PreparationAction.FlattenDryPaper
            ? "Làm phẳng giấy sạch, giữ khô" : action == PreparationAction.SeparateSleeve
            ? "Tách túi giấy và cốc" : "Đã sẵn sàng, không xử lý thêm";
        public static string Clue(WasteType type) => type == WasteType.Plastic
            ? "Quan sát thân vật phẩm: bề mặt nhẹ, có thể có đường khuôn đúc."
            : type == WasteType.Metal ? "Quan sát thân vật phẩm: có ánh kim, mép gấp hoặc nắp dập."
            : type == WasteType.Paper ? "Quan sát thân vật phẩm: có thớ sợi, nếp gấp hoặc lớp bìa."
            : "Quan sát nguồn gốc: phần thực phẩm hoặc thực vật, đã tách khỏi bao bì.";
    }

    public sealed class RestorationMission
    {
        public const int Total = 12;
        readonly int target;
        public RestorationMission(int total=Total) { if(total<1)throw new ArgumentOutOfRangeException(nameof(total));target=total; }
        public int Cleaned { get; private set; }
        public int PreparationMistakes { get; private set; }
        public int Stage => Math.Min(3, Cleaned * 3 / target);
        public bool Complete => Cleaned == target;
        public string Objective => Cleaned < 4 ? "MỞ LẠI LỐI ĐI • Dọn 4 vật phẩm"
            : Cleaned < 8 ? "HỒI SINH VƯỜN HOA • Dọn 8 vật phẩm"
            : Cleaned < 12 ? "HOÀN THIỆN GÓC XANH • Dọn đủ 12 vật phẩm"
            : "KHU VỰC ĐÃ ĐƯỢC PHỤC HỒI";
        public bool Prepare(string id, PreparationAction action)
        {
            if (action == PreparationRules.Required(id)) return true;
            PreparationMistakes++;
            return false;
        }
        public void WrongPreparation() { PreparationMistakes++; }
        public bool CleanOne()
        {
            if (Complete) return false;
            Cleaned++;
            return true;
        }
        public int Stars(int firstTryCorrect) => !Complete ? 0
            : firstTryCorrect == Total && PreparationMistakes == 0 ? 3
            : firstTryCorrect >= 9 ? 2 : 1;
    }

    [Serializable]
    public sealed class RestorationRecord
    {
        public int bestStars, completions;
        public string lastSession = "";
    }
    public static class RestorationSave
    {
        public static string Key(string player, LearningContext context) => "EcoSortQuest.Restoration.v1." + player + "." + context;
        public static RestorationRecord Load(string player, LearningContext context)
        {
            try { return JsonUtility.FromJson<RestorationRecord>(PlayerPrefs.GetString(Key(player, context), "")) ?? new RestorationRecord(); }
            catch (ArgumentException) { return new RestorationRecord(); }
        }
        public static RestorationRecord Complete(string player, LearningContext context, string session, int stars)
        {
            var record = Load(player, context);
            if (record.lastSession == session) return record;
            record.lastSession = session;
            record.completions++;
            record.bestStars = Math.Max(record.bestStars, stars);
            PlayerPrefs.SetString(Key(player, context), JsonUtility.ToJson(record));
            PlayerPrefs.Save();
            return record;
        }
    }
}
