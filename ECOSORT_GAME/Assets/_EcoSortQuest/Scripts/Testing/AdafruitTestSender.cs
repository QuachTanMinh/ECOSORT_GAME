using System;
using System.Collections;
using UnityEngine;

namespace EcoSortQuest
{
    // Explicit debug action. Credentials stay on the backend; never posts automatically on Play.
    public sealed class AdafruitTestSender:MonoBehaviour
    {
        public string backendUrl="http://127.0.0.1:8000";
        [Range(0,100)]public int testScore=40;
        public string LastStatus {get;private set;}="Chưa gửi điểm kiểm tra.";
        bool sending;
        [ContextMenu("Gửi điểm kiểm tra qua backend")]
        public void SendTestScore()
        {
            if(!Application.isPlaying || sending)return;
            if(!Uri.TryCreate(backendUrl,UriKind.Absolute,out var uri) || (uri.Scheme!="http" && uri.Scheme!="https"))
            {LastStatus="Cần địa chỉ backend HTTP hoặc HTTPS.";return;}
            StartCoroutine(PostScore());
        }
        IEnumerator PostScore()
        {
            sending=true;var settings=ScriptableObject.CreateInstance<ApiSettings>();settings.mode=ApiMode.Rest;settings.baseUrl=backendUrl;
            try
            {
                var service=new RestEcoSortService(settings);
                yield return service.PostScore(new ScoreRequest{player_id="debug-dashboard-20261101",display_name="Kiểm thử Dashboard",level=1,
                    score=testScore,correct_count=testScore/10},()=>LastStatus="Đã gửi điểm kiểm tra có mã debug-dashboard-20261101.",error=>LastStatus="Chưa gửi được: "+error);
                Debug.Log(LastStatus);
            }
            finally{sending=false;Destroy(settings);}
        }
    }
}
