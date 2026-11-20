using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CatTanks.Editor
{
    public static class ButtonAlignmentAudit
    {
        public static int Validate(StudioUIRoot root)
        {
            int count=0;
            foreach(var layout in root.GetComponentsInChildren<StudioButtonLayout>())layout.Apply();
            Canvas.ForceUpdateCanvases();
            foreach(var layout in root.GetComponentsInChildren<StudioButtonLayout>())
            {
                var rect=(RectTransform)layout.transform;
                foreach(var graphic in new Graphic[]{layout.label,layout.icon,layout.badge,layout.hotkey,layout.caption,layout.cost}.Where(g=>g&&g.isActiveAndEnabled))
                {
                    if(graphic.transform.parent!=rect)throw new Exception("Button content detached: "+graphic.name);
                    var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(rect,graphic.rectTransform);
                    if(bounds.min.x<rect.rect.xMin-.5f||bounds.max.x>rect.rect.xMax+.5f||bounds.min.y<rect.rect.yMin-.5f||bounds.max.y>rect.rect.yMax+.5f)throw new Exception("Button content outside bounds: "+layout.name+" / "+graphic.name);
                    var text=graphic as Text;
                    if(text&&(text.preferredWidth>text.rectTransform.rect.width+.5f||text.preferredHeight>text.rectTransform.rect.height+.5f))throw new Exception("Button label does not fit: "+text.text);
                }
                if(layout.content==StudioButtonLayout.Content.Centered&&layout.label)
                {
                    var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(rect,layout.label.rectTransform);
                    if(Vector2.Distance(bounds.center,rect.rect.center)>.5f)throw new Exception("Button label is not centered: "+layout.name);
                }
                count++;
            }
            return count;
        }
    }
}
