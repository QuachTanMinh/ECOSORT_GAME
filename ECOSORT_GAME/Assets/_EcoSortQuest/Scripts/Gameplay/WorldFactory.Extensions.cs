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
        static Vector3[] Grid(float[] columns,params float[] rows)
        {
            var points=new Vector3[columns.Length*rows.Length];int index=0;
            foreach(float z in rows)foreach(float x in columns)points[index++]=new Vector3(x,.04f,z);
            return points;
        }
        static AreaSceneryLayout LayoutFor(ExtraEnvironment e,int level,bool indoor,Vector3[] items)
        {
            bool beach=e==ExtraEnvironment.Beach;
            var bounds=beach?Rect.MinMaxRect(-15,-10,15,8):Rect.MinMaxRect(-12.4f,-8.3f,12.4f,7.2f);
            var spawn=beach?new Vector3(0,.1f,-8.5f):new Vector3(0,.1f,-7.2f);
            var collection=beach?new Vector3(0,0,6.3f):new Vector3(0,0,5.8f);
            return new AreaSceneryLayout("extra-"+e+"-"+level,indoor,bounds,spawn,collection,items);
        }
        public static AreaSceneryLayout Layout(ExtraEnvironment e,int level)
        {
            if(level<0 || level>2)throw new ArgumentOutOfRangeException(nameof(level));
            switch(e)
            {
                case ExtraEnvironment.Hospital:
                    return level==0
                        ?LayoutFor(e,level,true,Grid(new[]{-6f,0f,6f},-6f,-4.2f,-2.4f,-.6f,1.2f,3f))
                        :level==1
                            ?LayoutFor(e,level,true,Grid(new[]{-6f,0f,6f},-6f,-4f,-2f,0f,1.4f,2.8f))
                            :LayoutFor(e,level,true,Grid(new[]{-5.5f,0f,5.5f},-6f,-4f,-2f,0f,1.5f,3f));
                case ExtraEnvironment.Supermarket:
                    return level==0
                        ?LayoutFor(e,level,true,Grid(new[]{-5.4f,0f,5.4f},-6.2f,-4.2f,-2.2f,-.2f,1.8f,3.2f))
                        :level==1
                            ?LayoutFor(e,level,true,Grid(new[]{-5.2f,0f,5.2f},-7f,-5.8f,-4.6f,-3.4f,-2.2f,-1f))
                            :LayoutFor(e,level,true,Grid(new[]{-5.6f,0f,5.6f},-6.2f,-4.2f,-2.2f,-.2f,1.8f,3.2f));
                case ExtraEnvironment.Beach:
                    return level==0
                        ?LayoutFor(e,level,false,Grid(new[]{-9f,0f,9f},-7f,-5f,-3f,-1f,1f,2.5f))
                        :level==1
                            ?LayoutFor(e,level,false,Grid(new[]{-6f,0f,6f},-7f,-5f,-3f,-1f,1f,3f))
                            :LayoutFor(e,level,false,Grid(new[]{-8f,0f,8f},-7.5f,-5.5f,-3.5f,-1.5f,.5f,2.5f));
                default:throw new ArgumentOutOfRangeException(nameof(e));
            }
        }
    }
    public sealed partial class WorldFactory
    {
        public AreaSceneryLayout BuildExtraEnvironment(ExtraEnvironment e,int level)
        {
            switch(e)
            {
                case ExtraEnvironment.Hospital:return level==0?CreateHospitalWaiting():level==1?CreateHospitalReception():level==2?CreateHospitalClinic():throw new ArgumentOutOfRangeException(nameof(level));
                case ExtraEnvironment.Supermarket:return level==0?CreateSupermarketAisle():level==1?CreateSupermarketCheckout():level==2?CreateSupermarketMarket():throw new ArgumentOutOfRangeException(nameof(level));
                case ExtraEnvironment.Beach:return level==0?CreateBeachShore():level==1?CreateBeachRest():level==2?CreateBeachCleanup():throw new ArgumentOutOfRangeException(nameof(level));
                default:throw new ArgumentOutOfRangeException(nameof(e));
            }
        }
        AreaSceneryLayout BeginExtra(ExtraEnvironment environment,int level)
        {
            if(level<0 || level>2)throw new ArgumentOutOfRangeException(nameof(level));
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            foreach(var collider in root.GetComponentsInChildren<Collider>())collider.enabled=false;
            activeLayout=ExtraEnvironmentInfo.Layout(environment,level);
            sceneryRoot=new GameObject("Extra scenery • "+environment).transform;sceneryRoot.SetParent(root,false);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.67f,.71f,.75f);
            foreach(var light in root.GetComponentsInChildren<Light>())if(light.type==LightType.Directional)light.intensity=activeLayout.Indoor?1.05f:1.25f;
            if(activeLayout.Indoor)BuildExpandedRoom(environment,level);
            else BuildBeachGround(level);
            Part("Extra collection station",activeLayout.Collection+new Vector3(0,.018f,0),new Vector3(activeLayout.Indoor?8.4f:10.8f,.025f,2.1f),new Color(.59f,.75f,.66f));
            return activeLayout;
        }
        void BuildExpandedRoom(ExtraEnvironment environment,int level)
        {
            Color floor,wall;
            if(environment==ExtraEnvironment.Hospital)
            {
                if(level==0){floor=new Color(.72f,.84f,.82f);wall=new Color(.84f,.93f,.92f);}
                else if(level==1){floor=new Color(.89f,.94f,.94f);wall=new Color(.72f,.85f,.91f);}
                else {floor=new Color(.78f,.87f,.84f);wall=new Color(.86f,.92f,.91f);}
            }
            else
            {
                if(level==0){floor=new Color(.82f,.79f,.67f);wall=new Color(.95f,.88f,.72f);}
                else if(level==1){floor=new Color(.84f,.90f,.91f);wall=new Color(.88f,.94f,.96f);}
                else {floor=new Color(.57f,.61f,.62f);wall=new Color(.94f,.95f,.93f);}
            }
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
                Part("Expanded window frame",new Vector3(x,2.0f,7.32f),new Vector3(3.4f,2.0f,.16f),Color.white);
                Part("Expanded window sky",new Vector3(x,2.0f,7.20f),new Vector3(3.0f,1.6f,.08f),new Color(.44f,.75f,.85f));
                Part("Expanded window landscape",new Vector3(x,1.43f,7.13f),new Vector3(3.0f,.45f,.06f),new Color(.36f,.61f,.36f));
            }
            bool hospital=environment==ExtraEnvironment.Hospital;
            float titleY=hospital?3.98f:3.56f;
            float plaqueHeight=hospital?.40f:.60f;
            float plaqueZ=hospital?7.31f:7.28f;
            float titleZ=hospital?7.21f:7.18f;
            float titleX=hospital?activeLayout.Collection.x:0f;
            var plaque=Part("Expanded room plaque",new Vector3(titleX,titleY,plaqueZ),new Vector3(8.8f,plaqueHeight,.08f),new Color(.94f,.96f,.90f));
            plaque.transform.localRotation=Quaternion.identity;
            Label("Expanded room title",AreaMissions.ForEnvironment(environment)[level].Zone,new Vector3(titleX,titleY,titleZ),.048f);
            if(hospital)
            {
                Part("Clinic cross vertical",new Vector3(5.7f,2.0f,7.18f),new Vector3(.25f,1,.08f),new Color(.72f,.18f,.21f));
                Part("Clinic cross horizontal",new Vector3(5.7f,2.0f,7.17f),new Vector3(1,.25f,.08f),new Color(.72f,.18f,.21f));
            }
        }
        void BuildBeachGround(int level)
        {
            Color sand=level==0?new Color(.91f,.82f,.62f):level==1?new Color(.88f,.77f,.54f):new Color(.77f,.68f,.54f);
            Color sea=level==0?new Color(.23f,.65f,.78f):level==1?new Color(.16f,.57f,.72f):new Color(.10f,.40f,.54f);
            Part("Beach sand floor",new Vector3(0,-.15f,-1f),new Vector3(34,.3f,21),sand,solid:true);
            for(int x=-15;x<=15;x+=3)for(int z=-9;z<=6;z+=3)
                Part("Beach sand pattern",new Vector3(x,.008f,z),new Vector3(2.7f,.012f,2.7f),(x+z)%2==0?Color.Lerp(sand,Color.white,.08f):Color.Lerp(sand,Oak,.06f));
            Part("Sea beyond play area",new Vector3(0,-.1f,14),new Vector3(44,.18f,10),sea);
            Part("Shore wet sand",new Vector3(0,.012f,level==2?3.5f:3.8f),new Vector3(34,.018f,level==2?1.4f:1.8f),Color.Lerp(sand,sea,.12f));
            Part("Shore foam",new Vector3(0,.025f,8.65f),new Vector3(31,.05f,.42f),new Color(.91f,.97f,.96f));
            if(level==2)
            {
                Part("Outer wave line",new Vector3(0,.025f,9.55f),new Vector3(28,.045f,.22f),new Color(.73f,.91f,.91f));
                Part("Wet sand wash",new Vector3(0,.024f,4.55f),new Vector3(34,.018f,.5f),Color.Lerp(sand,sea,.20f));
            }
            if(level==1)
            {
                Part("Beach boardwalk deck",new Vector3(11.1f,.025f,-1.45f),new Vector3(7.2f,.05f,13.2f),PaleWood);
                for(int i=0;i<13;i++)
                    Part("Boardwalk plank seam",new Vector3(11.1f,.055f,-7.45f+i),new Vector3(7.12f,.012f,.045f),Oak);
            }
            if(level==2)
            {
                Part("Beach rock shelf left",new Vector3(-13f,.24f,4.6f),new Vector3(3.8f,.48f,3.2f),new Color(.40f,.47f,.40f),shape:PrimitiveType.Sphere,solid:true);
                Part("Beach rock shelf right",new Vector3(-13.7f,.2f,.8f),new Vector3(1.8f,.4f,1.8f),new Color(.48f,.50f,.42f),shape:PrimitiveType.Sphere,solid:true);
            }
        }
        GameObject BeachSign(string name,string labelName,string text,Vector3 position,float width,float height,Color color)
        {
            var board=Part(name,position,new Vector3(width,height,.14f),color);
            float postHeight=position.y-height*.5f;
            for(int side=-1;side<=1;side+=2)
                Part(name+" support",new Vector3(position.x+side*(width*.5f-.35f),postHeight*.5f,position.z),
                    new Vector3(.14f,postHeight,.14f),Oak,solid:true);
            Label(labelName,text,new Vector3(position.x,position.y,position.z-.09f),.048f);
            sceneryRoot.Find(labelName).SetParent(board.transform,true);
            return board;
        }
        void BeachTitleBoard(int level)
        {
            BeachSign("Beach information board","Beach title",AreaMissions.ForEnvironment(ExtraEnvironment.Beach)[level].Title,
                new Vector3(0,2.6f,8.25f),9.4f,.8f,new Color(.94f,.96f,.90f));
        }
        public AreaSceneryLayout CreateHospitalWaiting()
        {
            var layout=BeginExtra(ExtraEnvironment.Hospital,0);
            for(int i=0;i<4;i++)
            {
                Chair("Waiting chair left",-10.2f,-4.5f+i*2.7f);
                Chair("Waiting chair right",10.2f,-4.5f+i*2.7f,180);
            }
            Bookshelf("Public information shelf",-10.6f,5.2f);
            Part("Drinking water dispenser",new Vector3(9f,1f,-6f),new Vector3(1f,2f,1f),new Color(.90f,.94f,.94f),solid:true);
            Part("Water dispenser bottle",new Vector3(9f,1.9f,-6f),new Vector3(.72f,.55f,.72f),new Color(.39f,.68f,.81f),shape:PrimitiveType.Cylinder);
            Part("Waiting room TV",new Vector3(3.8f,2.25f,7.30f),new Vector3(2.5f,1.4f,.12f),ViewFactory.Ink);
            Part("Waiting room notice board",new Vector3(-3.8f,2.25f,7.30f),new Vector3(2.6f,1.2f,.12f),new Color(.91f,.86f,.69f));
            Label("Waiting room notice","THÔNG BÁO",new Vector3(-3.8f,2.25f,7.22f),.05f);
            foreach(float x in new[]{-11.8f,11.8f})
            {
                Part("Waiting room plant pot",new Vector3(x,.24f,1f),new Vector3(.7f,.48f,.7f),Oak,shape:PrimitiveType.Cylinder);
                Part("Waiting room plant",new Vector3(x,.9f,1f),new Vector3(1.1f,1.2f,1.1f),new Color(.30f,.62f,.39f),shape:PrimitiveType.Sphere);
            }
            return layout;
        }
        public AreaSceneryLayout CreateHospitalReception()
        {
            var layout=BeginExtra(ExtraEnvironment.Hospital,1);
            var counter=Table("Reception counter",8.6f,2.7f,5.6f,1.8f,new Color(.38f,.67f,.69f));
            Part("Reception computer",new Vector3(.8f,1.68f,.1f),new Vector3(.9f,.16f,.55f),Steel,counter);
            Part("Reception computer monitor",new Vector3(.8f,2.1f,.35f),new Vector3(1.05f,.82f,.12f),ViewFactory.Ink,counter);
            Part("Reception computer display",new Vector3(.8f,2.1f,.275f),new Vector3(.86f,.62f,.025f),new Color(.38f,.72f,.80f),counter);
            Part("Reception bell",new Vector3(-.8f,1.27f,.1f),new Vector3(.22f,.07f,.22f),Steel,counter,PrimitiveType.Cylinder);
            Bookshelf("Reception flyer shelf",-10.6f,5.2f);
            Part("Clinic hand sanitizer",new Vector3(-6.2f,.5f,3.6f),new Vector3(.35f,.5f,.35f),new Color(.36f,.72f,.78f),shape:PrimitiveType.Cylinder);
            Part("Ticket machine body",new Vector3(-9f,.9f,2.8f),new Vector3(.65f,1.8f,.65f),Steel,solid:true);
            Part("Ticket machine screen",new Vector3(-9f,1.35f,2.455f),new Vector3(.38f,.34f,.025f),new Color(.30f,.69f,.76f));
            Part("Ticket machine button",new Vector3(-9f,.92f,2.44f),new Vector3(.14f,.14f,.035f),new Color(.72f,.18f,.21f));
            for(int i=0;i<3;i++)
            {
                float x=-11f+i*2.0f;
                Part("Queue stanchion post",new Vector3(x,.55f,-1.2f),new Vector3(.14f,.55f,.14f),Steel,shape:PrimitiveType.Cylinder,solid:true);
                if(i<2)Part("Queue rope",new Vector3(x+1f,.62f,-1.2f),new Vector3(1.9f,.08f,.08f),new Color(.66f,.18f,.22f));
            }
            for(int i=0;i<4;i++)Part("Queue floor marking",new Vector3(-10f+i*.9f,.012f,2.3f),new Vector3(.11f,.02f,3.2f),new Color(.90f,.78f,.42f));
            Part("Reception wall clock",new Vector3(-4.2f,3.05f,7.30f),new Vector3(.72f,.72f,.12f),Color.white,shape:PrimitiveType.Cylinder);
            Part("Reception clock face",new Vector3(-4.2f,3.05f,7.225f),new Vector3(.54f,.54f,.035f),ViewFactory.Ink,shape:PrimitiveType.Cylinder);
            return layout;
        }
        public AreaSceneryLayout CreateHospitalClinic()
        {
            var layout=BeginExtra(ExtraEnvironment.Hospital,2);
            Part("Examination bed",new Vector3(-9f,.45f,-2f),new Vector3(2.5f,.9f,4.2f),new Color(.80f,.88f,.88f),solid:true);
            Part("Examination mattress",new Vector3(-9f,.94f,-2f),new Vector3(2.35f,.12f,4.05f),Color.white);
            Part("Curtain rail",new Vector3(-6.9f,2.9f,-2f),new Vector3(.12f,.12f,5.6f),Steel,solid:true);
            for(int i=0;i<3;i++)
                Part("Clinic privacy curtain",new Vector3(-6.9f,1.65f,-4f+i*2f),new Vector3(.09f,2.4f,1.85f),new Color(.68f,.83f,.88f));
            Part("Medicine cabinet",new Vector3(3.8f,2.0f,7.28f),new Vector3(1.9f,2.0f,.12f),new Color(.91f,.95f,.91f),solid:true);
            Part("Medical trolley",new Vector3(9f,.58f,-3f),new Vector3(1.3f,.12f,1.5f),Steel);
            for(int side=-1;side<=1;side+=2)
                Part("Medical trolley leg",new Vector3(9f+side*.48f,.3f,-3f),new Vector3(.08f,.6f,.08f),Steel);
            for(int shelf=0;shelf<2;shelf++)
                Part("Medical trolley tray",new Vector3(9f,.42f+shelf*.4f,-3f),new Vector3(1.2f,.08f,1.4f),Color.white);
            Part("Clinic stool base",new Vector3(10.5f,.18f,.6f),new Vector3(.75f,.12f,.75f),Steel,shape:PrimitiveType.Cylinder,solid:true);
            Part("Clinic stool seat",new Vector3(10.5f,.62f,.6f),new Vector3(.75f,.16f,.75f),PaleWood,shape:PrimitiveType.Cylinder);
            Part("Hand-washing sink cabinet",new Vector3(9f,.58f,-6f),new Vector3(2.2f,1.16f,1.4f),new Color(.82f,.88f,.87f),solid:true);
            Part("Hand-washing basin",new Vector3(9f,1.2f,-6f),new Vector3(1.25f,.12f,.9f),Steel);
            Part("Hand-washing faucet",new Vector3(9f,1.48f,-6.45f),new Vector3(.12f,.45f,.12f),Steel);
            return layout;
        }
        public AreaSceneryLayout CreateSupermarketAisle()
        {
            var layout=BeginExtra(ExtraEnvironment.Supermarket,0);
            foreach(float x in new[]{-9.2f,-7.3f,7.3f,9.2f})for(int row=0;row<3;row++)
            {
                float z=-4.2f+row*3.4f;
                Part("Grocery aisle shelf",new Vector3(x,1.05f,z),new Vector3(1.7f,2.1f,2.7f),new Color(.67f,.72f,.69f),solid:true);
                for(int tier=0;tier<2;tier++)for(int item=0;item<3;item++)
                    Part("Aisle packaged goods",new Vector3(x-.45f+item*.45f,.65f+tier*.82f,z-.72f),new Vector3(.32f,.48f,.35f),
                        item==0?new Color(.82f,.34f,.25f):item==1?new Color(.31f,.62f,.42f):new Color(.93f,.72f,.28f));
            }
            for(int aisle=0;aisle<3;aisle++)
            {
                float x=-8f+aisle*8f;
                var board=Part("Aisle number board",new Vector3(x,3.35f,-.5f),new Vector3(1.60f,.72f,.12f),new Color(.18f,.39f,.39f));
                board.transform.localRotation=Quaternion.identity;
                Label("Aisle number","LỐI ĐI "+(aisle+1),new Vector3(x,3.35f,-.58f),.055f, Color.white);
                foreach(float side in new[]{-.42f,.42f})
                    Part("Aisle number hanger",new Vector3(x+side,3.84f,-.5f),new Vector3(.08f,.28f,.08f),Steel);
            }
            Part("Shopping cart basket",new Vector3(-11.2f,.72f,-5.8f),new Vector3(1.2f,.85f,1.4f),Steel);
            Part("Shopping cart handle",new Vector3(-11.2f,1.28f,-5.1f),new Vector3(.9f,.08f,.12f),Oak);
            for(int side=-1;side<=1;side+=2)
                Part("Shopping cart wheel",new Vector3(-11.2f+side*.48f,.22f,-6.2f),new Vector3(.22f,.22f,.22f),ViewFactory.Ink,shape:PrimitiveType.Cylinder);
            Part("Reusable basket stack",new Vector3(11f,.38f,-4.5f),new Vector3(1.2f,.42f,1.1f),new Color(.72f,.24f,.18f));
            Part("Reusable basket",new Vector3(11f,.82f,-4.5f),new Vector3(1.2f,.42f,1.1f),new Color(.30f,.58f,.67f));
            foreach(float x in new[]{-2.7f,2.7f})foreach(float z in new[]{-6f,-2.5f,1f})
            {
                Part("Aisle direction arrow",new Vector3(x,.035f,z),new Vector3(.12f,.025f,.75f),new Color(.24f,.55f,.39f));
                foreach(float side in new[]{-1f,1f})
                {
                    var head=Part("Aisle arrow head",new Vector3(x+side*.18f,.04f,z+.3f),new Vector3(.42f,.025f,.12f),new Color(.24f,.55f,.39f));
                    head.transform.localRotation=Quaternion.Euler(0,side*45f,0);
                }
            }
            return layout;
        }
        public AreaSceneryLayout CreateSupermarketCheckout()
        {
            var layout=BeginExtra(ExtraEnvironment.Supermarket,1);
            foreach(float x in new[]{-8.8f,0f,8.8f})
            {
                var counter=Table("Checkout counter",x,2.35f,4.6f,1.6f,Oak);
                Part("Checkout conveyor",new Vector3(0,1.23f,-.2f),new Vector3(3f,.1f,1.05f),new Color(.25f,.31f,.31f),counter);
                Part("Checkout register",new Vector3(.75f,1.62f,.3f),new Vector3(.75f,.68f,.65f),ViewFactory.Ink,counter);
                Part("Checkout display",new Vector3(.75f,2f,.3f),new Vector3(.6f,.36f,.08f),new Color(.35f,.7f,.77f),counter);
                Part("Checkout bag rack",new Vector3(-.85f,1.62f,.3f),new Vector3(.75f,.6f,.5f),new Color(.82f,.84f,.78f),counter);
                Part("Checkout divider",new Vector3(-.35f,1.32f,-.2f),new Vector3(.12f,.1f,.8f),new Color(.82f,.28f,.2f),counter);
                float signX=x;
                Part("Checkout number sign",new Vector3(signX,3.0f,1.2f),new Vector3(1.1f,.55f,.12f),new Color(.14f,.38f,.34f));
                Label("Checkout number",(Array.IndexOf(new[]{-8.8f,0f,8.8f},x)+1).ToString("00"),new Vector3(signX,3.0f,1.12f),.06f);
                for(int side=-1;side<=1;side+=2)
                    Part("Checkout sign support",new Vector3(signX+side*.42f,3.74f,1.2f),new Vector3(.08f,.92f,.08f),Steel);
            }
            Part("Candy rack",new Vector3(-11f,1.05f,-4.2f),new Vector3(1.1f,2.1f,2.8f),new Color(.77f,.56f,.34f),solid:true);
            for(int row=0;row<3;row++)for(int item=0;item<3;item++)
                Part("Candy display",new Vector3(-11.4f+item*.4f,.65f+row*.55f,-5.1f),new Vector3(.25f,.35f,.22f),
                    item==0?new Color(.85f,.28f,.24f):item==1?new Color(.92f,.72f,.25f):new Color(.34f,.57f,.76f));
            foreach(float x in new[]{-11.2f,-9.6f,-8f})
                Part("Checkout queue post",new Vector3(x,.55f,-.5f),new Vector3(.14f,.55f,.14f),Steel,shape:PrimitiveType.Cylinder,solid:true);
            foreach(float x in new[]{-10.4f,-8.8f})
                Part("Checkout queue rail",new Vector3(x,.62f,-.5f),new Vector3(1.65f,.08f,.08f),Oak);
            return layout;
        }
        public AreaSceneryLayout CreateSupermarketMarket()
        {
            var layout=BeginExtra(ExtraEnvironment.Supermarket,2);
            foreach(float x in new[]{-9f,9f})for(int row=0;row<3;row++)
            {
                float z=-3.8f+row*3.2f;
                Table("Fresh produce display",x,z,3.6f,2.2f,new Color(.58f,.42f,.27f));
                for(int bin=0;bin<3;bin++)
                    Part("Produce crate",new Vector3(x-1.05f+bin*1.05f,1.32f,z),new Vector3(.92f,.32f,1.4f),bin==0?new Color(.78f,.34f,.22f):bin==1?new Color(.83f,.67f,.25f):new Color(.35f,.62f,.34f));
            }
            Part("Refrigerated display case",new Vector3(-10.8f,1.25f,-6.5f),new Vector3(2.4f,2.5f,1.6f),new Color(.75f,.86f,.87f),solid:true);
            Part("Refrigerated display glass",new Vector3(-10.8f,1.35f,-7.33f),new Vector3(2.2f,1.8f,.08f),new Color(.50f,.75f,.82f));
            Part("Refrigerated display case",new Vector3(10.8f,1.25f,-6.5f),new Vector3(2.4f,2.5f,1.6f),new Color(.75f,.86f,.87f),solid:true);
            Part("Refrigerated display glass",new Vector3(10.8f,1.35f,-7.33f),new Vector3(2.2f,1.8f,.08f),new Color(.50f,.75f,.82f));
            Part("Fresh produce scale",new Vector3(8.8f,1.6f,4.3f),new Vector3(.8f,.65f,.65f),Color.white);
            Part("Pallet base",new Vector3(-11.2f,.12f,4.5f),new Vector3(2.2f,.24f,1.8f),Oak,solid:true);
            for(int row=0;row<2;row++)for(int side=-1;side<=1;side+=2)
                Part("Carton stack",new Vector3(-11.2f,.65f+row*.72f,4.5f+side*.45f),new Vector3(1f,.72f,.78f),new Color(.68f,.48f,.29f),solid:true);
            Part("Hand pallet truck handle",new Vector3(-8.5f,.8f,4.5f),new Vector3(.12f,1.6f,.12f),Steel);
            Part("Hand pallet truck handle grip",new Vector3(-8.5f,1.62f,4.5f),new Vector3(.55f,.1f,.12f),ViewFactory.Ink);
            Part("Hand pallet truck chassis",new Vector3(-8.5f,.14f,4.5f),new Vector3(.55f,.16f,1.6f),Steel);
            foreach(float side in new[]{-.28f,.28f})
                Part("Hand pallet truck fork",new Vector3(-8.5f+side,.12f,5.45f),new Vector3(.12f,.12f,1.9f),Steel);
            foreach(float x in new[]{-8.75f,-8.25f})
                Part("Hand pallet truck wheel",new Vector3(x,.12f,3.95f),new Vector3(.2f,.2f,.2f),ViewFactory.Ink,shape:PrimitiveType.Cylinder);
            return layout;
        }
        public AreaSceneryLayout CreateBeachShore()
        {
            var layout=BeginExtra(ExtraEnvironment.Beach,0);
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*11.7f;
                Part("Beach umbrella pole",new Vector3(x,1.5f,-2.6f),new Vector3(.14f,1.5f,.14f),PaleWood,shape:PrimitiveType.Cylinder,solid:true);
                Part("Beach umbrella canopy",new Vector3(x,2.8f,-2.6f),new Vector3(3.5f,.42f,3.5f),
                    side<0?new Color(.92f,.43f,.29f):new Color(.24f,.61f,.76f),shape:PrimitiveType.Sphere);
                Part("Beach lounger seat",new Vector3(x,.24f,-5.5f),new Vector3(1.15f,.28f,2.35f),PaleWood,solid:true);
                Part("Beach lounger back",new Vector3(x,.73f,-6.15f),new Vector3(1.12f,.9f,.16f),Oak);
                Part("Beach towel",new Vector3(x,.035f,-8.25f),new Vector3(1.7f,.04f,2.15f),
                    side<0?new Color(.29f,.65f,.74f):new Color(.91f,.72f,.42f));
                float palmX=side*13.8f;
                Part("Beach palm trunk",new Vector3(palmX,1.7f,2.8f),new Vector3(.38f,1.7f,.38f),Oak,shape:PrimitiveType.Cylinder,solid:true);
                Part("Beach palm crown",new Vector3(palmX,3.55f,2.8f),new Vector3(2.8f,.72f,2.4f),new Color(.27f,.55f,.34f),shape:PrimitiveType.Sphere);
                Part("Beach palm coconut",new Vector3(palmX-.35f,3.2f,2.7f),new Vector3(.38f,.38f,.38f),new Color(.43f,.31f,.19f),shape:PrimitiveType.Sphere);
            }
            Part("Sand castle base",new Vector3(-12.2f,.2f,.4f),new Vector3(1.5f,.4f,1.2f),new Color(.81f,.68f,.48f),solid:true);
            Part("Sand castle tower",new Vector3(-12.2f,.72f,.4f),new Vector3(.55f,.7f,.55f),new Color(.88f,.76f,.56f));
            Part("Beach ball",new Vector3(12.1f,.5f,.4f),new Vector3(.9f,.9f,.9f),new Color(.95f,.56f,.25f),shape:PrimitiveType.Sphere);
            BeachTitleBoard(0);
            return layout;
        }
        public AreaSceneryLayout CreateBeachRest()
        {
            var layout=BeginExtra(ExtraEnvironment.Beach,1);
            for(int side=0;side<4;side++)
            {
                float x=9.1f+(side%2)*4.1f;
                float z=side<2?-2.5f:3.2f;
                Table("Beach cafe table",x,z,1.8f,1.25f,PaleWood);
                Chair("Beach cafe chair",x-1.5f,z,90);
                Chair("Beach cafe chair",x+1.5f,z,270);
            }
            Part("Beach cafe canopy",new Vector3(11.1f,3.15f,.55f),new Vector3(4.5f,.3f,3.3f),new Color(.91f,.67f,.36f),solid:true);
            foreach(float x in new[]{9.2f,13f})foreach(float z in new[]{-.65f,1.7f})
                Part("Beach cafe canopy post",new Vector3(x,1.5f,z),new Vector3(.16f,3f,.16f),Oak,solid:true);
            Part("Beach drink stand counter",new Vector3(11.1f,.68f,1.15f),new Vector3(3.4f,1.36f,.95f),new Color(.38f,.64f,.60f),solid:true);
            Part("Beach drink cooler",new Vector3(13.4f,.45f,-6.2f),new Vector3(1.5f,.9f,1.25f),new Color(.34f,.65f,.75f),solid:true);
            BeachSign("Beach cafe sign","Beach cafe sign text","QUÁN NƯỚC",new Vector3(11.1f,2.45f,2.15f),2.9f,.62f,new Color(.96f,.91f,.73f));
            var bicycleFront=Part("Cafe bicycle front wheel",new Vector3(8.6f,.48f,-5.6f),new Vector3(.9f,.12f,.9f),ViewFactory.Ink,shape:PrimitiveType.Cylinder);
            bicycleFront.transform.localRotation=Quaternion.Euler(90,0,0);
            var bicycleRear=Part("Cafe bicycle rear wheel",new Vector3(10.3f,.48f,-5.6f),new Vector3(.9f,.12f,.9f),ViewFactory.Ink,shape:PrimitiveType.Cylinder);
            bicycleRear.transform.localRotation=Quaternion.Euler(90,0,0);
            Part("Cafe bicycle frame",new Vector3(9.45f,.85f,-5.6f),new Vector3(1.65f,.1f,.12f),new Color(.34f,.57f,.41f));
            Part("Beach lamp post",new Vector3(8.2f,1.85f,4.1f),new Vector3(.14f,1.85f,.14f),Steel,shape:PrimitiveType.Cylinder,solid:true);
            Part("Beach lamp",new Vector3(8.2f,3.55f,4.1f),new Vector3(.7f,.35f,.7f),new Color(.96f,.83f,.52f));
            Part("Beach wayfinding post",new Vector3(13.8f,1.1f,4.1f),new Vector3(.14f,1.1f,.14f),Oak,shape:PrimitiveType.Cylinder,solid:true);
            Part("Beach wayfinding board",new Vector3(13.8f,2.25f,4.1f),new Vector3(2.2f,.55f,.14f),new Color(.27f,.47f,.44f));
            Label("Beach wayfinding","LỐI ĐI BỘ",new Vector3(13.8f,2.25f,4.01f),.045f);
            sceneryRoot.Find("Beach wayfinding").SetParent(sceneryRoot.Find("Beach wayfinding board"),true);
            BeachTitleBoard(1);
            return layout;
        }
        public AreaSceneryLayout CreateBeachCleanup()
        {
            var layout=BeginExtra(ExtraEnvironment.Beach,2);
            Part("Beach pier deck",new Vector3(12f,.18f,5.9f),new Vector3(4.8f,.36f,8.8f),Oak,solid:true);
            foreach(float x in new[]{9.8f,14.2f})foreach(float z in new[]{2f,5f,8f,10f})
                Part("Pier support pile",new Vector3(x,-.55f,z),new Vector3(.24f,.55f,.24f),PaleWood,shape:PrimitiveType.Cylinder,solid:true);
            for(int plank=0;plank<10;plank++)
                Part("Beach pier plank seam",new Vector3(12f,.375f,1.8f+plank*.86f),new Vector3(4.72f,.025f,.045f),PaleWood);
            Part("Coastal palm trunk",new Vector3(-12.6f,1.55f,7.2f),new Vector3(.36f,1.55f,.36f),Oak,shape:PrimitiveType.Cylinder,solid:true);
            Part("Coastal palm crown",new Vector3(-12.6f,3.35f,7.2f),new Vector3(2.8f,.72f,2.3f),new Color(.28f,.56f,.35f),shape:PrimitiveType.Sphere);
            Part("Small beach boat hull",new Vector3(12f,.27f,0f),new Vector3(1.5f,.5f,2.2f),new Color(.30f,.51f,.57f),solid:true);
            Part("Small beach boat seat",new Vector3(12f,.58f,0f),new Vector3(.75f,.12f,.35f),PaleWood);
            Part("Beach fishing net frame",new Vector3(14.5f,1.51f,4f),new Vector3(.12f,2.3f,2.3f),Oak);
            for(int i=0;i<5;i++)
                Part("Beach fishing net strand",new Vector3(14.46f,.55f+i*.45f,4f),new Vector3(.035f,.035f,2.1f),new Color(.76f,.80f,.71f));
            for(int i=0;i<5;i++)
                Part("Cleanup shoreline marker",new Vector3(-12.5f,.07f,-6f+i*1.5f),new Vector3(.12f,.06f,.55f),i%2==0?Oak:PaleWood);
            Part("Lifeguard rescue marker",new Vector3(-12.3f,1.35f,1.35f),new Vector3(.22f,2.7f,.22f),new Color(.88f,.32f,.25f),solid:true);
            Part("Lifeguard sign board",new Vector3(-12.3f,2.35f,1.35f),new Vector3(1.5f,.56f,.14f),Color.white);
            Label("Lifeguard sign","CỨU HỘ",new Vector3(-12.3f,2.35f,1.26f),.045f);
            sceneryRoot.Find("Lifeguard sign").SetParent(sceneryRoot.Find("Lifeguard sign board"),true);
            BeachTitleBoard(2);
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
