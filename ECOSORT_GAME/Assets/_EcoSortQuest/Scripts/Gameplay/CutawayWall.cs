using UnityEngine;

namespace EcoSortQuest
{
    // Keep collision, but hide a room's near wall when the camera is outside it.
    public sealed class CutawayWall : MonoBehaviour
    {
        public Vector3 Inward;
        Renderer surface;
        void Awake()=>surface=GetComponent<Renderer>();
        void LateUpdate()
        {
            var camera=Camera.main;
            if(camera!=null && surface!=null)surface.enabled=Vector3.Dot(camera.transform.position-transform.position,Inward)>-.1f;
        }
    }
}
