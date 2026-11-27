using UnityEngine;
using UnityEngine.UI;
namespace MegaTanks
{
    // Crisp, resolution-independent HUD plate with a sloping right edge.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CombatHudPlate : MaskableGraphic
    {
        public float bevel=25;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;float cut=Mathf.Min(bevel,r.width*.2f);
            mesh.AddVert(new Vector3(r.xMin,r.yMin),color,Vector2.zero);
            mesh.AddVert(new Vector3(r.xMin,r.yMax),color,Vector2.zero);
            mesh.AddVert(new Vector3(r.xMax,r.yMax),color,Vector2.zero);
            mesh.AddVert(new Vector3(r.xMax-cut,r.yMin),color,Vector2.zero);
            mesh.AddTriangle(0,1,2);mesh.AddTriangle(0,2,3);
        }
    }
}
