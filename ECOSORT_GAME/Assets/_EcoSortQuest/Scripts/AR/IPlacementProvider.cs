using UnityEngine;
namespace EcoSortQuest
{
    public interface IPlacementProvider { bool TryPlace(Vector2 screenPoint, Camera camera, out Vector3 position); }
    // Desktop/editor fallback; mobile players use ARFoundationPlacementProvider.
    public sealed class SimulatedPlacementProvider : IPlacementProvider
    {
        public bool TryPlace(Vector2 screenPoint, Camera camera, out Vector3 position)
        {
            if (camera == null) { position = default; return false; }
            Ray ray = camera.ScreenPointToRay(screenPoint);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
            {
                position = ray.GetPoint(distance);
                return Mathf.Abs(position.x) <= 4 && Mathf.Abs(position.z) <= 3;
            }
            position = default; return false;
        }
    }
}
