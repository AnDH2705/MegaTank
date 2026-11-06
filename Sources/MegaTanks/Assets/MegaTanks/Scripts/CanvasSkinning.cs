using UnityEngine;
using UnityEngine.UI;
namespace MegaTanks
{
    public sealed partial class CanvasGameUI
    {
        [SerializeField] CanvasUISkin skin;
        public CanvasUISkin Skin=>skin;
        static readonly Color DarkButtonTint=new Color(.72f,.87f,1);
        void SpriteImage(Image image,Sprite sprite,Color tint,bool sliced=true)
        {
            if(sprite==null)return;image.sprite=sprite;image.color=tint;image.type=sliced?Image.Type.Sliced:Image.Type.Simple;
            image.pixelsPerUnitMultiplier=2;image.preserveAspect=!sliced;
        }
        Image Icon(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
        {
            var icon=Fill(parent,name,x,y,w,h,Color.white);icon.sprite=sprite;icon.preserveAspect=true;icon.raycastTarget=false;
            icon.rectTransform.pivot=Vector2.one*.5f;icon.rectTransform.anchoredPosition=new Vector2(x+w*.5f,-y-h*.5f);return icon;
        }
        void SkinPanel(Image image,string name)
        {
            if(skin==null)return;
            if(name=="Popup Panel"||name=="Pause Panel")SpriteImage(image,skin.panel,panel);
            else if(name=="Item Detail"||name=="Inventory"||name=="Filter List"||name=="Route Scroll"||name=="Stage Swipe Area"||name.EndsWith(" Counter"))
                SpriteImage(image,skin.roundFrame,image.color);
            else if(name.EndsWith(" Empty Bar")||name=="Hull Empty")SpriteImage(image,skin.barBackground,new Color(.20f,.25f,.33f));
            else if(name.EndsWith(" Bar"))SpriteImage(image,skin.barFill,image.color);
        }
        void SkinButton(Button button,string name,string title,bool selected=false)
        {
            if(skin==null||name=="Dismiss Backdrop")return;
            Sprite sprite=skin.buttonDark;Color tint=DarkButtonTint;
            if(name.StartsWith("Slot ")||name.StartsWith("Item ")){sprite=skin.itemFrame;tint=selected?new Color(.60f,1,1):new Color(.72f,.85f,1);}
            else if(name=="Play"||name=="Play Again"||title.StartsWith("BUY")){sprite=skin.buttonYellow;tint=Color.white;}
            else if(name=="Equipment"){sprite=skin.buttonSky;tint=Color.white;}
            else if(name=="Equip Item"||name=="Resume"||name=="Enter Node"||name=="Leave Service"||name=="Leave Shop"||name=="Action") {sprite=skin.buttonBlue;tint=Color.white;}
            else if(name=="Confirm Leave"){sprite=skin.buttonRed;tint=Color.white;}
            SpriteImage(button.image,sprite,tint);
            var states=button.spriteState;states.disabledSprite=skin.buttonDisabled;button.spriteState=states;
            var text=button.transform.Find("Label").GetComponent<Text>();text.fontStyle=FontStyle.Bold;
            var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(1.5f,-1.5f);
            Sprite icon=name=="Previous Stage"?skin.previous:name=="Next Stage"?skin.next:name=="Pause"?skin.pause:name=="Close Popup"?skin.close:null;
            if(icon!=null){text.text="";float size=Mathf.Min(button.image.rectTransform.sizeDelta.x,button.image.rectTransform.sizeDelta.y)*.55f;Icon(button.transform,"Control Icon",icon,(button.image.rectTransform.sizeDelta.x-size)*.5f,(button.image.rectTransform.sizeDelta.y-size)*.5f,size,size);}
        }
        void SkinStance(Button button,bool selected,bool attacking)
        {
            if(skin==null){button.image.color=selected?(attacking?new Color(.72f,.08f,.07f):new Color(.10f,.40f,.45f)):edge;return;}
            SpriteImage(button.image,selected?(attacking?skin.buttonRed:skin.buttonMint):skin.buttonDark,selected?Color.white:DarkButtonTint);
        }
    }
}
