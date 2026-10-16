using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoSortQuest
{
    // Scene composition root. Rules, input, visuals and transport remain separate.
    public sealed partial class EcoSortApp : MonoBehaviour
    {
        public ClientScreen screen;
        public ApiSettings apiSettings;
        public LevelConfig[] levels;
        public WasteItem[] itemPrefabs;
        public WasteBin[] binPrefabs;
        public Transform authoredWorld;
        public Camera authoredCamera;
        public static int SelectedLevel { get; set; } = 1;
        public static Prediction PendingPrediction { get; set; }

        ViewFactory view;
        WorldFactory world;
        Camera sceneCamera;
        PointerInput pointer;
        PlayerProgress progress;
        IEcoSortService service;
        Transform worldRoot;
        Text status;
        bool busy, paused, resolving, arPlaced, arComplete, resultShown;
        SortingRound round;
        LevelConfig level;
        WasteItem currentItem;
        bool dragging;
        Vector3 dragOrigin, dragOffset;
        Plane dragPlane;
        WasteType[] sequence;
        WasteDefinition[] roundItems;
        ConveyorMotion conveyor;
        Text lessonText, itemName;
        Text scoreText, correctText, timeText, instruction;
        RectTransform pausePanel;
        float remaining;
        readonly IPlacementProvider placement = new SimulatedPlacementProvider();
        string Mode => service.IsMock ? "OFFLINE / LUYỆN TẬP" : "ONLINE / FASTAPI";

        void Start()
        {
            Time.timeScale = 1;
            progress = LocalProgress.Load(); LocalProgress.Save(progress);
            learning = LearningStore.Load(progress.player_id);
            LoadConnectionSettings();
            service = apiSettings != null && apiSettings.mode == ApiMode.Rest ?
                (IEcoSortService)new RestEcoSortService(apiSettings) : new MockEcoSortService(apiSettings != null ? apiSettings.mockLatency : .35f);
            worldRoot = authoredWorld != null ? authoredWorld : new GameObject("Runtime World").transform;
            if (authoredWorld == null) worldRoot.SetParent(transform, false);
            world = new WorldFactory(worldRoot);
            sceneCamera = authoredCamera != null ? authoredCamera : world.BuildEnvironment(screen != ClientScreen.ARChallenge);
            if (screen == ClientScreen.Game && SelectedLearningMode != LearningMode.Explore)
            {
                sceneCamera.transform.position = new Vector3(0, 10, -15);
                sceneCamera.transform.LookAt(new Vector3(0, .5f, 1)); sceneCamera.orthographicSize = 6.2f;
                conveyor = gameObject.AddComponent<ConveyorMotion>(); conveyor.Configure(worldRoot);
            }
            if (screen == ClientScreen.Scan || screen == ClientScreen.Progress)
                foreach (var renderer in worldRoot.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            gameAudio = gameObject.AddComponent<EcoSortAudio>(); gameAudio.Initialize(sceneCamera, screen == ClientScreen.Game);
            view = new ViewFactory(transform);
            view.SettingsRequested=()=>ShowSettings(0);ApplyVisualSettings();
            pointer = gameObject.AddComponent<PointerInput>();
            pointer.Selected += OnWorldSelect; pointer.PausePressed += TogglePause;
            pointer.Dragged += DragItem; pointer.Released += DropItem;
            switch (screen)
            {
                case ClientScreen.MainMenu: ShowMenu(); break;
                case ClientScreen.Game: if (SelectedLearningMode == LearningMode.Explore) StartExploration(); else StartRound(); break;
                case ClientScreen.Scan: ShowScan(); break;
                case ClientScreen.ARChallenge: ShowAR(); break;
                case ClientScreen.Progress: ShowProgress(); break;
            }
        }
        void Update()
        {
            if (explorer != null) { UpdateExploration(); return; }
            if (round != null && !round.Finished && !paused && !resolving)
            {
                answerSeconds += Time.deltaTime;
                float distance = BeltSpeed * Time.deltaTime;
                conveyor?.Tick(distance);
                if (!dragging && currentItem != null)
                {
                    var p = currentItem.transform.localPosition;
                    p.x = Mathf.Repeat(p.x + distance + 3.5f, 7f) - 3.5f;
                    currentItem.transform.localPosition = p;
                }
            }
            if (round != null && !round.Finished && !paused && !resolving && sessionLimit > 0)
            {
                remaining = Mathf.Max(0, remaining - Time.deltaTime);
                timeText.text = "THỜI GIAN\n" + Mathf.CeilToInt(remaining) + "s";
                if (remaining <= 0) { round.Expire(); FinishRound(); }
            }
            if (webcam != null && webcam.isPlaying && scanPreview != null && webcam.didUpdateThisFrame)
            {
                scanPreview.uvRect = webcam.videoVerticallyMirrored ? new Rect(0, 1, 1, -1) : new Rect(0, 0, 1, 1);
                scanPreview.rectTransform.localEulerAngles = new Vector3(0, 0, -webcam.videoRotationAngle);
            }
        }
        void OnDestroy()
        {
            Time.timeScale = 1; StopCamera();
            if (imageTexture != null) Destroy(imageTexture);
            world?.Dispose();

            if (ownsSettings && apiSettings != null) Destroy(apiSettings);
        }
        void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
        void Navigate(string sceneName)
        {
            if (busy) { SetStatus("Đang gửi dữ liệu, vui lòng chờ trước khi chuyển màn."); return; }
            Time.timeScale = 1; StopCamera(); SceneManager.LoadScene(sceneName);
        }
        void Home() => Navigate("MainMenu");
        void SetStatus(string message) { if (status != null) status.text = message; }
        void Footer(string message)
        {
            var panel = view.Panel(view.Root, 0, 0, 1, .065f, ViewFactory.Cream);
            status = view.Text(panel, message, .035f, .1f, .93f, .8f, 19, ViewFactory.Muted);
        }
        WasteItem SpawnItem(WasteType type, Vector3 position)
        {
            int index = (int)type;
            if (itemPrefabs != null && itemPrefabs.Length > index && itemPrefabs[index] != null)
            { var item = Instantiate(itemPrefabs[index], worldRoot); item.transform.localPosition = position; item.Select(false); return item; }
            return world.Item(type, position);
        }
        void SpawnBins(Vector3 origin)
        {
            for (int i = 0; i < 4; i++)
            {
                var position = origin + new Vector3((i - 1.5f) * 2.6f, 0, 0);
                if (binPrefabs != null && binPrefabs.Length > i && binPrefabs[i] != null)
                { var bin = Instantiate(binPrefabs[i], worldRoot); bin.transform.localPosition = position; }
                else world.Bin((WasteType)i, position);
            }
        }
        void ShowMenu()
        {
            SpawnBins(new Vector3(0, 0, -1.7f));
            SpawnItem(WasteType.Plastic, new Vector3(1, .88f, 1));
            sceneCamera.transform.position = new Vector3(7, 10, -14);
            sceneCamera.transform.LookAt(new Vector3(-3, .5f, 0)); sceneCamera.orthographicSize = 7;
            view.Header("HỌC PHÂN LOẠI QUA TỪNG LƯỢT CHƠI", Mode, null);
            var card = view.Panel(view.Root, .025f, .09f, .48f, .74f, ViewFactory.Cream, "Menu card");
            view.Text(card, "SMALL ACTIONS.\nGREENER TOMORROW.", .06f, .73f, .88f, .22f, 44);
            view.Text(card, "Kéo vật phẩm. Thả vào đúng thùng.\nBa cấp độ, một thói quen tốt hơn.", .06f, .58f, .88f, .12f, 24, ViewFactory.Muted);
            for (int i = 1; i <= 3; i++)
            {
                int n = i; bool unlocked = progress.unlocked_level >= n;
                var button = view.Button(card, "LEVEL " + n + (unlocked ? "  →" : "  / KHÓA"), .06f + (n - 1) * .30f, .45f, .28f, .085f,
                    () => { SelectedLearningMode = LearningMode.Campaign; SelectedLevel = n; Navigate("GameScene"); }, n == 1);
                button.interactable = unlocked;
            }
            var challenge = view.Button(card, "Real-World Challenge" + (progress.ChallengeUnlocked ? "  →" : " / Sau Level 3"), .06f, .32f, .88f, .09f,
                () => { AiMission = false; Navigate("ScanScene"); }); challenge.interactable = progress.ChallengeUnlocked;
            view.Button(card, "Tiến trình", .06f, .20f, .42f, .085f, () => Navigate("ProgressScene"));
            view.Button(card, "Cách chơi", .52f, .20f, .42f, .085f, ShowHelp);
            view.Button(card, "Sổ tay 36 vật phẩm", .06f, .095f, .42f, .075f, () => ShowCatalog(0));
            view.Button(card, "Kết nối backend", .52f, .095f, .42f, .075f, ShowConnection);
            view.Button(card, "Học tập / AI / Thống kê", .06f, .005f, .64f, .065f, ShowLearningHub, true);
            view.Button(card, "Thoát", .73f, .005f, .21f, .065f, Quit);
            Footer("36 vật phẩm • Kéo thả trên băng chuyền • AI qua FastAPI • Thử thách 3D mô phỏng trên PC");
            if (!progress.player_id.StartsWith("test-") && PlayerPrefs.GetInt("EcoSortQuest.TutorialSeen", 0) == 0)
            { PlayerPrefs.SetInt("EcoSortQuest.TutorialSeen", 1); PlayerPrefs.Save(); ShowHelp(); }
        }
        void Quit()
        {
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
        void ShowHelp()
        {
            var overlay = view.Panel(view.Root, .18f, .22f, .64f, .58f, ViewFactory.Cream, "How to play");
            view.Text(overlay, "CÁCH CHƠI", .07f, .80f, .86f, .12f, 34);
            view.Text(overlay, "1. Giữ chuột trái trên vật phẩm để kéo.\n2. Thả rác vào thùng Plastic, Metal, Paper hoặc Organic.\n3. Đúng +10; sai −5; điểm không âm.\n4. Đúng ít nhất 7/10 để mở level tiếp theo.\n\nESC: tạm dừng. Thử thách mô phỏng mở sau Level 3.\nMàu thùng minh họa cho game, không phải quy định địa phương.", .07f, .22f, .86f, .55f, 25);
            view.Button(overlay, "Đã hiểu", .3f, .05f, .4f, .12f, () => Destroy(overlay.gameObject), true);
        }
        void StartRound()
        {
            if (SelectedLearningMode == LearningMode.Campaign) { AiMission = false; SelectedContext = (LearningContext)(SelectedLevel - 1); }
            if (SelectedLearningMode != LearningMode.Campaign) SelectedLevel = 1;
            SelectedLevel = Mathf.Clamp(SelectedLevel, 1, Mathf.Max(1, progress.unlocked_level));
            level = levels != null && levels.Length >= SelectedLevel ? levels[SelectedLevel - 1] : null;
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelConfig>(); level.level = SelectedLevel;
                level.timeLimit = SelectedLevel == 1 ? 0 : SelectedLevel == 2 ? 75 : 50;
            }
            round = new SortingRound(level.itemCount, level.requiredCorrect);
            sessionLimit = SelectedLearningMode == LearningMode.Test ? 75 : SelectedLearningMode == LearningMode.Campaign ? level.timeLimit : 0;
            remaining = sessionLimit; answerSeconds = 0; learningAnswers.Clear(); BuildLearningContext();
            sequence = new WasteType[level.itemCount];
            var source = level.itemSequence;
            for (int i = 0; i < sequence.Length; i++) sequence[i] = source != null && source.Length > 0 ? source[i % source.Length] : (WasteType)(i % 3);
            for (int i = sequence.Length - 1; i > 0; i--)
            { int j = UnityEngine.Random.Range(0, i + 1); var t = sequence[i]; sequence[i] = sequence[j]; sequence[j] = t; }
            roundItems = LearningDeck.Build(SelectedContext, level.itemCount, UnityEngine.Random.Range(0, int.MaxValue));
            if (SelectedLearningMode != LearningMode.Campaign)
                roundItems = LearningDeck.Build(SelectedContext, level.itemCount, UnityEngine.Random.Range(0,int.MaxValue),
                    SelectedLearningMode == LearningMode.Review ? LearningStore.Mistakes(learning) : null);
            view.Header(LearningLabel + " / " + ContextLabel, Mode, Home);
            var hud = view.Panel(view.Root, .02f, .80f, .96f, .09f, ViewFactory.Cream);
            scoreText = view.Text(hud, "", .025f, .1f, .2f, .8f, 27);
            correctText = view.Text(hud, "", .25f, .1f, .29f, .8f, 24);
            timeText = view.Text(hud, sessionLimit > 0 ? "THỜI GIAN\n" + remaining + "s" : "THỜI GIAN\nKhông giới hạn", .56f, .1f, .23f, .8f, 24);
            view.Button(hud, "Pause / ESC", .81f, .2f, .17f, .6f, TogglePause);
            var lower = view.Panel(view.Root, .02f, .065f, .96f, .135f, ViewFactory.Cream);
            itemName = view.Text(lower, "", .015f, .64f, .97f, .3f, 25, ViewFactory.Ink);
            instruction = view.Text(lower, "", .015f, .34f, .97f, .3f, 23, ViewFactory.Accent);
            lessonText = view.Text(lower, "", .015f, .02f, .97f, .3f, 20, ViewFactory.Muted);
            Footer("Đúng +10 • Sai −5 • Qua màn: " + level.requiredCorrect + "/" + level.itemCount + " • Rác chạy vòng lại nếu chưa nhặt • ESC tạm dừng");
            SpawnBins(new Vector3(0, 0, -1.8f));
            round.Changed += RefreshRound; RefreshRound(); NextItem();
        }
        void RefreshRound()
        {
            scoreText.text = SelectedLearningMode == LearningMode.Test && !round.Finished ? "KIỂM TRA\nẨn điểm" : "ĐIỂM\n" + round.Score;
            correctText.text = (SelectedLearningMode == LearningMode.Test && !round.Finished ? "ẨN ĐÁP ÁN" : "ĐÚNG " + round.Correct + "/" + round.Total) + "   •   ĐÃ CHỌN " + round.Answered + "/" + round.Total;
        }
        void NextItem()
        {
            if (round.Finished) return;
            currentItem = world.CatalogItem(roundItems[round.Answered], new Vector3(-2.8f, .89f, 1.1f));
            itemName.text = "VẬT PHẨM " + (round.Answered + 1) + "/" + round.Total + (SelectedLearningMode == LearningMode.Test ? " • Quan sát mô hình" : "  •  " + currentItem.Definition.Name);
            lessonText.text = Hints ? "Gợi ý: " + WasteInfo.Label(currentItem.type) : SelectedLearningMode == LearningMode.Test ? "Kiểm tra: giải thích hiển thị cuối lượt." : "Quan sát chất liệu, kéo vào thùng phù hợp. Đọc kiến thức sau mỗi đáp án.";
            instruction.text = "Vật phẩm " + (round.Answered + 1) + "/" + round.Total + " — Kéo rác và thả vào thùng.";
            instruction.color = ViewFactory.Accent;
            resolving = false;
        }
        void OnWorldSelect(Vector2 position)
        {
            if (explorer != null || settingsRoot != null || paused || resolving || resultShown || arComplete) return;
            Ray ray = sceneCamera.ScreenPointToRay(position);
            if (screen == ClientScreen.ARChallenge && !arPlaced)
            {
                if (placement.TryPlace(ray, out Vector3 point)) PlaceChallenge(point);
                else instruction.text = "Chọn mặt sàn ở khu vực giữa để đặt bốn thùng mô phỏng.";
                return;
            }
            if (screen != ClientScreen.Game && screen != ClientScreen.ARChallenge) return;
            if (!Physics.Raycast(ray, out RaycastHit hit, 100)) return;
            var item = hit.collider.GetComponentInParent<WasteItem>();
            if (item != null && item == currentItem)
            {
                dragging = true; dragOrigin = currentItem.transform.position;
                dragPlane = new Plane(-sceneCamera.transform.forward, dragOrigin);
                dragOffset = dragPlane.Raycast(ray, out float distance) ? dragOrigin - ray.GetPoint(distance) : Vector3.zero;
                currentItem.Select(true);gameAudio?.Pickup(); instruction.text = "Giữ chuột và kéo rác vào thùng, rồi thả chuột.";
            }
        }
        void DragItem(Vector2 position)
        {
            if (!dragging || paused || currentItem == null) return;
            var ray = sceneCamera.ScreenPointToRay(position);
            if (dragPlane.Raycast(ray, out float distance)) currentItem.transform.position = ray.GetPoint(distance) + dragOffset;
        }
        void CancelDrag()
        {
            if (dragging && currentItem != null) { currentItem.transform.position = dragOrigin; currentItem.Select(false); }
            dragging = false;
        }
        void DropItem(Vector2 position, bool overUI)
        {
            if (!dragging) return;
            CancelDrag();
            if (overUI || paused || resolving || resultShown || arComplete || currentItem == null) return;
            // Ignore the dragged item's colliders when finding the bin beneath the pointer.
            var hits = Physics.RaycastAll(sceneCamera.ScreenPointToRay(position), 100);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            WasteBin bin = null;
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<WasteItem>() == currentItem) continue;
                bin = hit.collider.GetComponentInParent<WasteBin>(); break;
            }
            if (bin == null) { instruction.text = "Thả vào một thùng rác. Vật phẩm đã trở về vị trí ban đầu."; return; }
            gameAudio?.Drop();
            if (screen == ClientScreen.Game) StartCoroutine(SortItem(bin.type));
            else CompleteChallenge(bin.type);
        }
        IEnumerator SortItem(WasteType bin)
        {
            if (round.Finished) yield break;
            resolving = true; bool correct = round.Submit(currentItem.type, bin);
            learningAnswers.Add(new LearningAnswer { item_id = currentItem.Definition.Id, expected = currentItem.type.ToString(), choice = bin.ToString(), correct = correct, seconds = answerSeconds });
            answerSeconds = 0;
            if (SelectedLearningMode != LearningMode.Test) { gameAudio?.FeedbackAfterDrop(correct); StartCoroutine(PulseBin(bin)); instruction.color = correct ? ViewFactory.Accent : new Color(.70f,.22f,.15f); }
            instruction.text = (correct ? "CHÍNH XÁC! +10 điểm • " : "CHƯA ĐÚNG. −5 điểm • Đáp án: ") + WasteInfo.Label(currentItem.type);
            if (lessonText != null) lessonText.text = currentItem.Definition != null ? currentItem.Definition.Lesson : WasteInfo.Tip(currentItem.type);
            if (SelectedLearningMode == LearningMode.Test) { instruction.text = "Đã ghi nhận đáp án. Xem giải thích cuối lượt."; lessonText.text = "Kiểm tra độc lập • Không hiển thị đúng/sai trong lượt."; }
            yield return new WaitForSeconds(SelectedLearningMode == LearningMode.Test ? .35f : 1.5f);
            if (currentItem != null) Destroy(currentItem.gameObject);
            if (round.Finished) FinishRound(); else NextItem();
        }
        void FinishRound()
        {
            if (resultShown) return; resultShown = true; resolving = true;
            CancelDrag();
            RecordLearningRound();
            if (SelectedLearningMode == LearningMode.Campaign) { progress.RecordRound(SelectedLevel, round); LocalProgress.Save(progress); }
            var overlay = view.Panel(view.Root, .15f, .18f, .70f, .58f, ViewFactory.Cream, "Round result");
            view.Text(overlay, SelectedLearningMode != LearningMode.Campaign ? "KẾT QUẢ " + LearningLabel : round.Passed ? "HOÀN THÀNH LEVEL " + SelectedLevel : "THỬ LẠI NHÉ!", .07f, .72f, .86f, .18f, 40);
            view.Text(overlay, "Điểm: " + round.Score + "   •   Đúng: " + round.Correct + "/" + round.Total +
                "\n" + (SelectedLearningMode != LearningMode.Campaign ? "Đã lưu lịch sử. Xem giải thích hoặc ôn lại câu sai." : round.Passed ? (SelectedLevel == 3 ? "Real-World Challenge đã mở khóa." : "Level tiếp theo đã mở khóa.") : "Cần ít nhất " + round.Required + " đáp án đúng. Các câu chưa trả lời không tính đúng."), .07f, .39f, .86f, .29f, 26);
            view.Button(overlay, "Chơi lại", .07f, .12f, .25f, .15f, () => Navigate("GameScene"));
            view.Button(overlay, "Menu", .37f, .12f, .25f, .15f, Home);
            if (round.Passed && SelectedLearningMode == LearningMode.Campaign) view.Button(overlay, SelectedLevel < 3 ? "Level tiếp →" : "Thử thách →", .67f, .12f, .26f, .15f, () =>
            { if (busy) return; if (SelectedLevel < 3) { SelectedLevel++; Navigate("GameScene"); } else Navigate("ScanScene"); }, true);
            view.Button(overlay, "Xem đáp án", .07f, .01f, .40f, .09f, () => ShowAnswerReview());
            view.Button(overlay, "Ôn câu sai", .53f, .01f, .40f, .09f, () => { if (LearningStore.Mistakes(learning).Count == 0) { SetStatus("Không còn câu sai cần ôn."); return; } SelectedLearningMode = LearningMode.Review; Navigate("GameScene"); });
            StartCoroutine(WithBusy(SaveRound()));
        }
        IEnumerator SaveRound()
        {
            if (SelectedLearningMode != LearningMode.Campaign) { yield return SyncLearning(); yield break; }
            SetStatus("Đã lưu cục bộ. Đang gửi kết quả đến " + Mode + "…");
            bool failed = false;
            yield return service.PostScore(new ScoreRequest { player_id = progress.player_id, level = SelectedLevel,
                score = round.Score, correct_count = round.Correct }, () => { }, e => { failed = true; SetStatus("Điểm chưa gửi: " + e + " • Tiến trình vẫn được lưu cục bộ."); });
            yield return service.PutProgress(progress, () => { if (!failed) SetStatus("Đã lưu điểm và tiến trình • " + Mode); },
                e => SetStatus("Đã lưu cục bộ; đồng bộ thất bại: " + e));
            yield return SyncLearning();
        }
        void TogglePause()
        {
            if(settingsRoot!=null){CloseSettings();return;}
            if (screen != ClientScreen.Game || round == null || resultShown || (round.Finished && explorer == null)) return;
            CancelDrag();
            paused = !paused; Time.timeScale = paused ? 0 : 1;
            if (!paused) { if (pausePanel != null) Destroy(pausePanel.gameObject); return; }
            pausePanel = view.Panel(view.Root, .30f, .28f, .40f, .37f, ViewFactory.Cream, "Paused");
            view.Text(pausePanel, "TẠM DỪNG", .08f, .65f, .84f, .23f, 38, null, TextAnchor.MiddleCenter);
            view.Button(pausePanel, "Tiếp tục / ESC", .1f, .36f, .8f, .20f, TogglePause, true);
            view.Button(pausePanel, "Về menu", .1f, .09f, .8f, .20f, Home);
        }
        void ShowProgress()
        {
            view.Header("MY PROGRESS", Mode, Home);
            var panel = view.Panel(view.Root, .10f, .16f, .8f, .64f, ViewFactory.Cream);
            var summary = view.Text(panel, "", .05f, .36f, .9f, .56f, 30);
            Action refresh = () => summary.text = "HÀNH TRÌNH CỦA BẠN\n\nLevel đã mở: " + progress.unlocked_level + "/3   •   Điểm cao nhất: " + progress.best_score +
                "\nAR mô phỏng: " + progress.ar_completed + " nhiệm vụ   •   " + progress.ar_completed * 20 + " điểm AR" + "\nHuy hiệu: " + (progress.HasBadge ? "EcoSort Beginner" : "Hoàn thành 3 level và một thử thách để nhận") +
                "\n\nPlayer ID: " + progress.player_id;
            refresh();
            view.Button(panel, "Tải từ " + (service.IsMock ? "mock" : "backend"), .05f, .19f, .28f, .12f, () =>
            {
                if (busy) return;
                StartCoroutine(WithBusy(service.GetProgress(progress.player_id, data =>
                { progress.Merge(data); LocalProgress.Save(progress); refresh(); SetStatus("Đã đọc và hợp nhất tiến trình • " + Mode); }, SetStatus)));
            });
            view.Button(panel, "Đồng bộ tiến trình", .36f, .19f, .28f, .12f, () =>
            { if (!busy) StartCoroutine(WithBusy(service.PutProgress(progress, () => SetStatus("Đã gửi tiến trình • " + Mode), SetStatus))); }, true);
            view.Button(panel, "Kiểm tra API", .67f, .19f, .28f, .12f, () =>
            { if (!busy) StartCoroutine(WithBusy(service.Health(SetStatus, SetStatus))); });
            view.Text(panel, "Tiến trình luôn có bản lưu cục bộ. Mock dùng PlayerPrefs riêng, không phải cloud.\nKhi backend lỗi, có thể thử đồng bộ lại tại đây.", .05f, .015f, .9f, .14f, 21, ViewFactory.Muted);
            Footer("Sẵn sàng • " + Mode);
        }
        IEnumerator WithBusy(IEnumerator operation)
        { busy = true; SetStatus("Đang xử lý…"); yield return operation; busy = false; }
    }
}

