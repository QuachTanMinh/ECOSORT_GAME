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
        AreaSceneryLayout sceneryLayout;
        WasteItem promptItem;string promptLabel;bool promptCarrying,promptPrepared;
        void StartExploration()
        {
            AiMission=false;SelectedLevel=1;sessionLimit=0;learningAnswers.Clear();answerSeconds=0;
            EnsureExplorationSelection();
            areaMission=AreaMissions.Find(SelectedAreaMission);
            extraEnvironment=areaMission!=null && areaMission.Environment!=ExtraEnvironment.None?areaMission.Environment:SelectedExtraEnvironment;
            if(extraEnvironment!=ExtraEnvironment.None)SelectedContext=extraEnvironment==ExtraEnvironment.Beach?LearningContext.Park:LearningContext.Home;
            if(areaMission!=null && !AreaJourneyStore.Unlocked(AreaJourneyStore.Load(progress.player_id),areaMission))
            {SelectedAreaMission="";Home();return;}
            if(areaMission!=null)SelectedContext=areaMission.Context;
            int extraLevel=0;
            if(extraEnvironment!=ExtraEnvironment.None && areaMission!=null && areaMission.Environment==extraEnvironment)
            {
                extraLevel=System.Array.IndexOf(AreaMissions.ForEnvironment(extraEnvironment),areaMission);
                if(extraLevel<0)throw new System.InvalidOperationException("The selected extra mission is not in its environment route.");
            }
            int total=areaMission!=null?areaMission.Items.Length:extraEnvironment!=ExtraEnvironment.None?ExtraEnvironmentInfo.ItemCount(extraEnvironment):RestorationMission.Total;
            round=new SortingRound(total,Mathf.CeilToInt(total*.75f));
            restoration=new RestorationMission(total);
            restorationSession=System.Guid.NewGuid().ToString("N");
            if(areaMission!=null && areaMission.Environment==ExtraEnvironment.None)sceneryLayout=world.BuildMissionEnvironment(areaMission);
            else if(extraEnvironment!=ExtraEnvironment.None)sceneryLayout=world.BuildExtraEnvironment(extraEnvironment,extraLevel);
            else {BuildLearningContext();world.PrepareWalkableWorld();}
            sceneCamera.orthographic=false;sceneCamera.fieldOfView=58;sceneCamera.farClipPlane=100;
            explorer=world.Explorer(sceneryLayout!=null?sceneryLayout.Spawn:new Vector3(0,.1f,-5),sceneCamera);
            if(sceneryLayout!=null)
            {
                explorer.WalkBounds=sceneryLayout.WalkBounds;
                explorer.CameraOffset=sceneryLayout.Indoor?new Vector3(0,8.2f,-10):new Vector3(0,7,-9);
                explorer.CameraFocusBounds=sceneryLayout.WalkBounds;
                sceneCamera.backgroundColor=sceneryLayout.Indoor?new Color(.71f,.78f,.79f):new Color(.64f,.81f,.90f);
            }
            SpawnBins(sceneryLayout!=null?sceneryLayout.Collection:new Vector3(0,0,3.8f),sceneryLayout!=null?sceneryLayout.BinScale:1);
            explorationBins=worldRoot.GetComponentsInChildren<WasteBin>();
            var deck=LearningDeck.Build(SelectedContext,total,Random.Range(0,int.MaxValue));
            if(areaMission!=null)
            {
                // Use the mission's authored item list directly.
                deck=areaMission.Items.Select(id=>WasteCatalog.All.FirstOrDefault(d=>d.Id==id)).Where(d=>d!=null).ToArray();
            }
            else
            {
                // Free-roam: seed the first slots with pedagogically useful items (only up to deck length).
                var seeds=new[]{"water-bottle","paper-box","drink-can","plastic-cup","paper-bag","banana-peel"};
                for(int s=0;s<seeds.Length && s<deck.Length;s++)
                {
                    var found=WasteCatalog.All.FirstOrDefault(d=>d.Id==seeds[s]);
                    if(found!=null)deck[s]=found;
                }
            }
            for(int i=0;i<deck.Length;i++)
            {
                // Four columns inside the plaza; no item is placed inside a building or garden.
                var item=world.CatalogItem(deck[i],AreaItemPosition(i));
                item.transform.localScale=Vector3.one*.65f;scattered.Add(item);
                if(areaMission==null){if(i==3)wrappedCup=item;if(i==4)paperSleeve=item;}
                else if(areaMission.Separate){if(deck[i].Id=="plastic-cup")wrappedCup=item;if(deck[i].Id=="paper-bag")paperSleeve=item;}
                if(paperSleeve==item)paperSleeve.gameObject.SetActive(false);
            }
            if(wrappedCup!=null && paperSleeve!=null)
            {
            sleeveVisual=new GameObject("Paper bag to separate");sleeveVisual.transform.SetParent(wrappedCup.transform,false);
            foreach(float side in new[]{-1f,1f})
            {
                world.Shape("Bag front/back",PrimitiveType.Cube,sleeveVisual.transform,
                    new Vector3(0,.26f,side*.4f),new Vector3(.88f,.52f,.04f),new Color(.91f,.83f,.64f),false);
                world.Shape("Bag side",PrimitiveType.Cube,sleeveVisual.transform,
                    new Vector3(side*.42f,.26f,0),new Vector3(.04f,.52f,.8f),new Color(.91f,.83f,.64f),false);
            }
            }
            view.Header(areaMission!=null?areaMission.Zone:extraEnvironment!=ExtraEnvironment.None?ExtraEnvironmentInfo.Title(extraEnvironment):"KHÁM PHÁ 3D / "+ContextLabel,Mode,Home,true);
            var top=view.Panel(view.Root,.015f,.877f,.48f,.044f,ViewFactory.HudSurface,"Exploration score");
            top.GetComponent<Image>().raycastTarget=false;
            explorationHud=view.Text(top,"",.025f,.1f,.95f,.8f,19);
            view.FitContent(top,.48f,240,explorationHud);
            view.Button(view.Root,"Tạm dừng / ESC",.86f,.877f,.125f,.044f,TogglePause);
            var bottom=view.Card(view.Root,.015f,.105f,.50f,.075f,ViewFactory.HudSurface,"Exploration prompt");
            bottom.GetComponent<Image>().raycastTarget=false;
            explorationPrompt=view.Text(bottom,"",.02f,.12f,.96f,.8f,20);
            view.FitContent(bottom,.50f,260,explorationPrompt);
            var objective=view.Panel(view.Root,.015f,.812f,.48f,.044f,ViewFactory.HudSurface,"Restoration objective");
            objective.GetComponent<Image>().raycastTarget=false;
            missionObjective=view.Text(objective,"",.025f,.05f,.95f,.9f,19);
            view.FitContent(objective,.48f,220,missionObjective);
            prepareButton=view.Button(view.Root,"Xem kỹ vật • R",.76f,.04f,.225f,.05f,ShowPreparation);
            restorationStages=extraEnvironment!=ExtraEnvironment.None?world.BuildExtraRewards():areaMission==null?world.BuildRestorationRewards():new GameObject[0];
            if(areaMission!=null && areaMission.Environment==ExtraEnvironment.None)
            {
                var journey=AreaJourneyStore.Load(progress.player_id);
                world.BuildAreaIdentity(areaMission.Context,areaMission.Zone,AreaJourneyStore.Decorations(journey,areaMission.Context),journey);
                view.Button(view.Root,"Gợi ý cho vật này • H",.76f,.105f,.225f,.05f,RequestAreaHint);
            }
            Footer("WASD: đi • Shift: chạy • Chuột phải: xoay • E: nhặt/bỏ thùng • R: xem kỹ • Q: đặt xuống");
            RefreshExplorationHud();explorer.CanMove=true;explorer.Tick(0);
            ShowMissionBrief();
        }
        Vector3 AreaItemPosition(int index)
        {
            if(sceneryLayout!=null && sceneryLayout.Items!=null && sceneryLayout.Items.Length>0)
                return sceneryLayout.Items[index % sceneryLayout.Items.Length];
            if(areaMission==null)return new Vector3(-4.5f+(index%4)*3,.04f,-5.8f+(index/4)*2.7f);
            if(areaMission.Context==LearningContext.Home)return new Vector3(-2.4f+(index%2)*4.8f,.04f,-4.5f+(index/2)*2.8f);
            if(areaMission.Context==LearningContext.School)return new Vector3(-4.5f+(index%4)*3,.04f,-5.5f+(index/4)*3.2f);
            // Authored search route remains inside the walkable boundary and clear of scenery.
            var route=new[]{new Vector3(-7,0,-7),new Vector3(0,0,-7),new Vector3(7,0,-7),new Vector3(7,0,-3),
                new Vector3(0,0,-3),new Vector3(-7,0,-3),new Vector3(-8.5f,0,0),new Vector3(8.5f,0,0),
                new Vector3(-4,0,-5),new Vector3(4,0,-5),new Vector3(-4,0,1),new Vector3(4,0,1)};
            return route[index % route.Length]+Vector3.up*.04f;
        }
        void RefreshExplorationHud()
        {
            explorationHud.text="Đã dọn: "+cleaned+"/"+round.Total+"   •   Bỏ đúng ngay: "+round.Correct+"/"+round.Total+(areaMission!=null?"   •   Gợi ý đã dùng: "+missionHints:"   •   Điểm: "+round.Score);
            missionObjective.text=(areaMission!=null?areaMission.Title:restoration.Objective)+"   ["+cleaned+"/"+round.Total+"]";
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
                bool carrying=carried!=null,prepared=carrying && preparedLitter.Contains(carried);
                var near=carrying?carried:NearestLitter();
                if(promptLabel==null || near!=promptItem || carrying!=promptCarrying || prepared!=promptPrepared)
                {
                    promptItem=near;promptCarrying=carrying;promptPrepared=prepared;
                    promptLabel=carrying?"Đang cầm: "+carried.Definition.Name+
                        (prepared?" • Đã sẵn sàng. Đến gần thùng rồi nhấn E.":" • Nhấn R để xem kỹ trước khi bỏ thùng."):
                        near!=null?"Nhấn E để nhặt "+near.Definition.Name:sceneryLayout!=null && sceneryLayout.Indoor?"Nhìn quanh phòng nhé. Lại gần một món đồ rồi nhấn E để nhặt.":"Nhìn quanh khu vực nhé. Lại gần một món đồ rồi nhấn E để nhặt.";
                }
                explorationPrompt.text=promptLabel;
            }
            var key=Keyboard.current;if(key==null)return;
            if(key.eKey.wasPressedThisFrame)InteractExploration();
            if(key.qKey.wasPressedThisFrame)PutDownExploration();
            if(key.rKey.wasPressedThisFrame)ShowPreparation();
            if(key.hKey.wasPressedThisFrame)RequestAreaHint();
        }
        float FlatDistance(Vector3 a,Vector3 b) {a.y=b.y=0;return Vector3.Distance(a,b);}
        WasteItem NearestLitter()
        {
            WasteItem nearest=null;float best=1.8f*1.8f;var origin=explorer.transform.position;
            foreach(var item in scattered)
            {
                if(item==null || item==carried || !item.gameObject.activeInHierarchy)continue;
                var delta=item.transform.position-origin;float distance=delta.x*delta.x+delta.z*delta.z;
                if(distance<best){best=distance;nearest=item;}
            }
            return nearest;
        }
        readonly HashSet<WasteItem> answeredLitter=new HashSet<WasteItem>();
        void InteractExploration()
        {
            if(paused || resultShown || settingsRoot!=null || preparationPanel!=null || missionBrief!=null)return;
            if(carried==null)
            {
                carried=NearestLitter();if(carried==null)return;
                AreaJourneyStore.Discover(progress.player_id,carried.Definition.Id);
                carriedAnswered=answeredLitter.Contains(carried);
                carried.GetComponent<Collider>().enabled=false;carried.Select(false);
                carried.transform.SetParent(explorer.CarryAnchor,false);carried.transform.localPosition=Vector3.zero;
                carried.transform.localScale=Vector3.one*.55f;gameAudio.Pickup();RefreshExplorationHud();return;
            }
            var bin=explorationBins.OrderBy(b=>FlatDistance(b.transform.position,explorer.transform.position)).FirstOrDefault();
            if(bin==null || FlatDistance(bin.transform.position,explorer.transform.position)>2.2f)
            {explorationPrompt.text="Lại gần một thùng rác rồi nhấn E.";feedbackUntil=Time.unscaledTime+1.5f;return;}
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
            explorationPrompt.text=correct?"Đúng rồi! "+carried.Definition.Lesson:
                "Chưa đúng. "+carried.Definition.Lesson+" Bạn vẫn có thể thử lại; điểm chỉ tính ở lần chọn đầu.";
            feedbackUntil=Time.unscaledTime+3;
            if(correct)
            {
                scattered.Remove(carried);preparedLitter.Remove(carried);Destroy(carried.gameObject);carried=null;cleaned++;
                restoration.CleanOne();
                for(int i=0;i<restorationStages.Length;i++)restorationStages[i].SetActive(i<restoration.Stage);
                if(extraEnvironment!=ExtraEnvironment.None && cleaned%4==0)
                {
                    explorationPrompt.text=cleaned==4?"Lối đi đã mở! Cây xanh đang trở lại.":
                        cleaned==8?"Vườn hoa đã nở! Cùng hoàn thiện góc xanh nhé.":"Khu vực đã hồi sinh. Cảm ơn bạn đã giữ môi trường sạch!";
                    feedbackUntil=Time.unscaledTime+5;
                }
                if(cleaned==round.Total)FinishRound();
            }
            RefreshExplorationHud();
        }
        void EnsureExplorationSelection()
        {
            // Opening GameScene directly must still create a complete, playable mission.
            if(!string.IsNullOrEmpty(SelectedAreaMission) || SelectedExtraEnvironment!=ExtraEnvironment.None)return;
            var data=AreaJourneyStore.Load(progress.player_id);
            var requested=AreaMissions.MainJourney.FirstOrDefault(m=>m.Context==SelectedContext);
            var fallback=requested!=null && AreaJourneyStore.Unlocked(data,requested)?requested:
                AreaMissions.MainJourney.First(m=>AreaJourneyStore.Unlocked(data,m));
            SelectedAreaMission=fallback.Id;SelectedContext=fallback.Context;
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
