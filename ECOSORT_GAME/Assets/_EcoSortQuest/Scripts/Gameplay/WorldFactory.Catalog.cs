using UnityEngine;

namespace EcoSortQuest
{
    public sealed partial class WorldFactory
    {
        public WasteItem CatalogItem(WasteDefinition definition, Vector3 position)
        {
            var go = new GameObject(definition.Name); go.transform.SetParent(root, false); go.transform.localPosition = position;
            var item = go.AddComponent<WasteItem>(); item.type = definition.Type; item.Definition = definition;
            var ink = ViewFactory.Ink; var paper = new Color(.96f, .92f, .77f);
            var silver = new Color(.72f, .78f, .81f); var blue = new Color(.22f, .67f, .82f);
            var orange = new Color(.91f, .47f, .20f); var brown = new Color(.67f, .43f, .23f);
            Transform parent = go.transform;
            GameObject Box(string name, Vector3 p, Vector3 s, Color c) => Shape(name, PrimitiveType.Cube, parent, p, s, c, false);
            GameObject Round(string name, Vector3 p, Vector3 s, Color c) => Shape(name, PrimitiveType.Cylinder, parent, p, s, c, false);
            int model = definition.Model;
            if (definition.Type == WasteType.Plastic)
            {
                if (model == 0 || model == 5)
                {
                    Round("Bottle body", new Vector3(0,.45f,0), new Vector3(model == 0 ? .55f : .72f,.45f,.55f), model == 0 ? blue : new Color(.64f,.40f,.79f));
                    Shape("Shoulder", PrimitiveType.Sphere,parent,new Vector3(0,.88f,0),new Vector3(.55f,.25f,.55f),blue,false);
                    Round("Cap",new Vector3(0,1.08f,0),new Vector3(.24f,.1f,.24f),Color.white);
                    Round("Label",new Vector3(0,.46f,0),new Vector3(model == 0 ? .57f : .74f,.13f,.57f),paper);
                }
                else if (model == 1)
                {
                    Box("Detergent body",new Vector3(0,.46f,0),new Vector3(.85f,.9f,.5f),orange);
                    Round("Spout",new Vector3(-.2f,1,0),new Vector3(.3f,.12f,.3f),blue);
                    Box("Handle top",new Vector3(.25f,1,0),new Vector3(.38f,.12f,.15f),orange);
                    Box("Handle side",new Vector3(.46f,.78f,0),new Vector3(.12f,.48f,.15f),orange);
                    Box("Label",new Vector3(0,.5f,-.26f),new Vector3(.55f,.45f,.02f),paper);
                }
                else if (model == 2)
                {
                    Round("Cup",new Vector3(0,.35f,0),new Vector3(.65f,.35f,.65f),blue);
                    Round("Rim",new Vector3(0,.72f,0),new Vector3(.75f,.04f,.75f),Color.white);
                    Round("Inside",new Vector3(0,.765f,0),new Vector3(.6f,.01f,.6f),ink);
                }
                else if (model == 3)
                {
                    Box("Food box",new Vector3(0,.2f,0),new Vector3(1,.4f,.7f),new Color(.55f,.80f,.86f));
                    Box("Blue lid",new Vector3(0,.43f,0),new Vector3(1.08f,.09f,.78f),blue);
                }
                else
                {
                    Round("Large cap",new Vector3(0,.13f,0),new Vector3(.8f,.13f,.8f),orange);
                    for(int i=0;i<10;i++)
                    {
                        float a=i*Mathf.PI/5;
                        Box("Grip",new Vector3(Mathf.Cos(a)*.39f,.13f,Mathf.Sin(a)*.39f),new Vector3(.05f,.23f,.05f),paper);
                    }
                }
            }
            else if (definition.Type == WasteType.Metal)
            {
                if (model == 0 || model == 1 || model == 4)
                {
                    float radius=model==1?.86f:.65f;
                    Round("Metal body",new Vector3(0,.4f,0),new Vector3(radius,.4f,radius),model==0?orange:silver);
                    Round("Top rim",new Vector3(0,.82f,0),new Vector3(radius+.04f,.025f,radius+.04f),silver);
                    if(model==4)
                    {
                        Round("Cup opening",new Vector3(0,.85f,0),new Vector3(.52f,.01f,.52f),ink);
                        Box("Handle top",new Vector3(.45f,.65f,0),new Vector3(.35f,.1f,.12f),silver);
                        Box("Handle edge",new Vector3(.61f,.45f,0),new Vector3(.1f,.4f,.12f),silver);
                        Box("Handle bottom",new Vector3(.45f,.25f,0),new Vector3(.35f,.1f,.12f),silver);
                    }
                    else if(model==0) Box("Pull tab",new Vector3(0,.86f,0),new Vector3(.12f,.025f,.24f),ink);
                    else for(int i=0;i<5;i++) Round("Tin rib",new Vector3(0,.1f+i*.15f,0),new Vector3(.9f,.015f,.9f),ink);
                }
                else if(model==2)
                {
                    Round("Metal lid",new Vector3(0,.06f,0),new Vector3(.95f,.06f,.95f),silver);
                    Round("Lid ring",new Vector3(0,.13f,0),new Vector3(.72f,.02f,.72f),ink);
                    Round("Lid face",new Vector3(0,.155f,0),new Vector3(.66f,.01f,.66f),silver);
                }
                else if(model==3)
                {
                    Box("Tray base",new Vector3(0,.06f,0),new Vector3(1.15f,.1f,.8f),silver);
                    for(int i=-1;i<=1;i+=2)
                    {
                        Box("Tray edge",new Vector3(i*.55f,.17f,0),new Vector3(.07f,.25f,.85f),silver);
                        Box("Tray edge",new Vector3(0,.17f,i*.39f),new Vector3(1.15f,.25f,.07f),silver);
                    }
                }
                else
                {
                    Box("Biscuit tin",new Vector3(0,.24f,0),new Vector3(1,.48f,.72f),new Color(.75f,.22f,.24f));
                    Box("Metal lid",new Vector3(0,.51f,0),new Vector3(1.06f,.06f,.78f),silver);
                    Box("Tin pattern",new Vector3(0,.55f,0),new Vector3(.65f,.02f,.45f),orange);
                }
            }
            else
            {
                if(model==0 || model==3 || model==4)
                {
                    Box("Paper pages",new Vector3(0,.12f,0),new Vector3(1.1f,model==3?.22f:.1f,.75f),paper);
                    if(model==3) Box("Notebook cover",new Vector3(0,.245f,0),new Vector3(1.14f,.03f,.79f),blue);
                    else if(model==4)
                    {
                        var flap=Box("Envelope flap",new Vector3(0,.18f,0),new Vector3(.6f,.025f,.5f),Color.white);
                        flap.transform.localRotation=Quaternion.Euler(0,35,0);
                    }
                    else for(int i=0;i<5;i++) Box("Newsprint",new Vector3(0,.18f,-.25f+i*.12f),new Vector3(.85f,.012f,.025f),ink);
                }
                else if(model==1)
                {
                    Box("Cardboard box",new Vector3(0,.45f,0),new Vector3(.95f,.9f,.85f),brown);
                    Box("Packing tape",new Vector3(0,.91f,0),new Vector3(.16f,.025f,.86f),paper);
                    Box("Shipping label",new Vector3(0,.52f,-.43f),new Vector3(.5f,.32f,.02f),paper);
                }
                else if(model==2)
                {
                    Box("Paper bag",new Vector3(0,.4f,0),new Vector3(.8f,.8f,.44f),brown);
                    for(int i=-1;i<=1;i+=2)
                        Box("Paper handle side",new Vector3(i*.2f,.94f,0),new Vector3(.055f,.3f,.055f),paper);
                    Box("Paper handle top",new Vector3(0,1.08f,0),new Vector3(.45f,.055f,.055f),paper);
                }
                else
                {
                    Round("Cardboard tube",new Vector3(0,.42f,0),new Vector3(.65f,.42f,.65f),brown);
                    Round("Hollow",new Vector3(0,.85f,0),new Vector3(.5f,.015f,.5f),ink);
                }
            }
            // A single generous pickup collider keeps all small/flat variants easy to grab.
            var bounds = new Bounds(go.transform.position, Vector3.zero);
            foreach(var renderer in go.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            var collider = go.AddComponent<BoxCollider>(); collider.center = go.transform.InverseTransformPoint(bounds.center);
            collider.size = bounds.size + new Vector3(.12f,.12f,.12f);
            item.selectionMarker = Shape("Selected",PrimitiveType.Cylinder,parent,new Vector3(0,.02f,0),new Vector3(1.45f,.012f,1.45f),new Color(1,.87f,.28f),false);
            item.Select(false); return item;
        }
    }
}
