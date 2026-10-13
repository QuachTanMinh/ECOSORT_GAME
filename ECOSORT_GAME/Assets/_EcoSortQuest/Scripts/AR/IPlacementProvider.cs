using UnityEngine;
namespace EcoSortQuest
{
    public interface IPlacementProvider { bool TryPlace(Ray ray, out Vector3 position); }
    // PC simulation only. A future ARRaycastManager adapter implements the same interface.
    public sealed class SimulatedPlacementProvider : IPlacementProvider
    {
        public bool TryPlace(Ray ray, out Vector3 position)
        {
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
