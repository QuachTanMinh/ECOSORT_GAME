using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace EcoSortQuest
{
    public enum ExtraEnvironment { None,Hospital,Supermarket,Beach }
    public static class ExtraEnvironmentInfo
    {
        public static string Title(ExtraEnvironment e)=>e==ExtraEnvironment.Hospital?"Bệnh viện • Khu tiếp đón":e==ExtraEnvironment.Supermarket?"Siêu thị":e==ExtraEnvironment.Beach?"Bãi biển":"Khám phá";
        public static AreaSceneryLayout Layout(ExtraEnvironment e)
        {
            var points=new Vector3[12];for(int i=0;i<12;i++)points[i]=new Vector3(-4.5f+(i%4)*3,.04f,-5+(i/4)*2.8f);
            return new AreaSceneryLayout("extra-"+e,e!=ExtraEnvironment.Beach,points);
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
            if(activeLayout.Indoor)BuildRoom(new AreaMission("extra-"+environment,LearningContext.School,"",ExtraEnvironmentInfo.Title(environment),"","",""));
            else Part("Beach sand floor",new Vector3(0,-.15f,-1.1f),new Vector3(24,.3f,18),new Color(.91f,.80f,.53f),solid:true);
            Part("Extra collection station",activeLayout.Collection+new Vector3(0,.018f,0),new Vector3(activeLayout.Indoor?7.4f:10,.025f,2.1f),new Color(.59f,.75f,.66f));
            return activeLayout;
        }
        public AreaSceneryLayout CreateHospital()
        {
            var layout=BeginExtra(ExtraEnvironment.Hospital);
            Table("Reception counter",7,3.6f,4,1.5f,new Color(.38f,.67f,.69f));
            Part("Reception screen",new Vector3(7,1.65f,3.8f),new Vector3(1.2f,.7f,.13f),ViewFactory.Ink);
            Label("Reception sign","TIẾP ĐÓN",new Vector3(7,2.4f,5.72f),.05f);
            Part("Clinic cross vertical",new Vector3(3.7f,2.6f,5.7f),new Vector3(.25f,1,.08f),new Color(.72f,.18f,.21f));
            Part("Clinic cross horizontal",new Vector3(3.7f,2.6f,5.69f),new Vector3(1,.25f,.08f),new Color(.72f,.18f,.21f));
            for(int i=0;i<3;i++){Chair("Waiting chair left",-7,-4+i*2.3f);Chair("Waiting chair right",7,-4+i*2.3f);}
            Bookshelf("Public information shelf",-8.4f,4.8f);
            Part("Waiting area rug",new Vector3(0,.02f,-1.5f),new Vector3(11,.025f,9),new Color(.74f,.84f,.82f));
            return layout;
        }
        public AreaSceneryLayout CreateSupermarket()
        {
            var layout=BeginExtra(ExtraEnvironment.Supermarket);
            foreach(float x in new[]{-7.7f,7.7f})for(int z=0;z<3;z++)
            {
                Part("Shop shelf",new Vector3(x,1,-4+z*2.7f),new Vector3(2.1f,2,1.4f),new Color(.67f,.72f,.69f),solid:true);
                for(int row=0;row<2;row++)for(int item=0;item<4;item++)
                    Part("Packaged goods",new Vector3(x-.75f+item*.5f,.65f+row*.8f,-4.76f+z*2.7f),new Vector3(.35f,.55f,.3f),new Color(.35f+.12f*item,.65f,.37f+.2f*row));
            }
            Table("Checkout counter",7,4.6f,3,1.3f,Oak);
            Part("Checkout register",new Vector3(7,1.5f,4.6f),new Vector3(.9f,.6f,.65f),ViewFactory.Ink);
            Label("Shop sign","LỐI ĐI CHUNG",new Vector3(0,2.5f,5.72f),.05f);
            return layout;
        }
        public AreaSceneryLayout CreateBeach()
        {
            var layout=BeginExtra(ExtraEnvironment.Beach);
            Part("Sea beyond play area",new Vector3(0,-.1f,12),new Vector3(40,.18f,8),new Color(.22f,.63f,.78f));
            Part("Shore foam",new Vector3(0,.01f,8.2f),new Vector3(26,.04f,.45f),new Color(.91f,.97f,.96f));
            foreach(float x in new[]{-8f,8f})
            {
                Part("Umbrella pole",new Vector3(x,1.3f,-2),new Vector3(.12f,1.3f,.12f),PaleWood,shape:PrimitiveType.Cylinder,solid:true);
                Part("Umbrella canopy",new Vector3(x,2.65f,-2),new Vector3(3.4f,.65f,3.4f),new Color(.92f,.43f,.29f),shape:PrimitiveType.Sphere);
                Table("Beach rest table",x,1.6f,2.2f,1.3f,PaleWood);Chair("Beach seat",x,.3f);
                Part("Coastal palm trunk",new Vector3(x*1.25f,1.5f,3),new Vector3(.35f,1.5f,.35f),Oak,shape:PrimitiveType.Cylinder,solid:true);
                Part("Coastal palm crown",new Vector3(x*1.25f,3.1f,3),new Vector3(3,.7f,2.4f),new Color(.28f,.56f,.35f),shape:PrimitiveType.Sphere);
            }
            Part("Beach information board",new Vector3(0,2.6f,6.4f),new Vector3(7,.8f,.16f),new Color(.94f,.96f,.90f));
            Label("Beach title","GIỮ BÃI BIỂN SẠCH",new Vector3(0,2.6f,6.28f),.06f);
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
