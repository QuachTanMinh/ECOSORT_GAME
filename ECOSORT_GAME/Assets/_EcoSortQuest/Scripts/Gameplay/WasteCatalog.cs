using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoSortQuest
{
    public sealed class WasteDefinition
    {
        public readonly string Id, Name, Lesson;
        public readonly WasteType Type;
        public readonly int Model;
        public readonly LearningContext[] Contexts;
        public bool AppearsIn(LearningContext context) => Array.IndexOf(Contexts, context) >= 0;
        public WasteDefinition(string id, string name, WasteType type, int model, string lesson, params LearningContext[] contexts)
        { Id = id; Name = name; Type = type; Model = model; Lesson = lesson; Contexts = contexts; }
    }

    // Only clean, single-material examples: acceptance still depends on local collection rules.
    public static class WasteCatalog
    {
        static readonly WasteDefinition[] entries = {
            new WasteDefinition("water-bottle", "Chai nước nhựa", WasteType.Plastic, 0, "Đổ hết nước và để ráo trước khi thu gom. Chai được phân loại theo thân nhựa.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("detergent-bottle", "Chai nước giặt", WasteType.Plastic, 1, "Chai nhựa cứng: làm rỗng và kiểm tra hướng dẫn thu gom tại địa phương.", LearningContext.Home),
            new WasteDefinition("plastic-cup", "Cốc nhựa", WasteType.Plastic, 2, "Cốc nhựa sạch thuộc nhóm Nhựa trong bài học. Không cho cốc còn đồ uống vào giấy.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("food-container", "Hộp nhựa đựng đồ", WasteType.Plastic, 3, "Lấy hết thức ăn khỏi hộp nhựa. Nhựa bẩn cần xử lý theo hướng dẫn địa phương.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("plastic-cap", "Nắp chai nhựa", WasteType.Plastic, 4, "Nắp nhựa nhỏ vẫn là Nhựa; cách thu gom nắp phụ thuộc đơn vị tái chế.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("shampoo-bottle", "Chai dầu gội", WasteType.Plastic, 5, "Làm rỗng chai. Tháo bộ phận khác vật liệu nếu đơn vị thu gom yêu cầu.", LearningContext.Home),
            new WasteDefinition("drink-can", "Lon nước nhôm", WasteType.Metal, 0, "Nhôm thuộc nhóm Kim loại. Làm rỗng lon trước khi thu gom.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("food-can", "Hộp thiếc thực phẩm", WasteType.Metal, 1, "Hộp thép/thiếc thuộc Kim loại. Làm sạch thức ăn thừa và tránh cạnh sắc.", LearningContext.Home),
            new WasteDefinition("metal-lid", "Nắp hộp kim loại", WasteType.Metal, 2, "Nắp kim loại thuộc Kim loại. Không chạm cạnh sắc bằng tay trần.", LearningContext.Home),
            new WasteDefinition("aluminium-tray", "Khay nhôm sạch", WasteType.Metal, 3, "Khay nhôm sạch là Kim loại; loại bỏ thức ăn trước khi thu gom.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("steel-cup", "Cốc inox cũ", WasteType.Metal, 4, "Inox là kim loại. Đồ còn dùng được nên ưu tiên tái sử dụng.", LearningContext.Home, LearningContext.School),
            new WasteDefinition("metal-box", "Hộp bánh kim loại", WasteType.Metal, 5, "Hộp kim loại có thể tái sử dụng để đựng đồ trước khi đem thu gom.", LearningContext.Home),
            new WasteDefinition("newspaper", "Tờ báo cũ", WasteType.Paper, 0, "Giữ báo sạch và khô. Giấy ướt hoặc dính dầu làm giảm khả năng tái chế.", LearningContext.Home, LearningContext.Park),
            new WasteDefinition("cardboard", "Thùng carton", WasteType.Paper, 1, "Tháo vật liệu không phải giấy nếu có, ép phẳng carton sạch và khô.", LearningContext.Home, LearningContext.School),
            new WasteDefinition("paper-bag", "Túi giấy", WasteType.Paper, 2, "Túi giấy không phủ nhựa thuộc Giấy trong bài học. Giữ túi sạch, khô.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("notebook", "Vở giấy cũ", WasteType.Paper, 3, "Tách bìa nhựa hoặc gáy kim loại nếu có. Phần ruột giấy sạch thuộc Giấy.", LearningContext.Home, LearningContext.School),
            new WasteDefinition("envelope", "Phong bì giấy", WasteType.Paper, 4, "Phong bì giấy không có cửa sổ nhựa thuộc Giấy. Bảo vệ thông tin cá nhân trước khi bỏ.", LearningContext.Home, LearningContext.School),
            new WasteDefinition("paper-tube", "Lõi cuộn giấy", WasteType.Paper, 5, "Lõi carton sạch là Giấy. Đây là lõi cuộn, không phải giấy vệ sinh đã dùng.", LearningContext.Home, LearningContext.School),
            new WasteDefinition("plastic-spoon", "Muỗng nhựa", WasteType.Plastic, 6, "Muỗng nhựa sạch thuộc Nhựa; ưu tiên dùng muỗng tái sử dụng.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("yogurt-pot", "Hũ sữa chua nhựa", WasteType.Plastic, 7, "Lấy hết sữa chua, tách nắp khác vật liệu trước khi thu gom.", LearningContext.Home, LearningContext.School),
            new WasteDefinition("plant-pot", "Chậu cây nhựa", WasteType.Plastic, 8, "Đổ đất ra; chậu nhựa hỏng thuộc Nhựa theo hướng dẫn thu gom.", LearningContext.Home, LearningContext.Park),
            new WasteDefinition("metal-spoon", "Muỗng inox cũ", WasteType.Metal, 6, "Muỗng inox là Kim loại. Ưu tiên sử dụng lại nếu còn tốt.", LearningContext.Home, LearningContext.School),
            new WasteDefinition("bottle-crown", "Nắp chai kim loại", WasteType.Metal, 7, "Nắp chai bằng thép là Kim loại, khác nắp nhựa.", LearningContext.Home, LearningContext.Park),
            new WasteDefinition("foil-ball", "Giấy nhôm sạch", WasteType.Metal, 8, "Giấy nhôm là Kim loại dù tên có chữ giấy. Loại bỏ thức ăn bám.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("worksheet", "Tờ bài tập", WasteType.Paper, 6, "Giấy bài tập sạch và khô thuộc nhóm Giấy.", LearningContext.Home, LearningContext.School),
            new WasteDefinition("paper-box", "Hộp giấy sạch", WasteType.Paper, 7, "Hộp giấy không phủ nhựa, không dính dầu thuộc Giấy.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("flyer", "Tờ rơi giấy", WasteType.Paper, 8, "Tờ rơi sạch không cán nhựa thuộc Giấy; tránh phát sinh giấy dùng một lần.", LearningContext.School, LearningContext.Park),
            new WasteDefinition("banana-peel", "Vỏ chuối", WasteType.Organic, 0, "Vỏ chuối là rác hữu cơ. Tách khỏi bao bì nhựa trước khi thu gom.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("apple-core", "Lõi táo", WasteType.Organic, 1, "Lõi táo và phần quả bỏ đi là hữu cơ. Không vứt trên lối đi.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("orange-peel", "Vỏ cam", WasteType.Organic, 2, "Vỏ cam thuộc hữu cơ, không cho vào thùng vật liệu tái chế khô.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("vegetable-scraps", "Rau củ thừa", WasteType.Organic, 3, "Rau củ thừa là hữu cơ; tách dây buộc và túi đựng.", LearningContext.Home, LearningContext.School),
            new WasteDefinition("bread-scrap", "Mẩu bánh mì", WasteType.Organic, 4, "Bánh mì bỏ đi thuộc hữu cơ. Chỉ lấy khẩu phần vừa đủ để giảm lãng phí.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("eggshell", "Vỏ trứng", WasteType.Organic, 5, "Vỏ trứng thuộc hữu cơ trong bài học. Quy trình thu gom phụ thuộc địa phương.", LearningContext.Home, LearningContext.School),
            new WasteDefinition("coffee-grounds", "Bã cà phê", WasteType.Organic, 6, "Bã cà phê là hữu cơ; tách cốc và bao bì ra trước.", LearningContext.Home),
            new WasteDefinition("watermelon-rind", "Vỏ dưa hấu", WasteType.Organic, 7, "Vỏ dưa hấu thuộc hữu cơ; bỏ đúng nơi sau buổi dã ngoại.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("fallen-leaf", "Lá cây rụng", WasteType.Organic, 8, "Lá rụng là hữu cơ từ vườn. Thu gom theo quy định rác vườn của địa phương.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            // Cardboard
            new WasteDefinition("cereal-box", "Hộp ngũ cốc", WasteType.Cardboard, 0, "Ép phẳng carton, bỏ túi lót bên trong; giữ khô và sạch.", LearningContext.Home, LearningContext.School),
            new WasteDefinition("pizza-box-clean", "Hộp pizza sạch", WasteType.Cardboard, 1, "Chỉ carton không dính dầu mới được tái chế; loại bỏ phần bẩn nếu có.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            // GreenGlass
            new WasteDefinition("wine-bottle-green", "Chai thủy tinh xanh", WasteType.GreenGlass, 0, "Làm rỗng và bỏ nắp nhựa trước khi thu gom; thủy tinh vỡ cần bọc an toàn.", LearningContext.Home),
            new WasteDefinition("glass-jar-green", "Hũ thủy tinh xanh", WasteType.GreenGlass, 1, "Rửa sạch hũ; kiểm tra nơi thu gom có yêu cầu tách màu hay không.", LearningContext.Home, LearningContext.School),
            // BrownGlass
            new WasteDefinition("beer-bottle-brown", "Chai bia nâu", WasteType.BrownGlass, 0, "Làm rỗng. Thu gom riêng thủy tinh màu; không gộp cùng thủy tinh trong.", LearningContext.Home, LearningContext.Park),
            new WasteDefinition("sauce-bottle-brown", "Chai nước sốt nâu", WasteType.BrownGlass, 1, "Rửa sạch và tháo nắp nhựa. Thủy tinh màu nâu giữ riêng.", LearningContext.Home),
            // WhiteGlass
            new WasteDefinition("jam-jar", "Hũ thủy tinh trong", WasteType.WhiteGlass, 0, "Làm sạch, bỏ nắp kim loại; thủy tinh trong là nhóm thu gom phổ biến nhất.", LearningContext.Home, LearningContext.School),
            new WasteDefinition("glass-bottle-clear", "Chai thủy tinh trong", WasteType.WhiteGlass, 1, "Kiểm tra kỹ: thủy tinh trong có thể nhầm với nhựa PET. Thủy tinh nặng và cứng hơn.", LearningContext.Home, LearningContext.Park),
            // Clothes
            new WasteDefinition("old-tshirt", "Áo cũ", WasteType.Clothes, 0, "Ưu tiên tái sử dụng hoặc điểm thu gom quần áo; không bỏ vào thùng tái chế thông thường.", LearningContext.Home, LearningContext.School),
            new WasteDefinition("worn-jeans", "Quần jeans cũ", WasteType.Clothes, 1, "Quần áo còn phù hợp nên được cho tặng; vải hỏng liên hệ điểm tiếp nhận vải.", LearningContext.Home),
            // Shoes
            new WasteDefinition("worn-shoes", "Giày hỏng", WasteType.Shoes, 0, "Liên hệ điểm thu gom giày cũ; giày gồm nhiều vật liệu không phân loại đơn thuần.", LearningContext.Home),
            new WasteDefinition("old-sandals", "Dép cũ", WasteType.Shoes, 1, "Dép còn phù hợp nên cho tặng; dép hỏng mang đến điểm tiếp nhận riêng.", LearningContext.Home, LearningContext.Park),
            // Batteries
            new WasteDefinition("aa-battery", "Pin AA", WasteType.Batteries, 0, "NGUY HẠI — bỏ điểm thu gom pin riêng; tuyệt đối không tự tháo hoặc đốt.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("phone-battery", "Pin điện thoại", WasteType.Batteries, 1, "Pin lithium NGUY HẠI — liên hệ nhà sản xuất hoặc điểm thu gom điện tử.", LearningContext.Home),
            // Trash
            new WasteDefinition("tissue-used", "Khăn giấy đã dùng", WasteType.Trash, 0, "Khăn giấy ướt/bẩn vào rác thải chung; không tái chế được.", LearningContext.Home, LearningContext.School, LearningContext.Park),
            new WasteDefinition("candy-wrapper", "Vỏ kẹo hỗn hợp", WasteType.Trash, 1, "Vỏ kẹo nhiều lớp vật liệu thường vào rác thải chung; giảm sử dụng bao bì một lần.", LearningContext.Home, LearningContext.School, LearningContext.Park)
        };
        public static IReadOnlyList<WasteDefinition> All => Array.AsReadOnly(entries);
        public static WasteDefinition Get(WasteType type, int model)
        {
            var pool = entries.Where(d => d.Type == type).ToArray();
            return pool[Math.Abs(model % pool.Length)];
        }
        public static WasteDefinition[] Deck(WasteType[] types, int level, int seed)
        {
            var random = new Random(seed);
            var pools = entries.GroupBy(d => d.Type).ToDictionary(g => g.Key, g => g.OrderBy(_ => random.Next()).ToArray());
            var cursor = new Dictionary<WasteType, int>();
            return types.Select(t => { if (!cursor.ContainsKey(t)) cursor[t] = 0; return pools[t][cursor[t]++ % pools[t].Length]; }).ToArray();
        }
    }
}
