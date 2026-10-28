using UnityEngine;
using UnityEngine.UI;

namespace EcoSortQuest
{
    // Size the background to actual text, including feedback changed during play.
    public sealed class UiContentFit : MonoBehaviour
    {
        RectTransform panel, parent;
        Text[] lines;
        float fraction, minimum;
        string[] previousText;
        float previousWidth=-1;
        public void Configure(float maxWidthFraction,float minWidth,Text[] content)
        {
            panel=(RectTransform)transform;parent=(RectTransform)transform.parent;
            lines=content;fraction=maxWidthFraction;minimum=minWidth;previousText=new string[lines.Length];
            var origin=panel.anchorMin;panel.anchorMin=panel.anchorMax=origin;panel.pivot=Vector2.zero;
            foreach(var line in lines){line.resizeTextForBestFit=false;line.horizontalOverflow=HorizontalWrapMode.Wrap;}
            Reflow();
        }
        void LateUpdate()=>Reflow();
        void Reflow()
        {
            if(panel==null || parent==null || lines==null)return;
            bool changed=!Mathf.Approximately(previousWidth,parent.rect.width);
            for(int i=0;i<lines.Length;i++)if(lines[i]!=null && previousText[i]!=lines[i].text)changed=true;
            if(!changed)return;
            previousWidth=parent.rect.width;
            for(int i=0;i<lines.Length;i++)previousText[i]=lines[i]!=null?lines[i].text:null;
            float width=minimum;
            foreach(var line in lines)if(line!=null && !string.IsNullOrEmpty(line.text))width=Mathf.Max(width,line.preferredWidth+28);
            width=Mathf.Min(width,Mathf.Max(80,parent.rect.width*fraction));
            float height=20;
            for(int i=lines.Length-1;i>=0;i--)
            {
                var line=lines[i];if(line==null)continue;
                var rect=line.rectTransform;rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=Vector2.zero;
                if(string.IsNullOrEmpty(line.text)){rect.sizeDelta=Vector2.zero;continue;}
                var settings=line.GetGenerationSettings(new Vector2(width-28,0));
                float textHeight=Mathf.Ceil(line.cachedTextGeneratorForLayout.GetPreferredHeight(line.text,settings)/line.pixelsPerUnit);
                rect.anchoredPosition=new Vector2(14,height-10);rect.sizeDelta=new Vector2(width-28,textHeight+2);
                height+=textHeight+5;
            }
            panel.sizeDelta=new Vector2(width,height-5);
        }
    }
}
