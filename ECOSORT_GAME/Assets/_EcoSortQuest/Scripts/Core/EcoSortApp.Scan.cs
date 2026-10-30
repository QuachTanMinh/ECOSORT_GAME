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
        Text predictionText, mockTypeText, recognitionGuidance;
        Text scanPlaceholder;
        WasteType mockType;
        byte[] jpeg;
        Prediction scanResult;
        bool cameraStarting;
        int cameraIndex;

        string labChoice;
        Text labChoiceText;
        bool labAttempted;
        AiMissionRecord labRecord;
        UnityEngine.UI.Button[] labChoices;
        void ShowScan()
        {
            view.Header("PHÂN LOẠI BẰNG ẢNH", Mode, Home);
            var panel=view.Panel(view.Root,.035f,.09f,.93f,.78f,ViewFactory.Cream);
            view.Text(panel,"01  ẢNH VẬT THẬT    →    02  TỰ DỰ ĐOÁN    →    03  ĐỐI CHIẾU    →    04  XÁC NHẬN",.035f,.91f,.93f,.06f,24);
            var frame=view.Panel(panel,.035f,.31f,.40f,.56f,new Color(.80f,.87f,.83f));
            scanPlaceholder=view.Text(frame,"WEBCAM / ẢNH PNG, JPG\nMột vật chính, đủ sáng, nền đơn giản.",.08f,.15f,.84f,.70f,24,ViewFactory.Muted,TextAnchor.MiddleCenter);
            var rect=view.Rect("Preview",frame,0,0,1,1);
            scanPreview=rect.gameObject.AddComponent<RawImage>();scanPreview.raycastTarget=false;scanPreview.enabled=false;
            var aspect=rect.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=4f/3;
            view.Button(panel,"Mở / chụp lại",.035f,.21f,.13f,.075f,()=>{if(!busy&&!cameraStarting)StartCoroutine(OpenCamera());});
            view.Button(panel,"Đổi camera",.172f,.21f,.13f,.075f,ChangeCamera);
            view.Button(panel,"Chụp ảnh",.31f,.21f,.125f,.075f,CaptureCamera);
            imagePath=view.Input(panel,"Đường dẫn tuyệt đối JPG / PNG…",.035f,.115f,.29f,.075f);
            view.Button(panel,"Nạp ảnh",.335f,.115f,.10f,.075f,LoadImage);
            view.Button(panel,"Sổ tay 12 nhóm",.035f,.025f,.195f,.065f,()=>ShowRecognitionGuide());
            view.Button(panel,"Lưu ảnh kiểm tra",.24f,.025f,.195f,.065f,SaveReviewPhoto);
            labChoiceText=view.Text(panel,"TỰ DỰ ĐOÁN TRƯỚC KHI GỬI ẢNH",.48f,.81f,.48f,.06f,24);
            labChoices=new Button[RecognitionInfo.All.Count+1];
            for(int i=0;i<RecognitionInfo.All.Count;i++)
            {
                int index=i;var entry=RecognitionInfo.All[i];
                labChoices[i]=view.Button(panel,entry.display_name,.48f+(i%4)*.12f,.73f-(i/4)*.075f,.112f,.062f,()=>ChooseLabCategory(index));
            }
            labChoices[labChoices.Length-1]=view.Button(panel,"Chưa biết / cần kiểm tra",.48f,.495f,.48f,.062f,()=>ChooseLabCategory(-1));
            predictionText=view.Text(panel,"Chưa có kết quả. Hãy tự dự đoán trước.",.48f,.355f,.48f,.125f,25);
            recognitionGuidance=view.Text(panel,"AI có thể sai. Độ tin cậy không bảo đảm đúng với ảnh lạ. Kiểm tra vật liệu và hướng dẫn nơi thu gom.",.48f,.205f,.48f,.14f,22,ViewFactory.Muted);
            mockTypeText=view.Text(panel,service.IsMock?"Ví dụ: Nhựa • Không phân tích ảnh":"Đáp án bài học lấy từ danh mục kiến thức.",.48f,.15f,.48f,.045f,18,ViewFactory.Muted);
            if(service.IsMock)view.Button(panel,"Đổi ví dụ",.48f,.095f,.19f,.048f,()=>{if(busy)return;mockType=(WasteType)(((int)mockType+1)%4);mockTypeText.text="Ví dụ: "+WasteInfo.Label(mockType)+" • Không phân tích ảnh";ResetLabInput();});
            view.Button(panel,service.IsMock?"Xem kết quả ví dụ":"Phân tích ảnh",.68f,.095f,.28f,.048f,RunPrediction,true);
            challengeButton=view.Button(panel,"Xác nhận kiến thức →",.48f,.025f,.48f,.055f,ShowLabConfirmation,true);
            challengeButton.interactable=false;
            Footer(service.IsMock?"Bạn đang xem ví dụ có sẵn. Bật kết nối trong Cài đặt để nhận diện ảnh của bạn.":"Ảnh được gửi đến dịch vụ nhận diện đã chọn. Kết quả là gợi ý để bạn đối chiếu.");
        }
        void ChooseLabCategory(int index)
        {
            if(busy||cameraStarting)return;
            labChoice=index<0?"Unknown":RecognitionInfo.All[index].class_name;
            ResetLabInput(false);
            labChoiceText.text="Bạn dự đoán: "+(index<0?"Chưa biết":RecognitionInfo.All[index].display_name);
            for(int i=0;i<labChoices.Length;i++)
                labChoices[i].GetComponent<Image>().color=(i==(index<0?labChoices.Length-1:index))?new Color(.58f,.83f,.69f):new Color(.88f,.93f,.88f);
        }
        void ResetLabInput(bool clearChoice=true)
        {
            ClearPrediction();labAttempted=false;labRecord=null;
            if(clearChoice)
            {
                labChoice=null;
                if(labChoiceText!=null)labChoiceText.text="TỰ DỰ ĐOÁN TRƯỚC KHI GỬI ẢNH";
                if(labChoices!=null)foreach(var button in labChoices)button.GetComponent<Image>().color=new Color(.88f,.93f,.88f);
            }
        }
        void ClearPrediction()
        {
            scanResult = null; PendingPrediction = null;
            if (challengeButton != null) challengeButton.interactable = false;
            if (predictionText != null) predictionText.text = "Chưa có kết quả cho đầu vào hiện tại.";
            if (recognitionGuidance != null) recognitionGuidance.text = "Nạp/chụp một ảnh mới và kiểm tra vật liệu thật trước khi phân loại.";
        }
        IEnumerator OpenCamera()
        {
            cameraStarting = true; ResetLabInput(); jpeg = null; StopCamera();
            scanPreview.enabled=false;scanPlaceholder.gameObject.SetActive(true);
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            { SetStatus("Không có quyền webcam. Có thể nạp ảnh từ file."); cameraStarting = false; yield break; }
            if (WebCamTexture.devices.Length == 0)
            { SetStatus("Không tìm thấy webcam. Có thể nạp ảnh JPG/PNG."); cameraStarting = false; yield break; }
            cameraIndex%=WebCamTexture.devices.Length;
            webcam = new WebCamTexture(WebCamTexture.devices[cameraIndex].name, 640, 480, 15);
            webcam.Play();
            float deadline = Time.realtimeSinceStartup + 8;
            while (webcam != null && webcam.width <= 16 && Time.realtimeSinceStartup < deadline) yield return null;
            cameraStarting = false;
            if (webcam == null || webcam.width <= 16)
            { StopCamera(); SetStatus("Webcam không sẵn sàng. Kiểm tra quyền camera hoặc nạp ảnh."); yield break; }
            scanPreview.texture = webcam; scanPreview.enabled = true;
            scanPlaceholder.gameObject.SetActive(false);
            scanPreview.GetComponent<AspectRatioFitter>().aspectRatio = webcam.videoRotationAngle % 180 == 0 ?
                (float)webcam.width / webcam.height : (float)webcam.height / webcam.width;
            SetStatus("Camera "+(cameraIndex+1)+"/"+WebCamTexture.devices.Length+" sẵn sàng. Đưa một vật vào khung, giữ yên rồi chụp.");
        }
        void ChangeCamera()
        {
            if(busy||cameraStarting)return;
            var devices=WebCamTexture.devices;
            if(devices.Length==0){SetStatus("Không tìm thấy webcam. Có thể nạp ảnh từ file.");return;}
            cameraIndex=(cameraIndex+1)%devices.Length;StartCoroutine(OpenCamera());
        }
        void SaveReviewPhoto()
        {
            if(busy||cameraStarting)return;
            if(jpeg==null || imageTexture==null){SetStatus("Chụp hoặc nạp ảnh trước. Chỉ lưu ảnh khi bạn bấm nút này.");return;}
            try
            {
                string directory=Path.Combine(Application.persistentDataPath,"PhotoReview");Directory.CreateDirectory(directory);
                string name="photo-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,6)+".jpg";
                File.WriteAllBytes(Path.Combine(directory,name),jpeg);
                string manifest=Path.Combine(directory,"manifest.csv");
                if(!File.Exists(manifest))File.WriteAllText(manifest,"path,label,object_id,condition\n",System.Text.Encoding.UTF8);
                File.AppendAllText(manifest,name+",,,\n",System.Text.Encoding.UTF8);
                SetStatus("Đã lưu ảnh tại "+directory+". Người kiểm tra điền vật liệu thật, mã vật và điều kiện trong manifest.csv; không lấy nhãn AI làm đáp án.");
            }
            catch(Exception e){SetStatus("Chưa lưu được ảnh: "+e.Message);}
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
            ResetLabInput();
            if (imageTexture != null) Destroy(imageTexture);
            imageTexture = ImageCapture.Resize(texture, 1024); Destroy(texture);
            jpeg = imageTexture.EncodeToJPG(85);
            scanPreview.texture = imageTexture; scanPreview.enabled = true;
            scanPlaceholder.gameObject.SetActive(false);
            scanPreview.uvRect = new Rect(0, 0, 1, 1); scanPreview.rectTransform.localEulerAngles = Vector3.zero;
            scanPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)imageTexture.width / imageTexture.height;
            SetStatus(PhotoQuality.Advice(imageTexture.GetPixels32(),imageTexture.width,imageTexture.height));
        }
        void RunPrediction()
        {
            if (busy || cameraStarting) return;
            if (string.IsNullOrEmpty(labChoice)) { SetStatus("Tự dự đoán một nhóm hoặc chọn Chưa biết trước khi gửi ảnh."); return; }
            ClearPrediction(); labAttempted=false; labRecord=null;
            if (!service.IsMock && (jpeg == null || jpeg.Length == 0))
            { SetStatus("Chụp hoặc nạp ảnh trước khi phân tích."); return; }
            StartCoroutine(WithBusy(service.Predict(jpeg, mockType, ApplyPrediction,
                e => { predictionText.text = "Nhận diện thất bại. Có thể chụp lại và thử lại."; SetStatus(e); })));
        }
        void ApplyPrediction(Prediction result)
        {
            // Every response resets the previous usable result, including refused/unknown labels.
            ClearPrediction();
            labAttempted=true; RecordLabObservation(result);
            challengeButton.interactable=true;
            float threshold = apiSettings != null ? apiSettings.minimumConfidence : .75f;
            if (result == null || !result.TryRecognize(threshold, out var category))
            {
                predictionText.text = "AI chưa đủ chắc chắn / nhãn không hợp lệ." + (result != null && !float.IsNaN(result.confidence) && !float.IsInfinity(result.confidence) && result.confidence>=0 && result.confidence<=1 ? "\nĐộ tin cậy: " + result.confidence.ToString("P0") : "");
                recognitionGuidance.text = "Chụp lại một vật rõ, đủ sáng, nền đơn giản; tự xem ký hiệu và vật liệu. Vẫn có thể làm câu xác nhận kiến thức.";
                SetStatus("Chưa kết luận vật liệu. Chụp lại hoặc đối chiếu sổ tay."); return;
            }
            scanResult = result;
            predictionText.text = category.display_name + "\nĐộ tin cậy: " + result.confidence.ToString("P0") +
                (service.IsMock ? " • VÍ DỤ CÓ SẴN, KHÔNG PHÂN TÍCH ẢNH" : " • AI");
            RecognitionInfo.TryGet(labChoice,out var ownCategory);
            recognitionGuidance.text = category.guidance + "\nBạn chọn: " + (ownCategory!=null?ownCategory.display_name:"Chưa biết") + ". " +
                (string.Equals(labChoice,result.class_name,StringComparison.OrdinalIgnoreCase)?"Đồng thuận chưa chứng minh đúng.":"Khác dự đoán: hãy kiểm tra lại vật liệu.") +
                " Độ tin cậy không phải xác suất đúng đã kiểm chứng.";
            SetStatus("Đã lưu quan sát riêng. Hoàn thành câu xác nhận từ kiến thức, không từ nhãn AI.");
        }

        void ShowRecognitionGuide(int selected = 0)
        {
            if (busy) return;
            var overlay = view.Panel(view.Root, .025f, .085f, .95f, .80f, ViewFactory.Cream, "AI recognition guide");
            view.Text(overlay, "12 NHÓM NHẬN DIỆN • CÁCH THU GOM", .03f, .86f, .94f, .10f, 30);
            for (int i = 0; i < RecognitionInfo.All.Count; i++)
            {
                int next = i; var entry = RecognitionInfo.All[i];
                view.Button(overlay, entry.display_name, .03f + (i % 3) * .19f, .68f - (i / 3) * .15f, .18f, .12f,
                    () => { Destroy(overlay.gameObject); ShowRecognitionGuide(next); }, selected == i);
            }
            var current = RecognitionInfo.All[selected];
            view.Text(overlay, current.display_name, .63f, .67f, .33f, .15f, 29);
            view.Text(overlay, current.guidance + "\n\n" + (current.game_supported ? "Có bài phân loại trong game." :
                "Nhóm mở rộng: hướng dẫn riêng, chưa có thử thách 3D."), .63f, .23f, .33f, .43f, 25, ViewFactory.Muted);
            view.Text(overlay, "AI không xác định mọi loại rác. Hãy kiểm tra vật liệu và quy định nơi thu gom.", .03f, .06f, .72f, .09f, 21);
            view.Button(overlay, "Đóng", .79f, .06f, .17f, .085f, () => Destroy(overlay.gameObject));
        }
        void ShowAR()
        {
            view.Header(deviceARActive ? "THỬ THÁCH TRONG KHÔNG GIAN THẬT" : "THỬ THÁCH MÔ HÌNH", Mode, Home);
            var info = view.Panel(view.Root, .035f, .72f, .93f, .13f, ViewFactory.Cream);
            bool fixture = PendingPrediction == null;
            if (fixture) PendingPrediction = new Prediction { class_name = "Plastic", confidence = .92f, accepted = true };
            if (!PendingPrediction.TryAccept(apiSettings != null ? apiSettings.minimumConfidence : .75f, out WasteType type))
            {
                PendingPrediction = null;
                view.Text(info, "Chưa có kết quả phù hợp. Hãy quay lại phần phân loại bằng ảnh.", .03f, .1f, .94f, .8f, 26);
                Footer("Không thể mở nhiệm vụ."); arComplete = true; return;
            }
            view.Text(info, "NHIỆM VỤ MÔ HÌNH: " + WasteInfo.Label(type) + "\n" + (fixture ? "Dữ liệu mẫu để chạy scene trực tiếp." : "Phân loại mô hình có nhãn bài học; không chấm ảnh thực tế."), .025f, .15f, .75f, .7f, 26);
            var centerButton = view.Button(info, "Đặt ở giữa", .80f, .22f, .17f, .56f, () => { if (!arPlaced && !deviceARActive) PlaceChallenge(Vector3.zero); });
            centerButton.interactable = !deviceARActive;
            var lower = view.Panel(view.Root, 0, .065f, 1, .12f, ViewFactory.Cream);
            instruction = view.Text(lower, deviceARActive ? "Di chuyển điện thoại để dò mặt phẳng, rồi chạm để đặt thử thách." : "Chạm để đặt lên mặt phẳng mô phỏng, hoặc chọn Đặt ở giữa.", .035f, .1f, .93f, .8f, 26, ViewFactory.Accent, TextAnchor.MiddleCenter);
            Footer(deviceARActive ? "Di chuyển điện thoại để tìm mặt phẳng, rồi chạm để đặt mô hình." : "Bạn đang luyện với mô hình trên máy tính.");
        }
        void PlaceChallenge(Vector3 position)
        {
            if (arPlaced || arComplete) return;
            // Keep the task readable and bins inside the island regardless of the clicked point.
            if (!deviceARActive)
            { position.x = Mathf.Clamp(position.x, -1.2f, 1.2f); position.z = Mathf.Clamp(position.z, -1.3f, .6f); }
            SpawnBins(position + new Vector3(0, 0, -1));
            PendingPrediction.TryAccept(0, out WasteType type);
            currentItem = SpawnItem(type, position + new Vector3(0, .15f, 1.5f));
            arPlaced = true; instruction.text = deviceARActive ? "Đã đặt thử thách lên mặt phẳng. Chạm giữ vật phẩm, kéo đến thùng đúng rồi thả." : "Đã đặt thùng mô phỏng. Kéo rác và thả vào thùng đúng.";
        }
        void CompleteChallenge(WasteType choice)
        {
            if (arComplete || currentItem == null) return;
            gameAudio?.FeedbackAfterDrop(choice == currentItem.type);
            if (choice != currentItem.type)
            { instruction.text = "Chưa đúng. " + WasteInfo.Tip(currentItem.type) + " Hãy thử lại; không cộng điểm."; return; }
            arComplete = true; currentItem.Select(false);
            progress.ar_completed++; LocalProgress.Save(progress);
            instruction.text = deviceARActive ? "HOÀN THÀNH! +20 điểm thử thách." : "HOÀN THÀNH MÔ HÌNH! +20 điểm.";
            var panel = view.Panel(view.Root, .26f, .30f, .48f, .32f, ViewFactory.Cream);
            view.Text(panel, "+20 / THỬ THÁCH HOÀN THÀNH", .05f, .59f, .9f, .30f, 31, ViewFactory.Accent, TextAnchor.MiddleCenter);
            view.Text(panel, progress.HasBadge ? "Huy hiệu: Người bạn môi trường" : "Hoàn thành cả 3 cấp độ để nhận huy hiệu.", .06f, .37f, .88f, .19f, 22, null, TextAnchor.MiddleCenter);
            view.Button(panel, "Phân loại ảnh khác", .08f, .08f, .39f, .20f, () => { if (busy) return; PendingPrediction = null; Navigate("ScanScene"); });
            view.Button(panel, "Kết quả học tập", .53f, .08f, .39f, .20f, () => Navigate("ProgressScene"), true);
            StartCoroutine(WithBusy(SaveChallenge(choice)));
        }
        IEnumerator SaveChallenge(WasteType choice)
        {
            yield return service.PostArResult(new ArResultRequest { player_id = progress.player_id,
                predicted_class = PendingPrediction.class_name, confidence = PendingPrediction.confidence,
                player_choice = choice.ToString(), is_correct = true },
                () => SetStatus("Đã lưu kết quả thử thách • " + Mode), e => SetStatus("Kết quả lưu cục bộ, chưa lưu trực tuyến: " + e));
            yield return service.PutProgress(progress, () => { }, e => SetStatus("Tiến trình lưu cục bộ; đồng bộ thất bại: " + e));
        }
    }
}

