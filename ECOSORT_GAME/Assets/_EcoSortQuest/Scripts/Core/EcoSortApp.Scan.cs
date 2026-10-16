using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace EcoSortQuest
{
    public sealed partial class EcoSortApp
    {
        WebCamTexture webcam;
        Texture2D imageTexture;
        RawImage scanPreview;
        InputField imagePath;
        Button challengeButton;
        Text predictionText, mockTypeText;
        WasteType mockType;
        byte[] jpeg;
        Prediction scanResult;
        bool cameraStarting;

        void ShowScan()
        {
            view.Header(AiMission ? "NHIỆM VỤ AI / 4 VẬT LIỆU" : "REAL-WORLD SCAN", Mode, Home);
            var panel = view.Panel(view.Root, .035f, .10f, .93f, .72f, ViewFactory.Cream);
            view.Text(panel, AiMission ? "TÌM VẬT THẬT • TỰ CHỌN • ĐỐI CHIẾU AI" : "MỘT VẬT PHẨM. MỘT LỰA CHỌN TỐT.", .035f, .86f, .93f, .10f, 32);
            var previewFrame = view.Panel(panel, .035f, .30f, .44f, .52f, new Color(.80f, .87f, .83f));
            view.Text(previewFrame, "WEBCAM / ẢNH PNG, JPG\n\nĐặt một vật phẩm chính trong ảnh.\nChụp rõ, đủ sáng, nền đơn giản.", .08f, .15f, .84f, .70f, 24, ViewFactory.Muted, TextAnchor.MiddleCenter);
            var previewRect = view.Rect("Preview", previewFrame, 0, 0, 1, 1);
            scanPreview = previewRect.gameObject.AddComponent<RawImage>(); scanPreview.raycastTarget = false; scanPreview.enabled = false;
            var aspect = previewRect.gameObject.AddComponent<AspectRatioFitter>(); aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 4f / 3;
            view.Button(panel, "Mở webcam", .035f, .19f, .21f, .085f, () => { if (!busy && !cameraStarting) StartCoroutine(OpenCamera()); });
            view.Button(panel, "Chụp ảnh", .265f, .19f, .21f, .085f, CaptureCamera);
            imagePath = view.Input(panel, "Dán đường dẫn tuyệt đối tới ảnh JPG/PNG…", .035f, .075f, .33f, .08f);
            view.Button(panel, "Nạp ảnh", .38f, .075f, .095f, .08f, LoadImage);
            view.Text(panel, "NHẬN DIỆN VẬT PHẨM", .53f, .73f, .43f, .09f, 28);
            predictionText = view.Text(panel, service.IsMock ? "CHẾ ĐỘ MOCK\nKết quả giả lập do bạn chọn.\nKhông thực hiện nhận diện ảnh." : "Chưa có kết quả.\nChụp hoặc nạp ảnh rồi gửi tới API.", .53f, .49f, .43f, .22f, 25);
            mockTypeText = view.Text(panel, "Mock label: Plastic", .53f, .40f, .43f, .07f, 21, ViewFactory.Muted);
            mockTypeText.gameObject.SetActive(service.IsMock || AiMission);
            if (AiMission) { missionLabel = mockTypeText; missionLabel.text = "Mục tiêu 1/4: Nhựa • Chọn vật liệu trước khi gửi ảnh."; }
            if (service.IsMock || AiMission)
            {
                for (int i = 0; i < 4; i++)
                {
                    var type = (WasteType)i;
                    view.Button(panel, type.ToString(), .53f + i * .11f, .30f, .10f, .07f, () =>
                    {
                        if (busy) return;
                        if (AiMission) { ChooseMissionMaterial(type); if (service.IsMock) mockType = type; return; }
                        mockType = type; mockTypeText.text = "Mock label: " + type;
                        ClearPrediction();
                    });
                }
            }
            view.Button(panel, service.IsMock ? "Chạy dự đoán MOCK" : "Gửi ảnh đến AI", .53f, .19f, .43f, .085f, RunPrediction, true);
            challengeButton = view.Button(panel, AiMission ? "Nhiệm vụ tiếp / hoàn thành →" : "Mở thử thách AR mô phỏng →", .53f, .075f, .43f, .085f, () =>
            { if (busy) return; if (AiMission) { AdvanceMission(); return; } PendingPrediction = scanResult; StopCamera(); Navigate("ARChallengeScene"); });
            challengeButton.interactable = false;
            Footer(service.IsMock ? "Mock phục vụ tích hợp UI. Không phải Deep Learning thật. Có thể thử mà không cần webcam." :
                "Ảnh gửi đến FastAPI; client không lưu ảnh trên đĩa. Chưa xác minh backend thật.");
        }
        void ClearPrediction()
        {
            missionPassed = false;
            scanResult = null; PendingPrediction = null;
            if (challengeButton != null) challengeButton.interactable = false;
            if (predictionText != null) predictionText.text = "Chưa có kết quả cho đầu vào hiện tại.";
        }
        IEnumerator OpenCamera()
        {
            cameraStarting = true; ClearPrediction(); jpeg = null; StopCamera();
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            { SetStatus("Không có quyền webcam. Có thể nạp ảnh từ file."); cameraStarting = false; yield break; }
            if (WebCamTexture.devices.Length == 0)
            { SetStatus("Không tìm thấy webcam. Có thể nạp ảnh JPG/PNG."); cameraStarting = false; yield break; }
            webcam = new WebCamTexture(WebCamTexture.devices[0].name, 640, 480, 15);
            webcam.Play();
            float deadline = Time.realtimeSinceStartup + 8;
            while (webcam != null && webcam.width <= 16 && Time.realtimeSinceStartup < deadline) yield return null;
            cameraStarting = false;
            if (webcam == null || webcam.width <= 16)
            { StopCamera(); SetStatus("Webcam không sẵn sàng. Kiểm tra quyền camera hoặc nạp ảnh."); yield break; }
            scanPreview.texture = webcam; scanPreview.enabled = true;
            scanPreview.GetComponent<AspectRatioFitter>().aspectRatio = webcam.videoRotationAngle % 180 == 0 ?
                (float)webcam.width / webcam.height : (float)webcam.height / webcam.width;
            SetStatus("Webcam sẵn sàng. Nhấn Chụp ảnh để giữ khung hình.");
        }
        void StopCamera()
        {
            if (webcam == null) return;
            webcam.Stop(); Destroy(webcam); webcam = null;
        }
        void CaptureCamera()
        {
            if (busy || cameraStarting) return;
            if (webcam == null || !webcam.isPlaying || webcam.width <= 16)
            { SetStatus("Mở webcam trước khi chụp."); return; }
            var texture = new Texture2D(webcam.width, webcam.height, TextureFormat.RGB24, false);
            texture.SetPixels32(webcam.GetPixels32()); texture.Apply();
            var corrected = ImageCapture.Orient(texture, webcam.videoRotationAngle, webcam.videoVerticallyMirrored);
            Destroy(texture); StopCamera(); SetImage(corrected);
        }
        void LoadImage()
        {
            if (busy || cameraStarting) return;
            string path = imagePath.text.Trim().Trim('"');
            Texture2D texture = null;
            try
            {
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (!Path.IsPathRooted(path) || (extension != ".png" && extension != ".jpg" && extension != ".jpeg"))
                { SetStatus("Cần đường dẫn tuyệt đối tới ảnh PNG/JPG/JPEG."); return; }
                if (!File.Exists(path) || new FileInfo(path).Length > 16 * 1024 * 1024)
                { SetStatus("File không tồn tại hoặc lớn hơn 16 MB."); return; }
                texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path)))
                { Destroy(texture); SetStatus("Không đọc được nội dung ảnh."); return; }
                StopCamera(); SetImage(texture);
            }
            catch (Exception e) { if (texture != null) Destroy(texture); SetStatus("Không thể nạp ảnh: " + e.Message); }
        }
        void SetImage(Texture2D texture)
        {
            ClearPrediction();
            if (imageTexture != null) Destroy(imageTexture);
            imageTexture = ImageCapture.Resize(texture, 1024); Destroy(texture);
            jpeg = imageTexture.EncodeToJPG(85);
            scanPreview.texture = imageTexture; scanPreview.enabled = true;
            scanPreview.uvRect = new Rect(0, 0, 1, 1); scanPreview.rectTransform.localEulerAngles = Vector3.zero;
            scanPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)imageTexture.width / imageTexture.height;
            SetStatus("Ảnh sẵn sàng: " + imageTexture.width + "×" + imageTexture.height + " • " + jpeg.Length / 1024 + " KB.");
        }
        void RunPrediction()
        {
            if (busy || cameraStarting) return;
            if (AiMission && !missionChoice.HasValue) { SetStatus("Tự chọn vật liệu ở ba nút trước khi gửi ảnh."); return; }
            ClearPrediction();
            if (!service.IsMock && (jpeg == null || jpeg.Length == 0))
            { SetStatus("Chụp hoặc nạp ảnh trước khi gọi AI thật."); return; }
            StartCoroutine(WithBusy(service.Predict(jpeg, mockType, result =>
            {
                float threshold = apiSettings != null ? apiSettings.minimumConfidence : .75f;
                if (AiMission) RecordMission(result);
                if (!result.TryAccept(threshold, out WasteType type))
                { predictionText.text = "Chưa đủ tin cậy hoặc nhãn không hợp lệ.\nHãy chụp lại vật phẩm rõ hơn."; SetStatus("Không mở thử thách cho kết quả bị từ chối."); return; }
                scanResult = result;
                predictionText.text = WasteInfo.Label(type) + "\nConfidence: " + result.confidence.ToString("P0") +
                    (service.IsMock ? "\nKẾT QUẢ MOCK, KHÔNG PHẢI AI THẬT" : "\nKết quả do backend trả về");
                challengeButton.interactable = !AiMission || missionPassed; SetStatus(AiMission ? "Đối chiếu AI đã lưu. Nếu đồng thuận mục tiêu, chọn Nhiệm vụ tiếp." : "Sẵn sàng mở thử thách mô phỏng. Nhận diện chưa cộng điểm.");
            }, e => { predictionText.text = "Nhận diện thất bại. Có thể chụp lại và thử lại."; SetStatus(e); })));
        }
        void ShowAR()
        {
            view.Header("AR CHALLENGE / PC SIMULATION", Mode, Home);
            var info = view.Panel(view.Root, .035f, .72f, .93f, .13f, ViewFactory.Cream);
            bool fixture = PendingPrediction == null;
            if (fixture) PendingPrediction = new Prediction { class_name = "Plastic", confidence = .92f, accepted = true };
            if (!PendingPrediction.TryAccept(apiSettings != null ? apiSettings.minimumConfidence : .75f, out WasteType type))
            {
                PendingPrediction = null;
                view.Text(info, "Kết quả AI không hợp lệ. Hãy quay lại ScanScene.", .03f, .1f, .94f, .8f, 26);
                Footer("Không thể mở nhiệm vụ."); arComplete = true; return;
            }
            view.Text(info, "NHIỆM VỤ: " + WasteInfo.Label(type) + "\n" + (fixture ? "Dữ liệu mẫu để chạy scene trực tiếp." : "Vật phẩm được chọn theo kết quả từ ScanScene."), .025f, .15f, .75f, .7f, 26);
            view.Button(info, "Đặt ở giữa", .80f, .22f, .17f, .56f, () => { if (!arPlaced) PlaceChallenge(Vector3.zero); });
            var lower = view.Panel(view.Root, 0, .065f, 1, .12f, ViewFactory.Cream);
            instruction = view.Text(lower, "Chạm mặt sàn để đặt ba thùng, hoặc nhấn Đặt ở giữa.", .035f, .1f, .93f, .8f, 26, ViewFactory.Accent, TextAnchor.MiddleCenter);
            Footer("MÔ PHỎNG 3D TRÊN PC • Không có plane tracking, camera AR hoặc bằng chứng AR trên thiết bị.");
        }
        void PlaceChallenge(Vector3 position)
        {
            if (arPlaced || arComplete) return;
            // Keep the task readable and bins inside the island regardless of the clicked point.
            position.x = Mathf.Clamp(position.x, -1.2f, 1.2f); position.z = Mathf.Clamp(position.z, -1.3f, .6f);
            SpawnBins(position + new Vector3(0, 0, -1));
            PendingPrediction.TryAccept(0, out WasteType type);
            currentItem = SpawnItem(type, position + new Vector3(0, .15f, 1.5f));
            arPlaced = true; instruction.text = "Đã đặt thùng mô phỏng. Kéo rác và thả vào thùng đúng.";
        }
        void CompleteChallenge(WasteType choice)
        {
            if (arComplete || currentItem == null) return;
            gameAudio?.FeedbackAfterDrop(choice == currentItem.type);
            if (choice != currentItem.type)
            { instruction.text = "Chưa đúng. " + WasteInfo.Tip(currentItem.type) + " Hãy thử lại; không cộng điểm."; return; }
            arComplete = true; currentItem.Select(false);
            progress.ar_completed++; LocalProgress.Save(progress);
            instruction.text = "HOÀN THÀNH! +20 điểm nhiệm vụ AR mô phỏng.";
            var panel = view.Panel(view.Root, .26f, .30f, .48f, .32f, ViewFactory.Cream);
            view.Text(panel, "+20 / THỬ THÁCH HOÀN THÀNH", .05f, .59f, .9f, .30f, 31, ViewFactory.Accent, TextAnchor.MiddleCenter);
            view.Text(panel, progress.HasBadge ? "Huy hiệu: EcoSort Beginner" : "Hoàn thành cả 3 level để nhận EcoSort Beginner.", .06f, .37f, .88f, .19f, 22, null, TextAnchor.MiddleCenter);
            view.Button(panel, "Scan mới", .08f, .08f, .39f, .20f, () => { if (busy) return; PendingPrediction = null; Navigate("ScanScene"); });
            view.Button(panel, "Tiến trình", .53f, .08f, .39f, .20f, () => Navigate("ProgressScene"), true);
            StartCoroutine(WithBusy(SaveChallenge(choice)));
        }
        IEnumerator SaveChallenge(WasteType choice)
        {
            yield return service.PostArResult(new ArResultRequest { player_id = progress.player_id,
                predicted_class = PendingPrediction.class_name, confidence = PendingPrediction.confidence,
                player_choice = choice.ToString(), is_correct = true },
                () => SetStatus("Đã gửi kết quả thử thách • " + Mode), e => SetStatus("Kết quả lưu cục bộ, gửi AR thất bại: " + e));
            yield return service.PutProgress(progress, () => { }, e => SetStatus("Tiến trình lưu cục bộ; đồng bộ thất bại: " + e));
        }
    }
}

