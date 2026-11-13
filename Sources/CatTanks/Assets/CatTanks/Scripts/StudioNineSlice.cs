using UnityEngine;
using UnityEngine.UI;

namespace CatTanks
{
    public static class StudioNineSlice
    {
        public static void Apply(Image image, Sprite sprite, float edgeSize = 14, bool preserve = false)
        {
            image.sprite=sprite;image.overrideSprite=null;image.useSpriteMesh=false;
            image.preserveAspect=preserve;image.fillCenter=true;
            bool sliced=sprite&&!preserve&&sprite.border.sqrMagnitude>0;
            image.type=sliced?Image.Type.Sliced:Image.Type.Simple;
            if(sliced)
            {
                var b=sprite.border;float largest=Mathf.Max(b.x,b.y,b.z,b.w);
                float reference=image.canvas?image.canvas.referencePixelsPerUnit:100;
                image.pixelsPerUnitMultiplier=Mathf.Max(.01f,largest/Mathf.Max(1,edgeSize)*reference/sprite.pixelsPerUnit);
            }
            else image.pixelsPerUnitMultiplier=1;
        }
    }
}
