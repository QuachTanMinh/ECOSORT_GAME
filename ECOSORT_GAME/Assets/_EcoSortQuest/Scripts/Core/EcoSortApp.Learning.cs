using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSortQuest
{
    public sealed partial class EcoSortApp
    {
        public static LearningMode SelectedLearningMode = LearningMode.Campaign;
        public static LearningContext SelectedContext = LearningContext.Home;
        public static bool AiMission;
        LearningHistory learning;
        readonly List<LearningAnswer> learningAnswers = new List<LearningAnswer>();
        float answerSeconds, sessionLimit;
        EcoSortAudio gameAudio;
        RectTransform learningOverlay;
        bool Hints => SelectedLearningMode == LearningMode.Learn || SelectedLearningMode == LearningMode.Review ||
            (SelectedLearningMode == LearningMode.Campaign && SelectedLevel == 1);
        float BeltSpeed => (.45f + SelectedLevel * .2f) *
            (SelectedLearningMode == LearningMode.Test ? 1f : PlayerPrefs.GetFloat("EcoSortQuest.BeltSpeed", 1f));
        string ContextLabel => SelectedContext == LearningContext.Home ? "NHÀ Ở" : SelectedContext == LearningContext.School ? "TRƯỜNG HỌC" : "CÔNG VIÊN";
        string LearningLabel => SelectedLearningMode == LearningMode.Explore ? "KHÁM PHÁ 3D" : SelectedLearningMode == LearningMode.Test ? "KIỂM TRA" : SelectedLearningMode == LearningMode.Learn ? "HỌC CÓ HƯỚNG DẪN" : SelectedLearningMode == LearningMode.Review ? "ÔN CÂU SAI" : "CHIẾN DỊCH";
        IEnumerator PulseBin(WasteType type)
        {
            var bin = FindObjectsByType<WasteBin>(FindObjectsSortMode.None).FirstOrDefault(b => b.type == type);
            if(bin == null) yield break;
            var original=bin.transform.localScale;
            for(float elapsed=0;elapsed<.35f;elapsed+=Time.deltaTime)
            { if(bin==null)yield break; bin.transform.localScale=original*(1+.08f*Mathf.Sin(elapsed/.35f*Mathf.PI));yield return null; }
            if(bin!=null)bin.transform.localScale=original;
        }
        void ShowCampaignLevels()
        {
            if(learningOverlay!=null)Destroy(learningOverlay.gameObject);
            learningOverlay=view.ModalCard(.23f,.27f,.54f,.46f,"Campaign selection");
            var panel=learningOverlay;
            view.Text(panel,"THỬ THÁCH PHÂN LOẠI",.06f,.77f,.88f,.13f,30);
            view.Text(panel,"Kéo rác vào thùng. Đúng ít nhất 7/10 để mở cấp độ tiếp theo.",.06f,.55f,.88f,.16f,23);
            for(int i=1;i<=3;i++)
            {
                int n=i;bool unlocked=progress.unlocked_level>=n;
                var button=view.Button(panel,"Cấp độ "+n+(unlocked?"":" • Chưa mở"),.06f+(i-1)*.30f,.31f,.28f,.15f,()=>
                {SelectedAreaMission="";SelectedLearningMode=LearningMode.Campaign;SelectedLevel=n;Navigate("GameScene");});
                button.interactable=unlocked;
            }
            view.Button(panel,"Đóng",.35f,.07f,.30f,.13f,()=>Destroy(panel.gameObject));
        }
        void ShowPracticePicker(LearningMode mode)
        {
            if(learningOverlay!=null)Destroy(learningOverlay.gameObject);
            learningOverlay=view.ModalCard(.18f,.22f,.64f,.56f,"Practice context");
            var panel=learningOverlay;
            string title=mode==LearningMode.Test?"KIỂM TRA":mode==LearningMode.Review?"ÔN CÂU SAI":"HỌC CÓ HƯỚNG DẪN";
            view.Text(panel,title,.06f,.78f,.88f,.13f,32);
            view.Text(panel,mode==LearningMode.Test?"10 câu trong 75 giây. Xem đáp án sau khi hoàn thành.":mode==LearningMode.Review?
                "Luyện lại những vật phẩm bạn còn nhầm. Trả lời đúng để xóa lỗi cần ôn.":"10 câu, không giới hạn thời gian. Có gợi ý và giải thích từng câu.",.06f,.55f,.88f,.17f,23);
            for(int i=0;i<3;i++)
            {
                var context=(LearningContext)i;
                view.Button(panel,LearningInsights.ContextName(context),.06f+i*.30f,.35f,.28f,.13f,()=>{SelectedContext=context;ShowPracticePicker(mode);},SelectedContext==context);
            }
            view.Button(panel,"Bắt đầu",.06f,.10f,.55f,.13f,()=>
            {
                if(busy)return;
                if(mode==LearningMode.Review && LearningStore.Mistakes(learning).Count==0)
                {SetStatus("Chưa có câu sai cần ôn. Hãy học hoặc làm một bài kiểm tra trước.");return;}
                SelectedAreaMission="";SelectedLearningMode=mode;Navigate("GameScene");
            },true);
            view.Button(panel,"Đóng",.66f,.10f,.28f,.13f,()=>Destroy(panel.gameObject));
        }
        void ShowLearningStats() => ShowLearningStatsPage(0);
        void ShowLearningStatsPage(int page)
        {
            if(learningOverlay!=null)Destroy(learningOverlay.gameObject);
            learningOverlay=view.ModalCard(.04f,.08f,.92f,.80f,"Learning statistics");
            var panel=learningOverlay;
            view.Text(panel,"HỌC TỪ KẾT QUẢ CỦA BẠN",.04f,.88f,.92f,.08f,32);
            string[] tabs={"Tiến bộ", "Kỹ năng vật liệu", "Danh sách ôn", "Thành tích", "Trước và sau"};
            for(int i=0;i<tabs.Length;i++) { int tab=i; view.Button(panel,tabs[i],.04f+i*.185f,.77f,.175f,.075f,()=>ShowLearningStatsPage(tab),page==i); }
            if(page==4){BuildEvaluationPanel(panel);return;}
            var recommendation=LearningInsights.Recommend(learning,SelectedContext);
            if(page==0)
            {
                var contextCard=view.Panel(panel,.04f,.32f,.45f,.42f,new Color(.88f,.93f,.88f),"Context progress card");
                foreach(LearningContext context in Enum.GetValues(typeof(LearningContext)))
                {
                    float y=.78f-(int)context*.29f;
                    view.Text(contextCard,LearningInsights.ContextProgress(learning,context),.045f,y,.91f,.18f,22);
                    var track=view.Panel(contextCard,.045f,y-.04f,.91f,.025f,new Color(.72f,.81f,.75f));
                    var tests=LearningInsights.Tests(learning,context);
                    if(tests.Length>0)view.Panel(track,0,0,Mathf.Clamp01(LearningInsights.Rate(tests.Last())/100),1,ViewFactory.Accent);
                }
                var confusionCard=view.Panel(panel,.52f,.32f,.43f,.42f,new Color(.92f,.90f,.80f),"Confusion card");
                view.Text(confusionCard,"HAY NHẦM",.06f,.76f,.88f,.16f,27);
                view.Text(confusionCard,"Đáp án → lựa chọn của bạn\n"+LearningInsights.Confusions(learning),.06f,.16f,.88f,.56f,24);
                view.Text(panel,"So với lần kiểm tra trước cùng bối cảnh. Câu bỏ trống tính vào tổng.\nCác bộ câu có thể khác nhau; chênh lệch chỉ để tham khảo tiến bộ.",.04f,.23f,.92f,.08f,21);
            }
            else if(page==1)
            {
                foreach(LearningContext context in Enum.GetValues(typeof(LearningContext)))
                {
                    var card=view.Panel(panel,.04f+(int)context*.31f,.34f,.29f,.39f,new Color(.88f,.93f,.88f));
                    view.Text(card,LearningInsights.ContextName(context),.05f,.77f,.90f,.18f,26);
                    view.Text(card,LearningInsights.MaterialProgress(learning,context),.05f,.08f,.90f,.65f,23);
                }
                view.Text(panel,"Kỹ năng đo được: nhận biết / phân loại vật liệu trong kiểm tra.\nChưa chấm riêng kỹ năng chuẩn bị và tách vật liệu. Không gộp lượt có gợi ý.",.04f,.23f,.92f,.10f,21);
            }
            else if(page==2)
            {
                var mistakes=LearningStore.Mistakes(learning);
                string rows=string.Join("\n",mistakes.Take(5).Select(d=>d.Name+" • "+WasteInfo.Label(d.Type)+" • "+string.Join(", ",d.Contexts.Select(LearningInsights.ContextName))));
                view.Text(panel,"LỖI CHƯA SỬA • MỚI NHẤT TRƯỚC\n"+(mistakes.Count==0?"Không còn lỗi cần ôn.":rows)+
                    (mistakes.Count>5?"\nCòn "+(mistakes.Count-5)+" vật phẩm trong danh sách.":""),.04f,.39f,.92f,.34f,23);
                view.Text(panel,"Đúng lại vật phẩm sẽ xóa lỗi đang chờ ôn.\nPhân loại bằng ảnh: "+learning.missions.Count(m=>m.knowledge_confirmed)+" bài xác nhận kiến thức, lưu riêng khỏi kiểm tra.",.04f,.23f,.92f,.14f,21);
            }
            else BuildAchievements(panel);
            view.Text(panel,"GỢI Ý: "+recommendation.Reason,.04f,.13f,.92f,.09f,22,ViewFactory.Accent);
            view.Button(panel,"Bắt đầu gợi ý →",.04f,.025f,.29f,.08f,()=>BeginRecommendedLearning(recommendation),true);
            view.Button(panel,"Đóng",.35f,.025f,.29f,.08f,()=>Destroy(panel.gameObject));
            view.Button(panel,"Lưu kết quả trực tuyến",.66f,.025f,.29f,.08f,()=>{if(!busy)StartCoroutine(WithBusy(SyncLearning(true)));});
        }
        void BeginRecommendedLearning(LearningRecommendation recommendation)
        {
            if(busy)return;
            SelectedAreaMission="";SelectedContext=recommendation.Context;SelectedLearningMode=recommendation.Mode;
            Navigate("GameScene");
        }
        IEnumerator SyncLearning(bool includeProgress=false)
        {
            if(service.IsMock){SetStatus("Kết quả đã lưu trên máy. Bật kết nối trong Cài đặt nếu muốn lưu trực tuyến.");yield break;}
            var rest=service as RestEcoSortService;
            if(rest==null)yield break;
            int sent=0;
            foreach(var id in learning.pending.ToArray())
            {
                var entry=learning.sessions.Find(s=>s.session_id==id);if(entry==null)continue;
                bool ok=false;string error="";
                yield return rest.PostLearning(entry,()=>ok=true,e=>error=e);
                if(!ok){SetStatus("Còn chờ gửi: "+error+" • Có thể thử lại từ Kết quả học tập.");yield break;}
                learning.pending.Remove(id);LearningStore.Save(progress.player_id,learning);sent++;
            }
            SetStatus("Đã lưu trực tuyến "+sent+" lượt học.");
            if(includeProgress)yield return service.PutProgress(progress,()=>SetStatus("Đã lưu lịch sử học và tiến trình trực tuyến."),e=>SetStatus("Lịch sử học đã lưu; tiến trình chưa gửi được: "+e));
        }
        void RecordLearningRound()
        {
            var entry=new LearningSession{player_id=progress.player_id,mode=SelectedLearningMode.ToString(),context=SelectedContext.ToString(),
                created_at=DateTime.UtcNow.ToString("O"),level=SelectedLevel,total=round.Total,score=round.Score,
                expired=round.Answered<round.Total,answers=learningAnswers.ToArray()};
            LearningStore.Add(learning,entry);LearningStore.Save(progress.player_id,learning);
            AreaJourneyStore.RecordReview(progress.player_id,entry);
        }
        void ShowAnswerReview(int page=0)
        {
            var panel=view.Panel(view.Root,.04f,.08f,.92f,.80f,ViewFactory.Cream,"Answer review");
            var answers=learningAnswers.Skip(page*5).Take(5).ToArray();
            view.Text(panel,"XEM LẠI ĐÁP ÁN • Trang "+(page+1),.04f,.88f,.92f,.08f,30);
            for(int i=0;i<answers.Length;i++)
            {
                var a=answers[i];var def=WasteCatalog.All.First(d=>d.Id==a.item_id);
                view.Text(panel,(a.correct?"ĐÚNG • ":"CẦN ÔN • ")+def.Name+" • Bạn chọn "+a.choice+" • Đáp án "+a.expected+"\n"+def.Lesson,
                    .04f,.72f-i*.13f,.92f,.12f,22);
            }
            view.Button(panel,"Đóng",.04f,.04f,.28f,.09f,()=>Destroy(panel.gameObject));
            if(page>0)view.Button(panel,"← Trước",.35f,.04f,.28f,.09f,()=>{Destroy(panel.gameObject);ShowAnswerReview(page-1);});
            if((page+1)*5<learningAnswers.Count)view.Button(panel,"Tiếp →",.66f,.04f,.28f,.09f,()=>{Destroy(panel.gameObject);ShowAnswerReview(page+1);});
        }
        void ShowExperienceSettings() => ShowSettings(0);
        void PlayFeedback(bool correct)
        {
            gameAudio?.Feedback(correct);
        }
        void BuildLearningContext()
        {
            world.BuildLearningEnvironment(SelectedContext);
            sceneCamera.backgroundColor = new Color(.77f,.87f,.92f);
        }
    }
}
