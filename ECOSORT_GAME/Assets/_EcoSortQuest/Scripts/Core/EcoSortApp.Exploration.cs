using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EcoSortQuest
{
    public sealed partial class EcoSortApp
    {
        ExplorerController explorer;
        readonly List<WasteItem> scattered=new List<WasteItem>();
        WasteItem carried;
        WasteBin[] explorationBins;
        bool carriedAnswered;
        Text explorationHud, explorationPrompt;
        int cleaned;
        float feedbackUntil;
        RestorationMission restoration;
        RectTransform preparationPanel, missionBrief;
        Text missionObjective, preparationFeedback;
        Button prepareButton;
        readonly HashSet<WasteItem> preparedLitter = new HashSet<WasteItem>();
        GameObject[] restorationStages;
        string restorationSession;
        bool viewingRestoration;
        WasteItem wrappedCup, paperSleeve;
        GameObject sleeveVisual;
        bool sleeveSeparated;
        void StartExploration()
        {
            AiMission=false;SelectedLevel=1;sessionLimit=0;learningAnswers.Clear();answerSeconds=0;
            round=new SortingRound(RestorationMission.Total,9);
            restoration=new RestorationMission();
            restorationSession=System.Guid.NewGuid().ToString("N");
            BuildLearningContext();world.PrepareWalkableWorld();
            sceneCamera.orthographic=false;sceneCamera.fieldOfView=58;sceneCamera.farClipPlane=100;
            explorer=world.Explorer(new Vector3(0,.1f,-5),sceneCamera);
            SpawnBins(new Vector3(0,0,3.8f));
            explorationBins=worldRoot.GetComponentsInChildren<WasteBin>();
            var deck=LearningDeck.Build(SelectedContext,12,Random.Range(0,int.MaxValue));
            // Ensure every mission teaches both container preparation and dry-paper handling.
            deck[0]=WasteCatalog.All.First(d=>d.Id=="water-bottle");
            deck[1]=WasteCatalog.All.First(d=>d.Id=="paper-box");
            deck[2]=WasteCatalog.All.First(d=>d.Id=="drink-can");
            deck[3]=WasteCatalog.All.First(d=>d.Id=="plastic-cup");
            deck[4]=WasteCatalog.All.First(d=>d.Id=="paper-bag");
            deck[5]=WasteCatalog.All.First(d=>d.Id=="banana-peel");
            for(int i=0;i<deck.Length;i++)
            {
                // Four columns inside the plaza; no item is placed inside a building or garden.
                var item=world.CatalogItem(deck[i],new Vector3(-4.5f+(i%4)*3,.04f,-5.8f+(i/4)*2.7f));
                item.transform.localScale=Vector3.one*.65f;scattered.Add(item);
                if(i==3)wrappedCup=item;
                if(i==4){paperSleeve=item;paperSleeve.gameObject.SetActive(false);}
            }
            sleeveVisual=new GameObject("Paper bag to separate");sleeveVisual.transform.SetParent(wrappedCup.transform,false);
            foreach(float side in new[]{-1f,1f})
            {
                world.Shape("Bag front/back",PrimitiveType.Cube,sleeveVisual.transform,
                    new Vector3(0,.26f,side*.4f),new Vector3(.88f,.52f,.04f),new Color(.91f,.83f,.64f),false);
                world.Shape("Bag side",PrimitiveType.Cube,sleeveVisual.transform,
                    new Vector3(side*.42f,.26f,0),new Vector3(.04f,.52f,.8f),new Color(.91f,.83f,.64f),false);
            }
            view.Header("KHÁM PHÁ 3D / "+ContextLabel,Mode,Home,true);
            var top=view.Panel(view.Root,.015f,.87f,.64f,.055f,ViewFactory.HudSurface);
            top.GetComponent<Image>().raycastTarget=false;
            explorationHud=view.Text(top,"",.025f,.1f,.74f,.8f,25);
            view.Button(view.Root,"Pause / ESC",.86f,.875f,.125f,.045f,TogglePause);
            var bottom=view.Panel(view.Root,.015f,.045f,.68f,.075f,ViewFactory.HudSurface);
            bottom.GetComponent<Image>().raycastTarget=false;
            explorationPrompt=view.Text(bottom,"",.02f,.12f,.96f,.8f,24);
            var objective=view.Panel(view.Root,.015f,.80f,.64f,.055f,ViewFactory.HudSurface,"Restoration objective");
            objective.GetComponent<Image>().raycastTarget=false;
            missionObjective=view.Text(objective,"",.025f,.05f,.95f,.9f,21);
            prepareButton=view.Button(view.Root,"Quan sát / xử lý • R",.73f,.055f,.255f,.055f,ShowPreparation);
            restorationStages=world.BuildRestorationRewards();
            Footer("WASD: đi • Shift: chạy • Chuột phải: xoay • E: nhặt / bỏ thùng • R: quan sát / chuẩn bị • Q: đặt xuống");
            RefreshExplorationHud();explorer.CanMove=true;explorer.Tick(0);
            ShowMissionBrief();
        }
        void RefreshExplorationHud()
        {
            explorationHud.text="ĐÃ DỌN "+cleaned+"/12   •   ĐÚNG LẦN ĐẦU "+round.Correct+"/12   •   ĐIỂM "+round.Score;
            missionObjective.text=restoration.Objective+"   ["+cleaned+"/12]";
            prepareButton.interactable=carried!=null && !resultShown;
        }
        void UpdateExploration()
        {
            explorer.CanMove=!paused && (!resultShown || viewingRestoration) && settingsRoot==null && preparationPanel==null && missionBrief==null;
            if(!explorer.CanMove)return;
            explorer.Tick(Time.deltaTime);answerSeconds+=Time.deltaTime;
            if(resultShown)return;
            if(Time.unscaledTime>=feedbackUntil)
            {
                if(carried!=null)
                    explorationPrompt.text="Đang cầm: "+carried.Definition.Name+
                        (preparedLitter.Contains(carried)?" • Đã chuẩn bị. Đến thùng rồi nhấn E.":" • R: quan sát / chuẩn bị trước khi bỏ thùng.");
                else
                {
                    var near=NearestLitter();
                    explorationPrompt.text=near!=null?"E • Nhặt "+near.Definition.Name:"Đi tìm rác quanh sân. Đến gần vật phẩm để nhặt.";
                }
            }
            var key=Keyboard.current;if(key==null)return;
            if(key.eKey.wasPressedThisFrame)InteractExploration();
            if(key.qKey.wasPressedThisFrame)PutDownExploration();
            if(key.rKey.wasPressedThisFrame)ShowPreparation();
        }
        float FlatDistance(Vector3 a,Vector3 b) {a.y=b.y=0;return Vector3.Distance(a,b);}
        WasteItem NearestLitter()=>scattered.Where(i=>i!=null && i.gameObject.activeInHierarchy && i!=carried && FlatDistance(i.transform.position,explorer.transform.position)<1.8f)
            .OrderBy(i=>FlatDistance(i.transform.position,explorer.transform.position)).FirstOrDefault();
        readonly HashSet<WasteItem> answeredLitter=new HashSet<WasteItem>();
        void InteractExploration()
        {
            if(paused || resultShown || settingsRoot!=null || preparationPanel!=null || missionBrief!=null)return;
            if(carried==null)
            {
                carried=NearestLitter();if(carried==null)return;
                carriedAnswered=answeredLitter.Contains(carried);
                carried.GetComponent<Collider>().enabled=false;carried.Select(false);
                carried.transform.SetParent(explorer.CarryAnchor,false);carried.transform.localPosition=Vector3.zero;
                carried.transform.localScale=Vector3.one*.55f;gameAudio.Pickup();RefreshExplorationHud();return;
            }
            var bin=explorationBins.OrderBy(b=>FlatDistance(b.transform.position,explorer.transform.position)).FirstOrDefault();
            if(bin==null || FlatDistance(bin.transform.position,explorer.transform.position)>2.2f)
            {explorationPrompt.text="Đến gần một thùng phân loại rồi nhấn E.";feedbackUntil=Time.unscaledTime+1.5f;return;}
            if(carried==wrappedCup && !sleeveSeparated){ShowPreparation();return;}
            if(!preparedLitter.Contains(carried) && PreparationRules.Required(carried.Definition.Id)!=PreparationAction.Ready)
            {ShowPreparation();return;}
            bool correct=carried.type==bin.type;
            if(!carriedAnswered)
            {
                round.Submit(carried.type,bin.type);carriedAnswered=true;answeredLitter.Add(carried);
                learningAnswers.Add(new LearningAnswer{item_id=carried.Definition.Id,expected=carried.type.ToString(),choice=bin.type.ToString(),correct=correct,seconds=answerSeconds});answerSeconds=0;
            }
            gameAudio.Drop();gameAudio.FeedbackAfterDrop(correct);
            explorationPrompt.text=correct?"ĐÃ BỎ ĐÚNG THÙNG • "+carried.Definition.Lesson:
                "THÙNG CHƯA ĐÚNG • "+carried.Definition.Lesson+" Giữ rác và thử lại; điểm chỉ tính lần chọn đầu.";
            feedbackUntil=Time.unscaledTime+3;
            if(correct)
            {
                scattered.Remove(carried);preparedLitter.Remove(carried);Destroy(carried.gameObject);carried=null;cleaned++;
                restoration.CleanOne();
                for(int i=0;i<restorationStages.Length;i++)restorationStages[i].SetActive(i<restoration.Stage);
                if(cleaned%4==0)
                {
                    explorationPrompt.text=cleaned==4?"LỐI ĐI ĐÃ MỞ • Các luống cây được hồi sinh!":
                        cleaned==8?"VƯỜN HOA ĐÃ NỞ • Tiếp tục hoàn thiện góc xanh!":"ĐÃ PHỤC HỒI KHU VỰC • Cảm ơn bạn đã giữ môi trường sạch!";
                    feedbackUntil=Time.unscaledTime+5;
                }
                if(cleaned==round.Total)FinishRound();
            }
            RefreshExplorationHud();
        }
        void PutDownExploration()
        {
            if(carried==null || paused || resultShown || settingsRoot!=null || preparationPanel!=null || missionBrief!=null)return;
            carried.transform.SetParent(worldRoot,true);
            // Place at the player's feet; this position has already passed world collision checks.
            var p=explorer.transform.position;p.y=.04f;carried.transform.position=p;
            carried.transform.localScale=Vector3.one*.65f;carried.GetComponent<Collider>().enabled=true;carried=null;
            RefreshExplorationHud();
        }
    }
}
