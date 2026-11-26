using UnityEngine;
using UnityEngine.UI;

// Attach only to a background Image (Type = Sliced), never to its content parent.
[AddComponentMenu("UI/Effects/Crew Panel Skew")]
[RequireComponent(typeof(RectTransform))]
public sealed class CrewPanelSkew : BaseMeshEffect
{
    [SerializeField, Range(-20f, 20f)] private float angle = -5.5f;

    public override void ModifyMesh(VertexHelper vertices)
    {
        if (!IsActive()) return;
        float slope = -Mathf.Tan(angle * Mathf.Deg2Rad);
        var vertex = new UIVertex();
        for (int i = 0; i < vertices.currentVertCount; i++)
        {
            vertices.PopulateUIVertex(ref vertex, i);
            vertex.position.x += vertex.position.y * slope;
            vertices.SetUIVertex(vertex, i);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (graphic != null) graphic.SetVerticesDirty();
    }
#endif
}
