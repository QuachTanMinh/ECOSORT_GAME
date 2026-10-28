using UnityEngine;
using UnityEngine.Rendering;

namespace EcoSortQuest
{
    public sealed partial class WorldFactory
    {
        Transform sceneryRoot;
        AreaSceneryLayout activeLayout;
        static readonly Color Oak=new Color(.66f,.43f,.24f), PaleWood=new Color(.84f,.68f,.45f), Steel=new Color(.52f,.61f,.65f);
        GameObject Part(string name,Vector3 p,Vector3 size,Color color,Transform parent=null,PrimitiveType shape=PrimitiveType.Cube,bool solid=false)
            =>Shape(name,shape,parent!=null?parent:sceneryRoot,p,size,color,solid);
        Transform Group(string name,float x,float z)
        {
            var group=new GameObject(name).transform;group.SetParent(sceneryRoot,false);group.localPosition=new Vector3(x,0,z);return group;
        }
        void Label(string name,string text,Vector3 position,float size=.065f)
        {
            var go=new GameObject(name,typeof(TextMesh));go.transform.SetParent(sceneryRoot,false);go.transform.localPosition=position;
            var label=go.GetComponent<TextMesh>();label.text=text;label.fontSize=48;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.color=ViewFactory.Ink;
        }
        public AreaSceneryLayout BuildMissionEnvironment(AreaMission mission)
        {
            // Replace the shared sorting plaza; keep the authored camera and lights.
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            foreach(var collider in root.GetComponentsInChildren<Collider>())collider.enabled=false;
            activeLayout=AreaSceneryLayout.For(mission);
            sceneryRoot=new GameObject("Mission scenery • "+mission.Id).transform;sceneryRoot.SetParent(root,false);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.67f,.71f,.75f);
            foreach(var light in root.GetComponentsInChildren<Light>())if(light.type==LightType.Directional)light.intensity=activeLayout.Indoor?1.05f:1.25f;
            if(activeLayout.Indoor)BuildRoom(mission);else BuildOutdoor(mission);
            switch(mission.Id)
            {
                case "home-kitchen":Kitchen();break;
                case "home-study":Bedroom();break;
                case "home-separate":UtilityRoom();break;
                case "school-class":Classroom();break;
                case "school-canteen":Canteen();break;
                case "school-yard":SchoolYard();break;
                case "park-path":ParkPath();break;
                case "park-picnic":Picnic();break;
                case "park-restore":CommunityGarden();break;
            }
            // The collection station is part of the environment, with room-sized bins.
            Part("Collection station mat",activeLayout.Collection+new Vector3(0,.018f,0),new Vector3(activeLayout.Indoor?7.4f:10,.025f,2.1f),new Color(.59f,.75f,.66f));
            return activeLayout;
        }
        void BuildRoom(AreaMission mission)
        {
            bool bedroom=mission.Id=="home-study",school=mission.Context==LearningContext.School;
            var floor=bedroom?new Color(.70f,.53f,.34f):new Color(.79f,.77f,.65f);
            Part("Room solid floor",new Vector3(0,-.15f,-1.1f),new Vector3(20.4f,.30f,14.6f),floor,solid:true);
            for(int x=0;x<10;x++)for(int z=0;z<7;z++)
                Part("Floor tile",new Vector3(-9+x*2,.008f,-7+z*2),new Vector3(1.97f,.012f,1.97f),(x+z)%2==0?floor:Color.Lerp(floor,Color.white,.13f));
            var paint=school?new Color(.72f,.84f,.86f):bedroom?new Color(.76f,.80f,.88f):new Color(.93f,.84f,.65f);
            void Wall(string name,Vector3 pos,Vector3 scale,Vector3 inward)
            {
                var wall=Part(name,pos,scale,paint,solid:true);wall.AddComponent<CutawayWall>().Inward=inward;
            }
            Wall("Room back wall",new Vector3(0,1.9f,6),new Vector3(20.4f,3.8f,.24f),Vector3.back);
            Wall("Room left wall",new Vector3(-10,1.9f,-1.1f),new Vector3(.24f,3.8f,14.4f),Vector3.right);
            Wall("Room right wall",new Vector3(10,1.9f,-1.1f),new Vector3(.24f,3.8f,14.4f),Vector3.left);
            Part("Cutaway threshold",new Vector3(0,.10f,-8.3f),new Vector3(20.4f,.20f,.22f),Oak,solid:true);
            Part("Back skirting",new Vector3(0,.14f,5.82f),new Vector3(20,.25f,.12f),Color.white);
            Window(-6.8f,5.82f);
            Part("Room name plaque",new Vector3(0,3.25f,5.79f),new Vector3(6.8f,.55f,.08f),new Color(.94f,.96f,.90f));
            Label("Room title",mission.Zone,new Vector3(0,3.26f,5.72f),.048f);
        }
        void Window(float x,float z)
        {
            Part("Window frame",new Vector3(x,2.6f,z),new Vector3(3.2f,1.9f,.16f),Color.white);
            Part("Window sky",new Vector3(x,2.6f,z-.10f),new Vector3(2.85f,1.55f,.08f),new Color(.43f,.74f,.85f));
            Part("Window landscape",new Vector3(x,2.10f,z-.16f),new Vector3(2.8f,.45f,.06f),new Color(.36f,.60f,.36f));
            Part("Window mullion",new Vector3(x,2.6f,z-.21f),new Vector3(.09f,1.7f,.08f),Color.white);
            Part("Window crossbar",new Vector3(x,2.6f,z-.21f),new Vector3(3,.09f,.08f),Color.white);
        }
        Transform Table(string name,float x,float z,float w,float d,Color color)
        {
            var group=Group(name,x,z);
            Part(name+" top",new Vector3(0,1.12f,0),new Vector3(w,.16f,d),color,group);
            for(int i=0;i<4;i++)Part("Table leg",new Vector3((i%2==0?-1:1)*(w*.5f-.18f),.52f,(i<2?-1:1)*(d*.5f-.18f)),new Vector3(.14f,1.04f,.14f),Oak,group);
            var col=group.gameObject.AddComponent<BoxCollider>();col.center=new Vector3(0,.6f,0);col.size=new Vector3(w,1.2f,d);
            return group;
        }
        void Chair(string name,float x,float z,float yaw=0)
        {
            var group=Group(name,x,z);group.localRotation=Quaternion.Euler(0,yaw,0);
            Part("Chair seat",new Vector3(0,.58f,0),new Vector3(.95f,.15f,.85f),PaleWood,group);
            Part("Chair back",new Vector3(0,1.02f,-.35f),new Vector3(.95f,.85f,.12f),Oak,group);
            for(int i=0;i<4;i++)Part("Chair leg",new Vector3(i%2==0?-.35f:.35f,.27f,i<2?-.28f:.28f),new Vector3(.10f,.54f,.10f),Oak,group);
            var col=group.gameObject.AddComponent<BoxCollider>();col.center=new Vector3(0,.7f,0);col.size=new Vector3(.95f,1.4f,.9f);
        }
        void Cabinet(string name,float x,float z,float w=1.8f,float h=1.25f,float d=1.6f)
        {
            Part(name,new Vector3(x,h*.5f,z),new Vector3(w,h,d),new Color(.66f,.77f,.70f),solid:true);
            Part("Cabinet worktop",new Vector3(x,h+.06f,z),new Vector3(w+.10f,.12f,d+.10f),new Color(.93f,.91f,.79f));
            Part("Cabinet door seam",new Vector3(x,h*.5f,z-d*.5f-.02f),new Vector3(.025f,h*.8f,.03f),Oak);
            foreach(float side in new[]{-1f,1f})Part("Cabinet handle",new Vector3(x+side*.15f,h*.66f,z-d*.5f-.05f),new Vector3(.06f,.28f,.06f),Steel);
        }
        void Kitchen()
        {
            for(int i=0;i<4;i++)Cabinet("Kitchen lower cabinet",-8.6f,-3.5f+i*2,1.9f,1.25f,1.9f);
            Part("Kitchen sink",new Vector3(-8.6f,1.39f,-1.5f),new Vector3(1.4f,.08f,1.3f),Steel);
            Part("Sink basin",new Vector3(-8.6f,1.44f,-1.5f),new Vector3(1.05f,.04f,.95f),new Color(.18f,.30f,.34f));
            Part("Sink tap",new Vector3(-9.15f,1.72f,-1.5f),new Vector3(.10f,.6f,.10f),Steel);
            Part("Tap spout",new Vector3(-8.92f,2,-1.5f),new Vector3(.55f,.09f,.10f),Steel);
            Part("Cooker hob",new Vector3(-8.6f,1.4f,2.5f),new Vector3(1.65f,.09f,1.6f),ViewFactory.Ink);
            for(int i=0;i<4;i++)Part("Cooker ring",new Vector3(-8.95f+(i%2)*.7f,1.47f,2.15f+(i/2)*.7f),new Vector3(.43f,.035f,.43f),Steel,shape:PrimitiveType.Cylinder);
            Part("Cooking pot",new Vector3(-8.95f,1.65f,2.15f),new Vector3(.48f,.22f,.48f),new Color(.77f,.35f,.22f),shape:PrimitiveType.Cylinder);
            Part("Kitchen refrigerator",new Vector3(8.1f,1.6f,4.7f),new Vector3(2.4f,3.2f,1.8f),new Color(.89f,.93f,.94f),solid:true);
            Part("Fridge door division",new Vector3(8.1f,2.15f,3.78f),new Vector3(2.3f,.045f,.04f),Steel);
            Part("Fridge handle",new Vector3(7.3f,1.4f,3.72f),new Vector3(.08f,.7f,.12f),ViewFactory.Ink);
            for(int i=0;i<3;i++)Part("Kitchen wall cupboard",new Vector3(-9.4f,2.85f,-3+i*2.3f),new Vector3(.8f,1.0f,1.9f),PaleWood);
            var table=Table("Kitchen dining table",5.8f,-1.7f,3.6f,2.2f,PaleWood);
            foreach(float x in new[]{4.9f,6.7f}){Chair("Dining chair",x,-3.35f);Chair("Dining chair",x,-.05f,180);}
            for(int i=0;i<2;i++)Part("Breakfast plate",new Vector3((i==0?-1:1)*.85f,1.23f,0),new Vector3(.65f,.035f,.65f),Color.white,table,PrimitiveType.Cylinder);
            Part("Fruit bowl",new Vector3(0,1.3f,0),new Vector3(.75f,.14f,.6f),new Color(.83f,.49f,.25f),table,PrimitiveType.Sphere);
            PotPlant("Kitchen herb pot",-8.5f,4.6f,.65f);
        }
        void Bedroom()
        {
            Part("Bedroom bed frame",new Vector3(-6.8f,.38f,-1.8f),new Vector3(3.3f,.75f,4.8f),Oak,solid:true);
            Part("Bed mattress",new Vector3(-6.8f,.87f,-1.8f),new Vector3(3.15f,.3f,4.65f),new Color(.96f,.94f,.84f));
            Part("Bed quilt",new Vector3(-6.8f,1.06f,-2.6f),new Vector3(3.18f,.16f,3),new Color(.28f,.55f,.70f));
            Part("Bed pillow",new Vector3(-6.8f,1.1f,-.2f),new Vector3(2.25f,.23f,.85f),Color.white);
            Part("Bed headboard",new Vector3(-6.8f,1.15f,.65f),new Vector3(3.5f,1.6f,.18f),PaleWood);
            Cabinet("Bedside cabinet",-8.9f,1.1f,1.1f,.85f,1.05f);
            Part("Bedside lamp stem",new Vector3(-8.9f,1.25f,1.1f),new Vector3(.07f,.6f,.07f),Steel);
            Part("Bedside lamp shade",new Vector3(-8.9f,1.6f,1.1f),new Vector3(.65f,.35f,.65f),new Color(1,.80f,.38f),shape:PrimitiveType.Cylinder);
            var desk=Table("Bedroom study desk",5.7f,1.5f,4,1.8f,PaleWood);Chair("Study chair",5.7f,-.1f);
            Part("Computer monitor",new Vector3(.6f,1.75f,.3f),new Vector3(1.3f,.85f,.12f),ViewFactory.Ink,desk);
            Part("Monitor display",new Vector3(.6f,1.75f,.22f),new Vector3(1.15f,.70f,.025f),new Color(.35f,.72f,.76f),desk);
            Part("Monitor stand",new Vector3(.6f,1.4f,.3f),new Vector3(.10f,.45f,.10f),Steel,desk);
            Part("Keyboard",new Vector3(.6f,1.24f,-.45f),new Vector3(1,.06f,.35f),Steel,desk);
            for(int i=0;i<3;i++)Part("Study exercise book",new Vector3(-1.05f,1.25f+i*.09f,0),new Vector3(.8f,.08f,.95f),new Color(.34f+i*.15f,.55f,.33f),desk);
            Part("Bedroom wardrobe",new Vector3(8.5f,1.7f,-4.5f),new Vector3(2,3.4f,2.4f),PaleWood,solid:true);
            Part("Wardrobe doors",new Vector3(8.5f,1.7f,-5.72f),new Vector3(.035f,3.2f,.04f),Oak);
            foreach(float x in new[]{8.3f,8.7f})Part("Wardrobe handle",new Vector3(x,1.7f,-5.78f),new Vector3(.06f,.5f,.08f),Steel);
            Bookshelf("Bedroom bookshelf",8.5f,4.75f);
            Part("Bedroom rug",new Vector3(0,.025f,-2),new Vector3(6.4f,.025f,5.8f),new Color(.60f,.70f,.75f));
        }
        void Bookshelf(string name,float x,float z)
        {
            Part(name,new Vector3(x,1.6f,z),new Vector3(2.2f,3.2f,.8f),Oak,solid:true);
            for(int row=0;row<3;row++)
            {
                Part("Shelf opening",new Vector3(x,.65f+row*.9f,z-.42f),new Vector3(1.9f,.72f,.06f),new Color(.35f,.26f,.20f));
                for(int book=0;book<6;book++)Part("Shelf book",new Vector3(x-.75f+book*.29f,.55f+row*.9f,z-.5f),new Vector3(.20f,.47f+(book%2)*.12f,.28f),book%3==0?new Color(.79f,.35f,.26f):book%3==1?new Color(.29f,.55f,.68f):new Color(.84f,.71f,.32f));
            }
        }
        void UtilityRoom()
        {
            Cabinet("Utility sink counter",-8.3f,3,2.6f,1.2f,2.1f);
            Part("Utility sink",new Vector3(-8.3f,1.33f,3),new Vector3(1.7f,.10f,1.5f),Steel);
            Part("Washing machine",new Vector3(-8.3f,.95f,-.5f),new Vector3(2.4f,1.9f,2.1f),new Color(.91f,.95f,.93f),solid:true);
            var drum=Part("Washer round door",new Vector3(-8.3f,.95f,-1.58f),new Vector3(1.4f,.10f,1.4f),new Color(.25f,.41f,.48f),shape:PrimitiveType.Cylinder);drum.transform.localRotation=Quaternion.Euler(90,0,0);
            Bookshelf("Utility storage shelf",8.2f,4.7f);
            for(int i=0;i<3;i++)
            {
                Part("Reusable storage box",new Vector3(8.3f,.4f+i*.65f,-3.3f),new Vector3(2.2f,.75f,2),i%2==0?new Color(.67f,.81f,.67f):new Color(.81f,.70f,.48f),solid:i==0);
                Part("Broom handle",new Vector3(-9.2f,1.2f,-4-i*.45f),new Vector3(.07f,2.4f,.07f),Oak);
                Part("Broom bristles",new Vector3(-9.2f,.15f,-4-i*.45f),new Vector3(.6f,.3f,.22f),new Color(.77f,.60f,.29f));
            }
            Window(6.5f,5.82f);
        }
        void Classroom()
        {
            Part("Classroom blackboard frame",new Vector3(0,2.2f,5.79f),new Vector3(7.3f,1.7f,.14f),Oak);
            Part("Classroom blackboard",new Vector3(0,2.2f,5.69f),new Vector3(7,1.45f,.06f),new Color(.12f,.30f,.25f));
            Part("Chalk rail",new Vector3(0,1.45f,5.53f),new Vector3(7.1f,.10f,.3f),PaleWood);
            for(int i=0;i<3;i++)Part("Chalk writing",new Vector3(-.8f,2.55f-i*.33f,5.64f),new Vector3(3.9f-i*.4f,.035f,.02f),new Color(.92f,.94f,.80f));
            Part("Teacher podium",new Vector3(-7,.15f,3.5f),new Vector3(3.8f,.3f,2.4f),new Color(.60f,.47f,.33f),solid:true);
            Table("Teacher desk",-7,3.5f,3.2f,1.7f,PaleWood);
            for(int row=0;row<2;row++)for(int side=-1;side<=1;side+=2)
            {
                float x=side*5.4f,z=-3.7f+row*3.6f;
                var desk=Table("Class student desk "+row+" "+side,x,z,3.0f,1.3f,PaleWood);
                Chair("Student chair",x-.7f,z-1.1f);Chair("Student chair",x+.7f,z-1.1f);
                Part("Desk workbook",new Vector3(0,1.24f,0),new Vector3(.8f,.05f,.65f),new Color(.32f,.63f,.74f),desk);
            }
            Bookshelf("Classroom book corner",8.3f,4.9f);Window(6.3f,5.82f);
        }
        void Canteen()
        {
            for(int i=0;i<3;i++)Cabinet("Canteen serving counter",-8.1f,-2.5f+i*2.3f,2.6f,1.2f,2.2f);
            for(int i=0;i<4;i++)Part("Serving food bowl",new Vector3(-8.1f,1.43f,-2.6f+i*1.6f),new Vector3(.7f,.18f,.7f),i%2==0?new Color(.84f,.64f,.29f):new Color(.36f,.64f,.32f),shape:PrimitiveType.Sphere);
            foreach(float z in new[]{-3.5f,.5f})
            {
                var table=Table("Canteen dining table",5.6f,z,4,1.7f,new Color(.69f,.77f,.67f));
                foreach(float x in new[]{4.6f,6.6f}){Chair("Canteen chair",x,z-1.45f);Chair("Canteen chair",x,z+1.45f,180);}
                Part("Lunch tray",new Vector3(0,1.24f,0),new Vector3(1.2f,.05f,.65f),Steel,table);
            }
            Part("Drinking water dispenser",new Vector3(8.4f,1.0f,4.6f),new Vector3(1.1f,2,1.2f),new Color(.90f,.94f,.94f),solid:true);
            Part("Water bottle dispenser",new Vector3(8.4f,2.4f,4.6f),new Vector3(.85f,.6f,.85f),new Color(.39f,.68f,.81f),shape:PrimitiveType.Cylinder);
            Window(6.1f,5.82f);
        }
    }
}
