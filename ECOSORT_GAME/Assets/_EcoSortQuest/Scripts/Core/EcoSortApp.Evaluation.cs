using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EcoSortQuest
{
    public sealed partial class EcoSortApp
    {
        string EvaluationKey=>"EcoSortQuest.Evaluation.v1."+progress.player_id+"."+SelectedContext;
        EvaluationPair LoadEvaluation()
        {
            try{return JsonUtility.FromJson<EvaluationPair>(PlayerPrefs.GetString(EvaluationKey,""))??new EvaluationPair();}
            catch(ArgumentException){return new EvaluationPair();}
        }
        void BuildEvaluationPanel(RectTransform panel)
        {
            for(int i=0;i<3;i++)
            {
                var context=(LearningContext)i;
                view.Button(panel,LearningInsights.ContextName(context),.04f+i*.31f,.65f,.29f,.075f,()=>{SelectedContext=context;ShowLearningStatsPage(4);},context==SelectedContext);
            }
            var pair=LoadEvaluation();var tests=LearningInsights.Tests(learning,SelectedContext).Where(LearningEvaluation.Valid).ToArray();
            var latest=tests.LastOrDefault();
            string details="Chưa ghi lượt đầu. Làm Kiểm tra 10 câu rồi quay lại ghi nhận.";
            if(pair.before!=null)details="Lượt đầu: "+LearningInsights.Rate(pair.before).ToString("F0")+"% • "+
                (DateTimeOffset.TryParse(pair.before.created_at,out var date)?date.ToLocalTime().ToString("dd/MM/yyyy HH:mm"):"đã ghi nhận");
            if(LearningEvaluation.CanPair(pair.before,pair.after))details+="\nLượt sau: "+LearningInsights.Rate(pair.after).ToString("F0")+"% • Chênh lệch "+
                (LearningInsights.Rate(pair.after)-LearningInsights.Rate(pair.before)).ToString("+0;-0;0")+" điểm phần trăm";
            view.Text(panel,details,.04f,.44f,.92f,.17f,23);
            view.Text(panel,"Trình tự: kiểm tra → ghi lượt đầu → luyện tập → kiểm tra lại → ghi lượt sau.\nHai lượt cùng bối cảnh và 75 giây. Bộ câu có thể khác; kết quả chỉ theo dõi tiến bộ của bạn.",.04f,.29f,.92f,.13f,21,ViewFactory.Muted);
            var before=view.Button(panel,"Ghi lượt đầu",.04f,.20f,.29f,.075f,()=>
            {
                if(latest==null)return;
                PlayerPrefs.SetString(EvaluationKey,JsonUtility.ToJson(new EvaluationPair{before=latest}));PlayerPrefs.Save();ShowLearningStatsPage(4);
            });before.interactable=latest!=null;
            var after=view.Button(panel,"Ghi lượt sau",.35f,.20f,.29f,.075f,()=>
            {
                if(!LearningEvaluation.CanPair(pair.before,latest))return;
                pair.after=latest;PlayerPrefs.SetString(EvaluationKey,JsonUtility.ToJson(pair));PlayerPrefs.Save();ShowLearningStatsPage(4);
            });after.interactable=LearningEvaluation.CanPair(pair.before,latest);
            view.Button(panel,"Làm bài kiểm tra",.66f,.20f,.29f,.075f,()=>{SelectedAreaMission="";SelectedLearningMode=LearningMode.Test;Navigate("GameScene");},true);
            var export=view.Button(panel,"Xuất bảng kết quả",.04f,.08f,.44f,.08f,()=>
            {
                try
                {
                    string directory=Path.Combine(Application.persistentDataPath,"Exports");Directory.CreateDirectory(directory);
                    string path=Path.Combine(directory,"learning-"+SelectedContext+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".csv");
                    File.WriteAllText(path,LearningEvaluation.Csv(pair),new UTF8Encoding(true));SetStatus("Đã lưu bảng kết quả tại "+path);
                }
                catch(Exception e){SetStatus("Chưa xuất được kết quả: "+e.Message);}
            });export.interactable=LearningEvaluation.CanPair(pair.before,pair.after);
            view.Button(panel,"Đóng",.52f,.08f,.43f,.08f,()=>Destroy(panel.gameObject));
        }
    }
}
