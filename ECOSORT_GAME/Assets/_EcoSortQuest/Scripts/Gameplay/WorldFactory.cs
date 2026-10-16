using System.Collections.Generic;
using UnityEngine;

namespace EcoSortQuest
{
    // Primitive-based low-poly art. Shared by runtime scenes and the prefab setup tool.
    public sealed partial class WorldFactory
    {
        readonly Transform root;
        readonly List<Material> materials = new List<Material>();
        public WorldFactory(Transform root) { this.root = root; }
        public Material Material(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .22f);
            materials.Add(mat); return mat;
        }
        public GameObject Shape(string name, PrimitiveType shape, Transform parent, Vector3 position, Vector3 scale, Color color, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(shape); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Material(color);
            if (!collider)
            {
                var col = go.GetComponent<Collider>(); col.enabled = false;
                if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col);
            }
            return go;
        }
        public Camera BuildEnvironment(bool conveyor)
        {
            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera"; cameraGo.transform.SetParent(root, false);
            cameraGo.transform.position = new Vector3(8, 10, -13);
            cameraGo.transform.LookAt(new Vector3(0, .4f, 0));
            var camera = cameraGo.GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 6.7f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = ViewFactory.Cream;
            camera.nearClipPlane = .1f; camera.farClipPlane = 80;
            var lightGo = new GameObject("Key Light", typeof(Light)); lightGo.transform.SetParent(root, false);
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = lightGo.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.6f;
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientLight = new Color(.65f, .72f, .70f);
            Shape("Sorting island", PrimitiveType.Cube, root, new Vector3(0, -.3f, 0), new Vector3(14, .5f, 10), new Color(.79f, .86f, .78f));
            for (int i = 0; i < 6; i++)
            {
                float x = i % 2 == 0 ? -6 : 6; float z = -3 + i / 2 * 3;
                Shape("Planter", PrimitiveType.Cylinder, root, new Vector3(x, .25f, z), new Vector3(.7f, .3f, .7f), new Color(.74f, .55f, .35f), false);
                Shape("Tree", PrimitiveType.Sphere, root, new Vector3(x, 1, z), new Vector3(1.1f, 1.4f, 1.1f), new Color(.25f, .53f, .35f), false);
            }
            if (conveyor)
            {
                Shape("Conveyor frame", PrimitiveType.Cube, root, new Vector3(0, .45f, 1.1f), new Vector3(9, .65f, 2), new Color(.18f, .29f, .30f));
                Shape("Conveyor belt", PrimitiveType.Cube, root, new Vector3(0, .81f, 1.1f), new Vector3(8.7f, .1f, 1.65f), new Color(.36f, .44f, .43f));
                for (int i = 0; i < 15; i++)
                    Shape("Belt seam", PrimitiveType.Cube, root, new Vector3(-4.2f + i * .6f, .87f, 1.1f), new Vector3(.035f, .015f, 1.6f), new Color(.55f, .62f, .59f), false);
                for (int i = 0; i < 4; i++)
                    Shape("Conveyor support", PrimitiveType.Cube, root, new Vector3(i < 2 ? -3.5f : 3.5f, .1f, i % 2 == 0 ? .5f : 1.7f), new Vector3(.2f, .8f, .2f), ViewFactory.Ink, false);
            }
            return camera;
        }
        public WasteBin Bin(WasteType type, Vector3 position)
        {
            var go = new GameObject(type + " Bin"); go.transform.SetParent(root, false); go.transform.localPosition = position;
            var bin = go.AddComponent<WasteBin>(); bin.type = type; var color = WasteInfo.Color(type);
            Shape("Bin body", PrimitiveType.Cube, go.transform, new Vector3(0, .7f, 0), new Vector3(1.6f, 1.4f, 1.4f), color);
            Shape("Bin rim", PrimitiveType.Cube, go.transform, new Vector3(0, 1.45f, 0), new Vector3(1.72f, .16f, 1.5f), color * .8f);
            Shape("Opening", PrimitiveType.Cube, go.transform, new Vector3(0, 1.54f, 0), new Vector3(1.1f, .03f, .85f), ViewFactory.Ink, false);
            var label = new GameObject("Category label", typeof(TextMesh)); label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0, .85f, -.711f);
            label.transform.localRotation = Quaternion.Euler(0, 180, 0);
            // TextMesh faces local -Z; identity faces the gameplay camera on -Z.
            label.transform.localRotation = Quaternion.identity;
            var text = label.GetComponent<TextMesh>(); text.text = type.ToString().ToUpperInvariant();
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
            text.fontSize = 50; text.characterSize = .065f; text.color = ViewFactory.Ink;
            return bin;
        }
        public WasteItem Item(WasteType type, Vector3 position)
        {
            if (type == WasteType.Organic) return CatalogItem(WasteCatalog.Get(type,0),position);
            var go = new GameObject(type + " Item"); go.transform.SetParent(root, false); go.transform.localPosition = position;
            var item = go.AddComponent<WasteItem>(); item.type = type;
            var color = WasteInfo.Color(type);
            if (type == WasteType.Plastic)
            {
                Shape("Bottle", PrimitiveType.Cylinder, go.transform, new Vector3(0, .55f, 0), new Vector3(.65f, .55f, .65f), color);
                Shape("Shoulder", PrimitiveType.Sphere, go.transform, new Vector3(0, 1.05f, 0), new Vector3(.64f, .37f, .64f), color);
                Shape("Cap", PrimitiveType.Cylinder, go.transform, new Vector3(0, 1.3f, 0), new Vector3(.26f, .13f, .26f), Color.white);
                Shape("Bottle label", PrimitiveType.Cylinder, go.transform, new Vector3(0, .6f, 0), new Vector3(.67f, .18f, .67f), new Color(.9f, .96f, .94f), false);
            }
            else if (type == WasteType.Metal)
            {
                Shape("Can", PrimitiveType.Cylinder, go.transform, new Vector3(0, .5f, 0), new Vector3(.8f, .5f, .8f), color);
                Shape("Can top", PrimitiveType.Cylinder, go.transform, new Vector3(0, 1.01f, 0), new Vector3(.76f, .025f, .76f), new Color(.8f, .83f, .82f), false);
                Shape("Pull tab", PrimitiveType.Cube, go.transform, new Vector3(0, 1.05f, 0), new Vector3(.12f, .02f, .25f), ViewFactory.Ink, false);
            }
            else
            {
                var sheet = Shape("Folded paper", PrimitiveType.Cube, go.transform, new Vector3(0, .16f, 0), new Vector3(1.1f, .27f, .8f), new Color(.97f, .94f, .81f));
                sheet.transform.localRotation = Quaternion.Euler(0, -15, 0);
                for (int i = 0; i < 3; i++) Shape("Printed line", PrimitiveType.Cube, go.transform,
                    new Vector3(0, .305f, -.2f + i * .17f), new Vector3(.65f, .015f, .035f), ViewFactory.Muted, false);
            }
            item.selectionMarker = Shape("Selected", PrimitiveType.Cylinder, go.transform, new Vector3(0, .025f, 0), new Vector3(1.45f, .015f, 1.45f), new Color(1, .87f, .28f), false);
            item.Select(false); return item;
        }
        public void Dispose()
        { foreach (var material in materials) if (material != null) Object.Destroy(material); materials.Clear(); }
    }
}
