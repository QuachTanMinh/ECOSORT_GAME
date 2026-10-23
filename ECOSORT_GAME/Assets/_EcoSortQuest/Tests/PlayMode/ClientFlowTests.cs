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
            if (hadSave) PlayerPrefs.SetString(SaveKey, originalSave); else PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(LearningStore.Key(testId));
            foreach(LearningContext context in System.Enum.GetValues(typeof(LearningContext)))
                PlayerPrefs.DeleteKey(RestorationSave.Key(testId,context));
            PlayerPrefs.DeleteKey("EcoSort.MockCloud." + testId); PlayerPrefs.Save();
            EcoSortApp.SelectedLevel = 1; EcoSortApp.PendingPrediction = null;
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
            yield return Open("MainMenu");Call(App,"ShowLearningHub");
            ButtonNamed("Công viên").onClick.Invoke();yield return null;
            yield return Capture("learning-hub");
            ButtonNamed("Kết quả học tập").onClick.Invoke();yield return null;
            yield return Capture("learning-statistics");
            foreach(var context in new[]{LearningContext.Home,LearningContext.School,LearningContext.Park})
            {
                EcoSortApp.SelectedContext=context;EcoSortApp.SelectedLearningMode=LearningMode.Learn;
                yield return Open("GameScene");yield return Capture("learning-context-"+context);
                Assert.AreEqual(0,Field<float>(App,"sessionLimit"));
            }
        }
        [UnityTest] public IEnumerator AiMissionRequiresOwnChoiceAndSeparatesMockFromReal()
        {
            EcoSortApp.AiMission=true;yield return Open("ScanScene");
            ButtonNamed("Chạy dự đoán MOCK").onClick.Invoke();
            Assert.IsNull(Field<Prediction>(App,"scanResult"));
            ButtonNamed("Plastic").onClick.Invoke();
            ButtonNamed("Chạy dự đoán MOCK").onClick.Invoke();yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(Field<bool>(App,"missionPassed"));
            Assert.IsTrue(LearningStore.Load(testId).missions.Single().mock);
            yield return Capture("learning-ai-mission-mock");
            ButtonNamed("Nhiệm vụ tiếp / hoàn thành →").onClick.Invoke();
            Assert.IsNull(Field<byte[]>(App,"jpeg"));Assert.IsNull(Field<Prediction>(App,"scanResult"));
            Assert.AreEqual(1,Field<int>(App,"missionStep"));
        }
        [UnityTest] public IEnumerator ExplorationRequiresProximitySupportsCorrectionAndSavesCompleteRound()
        {
            yield return Open("MainMenu");yield return Capture("restoration-main-menu");
            ButtonNamed("PHỤC HỒI CÔNG VIÊN →").onClick.Invoke();yield return null;yield return null;
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
