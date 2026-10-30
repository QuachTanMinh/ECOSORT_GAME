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
        ApiSettings nextSceneSettings;
        void ConfigureMock(Scene scene, LoadSceneMode mode)
        {
            var app = UnityEngine.Object.FindFirstObjectByType<EcoSortApp>();
            if (app == null) return;
            if (nextSceneSettings != null) { app.apiSettings = nextSceneSettings; return; }
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
            EcoSortApp.SelectedLevel = 1; EcoSortApp.PendingPrediction = null; EcoSortApp.SelectedAreaMission="";
            EcoSortApp.SelectedLearningMode = LearningMode.Campaign; EcoSortApp.SelectedContext = LearningContext.Home; EcoSortApp.AiMission = false;
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
            SceneManager.sceneLoaded -= ConfigureMock;
            typeof(EcoSortApp).GetField("SessionMode",StaticFields).SetValue(null,originalSessionMode);
            typeof(EcoSortApp).GetField("SessionUrl",StaticFields).SetValue(null,originalSessionUrl);
            if(hadConnectionMode) PlayerPrefs.SetInt("EcoSortQuest.Connection.mode",originalConnectionMode);
            else PlayerPrefs.DeleteKey("EcoSortQuest.Connection.mode");
            if(hadConnectionUrl) PlayerPrefs.SetString("EcoSortQuest.Connection.url",originalConnectionUrl);
            else PlayerPrefs.DeleteKey("EcoSortQuest.Connection.url");
            foreach(var settings in testSettings) UnityEngine.Object.Destroy(settings);
            testSettings.Clear();
            nextSceneSettings = null;
            if (hadSave) PlayerPrefs.SetString(SaveKey, originalSave); else PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(LearningStore.Key(testId));
            PlayerPrefs.DeleteKey(AreaJourneyStore.Key(testId));
            foreach(LearningContext context in System.Enum.GetValues(typeof(LearningContext)))
                PlayerPrefs.DeleteKey(RestorationSave.Key(testId,context));
            PlayerPrefs.DeleteKey("EcoSort.MockCloud." + testId); PlayerPrefs.Save();
            EcoSortApp.SelectedLevel = 1; EcoSortApp.PendingPrediction = null; EcoSortApp.SelectedAreaMission="";
            EcoSortApp.SelectedLearningMode = LearningMode.Campaign; EcoSortApp.SelectedContext = LearningContext.Home; EcoSortApp.AiMission = false;
            InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = oldBackground;
            #if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorBehavior;
            #endif
            // Restore preferences before any scene operation can throw, including skipped tests.
            var previousScene=SceneManager.GetActiveScene();
            var cleanupScene=SceneManager.CreateScene("EcoSort test cleanup "+System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(cleanupScene);
            if(previousScene.IsValid() && previousScene.isLoaded)yield return SceneManager.UnloadSceneAsync(previousScene);
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

        [UnityTest] public IEnumerator SharedPaletteAndDropCueDoNotRevealTheAnswer()
        {
            yield return Open("GameScene");
            var factory=Field<WorldFactory>(App,"world");
            Assert.AreSame(factory.Material(Color.cyan),factory.Material(Color.cyan));
            Assert.IsTrue(factory.Material(Color.cyan).enableInstancing);
            var bins=UnityEngine.Object.FindObjectsByType<WasteBin>(FindObjectsSortMode.None);
            foreach(var bin in bins) { bin.Highlight(true);Assert.IsTrue(bin.transform.Find("Drop target outline").gameObject.activeSelf);bin.Highlight(false); }
            var item=Field<WasteItem>(App,"currentItem");
            var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
            var start=(Vector2)camera.WorldToScreenPoint(item.GetComponentsInChildren<Collider>().First(c=>c.enabled).bounds.center);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=start}.WithButton(MouseButton.Left));yield return null;yield return null;
            var wrong=bins.First(b=>b.type!=item.type);
            var end=(Vector2)camera.WorldToScreenPoint(wrong.GetComponentsInChildren<Collider>().First(c=>c.enabled).bounds.center);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=end}.WithButton(MouseButton.Left));yield return null;yield return null;
            Assert.AreSame(wrong,Field<WasteBin>(App,"hoveredBin"));
            Assert.IsTrue(wrong.transform.Find("Drop target outline").gameObject.activeSelf);
            Assert.AreEqual(0,Field<SortingRound>(App,"round").Answered);
            // Capture renders at a fixed aspect; freeze input so the old screen pointer
            // cannot move the held object while that temporary camera aspect is active.
            App.GetComponent<PointerInput>().enabled=false;
            yield return Capture("gameplay-drop-cue-20261028");
            App.GetComponent<PointerInput>().enabled=true;
            Call(App,"TogglePause");Assert.IsFalse(wrong.transform.Find("Drop target outline").gameObject.activeSelf);
        }
        [UnityTest] public IEnumerator MenuSeparatesActivitiesAndUsesPlainLanguage()
        {
            yield return Open("MainMenu");
            var labels=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Select(b=>b.GetComponentInChildren<Text>().text).ToArray();
            Assert.AreEqual(1,labels.Count(t=>t=="Phân loại bằng ảnh"));
            Assert.IsFalse(labels.Any(t=>t.Contains(" / AI")||t.Contains("Thống kê")||t=="Tiến trình"));
            foreach(var label in new[]{"Học có hướng dẫn","Kiểm tra","Ôn câu sai","Thử thách phân loại","Kết quả học tập"})Assert.Contains(label,labels);
            yield return Capture("menu-separated-functions",1920,1000);
            ButtonNamed("Kiểm tra").onClick.Invoke();yield return null;
            Assert.IsFalse(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.transform.IsChildOf(GameObject.Find("Practice context").transform) && b.GetComponentInChildren<Text>().text.Contains("ảnh")));
            ButtonNamed("Công viên").onClick.Invoke();yield return null;yield return Capture("menu-test-context");
            ButtonNamed("Bắt đầu").onClick.Invoke();yield return null;yield return null;yield return null;
            Assert.AreEqual(LearningMode.Test,EcoSortApp.SelectedLearningMode);Assert.AreEqual(LearningContext.Park,EcoSortApp.SelectedContext);
            yield return Open("MainMenu");ButtonNamed("Thử thách phân loại").onClick.Invoke();yield return null;
            Assert.IsTrue(ButtonNamed("Cấp độ 1").interactable);Assert.IsFalse(ButtonNamed("Cấp độ 2 • Chưa mở").interactable);
            ButtonNamed("Đóng").onClick.Invoke();yield return null;
            ButtonNamed("Kết quả học tập").onClick.Invoke();yield return null;ButtonNamed("Thành tích").onClick.Invoke();yield return null;
            Assert.AreEqual(1,GameObject.Find("Learning statistics").GetComponentsInChildren<Button>().Count(b=>b.GetComponentInChildren<Text>().text.StartsWith("Lưu")));
            yield return Capture("menu-achievements");
            ButtonNamed("Đóng").onClick.Invoke();yield return null;
            Call(App,"ShowExperienceSettings");ButtonNamed("Kết nối").onClick.Invoke();yield return null;ButtonNamed("Thiết lập kết nối").onClick.Invoke();yield return null;yield return Capture("menu-connection");
            ButtonNamed("Đóng").onClick.Invoke();yield return null;
            ButtonNamed("Phân loại bằng ảnh").onClick.Invoke();yield return null;yield return null;yield return null;
            var texts=UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Select(t=>t.text).ToArray();
            foreach(var text in texts)Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(text,@"(?i)\b(mock|api|backend|fastapi|json|confidence)\b"),text);
            ButtonNamed("Chưa biết / cần kiểm tra").onClick.Invoke();ButtonNamed("Xem kết quả ví dụ").onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);
            StringAssert.Contains("KHÔNG PHÂN TÍCH ẢNH",Field<Text>(App,"predictionText").text);
            yield return Capture("menu-photo-example");
        }
        [UnityTest] public IEnumerator CompactUiFitsChangingTextAndLeavesTheSceneVisible()
        {
            yield return Open("MainMenu");yield return Capture("ui-main-menu-wide",1920,1000);
            yield return Open("GameScene");yield return null;
            var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>().GetComponent<RectTransform>();
            var card=GameObject.Find("Item hint card").GetComponent<RectTransform>();
            Assert.LessOrEqual(card.rect.width,canvas.rect.width*.44f);
            float originalHeight=card.rect.height;
            string originalLesson=Field<Text>(App,"lessonText").text;
            Field<Text>(App,"lessonText").text="Tách bìa nhựa và gáy kim loại nếu có. Giữ phần giấy sạch, khô; đọc hướng dẫn nơi thu gom trước khi phân loại. Mỗi phần vật liệu cần được xử lý phù hợp.";
            yield return null;yield return null;
            Assert.Greater(card.rect.height,originalHeight,"Long feedback must grow the card instead of shrinking the font.");
            foreach(var text in card.GetComponentsInChildren<Text>())
                Assert.GreaterOrEqual(text.rectTransform.rect.height+2,text.preferredHeight,"Feedback must fit its text box.");
            yield return Capture("ui-gameplay-long-feedback-720p",1280,720);
            Field<Text>(App,"lessonText").text=originalLesson;yield return null;yield return null;
            Call(App,"TogglePause");yield return null;yield return Capture("ui-pause");
            var dialog=GameObject.Find("Pause dialog").GetComponent<RectTransform>();
            Assert.Less(dialog.rect.width*dialog.rect.height,canvas.rect.width*canvas.rect.height*.12f);
            ButtonNamed("Tiếp tục / ESC").onClick.Invoke();yield return null;
            yield return Capture("ui-gameplay");
            Call(App,"ShowExperienceSettings");yield return null;yield return Capture("ui-settings");
            ButtonNamed("Đóng").onClick.Invoke();yield return null;
            EcoSortApp.SelectedLearningMode=LearningMode.Explore;
            yield return Open("GameScene");Call(App,"AcceptRestorationMission");yield return null;yield return null;
            yield return Capture("ui-exploration");
            LogAssert.NoUnexpectedReceived();
        }
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
            input.text="not-a-url"; ButtonNamed("Bật kết nối").onClick.Invoke();
            Assert.IsTrue(Field<IEcoSortService>(App,"service").IsMock);
            Assert.IsTrue(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.StartsWith("Địa chỉ chưa đúng")));
            input.text="http://127.0.0.1:8000"; ButtonNamed("Bật kết nối").onClick.Invoke();
            yield return null; yield return null; yield return null;
            Assert.IsFalse(Field<IEcoSortService>(App,"service").IsMock);
            Assert.AreEqual("http://127.0.0.1:8000",App.apiSettings.baseUrl);
            Call(App,"ShowConnection"); ButtonNamed("Chơi trên máy").onClick.Invoke();
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
            ButtonNamed(RecognitionInfo.All[0].display_name).onClick.Invoke();
            ButtonNamed("Xem kết quả ví dụ").onClick.Invoke();
            yield return new WaitForSecondsRealtime(.6f);
            Assert.IsNotNull(Field<Prediction>(App, "scanResult")); yield return Capture("04-mock-scan");
            EcoSortApp.PendingPrediction=Field<Prediction>(App,"scanResult");
            yield return Open("ARChallengeScene");
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
            ButtonNamed("Khôi phục tiến trình").onClick.Invoke(); yield return new WaitForSecondsRealtime(.6f);
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
        [UnityTest] public IEnumerator LearningTestHidesAnswersSavesHistoryAndDoesNotUnlockCampaign()
        {
            EcoSortApp.SelectedLearningMode=LearningMode.Test; EcoSortApp.SelectedContext=LearningContext.School;
            yield return Open("GameScene");
            StringAssert.Contains("Quan sát mô hình",Field<Text>(App,"itemName").text);
            StringAssert.Contains("Ẩn điểm",Field<Text>(App,"scoreText").text);
            yield return Capture("learning-test-school");
            yield return Sort(false);
            Assert.AreEqual(1,Field<SortingRound>(App,"round").Answered);
            StringAssert.Contains("Ẩn điểm",Field<Text>(App,"scoreText").text);
            typeof(EcoSortApp).GetField("remaining",Fields).SetValue(App,.01f);
            yield return new WaitForSeconds(.15f);
            var history=LearningStore.Load(testId);
            Assert.AreEqual("Test",history.sessions.Single().mode);
            Assert.AreEqual(1,history.sessions[0].answers.Length);
            Assert.IsTrue(history.sessions[0].expired);
            Assert.AreEqual(1,LocalProgress.Load().unlocked_level);
            Assert.AreEqual(1,LearningStore.Mistakes(history).Count);
            typeof(EcoSortApp).GetMethod("ShowAnswerReview",Fields).Invoke(App,new object[]{0});
            yield return Capture("learning-answer-review");
            EcoSortApp.SelectedLearningMode=LearningMode.Review;
            yield return Open("GameScene");
            Assert.IsTrue(Field<WasteDefinition[]>(App,"roundItems").All(d=>d.Id==history.sessions[0].answers[0].item_id));
            StringAssert.Contains("Gợi ý",Field<Text>(App,"lessonText").text);
        }
        [UnityTest] public IEnumerator LearningHubThemesAndStatisticsAreReachable()
        {
            yield return Open("MainMenu");ButtonNamed("Học có hướng dẫn").onClick.Invoke();
            ButtonNamed("Công viên").onClick.Invoke();yield return null;
            yield return Capture("learning-hub");
            ButtonNamed("Đóng").onClick.Invoke();yield return null;
            ButtonNamed("Kết quả học tập").onClick.Invoke();yield return null;
            yield return Capture("learning-statistics");
            foreach(var context in new[]{LearningContext.Home,LearningContext.School,LearningContext.Park})
            {
                EcoSortApp.SelectedContext=context;EcoSortApp.SelectedLearningMode=LearningMode.Learn;
                yield return Open("GameScene");yield return Capture("learning-context-"+context);
                Assert.AreEqual(0,Field<float>(App,"sessionLimit"));
            }
        }
        [UnityTest] public IEnumerator LabRequiresOwnChoiceAndConfirmsKnowledgeIndependentlyOfAi()
        {
            yield return Open("ScanScene");
            ButtonNamed("Xem kết quả ví dụ").onClick.Invoke();
            Assert.IsNull(Field<Prediction>(App,"scanResult"));
            ButtonNamed("Chưa biết / cần kiểm tra").onClick.Invoke();
            ButtonNamed("Xem kết quả ví dụ").onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(LearningStore.Load(testId).missions.Single().mock);
            Assert.IsFalse(LearningStore.Load(testId).missions.Single().agrees);
            Assert.IsTrue(Field<Button>(App,"challengeButton").interactable);
            yield return Capture("lab-prediction-20261028");
            ButtonNamed("Xác nhận kiến thức →").onClick.Invoke();yield return null;
            ButtonNamed("Luôn tin AI vì độ tin cậy cao").onClick.Invoke();
            Assert.IsFalse(LearningStore.Load(testId).missions.Single().knowledge_confirmed);
            ButtonNamed("Tự kiểm tra vật liệu và quy định nơi thu gom").onClick.Invoke();yield return null;
            ButtonNamed(WasteInfo.Label(WasteType.Metal)).onClick.Invoke();
            Assert.IsFalse(LearningStore.Load(testId).missions.Single().knowledge_confirmed);
            ButtonNamed(WasteInfo.Label(WasteType.Plastic)).onClick.Invoke();
            Assert.IsTrue(LearningStore.Load(testId).missions.Single().knowledge_confirmed);
            Assert.AreEqual(0,LearningStore.Load(testId).sessions.Count);
            yield return Capture("lab-confirmed-20261028");
            ButtonNamed("Thí nghiệm mới →").onClick.Invoke();yield return null;
            Assert.IsNull(Field<byte[]>(App,"jpeg"));Assert.IsNull(Field<Prediction>(App,"scanResult"));
            Assert.IsNull(Field<string>(App,"labChoice"));
        }
        [UnityTest] public IEnumerator StatisticsPagesAndRecommendationUseRecordedAnswers()
        {
            var h=new LearningHistory();
            for(int i=0;i<2;i++)h.sessions.Add(new LearningSession { mode="Test",context="Home",total=10, answers=new[]{
                new LearningAnswer{item_id="water-bottle",expected="Plastic",choice="Metal",correct=false},
                new LearningAnswer{item_id="drink-can",expected="Metal",choice="Metal",correct=true},
                new LearningAnswer{item_id="paper-bag",expected="Paper",choice="Paper",correct=true},
                new LearningAnswer{item_id="banana-peel",expected="Organic",choice="Organic",correct=true} } });
            LearningStore.Save(testId,h);
            yield return Open("MainMenu");Call(App,"ShowLearningStats");yield return null;
            yield return Capture("learning-insights-20261028");
            ButtonNamed("Kỹ năng vật liệu").onClick.Invoke();yield return null;
            yield return Capture("learning-skills-20261028");
            ButtonNamed("Danh sách ôn").onClick.Invoke();yield return null;
            yield return Capture("learning-review-20261028");
            ButtonNamed("Bắt đầu gợi ý →").onClick.Invoke();yield return null;yield return null;yield return null;
            Assert.AreEqual(LearningMode.Review,EcoSortApp.SelectedLearningMode);
            Assert.IsTrue(Field<WasteDefinition[]>(App,"roundItems").All(d=>d.Id=="water-bottle"));
        }
        [UnityTest] public IEnumerator UncertainAiStillAllowsVerifiedKnowledgeCompletion()
        {
            yield return Open("ScanScene");
            ButtonNamed("Chưa biết / cần kiểm tra").onClick.Invoke();
            typeof(EcoSortApp).GetMethod("ApplyPrediction",Fields).Invoke(App,new object[]{new Prediction{class_name="Paper",confidence=.4f,accepted=false}});
            Assert.IsNull(Field<Prediction>(App,"scanResult"));
            Assert.IsFalse(LearningStore.Load(testId).missions.Single().accepted);
            ButtonNamed("Xác nhận kiến thức →").onClick.Invoke();yield return null;
            ButtonNamed("Tự kiểm tra vật liệu và quy định nơi thu gom").onClick.Invoke();yield return null;
            ButtonNamed(WasteInfo.Label(WasteType.Plastic)).onClick.Invoke();
            Assert.IsTrue(LearningStore.Load(testId).missions.Single().knowledge_confirmed);
            yield return Capture("lab-uncertain-confirmed-20261028");
            ButtonNamed("Luyện 3D mẫu").onClick.Invoke();yield return null;yield return null;yield return null;
            Assert.AreEqual("ARChallengeScene",SceneManager.GetActiveScene().name);
            Assert.AreEqual("Plastic",EcoSortApp.PendingPrediction.class_name,"3D uses the verified lesson item, not AI's uncertain Paper label.");
        }
        [UnityTest] public IEnumerator ExplorationRequiresProximitySupportsCorrectionAndSavesCompleteRound()
        {
            yield return Open("MainMenu");yield return Capture("restoration-main-menu");
            EcoSortApp.SelectedContext=LearningContext.Park;EcoSortApp.SelectedLearningMode=LearningMode.Explore;yield return Open("GameScene");
            Assert.AreEqual(LearningMode.Explore,EcoSortApp.SelectedLearningMode);
            Assert.AreEqual(LearningContext.Park,EcoSortApp.SelectedContext);
            Assert.IsNotNull(Field<RectTransform>(App,"missionBrief"));
            yield return Capture("restoration-briefing");
            Call(App,"AcceptRestorationMission");yield return null;
            var actor=Field<ExplorerController>(App,"explorer");
            Assert.IsFalse(Camera.main.orthographic);
            var litter=Field<System.Collections.Generic.List<WasteItem>>(App,"scattered");
            Assert.AreEqual(12,litter.Count);
            Assert.IsTrue(litter.All(i=>i.Definition.AppearsIn(LearningContext.Park)));
            var bins=UnityEngine.Object.FindObjectsByType<WasteBin>(FindObjectsSortMode.None);
            Assert.AreEqual(4,bins.Length);
            yield return Capture("exploration-park-start");
            // Keyboard movement passes through the actual controller rather than moving the transform.
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                var start=actor.transform.position;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return new WaitForSeconds(.3f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                Assert.Greater(Vector3.Distance(start,actor.transform.position),.2f);
                Call(App,"TogglePause");start=actor.transform.position;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return new WaitForSecondsRealtime(.15f);
                Assert.Less(Vector3.Distance(start,actor.transform.position),.01f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());Call(App,"TogglePause");yield return null;
            }
            finally{InputSystem.RemoveDevice(keyboard);}
            void MoveTo(Vector3 position)
            {var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=position;cc.enabled=true;Physics.SyncTransforms();}
            MoveTo(new Vector3(10,.1f,-7));Call(App,"InteractExploration");Assert.IsNull(Field<WasteItem>(App,"carried"));
            var first=litter.First();MoveTo(first.transform.position+Vector3.back*.6f);Call(App,"InteractExploration");
            Assert.AreSame(first,Field<WasteItem>(App,"carried"));
            Call(App,"InteractExploration");Assert.AreEqual(0,Field<SortingRound>(App,"round").Answered);
            var wrong=bins.First(b=>b.type!=first.type);MoveTo(wrong.transform.position+Vector3.back*1.5f);Call(App,"InteractExploration");
            Assert.AreEqual(0,Field<SortingRound>(App,"round").Answered,"Unprepared containers cannot be scored.");
            Assert.IsNotNull(Field<RectTransform>(App,"preparationPanel"));
            typeof(EcoSortApp).GetMethod("ChoosePreparation",Fields).Invoke(App,new object[]{PreparationAction.Ready});
            Assert.AreEqual(1,Field<RestorationMission>(App,"restoration").PreparationMistakes);
            yield return Capture("restoration-preparation");
            typeof(EcoSortApp).GetMethod("ChoosePreparation",Fields).Invoke(App,new object[]{PreparationAction.EmptyContents});yield return null;
            Call(App,"InteractExploration");
            Assert.AreSame(first,Field<WasteItem>(App,"carried"));Assert.AreEqual(1,Field<SortingRound>(App,"round").Answered);
            Call(App,"PutDownExploration");Assert.IsNull(Field<WasteItem>(App,"carried"));Call(App,"InteractExploration");
            var right=bins.Single(b=>b.type==first.type);MoveTo(right.transform.position+Vector3.back*1.5f);Call(App,"InteractExploration");
            Assert.AreEqual(1,Field<int>(App,"cleaned"));Assert.AreEqual(1,Field<SortingRound>(App,"round").Answered);
            yield return Capture("exploration-corrected-drop");
            foreach(var item in litter.ToArray())
            {
                MoveTo(item.transform.position+Vector3.back*.6f);Call(App,"InteractExploration");
                Assert.AreSame(item,Field<WasteItem>(App,"carried"));
                if(item==Field<WasteItem>(App,"wrappedCup"))
                {
                    int beforeSeparation=Field<int>(App,"cleaned");
                    Assert.IsFalse(Field<WasteItem>(App,"paperSleeve").gameObject.activeSelf);
                    Call(App,"ShowPreparation");
                    typeof(EcoSortApp).GetMethod("ChoosePreparation",Fields).Invoke(App,new object[]{PreparationAction.SeparateSleeve});
                    Assert.IsTrue(Field<WasteItem>(App,"paperSleeve").gameObject.activeSelf);
                    Assert.AreEqual(beforeSeparation,Field<int>(App,"cleaned"),"Separation cannot count as sorted waste.");
                    yield return Capture("restoration-separated-materials");
                    typeof(EcoSortApp).GetMethod("ChoosePreparation",Fields).Invoke(App,new object[]{PreparationAction.EmptyContents});
                    yield return null;
                }
                else if(PreparationRules.Required(item.Definition.Id)!=PreparationAction.Ready)
                {
                    Call(App,"ShowPreparation");
                    typeof(EcoSortApp).GetMethod("ChoosePreparation",Fields).Invoke(App,new object[]{PreparationRules.Required(item.Definition.Id)});
                    yield return null;
                }
                var bin=bins.Single(b=>b.type==item.type);MoveTo(bin.transform.position+Vector3.back*1.5f);Call(App,"InteractExploration");yield return null;
            }
            yield return new WaitForSeconds(.2f);
            var session=LearningStore.Load(testId).sessions.Single();
            Assert.AreEqual("Explore",session.mode);Assert.AreEqual(12,session.answers.Length);Assert.AreEqual(110,session.score);
            Assert.AreEqual(11,session.answers.Count(a=>a.correct));Assert.IsFalse(session.expired);
            Assert.IsTrue(Field<RestorationMission>(App,"restoration").Complete);
            Assert.IsTrue(Field<GameObject[]>(App,"restorationStages").All(s=>s.activeSelf));
            var reward=RestorationSave.Load(testId,LearningContext.Park);
            Assert.AreEqual(2,reward.bestStars);Assert.AreEqual(1,reward.completions);
            ButtonNamed("Ngắm thành quả").onClick.Invoke();yield return null;
            Assert.IsTrue(actor.CanMove);
            yield return Capture("restoration-finished-world");
            ButtonNamed("Xem kết quả").onClick.Invoke();yield return null;
            yield return Capture("exploration-result");
            PlayerPrefs.DeleteKey(RestorationSave.Key(testId,LearningContext.Park));PlayerPrefs.Save();
        }
        [Serializable] sealed class LearningApiCount { public int total_sessions; public int test_unanswered; }
        [UnityTest] public IEnumerator LearningRestRoundTripDoesNotDuplicate()
        {
            string url=Environment.GetEnvironmentVariable("ECOSORT_LEARNING_TEST_URL");
            if(string.IsNullOrEmpty(url)){Assert.Ignore("Set a dedicated local backend URL for REST integration.");yield break;}
            Assert.AreEqual("127.0.0.1",new Uri(url).Host,"Integration tests must use an isolated local backend.");
            var settings=ScriptableObject.CreateInstance<ApiSettings>();settings.mode=ApiMode.Rest;settings.baseUrl=url;
            try
            {
                var rest=new RestEcoSortService(settings);
                var session=new LearningSession{player_id=testId,mode="Test",context="School",created_at=DateTime.UtcNow.ToString("O"),
                    level=1,total=10,score=10,expired=true,answers=new[]{new LearningAnswer{item_id="water-bottle",expected="Plastic",choice="Plastic",correct=true,seconds=1.5f}}};
                int successes=0;
                yield return rest.PostLearning(session,()=>successes++,e=>Assert.Fail(e));
                yield return rest.PostLearning(session,()=>successes++,e=>Assert.Fail(e));
                Assert.AreEqual(2,successes);
                using(var request=UnityEngine.Networking.UnityWebRequest.Get(url+"/learning/"+testId))
                {
                    request.timeout=10;yield return request.SendWebRequest();Assert.AreEqual(200,request.responseCode);
                    var data=JsonUtility.FromJson<LearningApiCount>(request.downloadHandler.text);
                    Assert.AreEqual(1,data.total_sessions);Assert.AreEqual(9,data.test_unanswered);
                }
            }
            finally{UnityEngine.Object.Destroy(settings);}
        }
        [UnityTest] public IEnumerator AudioAssetsAndTwoDimensionalRoutingAreReady()
        {
            float oldListenerVolume=AudioListener.volume;
            #if UNITY_EDITOR
            bool oldEditorMute=UnityEditor.EditorUtility.audioMasterMute;
            #endif
            int old=PlayerPrefs.GetInt("EcoSortQuest.Sound",1);bool had=PlayerPrefs.HasKey("EcoSortQuest.Sound");
            try
            {
                PlayerPrefs.SetInt("EcoSortQuest.Sound",1);yield return Open("GameScene");
                var sound=App.GetComponent<EcoSortAudio>();Assert.IsNotNull(sound);
                Assert.AreEqual(0,sound.Effects.spatialBlend);Assert.AreEqual(0,sound.Music.spatialBlend);
                Assert.IsNotNull(sound.Music.clip);Assert.IsTrue(sound.Music.loop);
                foreach(var name in new[]{"correct","retry","click","pickup","drop"})
                {var clip=Resources.Load<AudioClip>("Audio/"+name);Assert.IsNotNull(clip);Assert.Greater(clip.samples,100);}
                var samples=new float[sound.Music.clip.samples];sound.Music.clip.GetData(samples,0);
                Assert.IsTrue(samples.Any(x=>Mathf.Abs(x)>.01f));
                PlayerPrefs.SetInt("EcoSortQuest.Sound",0);sound.Apply();Assert.IsTrue(sound.Effects.mute);
                Call(App,"ShowExperienceSettings");ButtonNamed("Nghe thử").onClick.Invoke();
                Assert.AreEqual(1,PlayerPrefs.GetInt("EcoSortQuest.Sound"));Assert.IsFalse(sound.Effects.mute);
                yield return Capture("audio-settings");
            }
            finally{
                AudioListener.volume=oldListenerVolume;
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.audioMasterMute=oldEditorMute;
                #endif
                if(had)PlayerPrefs.SetInt("EcoSortQuest.Sound",old);else PlayerPrefs.DeleteKey("EcoSortQuest.Sound");PlayerPrefs.Save();}
        }
        [UnityTest] public IEnumerator AudioVolumeControlsPersistAndMixChannels()
        {
            string[] keys={"EcoSortQuest.Volume.Master","EcoSortQuest.Volume.Music","EcoSortQuest.Volume.Effects"};
            bool[] had=keys.Select(PlayerPrefs.HasKey).ToArray();float[] old=keys.Select(k=>PlayerPrefs.GetFloat(k)).ToArray();
            try
            {
                yield return Open("GameScene");Call(App,"ShowExperienceSettings");
                var sliders=UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsSortMode.None);
                Assert.AreEqual(3,sliders.Length);
                sliders.Single(x=>x.name=="Âm lượng tổng slider").value=.5f;
                sliders.Single(x=>x.name=="Nhạc nền slider").value=.4f;
                sliders.Single(x=>x.name=="Hiệu ứng slider").value=.8f;
                var audio=App.GetComponent<EcoSortAudio>();
                Assert.AreEqual(.2f,audio.Music.volume,.001f);Assert.AreEqual(.4f,audio.Effects.volume,.001f);
                yield return Capture("audio-volume-controls");
                yield return Open("GameScene");audio=App.GetComponent<EcoSortAudio>();
                Assert.AreEqual(.2f,audio.Music.volume,.001f);Assert.AreEqual(.4f,audio.Effects.volume,.001f);
                audio.SetVolume("Master",2);Assert.AreEqual(1,EcoSortAudio.Volume("Master",0));
            }
            finally{for(int i=0;i<keys.Length;i++){if(had[i])PlayerPrefs.SetFloat(keys[i],old[i]);else PlayerPrefs.DeleteKey(keys[i]);}PlayerPrefs.Save();}
        }
        [UnityTest] public IEnumerator SettingsGearPausesAndRestoresRoundAcrossTabs()
        {
            yield return Open("GameScene");
            var gear=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.name=="Settings gear");
            gear.onClick.Invoke();yield return null;
            Assert.AreEqual(0,Time.timeScale);Assert.IsTrue(Field<bool>(App,"paused"));
            float remaining=Field<float>(App,"remaining");yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(remaining,Field<float>(App,"remaining"));
            yield return Capture("settings-audio-tab");
            ButtonNamed("Hình ảnh").onClick.Invoke();yield return null;yield return Capture("settings-visual-tab");
            ButtonNamed("Lối chơi").onClick.Invoke();yield return null;yield return Capture("settings-gameplay-tab");
            Call(App,"TogglePause");yield return null;Assert.AreEqual(1,Time.timeScale);Assert.IsFalse(Field<bool>(App,"paused"));
            Call(App,"TogglePause");gear.onClick.Invoke();yield return null;ButtonNamed("Đóng").onClick.Invoke();
            Assert.AreEqual(0,Time.timeScale);Assert.IsTrue(Field<bool>(App,"paused"));Call(App,"TogglePause");
        }
        [UnityTest] public IEnumerator DragAndBinDropPlayDistinctInteractionAndOutcomeClips()
        {
            bool had=PlayerPrefs.HasKey("EcoSortQuest.Sound");int old=PlayerPrefs.GetInt("EcoSortQuest.Sound",1);
            try
            {
                PlayerPrefs.SetInt("EcoSortQuest.Sound",1);yield return Open("GameScene");
                var sounds=new System.Collections.Generic.List<string>();App.GetComponent<EcoSortAudio>().CuePlayed+=sounds.Add;
                yield return Sort(true);
                CollectionAssert.AreEqual(new[]{"pickup","drop","correct"},sounds);
                sounds.Clear();yield return Sort(false);
                CollectionAssert.AreEqual(new[]{"pickup","drop","retry"},sounds);
                EcoSortApp.SelectedLearningMode=LearningMode.Test;yield return Open("GameScene");
                sounds.Clear();App.GetComponent<EcoSortAudio>().CuePlayed+=sounds.Add;yield return Sort(false);
                CollectionAssert.AreEqual(new[]{"pickup","drop"},sounds,"Testing mode must not reveal correctness through audio.");
            }
            finally{if(had)PlayerPrefs.SetInt("EcoSortQuest.Sound",old);else PlayerPrefs.DeleteKey("EcoSortQuest.Sound");PlayerPrefs.Save();}
        }
        [Serializable] sealed class RecognitionHealth { public string[] class_names; public string storage_mode; }
        [UnityTest] public IEnumerator RecognitionRestUsesRealModelAndDisplaysResponse()
        {
            string url=Environment.GetEnvironmentVariable("ECOSORT_LEARNING_TEST_URL");
            string image=Environment.GetEnvironmentVariable("ECOSORT_RECOGNITION_TEST_IMAGE");
            if(string.IsNullOrEmpty(url)||string.IsNullOrEmpty(image))
            {Assert.Ignore("Dedicated local backend and image required for real AI integration.");yield break;}
            Assert.AreEqual("127.0.0.1",new Uri(url).Host);
            using(var request=UnityEngine.Networking.UnityWebRequest.Get(url+"/health"))
            {
                request.timeout=20;yield return request.SendWebRequest();Assert.AreEqual(200,request.responseCode);
                var health=JsonUtility.FromJson<RecognitionHealth>(request.downloadHandler.text);
                Assert.AreEqual("local",health.storage_mode,"Integration tests must never write to live cloud.");
                Assert.AreEqual(12,health.class_names.Length);
            }
            nextSceneSettings=ScriptableObject.CreateInstance<ApiSettings>();testSettings.Add(nextSceneSettings);
            nextSceneSettings.mode=ApiMode.Rest;nextSceneSettings.baseUrl=url;nextSceneSettings.timeoutSeconds=30;
            typeof(EcoSortApp).GetField("SessionMode",StaticFields).SetValue(null,(ApiMode?)ApiMode.Rest);
            typeof(EcoSortApp).GetField("SessionUrl",StaticFields).SetValue(null,url);
            yield return Open("ScanScene");
            var rest=Field<IEcoSortService>(App,"service");Assert.IsFalse(rest.IsMock);
            Prediction result=null;
            yield return rest.Predict(File.ReadAllBytes(image),WasteType.Organic,p=>result=p,e=>Assert.Fail(e));
            Assert.IsNotNull(result);Assert.IsTrue(RecognitionInfo.TryGet(result.class_name,out var category));
            typeof(EcoSortApp).GetMethod("ApplyPrediction",Fields).Invoke(App,new object[]{result});
            if(result.TryRecognize(.75f,out _))StringAssert.Contains(category.display_name,Field<Text>(App,"predictionText").text);
            else StringAssert.Contains("chưa đủ chắc chắn",Field<Text>(App,"predictionText").text);
            // Also exercise file loading, JPEG encoding and the actual Scan UI callback.
            Field<InputField>(App,"imagePath").text=image; Call(App,"LoadImage");
            Assert.IsTrue(Field<RawImage>(App,"scanPreview").enabled);
            ButtonNamed("Chưa biết / cần kiểm tra").onClick.Invoke();
            Call(App,"RunPrediction");float deadline=Time.realtimeSinceStartup+35;
            while(Field<bool>(App,"busy") && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsFalse(Field<bool>(App,"busy"),"Real inference timed out");
            var uiResult=Field<Prediction>(App,"scanResult");
            if(uiResult!=null) Assert.IsTrue(uiResult.TryRecognize(.75f,out _));
            else StringAssert.Contains("chưa đủ chắc chắn",Field<Text>(App,"predictionText").text);
            yield return Capture("recognition-real-rest-20261024");
        }
        [UnityTest] public IEnumerator ExtendedRecognitionShowsGuidanceAndNeverStartsWrongBinChallenge()
        {
            yield return Open("ScanScene");
            var apply=typeof(EcoSortApp).GetMethod("ApplyPrediction",Fields);
            foreach(var entry in RecognitionInfo.All)
            {
                apply.Invoke(App,new object[]{new Prediction{class_name=entry.class_name,confidence=.92f,accepted=true}});
                yield return null;
                StringAssert.Contains(entry.display_name,Field<Text>(App,"predictionText").text);
                StringAssert.Contains(entry.guidance,Field<Text>(App,"recognitionGuidance").text);
                Assert.IsTrue(Field<Button>(App,"challengeButton").interactable);
            }
            apply.Invoke(App,new object[]{new Prediction{class_name="Batteries",confidence=.92f,accepted=true}});
            yield return Capture("recognition-battery-fixture-20261024");
            apply.Invoke(App,new object[]{new Prediction{class_name="Paper",confidence=.92f,accepted=true}});
            Assert.IsTrue(Field<Button>(App,"challengeButton").interactable);
            apply.Invoke(App,new object[]{new Prediction{class_name="Batteries",confidence=.4f,accepted=false}});
            Assert.IsTrue(Field<Button>(App,"challengeButton").interactable);
            Assert.IsNull(Field<Prediction>(App,"scanResult"));
            typeof(EcoSortApp).GetMethod("ShowRecognitionGuide",Fields).Invoke(App,new object[]{10});
            yield return Capture("recognition-guide-fixture-20261024");
            Assert.IsNotNull(GameObject.Find("AI recognition guide"));
        }
        [UnityTest] public IEnumerator AreaJourneyPlaysAllNineMissionsUnlocksRewardsAndRetainsDiscoveries()
        {
            yield return Open("MainMenu");
            ButtonNamed("HÀNH TRÌNH 3 KHU VỰC →").onClick.Invoke();yield return null;
            Assert.IsNotNull(GameObject.Find("Area journey"));
            yield return Capture("areas-home-hub-20261024");
            Assert.AreEqual(1,UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Count(b=>b.interactable && b.GetComponentInChildren<Text>().text=="Nhận nhiệm vụ"));
            foreach(var mission in AreaMissions.All)
            {
                typeof(EcoSortApp).GetMethod("BeginAreaMission",Fields).Invoke(App,new object[]{mission});
                yield return null;yield return null;yield return null;
                Assert.AreEqual("GameScene",SceneManager.GetActiveScene().name);
                Assert.AreEqual(mission.Context,EcoSortApp.SelectedContext);
                Assert.IsNotNull(Field<RectTransform>(App,"missionBrief"));
                if(mission.Context==LearningContext.School)
                {
                    Call(App,"AcceptRestorationMission");Assert.IsNotNull(Field<RectTransform>(App,"missionBrief"),"Question cannot be skipped.");
                    Assert.IsFalse(ButtonNamed("Bắt đầu nhiệm vụ").interactable);
                    if(mission.Id=="school-class")
                    {
                        typeof(EcoSortApp).GetMethod("AnswerAreaQuestion",Fields).Invoke(App,new object[]{(mission.Answer+1)%3});
                        Assert.AreEqual(1,Field<int>(App,"quizMistakes"));
                        yield return Capture("areas-school-question-20261024");
                    }
                    typeof(EcoSortApp).GetMethod("AnswerAreaQuestion",Fields).Invoke(App,new object[]{mission.Answer});
                }
                Call(App,"AcceptRestorationMission");yield return null;
                Assert.IsNull(Field<RectTransform>(App,"missionBrief"));
                var actor=Field<ExplorerController>(App,"explorer");
                var litter=Field<System.Collections.Generic.List<WasteItem>>(App,"scattered");
                Assert.AreEqual(mission.Items.Length,litter.Count);
                Assert.AreEqual(0,Field<float>(App,"sessionLimit"));
                if(mission.Id=="park-path")yield return Capture("areas-park-search-20261024");
                if(mission.Id=="home-kitchen")yield return Capture("areas-home-start-20261024");
                var bins=UnityEngine.Object.FindObjectsByType<WasteBin>(FindObjectsSortMode.None);
                foreach(var item in litter.ToArray())
                {
                    Assert.IsTrue(item.gameObject.activeSelf,"Separate the wrapper before collecting its paper.");
                    var obstacles=Physics.OverlapCapsule(item.transform.position+Vector3.up*.4f,item.transform.position+Vector3.up*1.4f,.33f);
                    Assert.IsFalse(obstacles.Any(c=>c.name=="Raised garden" || c.name=="Boundary wall" || c.name.EndsWith("facade")),item.Definition.Id+" must be reachable");
                    var controller=actor.GetComponent<CharacterController>();controller.enabled=false;
                    actor.transform.position=item.transform.position+Vector3.back*.6f;controller.enabled=true;
                    Call(App,"InteractExploration");Assert.AreSame(item,Field<WasteItem>(App,"carried"));
                    if(mission.Id=="home-kitchen" && Field<int>(App,"missionHints")==0)
                    {Call(App,"RequestAreaHint");Assert.AreEqual(1,Field<int>(App,"missionHints"));}
                    Call(App,"ShowPreparation");
                    if(item==Field<WasteItem>(App,"wrappedCup"))
                    {
                        typeof(EcoSortApp).GetMethod("ChoosePreparation",Fields).Invoke(App,new object[]{PreparationAction.SeparateSleeve});
                        yield return null;
                    }
                    typeof(EcoSortApp).GetMethod("ChoosePreparation",Fields).Invoke(App,new object[]{PreparationRules.Required(item.Definition.Id)});
                    yield return null;
                    var bin=bins.Single(b=>b.type==item.type);controller.enabled=false;
                    actor.transform.position=bin.transform.position+Vector3.back*1.5f;controller.enabled=true;
                    Call(App,"InteractExploration");yield return null;
                }
                Assert.IsTrue(Field<bool>(App,"resultShown"));
                var saved=AreaJourneyStore.Load(testId);Assert.IsTrue(AreaJourneyStore.Completed(saved,mission.Id));
                Assert.AreEqual(mission.Items.Length,saved.missions.Single(m=>m.id==mission.Id).bestCorrect);
                if(mission.Id=="home-kitchen")
                {
                    Assert.AreEqual(2,saved.missions.Single().bestStars,"Using hint must not award independent-decision star.");
                    Assert.IsNotNull(GameObject.Find("Earned green plant"));
                    Assert.IsNull(GameObject.Find("Earned reading stool"),"Later rewards stay locked.");
                    yield return Capture("areas-home-reward-20261024");
                    ButtonNamed("Hành trình").onClick.Invoke();yield return null;yield return null;yield return null;
                    Assert.IsNotNull(GameObject.Find("Area journey"),"Result should return directly to mission selection.");
                }
                else yield return Open("MainMenu");
            }
            var completed=AreaJourneyStore.Load(testId);Assert.AreEqual(9,completed.missions.Count);
            Assert.Contains("Người giữ góc xanh",completed.badges);Assert.GreaterOrEqual(completed.discovered.Count,20);
            Call(App,"ShowAreaRewards");yield return null;
            yield return Capture("areas-rewards-20261024");
            ButtonNamed("Cây: BẬT").onClick.Invoke();yield return null;
            Assert.IsFalse(AreaJourneyStore.Load(testId).plants);
            yield return Open("MainMenu");
            typeof(EcoSortApp).GetMethod("BeginAreaMission",Fields).Invoke(App,new object[]{AreaMissions.All[0]});
            yield return null;yield return null;yield return null;Call(App,"AcceptRestorationMission");yield return null;
            Assert.IsNull(GameObject.Find("Earned green plant"));Assert.IsNotNull(GameObject.Find("Earned reading stool"));
            yield return Capture("areas-earned-decoration-20261024");
            yield return Open("MainMenu");
            typeof(EcoSortApp).GetMethod("ShowCatalog",Fields).Invoke(App,new object[]{0});yield return null;
            Assert.IsTrue(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="Chai nước nhựa"));
            Assert.IsTrue(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="VẬT PHẨM CHƯA GẶP"));
            yield return Capture("areas-discovered-guide-20261024");
        }
        [UnityTest] public IEnumerator EvaluationPageRecordsDistinctTestsAndExportsData()
        {
            yield return Open("MainMenu");var history=Field<LearningHistory>(App,"learning");
            LearningSession Entry(string id,string date)=>new LearningSession{session_id=id,created_at=date,mode="Test",context="Home",total=10,
                answers=new[]{new LearningAnswer{item_id="water-bottle",expected="Plastic",choice="Plastic",correct=true,seconds=2}}};
            history.sessions.Add(Entry("before","2026-10-30T01:00:00Z"));
            typeof(EcoSortApp).GetMethod("ShowLearningStatsPage",Fields).Invoke(App,new object[]{4});yield return null;
            Assert.IsFalse(ButtonNamed("Ghi lượt sau").interactable);ButtonNamed("Ghi lượt đầu").onClick.Invoke();yield return null;
            history.sessions.Add(Entry("after","2026-10-30T02:00:00Z"));
            typeof(EcoSortApp).GetMethod("ShowLearningStatsPage",Fields).Invoke(App,new object[]{4});yield return null;
            Assert.IsTrue(ButtonNamed("Ghi lượt sau").interactable);ButtonNamed("Ghi lượt sau").onClick.Invoke();yield return null;
            Assert.IsTrue(ButtonNamed("Xuất bảng kết quả").interactable);
            yield return Capture("learning-before-after-20261030");
            PlayerPrefs.DeleteKey("EcoSortQuest.Evaluation.v1."+testId+".Home");
        }
        [UnityTest] public IEnumerator PhotoCaptureControlsAndDarkImageAdvice()
        {
            yield return Open("ScanScene");
            Assert.IsNotNull(ButtonNamed("Đổi camera"));Assert.IsNotNull(ButtonNamed("Lưu ảnh kiểm tra"));
            var texture=new Texture2D(32,32,TextureFormat.RGB24,false);var pixels=new Color32[1024];
            for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(5,5,5,255);texture.SetPixels32(pixels);texture.Apply();
            typeof(EcoSortApp).GetMethod("SetImage",Fields).Invoke(App,new object[]{texture});yield return null;
            StringAssert.Contains("tối",Field<Text>(App,"status").text);
            Assert.IsFalse(Field<Button>(App,"challengeButton").interactable);
            yield return Capture("photo-quality-advice-20261030");
        }
        [UnityTest] public IEnumerator NearestItemPreservesRangeAndExclusions()
        {
            EcoSortApp.SelectedAreaMission="home-kitchen";EcoSortApp.SelectedLearningMode=LearningMode.Explore;
            yield return Open("GameScene");Call(App,"AcceptRestorationMission");
            var items=Field<System.Collections.Generic.List<WasteItem>>(App,"scattered");var actor=Field<ExplorerController>(App,"explorer");
            foreach(var item in items)item.transform.position=actor.transform.position+Vector3.right*5;
            items[0].transform.position=actor.transform.position+Vector3.right;
            items[1].transform.position=actor.transform.position+Vector3.right*.5f;
            var lookup=(Func<WasteItem>)Delegate.CreateDelegate(typeof(Func<WasteItem>),App,typeof(EcoSortApp).GetMethod("NearestLitter",Fields));
            Assert.AreEqual(items[1],lookup());items[1].gameObject.SetActive(false);Assert.AreEqual(items[0],lookup());
            typeof(EcoSortApp).GetField("carried",Fields).SetValue(App,items[0]);Assert.IsNull(lookup());
        }
        [UnityTest] public IEnumerator WebcamCaptureOptInSmoke()
        {
            if(Environment.GetEnvironmentVariable("ECOSORT_WEBCAM_SMOKE")!="1")Assert.Ignore("Physical webcam smoke requires explicit opt in");
            yield return Open("ScanScene");
            if(WebCamTexture.devices.Length==0)Assert.Ignore("No webcam available on this machine");
            yield return (IEnumerator)typeof(EcoSortApp).GetMethod("OpenCamera",Fields).Invoke(App,null);
            var camera=Field<WebCamTexture>(App,"webcam");Assert.IsNotNull(camera,"Webcam unavailable or permission denied");
            for(int i=0;i<5;i++)yield return null;
            int width=camera.width,height=camera.height;Call(App,"CaptureCamera");
            Assert.Greater(Field<byte[]>(App,"jpeg").Length,0);Assert.IsNull(Field<WebCamTexture>(App,"webcam"));
            string path=Path.Combine(Application.dataPath,"../Logs/Demo-Webcam-20261030.json");
            File.WriteAllText(path,"{\"physical_capture\":true,\"width\":"+width+",\"height\":"+height+",\"image_saved\":false}");
        }
        [UnityTest] public IEnumerator ExplorationPerformanceSample()
        {
            EcoSortApp.SelectedAreaMission="home-kitchen";EcoSortApp.SelectedLearningMode=LearningMode.Explore;
            yield return Open("GameScene");Call(App,"AcceptRestorationMission");
            for(int i=0;i<40;i++)yield return null;
            var nearest=(Func<WasteItem>)Delegate.CreateDelegate(typeof(Func<WasteItem>),App,typeof(EcoSortApp).GetMethod("NearestLitter",Fields));
            for(int i=0;i<100;i++)nearest();
            const int calls=20000;long allocated=GC.GetAllocatedBytesForCurrentThread();
            var clock=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<calls;i++)nearest();clock.Stop();
            long bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            var samples=new double[180];
            for(int i=0;i<samples.Length;i++){long before=System.Diagnostics.Stopwatch.GetTimestamp();yield return null;samples[i]=1000.0*(System.Diagnostics.Stopwatch.GetTimestamp()-before)/System.Diagnostics.Stopwatch.Frequency;}
            Array.Sort(samples);
            string label=Environment.GetEnvironmentVariable("ECOSORT_BENCHMARK_LABEL")=="before"?"before":"after";
            string directory=Path.Combine(Application.dataPath,"../Logs/Performance-20261030");Directory.CreateDirectory(directory);
            string json="{\"environment\":\"Unity Editor PlayMode\",\"scene\":\"home-kitchen stationary\",\"calls\":"+calls+
                ",\"bytes\":"+bytes+",\"search_ms\":"+clock.Elapsed.TotalMilliseconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+
                ",\"frame_median_ms\":"+samples[90].ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+
                ",\"frame_p95_ms\":"+samples[171].ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+
                ",\"width\":"+Screen.width+",\"height\":"+Screen.height+"}";
            File.WriteAllText(Path.Combine(directory,label+".json"),json);
            if(label=="after")Assert.Less(bytes,200000,"Nearest-item lookup should avoid allocations proportional to calls");
        }
        [UnityTest] public IEnumerator MissionSceneryHasReachableItemsAndDistinctRooms()
        {
            foreach(var mission in AreaMissions.All)
            {
                EcoSortApp.SelectedAreaMission=mission.Id;EcoSortApp.SelectedLearningMode=LearningMode.Explore;
                yield return Open("GameScene");
                var layout=Field<AreaSceneryLayout>(App,"sceneryLayout");Assert.IsNotNull(layout);
                Assert.AreEqual(mission.Items.Length,layout.Items.Length);
                Assert.IsNotNull(GameObject.Find("Mission scenery • "+mission.Id));
                if(mission.Context==LearningContext.School)typeof(EcoSortApp).GetMethod("AnswerAreaQuestion",Fields).Invoke(App,new object[]{mission.Answer});
                Call(App,"AcceptRestorationMission");yield return null;Physics.SyncTransforms();
                var actor=Field<ExplorerController>(App,"explorer");
                Assert.Less(Vector2.Distance(new Vector2(actor.transform.position.x,actor.transform.position.z),new Vector2(layout.Spawn.x,layout.Spawn.z)),.35f,mission.Id+" character must start at the authored entry");
                bool Blocked(Vector3 point)
                {
                    return Physics.OverlapCapsule(point+Vector3.up*.42f,point+Vector3.up*1.50f,.34f).Any(c=>
                        c.bounds.max.y>.12f && c.GetComponentInParent<WasteItem>()==null && c.GetComponentInParent<ExplorerController>()==null);
                }
                const float step=.65f;var bounds=layout.WalkBounds;
                int cols=Mathf.FloorToInt(bounds.width/step)+1,rows=Mathf.FloorToInt(bounds.height/step)+1;
                var free=new bool[cols,rows];var seen=new bool[cols,rows];
                Vector3 Cell(int x,int z)=>new Vector3(bounds.xMin+x*step,.04f,bounds.yMin+z*step);
                for(int x=0;x<cols;x++)for(int z=0;z<rows;z++)free[x,z]=!Blocked(Cell(x,z));
                int sx=Mathf.Clamp(Mathf.RoundToInt((layout.Spawn.x-bounds.xMin)/step),0,cols-1),sz=Mathf.Clamp(Mathf.RoundToInt((layout.Spawn.z-bounds.yMin)/step),0,rows-1);
                Assert.IsTrue(free[sx,sz],mission.Id+" spawn blocked");
                var queue=new System.Collections.Generic.Queue<Vector2Int>();queue.Enqueue(new Vector2Int(sx,sz));seen[sx,sz]=true;
                var reachable=new System.Collections.Generic.List<Vector3>();
                while(queue.Count>0)
                {
                    var cell=queue.Dequeue();reachable.Add(Cell(cell.x,cell.y));
                    foreach(var dir in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down})
                    {var n=cell+dir;if(n.x>=0&&n.x<cols&&n.y>=0&&n.y<rows&&free[n.x,n.y]&&!seen[n.x,n.y]){seen[n.x,n.y]=true;queue.Enqueue(n);}}
                }
                foreach(var position in layout.Items)
                {
                    Assert.IsFalse(Blocked(position),mission.Id+" item in furniture at "+position);
                    Assert.IsTrue(reachable.Any(p=>Vector2.Distance(new Vector2(p.x,p.z),new Vector2(position.x,position.z))<1.5f),mission.Id+" unreachable item "+position);
                }
                foreach(var bin in UnityEngine.Object.FindObjectsByType<WasteBin>(FindObjectsSortMode.None))
                    Assert.IsTrue(reachable.Any(p=>Vector2.Distance(new Vector2(p.x,p.z),new Vector2(bin.transform.position.x,bin.transform.position.z))<1.8f),mission.Id+" unreachable bin");
                yield return Capture("scenery-"+mission.Id+"-play");
                typeof(EcoSortApp).GetField("paused",Fields).SetValue(App,true);
                var camera=actor.FollowCamera;camera.transform.position=new Vector3(16,22,-24);camera.transform.LookAt(new Vector3(0,0,-1));
                yield return Capture("scenery-"+mission.Id+"-overview");
                int total=mission.Items.Length;
                AreaJourneyStore.Complete(testId,mission,"scenery-"+mission.Id,total,total,0,0,new[]{"Plastic","Metal","Paper","Organic"});
            }
        }
        static IEnumerator Capture(string name,int width=1600,int height=900)
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Evidence/Scenery-20261028"));
            Directory.CreateDirectory(dir);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            var target = new RenderTexture(width, height, 24);
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var oldMode = canvas.renderMode; float oldAspect = camera.aspect;
            camera.targetTexture = target; camera.aspect = (float)width / height;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases(); yield return null;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
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
