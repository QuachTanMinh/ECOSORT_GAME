using System;
using UnityEngine;

namespace EcoSortQuest
{
    public sealed partial class EcoSortApp
    {
        RectTransform labConfirmation;
        RectTransform CreateLabPanel(string title)
        {
            labConfirmation=view.Panel(view.Root,0,0,1,1,new Color(.02f,.09f,.08f,.62f),"Lab modal backdrop");
            return view.Panel(labConfirmation,.12f,.17f,.76f,.66f,ViewFactory.Cream,title);
        }
        void RecordLabObservation(Prediction result)
        {
            float threshold=apiSettings!=null?apiSettings.minimumConfidence:.75f;
            bool accepted=result!=null && result.TryRecognize(threshold,out _);
            int lessonIndex=0;
            string previous=learning.missions.Count>0?learning.missions[learning.missions.Count-1].knowledge_item_id:null;
            for(int i=0;i<WasteCatalog.All.Count;i++)if(WasteCatalog.All[i].Id==previous)lessonIndex=(i+1)%WasteCatalog.All.Count;
            labRecord=new AiMissionRecord { created_at=DateTime.UtcNow.ToString("O"),target="Observation",
                knowledge_item_id=WasteCatalog.All[lessonIndex].Id,
                choice=labChoice,predicted=result!=null?result.class_name:"Unknown",
                confidence=result!=null && !float.IsNaN(result.confidence) && !float.IsInfinity(result.confidence)?result.confidence:0,
                accepted=accepted,mock=service.IsMock,
                agrees=accepted && string.Equals(labChoice,result.class_name,StringComparison.OrdinalIgnoreCase) };
            learning.missions.Add(labRecord);
            if(learning.missions.Count>200)learning.missions.RemoveAt(0);
            LearningStore.Save(progress.player_id,learning);
        }
        void ShowLabConfirmation()
        {
            if(busy||!labAttempted||labRecord==null||labConfirmation!=null)return;
            var panel=CreateLabPanel("Lab confirmation");
            view.Text(panel,"XÁC NHẬN 1/2 • HIỂU GIỚI HẠN AI",.06f,.80f,.88f,.12f,30);
            view.Text(panel,"AI báo độ tin cậy cao. Bạn nên làm gì trước khi phân loại vật thật?",.06f,.59f,.88f,.17f,27);
            var feedback=view.Text(panel,"Câu hỏi này có đáp án cố định từ nội dung bài học.",.06f,.17f,.88f,.14f,23,ViewFactory.Muted);
            view.Button(panel,"Tự kiểm tra vật liệu và quy định nơi thu gom",.06f,.43f,.88f,.11f,()=>ShowLabMaterialQuestion(labConfirmation),true);
            view.Button(panel,"Luôn tin AI vì độ tin cậy cao",.06f,.29f,.88f,.11f,()=>feedback.text="Chưa đúng. AI có thể tự tin nhưng vẫn sai, nhất là ảnh khác dữ liệu huấn luyện.");
            view.Button(panel,"Đối chiếu thêm / chụp lại",.26f,.04f,.48f,.09f,()=>{Destroy(labConfirmation.gameObject);labConfirmation=null;});
        }
        void ShowLabMaterialQuestion(RectTransform previous)
        {
            Destroy(previous.gameObject);
            var panel=CreateLabPanel("Verified material question");
            // Independent catalog item: never use AI output as a ground-truth label.
            var item=WasteCatalog.All[0];
            foreach(var definition in WasteCatalog.All)if(definition.Id==labRecord.knowledge_item_id){item=definition;break;}
            view.Text(panel,"XÁC NHẬN 2/2 • KIẾN THỨC VẬT LIỆU",.06f,.80f,.88f,.12f,30);
            view.Text(panel,"Trong bài học: "+item.Name+" thuộc nhóm nào?\nĐây là vật phẩm mẫu, không chấm ảnh bạn vừa tải.",.06f,.57f,.88f,.19f,27);
            var feedback=view.Text(panel,labRecord.knowledge_confirmed?"ĐÃ HOÀN THÀNH • "+item.Lesson:"Chọn vật liệu dựa trên kiến thức của bạn.",.06f,.22f,.88f,.16f,23,ViewFactory.Muted);
            for(int i=0;i<4;i++)
            {
                var choice=(WasteType)i;
                view.Button(panel,WasteInfo.Label(choice),.06f+i*.225f,.43f,.21f,.10f,()=>
                {
                    if(labRecord.knowledge_confirmed)return;
                    if(choice!=item.Type){feedback.text="Cần ôn: "+item.Lesson;return;}
                    labRecord.knowledge_confirmed=true;LearningStore.Save(progress.player_id,learning);
                    PlayFeedback(true);feedback.text="HOÀN THÀNH • "+item.Lesson;
                });
            }
            view.Button(panel,"Đối chiếu ảnh",.06f,.055f,.27f,.10f,()=>{Destroy(labConfirmation.gameObject);labConfirmation=null;});
            view.Button(panel,"Luyện 3D mẫu",.36f,.055f,.27f,.10f,()=>
            {
                if(!labRecord.knowledge_confirmed){feedback.text="Hoàn thành câu xác nhận trước khi luyện 3D.";return;}
                // A known model from the authored lesson, not a label for the uploaded photo.
                PendingPrediction=new Prediction{class_name=item.Type.ToString(),confidence=1,accepted=true};
                Navigate("ARChallengeScene");
            });
            view.Button(panel,"Thí nghiệm mới →",.66f,.055f,.28f,.10f,()=>
            {
                if(!labRecord.knowledge_confirmed){feedback.text="Hoàn thành câu xác nhận trước khi bắt đầu bài mới.";return;}
                Destroy(labConfirmation.gameObject);labConfirmation=null;ResetLabInput();jpeg=null;
                StopCamera();scanPreview.enabled=false;
                scanPlaceholder.gameObject.SetActive(true);
                if(imageTexture!=null){Destroy(imageTexture);imageTexture=null;}
                imagePath.text="";SetStatus("Chụp hoặc nạp ảnh mới, rồi tự dự đoán trước.");
            },true);
        }
    }
}
