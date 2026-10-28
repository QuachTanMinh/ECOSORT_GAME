using UnityEngine;
namespace EcoSortQuest
{
    public sealed class UiModalBackdrop : MonoBehaviour
    {
        public GameObject Backdrop;
        void OnDestroy(){if(Backdrop!=null)Destroy(Backdrop);}
    }
}
