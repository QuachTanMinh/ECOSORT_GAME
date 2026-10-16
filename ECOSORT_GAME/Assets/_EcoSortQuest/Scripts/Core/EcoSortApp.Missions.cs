using System;
using UnityEngine;

namespace EcoSortQuest
{
    public sealed partial class EcoSortApp
    {
        int missionStep;
        WasteType? missionChoice;
        bool missionPassed;
        UnityEngine.UI.Text missionLabel;
        void ChooseMissionMaterial(WasteType type)
        {
            if(busy || missionPassed)return;
            missionChoice=type; ClearPrediction();
            if(missionLabel!=null)missionLabel.text="Bạn chọn: "+WasteInfo.Label(type)+" • Sau đó gửi ảnh để đối chiếu AI.";
        }
        void RecordMission(Prediction result)
        {
            if(!AiMission || !missionChoice.HasValue)return;
            bool accepted=result.TryAccept(apiSettings.minimumConfidence,out var predicted);
            var target=(WasteType)missionStep;
            missionPassed=accepted && predicted==target && missionChoice.Value==target;
            learning.missions.Add(new AiMissionRecord{created_at=DateTime.UtcNow.ToString("O"),target=target.ToString(),
                choice=missionChoice.Value.ToString(),predicted=result.class_name,confidence=result.confidence,
                accepted=accepted,mock=service.IsMock,agrees=missionPassed});
            if(learning.missions.Count>200)learning.missions.RemoveAt(0);
            LearningStore.Save(progress.player_id,learning);
            if(missionLabel!=null)missionLabel.text=(missionPassed?"ĐỒNG THUẬN • ":"THỬ ẢNH KHÁC • ")+"Mục tiêu "+WasteInfo.Label(target)+"\nAI chỉ là tham chiếu; người học kiểm tra vật liệu thật.";
            challengeButton.interactable=missionPassed;
            if(missionPassed)PlayFeedback(true);
        }
        void AdvanceMission()
        {
            if(busy || !missionPassed)return;
            if(missionStep==3)
            {
                challengeButton.interactable=false;
                predictionText.text="HOÀN THÀNH 4 NHIỆM VỤ\n"+(service.IsMock?"Luyện tập mock; không tính là AI thật.":"Bốn lần đối chiếu AI, không phải đánh giá accuracy.");
                SetStatus("Kết quả đã lưu trên máy. Về Học tập → Kết quả học tập để xem.");return;
            }
            missionStep++;missionChoice=null;missionPassed=false;jpeg=null;ClearPrediction();
            if(scanPreview!=null)scanPreview.enabled=false;
            missionLabel.text="Mục tiêu "+(missionStep+1)+"/4: "+WasteInfo.Label((WasteType)missionStep)+"\nNạp/chụp một ảnh mới rồi tự chọn vật liệu.";
            SetStatus("Nhiệm vụ tiếp theo cần ảnh mới. Không tái sử dụng kết quả dự đoán trước.");
        }
    }
}
