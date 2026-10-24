using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSortQuest
{
    public sealed partial class EcoSortApp
    {
        public static string SelectedAreaMission="";
        static bool openJourneyOnMenu;
        AreaMission areaMission;
        int missionHints, quizMistakes;
        bool quizPassed;
        Text hintText;
        Button areaStartButton;
        RectTransform areaOverlay;
        void ShowAreaHub()=>ShowAreaHub(SelectedContext);
        void ShowAreaHub(LearningContext context)
        {
            SelectedContext=context;
            if(areaOverlay!=null)Destroy(areaOverlay.gameObject);
            areaOverlay=view.Panel(view.Root,.04f,.09f,.92f,.79f,ViewFactory.Cream,"Area journey");
            var panel=areaOverlay;var data=AreaJourneyStore.Load(progress.player_id);
            view.Text(panel,"HÀNH TRÌNH GÓC XANH",.035f,.865f,.70f,.09f,33);
            view.Text(panel,"9 nhiệm vụ • "+data.missions.Sum(m=>m.bestStars)+"/27 sao • Sổ tay "+data.discovered.Count+"/36",.035f,.785f,.92f,.065f,22,ViewFactory.Accent);
            for(int i=0;i<3;i++)
            {
                var selected=(LearningContext)i;
                bool open=AreaJourneyStore.Unlocked(data,AreaMissions.All.First(m=>m.Context==selected));
                view.Button(panel,AreaMissions.Label(selected)+(open?"":" • KHÓA"),.035f+i*.315f,.67f,.30f,.085f,()=>ShowAreaHub(selected),selected==context);
            }
            var missions=AreaMissions.All.Where(m=>m.Context==context).ToArray();
            for(int i=0;i<missions.Length;i++)
            {
                var mission=missions[i];var record=data.missions.Find(r=>r.id==mission.Id);
                bool open=AreaJourneyStore.Unlocked(data,mission);
                var card=view.Panel(panel,.035f+i*.315f,.235f,.30f,.405f,new Color(.88f,.93f,.88f),mission.Id);
                view.Text(card,mission.Title,.055f,.79f,.89f,.17f,25);
                view.Text(card,mission.Brief+"\n\n"+mission.Items.Length+" vật phẩm • Không tính tốc độ",.055f,.28f,.89f,.49f,20);
                view.Text(card,"Sao tốt nhất: "+(record?.bestStars??0)+"/3",.055f,.17f,.89f,.09f,20,ViewFactory.Accent);
                var button=view.Button(card,open?"Nhận nhiệm vụ":"Hoàn thành nhiệm vụ trước",.055f,.035f,.89f,.12f,()=>BeginAreaMission(mission),true);
                button.interactable=open;
            }
            view.Text(panel,context==LearningContext.Home?"NHÀ Ở • Quan sát từng bước, chuẩn bị rác sinh hoạt, thử lại thoải mái.":context==LearningContext.School?"TRƯỜNG HỌC • Câu hỏi tình huống + phân loại tại ba khu vực.":"CÔNG VIÊN • Tìm kiếm xa hơn, thu gom 8–12 vật phẩm, phục hồi cảnh quan.",.035f,.13f,.92f,.08f,21);
            view.Button(panel,"Phần thưởng / huy hiệu",.035f,.025f,.40f,.08f,()=>{Destroy(panel.gameObject);ShowAreaRewards();});
            view.Button(panel,"Đóng",.66f,.025f,.30f,.08f,()=>Destroy(panel.gameObject));
        }
        void BeginAreaMission(AreaMission mission)
        {
            if(busy || !AreaJourneyStore.Unlocked(AreaJourneyStore.Load(progress.player_id),mission))return;
            SelectedAreaMission=mission.Id;SelectedContext=mission.Context;SelectedLearningMode=LearningMode.Explore;
            AiMission=false;Navigate("GameScene");
        }
        void ShowAreaRewards()
        {
            var panel=view.Panel(view.Root,.04f,.09f,.92f,.79f,ViewFactory.Cream,"Area rewards");
            var data=AreaJourneyStore.Load(progress.player_id);
            view.Text(panel,"THÀNH QUẢ CỦA BẠN",.04f,.86f,.92f,.10f,33);
            view.Text(panel,"HUY HIỆU\n"+(data.badges.Count==0?"Chưa có • Phân loại đúng bốn vật liệu, ôn tập đúng trọn lượt hoặc cải thiện kỷ lục.":string.Join(" • ",data.badges)),.04f,.64f,.92f,.20f,23);
            view.Text(panel,"TRANG TRÍ ĐÃ MỞ\n"+string.Join("\n",AreaMissions.All.Where(m=>AreaJourneyStore.Completed(data,m.Id)).Select(m=>AreaMissions.Label(m.Context)+": "+m.Reward))+
                (data.missions.Count==0?"Hoàn thành nhiệm vụ để nhận cây, hoa và ghế cho khu vực.":""),.04f,.31f,.92f,.31f,21);
            void Toggle(int kind)
            {
                if(kind==0)data.plants=!data.plants;else if(kind==1)data.flowers=!data.flowers;else data.benches=!data.benches;
                AreaJourneyStore.Save(progress.player_id,data);Destroy(panel.gameObject);ShowAreaRewards();
            }
            view.Button(panel,"Cây: "+(data.plants?"BẬT":"TẮT"),.04f,.19f,.29f,.08f,()=>Toggle(0));
            view.Button(panel,"Hoa: "+(data.flowers?"BẬT":"TẮT"),.355f,.19f,.29f,.08f,()=>Toggle(1));
            view.Button(panel,"Ghế: "+(data.benches?"BẬT":"TẮT"),.67f,.19f,.29f,.08f,()=>Toggle(2));
            view.Text(panel,"Trang trí đã mở sẽ xuất hiện khi vào lại nhiệm vụ cùng khu vực. Tiến trình này lưu trên máy theo người chơi.",.04f,.10f,.92f,.075f,19,ViewFactory.Muted);
            view.Button(panel,"Về hành trình",.04f,.015f,.44f,.075f,()=>{Destroy(panel.gameObject);ShowAreaHub();},true);
            view.Button(panel,"Đóng",.52f,.015f,.44f,.075f,()=>Destroy(panel.gameObject));
        }
        void RequestAreaHint()
        {
            if(areaMission==null || resultShown || paused || missionBrief!=null || preparationPanel!=null || settingsRoot!=null)return;
            if(carried==null){explorationPrompt.text="Tìm các vật phẩm trong khu vực có biển "+areaMission.Zone+"; đến gần và nhấn E.";}
            else {missionHints++;explorationPrompt.text="GỢI Ý • "+carried.Definition.Name+": "+WasteInfo.Label(carried.type)+". "+PreparationRules.Label(PreparationRules.Required(carried.Definition.Id));}
            feedbackUntil=Time.unscaledTime+6;RefreshExplorationHud();
        }
        string CompleteAreaMission()
        {
            var record=AreaJourneyStore.Complete(progress.player_id,areaMission,restorationSession,cleaned,round.Correct,
                restoration.PreparationMistakes+quizMistakes,missionHints,learningAnswers.Where(a=>a.correct).Select(a=>a.expected));
            if(record!=null)world.RefreshAreaDecorations(AreaJourneyStore.Decorations(AreaJourneyStore.Load(progress.player_id),areaMission.Context),AreaJourneyStore.Load(progress.player_id));
            return areaMission.Title+" • "+AreaJourneyStore.Stars(round.Total,cleaned,round.Correct,restoration.PreparationMistakes+quizMistakes,missionHints)+"/3 SAO\n"+
                "Đã dọn "+cleaned+"/"+round.Total+" • Đúng lần đầu "+round.Correct+"/"+round.Total+
                " • Gợi ý: "+missionHints+"\nSai chuẩn bị / câu hỏi: "+(restoration.PreparationMistakes+quizMistakes)+
                " • Kỷ lục: "+(record?.bestStars??0)+"/3\nThành quả: "+areaMission.Reward;
        }
    }
}
