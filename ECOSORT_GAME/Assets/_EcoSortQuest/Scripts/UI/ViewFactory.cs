using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace EcoSortQuest
{
    public sealed class ViewFactory
    {
        public static readonly Color Ink = new Color(.06f, .19f, .19f);
        public static readonly Color Muted = new Color(.30f, .43f, .43f);
        public static readonly Color Accent = new Color(.08f, .46f, .37f);
        public static readonly Color Cream = new Color(.96f, .96f, .91f);
        public RectTransform Root { get; }
        public Action SettingsRequested { get; set; }
        readonly Font font;
        public ViewFactory(Transform parent)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        { var rect = Rect(name, parent, x, y, w, h); rect.gameObject.AddComponent<Image>().color = color; return rect; }
        public Text Text(Transform parent, string content, float x, float y, float w, float h, int size = 24,
            Color? color = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rect = Rect("Text", parent, x, y, w, h); var text = rect.gameObject.AddComponent<Text>();
            text.font = font; text.text = content; text.fontSize = size; text.color = color ?? Ink;
            text.alignment = align; text.raycastTarget = false; text.supportRichText = false;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = Mathf.Min(16, size); text.resizeTextMaxSize = size;
            return text;
        }
        public Button Button(Transform parent, string title, float x, float y, float w, float h, Action clicked,
            bool primary = false)
        {
            var rect = Panel(parent, x, y, w, h, primary ? Accent : new Color(.88f, .93f, .88f), title);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(.82f, .94f, .85f);
            colors.pressedColor = new Color(.65f, .83f, .72f); colors.disabledColor = new Color(.7f, .73f, .7f);
            button.colors = colors; button.onClick.AddListener(() => { EcoSortAudio.Current?.Click(); clicked?.Invoke(); });
            Text(rect, title, .05f, .06f, .9f, .88f, 23, primary ? Color.white : Ink, TextAnchor.MiddleCenter);
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
        public RectTransform Header(string subtitle, string mode, Action home)
        {
            var header = Panel(Root, 0, .91f, 1, .09f, Cream, "Header");
            Text(header, "ECOSORT / QUEST", .035f, .3f, .30f, .45f, 28);
            Text(header, subtitle, .36f, .3f, .29f, .45f, 23, Muted);
            Text(header, mode, .65f, .3f, .19f, .45f, 18, Accent, TextAnchor.MiddleRight);
            if (home != null) Button(header, "Menu", .845f, .25f, .085f, .5f, home);
            var gear=Button(header,"",.94f,.25f,.045f,.5f,()=>SettingsRequested?.Invoke());gear.name="Settings gear";
            var icon=Rect("Gear icon",gear.transform,.2f,.12f,.6f,.76f);icon.gameObject.AddComponent<GearIcon>().color=Ink;
            return header;
        }
    }
}
