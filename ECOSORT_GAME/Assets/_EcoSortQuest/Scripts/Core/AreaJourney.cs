using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EcoSortQuest
{
    public sealed class AreaMission
    {
        public readonly string Id, Title, Zone, Brief, Reward, Question, Explanation;
        public readonly LearningContext Context;
        public readonly string[] Items, Options;
        public readonly int Answer;
        public readonly bool Separate;
        public AreaMission(string id, LearningContext context, string title, string zone, string brief, string reward,
            string items, bool separate=false, string question="", string options="", int answer=0, string explanation="")
        { Id=id;Context=context;Title=title;Zone=zone;Brief=brief;Reward=reward;Items=items.Split(',');Separate=separate;
          Question=question;Options=options.Length==0?Array.Empty<string>():options.Split('|');Answer=answer;Explanation=explanation; }
    }
    public static class AreaMissions
    {
        public static readonly AreaMission[] All={
            new AreaMission("home-kitchen",LearningContext.Home,"01 • Sau bữa sáng","GÓC BẾP","Bốn vật phẩm sau bữa sáng. Làm rỗng chai và lon; tách thực phẩm khỏi vật liệu khô. Không giới hạn thời gian.","Chậu cây ban công","water-bottle,drink-can,paper-box,banana-peel"),
            new AreaMission("home-study",LearningContext.Home,"02 • Góc học tập gọn gàng","GÓC HỌC TẬP","Giữ giấy khô, làm phẳng thùng carton. Nhận biết giấy nhôm dù tên có chữ giấy.","Luống hoa nhỏ","notebook,cardboard,plastic-cap,foil-ball"),
            new AreaMission("home-separate",LearningContext.Home,"03 • Hai vật liệu, hai nơi","GÓC THU GOM","Cốc nhựa còn đồ uống nằm trong túi giấy sạch. Tách hai phần, làm rỗng cốc và thu gom riêng từng vật phẩm.","Ghế nghỉ gia đình • Mở Trường học","plastic-cup,paper-bag,food-can,apple-core",true),
            new AreaMission("school-class",LearningContext.School,"01 • Đội trực nhật","LỚP HỌC","Trả lời câu hỏi rồi dọn vật phẩm quanh góc lớp. Chú ý vật có tên dễ gây nhầm.","Chậu cây lớp học","worksheet,notebook,foil-ball,plastic-cap",false,"Giấy nhôm sạch thuộc nhóm nào?","Giấy|Kim loại|Hữu cơ",1,"Tên có chữ giấy nhưng vật liệu chính là nhôm, thuộc Kim loại."),
            new AreaMission("school-canteen",LearningContext.School,"02 • Sau giờ ăn","CĂNG TIN","Làm rỗng hộp và cốc, thu gom phần thức ăn riêng. Tách túi giấy khỏi cốc trước khi phân loại.","Vườn hoa sân trường","plastic-cup,paper-bag,yogurt-pot,bread-scrap",true,"Hộp nhựa còn thức ăn cần làm gì trước?","Bỏ nguyên vào Giấy|Lấy hết thức ăn ra|Bỏ cả hộp vào Hữu cơ",1,"Lấy hết thức ăn, sau đó thu gom hộp theo vật liệu nhựa."),
            new AreaMission("school-yard",LearningContext.School,"03 • Sân trường xanh","SÂN TRƯỜNG","Tìm tám vật phẩm quanh sân; giữ sạch khu sinh hoạt chung và vận dụng những gì đã học.","Ghế đọc sách • Mở Công viên","water-bottle,drink-can,flyer,fallen-leaf,plastic-spoon,paper-box,orange-peel,aluminium-tray",false,"Cách giảm rác sau giờ ăn phù hợp nhất?","Dùng đồ tái sử dụng khi phù hợp|Đổi sang nhiều đồ một lần|Để rác lại sân",0,"Ưu tiên giảm phát sinh và tái sử dụng trước khi cần phân loại rác."),
            new AreaMission("park-path",LearningContext.Park,"01 • Lối đi an toàn","LỐI ĐI","Khám phá lối đi, tìm tám vật phẩm và đưa về trạm thu gom. Có thể hỏi gợi ý khi cần.","Cây xanh lối đi","water-bottle,drink-can,newspaper,banana-peel,plastic-cap,bottle-crown,flyer,fallen-leaf"),
            new AreaMission("park-picnic",LearningContext.Park,"02 • Sau buổi dã ngoại","BÃI DÃ NGOẠI","Tìm vật phẩm rải quanh bãi cỏ. Tách túi giấy khỏi cốc và chuẩn bị hộp nhựa trước khi thu gom.","Hoa bãi dã ngoại","plastic-cup,paper-bag,food-container,watermelon-rind,aluminium-tray,apple-core,paper-box,plastic-spoon",true),
            new AreaMission("park-restore",LearningContext.Park,"03 • Trả lại góc xanh","VƯỜN CỘNG ĐỒNG","Hoàn thành tuyến làm sạch 12 vật phẩm. Kết hợp quan sát, xử lý và phân loại để phục hồi khu vườn.","Ghế công viên • Huy hiệu Người giữ góc xanh","water-bottle,paper-box,drink-can,plastic-cup,paper-bag,banana-peel,newspaper,foil-ball,plastic-spoon,fallen-leaf,orange-peel,bottle-crown",true)
        };
        public static AreaMission Find(string id)=>All.FirstOrDefault(m=>m.Id==id);
        public static string Label(LearningContext context)=>context==LearningContext.Home?"NHÀ Ở":context==LearningContext.School?"TRƯỜNG HỌC":"CÔNG VIÊN";
    }
    [Serializable] public sealed class AreaMissionRecord
    { public string id; public int bestStars, completions, bestCorrect, total; }
    [Serializable] public sealed class AreaJourney
    {
        public List<AreaMissionRecord> missions=new List<AreaMissionRecord>();
        public List<string> sessions=new List<string>(), discovered=new List<string>(), materials=new List<string>(), badges=new List<string>();
        public bool plants=true, flowers=true, benches=true;
    }
    public static class AreaJourneyStore
    {
        public static string Key(string player)=>"EcoSortQuest.AreaJourney.v1."+player;
        public static AreaJourney Load(string player)
        {
            AreaJourney data=null;
            try {data=JsonUtility.FromJson<AreaJourney>(PlayerPrefs.GetString(Key(player),""));}catch(ArgumentException){}
            data=data??new AreaJourney();data.missions=data.missions??new List<AreaMissionRecord>();
            data.sessions=data.sessions??new List<string>();data.discovered=data.discovered??new List<string>();
            data.materials=data.materials??new List<string>();data.badges=data.badges??new List<string>();return data;
        }
        public static void Save(string player,AreaJourney data)
        {PlayerPrefs.SetString(Key(player),JsonUtility.ToJson(data));PlayerPrefs.Save();}
        public static bool Completed(AreaJourney data,string id)=>data.missions.Any(r=>r.id==id && r.bestStars>0);
        public static bool Unlocked(AreaJourney data,AreaMission mission)
        {int index=Array.IndexOf(AreaMissions.All,mission);return index==0 || index>0 && Completed(data,AreaMissions.All[index-1].Id);}
        public static int Stars(int total,int cleaned,int correct,int mistakes,int hints)
        {
            if(total<=0 || cleaned!=total || correct<0 || correct>total || mistakes<0 || hints<0)return 0;
            float accuracy=(float)correct/total;
            return accuracy>=.9f && mistakes==0 && hints==0?3:accuracy>=.75f && hints<=2?2:1;
        }
        public static void Discover(string player,string id)
        {var d=Load(player);if(WasteCatalog.All.Any(w=>w.Id==id) && !d.discovered.Contains(id)){d.discovered.Add(id);Save(player,d);}}
        public static AreaMissionRecord Complete(string player,AreaMission mission,string session,int cleaned,int correct,int mistakes,int hints,IEnumerable<string> materials)
        {
            var data=Load(player);int stars=Stars(mission.Items.Length,cleaned,correct,mistakes,hints);
            if(stars==0 || !Unlocked(data,mission))return null;
            var record=data.missions.Find(r=>r.id==mission.Id);
            if(data.sessions.Contains(session))return record;
            data.sessions.Add(session);
            if(record==null){record=new AreaMissionRecord{id=mission.Id,total=mission.Items.Length};data.missions.Add(record);}
            if(record.completions>0 && (stars>record.bestStars || correct>record.bestCorrect))Award(data,"Tiến bộ từng ngày");
            record.completions++;record.bestStars=Math.Max(record.bestStars,stars);record.bestCorrect=Math.Max(record.bestCorrect,correct);
            foreach(var material in materials)if(Enum.TryParse<WasteType>(material,out var type) && Enum.IsDefined(typeof(WasteType),type) && !data.materials.Contains(material))data.materials.Add(material);
            if(data.materials.Count==4)Award(data,"Nhận biết bốn vật liệu");
            if(Completed(data,"park-restore"))Award(data,"Người giữ góc xanh");
            Save(player,data);return record;
        }
        static void Award(AreaJourney data,string badge){if(!data.badges.Contains(badge))data.badges.Add(badge);}
        public static void RecordReview(string player,LearningSession session)
        {
            if(session.total<=0 || session.mode!="Review" || session.expired || session.answers==null || session.answers.Length!=session.total || !session.answers.All(a=>a.correct))return;
            var data=Load(player);Award(data,"Chăm chỉ ôn tập");Save(player,data);
        }
        public static int Decorations(AreaJourney data,LearningContext context)=>AreaMissions.All.Count(m=>m.Context==context && Completed(data,m.Id));
    }
}
