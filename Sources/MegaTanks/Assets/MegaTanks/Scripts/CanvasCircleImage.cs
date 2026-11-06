using UnityEngine;
using UnityEngine.UI;
namespace MegaTanks
{
    public sealed class CanvasCircleImage : Image
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=GetPixelAdjustedRect();var center=r.center;var v=UIVertex.simpleVert;v.color=color;v.position=center;v.uv0=new Vector2(.5f,.5f);mesh.AddVert(v);
            const int segments=80;
            for(int i=0;i<=segments;i++){float a=i*Mathf.PI*2/segments;var p=new Vector2(Mathf.Cos(a),Mathf.Sin(a));v.position=center+Vector2.Scale(p,r.size*.5f);v.uv0=p*.5f+Vector2.one*.5f;mesh.AddVert(v);if(i>0)mesh.AddTriangle(0,i,i+1);}
        }
    }
}
