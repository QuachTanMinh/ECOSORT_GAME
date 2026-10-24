using UnityEngine;
namespace EcoSortQuest
{
    public sealed partial class WorldFactory
    {
        public void BuildAreaIdentity(LearningContext context,string zone,int rewards,AreaJourney data)
        {
            Color color=context==LearningContext.Home?new Color(.83f,.71f,.52f):context==LearningContext.School?new Color(.41f,.64f,.77f):new Color(.37f,.61f,.34f);
            var sign=new GameObject("Area identity sign",typeof(TextMesh));sign.transform.SetParent(root,false);
            sign.transform.localPosition=new Vector3(0,2.8f,5.3f);
            var text=sign.GetComponent<TextMesh>();text.text=zone;text.anchor=TextAnchor.MiddleCenter;text.fontSize=48;text.characterSize=.07f;text.color=ViewFactory.Ink;
            Shape("Area welcome board",PrimitiveType.Cube,root,new Vector3(0,2.8f,5.5f),new Vector3(9,.9f,.12f),color,false);
            // Distinct activity spaces: compact home corner, school workstations, park search trail.
            if(context==LearningContext.Home)
                Shape("Home work mat",PrimitiveType.Cube,root,new Vector3(0,-.025f,-3),new Vector3(7,.025f,6),new Color(.87f,.80f,.64f),false);
            if(context==LearningContext.Home)
            {
                Shape("Home preparation table",PrimitiveType.Cube,root,new Vector3(9,.7f,-4),new Vector3(2,.15f,2),new Color(.70f,.47f,.28f),false);
                Shape("Home table base",PrimitiveType.Cube,root,new Vector3(9,.3f,-4),new Vector3(1,.6f,1),color,false);
            }
            if(context==LearningContext.School)
            {
                bool canteen=zone=="CĂNG TIN",classroom=zone=="LỚP HỌC";
                Shape(classroom?"Class blackboard":canteen?"Canteen counter":"School notice board",PrimitiveType.Cube,root,
                    new Vector3(9,classroom?1.5f:.8f,-4),new Vector3(2,classroom?1.7f:1.1f,1),classroom?new Color(.15f,.35f,.29f):color,false);
                for(int i=0;i<3;i++)Shape("School zone marker",PrimitiveType.Cube,root,new Vector3(-5+i*5,-.025f,-4),new Vector3(3.4f,.025f,5),i%2==0?color:new Color(.87f,.78f,.47f),false);
            }
            if(context==LearningContext.Park)
                for(int i=0;i<4;i++)Shape("Park trail marker",PrimitiveType.Cube,root,new Vector3(-6+i*4,-.025f,-7.8f),new Vector3(1.2f,.025f,.3f),color,false);
            RefreshAreaDecorations(rewards,data);
        }
        public void RefreshAreaDecorations(int rewards,AreaJourney data)
        {
            foreach(var child in root.GetComponentsInChildren<Transform>())
                if(child.name.StartsWith("Earned ")){child.gameObject.SetActive(false);Object.Destroy(child.gameObject);}
            var color=new Color(.24f,.53f,.43f);
            for(int side=-1;side<=1;side+=2)
            {
                if(rewards>=1 && data.plants)
                {
                    Shape("Earned plant pot",PrimitiveType.Cylinder,root,new Vector3(side*10,.3f,-5),new Vector3(.8f,.3f,.8f),new Color(.64f,.37f,.23f),false);
                    Shape("Earned green tree",PrimitiveType.Sphere,root,new Vector3(side*10,1.2f,-5),new Vector3(1.2f,1.5f,1.2f),ViewFactory.Accent,false);
                }
                if(rewards>=2 && data.flowers)
                    for(int i=0;i<4;i++)Shape("Earned flower",PrimitiveType.Sphere,root,new Vector3(side*10,.45f,-3+i*.65f),Vector3.one*.4f,new Color(.95f,.65f,.29f),false);
                if(rewards>=3 && data.benches)
                {
                    Shape("Earned bench seat",PrimitiveType.Cube,root,new Vector3(side*10,.6f,1.7f),new Vector3(1.4f,.2f,1),color,false);
                    Shape("Earned bench base",PrimitiveType.Cube,root,new Vector3(side*10,.25f,1.7f),new Vector3(1,.5f,.7f),ViewFactory.Ink,false);
                }
            }
        }
    }
}
