using UnityEngine;
using System.Linq;

namespace EcoSortQuest
{
    public sealed partial class EcoSortApp
    {
        public static ExtraEnvironment SelectedExtraEnvironment=ExtraEnvironment.None;
        ExtraEnvironment extraEnvironment;
        string RestorationScopeId=>extraEnvironment==ExtraEnvironment.None?progress.player_id:
            progress.player_id+".extra."+(areaMission!=null?areaMission.Id:extraEnvironment.ToString());
        void BeginExtraEnvironment(ExtraEnvironment environment)
        {
            if(busy)return;
            var first=AreaMissions.ForEnvironment(environment).FirstOrDefault();
            if(first==null)return;
            SelectedExtraEnvironment=environment;SelectedAreaMission=first.Id;SelectedLearningMode=LearningMode.Explore;
            SelectedContext=environment==ExtraEnvironment.Beach?LearningContext.Park:LearningContext.Home;Navigate("GameScene");
        }
        void OpenDashboard()
        {
            string address=apiSettings!=null?apiSettings.baseUrl:"http://127.0.0.1:8000";
            if(!System.Uri.TryCreate(address,System.UriKind.Absolute,out var uri) || (uri.Scheme!="http" && uri.Scheme!="https"))
            {SetStatus("Thiết lập địa chỉ kết nối trước khi mở bảng kết quả.");return;}
            Application.OpenURL(address.TrimEnd('/')+"/dashboard?player_id="+UnityEngine.Networking.UnityWebRequest.EscapeURL(progress.player_id));
        }
    }
}
