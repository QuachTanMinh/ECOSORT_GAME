using UnityEngine;

namespace EcoSortQuest
{
    public sealed partial class WorldFactory
    {
        public ExplorerController Explorer(Vector3 position, Camera camera)
        {
            var actor=new GameObject("Eco Ranger",typeof(CharacterController),typeof(ExplorerController));
            actor.transform.SetParent(root,false);actor.transform.position=position;
            var controller=actor.GetComponent<ExplorerController>();controller.FollowCamera=camera;
            var visual=new GameObject("Block character").transform;visual.SetParent(actor.transform,false);controller.Visual=visual;
            var skin=new Color(.88f,.65f,.43f);var shirt=new Color(.17f,.53f,.46f);var jeans=new Color(.19f,.26f,.39f);
            Shape("Torso",PrimitiveType.Cube,visual,new Vector3(0,1.12f,0),new Vector3(.66f,.65f,.35f),shirt,false);
            Shape("Head",PrimitiveType.Cube,visual,new Vector3(0,1.68f,0),new Vector3(.47f,.47f,.43f),skin,false);
            Shape("Hair",PrimitiveType.Cube,visual,new Vector3(0,1.92f,-.03f),new Vector3(.49f,.11f,.46f),new Color(.19f,.13f,.09f),false);
            for(int i=-1;i<=1;i+=2)
                Shape("Eye",PrimitiveType.Cube,visual,new Vector3(i*.11f,1.70f,.22f),new Vector3(.055f,.055f,.025f),ViewFactory.Ink,false);
            Shape("Backpack",PrimitiveType.Cube,visual,new Vector3(0,1.13f,-.25f),new Vector3(.5f,.5f,.22f),new Color(.91f,.68f,.2f),false);
            Transform Limb(string name,Vector3 pivot,Vector3 offset,Vector3 scale,Color color)
            {var joint=new GameObject(name).transform;joint.SetParent(visual,false);joint.localPosition=pivot;Shape(name+" block",PrimitiveType.Cube,joint,offset,scale,color,false);return joint;}
            controller.LeftArm=Limb("Left arm",new Vector3(-.45f,1.38f,0),new Vector3(0,-.28f,0),new Vector3(.22f,.6f,.25f),skin);
            controller.RightArm=Limb("Right arm",new Vector3(.45f,1.38f,0),new Vector3(0,-.28f,0),new Vector3(.22f,.6f,.25f),skin);
            controller.LeftLeg=Limb("Left leg",new Vector3(-.18f,.82f,0),new Vector3(0,-.38f,0),new Vector3(.27f,.75f,.3f),jeans);
            controller.RightLeg=Limb("Right leg",new Vector3(.18f,.82f,0),new Vector3(0,-.38f,0),new Vector3(.27f,.75f,.3f),jeans);
            controller.CarryAnchor=new GameObject("Carried item").transform;controller.CarryAnchor.SetParent(visual,false);controller.CarryAnchor.localPosition=new Vector3(0,.8f,.65f);
            return controller;
        }
        public void PrepareWalkableWorld()
        {
            foreach(var t in root.GetComponentsInChildren<Transform>())
            {
                if(t.name.StartsWith("Conveyor") || t.name=="Belt seam" || t.name=="Sorting island" || t.name=="Tree" || t.name=="Planter") t.gameObject.SetActive(false);
                if(t.name.EndsWith("facade") || t.name=="Raised garden" || t.name=="Bench back" || t.name=="Outdoor table")
                {t.gameObject.AddComponent<BoxCollider>();}
            }
            Shape("Walkable ground",PrimitiveType.Cube,root,new Vector3(0,-.18f,-1),new Vector3(26,.25f,20),new Color(.64f,.73f,.52f));
            // Solid low walls give a visible boundary and keep every item reachable.
            for(int side=-1;side<=1;side+=2)
                Shape("Boundary wall",PrimitiveType.Cube,root,new Vector3(side*11.8f,.25f,-1.5f),new Vector3(.25f,.5f,15),new Color(.66f,.55f,.39f));
        }
    }
}
