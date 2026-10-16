using UnityEngine;

namespace EcoSortQuest
{
    public sealed partial class WorldFactory
    {
        void OrganicModel(Transform parent, int model)
        {
            var yellow = new Color(.98f,.78f,.18f); var green = new Color(.32f,.62f,.20f);
            var cream = new Color(.96f,.88f,.66f); var brown = new Color(.43f,.25f,.12f);
            GameObject Piece(string name, Vector3 p, Vector3 s, Color c, PrimitiveType shape=PrimitiveType.Cube)
                => Shape(name,shape,parent,p,s,c,false);
            if(model==0)
            {
                for(int i=0;i<3;i++)
                {var peel=Piece("Banana peel",new Vector3((i-1)*.28f,.23f,0),new Vector3(.22f,.65f,.22f),yellow);peel.transform.localRotation=Quaternion.Euler(0,0,(i-1)*-45);}
                Piece("Banana stem",new Vector3(0,.65f,0),new Vector3(.13f,.17f,.13f),brown);
            }
            else if(model==1)
            {
                Piece("Apple core",new Vector3(0,.42f,0),new Vector3(.34f,.65f,.34f),cream);
                for(int side=-1;side<=1;side+=2)Piece("Red apple edge",new Vector3(0,.42f+side*.3f,0),new Vector3(.66f,.20f,.60f),new Color(.8f,.19f,.12f),PrimitiveType.Sphere);
                Piece("Apple stem",new Vector3(0,.86f,0),new Vector3(.08f,.2f,.08f),brown);
            }
            else if(model==2)
                for(int i=0;i<5;i++){float a=i*1.25f;var peel=Piece("Orange peel",new Vector3(Mathf.Cos(a)*.27f,.12f,Mathf.Sin(a)*.27f),new Vector3(.38f,.14f,.25f),new Color(1,.43f,.05f));peel.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,15);}
            else if(model==3 || model==8)
                for(int i=0;i<5;i++){var leaf=Piece(model==8?"Fallen leaf":"Vegetable leaf",new Vector3((i-2)*.13f,.10f+i*.035f,0),new Vector3(.23f,.06f,.75f),model==8?new Color(.68f,.42f,.12f):green);leaf.transform.localRotation=Quaternion.Euler(0,(i-2)*27,0);}
            else if(model==4)
            {Piece("Bread crust",new Vector3(0,.22f,0),new Vector3(.88f,.38f,.60f),new Color(.69f,.39f,.15f));Piece("Bread crumb",new Vector3(0,.43f,0),new Vector3(.72f,.045f,.47f),cream);}
            else if(model==5)
                for(int i=0;i<2;i++){Piece("Eggshell",new Vector3(i*.47f-.23f,.18f,0),new Vector3(.43f,.36f,.43f),cream,PrimitiveType.Sphere);Piece("Eggshell hollow",new Vector3(i*.47f-.23f,.35f,0),new Vector3(.32f,.03f,.32f),brown,PrimitiveType.Cylinder);}
            else if(model==6)
                for(int i=0;i<7;i++)Piece("Coffee grounds",new Vector3(Mathf.Sin(i*2)*.25f,.09f+(i%2)*.05f,Mathf.Cos(i*2)*.25f),new Vector3(.34f,.17f,.34f),brown,PrimitiveType.Sphere);
            else
            {Piece("Watermelon rind",new Vector3(0,.16f,0),new Vector3(1,.24f,.5f),green);Piece("White rind",new Vector3(0,.30f,0),new Vector3(.95f,.06f,.48f),cream);Piece("Red fruit",new Vector3(0,.36f,0),new Vector3(.85f,.08f,.42f),new Color(.88f,.25f,.25f));}
        }
        void ExtraModel(Transform parent, WasteType type, int model)
        {
            Color c=type==WasteType.Metal?new Color(.72f,.78f,.81f):type==WasteType.Paper?new Color(.94f,.89f,.72f):new Color(.30f,.65f,.83f);
            GameObject Piece(string n,Vector3 p,Vector3 s,PrimitiveType shape=PrimitiveType.Cube)=>Shape(n,shape,parent,p,s,c,false);
            if(model==6 && type!=WasteType.Paper)
            {Piece("Spoon handle",new Vector3(0,.08f,.2f),new Vector3(.12f,.09f,.8f));Piece("Spoon bowl",new Vector3(0,.11f,-.35f),new Vector3(.43f,.10f,.5f),PrimitiveType.Sphere);}
            else if(type==WasteType.Paper)
            {
                Piece("Paper",new Vector3(0,model==7?.25f:.08f,0),new Vector3(.85f,model==7?.5f:.06f,.65f));
                for(int i=0;i<4;i++)Shape("Print",PrimitiveType.Cube,parent,new Vector3(0,model==7?.51f:.12f,-.2f+i*.12f),new Vector3(.55f,.01f,.025f),ViewFactory.Muted,false);
            }
            else if(type==WasteType.Metal)
            {Piece(model==7?"Crown cap":"Foil ball",new Vector3(0,.18f,0),new Vector3(.65f,model==7?.12f:.38f,.65f),model==7?PrimitiveType.Cylinder:PrimitiveType.Sphere);}
            else
            {
                Piece(model==7?"Yogurt pot":"Plant pot",new Vector3(0,.3f,0),new Vector3(.7f,.3f,.7f),PrimitiveType.Cylinder);
                Shape("Opening",PrimitiveType.Cylinder,parent,new Vector3(0,.61f,0),new Vector3(.53f,.015f,.53f),ViewFactory.Ink,false);
                if(model==8)Shape("Pot rim",PrimitiveType.Cylinder,parent,new Vector3(0,.56f,0),new Vector3(.78f,.035f,.78f),c,false);
            }
        }
    }
}
