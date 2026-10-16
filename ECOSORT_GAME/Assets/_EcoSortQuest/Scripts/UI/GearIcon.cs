using UnityEngine;
using UnityEngine.UI;

namespace EcoSortQuest
{
    // Mesh icon avoids missing Unicode glyphs in the built-in Vietnamese UI font.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GearIcon : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var center=rectTransform.rect.center;
            float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
            const int segments=64;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                float outer=radius*(i%8<4?1:.77f);int start=mesh.currentVertCount;
                mesh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*.34f,color,Vector2.zero);
                mesh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*outer,color,Vector2.zero);
                mesh.AddVert(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*outer,color,Vector2.zero);
                mesh.AddVert(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius*.34f,color,Vector2.zero);
                mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
