using UnityEngine;

namespace EcoSortQuest
{
    public sealed partial class WorldFactory
    {
        /// <summary>
        /// Mô hình 3D cho 8 nhóm vật liệu mở rộng từ mô hình AI 12 lớp:
        /// Cardboard, GreenGlass, BrownGlass, WhiteGlass, Clothes, Shoes, Batteries, Trash.
        /// </summary>
        void ExtendedModel(Transform parent, WasteType type, int model)
        {
            Color color = WasteInfo.Color(type);
            GameObject Piece(string n, Vector3 p, Vector3 s, Color c, PrimitiveType shape = PrimitiveType.Cube)
                => Shape(n, shape, parent, p, s, c, false);

            if (type == WasteType.Cardboard)
            {
                // Hộp carton / bìa carton ép phẳng
                Piece("Carton Box", new Vector3(0, .35f, 0), new Vector3(.9f, .6f, .75f), color);
                Piece("Tape Line", new Vector3(0, .66f, 0), new Vector3(.16f, .02f, .76f), new Color(.92f, .85f, .65f));
                Piece("Shipping Mark", new Vector3(0, .4f, -.38f), new Vector3(.4f, .25f, .02f), Color.white);
            }
            else if (type == WasteType.GreenGlass)
            {
                // Chai / hũ thủy tinh xanh lá
                Piece("Glass Body", new Vector3(0, .45f, 0), new Vector3(.52f, .65f, .52f), color, PrimitiveType.Cylinder);
                Piece("Bottle Neck", new Vector3(0, .88f, 0), new Vector3(.22f, .26f, .22f), color, PrimitiveType.Cylinder);
                Piece("Cap", new Vector3(0, 1.02f, 0), new Vector3(.24f, .06f, .24f), Color.white, PrimitiveType.Cylinder);
                Piece("Green Label", new Vector3(0, .42f, 0), new Vector3(.54f, .22f, .54f), new Color(.9f, .95f, .9f), PrimitiveType.Cylinder);
            }
            else if (type == WasteType.BrownGlass)
            {
                // Chai bia / hũ thủy tinh màu nâu
                Piece("Brown Glass Body", new Vector3(0, .42f, 0), new Vector3(.54f, .62f, .54f), color, PrimitiveType.Cylinder);
                Piece("Bottle Neck", new Vector3(0, .85f, 0), new Vector3(.24f, .28f, .24f), color, PrimitiveType.Cylinder);
                Piece("Crown", new Vector3(0, 1.0f, 0), new Vector3(.26f, .05f, .26f), new Color(.85f, .75f, .2f), PrimitiveType.Cylinder);
            }
            else if (type == WasteType.WhiteGlass)
            {
                // Hũ / chai thủy tinh trong suốt
                Piece("Clear Jar Body", new Vector3(0, .35f, 0), new Vector3(.6f, .5f, .6f), color, PrimitiveType.Cylinder);
                Piece("Jar Rim", new Vector3(0, .62f, 0), new Vector3(.64f, .06f, .64f), new Color(.75f, .78f, .8f), PrimitiveType.Cylinder);
                Piece("White Label", new Vector3(0, .34f, 0), new Vector3(.62f, .20f, .62f), Color.white, PrimitiveType.Cylinder);
            }
            else if (type == WasteType.Clothes)
            {
                // Áo gấp / chồng vải
                Piece("Folded Fabric", new Vector3(0, .15f, 0), new Vector3(.85f, .22f, .65f), color);
                Piece("Collar", new Vector3(0, .28f, .12f), new Vector3(.45f, .06f, .32f), color * 1.2f);
                Piece("Button Line", new Vector3(0, .27f, -.12f), new Vector3(.08f, .03f, .25f), Color.white);
            }
            else if (type == WasteType.Shoes)
            {
                // Đế giày + thân giày
                Piece("Shoe Sole", new Vector3(0, .08f, 0), new Vector3(.45f, .12f, .95f), new Color(.25f, .25f, .25f));
                Piece("Shoe Upper", new Vector3(0, .24f, -.08f), new Vector3(.42f, .22f, .65f), color);
                Piece("Toe Cap", new Vector3(0, .16f, .3f), new Vector3(.4f, .14f, .25f), color * .8f, PrimitiveType.Sphere);
            }
            else if (type == WasteType.Batteries)
            {
                // Pin hình trụ có sọc đen cảnh báo và cực dương kim loại
                Piece("Battery Body", new Vector3(0, .35f, 0), new Vector3(.38f, .6f, .38f), color, PrimitiveType.Cylinder);
                Piece("Warning Stripe", new Vector3(0, .22f, 0), new Vector3(.39f, .18f, .39f), new Color(.15f, .15f, .15f), PrimitiveType.Cylinder);
                Piece("Positive Tip", new Vector3(0, .68f, 0), new Vector3(.14f, .08f, .14f), new Color(.85f, .85f, .85f), PrimitiveType.Cylinder);
            }
            else // Trash
            {
                // Túi rác thải chung / mẩu rác hỗn hợp
                Piece("Trash Bag", new Vector3(0, .32f, 0), new Vector3(.65f, .55f, .6f), color, PrimitiveType.Sphere);
                Piece("Bag Neck", new Vector3(0, .60f, 0), new Vector3(.22f, .12f, .22f), color * .8f, PrimitiveType.Cylinder);
                Piece("Bag Tie", new Vector3(0, .67f, 0), new Vector3(.18f, .05f, .18f), new Color(.3f, .3f, .3f), PrimitiveType.Cylinder);
            }
        }
    }
}
