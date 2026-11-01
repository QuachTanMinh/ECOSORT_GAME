using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoSortQuest.Editor
{
    public static class MenuFeatureSetup
    {
        [MenuItem("EcoSort/Wire player prompt and extra environments")]
        public static void Wire()
        {
            string main=ProjectSetup.Root+"/Scenes/MainMenu.unity";
            var scene=EditorSceneManager.OpenScene(main,OpenSceneMode.Single);
            var app=Object.FindFirstObjectByType<EcoSortApp>();
            if(app.GetComponent<PlayerManager>()==null)app.gameObject.AddComponent<PlayerManager>();
            if(app.GetComponent<PlayerNamePrompt>()==null)app.gameObject.AddComponent<PlayerNamePrompt>();
            if(app.GetComponent<MainMenuButtons>()==null)app.gameObject.AddComponent<MainMenuButtons>();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            string debug=ProjectSetup.Root+"/Scenes/DashboardDebug.unity";
            if(!System.IO.File.Exists(debug))
            {
                var debugScene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                new GameObject("Manual dashboard test",typeof(AdafruitTestSender));
                new GameObject("Debug camera",typeof(Camera));EditorSceneManager.SaveScene(debugScene,debug);
            }
            EditorSceneManager.OpenScene(main,OpenSceneMode.Single);AssetDatabase.SaveAssets();
        }
    }
}
