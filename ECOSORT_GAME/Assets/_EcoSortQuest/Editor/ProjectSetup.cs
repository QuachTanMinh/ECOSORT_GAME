using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoSortQuest.Editor
{
    public static class ProjectSetup
    {
        public const string Root = "Assets/_EcoSortQuest";
        static readonly string[] SceneNames = { "MainMenu", "GameScene", "ScanScene", "ARChallengeScene", "ProgressScene" };

        [MenuItem("EcoSort/Setup missing assets and scenes")]
        public static void Build()
        {
            foreach (string folder in new[] { "Art/Materials", "Art/Models", "Prefabs/Bins", "Prefabs/Items", "Prefabs/UI", "Scenes", "ScriptableObjects" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            var settings = GetOrCreate<ApiSettings>(Root + "/ScriptableObjects/ApiSettings.asset");
            var levels = new LevelConfig[3];
            for (int i = 0; i < 3; i++)
            {
                string path = Root + "/ScriptableObjects/Level" + (i + 1) + ".asset";
                bool exists = File.Exists(path); levels[i] = GetOrCreate<LevelConfig>(path);
                if (!exists)
                {
                    levels[i].level = i + 1; levels[i].timeLimit = i == 0 ? 0 : i == 1 ? 75 : 50;
                    levels[i].description = i == 0 ? "Làm quen ba nhóm rác — không giới hạn thời gian." :
                        i == 1 ? "Phân loại 10 vật phẩm trong 75 giây." : "Thử thách 50 giây — vượt màn để mở Real-World Challenge.";
                    EditorUtility.SetDirty(levels[i]);
                }
            }
            var items = new WasteItem[3]; var bins = new WasteBin[3];
            for (int i = 0; i < 3; i++)
            {
                var type = (WasteType)i;
                string itemPath = Root + "/Prefabs/Items/" + type + "Item.prefab";
                string binPath = Root + "/Prefabs/Bins/" + type + "Bin.prefab";
                if (!File.Exists(itemPath)) CreatePrefab(type, false, itemPath);
                if (!File.Exists(binPath)) CreatePrefab(type, true, binPath);
                items[i] = AssetDatabase.LoadAssetAtPath<GameObject>(itemPath).GetComponent<WasteItem>();
                bins[i] = AssetDatabase.LoadAssetAtPath<GameObject>(binPath).GetComponent<WasteBin>();
            }
            if (string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
            {
                if (Application.isBatchMode) EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
                else throw new InvalidOperationException("Open a saved scene before running EcoSort Setup. Your unsaved scene was not changed.");
            }
            Scene previous = SceneManager.GetActiveScene();
            for (int i = 0; i < SceneNames.Length; i++)
            {
                string scenePath = Root + "/Scenes/" + SceneNames[i] + ".unity";
                if (File.Exists(scenePath)) continue; // Never overwrite a hand-edited scene.
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                var root = new GameObject("EcoSort / " + SceneNames[i]);
                var app = root.AddComponent<EcoSortApp>(); app.screen = (ClientScreen)i;
                app.apiSettings = settings; app.levels = levels; app.itemPrefabs = items; app.binPrefabs = bins;
                app.authoredWorld = new GameObject("Environment").transform; app.authoredWorld.SetParent(root.transform, false);
                var factory = new WorldFactory(app.authoredWorld);
                app.authoredCamera = factory.BuildEnvironment(i != (int)ClientScreen.ARChallenge);
                PersistMaterials(root);
                EditorSceneManager.SaveScene(scene, scenePath); EditorSceneManager.CloseScene(scene, true);
            }
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            var newScenes = SceneNames.Select(name => new EditorBuildSettingsScene(Root + "/Scenes/" + name + ".unity", true));
            var existing = EditorBuildSettings.scenes.Where(s => !SceneNames.Any(name => s.path == Root + "/Scenes/" + name + ".unity"));
            EditorBuildSettings.scenes = newScenes.Concat(existing).ToArray();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("EcoSort: 5 scenes, 3 levels, 6 prefabs and API settings ready. Open MainMenu and press Play.");
        }
        static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        static void CreatePrefab(WasteType type, bool bin, string path)
        {
            var temporary = new GameObject("Prefab authoring");
            try
            {
                var factory = new WorldFactory(temporary.transform);
                var content = bin ? factory.Bin(type, Vector3.zero).gameObject : factory.Item(type, Vector3.zero).gameObject;
                PersistMaterials(content); PrefabUtility.SaveAsPrefabAsset(content, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(temporary); }
        }
        static void PersistMaterials(GameObject root)
        {
            var replacements=new System.Collections.Generic.Dictionary<Material,Material>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer.sharedMaterial;
                if(material!=null && replacements.TryGetValue(material,out var replacement))
                { renderer.sharedMaterial=replacement;continue; }
                if (material == null || AssetDatabase.Contains(material) || !material.HasProperty("_BaseColor")) continue;
                string hex = ColorUtility.ToHtmlStringRGBA(material.color);
                string path = Root + "/Art/Materials/Color_" + hex + ".mat";
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing == null) { material.name = "EcoSort " + hex; AssetDatabase.CreateAsset(material, path); }
                else { renderer.sharedMaterial = existing; replacements[material]=existing; }
            }
            // Shared runtime palette entries can be referenced by several renderers.
            foreach(var material in replacements.Keys)UnityEngine.Object.DestroyImmediate(material);
        }
        [MenuItem("EcoSort/Open Main Menu")]
        public static void OpenMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(Root + "/Scenes/MainMenu.unity");
        }
        [MenuItem("EcoSort/Build Windows")]
        public static void BuildWindows()
        {
            Build(); BuildWindowsCurrent();
        }
        [MenuItem("EcoSort/Build Windows (current scenes)")]
        public static void BuildWindowsCurrent()
        {
            BuildWindowsTo("Builds/Windows/EcoSortQuest.exe");
        }
        public static void BuildWindowsUI()
        {
            BuildWindowsTo("Builds/Windows-UI/EcoSortQuest.exe");
        }
        static void BuildWindowsTo(string output)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = SceneNames.Select(n => Root + "/Scenes/" + n + ".unity").ToArray(),
                locationPathName = output, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Windows build failed: " + report.summary.result);
            Debug.Log("EcoSort Windows build: " + report.summary.totalSize + " bytes");
        }
    }
}
