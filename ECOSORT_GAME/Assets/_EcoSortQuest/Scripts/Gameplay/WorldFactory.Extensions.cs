using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace EcoSortQuest
{
    public enum ExtraEnvironment { None,Hospital,Supermarket,Beach }
    public static class ExtraEnvironmentInfo
    {
        public static string Title(ExtraEnvironment e)=>e==ExtraEnvironment.Hospital?"Bệnh viện • Khu tiếp đón":e==ExtraEnvironment.Supermarket?"Siêu thị":e==ExtraEnvironment.Beach?"Bãi biển":"Khám phá";
        public static int ItemCount(ExtraEnvironment e)=>18;
        public static AreaSceneryLayout Layout(ExtraEnvironment e)
        {
            Vector3 P(float x,float z)=>new Vector3(x,.04f,z);
            switch(e)
            {
                case ExtraEnvironment.Hospital:
                    return new AreaSceneryLayout("extra-"+e,true,Rect.MinMaxRect(-12.4f,-8.3f,12.4f,7.2f),new Vector3(0,.1f,-7.2f),new Vector3(0,0,5.8f),
                        P(-5.8f,-6.2f),P(-2.5f,-5.7f),P(1.0f,-6.1f),P(4.4f,-5.6f),P(-6.5f,-2.8f),P(-2.8f,-2.0f),P(.5f,-2.7f),P(4.8f,-2.1f),P(-5.7f,.8f),P(-1.9f,.5f),P(1.8f,.9f),P(5.7f,.4f),P(-5.2f,3.4f),P(-1.5f,3.1f),P(2.2f,3.6f),P(5.5f,3.0f),P(-7.5f,4.5f),P(3.8f,1.4f));
                case ExtraEnvironment.Supermarket:
                    return new AreaSceneryLayout("extra-"+e,true,Rect.MinMaxRect(-13.2f,-8.3f,13.2f,7.2f),new Vector3(0,.1f,-7.2f),new Vector3(0,0,5.8f),
                        P(-4.8f,-6.2f),P(-1.6f,-5.8f),P(1.5f,-6.2f),P(4.8f,-5.8f),P(-5.2f,-3.2f),P(-1.8f,-2.5f),P(1.8f,-3.0f),P(5.1f,-2.4f),P(-4.9f,.1f),P(-1.4f,.6f),P(1.5f,.1f),P(4.8f,.7f),P(-5.5f,3.0f),P(-2.0f,3.5f),P(1.7f,3.1f),P(5.0f,3.6f),P(-6.0f,4.5f),P(2.7f,2.0f));
                case ExtraEnvironment.Beach:
                    return new AreaSceneryLayout("extra-"+e,false,Rect.MinMaxRect(-15,-10,15,8),new Vector3(0,.1f,-8.5f),new Vector3(0,0,6.3f),
                        P(-10,-7.5f),P(-6,-7.0f),P(-2,-7.8f),P(2,-7.1f),P(6,-7.7f),P(10,-7.0f),P(-10,-3.8f),P(-5.8f,-3.4f),P(-1.5f,-4.0f),P(2.5f,-3.4f),P(6.5f,-4.0f),P(10,-3.4f),P(-8.5f,.0f),P(-4,.8f),P(-3.0f,.0f),P(4,.8f),P(8.5f,.0f),P(0,5.0f));
                default:throw new ArgumentOutOfRangeException(nameof(e));
            }
        }
    }
    public sealed partial class WorldFactory
    {
        public AreaSceneryLayout BuildExtraEnvironment(ExtraEnvironment e)
        {
            switch(e){case ExtraEnvironment.Hospital:return CreateHospital();case ExtraEnvironment.Supermarket:return CreateSupermarket();case ExtraEnvironment.Beach:return CreateBeach();default:throw new ArgumentOutOfRangeException(nameof(e));}
        }
        AreaSceneryLayout BeginExtra(ExtraEnvironment environment)
        {
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            foreach(var collider in root.GetComponentsInChildren<Collider>())collider.enabled=false;
            activeLayout=ExtraEnvironmentInfo.Layout(environment);
            sceneryRoot=new GameObject("Extra scenery • "+environment).transform;sceneryRoot.SetParent(root,false);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.67f,.71f,.75f);
            foreach(var light in root.GetComponentsInChildren<Light>())if(light.type==LightType.Directional)light.intensity=activeLayout.Indoor?1.05f:1.25f;
            if(activeLayout.Indoor)BuildExpandedRoom(environment);
            else BuildBeachGround();
            Part("Extra collection station",activeLayout.Collection+new Vector3(0,.018f,0),new Vector3(activeLayout.Indoor?8.4f:10.8f,.025f,2.1f),new Color(.59f,.75f,.66f));
            return activeLayout;
        }
        void BuildExpandedRoom(ExtraEnvironment environment)
        {
            var floor=environment==ExtraEnvironment.Hospital?new Color(.72f,.80f,.79f):new Color(.78f,.74f,.65f);
            var wall=environment==ExtraEnvironment.Hospital?new Color(.83f,.91f,.91f):new Color(.94f,.88f,.72f);
            Part("Expanded room solid floor",new Vector3(0,-.15f,-.5f),new Vector3(27,.30f,17),floor,solid:true);
            for(int x=0;x<13;x++)for(int z=0;z<8;z++)
                Part("Expanded floor tile",new Vector3(-12+x*2,.008f,-7.5f+z*2),new Vector3(1.97f,.012f,1.97f),(x+z)%2==0?floor:Color.Lerp(floor,Color.white,.12f));
            void Wall(string name,Vector3 p,Vector3 size,Vector3 inward)
            {
                var section=Part(name,p,size,wall,solid:true);section.AddComponent<CutawayWall>().Inward=inward;
            }
            Wall("Expanded room back wall",new Vector3(0,2.1f,7.5f),new Vector3(27,4.2f,.24f),Vector3.back);
            Wall("Expanded room left wall",new Vector3(-13.25f,2.1f,-.5f),new Vector3(.24f,4.2f,16.2f),Vector3.right);
            Wall("Expanded room right wall",new Vector3(13.25f,2.1f,-.5f),new Vector3(.24f,4.2f,16.2f),Vector3.left);
            Part("Expanded cutaway threshold",new Vector3(0,.10f,-8.6f),new Vector3(27,.20f,.22f),Oak,solid:true);
            for(int i=0;i<3;i++)
            {
                float x=-8+i*8;
                Part("Expanded window frame",new Vector3(x,2.75f,7.32f),new Vector3(3.4f,2.0f,.16f),Color.white);
                Part("Expanded window sky",new Vector3(x,2.75f,7.20f),new Vector3(3.0f,1.6f,.08f),new Color(.44f,.75f,.85f));
                Part("Expanded window landscape",new Vector3(x,2.18f,7.13f),new Vector3(3.0f,.45f,.06f),new Color(.36f,.61f,.36f));
            }
            Part("Expanded room plaque",new Vector3(0,3.55f,7.28f),new Vector3(8.8f,.60f,.08f),new Color(.94f,.96f,.90f));
            Label("Expanded room title",ExtraEnvironmentInfo.Title(environment),new Vector3(0,3.56f,7.18f),.048f);
        }
        void BuildBeachGround()
        {
            Part("Beach sand floor",new Vector3(0,-.15f,-1f),new Vector3(34,.3f,21),new Color(.91f,.80f,.53f),solid:true);
            for(int x=-15;x<=15;x+=3)for(int z=-9;z<=6;z+=3)
                Part("Beach sand pattern",new Vector3(x,.008f,z),new Vector3(2.7f,.012f,2.7f),(x+z)%2==0?new Color(.94f,.84f,.58f):new Color(.88f,.76f,.48f));
            Part("Beach boardwalk",new Vector3(0,.025f,6.4f),new Vector3(11,.04f,2),PaleWood);
            for(int i=0;i<12;i++)Part("Boardwalk plank seam",new Vector3(-5+i, .052f,6.4f),new Vector3(.06f,.012f,2.05f),Oak);
            Part("Beach dune left",new Vector3(-14,.28f,2.2f),new Vector3(3.7f,.55f,9),new Color(.75f,.67f,.39f),shape:PrimitiveType.Sphere,solid:true);
            Part("Beach dune right",new Vector3(14,.28f,2.2f),new Vector3(3.7f,.55f,9),new Color(.75f,.67f,.39f),shape:PrimitiveType.Sphere,solid:true);
        }
        public AreaSceneryLayout CreateHospital()
        {
            var layout=BeginExtra(ExtraEnvironment.Hospital);
            Table("Reception counter",8.8f,3.9f,4.6f,1.5f,new Color(.38f,.67f,.69f));
            Part("Reception screen",new Vector3(8.8f,1.65f,4.1f),new Vector3(1.2f,.7f,.13f),ViewFactory.Ink);
            Label("Reception sign","TIẾP ĐÓN",new Vector3(8.8f,2.45f,6.45f),.05f);
            Part("Clinic cross vertical",new Vector3(4.7f,2.8f,6.4f),new Vector3(.25f,1,.08f),new Color(.72f,.18f,.21f));
            Part("Clinic cross horizontal",new Vector3(4.7f,2.8f,6.39f),new Vector3(1,.25f,.08f),new Color(.72f,.18f,.21f));
            for(int i=0;i<4;i++){Chair("Waiting chair left",-9,-5+i*2.5f);Chair("Waiting chair right",7,-5+i*2.5f);}
            Bookshelf("Public information shelf",-10.6f,5.2f);
            Part("Clinic queue marker",new Vector3(1.5f,.02f,2.2f),new Vector3(5,.025f,.7f),new Color(.90f,.78f,.42f));
            Part("Clinic hand sanitizer",new Vector3(-.8f,.75f,4.8f),new Vector3(.35f,1.5f,.35f),new Color(.36f,.72f,.78f),shape:PrimitiveType.Cylinder);
            Part("Waiting area rug",new Vector3(0,.02f,-1.0f),new Vector3(14,.025f,11),new Color(.74f,.84f,.82f));
            return layout;
        }
        public AreaSceneryLayout CreateSupermarket()
        {
            var layout=BeginExtra(ExtraEnvironment.Supermarket);
            foreach(float x in new[]{-10f,-7.5f,7.5f,10f})for(int z=0;z<3;z++)
            {
                Part("Shop shelf",new Vector3(x,1,-4+z*2.7f),new Vector3(2.1f,2,1.4f),new Color(.67f,.72f,.69f),solid:true);
                for(int row=0;row<2;row++)for(int item=0;item<4;item++)
                    Part("Packaged goods",new Vector3(x-.75f+item*.5f,.65f+row*.8f,-4.76f+z*2.7f),new Vector3(.35f,.55f,.3f),new Color(.35f+.12f*item,.65f,.37f+.2f*row));
            }
            Table("Checkout counter",8.5f,4.6f,4.2f,1.3f,Oak);
            Part("Checkout register",new Vector3(8.5f,1.5f,4.6f),new Vector3(.9f,.6f,.65f),ViewFactory.Ink);
            Part("Shopping basket stack",new Vector3(-10.5f,.65f,4.3f),new Vector3(1.4f,1.3f,1.1f),new Color(.72f,.24f,.18f));
            for(int i=0;i<3;i++)Part("Shopping basket handle",new Vector3(-10.5f,1.35f+i*.14f,4.3f),new Vector3(1.1f,.05f,.75f),Steel);
            Label("Shop sign","LỐI ĐI CHUNG",new Vector3(0,2.7f,6.45f),.05f);
            return layout;
        }
        public AreaSceneryLayout CreateBeach()
        {
            var layout=BeginExtra(ExtraEnvironment.Beach);
            Part("Sea beyond play area",new Vector3(0,-.1f,14),new Vector3(44,.18f,10),new Color(.22f,.63f,.78f));
            Part("Shore foam",new Vector3(0,.01f,8.7f),new Vector3(31,.04f,.45f),new Color(.91f,.97f,.96f));
            foreach(float x in new[]{-10f,0,10f})
            {
                Part("Umbrella pole",new Vector3(x,1.3f,-2),new Vector3(.12f,1.3f,.12f),PaleWood,shape:PrimitiveType.Cylinder,solid:true);
                Part("Umbrella canopy",new Vector3(x,2.65f,-2),new Vector3(3.4f,.65f,3.4f),new Color(.92f,.43f,.29f),shape:PrimitiveType.Sphere);
                Table("Beach rest table",x,1.6f,2.2f,1.3f,PaleWood);Chair("Beach seat",x,.3f);
                Part("Coastal palm trunk",new Vector3(x*1.2f,1.5f,3.8f),new Vector3(.35f,1.5f,.35f),Oak,shape:PrimitiveType.Cylinder,solid:true);
                Part("Coastal palm crown",new Vector3(x*1.2f,3.1f,3.8f),new Vector3(3,.7f,2.4f),new Color(.28f,.56f,.35f),shape:PrimitiveType.Sphere);
            }
            Part("Beach information board",new Vector3(0,2.6f,7.4f),new Vector3(8,.8f,.16f),new Color(.94f,.96f,.90f));
            Label("Beach title","GIỮ BÃI BIỂN SẠCH",new Vector3(0,2.6f,7.28f),.06f);
            return layout;
        }
        public GameObject[] BuildExtraRewards()
        {
            var result=new GameObject[3];
            for(int i=0;i<3;i++)
            {
                result[i]=new GameObject("Extra cleanup reward "+i);result[i].transform.SetParent(root,false);
                foreach(float x in new[]{-9f,9f})
                {
                    Shape("Cleanup flower pot",PrimitiveType.Cylinder,result[i].transform,new Vector3(x,.3f,-6+i*2),new Vector3(.65f,.3f,.65f),Oak,false);
                    Shape("Cleanup plant",PrimitiveType.Sphere,result[i].transform,new Vector3(x,.8f,-6+i*2),new Vector3(.9f,.9f,.9f),new Color(.3f,.68f,.43f),false);
                }
                result[i].SetActive(false);
            }
            return result;
        }
    }
}
