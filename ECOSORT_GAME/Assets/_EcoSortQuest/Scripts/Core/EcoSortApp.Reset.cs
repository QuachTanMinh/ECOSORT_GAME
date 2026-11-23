using System;
using UnityEngine;

namespace EcoSortQuest
{
    public sealed partial class EcoSortApp
    {
        RectTransform resetOverlay;
        void ShowResetProgress()
        {
            if(resetOverlay!=null)return;
            resetOverlay=view.ModalCard(.29f,.31f,.42f,.37f,"Reset progress");
            view.Text(resetOverlay,"CHƠI LẠI TỪ ĐẦU",.07f,.73f,.86f,.16f,32);
            view.Text(resetOverlay,"Bạn sẽ xóa điểm, sao, huy hiệu, sổ tay và lịch sử học trên máy này. Tên gọi và thiết lập trò chơi vẫn được giữ.",.07f,.40f,.86f,.27f,21,ViewFactory.Muted);
            view.Button(resetOverlay,"Đặt lại tiến trình",.07f,.15f,.41f,.14f,ResetAllLocalProgress,true);
            view.Button(resetOverlay,"Hủy",.52f,.15f,.41f,.14f,()=>{Destroy(resetOverlay.gameObject);resetOverlay=null;});
        }
        void ResetAllLocalProgress()
        {
            string player=progress.player_id;
            PlayerPrefs.DeleteKey(AreaJourneyStore.Key(player));
            PlayerPrefs.DeleteKey(LearningStore.Key(player));
            PlayerPrefs.DeleteKey("EcoSort.MockCloud."+player);
            foreach(LearningContext context in Enum.GetValues(typeof(LearningContext)))
            {
                PlayerPrefs.DeleteKey(RestorationSave.Key(player,context));
                PlayerPrefs.DeleteKey("EcoSortQuest.Evaluation.v1."+player+"."+context);
            }
            foreach(var mission in AreaMissions.All)
                if(mission.Environment!=ExtraEnvironment.None)
                    PlayerPrefs.DeleteKey(RestorationSave.Key(player+".extra."+mission.Id,mission.Context));
            foreach(ExtraEnvironment environment in Enum.GetValues(typeof(ExtraEnvironment)))
                if(environment!=ExtraEnvironment.None)
                {
                    var context=environment==ExtraEnvironment.Beach?LearningContext.Park:LearningContext.Home;
                    PlayerPrefs.DeleteKey(RestorationSave.Key(player+".extra."+environment,context));
                }
            progress=new PlayerProgress{player_id=player};LocalProgress.Save(progress);
            learning=LearningStore.Load(player);
            SelectedLevel=1;SelectedContext=LearningContext.Home;SelectedLearningMode=LearningMode.Campaign;
            SelectedAreaMission="";SelectedExtraEnvironment=ExtraEnvironment.None;PendingPrediction=null;AiMission=false;
            PlayerPrefs.Save();
            if(resetOverlay!=null)Destroy(resetOverlay.gameObject);resetOverlay=null;
            Navigate("MainMenu");
        }
    }
}
