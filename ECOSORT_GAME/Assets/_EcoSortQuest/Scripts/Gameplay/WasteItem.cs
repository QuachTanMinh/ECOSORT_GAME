using UnityEngine;

namespace EcoSortQuest
{
    public sealed class WasteItem : MonoBehaviour
    {
        public WasteType type;
        public WasteDefinition Definition { get; set; }
        public GameObject selectionMarker;
        public void Select(bool selected)
        { if (selectionMarker != null) selectionMarker.SetActive(selected); }
    }
}
