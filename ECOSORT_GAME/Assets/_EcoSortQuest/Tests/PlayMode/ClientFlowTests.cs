using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EcoSortQuest.Tests
{
    public sealed class ClientFlowTests
    {
        const string SaveKey = "EcoSortQuest.LocalProgress.v1";
        string originalSave, testId;
        bool hadSave;
        object originalSessionMode, originalSessionUrl;
        bool hadConnectionMode, hadConnectionUrl;
        int originalConnectionMode;
        string originalConnectionUrl;
        static readonly BindingFlags StaticFields = BindingFlags.Static | BindingFlags.NonPublic;
        Mouse mouse;
        readonly System.Collections.Generic.List<ApiSettings> testSettings = new System.Collections.Generic.List<ApiSettings>();
        void ConfigureMock(Scene scene, LoadSceneMode mode)
        {
            var app = UnityEngine.Object.FindFirstObjectByType<EcoSortApp>();
            if (app == null) return;
            var settings = ScriptableObject.CreateInstance<ApiSettings>(); settings.mode = ApiMode.Mock;
            settings.mockLatency = .05f; app.apiSettings = settings; testSettings.Add(settings);
        }
        InputSettings.BackgroundBehavior oldBackground;
        #if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode oldEditorBehavior;
        #endif
        static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        static T Field<T>(EcoSortApp app, string name) => (T)typeof(EcoSortApp).GetField(name, Fields).GetValue(app);
        static void Call(EcoSortApp app, string name) => typeof(EcoSortApp).GetMethod(name, Fields).Invoke(app, null);
        EcoSortApp App => UnityEngine.Object.FindFirstObjectByType<EcoSortApp>();
        [SetUp] public void Setup()
        {
            hadSave = PlayerPrefs.HasKey(SaveKey); originalSave = PlayerPrefs.GetString(SaveKey);
            originalSessionMode=typeof(EcoSortApp).GetField("SessionMode",StaticFields).GetValue(null);
            originalSessionUrl=typeof(EcoSortApp).GetField("SessionUrl",StaticFields).GetValue(null);
            typeof(EcoSortApp).GetField("SessionMode",StaticFields).SetValue(null,null);
            typeof(EcoSortApp).GetField("SessionUrl",StaticFields).SetValue(null,null);
            hadConnectionMode=PlayerPrefs.HasKey("EcoSortQuest.Connection.mode");
            hadConnectionUrl=PlayerPrefs.HasKey("EcoSortQuest.Connection.url");
            originalConnectionMode=PlayerPrefs.GetInt("EcoSortQuest.Connection.mode");
            originalConnectionUrl=PlayerPrefs.GetString("EcoSortQuest.Connection.url");
            testId = "test-" + Guid.NewGuid().ToString("N");
            LocalProgress.Save(new PlayerProgress { player_id = testId });
            EcoSortApp.SelectedLevel = 1; EcoSortApp.PendingPrediction = null;
            oldBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            #if UNITY_EDITOR
            oldEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            #endif
            mouse = InputSystem.AddDevice<Mouse>();
            SceneManager.sceneLoaded += ConfigureMock;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("SampleScene");
            SceneManager.sceneLoaded -= ConfigureMock;
            typeof(EcoSortApp).GetField("SessionMode",StaticFields).SetValue(null,originalSessionMode);
            typeof(EcoSortApp).GetField("SessionUrl",StaticFields).SetValue(null,originalSessionUrl);
            if(hadConnectionMode) PlayerPrefs.SetInt("EcoSortQuest.Connection.mode",originalConnectionMode);
            else PlayerPrefs.DeleteKey("EcoSortQuest.Connection.mode");
            if(hadConnectionUrl) PlayerPrefs.SetString("EcoSortQuest.Connection.url",originalConnectionUrl);
            else PlayerPrefs.DeleteKey("EcoSortQuest.Connection.url");
            foreach(var settings in testSettings) UnityEngine.Object.Destroy(settings);
            testSettings.Clear();
            if (hadSave) PlayerPrefs.SetString(SaveKey, originalSave); else PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey("EcoSort.MockCloud." + testId); PlayerPrefs.Save();
            EcoSortApp.SelectedLevel = 1; EcoSortApp.PendingPrediction = null;
            InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = oldBackground;
            #if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorBehavior;
            #endif
        }
        IEnumerator Open(string scene)
        { yield return SceneManager.LoadSceneAsync(scene); yield return null; yield return null; Assert.IsNotNull(App); }
        IEnumerator Click(Vector2 point)
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return null;
        }
        IEnumerator Sort(bool correct)
        {
            var item = Field<WasteItem>(App, "currentItem"); Assert.IsNotNull(item);
            var origin = item.transform.position;
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            var collider = item.GetComponentsInChildren<Collider>().First(c => c.enabled);
            var start = (Vector2)camera.WorldToScreenPoint(collider.bounds.center);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = start }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            Assert.IsTrue(item.selectionMarker.activeSelf, "Pointer selection must reach the item collider. Mouse=" + mouse.position.ReadValue() +
                "; expected=" + camera.WorldToScreenPoint(collider.bounds.center) + "; screen=" + Screen.width + "x" + Screen.height);
            var bin = UnityEngine.Object.FindObjectsByType<WasteBin>(FindObjectsSortMode.None)
                .First(b => correct ? b.type == item.type : b.type != item.type);
            var binCollider = bin.GetComponentsInChildren<Collider>().First(c => c.enabled);
            var end = (Vector2)camera.WorldToScreenPoint(binCollider.bounds.center);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = end }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            Assert.Greater(Vector3.Distance(item.transform.position, origin), .01f, "Held pointer must move the item.");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = end });
            yield return null;
            yield return new WaitForSeconds(1.65f);
        }
        static Button ButtonNamed(string name) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .First(button => button.GetComponentInChildren<Text>().text == name);

        [UnityTest] public IEnumerator ClickAndDropOutsideDoNotScore()
        {
            yield return Open("GameScene");
            var item = Field<WasteItem>(App, "currentItem");
            var origin = item.transform.position;
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            var start = (Vector2)camera.WorldToScreenPoint(item.GetComponentsInChildren<Collider>().First(c => c.enabled).bounds.center);
            yield return Click(start);
            Assert.AreEqual(0, Field<SortingRound>(App, "round").Answered);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = start }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(1, 1) });
            yield return null; yield return null;
            Assert.Less(Vector3.Distance(origin, item.transform.position), .15f, "Drop outside returns to the belt; belt can advance by a frame.");
            Assert.AreEqual(0, Field<SortingRound>(App, "round").Answered);
            Assert.IsFalse(item.selectionMarker.activeSelf);
        }

        [UnityTest] public IEnumerator ConveyorCarriesWasteAndPauseStopsBoth()
        {
            yield return Open("GameScene");
            var item=Field<WasteItem>(App,"currentItem");
            var seam=App.authoredWorld.GetComponentsInChildren<Transform>().First(t=>t.name=="Belt seam");
            var origin=item.transform.localPosition; var seamOrigin=seam.localPosition;
            yield return new WaitForSeconds(.25f);
            Assert.Greater(item.transform.localPosition.x,origin.x+.05f);
            Assert.AreNotEqual(seamOrigin,seam.localPosition);
            Call(App,"TogglePause"); origin=item.transform.localPosition; seamOrigin=seam.localPosition;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(origin,item.transform.localPosition); Assert.AreEqual(seamOrigin,seam.localPosition);
            Call(App,"TogglePause");
        }

        [UnityTest] public IEnumerator ConnectionMenuValidatesUrlAndKeepsChoiceAcrossScenes()
        {
            yield return Open("MainMenu"); Call(App,"ShowConnection");
            var input=UnityEngine.Object.FindObjectsByType<InputField>(FindObjectsSortMode.None).Single();
            input.text="not-a-url"; ButtonNamed("Kết nối Online").onClick.Invoke();
            Assert.IsTrue(Field<IEcoSortService>(App,"service").IsMock);
            Assert.IsTrue(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.StartsWith("Nhập URL HTTP")));
            input.text="http://127.0.0.1:8000"; ButtonNamed("Kết nối Online").onClick.Invoke();
            yield return null; yield return null; yield return null;
            Assert.IsFalse(Field<IEcoSortService>(App,"service").IsMock);
            Assert.AreEqual("http://127.0.0.1:8000",App.apiSettings.baseUrl);
            Call(App,"ShowConnection"); ButtonNamed("Chơi ngoại tuyến").onClick.Invoke();
            yield return null; yield return null; yield return null;
            Assert.IsTrue(Field<IEcoSortService>(App,"service").IsMock);
        }

        [UnityTest] public IEnumerator FieldGuideShowsAllCategoriesAndModelsRender()
        {
            yield return Open("MainMenu");
            typeof(EcoSortApp).GetMethod("ShowCatalog",Fields).Invoke(App,new object[]{0});
            yield return Capture("07-field-guide");
            yield return Open("GameScene");
            UnityEngine.Object.Destroy(Field<WasteItem>(App,"currentItem").gameObject);
            var factory=Field<WorldFactory>(App,"world");
            foreach(WasteType type in Enum.GetValues(typeof(WasteType)))
            {
                Field<Text>(App,"itemName").text="DANH MỤC 3D / "+WasteInfo.Label(type);
                Field<Text>(App,"instruction").text="Sáu mô hình minh họa của nhóm vật liệu.";
                Field<Text>(App,"lessonText").text="Ảnh fixture kiểm thử đồ họa; lượt chơi thực tế xuất hiện từng vật phẩm trên băng chuyền.";
                var models=new System.Collections.Generic.List<GameObject>();
                for(int i=0;i<6;i++)
                {
                    var model=factory.CatalogItem(WasteCatalog.Get(type,i),new Vector3((i%3-1)*2.2f,.89f,1.1f+(i/3)*1.9f));
                    models.Add(model.gameObject);
                }
                yield return Capture("08-catalog-"+type);
                foreach(var model in models) UnityEngine.Object.Destroy(model);
                yield return null;
            }
        }

        [UnityTest] public IEnumerator CampaignScanAndSimulatedChallengeWorkEndToEnd()
        {
            yield return Open("MainMenu"); yield return Capture("01-main-menu");
            for (int level = 1; level <= 3; level++)
            {
                EcoSortApp.SelectedLevel = level; yield return Open("GameScene");
                if (level == 1) yield return Capture("02-gameplay");
                for (int i = 0; i < 10; i++) yield return Sort(true);
                var round = Field<SortingRound>(App, "round");
                Assert.IsTrue(round.Passed); Assert.AreEqual(100, round.Score);
                yield return new WaitForSecondsRealtime(1);
            }
            Assert.IsTrue(LocalProgress.Load().ChallengeUnlocked);
            yield return Capture("03-level-complete");
            yield return Open("ScanScene");
            ButtonNamed("Chạy dự đoán MOCK").onClick.Invoke();
            yield return new WaitForSecondsRealtime(.6f);
            Assert.IsNotNull(Field<Prediction>(App, "scanResult")); yield return Capture("04-mock-scan");
            ButtonNamed("Mở thử thách AR mô phỏng →").onClick.Invoke();
            yield return null; yield return null; yield return null;
            Assert.AreEqual("ARChallengeScene", SceneManager.GetActiveScene().name);
            ButtonNamed("Đặt ở giữa").onClick.Invoke(); yield return null;
            Assert.IsTrue(Field<bool>(App, "arPlaced")); yield return Capture("05-ar-simulation");
            yield return Sort(false);
            Assert.IsFalse(Field<bool>(App, "arComplete")); Assert.AreEqual(0, LocalProgress.Load().ar_completed);
            yield return Sort(true);
            Assert.IsTrue(Field<bool>(App, "arComplete")); Assert.IsTrue(LocalProgress.Load().HasBadge);
            yield return new WaitForSecondsRealtime(1);
            yield return Open("ProgressScene"); yield return Capture("06-progress");
            ButtonNamed("Tải từ mock").onClick.Invoke(); yield return new WaitForSecondsRealtime(.6f);
            Assert.IsTrue(LocalProgress.Load().HasBadge);
            PlayerProgress remote = null;
            yield return new MockEcoSortService(0).GetProgress(testId, p => remote = p, e => Assert.Fail(e));
            Assert.IsNotNull(remote); Assert.IsTrue(remote.campaign_completed); Assert.AreEqual(1, remote.ar_completed);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ImageFileLoadsAndInvalidPathDoesNotCrash()
        {
            string path = Path.Combine(Application.temporaryCachePath, testId + ".png");
            var image = new Texture2D(32, 16, TextureFormat.RGB24, false);
            var pixels = Enumerable.Repeat(new Color(.2f, .6f, .4f), 32 * 16).ToArray();
            image.SetPixels(pixels); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);
            try
            {
                yield return Open("ScanScene");
                var input = Field<InputField>(App, "imagePath"); input.text = path;
                Call(App, "LoadImage"); yield return null;
                Assert.Greater(Field<byte[]>(App, "jpeg").Length, 0);
                Assert.AreEqual(32, Field<Texture2D>(App, "imageTexture").width);
                Assert.IsNull(Field<Prediction>(App, "scanResult"));
                input.text = "not-a-valid-absolute-path.txt"; Call(App, "LoadImage");
                Assert.IsTrue(Field<Text>(App, "status").text.Contains("đường dẫn tuyệt đối"));
                LogAssert.NoUnexpectedReceived();
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }
        [UnityTest] public IEnumerator PauseStopsTimerAndExpiryEndsRound()
        {
            var data = LocalProgress.Load(); data.unlocked_level = 2; LocalProgress.Save(data);
            EcoSortApp.SelectedLevel = 2; yield return Open("GameScene");
            Call(App, "TogglePause"); float before = Field<float>(App, "remaining");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(before, Field<float>(App, "remaining"));
            Call(App, "TogglePause");
            typeof(EcoSortApp).GetField("remaining", Fields).SetValue(App, .01f);
            yield return new WaitForSeconds(.1f);
            Assert.IsTrue(Field<SortingRound>(App, "round").Finished);
            Assert.IsFalse(Field<SortingRound>(App, "round").Passed);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RestWithoutUrlReportsErrorAndKeepsLocalSave()
        {
            var settings = ScriptableObject.CreateInstance<ApiSettings>(); settings.mode = ApiMode.Rest;
            var service = new RestEcoSortService(settings); string error = null;
            yield return service.Health(_ => Assert.Fail("Unconfigured REST must not succeed"), e => error = e);
            Assert.IsNotEmpty(error); Assert.AreEqual(testId, LocalProgress.Load().player_id);
            error = null;
            yield return service.Predict(null, WasteType.Plastic, _ => Assert.Fail("Missing image must not succeed"), e => error = e);
            Assert.IsNotEmpty(error); UnityEngine.Object.Destroy(settings);
        }
        static IEnumerator Capture(string name)
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Evidence"));
            Directory.CreateDirectory(dir);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            var target = new RenderTexture(1600, 900, 24);
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var oldMode = canvas.renderMode; float oldAspect = camera.aspect;
            camera.targetTexture = target; camera.aspect = 1600f / 900;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases(); yield return null;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); texture.Apply();
                File.WriteAllBytes(Path.Combine(dir, name + ".png"), texture.EncodeToPNG());
                UnityEngine.Object.Destroy(texture);
            }
            finally
            {
                camera.targetTexture = oldTarget; camera.aspect = oldAspect;
                RenderTexture.active = oldActive; canvas.renderMode = oldMode;
                target.Release(); UnityEngine.Object.Destroy(target); Canvas.ForceUpdateCanvases();
            }
            yield return null;
        }
    }
}
