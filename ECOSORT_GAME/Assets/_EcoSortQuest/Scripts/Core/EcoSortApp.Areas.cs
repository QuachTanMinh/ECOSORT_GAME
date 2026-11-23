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
            view.Text(panel,"HÀNH TRÌNH DỌN DẸP",.035f,.865f,.78f,.09f,33);
            view.Text(panel,"Trang 1/2 • 9 nhiệm vụ • "+data.missions.Sum(m=>m.bestStars)+"/54 sao • Sổ tay: "+data.discovered.Count+"/36 mục",.035f,.785f,.92f,.065f,21,ViewFactory.Accent);
            for(int i=0;i<3;i++)
            {
                var selected=(LearningContext)i;
                bool open=AreaJourneyStore.Unlocked(data,AreaMissions.MainJourney.First(m=>m.Context==selected));
                view.Button(panel,AreaMissions.Label(selected)+(open?"":" • KHÓA"),.035f+i*.315f,.67f,.30f,.085f,()=>ShowAreaHub(selected),selected==context);
            }
            DrawMissionCards(panel,data,AreaMissions.MainJourney.Where(m=>m.Context==context).ToArray());
            view.Text(panel,context==LearningContext.Home?"NHÀ Ở • Nhặt rác sinh hoạt, xử lý trước khi bỏ thùng và thử lại thoải mái.":context==LearningContext.School?"TRƯỜNG HỌC • Trả lời câu hỏi rồi phân loại rác ở ba khu vực.":"CÔNG VIÊN • Đi xa hơn, nhặt 8–12 món đồ và làm xanh khu vực.",.035f,.145f,.92f,.07f,20);
            view.Text(panel,"Còn 3 khu mới, mỗi khu có 3 nhiệm vụ",.035f,.085f,.50f,.045f,18,ViewFactory.Accent);
            view.Button(panel,"Khu mới  →",.66f,.08f,.30f,.065f,()=>ShowPracticeJourney(ExtraEnvironment.Hospital),true);
            view.Button(panel,"Phần thưởng và huy hiệu",.035f,.02f,.40f,.055f,()=>{Destroy(panel.gameObject);ShowAreaRewards();});
            view.Button(panel,"Đóng",.80f,.02f,.16f,.055f,()=>Destroy(panel.gameObject));
        }
        void ShowPracticeJourney(ExtraEnvironment environment)
        {
            if(areaOverlay!=null)Destroy(areaOverlay.gameObject);
            areaOverlay=view.Panel(view.Root,.04f,.09f,.92f,.79f,ViewFactory.Cream,"New locations journey");
            var panel=areaOverlay;var data=AreaJourneyStore.Load(progress.player_id);
            view.Text(panel,"HÀNH TRÌNH DỌN DẸP",.035f,.865f,.78f,.09f,33);
            view.Text(panel,"Trang 2/2 • 3 khu mới • Mỗi khu có 3 nhiệm vụ • "+data.missions.Sum(m=>m.bestStars)+"/54 sao",.035f,.785f,.92f,.065f,21,ViewFactory.Accent);
            var environments=new[]{ExtraEnvironment.Hospital,ExtraEnvironment.Supermarket,ExtraEnvironment.Beach};
            for(int i=0;i<environments.Length;i++)
            {
                var selected=environments[i];var route=AreaMissions.ForEnvironment(selected);
                bool open=route.Length>0 && AreaJourneyStore.Unlocked(data,route[0]);
                view.Button(panel,ExtraEnvironmentInfo.Title(selected)+(open?"":" • KHÓA"),.035f+i*.315f,.67f,.30f,.085f,()=>ShowPracticeJourney(selected),selected==environment);
            }
            DrawMissionCards(panel,data,AreaMissions.ForEnvironment(environment));
            view.Text(panel,environment==ExtraEnvironment.Hospital?"BỆNH VIỆN • Chỉ nhặt rác sinh hoạt sạch. Không chạm vào vật sắc nhọn hay rác y tế.":environment==ExtraEnvironment.Supermarket?"SIÊU THỊ • Giữ lối đi rộng rãi. Làm rỗng đồ chứa và tách vật liệu trước khi bỏ thùng.":"BÃI BIỂN • Đi dọc bờ cát, giữ khu nghỉ sạch và đưa rác về trạm thu gom.",.035f,.145f,.92f,.07f,20);
            view.Button(panel,"← Khu vực chính",.035f,.02f,.28f,.065f,()=>ShowAreaHub(SelectedContext));
            view.Button(panel,"Phần thưởng và huy hiệu",.34f,.02f,.31f,.065f,()=>{Destroy(panel.gameObject);DrawAreaRewards(false,0,()=>ShowPracticeJourney(environment));});
            view.Button(panel,"Đóng",.80f,.02f,.16f,.065f,()=>Destroy(panel.gameObject));
        }
        void DrawMissionCards(RectTransform panel,AreaJourney data,AreaMission[] missions)
        {
            for(int i=0;i<missions.Length;i++)
            {
                var mission=missions[i];var record=data.missions.Find(r=>r.id==mission.Id);
                bool open=AreaJourneyStore.Unlocked(data,mission);
                var card=view.Panel(panel,.035f+i*.315f,.235f,.30f,.405f,new Color(.88f,.93f,.88f),mission.Id);
                view.Text(card,mission.Title,.055f,.79f,.89f,.17f,25);
                view.Text(card,mission.Brief+"\n\n"+mission.Items.Length+" món đồ • Cứ thong thả, không bị tính giờ",.055f,.28f,.89f,.49f,20);
                view.Text(card,"Cao nhất: "+(record?.bestStars??0)+"/3 sao",.055f,.17f,.89f,.09f,20,ViewFactory.Accent);
                var button=view.Button(card,open?"Bắt đầu":"Hoàn thành nhiệm vụ trước",.055f,.035f,.89f,.12f,()=>BeginAreaMission(mission),true);
                button.interactable=open;
            }
        }
        void BeginAreaMission(AreaMission mission)
        {
            if(busy || !AreaJourneyStore.Unlocked(AreaJourneyStore.Load(progress.player_id),mission))return;
            SelectedAreaMission=mission.Id;SelectedContext=mission.Context;SelectedLearningMode=LearningMode.Explore;
            AiMission=false;Navigate("GameScene");
        }
        void ShowAreaRewards()=>DrawAreaRewards(false,0,ShowAreaHub);

        void DrawAreaRewards(bool showMissions,int region,System.Action back)
        {
            if(areaOverlay!=null)Destroy(areaOverlay.gameObject);
            var panel=areaOverlay=view.Panel(view.Root,.04f,.09f,.92f,.79f,ViewFactory.Cream,"Area rewards");
            var data=AreaJourneyStore.Load(progress.player_id);
            var badges=new[]{
                ("Nhận biết bốn vật liệu","Phân loại đúng Nhựa, Kim loại, Giấy và Hữu cơ trong hành trình."),
                ("Tiến bộ từng ngày","Chơi lại một nhiệm vụ và cải thiện số sao hoặc số lượt phân loại đúng."),
                ("Chăm chỉ ôn tập","Trả lời đúng tất cả câu trong một lượt Ôn câu sai."),
                ("Người giữ góc xanh","Hoàn thành nhiệm vụ cuối ở Công viên.")
            };
            int earned=badges.Count(b=>data.badges.Contains(b.Item1));
            int completed=AreaMissions.All.Count(m=>AreaJourneyStore.Completed(data,m.Id));
            view.Text(panel,"PHẦN THƯỞNG VÀ HUY HIỆU",.04f,.88f,.92f,.09f,33);
            view.Text(panel,$"{earned}/{badges.Length} huy hiệu   •   {completed}/{AreaMissions.All.Length} nhiệm vụ hoàn thành",.04f,.805f,.92f,.06f,21,ViewFactory.Accent);
            view.Button(panel,"Huy hiệu",.04f,.71f,.44f,.075f,()=>DrawAreaRewards(false,region,back),!showMissions);
            view.Button(panel,"Phần thưởng theo khu vực",.52f,.71f,.44f,.075f,()=>DrawAreaRewards(true,region,back),showMissions);
            Color Surface(bool unlocked)=>unlocked?new Color(.87f,.94f,.87f):new Color(.92f,.93f,.92f);
            if(!showMissions)
            {
                for(int i=0;i<badges.Length;i++)
                {
                    var (title,condition)=badges[i];
                    bool unlocked=data.badges.Contains(title);
                    var card=view.Card(panel,.04f+(i%2)*.48f,.415f-(i/2)*.275f,.44f,.255f,Surface(unlocked),title);
                    view.Text(card,unlocked?"ĐÃ ĐẠT":"CHƯA ĐẠT",.06f,.71f,.88f,.20f,18,unlocked?ViewFactory.Accent:ViewFactory.Muted);
                    view.Text(card,title,.06f,.46f,.88f,.22f,24);
                    view.Text(card,condition,.06f,.10f,.88f,.31f,19,ViewFactory.Muted);
                }
            }
            else
            {
                string[] regions={"Nhà ở","Trường học","Công viên","Bệnh viện","Siêu thị","Bãi biển"};
                var extras=new[]{ExtraEnvironment.Hospital,ExtraEnvironment.Supermarket,ExtraEnvironment.Beach};
                for(int i=0;i<regions.Length;i++)
                {
                    int choice=i;
                    view.Button(panel,regions[i],.04f+i*.155f,.61f,.145f,.07f,()=>DrawAreaRewards(true,choice,back),i==region);
                }
                var missions=region<3?AreaMissions.MainJourney.Where(m=>m.Context==(LearningContext)region).ToArray():AreaMissions.ForEnvironment(extras[region-3]);
                for(int i=0;i<missions.Length;i++)
                {
                    var mission=missions[i];
                    bool unlocked=AreaJourneyStore.Completed(data,mission.Id);
                    var card=view.Card(panel,.04f+i*.31f,.275f,.30f,.295f,Surface(unlocked),mission.Id);
                    view.Text(card,unlocked?"ĐÃ NHẬN":"CHƯA NHẬN",.06f,.72f,.88f,.20f,18,unlocked?ViewFactory.Accent:ViewFactory.Muted);
                    view.Text(card,mission.Reward,.06f,.36f,.88f,.34f,23);
                    view.Text(card,mission.Title,.06f,.07f,.88f,.25f,18,ViewFactory.Muted);
                }
                if(region<3)
                {
                    void Toggle(int kind)
                    {
                        if(kind==0)data.plants=!data.plants;else if(kind==1)data.flowers=!data.flowers;else data.benches=!data.benches;
                        AreaJourneyStore.Save(progress.player_id,data);
                        DrawAreaRewards(true,region,back);
                    }
                    view.Button(panel,"Cây: "+(data.plants?"BẬT":"TẮT"),.04f,.175f,.30f,.07f,()=>Toggle(0));
                    view.Button(panel,"Hoa: "+(data.flowers?"BẬT":"TẮT"),.35f,.175f,.30f,.07f,()=>Toggle(1));
                    view.Button(panel,"Ghế: "+(data.benches?"BẬT":"TẮT"),.66f,.175f,.30f,.07f,()=>Toggle(2));
                    view.Text(panel,"Hiển thị cây, hoa và ghế đã mở tại ba khu vực chính.",.04f,.10f,.92f,.055f,18,ViewFactory.Muted);
                }
                else view.Text(panel,"Hoàn thành nhiệm vụ để ghi nhận thành quả tại địa điểm này.",.04f,.12f,.92f,.085f,19,ViewFactory.Muted);
            }
            view.Button(panel,"Về hành trình",.04f,.015f,.44f,.075f,()=>{Destroy(panel.gameObject);back();},true);
            view.Button(panel,"Đóng",.52f,.015f,.44f,.075f,()=>Destroy(panel.gameObject));
        }
        void RequestAreaHint()
        {
            if(areaMission==null || resultShown || paused || missionBrief!=null || preparationPanel!=null || settingsRoot!=null)return;
            if(carried==null){explorationPrompt.text="Tìm món đồ trong khu có biển "+areaMission.Zone+". Lại gần rồi nhấn E.";}
            else {missionHints++;explorationPrompt.text="Gợi ý: "+carried.Definition.Name+" thuộc nhóm "+WasteInfo.Label(carried.type)+". "+PreparationRules.Label(PreparationRules.Required(carried.Definition.Id));}
            feedbackUntil=Time.unscaledTime+6;RefreshExplorationHud();
        }
        string CompleteAreaMission()
        {
            var record=AreaJourneyStore.Complete(progress.player_id,areaMission,restorationSession,cleaned,round.Correct,
                restoration.PreparationMistakes+quizMistakes,missionHints,learningAnswers.Where(a=>a.correct).Select(a=>a.expected));
            if(record!=null && areaMission.Environment==ExtraEnvironment.None)
                world.RefreshAreaDecorations(AreaJourneyStore.Decorations(AreaJourneyStore.Load(progress.player_id),areaMission.Context),AreaJourneyStore.Load(progress.player_id));
            return areaMission.Title+" • "+AreaJourneyStore.Stars(round.Total,cleaned,round.Correct,restoration.PreparationMistakes+quizMistakes,missionHints)+"/3 SAO\n"+
                "Đã dọn "+cleaned+"/"+round.Total+" • Đúng lần đầu "+round.Correct+"/"+round.Total+
                " • Gợi ý: "+missionHints+"\nSai chuẩn bị / câu hỏi: "+(restoration.PreparationMistakes+quizMistakes)+
                " • Kỷ lục: "+(record?.bestStars??0)+"/3\nThành quả: "+areaMission.Reward;
        }
    }
}
