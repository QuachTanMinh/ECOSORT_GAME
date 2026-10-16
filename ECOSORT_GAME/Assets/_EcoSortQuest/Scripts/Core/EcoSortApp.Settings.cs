using UnityEngine;
using UnityEngine.UI;

namespace EcoSortQuest
{
    public sealed partial class EcoSortApp
    {
        RectTransform settingsRoot,settingsBody;
        bool settingsPreviousPause;
        float settingsPreviousTime;
        void ShowSettings(int tab)
        {
            if(settingsRoot==null)
            {
                CancelDrag();settingsPreviousPause=paused;settingsPreviousTime=Time.timeScale;
                if(screen==ClientScreen.Game){paused=true;Time.timeScale=0;}
                settingsRoot=view.Panel(view.Root,0,0,1,1,new Color(.02f,.10f,.12f,.80f),"Settings modal");
            }
            if(settingsBody!=null)Destroy(settingsBody.gameObject);
            settingsBody=view.Panel(settingsRoot,.13f,.11f,.74f,.78f,ViewFactory.Cream,"Settings content");
            var panel=settingsBody;
            view.Text(panel,"CÀI ĐẶT",.05f,.88f,.76f,.08f,33);
            view.Button(panel,"Đóng",.80f,.88f,.15f,.08f,CloseSettings);
            string[] tabs={"Âm thanh","Hình ảnh","Lối chơi"};
            for(int i=0;i<3;i++){int n=i;view.Button(panel,tabs[i],.05f+i*.31f,.75f,.28f,.09f,()=>ShowSettings(n),i==tab);}
            if(tab==0)
            {
                var label=view.Text(panel,"",.05f,.64f,.90f,.07f,22);
                System.Action refresh=()=>label.text="Âm thanh: "+(PlayerPrefs.GetInt("EcoSortQuest.Sound",1)==1?"Bật":"Tắt")+" • Thay đổi tự lưu";
                refresh();
                view.Button(panel,"Bật / tắt âm thanh",.05f,.54f,.90f,.08f,()=>{PlayerPrefs.SetInt("EcoSortQuest.Sound",1-PlayerPrefs.GetInt("EcoSortQuest.Sound",1));PlayerPrefs.Save();gameAudio.Apply();refresh();});
                view.VolumeSlider(panel,"Âm lượng tổng",EcoSortAudio.Volume("Master",1),.43f,v=>gameAudio.SetVolume("Master",v));
                view.VolumeSlider(panel,"Nhạc nền",EcoSortAudio.Volume("Music",.65f),.32f,v=>gameAudio.SetVolume("Music",v));
                view.VolumeSlider(panel,"Hiệu ứng",EcoSortAudio.Volume("Effects",1),.21f,v=>gameAudio.SetVolume("Effects",v));
                view.Button(panel,"Nghe thử",.05f,.07f,.28f,.09f,()=>{gameAudio.Preview();refresh();},true);
                view.Button(panel,"Thử âm đúng",.36f,.07f,.28f,.09f,()=>gameAudio.Feedback(true));
                view.Button(panel,"Thử âm sai",.67f,.07f,.28f,.09f,()=>gameAudio.Feedback(false));
            }
            else if(tab==1)
            {
                view.Text(panel,"CHẾ ĐỘ HIỂN THỊ",.05f,.60f,.90f,.08f,26);
                view.Button(panel,"Cửa sổ",.05f,.47f,.43f,.09f,()=>SetDisplay(false));
                view.Button(panel,"Toàn màn hình",.52f,.47f,.43f,.09f,()=>SetDisplay(true));
                view.Text(panel,"BÓNG ĐỔ",.05f,.33f,.90f,.08f,26);
                var shadow=view.Button(panel,PlayerPrefs.GetInt("EcoSortQuest.Shadows",1)==1?"Bóng đổ: Bật":"Bóng đổ: Tắt",.05f,.21f,.90f,.09f,null);
                shadow.onClick.AddListener(()=>{int enabled=1-PlayerPrefs.GetInt("EcoSortQuest.Shadows",1);PlayerPrefs.SetInt("EcoSortQuest.Shadows",enabled);PlayerPrefs.Save();ApplyVisualSettings();shadow.GetComponentInChildren<Text>().text=enabled==1?"Bóng đổ: Bật":"Bóng đổ: Tắt";});
                view.Text(panel,"Bóng đổ áp dụng ngay. Chế độ cửa sổ áp dụng cho bản Windows; Editor dùng Game view.",.05f,.06f,.90f,.10f,20,ViewFactory.Muted);
            }
            else
            {
                view.Text(panel,"TỐC ĐỘ BĂNG CHUYỀN KHI LUYỆN TẬP",.05f,.60f,.90f,.08f,25);
                var label=view.Text(panel,"",.05f,.46f,.90f,.09f,23);
                System.Action refresh=()=>label.text="Hiện tại: "+PlayerPrefs.GetFloat("EcoSortQuest.BeltSpeed",1).ToString("F1")+"×";
                refresh();
                for(int i=0;i<3;i++){float speed=.6f+i*.4f;view.Button(panel,speed.ToString("F1")+"×",.05f+i*.31f,.32f,.28f,.10f,()=>{PlayerPrefs.SetFloat("EcoSortQuest.BeltSpeed",speed);PlayerPrefs.Save();refresh();});}
                view.Text(panel,"Kiểm tra giữ tốc độ chuẩn để so sánh kết quả.\nESC: đóng cài đặt và trở về đúng trạng thái trước đó.\nKhi mở cài đặt trong lượt chơi, timer và băng chuyền tạm dừng.",.05f,.08f,.90f,.19f,23,ViewFactory.Muted);
            }
        }
        void CloseSettings()
        {
            if(settingsRoot==null)return;
            Destroy(settingsRoot.gameObject);settingsRoot=null;settingsBody=null;
            paused=settingsPreviousPause;Time.timeScale=settingsPreviousTime;
        }
        void SetDisplay(bool full)
        {
            PlayerPrefs.SetInt("EcoSortQuest.Fullscreen",full?1:0);PlayerPrefs.Save();
            if(!Application.isEditor)Screen.fullScreenMode=full?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed;
            SetStatus(Application.isEditor?"Đã lưu lựa chọn cho bản Windows. Editor dùng kích thước Game view.":"Đã đổi chế độ hiển thị.");
        }
        void ApplyVisualSettings()
        {
            bool shadows=PlayerPrefs.GetInt("EcoSortQuest.Shadows",1)==1;
            foreach(var light in worldRoot.GetComponentsInChildren<Light>())light.shadows=shadows?LightShadows.Soft:LightShadows.None;
            if(!Application.isEditor && PlayerPrefs.HasKey("EcoSortQuest.Fullscreen"))Screen.fullScreenMode=PlayerPrefs.GetInt("EcoSortQuest.Fullscreen")==1?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed;
        }
    }
}
