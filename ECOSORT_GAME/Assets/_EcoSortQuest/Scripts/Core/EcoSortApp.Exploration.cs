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
        void StartExploration()
        {
            AiMission=false;SelectedLevel=1;sessionLimit=0;learningAnswers.Clear();answerSeconds=0;
            areaMission=AreaMissions.Find(SelectedAreaMission);
            if(areaMission!=null && !AreaJourneyStore.Unlocked(AreaJourneyStore.Load(progress.player_id),areaMission))
            {SelectedAreaMission="";Home();return;}
            if(areaMission!=null)SelectedContext=areaMission.Context;
            int total=areaMission?.Items.Length??RestorationMission.Total;
            round=new SortingRound(total,Mathf.CeilToInt(total*.75f));
            restoration=new RestorationMission(total);
            restorationSession=System.Guid.NewGuid().ToString("N");
            if(areaMission!=null)sceneryLayout=world.BuildMissionEnvironment(areaMission);
            else {BuildLearningContext();world.PrepareWalkableWorld();}
            sceneCamera.orthographic=false;sceneCamera.fieldOfView=58;sceneCamera.farClipPlane=100;
            explorer=world.Explorer(sceneryLayout!=null?sceneryLayout.Spawn:new Vector3(0,.1f,-5),sceneCamera);
            if(sceneryLayout!=null)
            {
                explorer.WalkBounds=sceneryLayout.WalkBounds;
                explorer.CameraOffset=sceneryLayout.Indoor?new Vector3(0,8.2f,-10):new Vector3(0,7,-9);
                explorer.CameraFocusBounds=Rect.MinMaxRect(-4.5f,-4,4.5f,2);
                sceneCamera.backgroundColor=sceneryLayout.Indoor?new Color(.71f,.78f,.79f):new Color(.64f,.81f,.90f);
            }
            SpawnBins(sceneryLayout!=null?sceneryLayout.Collection:new Vector3(0,0,3.8f),sceneryLayout!=null?sceneryLayout.BinScale:1);
            explorationBins=worldRoot.GetComponentsInChildren<WasteBin>();
            var deck=LearningDeck.Build(SelectedContext,12,Random.Range(0,int.MaxValue));
            // Ensure every mission teaches both container preparation and dry-paper handling.
            deck[0]=WasteCatalog.All.First(d=>d.Id=="water-bottle");
            deck[1]=WasteCatalog.All.First(d=>d.Id=="paper-box");
            deck[2]=WasteCatalog.All.First(d=>d.Id=="drink-can");
            deck[3]=WasteCatalog.All.First(d=>d.Id=="plastic-cup");
            deck[4]=WasteCatalog.All.First(d=>d.Id=="paper-bag");
            deck[5]=WasteCatalog.All.First(d=>d.Id=="banana-peel");
            if(areaMission!=null)deck=areaMission.Items.Select(id=>WasteCatalog.All.First(d=>d.Id==id)).ToArray();
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
            view.Header(areaMission!=null?areaMission.Zone:"KHÁM PHÁ 3D / "+ContextLabel,Mode,Home,true);
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
            prepareButton=view.Button(view.Root,"Quan sát / xử lý • R",.76f,.04f,.225f,.05f,ShowPreparation);
            restorationStages=areaMission==null?world.BuildRestorationRewards():new GameObject[0];
            if(areaMission!=null)
            {
                var journey=AreaJourneyStore.Load(progress.player_id);
                world.BuildAreaIdentity(areaMission.Context,areaMission.Zone,AreaJourneyStore.Decorations(journey,areaMission.Context),journey);
                view.Button(view.Root,"Gợi ý vật đang cầm • H",.76f,.105f,.225f,.05f,RequestAreaHint);
            }
            Footer("WASD: đi • Shift: chạy • Chuột phải: xoay • E: nhặt / bỏ thùng • R: quan sát / chuẩn bị • Q: đặt xuống");
            RefreshExplorationHud();explorer.CanMove=true;explorer.Tick(0);
            ShowMissionBrief();
        }
        Vector3 AreaItemPosition(int index)
        {
            if(sceneryLayout!=null)return sceneryLayout.Items[index];
            if(areaMission==null)return new Vector3(-4.5f+(index%4)*3,.04f,-5.8f+(index/4)*2.7f);
            if(areaMission.Context==LearningContext.Home)return new Vector3(-2.4f+(index%2)*4.8f,.04f,-4.5f+(index/2)*2.8f);
            if(areaMission.Context==LearningContext.School)return new Vector3(-4.5f+(index%4)*3,.04f,-5.5f+(index/4)*3.2f);
            // Authored search route remains inside the walkable boundary and clear of scenery.
            var route=new[]{new Vector3(-7,0,-7),new Vector3(0,0,-7),new Vector3(7,0,-7),new Vector3(7,0,-3),
                new Vector3(0,0,-3),new Vector3(-7,0,-3),new Vector3(-8.5f,0,0),new Vector3(8.5f,0,0),
                new Vector3(-4,0,-5),new Vector3(4,0,-5),new Vector3(-4,0,1),new Vector3(4,0,1)};
            return route[index]+Vector3.up*.04f;
        }
        void RefreshExplorationHud()
        {
            explorationHud.text="ĐÃ DỌN "+cleaned+"/"+round.Total+"   •   ĐÚNG LẦN ĐẦU "+round.Correct+"/"+round.Total+(areaMission!=null?"   •   GỢI Ý "+missionHints:"   •   ĐIỂM "+round.Score);
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
                if(carried!=null)
                    explorationPrompt.text="Đang cầm: "+carried.Definition.Name+
                        (preparedLitter.Contains(carried)?" • Đã chuẩn bị. Đến thùng rồi nhấn E.":" • R: quan sát / chuẩn bị trước khi bỏ thùng.");
                else
                {
                    var near=NearestLitter();
                    explorationPrompt.text=near!=null?"E • Nhặt "+near.Definition.Name:sceneryLayout!=null && sceneryLayout.Indoor?"Quan sát quanh phòng. Đến gần vật phẩm rồi nhấn E để nhặt.":"Quan sát quanh khu vực. Đến gần vật phẩm rồi nhấn E để nhặt.";
                }
            }
            var key=Keyboard.current;if(key==null)return;
            if(key.eKey.wasPressedThisFrame)InteractExploration();
            if(key.qKey.wasPressedThisFrame)PutDownExploration();
            if(key.rKey.wasPressedThisFrame)ShowPreparation();
            if(key.hKey.wasPressedThisFrame)RequestAreaHint();
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
                AreaJourneyStore.Discover(progress.player_id,carried.Definition.Id);
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
                if(areaMission==null && cleaned%4==0)
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
