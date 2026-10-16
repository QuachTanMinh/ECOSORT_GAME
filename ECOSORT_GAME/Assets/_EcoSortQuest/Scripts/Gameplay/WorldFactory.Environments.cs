using UnityEngine;

namespace EcoSortQuest
{
    public sealed partial class WorldFactory
    {
        void Decor(string name,Vector3 pos,Vector3 scale,Color color,PrimitiveType type=PrimitiveType.Cube)
        {Shape(name,type,root,pos,scale,color,false);}
        void Tree(float x,float z,float size=1)
        {
            Decor("Tree trunk",new Vector3(x,.85f,z),new Vector3(.25f,1.7f,.25f),new Color(.43f,.29f,.18f));
            Decor("Tree canopy",new Vector3(x,2.05f,z),new Vector3(1.8f,1.9f,1.8f)*size,new Color(.27f,.52f,.35f),PrimitiveType.Sphere);
            Decor("Tree canopy highlight",new Vector3(x-.35f,2.65f,z),new Vector3(1.2f,1.25f,1.2f)*size,new Color(.40f,.65f,.40f),PrimitiveType.Sphere);
        }
        void Bench(float x,float z)
        {
            var wood=new Color(.63f,.39f,.22f);
            for(int i=0;i<3;i++)Decor("Bench slat",new Vector3(x,.62f,z+i*.18f),new Vector3(2,.12f,.14f),wood);
            Decor("Bench back",new Vector3(x,1,z+.55f),new Vector3(2,.65f,.12f),wood);
            for(int i=0;i<2;i++)Decor("Bench leg",new Vector3(x+(i==0?-.7f:.7f),.3f,z+.25f),new Vector3(.12f,.6f,.6f),ViewFactory.Ink);
        }
        public void BuildLearningEnvironment(LearningContext context)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>())
            {
                if(t.name=="Tree" || t.name=="Planter" || t.name=="Sorting island")
                    foreach(var renderer in t.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            }
            var stone=new Color(.83f,.85f,.79f);var cream=new Color(.94f,.89f,.78f);var teal=new Color(.20f,.43f,.43f);
            Decor("Wide landscape ground",new Vector3(0,-.30f,0),new Vector3(42,.5f,32),context==LearningContext.Park?new Color(.51f,.68f,.40f):stone);
            Decor("Sorting plaza",new Vector3(0,-.025f,0),new Vector3(12,.04f,7.4f),new Color(.91f,.88f,.78f));
            for(int i=-5;i<=5;i++)Decor("Plaza joint",new Vector3(i,0,0),new Vector3(.018f,.012f,7.3f),new Color(.78f,.77f,.68f));
            Decor("Back walkway",new Vector3(0,.005f,4.3f),new Vector3(34,.04f,1.4f),new Color(.76f,.75f,.66f));
            for(int side=-1;side<=1;side+=2)
            {
                Decor("Raised garden",new Vector3(side*7,.16f,1),new Vector3(1.1f,.3f,5.5f),new Color(.74f,.60f,.43f));
                for(int i=0;i<4;i++)Decor("Garden shrub",new Vector3(side*7,.55f,-1+i*1.2f),new Vector3(.9f,.65f,.9f),new Color(.33f,.56f,.35f),PrimitiveType.Sphere);
            }
            if(context==LearningContext.Home)
            {
                Decor("Home facade",new Vector3(-5,1.65f,7),new Vector3(6,3.3f,2),cream);
                var roof=Shape("Home roof",PrimitiveType.Cube,root,new Vector3(-5,3.55f,7),new Vector3(6.6f,.35f,2.8f),new Color(.65f,.34f,.24f),false);
                roof.transform.localRotation=Quaternion.Euler(0,0,-5);
                Decor("Home door",new Vector3(-5,1.1f,5.97f),new Vector3(1.1f,2.2f,.06f),teal);
                foreach(float x in new[]{-7f,-3f})
                {Decor("Window frame",new Vector3(x,1.9f,5.94f),new Vector3(1.3f,1.35f,.10f),Color.white);Decor("Window glass",new Vector3(x,1.9f,5.86f),new Vector3(1.1f,1.15f,.08f),new Color(.42f,.70f,.78f));}
                Bench(4,6);Tree(8.5f,6);Tree(-9,5);
                Decor("Outdoor table",new Vector3(4,.85f,6),new Vector3(2.2f,.15f,1),teal);
            }
            else if(context==LearningContext.School)
            {
                Decor("School facade",new Vector3(0,1.7f,8),new Vector3(15,3.4f,2),cream);
                Decor("School cornice",new Vector3(0,3.45f,8),new Vector3(15.5f,.22f,2.4f),teal);
                for(int i=-3;i<=3;i++)
                {Decor("School window frame",new Vector3(i*2,2.05f,6.94f),new Vector3(1.3f,1.5f,.10f),Color.white);Decor("School glass",new Vector3(i*2,2.05f,6.86f),new Vector3(1.1f,1.3f,.08f),new Color(.42f,.68f,.78f));}
                Decor("School entrance",new Vector3(0,1.15f,6.75f),new Vector3(1.6f,2.3f,.12f),teal);
                Bench(-8,5);Bench(8,5);Tree(-10,7);Tree(10,7);
            }
            else
            {
                Bench(-5.2f,5.8f);Bench(5.2f,5.8f);
                for(int i=-2;i<=2;i++){Tree(i*4.5f,9,1.2f);}
                Decor("Pond rim",new Vector3(0,.05f,7),new Vector3(3.3f,.12f,2.1f),new Color(.65f,.69f,.63f),PrimitiveType.Cylinder);
                Decor("Pond water",new Vector3(0,.18f,7),new Vector3(3,.02f,1.8f),new Color(.29f,.65f,.76f),PrimitiveType.Cylinder);
                for(int side=-1;side<=1;side+=2)for(int i=0;i<5;i++)
                    Decor("Flower bed",new Vector3(side*8,.5f,-2+i*1.3f),new Vector3(.25f,.28f,.25f),i%2==0?new Color(.96f,.66f,.27f):new Color(.84f,.42f,.53f),PrimitiveType.Sphere);
            }
        }
    }
}
