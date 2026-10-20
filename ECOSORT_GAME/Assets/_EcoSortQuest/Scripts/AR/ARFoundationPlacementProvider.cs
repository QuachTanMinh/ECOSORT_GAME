using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace EcoSortQuest
{
    /// <summary>Places the challenge on a tracked horizontal plane on Android and iOS.</summary>
    public sealed class ARFoundationPlacementProvider : IPlacementProvider
    {
        readonly ARRaycastManager raycasts;
        readonly List<ARRaycastHit> hits = new List<ARRaycastHit>(4);

        public ARFoundationPlacementProvider(ARRaycastManager raycasts) => this.raycasts = raycasts;

        public bool TryPlace(Vector2 screenPoint, Camera camera, out Vector3 position)
        {
            if (raycasts != null && raycasts.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon) && hits.Count > 0)
            {
                position = hits[0].pose.position;
                return true;
            }
            position = default;
            return false;
        }
    }
}
