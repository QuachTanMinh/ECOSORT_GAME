using System;
using UnityEngine;

namespace EcoSortQuest
{
    // Uses the existing canvas and input system; no parallel menu or duplicate world root.
    public sealed class MainMenuButtons:MonoBehaviour
    {
        ViewFactory view;Action<ExtraEnvironment> begin;Action dashboard;RectTransform panel;
        public void Configure(ViewFactory factory,Action<ExtraEnvironment> onBegin,Action onDashboard)
        {view=factory;begin=onBegin;dashboard=onDashboard;}
        public void Show()
        {
            if(view==null || panel!=null)return;
            panel=view.ModalCard(.22f,.18f,.56f,.64f,"Extra environments");
            view.Text(panel,"KHÁM PHÁ THÊM",.06f,.82f,.88f,.11f,32);
            view.Text(panel,"Mỗi nơi có 12 vật phẩm sinh hoạt để thu gom và phân loại. Không xử lý rác y tế hoặc vật sắc nhọn.",.06f,.65f,.88f,.15f,22,ViewFactory.Muted);
            for(int i=1;i<=3;i++)
            {
                var environment=(ExtraEnvironment)i;
                view.Button(panel,ExtraEnvironmentInfo.Title(environment),.06f,.51f-(i-1)*.13f,.88f,.10f,()=>begin?.Invoke(environment),true);
            }
            view.Button(panel,"Bảng kết quả",.06f,.07f,.42f,.10f,()=>dashboard?.Invoke());
            view.Button(panel,"Đóng",.52f,.07f,.42f,.10f,()=>{Destroy(panel.gameObject);panel=null;});
        }
    }
}
