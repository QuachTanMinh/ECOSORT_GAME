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
        public readonly ExtraEnvironment Environment;
        public readonly string[] Items, Options;
        public readonly int Answer;
        public readonly bool Separate;
        public AreaMission(string id, LearningContext context, string title, string zone, string brief, string reward,
            string items, bool separate=false, string question="", string options="", int answer=0, string explanation="", ExtraEnvironment environment=ExtraEnvironment.None)
        { Id=id;Context=context;Title=title;Zone=zone;Brief=brief;Reward=reward;Items=items.Split(',');Separate=separate;
          Question=question;Options=options.Length==0?Array.Empty<string>():options.Split('|');Answer=answer;Explanation=explanation;Environment=environment; }
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
            new AreaMission("park-restore",LearningContext.Park,"03 • Trả lại góc xanh","VƯỜN CỘNG ĐỒNG","Hoàn thành tuyến làm sạch 12 vật phẩm. Kết hợp quan sát, xử lý và phân loại để phục hồi khu vườn.","Ghế công viên • Huy hiệu Người giữ góc xanh","water-bottle,paper-box,drink-can,plastic-cup,paper-bag,banana-peel,newspaper,foil-ball,plastic-spoon,fallen-leaf,orange-peel,bottle-crown",true),

            new AreaMission("hospital-waiting",LearningContext.Home,"01 • Phòng chờ sạch","BỆNH VIỆN / PHÒNG CHỜ","Thu gom rác sinh hoạt quanh ghế chờ. Chỉ xử lý vật sạch, không chạm vào vật sắc nhọn hoặc rác y tế.","Chậu cây sảnh","water-bottle,worksheet,banana-peel,drink-can,plastic-cap,paper-box",false,"", "",0,"",ExtraEnvironment.Hospital),
            new AreaMission("hospital-reception",LearningContext.Home,"02 • Quầy tiếp đón gọn","BỆNH VIỆN / QUẦY TIẾP ĐÓN","Quan sát khu xếp hàng và chuẩn bị vật chứa trước khi phân loại.","Bảng chỉ dẫn xanh","plastic-cup,paper-bag,food-can,notebook,yogurt-pot,orange-peel,foil-ball,water-bottle,envelope,apple-core",true,"", "",0,"",ExtraEnvironment.Hospital),
            new AreaMission("hospital-lobby",LearningContext.Home,"03 • Sảnh thân thiện","BỆNH VIỆN / SẢNH CHÍNH","Hoàn thành tuyến 18 vật phẩm ở sảnh. Đi chậm, quan sát kỹ và đưa rác sinh hoạt về trạm phân loại.","Góc cây phục hồi","water-bottle,drink-can,paper-box,banana-peel,notebook,plastic-cap,food-container,newspaper,foil-ball,plastic-spoon,orange-peel,apple-core,yogurt-pot,paper-bag,plastic-cup,aluminium-tray,fallen-leaf,bottle-crown",true,"", "",0,"",ExtraEnvironment.Hospital),

            new AreaMission("market-aisle",LearningContext.Home,"01 • Lối đi mua sắm","SIÊU THỊ / LỐI ĐI","Nhặt những vật phẩm rơi trên lối đi chung, giữ khoảng trống cho mọi người di chuyển.","Giỏ tái sử dụng","water-bottle,food-can,envelope,banana-peel,plastic-spoon,paper-box",false,"", "",0,"",ExtraEnvironment.Supermarket),
            new AreaMission("market-checkout",LearningContext.Home,"02 • Quầy thanh toán gọn","SIÊU THỊ / QUẦY THANH TOÁN","Làm rỗng hộp, giữ giấy khô và tách cốc khỏi túi giấy trước khi thu gom.","Kệ đổi quà xanh","plastic-cup,paper-bag,yogurt-pot,bread-scrap,drink-can,notebook,foil-ball,water-bottle,cardboard,orange-peel",true,"", "",0,"",ExtraEnvironment.Supermarket),
            new AreaMission("market-floor",LearningContext.Home,"03 • Siêu thị xanh","SIÊU THỊ / KHU MUA SẮM","Hoàn thành 18 vật phẩm rải dọc các lối đi; không đặt vật vào kệ hàng hay quầy thanh toán.","Khu phân loại thân thiện","water-bottle,drink-can,paper-box,banana-peel,plastic-cup,paper-bag,food-container,newspaper,foil-ball,plastic-spoon,orange-peel,apple-core,yogurt-pot,cardboard,envelope,aluminium-tray,fallen-leaf,bottle-crown",true,"", "",0,"",ExtraEnvironment.Supermarket),

            new AreaMission("beach-shore",LearningContext.Park,"01 • Mép nước sạch","BÃI BIỂN / MÉP NƯỚC","Thu gom các vật phẩm trên cát, tránh làm ảnh hưởng đến khu nghỉ của mọi người.","Cờ nhắc giữ sạch","water-bottle,drink-can,flyer,banana-peel,plastic-cap,fallen-leaf",false,"", "",0,"",ExtraEnvironment.Beach),
            new AreaMission("beach-rest",LearningContext.Park,"02 • Khu nghỉ gọn gàng","BÃI BIỂN / KHU NGHỈ","Tách túi giấy khỏi cốc và làm rỗng vật chứa trước khi đưa về trạm phân loại.","Chậu hoa ven biển","plastic-cup,paper-bag,food-container,watermelon-rind,aluminium-tray,apple-core,paper-box,plastic-spoon,bottle-crown,orange-peel",true,"", "",0,"",ExtraEnvironment.Beach),
            new AreaMission("beach-cleanup",LearningContext.Park,"03 • Bãi biển xanh","BÃI BIỂN / TUYẾN LÀM SẠCH","Đi dọc bãi cát rộng và hoàn thành 18 vật phẩm để trả lại không gian sạch cho mọi người.","Biển cảm ơn người giữ biển xanh","water-bottle,paper-box,drink-can,plastic-cup,paper-bag,banana-peel,newspaper,foil-ball,plastic-spoon,fallen-leaf,orange-peel,bottle-crown,food-container,watermelon-rind,plant-pot,flyer,apple-core,aluminium-tray",true,"", "",0,"",ExtraEnvironment.Beach)
        };
        public static AreaMission Find(string id)=>All.FirstOrDefault(m=>m.Id==id);
        public static AreaMission[] ForEnvironment(ExtraEnvironment environment)=>All.Where(m=>m.Environment==environment).ToArray();
        public static AreaMission[] MainJourney=>All.Where(m=>m.Environment==ExtraEnvironment.None).ToArray();
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
        {
            var route=mission.Environment==ExtraEnvironment.None?AreaMissions.MainJourney:AreaMissions.ForEnvironment(mission.Environment);
            int index=Array.IndexOf(route,mission);return index==0 || index>0 && Completed(data,route[index-1].Id);
        }
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
        public static int Decorations(AreaJourney data,LearningContext context)=>AreaMissions.MainJourney.Count(m=>m.Context==context && Completed(data,m.Id));
    }
}
