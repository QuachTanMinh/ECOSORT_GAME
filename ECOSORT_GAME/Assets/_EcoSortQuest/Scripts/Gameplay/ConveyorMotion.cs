using System.Collections.Generic;
using UnityEngine;

namespace EcoSortQuest
{
    public sealed class ConveyorMotion : MonoBehaviour
    {
        readonly List<Transform> seams = new List<Transform>();
        public void Configure(Transform environment)
        {
            seams.Clear();
            foreach(var part in environment.GetComponentsInChildren<Transform>())
                if(part.name == "Belt seam") seams.Add(part);
        }
        public static float Advance(float x, float distance) => Mathf.Repeat(x + distance + 4.35f, 8.7f) - 4.35f;
        public void Tick(float distance)
        {
            foreach(var seam in seams)
            { var p=seam.localPosition; p.x=Advance(p.x,distance); seam.localPosition=p; }
        }
    }
}
