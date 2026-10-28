using UnityEngine;
namespace EcoSortQuest
{
    public sealed class WasteBin : MonoBehaviour
    {
        public WasteType type;
        GameObject dropMarker;
        bool ownsMarker;
        public void Highlight(bool active)
        {
            if(dropMarker==null && active)
            {
                dropMarker=new GameObject("Drop target outline",typeof(LineRenderer));
                dropMarker.transform.SetParent(transform,false);
                var line=dropMarker.GetComponent<LineRenderer>();
                line.useWorldSpace=false;line.loop=true;line.positionCount=4;
                line.SetPositions(new[]{new Vector3(-.94f,.06f,-.84f),new Vector3(.94f,.06f,-.84f),new Vector3(.94f,.06f,.84f),new Vector3(-.94f,.06f,.84f)});
                line.startWidth=line.endWidth=.06f;line.startColor=line.endColor=new Color(1,.87f,.28f);
                line.sharedMaterial=SharedMarkerMaterial();markerUsers++;ownsMarker=true;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
            }
            if(dropMarker!=null)dropMarker.SetActive(active);
        }
        static Material markerMaterial;
        static int markerUsers;
        static Material SharedMarkerMaterial()
        {
            if(markerMaterial==null)markerMaterial=new Material(Shader.Find("Sprites/Default")){hideFlags=HideFlags.HideAndDontSave};
            return markerMaterial;
        }
        void OnDestroy()
        {
            // The marker material is shared by the bins of the active scene.
            if(ownsMarker)markerUsers--;
            if(markerMaterial!=null && markerUsers==0)
            { Destroy(markerMaterial);markerMaterial=null; }
        }
    }
}
