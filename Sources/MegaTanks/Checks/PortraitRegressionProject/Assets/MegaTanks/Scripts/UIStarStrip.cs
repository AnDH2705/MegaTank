using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    // Mesh stars avoid font-glyph support issues; the last tier uses a rainbow gradient in every star.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIStarStrip : MaskableGraphic
    {
        [SerializeField, Range(0,20)] int stars;
        public int Stars { get => stars; set { int next = Mathf.Clamp(value,0,20); if (stars != next) { stars = next; SetVerticesDirty(); } } }
        public static int VisibleCount(int value) => value <= 0 ? 0 : (Mathf.Clamp(value,1,20) - 1) % 5 + 1;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); int count = VisibleCount(stars); if (count == 0) return;
            var rect = GetPixelAdjustedRect(); float size = Mathf.Min(rect.height, rect.width / 5), gap = size * .06f;
            float left = rect.center.x - (count * size + (count - 1) * gap) / 2;
            Color tier = stars <= 5 ? new Color(1,.8f,.13f) : stars <= 10 ? new Color(.72f,.35f,1) : new Color(1,.24f,.27f);
            for (int star = 0; star < count; star++)
            {
                Vector2 center = new Vector2(left + size * .5f + star * (size+gap), rect.center.y);
                int start = vh.currentVertCount;
                vh.AddVert(center, (stars > 15 ? Color.HSVToRGB(.45f,.85f,1) : tier) * color, Vector2.zero);
                for (int p=0;p<10;p++)
                {
                    float angle=(90+p*36)*Mathf.Deg2Rad, radius=size*(p%2==0?.47f:.21f);
                    Vector2 pos=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                    Color shade=stars>15?Color.HSVToRGB(Mathf.Repeat((pos.x-center.x)/size+.5f,1),.85f,1):tier;
                    vh.AddVert(pos,shade*color,Vector2.zero);
                }
                for (int p=0;p<10;p++) vh.AddTriangle(start,start+1+p,start+1+(p+1)%10);
            }
        }
    }
}
