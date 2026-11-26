using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    // Renders the supplied AntiqueOliCom bitmap atlas without installing a system font.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CrewBitmapText : MaskableGraphic
    {
        [Serializable] public sealed class Glyph { public int code; public float advance; public float[] baseline; public float[] box; }
        [Serializable] public sealed class Metrics { public Glyph[] glyphs; }
        public Texture2D atlas;
        public TextAsset metrics;
        public float fontSize = 16;
        public bool centered;
        [SerializeField, TextArea] string value = "";
        Dictionary<int, Glyph> glyphs;
        public string Value { get => value; set { this.value = value ?? ""; SetVerticesDirty(); } }
        public override Texture mainTexture => atlas != null ? atlas : base.mainTexture;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (atlas == null || metrics == null) return;
            if (glyphs == null) { glyphs = new Dictionary<int,Glyph>(); foreach(var g in JsonUtility.FromJson<Metrics>(metrics.text).glyphs) glyphs[g.code] = g; }
            var rect = rectTransform.rect; var lines = (value ?? "").Split('\n');
            float scale = fontSize / 64f, lineHeight = fontSize * 1.2f;
            float widest = 0;
            foreach(var line in lines) { float width = 0; foreach(char c in line) if(glyphs.TryGetValue(c,out var g)) width += g.advance * scale; widest = Mathf.Max(widest,width); }
            scale *= Mathf.Min(1,rect.width / Mathf.Max(1,widest));
            for(int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex]; float width = 0;
                foreach(char c in line) if(glyphs.TryGetValue(c,out var g)) width += g.advance * scale;
                float x = rect.xMin + (centered ? (rect.width-width)*.5f : 0);
                float baseline = rect.center.y + (lines.Length-1)*lineHeight*.5f + fontSize*.3f - lineIndex*lineHeight - fontSize*.5f;
                foreach(char c in line)
                {
                    if(!glyphs.TryGetValue(c,out var g)) continue;
                    var b = g.box; if(b[2]>b[0] && b[3]>b[1])
                    {
                        float left=x+(b[0]-g.baseline[0])*scale, right=x+(b[2]-g.baseline[0])*scale;
                        float top=baseline+(g.baseline[1]-b[1])*scale, bottom=baseline+(g.baseline[1]-b[3])*scale;
                        float u0=b[0]/atlas.width,u1=b[2]/atlas.width,v0=1-b[3]/atlas.height,v1=1-b[1]/atlas.height;
                        int i=vh.currentVertCount;
                        vh.AddVert(new Vector3(left,bottom,0),color,new Vector2(u0,v0)); vh.AddVert(new Vector3(left,top,0),color,new Vector2(u0,v1));
                        vh.AddVert(new Vector3(right,top,0),color,new Vector2(u1,v1)); vh.AddVert(new Vector3(right,bottom,0),color,new Vector2(u1,v0));
                        vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
                    }
                    x += g.advance*scale;
                }
            }
        }
    }
}

