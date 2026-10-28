using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace EcoSortQuest
{
    public sealed class ViewFactory
    {
        public static readonly Color Ink = new Color(.055f, .10f, .16f);
        public static readonly Color Muted = new Color(.27f, .33f, .40f);
        public static readonly Color Accent = new Color(.015f, .36f, .29f);
        public static readonly Color Cream = new Color(.97f, .98f, .97f);
        public static readonly Color HudSurface = new Color(.055f, .095f, .15f, .97f);
        public static readonly Color HudInk = new Color(.98f, .99f, 1f);
        public static readonly Color HudMuted = new Color(.79f, .85f, .91f);
        public static readonly Color HudAccent = new Color(.48f, .95f, .76f);
        public RectTransform Root { get; }
        public Action SettingsRequested { get; set; }
        readonly Font font;
        readonly Texture2D cornerTexture;
        readonly Sprite rounded;
        public ViewFactory(Transform parent)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cornerTexture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="UI rounded corners",filterMode=FilterMode.Bilinear};
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                float dx=Mathf.Max(10.5f-x,x-20.5f,0);float dy=Mathf.Max(10.5f-y,y-20.5f,0);
                cornerTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(10.5f-Mathf.Sqrt(dx*dx+dy*dy))));
            }
            cornerTexture.Apply();
            rounded=Sprite.Create(cornerTexture,new UnityEngine.Rect(0,0,32,32),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(11,11,11,11));
            var canvas = new GameObject("EcoSort UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(parent, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            Root = canvas.GetComponent<RectTransform>();
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(parent, false);
            }
        }
        public RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(x + w, y + h);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; return rect;
        }
        public RectTransform Panel(Transform parent, float x, float y, float w, float h, Color color, string name = "Panel")
        {
            var rect = Rect(name, parent, x, y, w, h);var graphic=rect.gameObject.AddComponent<Image>();graphic.color=color;
            if(h>=.02f && !(w==1 && h==1)){graphic.sprite=rounded;graphic.type=Image.Type.Sliced;}
            return rect;
        }
        public RectTransform Card(Transform parent,float x,float y,float w,float h,Color color,string name)
        {
            var panel=Panel(parent,x,y,w,h,color,name);
            var shadow=panel.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.02f,.07f,.12f,.20f);shadow.effectDistance=new Vector2(0,-5);
            return panel;
        }
        public void FitContent(RectTransform panel,float maxWidthFraction,float minWidth,params Text[] lines)
        {
            var fit=panel.gameObject.AddComponent<UiContentFit>();fit.Configure(maxWidthFraction,minWidth,lines);
        }
        public RectTransform ModalCard(float x,float y,float w,float h,string name)
        {
            var shade=Panel(Root,0,0,1,1,new Color(.025f,.055f,.09f,.48f),name+" backdrop");
            var card=Card(Root,x,y,w,h,Cream,name);
            card.gameObject.AddComponent<UiModalBackdrop>().Backdrop=shade.gameObject;
            return card;
        }
        public void Dispose(){UnityEngine.Object.Destroy(rounded);UnityEngine.Object.Destroy(cornerTexture);}
        public Text Text(Transform parent, string content, float x, float y, float w, float h, int size = 24,
            Color? color = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rect = Rect("Text", parent, x, y, w, h); var text = rect.gameObject.AddComponent<Text>();
            text.font = font; text.text = content; text.fontSize = size; text.color = color ?? Ink;
            var surface=parent.GetComponentInParent<Image>();
            if(surface!=null && surface.color==HudSurface)
                text.color=!color.HasValue || color.Value==Ink?HudInk:color.Value==Muted?HudMuted:color.Value==Accent?HudAccent:color.Value;
            text.alignment = align; text.raycastTarget = false; text.supportRichText = false;
            text.lineSpacing=1.10f;if(size>=28)text.fontStyle=FontStyle.Bold;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = Mathf.Min(16, size); text.resizeTextMaxSize = size;
            return text;
        }
        public Button Button(Transform parent, string title, float x, float y, float w, float h, Action clicked,
            bool primary = false)
        {
            var rect = Panel(parent, x, y, w, h, primary ? Accent : new Color(.88f, .92f, .94f), title);
            var border=rect.gameObject.AddComponent<Outline>();border.effectColor=new Color(.22f,.33f,.40f,.28f);border.effectDistance=new Vector2(1,-1);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(.86f, .97f, 1f);
            colors.pressedColor = new Color(.65f, .81f, .89f); colors.disabledColor = new Color(.64f, .69f, .73f);
            button.colors = colors; button.onClick.AddListener(() => { EcoSortAudio.Current?.Click(); clicked?.Invoke(); });
            Text(rect, title, .05f, .09f, .9f, .82f, 22, primary ? Color.white : Ink, TextAnchor.MiddleCenter).fontStyle=FontStyle.Bold;
            return button;
        }
        public Slider VolumeSlider(Transform parent, string title, float value, float y, Action<float> changed)
        {
            var row=Rect(title,parent,.06f,y,.88f,.09f);
            var label=Text(row,title+": "+Mathf.RoundToInt(value*100)+"%",0,0,.35f,1,22);
            var track=Panel(row,.38f,.35f,.62f,.30f,new Color(.75f,.82f,.77f),title+" slider");
            var slider=track.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;
            var fill=Panel(track,0,0,1,1,Accent,"Fill");slider.fillRect=fill;
            var area=Rect("Handle area",track,0,-.8f,1,2.6f);
            var handle=Panel(area,0,0,0,1,Ink,"Handle");handle.sizeDelta=new Vector2(22,0);
            slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.value=value;
            slider.onValueChanged.AddListener(v=>{label.text=title+": "+Mathf.RoundToInt(v*100)+"%";changed(v);});
            return slider;
        }
        public InputField Input(Transform parent, string placeholder, float x, float y, float w, float h)
        {
            var rect = Panel(parent, x, y, w, h, Color.white, "Image path");
            var input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = rect.GetComponent<Image>();
            input.textComponent = Text(rect, "", .025f, .08f, .95f, .84f, 22);
            input.textComponent.supportRichText = false;
            input.placeholder = Text(rect, placeholder, .025f, .08f, .95f, .84f, 21, Muted);
            input.characterLimit = 1024; return input;
        }
        public RectTransform Header(string subtitle, string mode, Action home, bool compact = false)
        {
            if (compact)
            {
                var strip = Rect("Compact header", Root, .015f, .94f, .97f, .045f);
                var title = Panel(strip, 0, 0, .40f, 1, HudSurface, "Location chip");
                title.GetComponent<Image>().raycastTarget = false;
                var locationText=Text(title, subtitle, .035f, .08f, .93f, .84f, 20);
                FitContent(title,.40f,120,locationText);
                var connection = Panel(strip, .735f, 0, .115f, 1, HudSurface, "Connection chip");
                connection.GetComponent<Image>().raycastTarget = false;
                Text(connection, mode.StartsWith("CHƠI")?"CHƠI TRÊN MÁY":"TRỰC TUYẾN", .07f, .08f, .86f, .84f, 16, HudAccent, TextAnchor.MiddleCenter);
                if (home != null) Button(strip, "Menu", .865f, 0, .075f, 1, home);
                var settings = Button(strip, "", .95f, 0, .05f, 1, () => SettingsRequested?.Invoke());
                settings.name = "Settings gear";
                var symbol = Rect("Gear icon", settings.transform, .25f, .18f, .5f, .64f);
                symbol.gameObject.AddComponent<GearIcon>().color = Ink;
                return strip;
            }
            var header = Panel(Root, .02f, .92f, .96f, .065f, Cream, "Header");
            Text(header, "ECOSORT / QUEST", .015f, .20f, .26f, .60f, 26);
            Text(header, subtitle, .30f, .20f, .38f, .60f, 19, Muted);
            Text(header, mode.StartsWith("CHƠI")?"CHƠI TRÊN MÁY":"TRỰC TUYẾN", .69f, .20f, .14f, .60f, 16, Accent, TextAnchor.MiddleRight);
            if (home != null) Button(header, "Menu", .845f, .25f, .085f, .5f, home);
            var gear=Button(header,"",.94f,.25f,.045f,.5f,()=>SettingsRequested?.Invoke());gear.name="Settings gear";
            var icon=Rect("Gear icon",gear.transform,.2f,.12f,.6f,.76f);icon.gameObject.AddComponent<GearIcon>().color=Ink;
            return header;
        }
    }
}
