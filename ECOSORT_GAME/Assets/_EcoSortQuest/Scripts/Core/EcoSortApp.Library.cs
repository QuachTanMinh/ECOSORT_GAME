using System;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSortQuest
{
    public sealed partial class EcoSortApp
    {
        bool ownsSettings;
        const string ConnectionKey = "EcoSortQuest.Connection.";
        void LoadConnectionSettings()
        {
            // Do not alter the project asset. Tests and Editor keep explicit asset configuration.
            apiSettings = apiSettings != null ? Instantiate(apiSettings) : ScriptableObject.CreateInstance<ApiSettings>();
            ownsSettings = true;
            if (!Application.isEditor)
            {
                apiSettings.mode = (ApiMode)PlayerPrefs.GetInt(ConnectionKey + "mode", (int)ApiMode.Mock);
                apiSettings.baseUrl = PlayerPrefs.GetString(ConnectionKey + "url", "http://127.0.0.1:8000");
            }
            if (SessionMode.HasValue) { apiSettings.mode = SessionMode.Value; apiSettings.baseUrl = SessionUrl; }
        }
        void ShowConnection()
        {
            var overlay = view.Panel(view.Root, .08f, .14f, .84f, .69f, ViewFactory.Cream, "Backend connection");
            view.Text(overlay, "KẾT NỐI BACKEND", .05f,.82f,.9f,.12f,34);
            view.Text(overlay, "Ngoại tuyến: chơi phân loại; Scan dùng nhãn luyện tập, không phải AI.\nOnline: nhận diện ảnh và đồng bộ qua FastAPI. Không nhập Adafruit Key vào game.", .05f,.61f,.9f,.18f,24);
            var url = view.Input(overlay,"http://127.0.0.1:8000",.05f,.45f,.9f,.1f);
            url.text = apiSettings.baseUrl;
            var message = view.Text(overlay,"URL backend chỉ dùng khi chọn Online.",.05f,.32f,.9f,.1f,21,ViewFactory.Muted);
            void Apply(ApiMode mode)
            {
                string address = url.text.Trim().TrimEnd('/');
                if (mode == ApiMode.Rest && (!Uri.TryCreate(address,UriKind.Absolute,out var uri) ||
                    (uri.Scheme != "http" && uri.Scheme != "https") || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query)))
                { message.text="Nhập URL HTTP(S) của backend, không kèm tài khoản hoặc API key."; return; }
                PlayerPrefs.SetInt(ConnectionKey+"mode",(int)mode); PlayerPrefs.SetString(ConnectionKey+"url",address); PlayerPrefs.Save();
                apiSettings.mode=mode; apiSettings.baseUrl=address;
                // Persist only connection choice in Editor; service can change without editing ApiSettings.asset.
                SessionMode=mode; SessionUrl=address;
                Destroy(overlay.gameObject); Navigate("MainMenu");
            }
            view.Button(overlay,"Chơi ngoại tuyến",.05f,.15f,.28f,.12f,()=>Apply(ApiMode.Mock));
            view.Button(overlay,"Kết nối Online",.36f,.15f,.28f,.12f,()=>Apply(ApiMode.Rest),true);
            view.Button(overlay,"Đóng",.67f,.15f,.28f,.12f,()=>Destroy(overlay.gameObject));
        }
        static ApiMode? SessionMode;
        static string SessionUrl;
        void ShowCatalog(int page)
        {
            var overlay=view.Panel(view.Root,.03f,.09f,.94f,.79f,ViewFactory.Cream,"Waste field guide");
            view.Text(overlay,"SỔ TAY PHÂN LOẠI / "+WasteInfo.Label((WasteType)page),.035f,.86f,.85f,.1f,32);
            for(int i=0;i<9;i++)
            {
                var entry=WasteCatalog.Get((WasteType)page,i);
                float x=.035f+(i%3)*.315f, y=.60f-(i/3)*.22f;
                var card=view.Panel(overlay,x,y,.30f,.20f,new Color(.88f,.93f,.88f));
                view.Text(card,entry.Name,.04f,.64f,.92f,.30f,22);
                view.Text(card,entry.Lesson,.04f,.05f,.92f,.58f,19,ViewFactory.Muted);
            }
            for(int i=0;i<4;i++)
            { int next=i; view.Button(overlay,((WasteType)i).ToString(),.035f+i*.18f,.045f,.16f,.085f,()=>{Destroy(overlay.gameObject);ShowCatalog(next);},i==page); }
            view.Button(overlay,"Đóng",.77f,.045f,.19f,.085f,()=>Destroy(overlay.gameObject));
        }
    }
}
