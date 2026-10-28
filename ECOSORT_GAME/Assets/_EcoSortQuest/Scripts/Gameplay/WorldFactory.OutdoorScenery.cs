using UnityEngine;

namespace EcoSortQuest
{
    public sealed partial class WorldFactory
    {
        void BuildOutdoor(AreaMission mission)
        {
            bool school=mission.Context==LearningContext.School;
            Part("Outdoor solid ground",new Vector3(0,-.16f,-1),new Vector3(44,.32f,36),school?new Color(.73f,.76f,.66f):new Color(.43f,.63f,.35f),solid:true);
            if(school)Part("School courtyard paving",new Vector3(0,.012f,-1.5f),new Vector3(23,.025f,15),new Color(.80f,.74f,.60f));
            else
            {
                Part("Collection footpath",new Vector3(0,.016f,3.9f),new Vector3(23,.03f,2.4f),new Color(.78f,.72f,.57f));
                for(int side=-1;side<=1;side+=2)Part("Park path border",new Vector3(side*11.6f,.12f,-1.5f),new Vector3(.22f,.24f,15),PaleWood,solid:true);
            }
            // Background fence marks the edge without obscuring the character.
            for(int i=0;i<17;i++)Part("Fence post",new Vector3(-12+i*1.5f,.65f,6.5f),new Vector3(.13f,1.3f,.13f),Oak);
            for(int rail=0;rail<2;rail++)Part("Fence rail",new Vector3(0,.35f+rail*.55f,6.5f),new Vector3(25,.10f,.10f),PaleWood);
            Part("Outdoor zone sign",new Vector3(0,2.7f,6.45f),new Vector3(7.8f,.8f,.15f),new Color(.92f,.91f,.75f));
            Label("Outdoor title",mission.Zone,new Vector3(0,2.7f,6.35f),.05f);
        }
        void PotPlant(string name,float x,float z,float scale=1)
        {
            Part(name,new Vector3(x,.28f*scale,z),new Vector3(.85f,.55f,.85f)*scale,new Color(.67f,.37f,.24f),shape:PrimitiveType.Cylinder);
            Part("Plant soil",new Vector3(x,.56f*scale,z),new Vector3(.76f,.035f,.76f)*scale,new Color(.29f,.23f,.16f),shape:PrimitiveType.Cylinder);
            for(int i=0;i<3;i++)
            {
                float angle=i*2.1f;
                Part("Plant leaves",new Vector3(x+Mathf.Cos(angle)*.20f*scale,.85f*scale,z+Mathf.Sin(angle)*.20f*scale),new Vector3(.55f,.85f,.55f)*scale,new Color(.29f,.55f,.30f),shape:PrimitiveType.Sphere);
            }
        }
        void SceneryTree(float x,float z,float scale=1)
        {
            Part("Scenery tree trunk",new Vector3(x,1.1f*scale,z),new Vector3(.38f,2.2f,.38f)*scale,Oak,shape:PrimitiveType.Cylinder,solid:true);
            Part("Scenery tree crown",new Vector3(x,2.65f*scale,z),new Vector3(2.3f,2.7f,2.3f)*scale,new Color(.21f,.45f,.27f),shape:PrimitiveType.Sphere);
            Part("Scenery tree highlight",new Vector3(x-.45f*scale,3.4f*scale,z-.2f),new Vector3(1.6f,1.6f,1.6f)*scale,new Color(.40f,.63f,.32f),shape:PrimitiveType.Sphere);
        }
        void OutdoorBench(float x,float z)
        {
            Table("Park bench",x,z,2.6f,.8f,Oak).localScale=new Vector3(1,.6f,1);
            Part("Park bench back",new Vector3(x,1.0f,z+.4f),new Vector3(2.6f,.7f,.12f),PaleWood);
        }
        void SchoolBlock(float x,float z,float yaw,int bays)
        {
            var group=Group("School classroom block",x,z);group.localRotation=Quaternion.Euler(0,yaw,0);
            float width=bays*3.5f;
            Part("School building body",new Vector3(0,3,0),new Vector3(width,6,3.4f),new Color(.89f,.79f,.58f),group,solid:true);
            Part("School red roof",new Vector3(0,6.13f,0),new Vector3(width+.6f,.28f,4.1f),new Color(.64f,.29f,.20f),group);
            Part("School floor band",new Vector3(0,3,-1.78f),new Vector3(width,.22f,.18f),Color.white,group);
            for(int floor=0;floor<2;floor++)for(int i=0;i<bays;i++)
            {
                float bx=(i-(bays-1)*.5f)*3.5f;
                Part("Classroom window frame",new Vector3(bx,1.8f+floor*2.9f,-1.77f),new Vector3(2.1f,1.55f,.18f),Color.white,group);
                Part("Classroom blue window",new Vector3(bx,1.8f+floor*2.9f,-1.88f),new Vector3(1.85f,1.3f,.06f),new Color(.32f,.61f,.72f),group);
                Part("School corridor pillar",new Vector3(bx-1.5f,1.5f,-2.3f),new Vector3(.22f,3,.22f),new Color(.92f,.88f,.75f),group);
            }
        }
        void SchoolYard()
        {
            SchoolBlock(0,10,0,7);SchoolBlock(-16,0,-90,4);SchoolBlock(16,0,90,4);
            Part("School flagpole",new Vector3(0,3.5f,7.5f),new Vector3(.12f,7,.12f),Steel,shape:PrimitiveType.Cylinder);
            Part("School flag",new Vector3(.9f,6.2f,7.5f),new Vector3(1.7f,1.1f,.045f),new Color(.80f,.13f,.12f));
            var emblem=new GameObject("Five point flag star",typeof(MeshFilter),typeof(MeshRenderer));emblem.transform.SetParent(sceneryRoot,false);emblem.transform.localPosition=new Vector3(.9f,6.2f,7.46f);
            var vertices=new Vector3[11];var triangles=new int[30];
            for(int i=0;i<10;i++)
            {
                float angle=(90+i*36)*Mathf.Deg2Rad,radius=i%2==0?.36f:.15f;
                vertices[i+1]=new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0);
                triangles[i*3]=0;triangles[i*3+1]=(i+1)%10+1;triangles[i*3+2]=i+1;
            }
            var mesh=new Mesh{name="Flag star mesh",vertices=vertices,triangles=triangles};mesh.RecalculateNormals();
            emblem.GetComponent<MeshFilter>().sharedMesh=mesh;emblem.GetComponent<MeshRenderer>().sharedMaterial=Material(new Color(1,.85f,.24f));
            generatedMeshes.Add(mesh);
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<3;i++)SceneryTree(side*10.3f,-5.5f+i*5.1f,1.05f);
                OutdoorBench(side*9,-.3f);
            }
            // Flat court markings leave the central route fully walkable.
            foreach(float x in new[]{-5f,5f})Part("School court sideline",new Vector3(x,.035f,-3),new Vector3(.065f,.015f,7),Color.white);
            foreach(float z in new[]{-6.5f,.5f})Part("School court endline",new Vector3(0,.035f,z),new Vector3(10,.015f,.065f),Color.white);
            Part("Court center line",new Vector3(0,.035f,-3),new Vector3(10,.015f,.065f),Color.white);
            Part("Basketball post",new Vector3(9.5f,1.9f,3),new Vector3(.13f,3.8f,.13f),Steel,solid:true);
            Part("Basketball backboard",new Vector3(9.5f,3.7f,2.8f),new Vector3(1.8f,1.0f,.10f),Color.white);
        }
        void ParkPath()
        {
            foreach(float z in new[]{-6.5f,-3f})Part("Park broad walking path",new Vector3(0,.02f,z),new Vector3(19,.035f,1.8f),new Color(.79f,.71f,.56f));
            foreach(float x in new[]{-7f,0,7f})Part("Park connecting path",new Vector3(x,.022f,-1.5f),new Vector3(1.8f,.035f,10),new Color(.79f,.71f,.56f));
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<4;i++)SceneryTree(side*(10.0f+(i%2)*2.5f),-7+i*4,1.1f);
                OutdoorBench(side*4,1.3f);
                Part("Park lamp post",new Vector3(side*9.6f,1.9f,-2),new Vector3(.12f,3.8f,.12f),ViewFactory.Ink,solid:true);
                Part("Park lamp head",new Vector3(side*9.6f,3.9f,-2),new Vector3(.65f,.45f,.65f),new Color(.97f,.89f,.61f));
            }
            Part("Park pond bank",new Vector3(-17,.02f,-1),new Vector3(6.5f,.20f,8),new Color(.65f,.62f,.48f),shape:PrimitiveType.Cylinder);
            Part("Park pond water",new Vector3(-17,.14f,-1),new Vector3(5.7f,.035f,7.2f),new Color(.30f,.62f,.72f),shape:PrimitiveType.Cylinder);
        }
        void Picnic()
        {
            for(int side=-1;side<=1;side+=2)
            {
                SceneryTree(side*10.5f,-4,1.35f);SceneryTree(side*10.6f,4.8f,1.15f);
                var table=Table("Picnic table",side*6.7f,2.2f,3.8f,1.6f,Oak);
                foreach(float offset in new[]{-1.3f,1.3f})Table("Picnic bench",side*6.7f,2.2f+offset,3.8f,.65f,PaleWood).localScale=new Vector3(1,.55f,1);
                Part("Picnic fruit basket",new Vector3(0,1.42f,0),new Vector3(.8f,.4f,.7f),PaleWood,table);
                for(int i=0;i<3;i++)Part("Picnic fruit",new Vector3(-.25f+i*.25f,1.68f,0),Vector3.one*.28f,new Color(.84f,.35f,.17f),table,PrimitiveType.Sphere);
                for(int x=0;x<4;x++)for(int z=0;z<4;z++)Part("Picnic blanket square",new Vector3(side*5.7f+(x-1.5f)*.65f,.02f,-3.8f+(z-1.5f)*.65f),new Vector3(.64f,.025f,.64f),(x+z)%2==0?new Color(.87f,.48f,.32f):new Color(.98f,.88f,.70f));
            }
            Part("Picnic shelter platform",new Vector3(0,.14f,11),new Vector3(7,.28f,5),PaleWood);
            for(int i=0;i<4;i++)Part("Picnic shelter column",new Vector3(i%2==0?-3:3,1.8f,11+(i<2?-2:2)),new Vector3(.22f,3.6f,.22f),Oak);
            Part("Picnic shelter roof",new Vector3(0,3.8f,11),new Vector3(8,.4f,6),new Color(.57f,.31f,.24f));
        }
        void CommunityGarden()
        {
            for(int side=-1;side<=1;side+=2)
            {
                for(int row=0;row<2;row++)
                {
                    float x=side*6.8f,z=-4.5f+row*4.5f;
                    Part("Raised vegetable bed",new Vector3(x,.22f,z),new Vector3(2.8f,.44f,3.2f),Oak,solid:true);
                    Part("Vegetable bed soil",new Vector3(x,.46f,z),new Vector3(2.5f,.05f,2.9f),new Color(.30f,.22f,.14f));
                    for(int plant=0;plant<6;plant++)Part("Vegetable leaves",new Vector3(x+(plant%2==0?-.65f:.65f),.77f,z-1+(plant/2)),new Vector3(.75f,.65f,.75f),plant%2==0?new Color(.26f,.50f,.25f):new Color(.46f,.64f,.30f),shape:PrimitiveType.Sphere);
                }
                SceneryTree(side*12.5f,3,1.3f);
            }
            Part("Garden main path",new Vector3(0,.02f,-1.8f),new Vector3(8,.035f,12),new Color(.72f,.67f,.49f));
            Part("Garden shed",new Vector3(-9,1.7f,9),new Vector3(5,3.4f,4),new Color(.57f,.41f,.25f),solid:true);
            Part("Garden shed roof",new Vector3(-9,3.55f,9),new Vector3(5.6f,.3f,4.6f),new Color(.29f,.45f,.36f));
            Part("Garden shed door",new Vector3(-9,1.2f,6.96f),new Vector3(1.5f,2.4f,.10f),PaleWood);
            for(int i=0;i<2;i++)Part("Garden compost box",new Vector3(8+i*2.1f,.6f,4.8f),new Vector3(1.7f,1.2f,1.5f),new Color(.45f,.37f,.22f),solid:true);
            Part("Garden wheelbarrow tray",new Vector3(8.9f,.65f,-6.4f),new Vector3(1.2f,.55f,1.7f),new Color(.29f,.53f,.39f));
            Part("Wheelbarrow wheel",new Vector3(8.9f,.27f,-5.5f),new Vector3(.55f,.20f,.55f),ViewFactory.Ink,shape:PrimitiveType.Cylinder);
        }
    }
}
