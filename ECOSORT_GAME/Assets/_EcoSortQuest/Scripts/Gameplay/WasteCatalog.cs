using System;
using System.Collections.Generic;

namespace EcoSortQuest
{
    public sealed class WasteDefinition
    {
        public readonly string Id, Name, Lesson;
        public readonly WasteType Type;
        public readonly int Model;
        public WasteDefinition(string id, string name, WasteType type, int model, string lesson)
        { Id = id; Name = name; Type = type; Model = model; Lesson = lesson; }
    }

    // Only clean, single-material examples: acceptance still depends on local collection rules.
    public static class WasteCatalog
    {
        static readonly WasteDefinition[] entries = {
            new WasteDefinition("water-bottle", "Chai nước nhựa", WasteType.Plastic, 0, "Đổ hết nước và để ráo trước khi thu gom. Chai được phân loại theo thân nhựa."),
            new WasteDefinition("detergent-bottle", "Chai nước giặt", WasteType.Plastic, 1, "Chai nhựa cứng: làm rỗng và kiểm tra hướng dẫn thu gom tại địa phương."),
            new WasteDefinition("plastic-cup", "Cốc nhựa", WasteType.Plastic, 2, "Cốc nhựa sạch thuộc nhóm Nhựa trong bài học. Không cho cốc còn đồ uống vào giấy."),
            new WasteDefinition("food-container", "Hộp nhựa đựng đồ", WasteType.Plastic, 3, "Lấy hết thức ăn khỏi hộp nhựa. Nhựa bẩn cần xử lý theo hướng dẫn địa phương."),
            new WasteDefinition("plastic-cap", "Nắp chai nhựa", WasteType.Plastic, 4, "Nắp nhựa nhỏ vẫn là Nhựa; cách thu gom nắp phụ thuộc đơn vị tái chế."),
            new WasteDefinition("shampoo-bottle", "Chai dầu gội", WasteType.Plastic, 5, "Làm rỗng chai. Tháo bộ phận khác vật liệu nếu đơn vị thu gom yêu cầu."),
            new WasteDefinition("drink-can", "Lon nước nhôm", WasteType.Metal, 0, "Nhôm thuộc nhóm Kim loại. Làm rỗng lon trước khi thu gom."),
            new WasteDefinition("food-can", "Hộp thiếc thực phẩm", WasteType.Metal, 1, "Hộp thép/thiếc thuộc Kim loại. Làm sạch thức ăn thừa và tránh cạnh sắc."),
            new WasteDefinition("metal-lid", "Nắp hộp kim loại", WasteType.Metal, 2, "Nắp kim loại thuộc Kim loại. Không chạm cạnh sắc bằng tay trần."),
            new WasteDefinition("aluminium-tray", "Khay nhôm sạch", WasteType.Metal, 3, "Khay nhôm sạch là Kim loại; loại bỏ thức ăn trước khi thu gom."),
            new WasteDefinition("steel-cup", "Cốc inox cũ", WasteType.Metal, 4, "Inox là kim loại. Đồ còn dùng được nên ưu tiên tái sử dụng."),
            new WasteDefinition("metal-box", "Hộp bánh kim loại", WasteType.Metal, 5, "Hộp kim loại có thể tái sử dụng để đựng đồ trước khi đem thu gom."),
            new WasteDefinition("newspaper", "Tờ báo cũ", WasteType.Paper, 0, "Giữ báo sạch và khô. Giấy ướt hoặc dính dầu làm giảm khả năng tái chế."),
            new WasteDefinition("cardboard", "Thùng carton", WasteType.Paper, 1, "Tháo vật liệu không phải giấy nếu có, ép phẳng carton sạch và khô."),
            new WasteDefinition("paper-bag", "Túi giấy", WasteType.Paper, 2, "Túi giấy không phủ nhựa thuộc Giấy trong bài học. Giữ túi sạch, khô."),
            new WasteDefinition("notebook", "Vở giấy cũ", WasteType.Paper, 3, "Tách bìa nhựa hoặc gáy kim loại nếu có. Phần ruột giấy sạch thuộc Giấy."),
            new WasteDefinition("envelope", "Phong bì giấy", WasteType.Paper, 4, "Phong bì giấy không có cửa sổ nhựa thuộc Giấy. Bảo vệ thông tin cá nhân trước khi bỏ."),
            new WasteDefinition("paper-tube", "Lõi cuộn giấy", WasteType.Paper, 5, "Lõi carton sạch là Giấy. Đây là lõi cuộn, không phải giấy vệ sinh đã dùng.")
        };
        public static IReadOnlyList<WasteDefinition> All => Array.AsReadOnly(entries);
        public static WasteDefinition Get(WasteType type, int model) => entries[(int)type * 6 + Math.Abs(model % 6)];
        public static WasteDefinition[] Deck(WasteType[] types, int level, int seed)
        {
            var random = new Random(seed);
            int count = level <= 1 ? 3 : level == 2 ? 4 : 6;
            var pools = new int[3][]; var cursor = new int[3];
            for (int t = 0; t < 3; t++)
            {
                pools[t] = new int[count];
                for (int i = 0; i < count; i++) pools[t][i] = i;
                for (int i = count - 1; i > 0; i--)
                { int j = random.Next(i + 1); int value = pools[t][i]; pools[t][i] = pools[t][j]; pools[t][j] = value; }
            }
            var deck = new WasteDefinition[types.Length];
            for (int i = 0; i < types.Length; i++)
            { int t = (int)types[i]; deck[i] = Get(types[i], pools[t][cursor[t]++ % count]); }
            return deck;
        }
    }
}
