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
        bool busy, paused, resolving, arPlaced, arComplete, resultShown, deviceARActive;
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
        RectTransform roundProgressFill;
        WasteBin hoveredBin;
        readonly RaycastHit[] dropHits=new RaycastHit[48];
        RectTransform pausePanel;
        float remaining;
        IPlacementProvider placement;
        string Mode => service.IsMock ? "CHƠI TRÊN MÁY" : "ĐÃ BẬT KẾT NỐI";

        void Start()
        {
            Time.timeScale = 1;
            progress = LocalProgress.Load(); LocalProgress.Save(progress);
            learning = LearningStore.Load(progress.player_id);
            LoadConnectionSettings();
            service = apiSettings != null && apiSettings.mode == ApiMode.Rest ?
                (IEcoSortService)new RestEcoSortService(apiSettings) : new MockEcoSortService(apiSettings != null ? apiSettings.mockLatency : .35f);
            deviceARActive = screen == ClientScreen.ARChallenge && Application.isMobilePlatform &&
                (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android);
            if (deviceARActive)
            {
                worldRoot = new GameObject("AR Challenge Content").transform;
                if (authoredCamera != null) authoredCamera.transform.SetParent(null, true);
                if (authoredWorld != null) authoredWorld.gameObject.SetActive(false);
            }
            else
            {
                worldRoot = authoredWorld != null ? authoredWorld : new GameObject("Runtime World").transform;
                if (authoredWorld == null) worldRoot.SetParent(transform, false);
            }
            world = new WorldFactory(worldRoot);
            sceneCamera = authoredCamera != null ? authoredCamera : world.BuildEnvironment(screen != ClientScreen.ARChallenge);
            placement = deviceARActive ?
                (IPlacementProvider)new ARFoundationPlacementProvider(ARDeviceSession.Create(sceneCamera, worldRoot)) :
                new SimulatedPlacementProvider();
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
                timeText.text = Mathf.CeilToInt(remaining) + "s";
                timeText.color=remaining<=15?new Color(1f,.65f,.48f):ViewFactory.HudInk;
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
            world?.Dispose(); view?.Dispose();
            if (deviceARActive && worldRoot != null) Destroy(worldRoot.gameObject);

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
            bool playing = screen == ClientScreen.Game;
            float maxWidth=playing?(explorer!=null?.68f:.385f):.94f;
            var panel=view.Panel(view.Root,playing?(explorer!=null?.015f:.60f):.025f,.012f,maxWidth,.035f,
                playing?ViewFactory.HudSurface:ViewFactory.Cream,"Status note");
            panel.GetComponent<Image>().raycastTarget=false;
            status=view.Text(panel,message,.02f,.1f,.96f,.8f,playing?16:18,ViewFactory.Muted);
            view.FitContent(panel,maxWidth,160,status);
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
                float spacing = deviceARActive ? .95f : 2.6f;
                var position = origin + new Vector3((i - 1.5f) * spacing, 0, 0);
                if (binPrefabs != null && binPrefabs.Length > i && binPrefabs[i] != null)
                { var bin = Instantiate(binPrefabs[i], worldRoot); bin.transform.localPosition = position; }
                else world.Bin((WasteType)i, position);
            }
        }
        void ShowMenu()
        {
            SpawnBins(new Vector3(0, 0, -1.7f));
            SpawnItem(WasteType.Plastic, new Vector3(1, .88f, 1));
            sceneCamera.transform.position = new Vector3(8, 11, -15);
            sceneCamera.transform.LookAt(new Vector3(-5.2f, .4f, 0)); sceneCamera.orthographicSize = 7.8f;
            var menuHeader=view.Header("MỖI VẬT PHẨM • MỘT HÀNH ĐỘNG XANH", Mode, null);
            view.Button(menuHeader,"Thoát",.845f,.25f,.085f,.5f,Quit);
            var card=view.Card(view.Root,.035f,.125f,.405f,.735f,ViewFactory.Cream,"Menu card");
            view.Text(card,"HỌC QUA HÀNH ĐỘNG",.06f,.91f,.88f,.045f,18,ViewFactory.Accent);
            view.Text(card,"Phân loại hôm nay.\nXanh hơn ngày mai.",.06f,.735f,.88f,.16f,40);
            view.Text(card,"Khám phá, phân loại và hồi sinh từng khu vực.",.06f,.655f,.88f,.065f,21,ViewFactory.Muted);
            view.Button(card,"HÀNH TRÌNH 3 KHU VỰC →",.06f,.54f,.88f,.085f,ShowAreaHub,true);
            view.Button(card,"Học có hướng dẫn",.06f,.44f,.42f,.07f,()=>ShowPracticePicker(LearningMode.Learn));
            view.Button(card,"Kiểm tra",.52f,.44f,.42f,.07f,()=>ShowPracticePicker(LearningMode.Test));
            view.Button(card,"Ôn câu sai",.06f,.345f,.42f,.07f,()=>ShowPracticePicker(LearningMode.Review));
            view.Button(card,"Phân loại bằng ảnh",.52f,.345f,.42f,.07f,()=>{AiMission=false;Navigate("ScanScene");});
            view.Button(card,"Thử thách phân loại",.06f,.25f,.42f,.07f,ShowCampaignLevels);
            view.Button(card,"Kết quả học tập",.52f,.25f,.42f,.07f,ShowLearningStats);
            view.Button(card,"Sổ tay khám phá",.06f,.145f,.42f,.07f,()=>ShowCatalog(0));
            view.Button(card,"Cách chơi",.52f,.145f,.42f,.07f,ShowHelp);
            view.Text(card,"Chọn một hoạt động để bắt đầu.",.06f,.04f,.88f,.06f,18,ViewFactory.Muted);
            if(openJourneyOnMenu){openJourneyOnMenu=false;ShowAreaHub();}
            Footer("36 vật phẩm • 3 bối cảnh • Học từ những lựa chọn của bạn");
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
            var overlay = view.Card(view.Root, .22f, .23f, .56f, .56f, ViewFactory.Cream, "How to play");
            view.Text(overlay, "CÁCH CHƠI", .07f, .80f, .86f, .12f, 34);
            view.Text(overlay, "1. Giữ chuột trái trên vật phẩm để kéo.\n2. Thả rác vào thùng Plastic, Metal, Paper hoặc Organic.\n3. Đúng +10; sai −5; điểm không âm.\n4. Đúng ít nhất 7/10 để mở level tiếp theo.\n\nESC: tạm dừng. Phân loại bằng ảnh có ví dụ để bạn thử trước.\nMàu thùng minh họa cho game, không phải quy định địa phương.", .07f, .22f, .86f, .55f, 25);
            view.Button(overlay, "Đã hiểu", .3f, .05f, .4f, .12f, () => Destroy(overlay.gameObject), true);
        }
        void StartRound()
        {
            if(SelectedLearningMode==LearningMode.Review)
            {
                var mistakes=LearningStore.Mistakes(learning);
                if(mistakes.Count>0 && !mistakes.Exists(d=>d.AppearsIn(SelectedContext)))
                    SelectedContext=mistakes[0].Contexts[0];
            }
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
            view.Header(LearningLabel + " / " + ContextLabel, Mode, Home, true);
            var hud=view.Panel(view.Root,.015f,.877f,.115f,.044f,ViewFactory.HudSurface,"Compact score HUD");
            hud.GetComponent<Image>().raycastTarget=false;
            scoreText=view.Text(hud,"",0,0,1,1,20);view.FitContent(hud,.115f,110,scoreText);
            var answers=view.Panel(view.Root,.14f,.877f,.275f,.044f,ViewFactory.HudSurface,"Answer count chip");
            answers.GetComponent<Image>().raycastTarget=false;
            correctText=view.Text(answers,"",0,0,1,1,19);view.FitContent(answers,.275f,160,correctText);
            var clock=view.Panel(view.Root,.425f,.877f,.135f,.044f,ViewFactory.HudSurface,"Timer chip");
            clock.GetComponent<Image>().raycastTarget=false;
            timeText=view.Text(clock,sessionLimit>0?remaining+"s":"Không giới hạn",0,0,1,1,19);view.FitContent(clock,.135f,72,timeText);
            view.Button(view.Root,"Tạm dừng / ESC",.86f,.877f,.125f,.044f,TogglePause);
            var track=view.Panel(view.Root,.015f,.864f,.30f,.004f,new Color(.40f,.52f,.57f),"Round progress track");
            track.GetComponent<Image>().raycastTarget=false;
            roundProgressFill=view.Panel(track,0,0,0,1,ViewFactory.Accent,"Round progress");
            roundProgressFill.GetComponent<Image>().raycastTarget=false;
            var lower=view.Card(view.Root,.015f,.055f,.43f,.10f,ViewFactory.HudSurface,"Item hint card");
            lower.GetComponent<Image>().raycastTarget=false;
            itemName=view.Text(lower,"",0,0,1,1,21);itemName.fontStyle=FontStyle.Bold;
            instruction=view.Text(lower,"",0,0,1,1,20,ViewFactory.HudAccent);
            lessonText=view.Text(lower,"",0,0,1,1,18,ViewFactory.HudMuted);
            view.FitContent(lower,.43f,280,itemName,instruction,lessonText);
            Footer("Đúng +10 • Sai −5 • Qua màn: "+level.requiredCorrect+"/"+level.itemCount+" • ESC: tạm dừng");
            SpawnBins(new Vector3(0, 0, -1.8f));
            round.Changed += RefreshRound; RefreshRound(); NextItem();
        }
        void RefreshRound()
        {
            scoreText.text = SelectedLearningMode == LearningMode.Test && !round.Finished ? "Ẩn điểm" : "ĐIỂM " + round.Score;
            correctText.text = (SelectedLearningMode == LearningMode.Test && !round.Finished ? "ẨN ĐÁP ÁN" : "ĐÚNG " + round.Correct + "/" + round.Total) + "   •   ĐÃ CHỌN " + round.Answered + "/" + round.Total;
            if(roundProgressFill!=null)roundProgressFill.anchorMax=new Vector2((float)round.Answered/round.Total,1);
        }
        void NextItem()
        {
            if (round.Finished) return;
            currentItem = world.CatalogItem(roundItems[round.Answered], new Vector3(-2.8f, .89f, 1.1f));
            AreaJourneyStore.Discover(progress.player_id,currentItem.Definition.Id);
            itemName.text = "VẬT PHẨM " + (round.Answered + 1) + "/" + round.Total + (SelectedLearningMode == LearningMode.Test ? " • Quan sát mô hình" : "  •  " + currentItem.Definition.Name);
            lessonText.text = Hints ? "Gợi ý: " + WasteInfo.Label(currentItem.type) : SelectedLearningMode == LearningMode.Test ? "Kiểm tra: giải thích hiển thị cuối lượt." : "Quan sát chất liệu. Giải thích xuất hiện sau khi chọn.";
            instruction.text = "Kéo vật phẩm và thả vào thùng bạn chọn.";
            instruction.color = ViewFactory.HudAccent;
            resolving = false;
        }
        void OnWorldSelect(Vector2 position)
        {
            if (explorer != null || settingsRoot != null || paused || resolving || resultShown || arComplete) return;
            Ray ray = sceneCamera.ScreenPointToRay(position);
            if (screen == ClientScreen.ARChallenge && !arPlaced)
            {
                if (placement.TryPlace(position, sceneCamera, out Vector3 point)) PlaceChallenge(point);
                else instruction.text = deviceARActive ?
                    "Chưa thấy mặt phẳng. Di chuyển điện thoại chậm quanh sàn hoặc mặt bàn, rồi chạm lại." :
                    "Chưa trúng mặt sàn mô phỏng. Hãy chạm lại trong khu vực giữa.";
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
            if (dragPlane.Raycast(ray, out float distance))
            {
                currentItem.transform.position = ray.GetPoint(distance) + dragOffset;
                // Orthographic depth does not change pointer alignment or apparent size.
                // Bring the held model in front of the bins so it remains visible.
                if(sceneCamera.orthographic)currentItem.transform.position-=sceneCamera.transform.forward*5f;
            }
            var target=FindDropBin(position);
            if(target!=hoveredBin)
            {
                if(hoveredBin!=null)hoveredBin.Highlight(false);
                hoveredBin=target;if(hoveredBin!=null)hoveredBin.Highlight(true);
            }
            instruction.text=target!=null?"Thả vào "+WasteInfo.Label(target.type):"Kéo tới thùng bạn chọn • Viền vàng chỉ vùng thả.";
        }
        void CancelDrag()
        {
            if(hoveredBin!=null)hoveredBin.Highlight(false);hoveredBin=null;
            if (dragging && currentItem != null) { currentItem.transform.position = dragOrigin; currentItem.Select(false); }
            dragging = false;
        }
        void DropItem(Vector2 position, bool overUI)
        {
            if (!dragging) return;
            CancelDrag();
            if (overUI || paused || resolving || resultShown || arComplete || currentItem == null) return;
            // Ignore the dragged item's colliders when finding the bin beneath the pointer.
            WasteBin bin = FindDropBin(position);
            if (bin == null) { instruction.text = "Thả vào một thùng rác. Vật phẩm đã trở về vị trí ban đầu."; return; }
            gameAudio?.Drop();
            if (screen == ClientScreen.Game) StartCoroutine(SortItem(bin.type));
            else CompleteChallenge(bin.type);
        }
        WasteBin FindDropBin(Vector2 position)
        {
            int count=Physics.RaycastNonAlloc(sceneCamera.ScreenPointToRay(position),dropHits,100);
            // Rare overflow uses the complete query so it cannot silently choose a hidden bin.
            var hits=count==dropHits.Length?Physics.RaycastAll(sceneCamera.ScreenPointToRay(position),100):dropHits;
            if(hits!=dropHits)count=hits.Length;
            float nearest=float.MaxValue;WasteBin result=null;
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];
                if(hit.collider.GetComponentInParent<WasteItem>()==currentItem)continue;
                if(hit.distance>=nearest)continue;
                nearest=hit.distance;result=hit.collider.GetComponentInParent<WasteBin>();
            }
            return result;
        }
        IEnumerator SortItem(WasteType bin)
        {
            if (round.Finished) yield break;
            resolving = true; bool correct = round.Submit(currentItem.type, bin);
            learningAnswers.Add(new LearningAnswer { item_id = currentItem.Definition.Id, expected = currentItem.type.ToString(), choice = bin.ToString(), correct = correct, seconds = answerSeconds });
            answerSeconds = 0;
            if (SelectedLearningMode != LearningMode.Test) { gameAudio?.FeedbackAfterDrop(correct); StartCoroutine(PulseBin(bin)); instruction.color = correct ? ViewFactory.HudAccent : new Color(1f,.65f,.48f); }
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
            var overlay = view.Card(view.Root, .22f, .23f, .56f, .54f, ViewFactory.Cream, "Round result");
            view.Text(overlay, explorer != null ? (areaMission!=null?"HOÀN THÀNH NHIỆM VỤ":"KHU VỰC ĐÃ HỒI SINH") : SelectedLearningMode != LearningMode.Campaign ? "KẾT QUẢ " + LearningLabel : round.Passed ? "HOÀN THÀNH CẤP ĐỘ " + SelectedLevel : "THỬ LẠI NHÉ!", .07f, .72f, .86f, .18f, 40);
            view.Text(overlay, explorer != null ? (areaMission!=null?CompleteAreaMission():RestorationSummary()) : "Điểm: " + round.Score + "   •   Đúng: " + round.Correct + "/" + round.Total +
                "\n" + (SelectedLearningMode != LearningMode.Campaign ? "Đã lưu lịch sử. Xem giải thích hoặc ôn lại câu sai." : round.Passed ? (SelectedLevel == 3 ? "Bạn đã hoàn thành cả ba cấp độ." : "Level tiếp theo đã mở khóa.") : "Cần ít nhất " + round.Required + " đáp án đúng. Các câu chưa trả lời không tính đúng."), .07f, .39f, .86f, .29f, 26);
            view.Button(overlay, "Chơi lại", .07f, .12f, .25f, .15f, () => Navigate("GameScene"));
            view.Button(overlay, areaMission!=null?"Hành trình":"Menu", .37f, .12f, .25f, .15f, ()=>{openJourneyOnMenu=areaMission!=null;Home();});
            if(explorer!=null)
            {
                view.Button(overlay,"Ngắm thành quả",.67f,.12f,.26f,.15f,()=>{overlay.gameObject.SetActive(false);viewingRestoration=true;},true);
                view.Button(view.Root,"Xem kết quả",.80f,.79f,.18f,.055f,()=>{overlay.gameObject.SetActive(true);viewingRestoration=false;});
            }
            if (round.Passed && SelectedLearningMode == LearningMode.Campaign) view.Button(overlay, SelectedLevel < 3 ? "Level tiếp →" : "Phân loại bằng ảnh", .67f, .12f, .26f, .15f, () =>
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
            if(preparationPanel!=null){ClosePreparation();return;}
            if(missionBrief!=null)return;
            if (screen != ClientScreen.Game || round == null || resultShown || (round.Finished && explorer == null)) return;
            CancelDrag();
            paused = !paused; Time.timeScale = paused ? 0 : 1;
            if (!paused) { if (pausePanel != null) Destroy(pausePanel.gameObject); return; }
            pausePanel=view.Panel(view.Root,0,0,1,1,new Color(.025f,.055f,.09f,.48f),"Paused");
            var dialog=view.Card(pausePanel,.355f,.335f,.29f,.33f,ViewFactory.Cream,"Pause dialog");
            view.Text(dialog,"TẠM DỪNG",.08f,.73f,.84f,.15f,30,null,TextAnchor.MiddleCenter);
            view.Text(dialog,"Thời gian và băng chuyền đã dừng",.07f,.59f,.86f,.10f,18,ViewFactory.Muted,TextAnchor.MiddleCenter);
            view.Button(dialog,"Tiếp tục / ESC",.08f,.32f,.84f,.18f,TogglePause,true);
            view.Button(dialog,"Về menu",.08f,.10f,.84f,.16f,Home);
        }

        void ShowProgress()
        {
            view.Header("KẾT QUẢ HỌC TẬP",Mode,Home);
            Footer("Thành tích luôn được lưu trên máy.");
            ShowLearningStatsPage(3);
        }
        void BuildAchievements(RectTransform panel)
        {
            var summary=view.Text(panel,"",.04f,.39f,.92f,.34f,25);
            Action refresh=()=>summary.text="THÀNH TÍCH CỦA BẠN\n\nCấp độ đã mở: "+progress.unlocked_level+"/3 • Điểm cao nhất: "+progress.best_score+
                "\nThử thách mô hình: "+progress.ar_completed+" lần hoàn thành"+
                "\nHuy hiệu: "+(progress.HasBadge?"Người bạn môi trường":"Hoàn thành 3 cấp độ và một thử thách mô hình để nhận");
            refresh();
            view.Button(panel,"Khôi phục tiến trình",.04f,.27f,.44f,.09f,()=>
            {
                if(busy)return;
                if(service.IsMock){SetStatus("Tiến trình đang được lưu trên máy này. Bật kết nối trong Cài đặt để khôi phục bản lưu trực tuyến.");return;}
                StartCoroutine(WithBusy(service.GetProgress(progress.player_id,data=>{progress.Merge(data);LocalProgress.Save(progress);refresh();SetStatus("Đã khôi phục tiến trình.");},SetStatus)));
            });
            view.Text(panel,"Nút Lưu kết quả trực tuyến bên dưới lưu cả lịch sử học và tiến trình.",.51f,.25f,.44f,.12f,21,ViewFactory.Muted);
        }
        IEnumerator WithBusy(IEnumerator operation)
        { busy = true; SetStatus("Đang xử lý…"); yield return operation; busy = false; }
    }
}

