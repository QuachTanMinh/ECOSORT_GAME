using UnityEngine;

namespace EcoSortQuest
{
    public sealed partial class WorldFactory
    {
        public GameObject[] BuildRestorationRewards()
        {
            var stages=new GameObject[3];
            for(int i=0;i<3;i++)
            {
                stages[i]=new GameObject("Restoration stage "+(i+1));
                stages[i].transform.SetParent(root,false);
                stages[i].SetActive(false);
            }
            for(int side=-1;side<=1;side+=2)
            {
                Shape("Restored green verge",PrimitiveType.Cube,stages[0].transform,
                    new Vector3(side*9,-.035f,-2),new Vector3(2,.025f,10),new Color(.32f,.62f,.32f),false);
                for(int i=0;i<7;i++)
                {
                    var p=new Vector3(side*9,.3f,-6+i*1.3f);
                    Shape("Flower stem",PrimitiveType.Cylinder,stages[1].transform,p,new Vector3(.06f,.3f,.06f),new Color(.19f,.43f,.23f),false);
                    Shape("Restored flower",PrimitiveType.Sphere,stages[1].transform,p+Vector3.up*.35f,
                        new Vector3(.42f,.3f,.42f),i%2==0?new Color(1,.68f,.24f):new Color(.91f,.38f,.55f),false);
                }
                var pos=new Vector3(side*9.3f,.7f,2.6f);
                Shape("Restored seating",PrimitiveType.Cube,stages[2].transform,pos,new Vector3(1.7f,.18f,1),new Color(.24f,.53f,.43f),false);
                for(int leg=-1;leg<=1;leg+=2)
                    Shape("Seating leg",PrimitiveType.Cube,stages[2].transform,pos+new Vector3(leg*.6f,-.4f,0),new Vector3(.15f,.7f,.8f),ViewFactory.Ink,false);
            }
            var sign=new GameObject("Restored area sign",typeof(TextMesh));sign.transform.SetParent(stages[2].transform,false);
            sign.transform.localPosition=new Vector3(0,2.9f,4.8f);
            var label=sign.GetComponent<TextMesh>();label.text="GÓC XANH ĐÃ HỒI SINH";label.anchor=TextAnchor.MiddleCenter;
            label.fontSize=48;label.characterSize=.075f;label.color=ViewFactory.Accent;
            return stages;
        }
    }
}
