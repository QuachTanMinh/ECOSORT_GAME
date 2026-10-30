using System;
using System.Globalization;
using System.Text;

namespace EcoSortQuest
{
    [Serializable]public sealed class EvaluationPair
    {
        public LearningSession before,after;
    }
    public static class LearningEvaluation
    {
        public static bool Valid(LearningSession s)=>s!=null && s.mode=="Test" && s.total==10 &&
            !string.IsNullOrEmpty(s.session_id) && Enum.TryParse<LearningContext>(s.context,out _);
        public static bool CanPair(LearningSession before,LearningSession after)=>Valid(before) && Valid(after) &&
            before.context==after.context && before.session_id!=after.session_id &&
            DateTimeOffset.TryParse(before.created_at,CultureInfo.InvariantCulture,DateTimeStyles.None,out var a) &&
            DateTimeOffset.TryParse(after.created_at,CultureInfo.InvariantCulture,DateTimeStyles.None,out var b) && b>a;
        public static string Csv(EvaluationPair pair)
        {
            if(pair==null || !CanPair(pair.before,pair.after))throw new ArgumentException("Cần hai lượt kiểm tra cùng bối cảnh, lượt sau thực hiện sau lượt đầu.");
            var csv=new StringBuilder("phase,context,session_id,created_at,total,correct,accuracy_percent,item_id,expected,choice,correct_answer,seconds\r\n");
            Add("before",pair.before);Add("after",pair.after);return csv.ToString();
            void Add(string phase,LearningSession s)
            {
                var answers=s.answers??Array.Empty<LearningAnswer>();int right=0;foreach(var a in answers)if(a.correct)right++;
                for(int i=0;i<s.total;i++)
                {
                    LearningAnswer a=i<answers.Length?answers[i]:null;
                    var fields=new[]{phase,s.context,s.session_id,s.created_at,s.total.ToString(),right.ToString(),
                        (100f*right/s.total).ToString("F2",CultureInfo.InvariantCulture),a?.item_id??"",a?.expected??"",a?.choice??"",
                        a==null?"unanswered":a.correct?"true":"false",a==null?"":a.seconds.ToString("F3",CultureInfo.InvariantCulture)};
                    csv.AppendLine(string.Join(",",Array.ConvertAll(fields,Escape)));
                }
            }
        }
        static string Escape(string value)
        {
            if(value.Length>0 && "=+-@".IndexOf(value[0])>=0)value="'"+value;
            return "\""+value.Replace("\"","\"\"")+"\"";
        }
    }
}
