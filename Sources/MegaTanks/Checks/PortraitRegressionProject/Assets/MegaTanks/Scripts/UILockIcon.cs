using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    // Native UI geometry keeps the small lock crisp without a raster/font dependency.
    public sealed class UILockIcon : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Arc(mesh,6,6,new Color32(16,18,23,255));
            Arc(mesh,6,3,color);
            Rect(mesh,3,1,18,13,new Color32(16,18,23,255));
            Rect(mesh,5,3,14,9,color);
            Disc(mesh,12,8,1.6f,new Color32(16,18,23,255));
            Rect(mesh,11.3f,4.5f,1.4f,3.2f,new Color32(16,18,23,255));
        }
        Vector2 Point(float x,float y)
        {var r=GetPixelAdjustedRect();return new Vector2(r.x+x*r.width/24,r.y+y*r.height/24);}
        void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color32 tint)
        {
            int start=mesh.currentVertCount;mesh.AddVert(a,tint,Vector2.zero);mesh.AddVert(b,tint,Vector2.zero);mesh.AddVert(c,tint,Vector2.zero);mesh.AddVert(d,tint,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
        void Rect(VertexHelper mesh,float x,float y,float w,float h,Color32 tint)=>Quad(mesh,Point(x,y),Point(x,y+h),Point(x+w,y+h),Point(x+w,y),tint);
        void Arc(VertexHelper mesh,float radius,float width,Color32 tint)
        {
            const int steps=16;
            for(int i=0;i<steps;i++)
            {
                float a=i*Mathf.PI/steps,b=(i+1)*Mathf.PI/steps,inner=radius-width*.5f,outer=radius+width*.5f;
                Quad(mesh,Point(12+Mathf.Cos(a)*inner,13+Mathf.Sin(a)*inner),Point(12+Mathf.Cos(b)*inner,13+Mathf.Sin(b)*inner),Point(12+Mathf.Cos(b)*outer,13+Mathf.Sin(b)*outer),Point(12+Mathf.Cos(a)*outer,13+Mathf.Sin(a)*outer),tint);
            }
            Rect(mesh,12-radius-width*.5f,9,width,4,tint);Rect(mesh,12+radius-width*.5f,9,width,4,tint);
        }
        void Disc(VertexHelper mesh,float x,float y,float radius,Color32 tint)
        {
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;Quad(mesh,Point(x,y),Point(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius),Point(x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius),Point(x,y),tint);}
        }
    }
}
