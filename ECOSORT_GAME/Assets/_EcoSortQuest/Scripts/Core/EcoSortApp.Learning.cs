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
        void ShowLearningHub()
        {
            if (learningOverlay != null) Destroy(learningOverlay.gameObject);
            learningOverlay = view.Panel(view.Root,.04f,.08f,.92f,.80f,ViewFactory.Cream,"Learning hub");
            var panel=learningOverlay;
            view.Text(panel,"HỌC • KIỂM TRA • ÔN TẬP",.04f,.87f,.92f,.09f,34);
            view.Text(panel,"Bối cảnh: "+ContextLabel+" • Mỗi lượt 10 câu • Kiểm tra 75 giây, không gợi ý",.04f,.77f,.92f,.07f,23);
            for(int i=0;i<3;i++)
            {
                var context=(LearningContext)i;
                string label=i==0?"Nhà ở":i==1?"Trường học":"Công viên";
                view.Button(panel,label,.04f+i*.31f,.65f,.29f,.09f,()=>{SelectedContext=context;ShowLearningHub();},SelectedContext==context);
            }
            view.Text(panel,"Học: có gợi ý và giải thích. Kiểm tra: ẩn đáp án đến cuối lượt.\nÔn tập: luyện lại vật phẩm sai gần đây; trả lời đúng sẽ gỡ khỏi danh sách cần ôn.",.04f,.46f,.92f,.14f,24);
            void Begin(LearningMode mode)
            {
                if(busy)return;
                if(mode==LearningMode.Review && LearningStore.Mistakes(learning).Count==0)
                {SetStatus("Chưa có câu sai cần ôn. Hãy làm một lượt học hoặc kiểm tra.");return;}
                SelectedLearningMode=mode; Navigate("GameScene");
            }
            view.Button(panel,"Học có hướng dẫn",.04f,.33f,.29f,.09f,()=>Begin(LearningMode.Learn),true);
            view.Button(panel,"Kiểm tra",.35f,.33f,.29f,.09f,()=>Begin(LearningMode.Test));
            view.Button(panel,"Ôn câu sai",.66f,.33f,.29f,.09f,()=>Begin(LearningMode.Review));
            view.Button(panel,"Kết quả học tập",.04f,.19f,.44f,.09f,()=>ShowLearningStats());
            view.Button(panel,"Nhiệm vụ AI",.51f,.19f,.44f,.09f,()=>{AiMission=true;Navigate("ScanScene");});
            view.Button(panel,"Khám phá 3D",.04f,.055f,.44f,.09f,()=>Begin(LearningMode.Explore),true);
            view.Button(panel,"Đóng",.51f,.055f,.44f,.09f,()=>Destroy(panel.gameObject));
        }
        void ShowLearningStats()
        {
            if(learningOverlay!=null)Destroy(learningOverlay.gameObject);
            learningOverlay=view.Panel(view.Root,.04f,.08f,.92f,.80f,ViewFactory.Cream,"Learning statistics");
            var panel=learningOverlay;
            view.Text(panel,"KẾT QUẢ HỌC TẬP",.04f,.87f,.92f,.09f,34);
            view.Text(panel,LearningStore.Summary(learning),.04f,.48f,.92f,.36f,24);
            var recent=learning.sessions.Skip(Math.Max(0,learning.sessions.Count-4)).Reverse();
            string history=string.Join("\n",recent.Select(s=>s.created_at+" • "+s.mode+" / "+s.context+" • "+s.answers.Count(a=>a.correct)+"/"+s.total+" • "+s.score+" điểm"));
            view.Text(panel,"4 LƯỢT GẦN NHẤT\n"+(history.Length==0?"Chưa có lượt hoàn thành.":history),.04f,.22f,.92f,.23f,21);
            view.Button(panel,"Ôn tập / chọn chế độ",.04f,.06f,.44f,.10f,ShowLearningHub,true);
            view.Button(panel,"Gửi các lượt còn chờ",.51f,.06f,.44f,.10f,()=>{if(!busy)StartCoroutine(WithBusy(SyncLearning()));});
        }
        IEnumerator SyncLearning()
        {
            if(service.IsMock){SetStatus("Lịch sử được lưu trên máy. Chọn Online để gửi backend thật.");yield break;}
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
            SetStatus("Đã gửi "+sent+" lượt. Lịch sử chi tiết nằm ở backend local, không ghi thêm feed Adafruit.");
        }
        void RecordLearningRound()
        {
            var entry=new LearningSession{player_id=progress.player_id,mode=SelectedLearningMode.ToString(),context=SelectedContext.ToString(),
                created_at=DateTime.UtcNow.ToString("O"),level=SelectedLevel,total=round.Total,score=round.Score,
                expired=round.Answered<round.Total,answers=learningAnswers.ToArray()};
            LearningStore.Add(learning,entry);LearningStore.Save(progress.player_id,learning);
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
