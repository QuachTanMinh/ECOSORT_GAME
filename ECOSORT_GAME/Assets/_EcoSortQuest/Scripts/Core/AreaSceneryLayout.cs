using UnityEngine;

namespace EcoSortQuest
{
    // Layout shared by scenery, movement and item placement. Coordinates are floor positions.
    public sealed class AreaSceneryLayout
    {
        public readonly string Id;
        public readonly bool Indoor;
        public readonly Rect WalkBounds;
        public readonly Vector3 Spawn, Collection;
        public readonly Vector3[] Items;
        public float BinScale => Indoor ? .7f : .9f;
        public AreaSceneryLayout(string id,bool indoor,params Vector3[] items)
        {
            Id=id;Indoor=indoor;Items=items;
            WalkBounds=indoor?Rect.MinMaxRect(-9.45f,-7.65f,9.45f,5.25f):Rect.MinMaxRect(-11,-8,11,5);
            Spawn=new Vector3(0,.1f,-6.7f);Collection=new Vector3(0,0,4.25f);
        }
        static Vector3 P(float x,float z)=>new Vector3(x,.04f,z);
        public static AreaSceneryLayout For(AreaMission mission)
        {
            switch(mission.Id)
            {
                case "home-kitchen":return new AreaSceneryLayout(mission.Id,true,P(-4.3f,-4.8f),P(2,-4.5f),P(-3,-.4f),P(1,1.4f));
                case "home-study":return new AreaSceneryLayout(mission.Id,true,P(-3,-4.8f),P(3,-4.5f),P(-2.7f,-1),P(2.7f,1));
                case "home-separate":return new AreaSceneryLayout(mission.Id,true,P(-3.2f,-4.7f),P(2.5f,-4.7f),P(-3,-.5f),P(2.5f,.7f));
                case "school-class":return new AreaSceneryLayout(mission.Id,true,P(-2.7f,-4.9f),P(2.7f,-4.9f),P(-2.8f,.8f),P(2.8f,.8f));
                case "school-canteen":return new AreaSceneryLayout(mission.Id,true,P(-3.5f,-4.9f),P(1,-4.9f),P(-3,-.7f),P(1,1));
                case "school-yard":return new AreaSceneryLayout(mission.Id,false,P(-7,-6),P(0,-6),P(7,-6),P(-7,-2),P(0,-2),P(7,-2),P(-4,1),P(4,1));
                case "park-path":return new AreaSceneryLayout(mission.Id,false,P(-7,-6.5f),P(0,-6.5f),P(7,-6.5f),P(7,-3),P(0,-3),P(-7,-3),P(-7,1),P(7,1));
                case "park-picnic":return new AreaSceneryLayout(mission.Id,false,P(-7,-6),P(0,-6),P(7,-6),P(-7,-2),P(0,-2),P(7,-2),P(-3,1),P(3,1));
                case "park-restore":return new AreaSceneryLayout(mission.Id,false,P(-3,-6),P(0,-6),P(3,-6),P(-9,-2.5f),P(0,-3),P(9,-2.5f),P(-3,0),P(0,0),P(3,0),P(-9,2),P(0,2),P(9,2));
                default:throw new System.ArgumentException("Missing scenery layout: "+mission.Id);
            }
        }
    }
}
