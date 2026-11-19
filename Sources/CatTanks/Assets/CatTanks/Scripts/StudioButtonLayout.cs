using UnityEngine;
using UnityEngine.UI;

namespace CatTanks
{
    [DisallowMultipleComponent]
    public sealed class StudioButtonLayout : MonoBehaviour
    {
        public enum Content { Centered, Navigation, Skill }
        public Content content;
        public Text label,hotkey,caption,cost;
        public Image icon;
        public int fontSize=19;
        readonly TextGenerator generator=new TextGenerator();
        struct FitCache
        {
            public bool valid;public string value;public Font font;public FontStyle style;
            public Vector2 size;public float pixels;public int maximum,result;
        }
        FitCache labelFit,hotkeyFit,captionFit,costFit;

        public void Apply()
        {
            var rect=(RectTransform)transform;float w=rect.rect.width,h=rect.rect.height;
            if(label)
            {
                label.gameObject.SetActive(content!=Content.Skill);
                Stretch(label.rectTransform,content==Content.Navigation?58:12,6,12,6);
                label.alignment=content==Content.Navigation?TextAnchor.MiddleLeft:TextAnchor.MiddleCenter;
                Fit(label,fontSize,ref labelFit);
            }
            if(content==Content.Navigation&&icon)
            {
                Place(icon.rectTransform,8,(h-40)*.5f,40,40);
                icon.preserveAspect=true;icon.raycastTarget=false;
            }
            if(content==Content.Skill)
            {
                if(icon){Place(icon.rectTransform,(w-32)*.5f,9,32,32);icon.preserveAspect=true;icon.raycastTarget=false;}
                if(hotkey){Place(hotkey.rectTransform,8,8,24,24);hotkey.alignment=TextAnchor.MiddleCenter;Fit(hotkey,14,ref hotkeyFit);}
                if(caption){Place(caption.rectTransform,8,45,w-16,36);caption.alignment=TextAnchor.MiddleCenter;Fit(caption,16,ref captionFit);}
                if(cost){Place(cost.rectTransform,8,h-27,w-16,20);cost.alignment=TextAnchor.MiddleCenter;Fit(cost,14,ref costFit);}
            }
        }
        void Fit(Text text,int maximum,ref FitCache cache)
        {
            text.resizeTextForBestFit=false;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Truncate;
            var size=text.rectTransform.rect.size;float pixels=text.pixelsPerUnit;
            if(cache.valid&&cache.value==text.text&&cache.font==text.font&&cache.style==text.fontStyle&&cache.size==size&&cache.pixels==pixels&&cache.maximum==maximum)
            {text.fontSize=cache.result;text.raycastTarget=false;return;}
            var settings=text.GetGenerationSettings(size);
            settings.resizeTextForBestFit=false;settings.horizontalOverflow=HorizontalWrapMode.Overflow;settings.verticalOverflow=VerticalWrapMode.Overflow;
            int font=Mathf.Clamp(maximum,13,24);
            while(font>13)
            {
                settings.fontSize=font;
                if(generator.GetPreferredWidth(text.text,settings)<=size.x*text.pixelsPerUnit-2&&generator.GetPreferredHeight(text.text,settings)<=size.y*text.pixelsPerUnit-2)break;
                font--;
            }
            text.fontSize=font;text.raycastTarget=false;
            cache=new FitCache{valid=true,value=text.text,font=text.font,style=text.fontStyle,size=size,pixels=pixels,maximum=maximum,result=font};
        }
        void Own(RectTransform child)
        {
            if(child.parent!=transform)child.SetParent(transform,false);
            var binding=child.GetComponent<StudioWidgetBinding>();if(binding)binding.layoutOwner=(RectTransform)transform;
        }
        void Place(RectTransform child,float x,float y,float w,float h)
        {
            Own(child);var topLeft=new Vector2(0,1);
            if(child.anchorMin!=topLeft)child.anchorMin=topLeft;if(child.anchorMax!=topLeft)child.anchorMax=topLeft;if(child.pivot!=topLeft)child.pivot=topLeft;
            var pos=new Vector2(x,-y);var size=new Vector2(w,h);
            if(child.anchoredPosition!=pos)child.anchoredPosition=pos;if(child.sizeDelta!=size)child.sizeDelta=size;if(child.localScale!=Vector3.one)child.localScale=Vector3.one;
        }
        void Stretch(RectTransform child,float left,float top,float right,float bottom)
        {
            Own(child);var center=new Vector2(.5f,.5f);
            if(child.anchorMin!=Vector2.zero)child.anchorMin=Vector2.zero;if(child.anchorMax!=Vector2.one)child.anchorMax=Vector2.one;if(child.pivot!=center)child.pivot=center;
            var min=new Vector2(left,bottom);var max=new Vector2(-right,-top);
            if(child.offsetMin!=min)child.offsetMin=min;if(child.offsetMax!=max)child.offsetMax=max;if(child.localScale!=Vector3.one)child.localScale=Vector3.one;
        }
    }
}
