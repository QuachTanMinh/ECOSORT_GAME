using UnityEngine;
using UnityEngine.UI;

namespace EcoSortQuest
{
    public sealed partial class EcoSortApp
    {
        void ShowMissionBrief()
        {
            if(areaMission!=null){ShowAreaBrief();return;}
            missionBrief=view.Panel(view.Root,.18f,.20f,.64f,.57f,ViewFactory.Cream,"Restoration briefing");
            view.Text(missionBrief,"PHỤC HỒI "+ContextLabel,.06f,.80f,.88f,.13f,32);
            var previous=RestorationSave.Load(progress.player_id,SelectedContext);
            view.Text(missionBrief,"Một khu vực sạch bắt đầu từ những quyết định nhỏ.\n\n1. Nhặt và dọn 4 vật phẩm để mở lại lối đi.\n2. Dọn 8 vật phẩm để hồi sinh vườn hoa.\n3. Dọn đủ 12 vật phẩm để hoàn thiện góc xanh.\n\nR: xem tình trạng và chọn cách chuẩn bị. E: nhặt / bỏ thùng.\nChọn sai thùng được sửa lại; điểm chỉ tính lần đầu.",.06f,.23f,.88f,.55f,23);
            view.Text(missionBrief,"Kỷ lục khu vực: "+previous.bestStars+"/3 sao • "+previous.completions+" lần hoàn thành",.06f,.12f,.88f,.08f,20,ViewFactory.Accent);
            view.Button(missionBrief,"Nhận nhiệm vụ",.06f,.015f,.55f,.10f,AcceptRestorationMission,true);
            view.Button(missionBrief,"Về menu",.65f,.015f,.29f,.10f,Home);
        }
        void AcceptRestorationMission()
        {
            if(missionBrief==null || areaMission!=null && areaMission.Question.Length>0 && !quizPassed)return;
            Destroy(missionBrief.gameObject);missionBrief=null;
            RefreshExplorationHud();
        }
        void ShowAreaBrief()
        {
            missionBrief=view.Panel(view.Root,.15f,.16f,.70f,.66f,ViewFactory.Cream,"Area mission briefing");
            view.Text(missionBrief,AreaMissions.Label(areaMission.Context)+" / "+areaMission.Title,.05f,.84f,.90f,.12f,30);
            view.Text(missionBrief,areaMission.Brief+"\n\n1 sao: dọn đủ; 2 sao: đúng ≥75%, gợi ý ≤2.\n3 sao: đúng ≥90%, không sai chuẩn bị/câu hỏi, không gợi ý.\nKhông tính tốc độ. Sai được sửa; hoàn thành sẽ mở nhiệm vụ tiếp.",.05f,.48f,.90f,.34f,22);
            if(areaMission.Question.Length>0)
            {
                hintText=view.Text(missionBrief,areaMission.Question,.05f,.34f,.90f,.12f,22,ViewFactory.Accent);
                for(int i=0;i<areaMission.Options.Length;i++)
                {
                    int option=i;
                    view.Button(missionBrief,areaMission.Options[i],.05f+i*.305f,.19f,.29f,.13f,()=>AnswerAreaQuestion(option));
                }
            }
            else view.Text(missionBrief,"WASD: di chuyển • E: nhặt / bỏ thùng • R: chuẩn bị\nH: hỏi gợi ý vật đang cầm. Thao tác quan sát R không tính gợi ý.",.05f,.20f,.90f,.20f,22);
            areaStartButton=view.Button(missionBrief,"Bắt đầu nhiệm vụ",.05f,.035f,.58f,.10f,AcceptRestorationMission,true);
            areaStartButton.interactable=areaMission.Question.Length==0 || quizPassed;
            view.Button(missionBrief,"Về menu",.66f,.035f,.29f,.10f,Home);
        }
        void AnswerAreaQuestion(int choice)
        {
            if(areaMission==null || missionBrief==null || quizPassed || choice<0 || choice>=areaMission.Options.Length)return;
            if(choice!=areaMission.Answer){quizMistakes++;hintText.text="Cùng suy nghĩ lại: "+areaMission.Explanation;return;}
            quizPassed=true;areaStartButton.interactable=true;hintText.text="ĐÚNG • "+areaMission.Explanation+" Bấm Bắt đầu nhiệm vụ.";
        }
        void ShowPreparation()
        {
            if(carried==null || paused || resultShown || settingsRoot!=null || missionBrief!=null || preparationPanel!=null)return;
            preparationPanel=view.Panel(view.Root,.19f,.17f,.62f,.62f,ViewFactory.Cream,"Preparation decision");
            view.Text(preparationPanel,"QUAN SÁT • "+carried.Definition.Name,.055f,.82f,.89f,.12f,29);
            view.Text(preparationPanel,(carried==wrappedCup && !sleeveSeparated
                ? "Tình huống: cốc nhựa còn phần dư đang nằm trong túi giấy sạch. Tách túi giấy trước, rồi làm rỗng cốc. Hai phần cần được thu gom riêng."
                : PreparationRules.Condition(carried.Definition.Id))+"\n"+PreparationRules.Clue(carried.type),.055f,.57f,.89f,.24f,22);
            preparationFeedback=view.Text(preparationPanel,preparedLitter.Contains(carried)?"Đã chuẩn bị xong. Bạn có thể đóng và đến thùng.":"Chọn thao tác phù hợp với tình trạng được mô tả.",.055f,.43f,.89f,.13f,21,ViewFactory.Accent);
            for(int i=0;i<4;i++)
            {
                var action=(PreparationAction)i;
                var button=view.Button(preparationPanel,PreparationRules.Label(action),.055f,.34f-i*.078f,.89f,.067f,()=>ChoosePreparation(action));
                button.interactable=!preparedLitter.Contains(carried);
            }
            view.Button(preparationPanel,"Đóng • tiếp tục quan sát",.055f,.015f,.89f,.08f,ClosePreparation);
        }
        void ChoosePreparation(PreparationAction action)
        {
            if(carried==null || preparationPanel==null || preparedLitter.Contains(carried) || paused || settingsRoot!=null)return;
            if(carried==wrappedCup && !sleeveSeparated)
            {
                if(action!=PreparationAction.SeparateSleeve)
                {restoration.WrongPreparation();preparationFeedback.text="Hãy tách túi giấy và cốc trước. Chưa thể bỏ cả hai vật liệu vào một thùng.";return;}
                sleeveSeparated=true;sleeveVisual.SetActive(false);
                paperSleeve.transform.position=new Vector3(explorer.transform.position.x,.04f,explorer.transform.position.z);
                paperSleeve.gameObject.SetActive(true);
                ClosePreparation();ShowPreparation();
                preparationFeedback.text="Đã tách túi giấy và đặt cạnh chân bạn. Tiếp theo hãy làm rỗng cốc; nhớ quay lại nhặt phần giấy.";
                return;
            }
            if(!restoration.Prepare(carried.Definition.Id,action))
            {
                preparationFeedback.text="CHƯA PHÙ HỢP • "+PreparationRules.Condition(carried.Definition.Id);
                preparationFeedback.color=new Color(.65f,.25f,.12f);
                return;
            }
            preparedLitter.Add(carried);
            explorationPrompt.text="ĐÃ CHUẨN BỊ • "+PreparationRules.Label(action)+". Hãy chọn thùng theo vật liệu chính.";
            feedbackUntil=Time.unscaledTime+4;
            ClosePreparation();
            RefreshExplorationHud();
        }
        void ClosePreparation()
        {
            if(preparationPanel!=null)Destroy(preparationPanel.gameObject);
            preparationPanel=null;
        }
        string RestorationSummary()
        {
            int stars=restoration.Stars(round.Correct);
            var record=RestorationSave.Complete(progress.player_id,SelectedContext,restorationSession,stars);
            return "PHỤC HỒI HOÀN TẤT • "+stars+"/3 SAO\n"+
                "Đã dọn 12/12 • Đúng lần đầu: "+round.Correct+"/12 • Điểm: "+round.Score+
                "\nChọn cách chuẩn bị chưa đúng: "+restoration.PreparationMistakes+
                " lần • Kỷ lục: "+record.bestStars+"/3 sao\n"+
                "1 sao: dọn đủ • 2 sao: đúng ≥9 • 3 sao: đúng 12 và chuẩn bị không sai.";
        }
    }
}
