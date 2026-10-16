using UnityEngine;

namespace EcoSortQuest
{
    [CreateAssetMenu(menuName = "EcoSort/Level Config")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Range(1, 3)] public int level = 1;
        [Min(1)] public int itemCount = 10;
        [Min(1)] public int requiredCorrect = 7;
        [Min(0)] public float timeLimit;
        public string description = "Chọn vật phẩm, sau đó chọn thùng phù hợp.";
        public WasteType[] itemSequence = { WasteType.Plastic, WasteType.Metal, WasteType.Paper, WasteType.Organic };
        void OnValidate()
        {
            itemCount = Mathf.Max(1, itemCount);
            requiredCorrect = Mathf.Clamp(requiredCorrect, 1, itemCount);
        }
    }
}
