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
            view.Button(panel,"Phần thưởng và huy hiệu",.34f,.02f,.31f,.065f,()=>{Destroy(panel.gameObject);ShowAreaRewards();});
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
        void ShowAreaRewards()
        {
            var panel=view.Panel(view.Root,.04f,.09f,.92f,.79f,ViewFactory.Cream,"Area rewards");
            var data=AreaJourneyStore.Load(progress.player_id);
            view.Text(panel,"PHẦN THƯỞNG CỦA BẠN",.04f,.86f,.92f,.10f,33);
            view.Text(panel,"HUY HIỆU\n"+(data.badges.Count==0?"Chưa có huy hiệu. Hãy phân loại đúng bốn nhóm rác, ôn lại thật tốt hoặc phá kỷ lục của bạn.":string.Join(" • ",data.badges)),.04f,.64f,.92f,.20f,23);
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
            view.Text(panel,"Trang trí đã mở sẽ hiện khi bạn quay lại cùng khu vực. Tiến trình được lưu trên máy theo từng người chơi.",.04f,.10f,.92f,.075f,19,ViewFactory.Muted);
            view.Button(panel,"Về hành trình",.04f,.015f,.44f,.075f,()=>{Destroy(panel.gameObject);ShowAreaHub();},true);
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
