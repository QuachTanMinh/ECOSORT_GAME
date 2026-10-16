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
        void StartExploration()
        {
            AiMission=false;SelectedLevel=1;sessionLimit=0;learningAnswers.Clear();answerSeconds=0;
            round=new SortingRound(12,9);
            BuildLearningContext();world.PrepareWalkableWorld();
            sceneCamera.orthographic=false;sceneCamera.fieldOfView=58;sceneCamera.farClipPlane=100;
            explorer=world.Explorer(new Vector3(0,.1f,-5),sceneCamera);
            SpawnBins(new Vector3(0,0,3.8f));
            explorationBins=worldRoot.GetComponentsInChildren<WasteBin>();
            var deck=LearningDeck.Build(SelectedContext,12,Random.Range(0,int.MaxValue));
            for(int i=0;i<deck.Length;i++)
            {
                // Four columns inside the plaza; no item is placed inside a building or garden.
                var item=world.CatalogItem(deck[i],new Vector3(-4.5f+(i%4)*3,.04f,-5.8f+(i/4)*2.7f));
                item.transform.localScale=Vector3.one*.65f;scattered.Add(item);
            }
            view.Header("KHÁM PHÁ 3D / "+ContextLabel,Mode,Home);
            var top=view.Panel(view.Root,.02f,.80f,.96f,.09f,ViewFactory.Cream);
            explorationHud=view.Text(top,"",.025f,.1f,.74f,.8f,25);
            view.Button(top,"Pause / ESC",.80f,.16f,.18f,.68f,TogglePause);
            var bottom=view.Panel(view.Root,.02f,.08f,.96f,.13f,ViewFactory.Cream);
            explorationPrompt=view.Text(bottom,"",.02f,.12f,.96f,.8f,24);
            Footer("WASD / phím mũi tên: đi • Shift: chạy • Chuột phải: xoay camera • E: nhặt / bỏ vào thùng • Q: đặt xuống");
            RefreshExplorationHud();explorer.CanMove=true;explorer.Tick(0);
        }
        void RefreshExplorationHud()
        {explorationHud.text="ĐÃ DỌN "+cleaned+"/12   •   ĐÚNG LẦN ĐẦU "+round.Correct+"/12   •   ĐIỂM "+round.Score;}
        void UpdateExploration()
        {
            explorer.CanMove=!paused && !resultShown && settingsRoot==null;
            if(!explorer.CanMove)return;
            explorer.Tick(Time.deltaTime);answerSeconds+=Time.deltaTime;
            if(Time.unscaledTime>=feedbackUntil)
            {
                if(carried!=null)
                    explorationPrompt.text="Đang cầm: "+carried.Definition.Name+" • Đến gần thùng rồi nhấn E. Q: đặt xuống.";
                else
                {
                    var near=NearestLitter();
                    explorationPrompt.text=near!=null?"E • Nhặt "+near.Definition.Name:"Đi tìm rác quanh sân. Đến gần vật phẩm để nhặt.";
                }
            }
            var key=Keyboard.current;if(key==null)return;
            if(key.eKey.wasPressedThisFrame)InteractExploration();
            if(key.qKey.wasPressedThisFrame)PutDownExploration();
        }
        float FlatDistance(Vector3 a,Vector3 b) {a.y=b.y=0;return Vector3.Distance(a,b);}
        WasteItem NearestLitter()=>scattered.Where(i=>i!=null && i!=carried && FlatDistance(i.transform.position,explorer.transform.position)<1.8f)
            .OrderBy(i=>FlatDistance(i.transform.position,explorer.transform.position)).FirstOrDefault();
        readonly HashSet<WasteItem> answeredLitter=new HashSet<WasteItem>();
        void InteractExploration()
        {
            if(paused || resultShown || settingsRoot!=null)return;
            if(carried==null)
            {
                carried=NearestLitter();if(carried==null)return;
                carriedAnswered=answeredLitter.Contains(carried);
                carried.GetComponent<Collider>().enabled=false;carried.Select(false);
                carried.transform.SetParent(explorer.CarryAnchor,false);carried.transform.localPosition=Vector3.zero;
                carried.transform.localScale=Vector3.one*.55f;gameAudio.Pickup();return;
            }
            var bin=explorationBins.OrderBy(b=>FlatDistance(b.transform.position,explorer.transform.position)).FirstOrDefault();
            if(bin==null || FlatDistance(bin.transform.position,explorer.transform.position)>2.2f)
            {explorationPrompt.text="Đến gần một thùng phân loại rồi nhấn E.";feedbackUntil=Time.unscaledTime+1.5f;return;}
            bool correct=carried.type==bin.type;
            if(!carriedAnswered)
            {
                round.Submit(carried.type,bin.type);carriedAnswered=true;answeredLitter.Add(carried);
                learningAnswers.Add(new LearningAnswer{item_id=carried.Definition.Id,expected=carried.type.ToString(),choice=bin.type.ToString(),correct=correct,seconds=answerSeconds});answerSeconds=0;
            }
            gameAudio.Drop();gameAudio.FeedbackAfterDrop(correct);
            explorationPrompt.text=correct?"ĐÃ BỎ ĐÚNG THÙNG • "+carried.Definition.Lesson:
                "THÙNG CHƯA ĐÚNG • "+WasteInfo.Label(carried.type)+". Giữ rác và thử lại; điểm chỉ tính lần chọn đầu.";
            feedbackUntil=Time.unscaledTime+3;
            if(correct)
            {
                scattered.Remove(carried);Destroy(carried.gameObject);carried=null;cleaned++;
                if(cleaned==round.Total)FinishRound();
            }
            RefreshExplorationHud();
        }
        void PutDownExploration()
        {
            if(carried==null || paused || resultShown || settingsRoot!=null)return;
            carried.transform.SetParent(worldRoot,true);
            // Place at the player's feet; this position has already passed world collision checks.
            var p=explorer.transform.position;p.y=.04f;carried.transform.position=p;
            carried.transform.localScale=Vector3.one*.65f;carried.GetComponent<Collider>().enabled=true;carried=null;
        }
    }
}
