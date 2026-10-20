using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

namespace EcoSortQuest
{
    /// <summary>Builds the AR Foundation camera/session rig for supported mobile players.</summary>
    public static class ARDeviceSession
    {
        public static ARRaycastManager Create(Camera camera, Transform contentRoot)
        {
            var sessionObject = new GameObject("EcoSort AR Session", typeof(ARSession), typeof(ARInputManager));
            var originObject = new GameObject("EcoSort XR Origin");
            var origin = originObject.AddComponent<XROrigin>();
            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(originObject.transform, false);

            camera.transform.SetParent(cameraOffset.transform, false);
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localRotation = Quaternion.identity;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 50f;
            camera.tag = "MainCamera";

            origin.Camera = camera;
            origin.CameraFloorOffsetObject = cameraOffset;

            var poseDriver = camera.GetComponent<TrackedPoseDriver>();
            if (poseDriver == null) poseDriver = camera.gameObject.AddComponent<TrackedPoseDriver>();
            poseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            poseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            poseDriver.positionInput = new InputActionProperty(new InputAction("AR Device Position", binding: "<XRHMD>/centerEyePosition"));
            poseDriver.rotationInput = new InputActionProperty(new InputAction("AR Device Rotation", binding: "<XRHMD>/centerEyeRotation"));

            if (camera.GetComponent<ARCameraManager>() == null) camera.gameObject.AddComponent<ARCameraManager>();
            if (camera.GetComponent<ARCameraBackground>() == null) camera.gameObject.AddComponent<ARCameraBackground>();

            var planes = originObject.AddComponent<ARPlaneManager>();
            planes.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            planes.planePrefab = CreatePlaneVisual(sessionObject.transform);

            var raycasts = originObject.AddComponent<ARRaycastManager>();
            raycasts.enabled = true;
            contentRoot.SetParent(null, true);
            return raycasts;
        }

        static GameObject CreatePlaneVisual(Transform parent)
        {
            var plane = new GameObject("Detected Plane", typeof(MeshFilter), typeof(MeshRenderer), typeof(ARPlane), typeof(ARPlaneMeshVisualizer));
            plane.transform.SetParent(parent, false);
            var renderer = plane.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var material = new Material(shader) { color = new Color(.25f, .78f, .55f, .18f) };
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return plane;
        }
    }
}
