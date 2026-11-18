using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    // Authoring pass only: no gameplay listeners, bindings or screen transitions are changed.
    public static class UIVisualPolish
    {
        const string Folder="Assets/MegaTanks/Prefabs/UI/";
        static readonly Color Ink=new Color(.035f,.055f,.08f),Panel=new Color(.075f,.115f,.16f),Line=new Color(.19f,.27f,.34f);
        static readonly Color Paper=new Color(.92f,.95f,.96f),Muted=new Color(.62f,.72f,.78f),Gold=new Color(1,.77f,.34f),Cyan=new Color(.35f,.85f,.91f);
        static CanvasUISkin skin;
        static void Edit(string name,Action<UIView> action)
        {
            string path=Folder+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try{action(root.GetComponent<UIView>());PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void Place(RectTransform r,float x,float y,float w,float h)
        {r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Type(Text t,int size,Color color,bool bold=false)
        {
            t.fontSize=size;t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;t.color=color;t.lineSpacing=1.08f;t.raycastTarget=false;
            foreach(var outline in t.GetComponents<Outline>())outline.enabled=false;
            foreach(var shadow in t.GetComponents<Shadow>().Where(s=>!(s is Outline)))shadow.enabled=false;
        }
        static Image Deco(UIView v,string name,float x,float y,float w,float h,Color color,Sprite sprite=null)
        {
            var child=v.transform.Find("Decor "+name);var image=child!=null?child.GetComponent<Image>():new GameObject("Decor "+name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(v.transform,false);Place(image.rectTransform,x,y,w,h);image.sprite=sprite;image.type=sprite!=null?Image.Type.Sliced:Image.Type.Simple;
            image.color=color;image.raycastTarget=false;return image;
        }
        static Text Caption(UIView v,string name,string value,float x,float y,float w,float h,int size,Color color)
        {
            var child=v.transform.Find("Decor "+name);var t=child!=null?child.GetComponent<Text>():new GameObject("Decor "+name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(v.transform,false);t.font=v.GetComponentsInChildren<Text>(true).First(t2=>t2!=t).font;t.text=value;
            Place(t.rectTransform,x,y,w,h);Type(t,size,color,true);t.alignment=TextAnchor.MiddleLeft;return t;
        }
        static void Back(UIView v,string name,float x,float y,float w,float h,Color color,Sprite sprite=null)
        {
            var image=Deco(v,name,x,y,w,h,color,sprite);
            var background=v.transform.Cast<Transform>().FirstOrDefault(t=>t.name=="Screen Background"||t.name=="Equipment Background"||t.name=="Dismiss Backdrop"||t.name=="Radar Route Background");
            image.transform.SetSiblingIndex(background!=null?background.GetSiblingIndex()+1:0);
        }
        static void ButtonStyle(Button button,bool primary=false)
        {
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.94f,.78f);colors.pressedColor=new Color(.72f,.79f,.84f);colors.selectedColor=Color.white;colors.disabledColor=new Color(.36f,.43f,.49f,.7f);colors.fadeDuration=.12f;button.colors=colors;
            foreach(var t in button.GetComponentsInChildren<Text>(true))Type(t,t.fontSize,primary?Ink:Paper,true);
        }
        static void Common(UIView v)
        {
            foreach(var t in v.GetComponentsInChildren<Text>(true).Where(t=>t.GetComponentInParent<UIView>()==v))
            {
                bool title=t.name.Contains("Title")||t.name.EndsWith("Name");
                Type(t,t.fontSize,title?Paper:Muted,title);
            }
            foreach(var b in v.GetComponentsInChildren<Button>(true).Where(b=>b.GetComponentInParent<UIView>()==v))ButtonStyle(b);
            foreach(var t in v.GetComponentsInChildren<Text>(true).Where(t=>t.name=="Screen Title"||t.name=="Popup Title"||t.name=="Pause Title"))Type(t,42,Paper,true);
            var background=v.transform.Find("Screen Background");if(background!=null)background.GetComponent<Image>().color=Ink;
        }

        [MenuItem("MegaTanks/Polish UI Visuals")]
        public static void Apply()
        {
            skin=AssetDatabase.LoadAssetAtPath<CanvasUISkin>("Assets/MegaTanks/Data/UI/LayerLabSkin.asset");
            foreach(string screen in new[]{"Home","BattleHUD","Equipment","Settings","Crew","Tech","LeaveRun","Pause","Defeat","Victory","Rewards","Shop","Service","Reveal","WaveBreak","Route"})Edit(screen,Common);
            Edit("Home",Home);Edit("BattleHUD",Hud);Edit("Equipment",Garage);Edit("Route",Route);
            foreach(string name in new[]{"RewardCard","ServiceCard","ShopCard"})Edit(name,v=>Card(v,name=="ShopCard"));
            foreach(string name in new[]{"Rewards","Shop","Service","Reveal","WaveBreak"})Edit(name,v=>{
                Deco(v,"Header Rule",870,128,180,3,Gold);Deco(v,"Top Rule",80,24,1760,1,Line);
                foreach(var t in v.GetComponentsInChildren<Text>(true).Where(t=>t.name=="Reward Caption"||t.name=="Scraps"))Type(t,26,Muted);
            });
            foreach(string name in new[]{"Defeat","Victory"})Edit(name,v=>Result(v,name=="Defeat"));
            foreach(string name in new[]{"Settings","Crew","Tech","LeaveRun","Pause"})Edit(name,Modal);
            Edit("InventoryItem",v=>{
                Common(v);var t=v.Get<Text>("Item Name");Type(t,20,Paper,true);
                foreach(var image in v.GetComponentsInChildren<Image>(true).Where(i=>i.name=="Item Name"))image.color=Panel;
            });
            AssetDatabase.SaveAssets();Debug.Log("UI_VISUAL_POLISH_APPLIED");
        }
        static void Home(UIView v)
        {
            Back(v,"Navigation Rail",1575,210,305,565,new Color(.035f,.055f,.08f,.93f),skin.panel);
            Deco(v,"Navigation Accent",1596,242,4,485,Gold);
            Caption(v,"Navigation Title","HANGAR",1622,227,230,32,19,Muted);
            int i=0;
            foreach(string key in new[]{"Equipment","Crew","Tech"})
            {
                float y=276+i*154;var b=v.Get<Button>(key);Place((RectTransform)b.transform,1614,y,246,116);b.image.color=new Color(.54f,.66f,.77f);b.image.preserveAspect=false;
                if(b.image.transform!=b.transform)Place(b.image.rectTransform,0,0,246,116);
                var icon=(RectTransform)b.transform.Find("Home Icon");Place(icon,10,15,78,78);
                var caption=v.Get<Text>(key+" Caption");Place(caption.rectTransform,1710,y+23,145,36);Type(caption,25,Paper,true);caption.alignment=TextAnchor.MiddleLeft;
                if(i>0){var badge=v.Get<Text>(key+" Availability");Place(badge.rectTransform,1710,y+61,150,28);Type(badge,16,Gold,true);badge.alignment=TextAnchor.MiddleLeft;}
                i++;
            }
            var stage=v.Get<RectTransform>("Stage Name Plate");Place(stage,40,34,405,112);
            var name=v.Get<Text>("Stage Name Plate/Selected Stage");Type(name,34,Ink,true);Place(name.rectTransform,24,16,355,78);
            var selector=v.Get<RectTransform>("Stage Swipe Area");Place(selector,40,920,495,124);
            var count=v.Get<Text>("Stage Swipe Area/Stage Count");Type(count,21,Gold,true);Place(count.rectTransform,122,16,280,35);
            var route=v.Get<Text>("Stage Swipe Area/Route Name");Type(route,28,Paper,true);Place(route.rectTransform,122,56,280,48);
            var play=v.Get<Button>("Play");Place((RectTransform)play.transform,1510,920,370,124);play.image.preserveAspect=false;ButtonStyle(play,true);
            var label=v.Get<Text>("Play/Label");Type(label,34,Ink,true);Place(label.rectTransform,20,16,330,90);
            var menu=v.Get<RectTransform>("Avatar Menu");Place(menu,1792,34,88,70);
        }
        static void Hud(UIView v)
        {
            var header=v.Get<Image>("Header");header.color=new Color(.035f,.055f,.08f,.95f);Place(header.rectTransform,0,0,1920,194);
            Type(v.Get<Text>("Brand"),33,Paper,true);Type(v.Get<Text>("Round"),23,Muted,true);Type(v.Get<Text>("Encounter"),22,Muted);
            Type(v.Get<Text>("Score"),24,Gold,true);Type(v.Get<Text>("Pan Hint"),18,Muted);
            Back(v,"Pan Hint Chip",24,200,355,35,new Color(.035f,.055f,.08f,.85f),skin.panel);
            foreach(string key in new[]{"mg","cannon","mortar","scraps"})
            {
                v.Get<Image>(key+" Counter").color=Panel;Type(v.Get<Text>(key+" Title"),19,Muted,true);Type(v.Get<Text>(key+" Value"),30,key=="scraps"?Gold:Cyan,true);
                if(key!="scraps")Type(v.Get<Text>(key+" Status"),20,Gold,true);
            }
            Deco(v,"HUD Edge",32,191,1856,2,Line);
        }
        static void Garage(UIView v)
        {
            Type(v.Get<Text>("Popup Title"),42,Ink,true);
            foreach(var image in v.GetComponentsInChildren<Image>(true).Where(i=>i.name=="Equipment Inventory Panel"||i.name.StartsWith("Slot ")))image.color=new Color(.7f,.8f,.94f);
            Type(v.Get<Text>("Detail Name"),27,Paper,true);Type(v.Get<Text>("Item Stats"),20,Paper);
            v.Get<Text>("Item Stats").lineSpacing=1;
            foreach(var t in v.GetComponentsInChildren<Text>(true).Where(t=>t.name=="Slot Description"))Type(t,28,Paper,true);
            ButtonStyle(v.Get<Button>("Equip Item"),true);
            Back(v,"Garage Footer",1010,838,866,204,new Color(.035f,.055f,.08f,.4f),skin.panel);
            Deco(v,"Detail Divider",985,852,2,174,Line);
        }
        static void Route(UIView v)
        {
            Type(v.Get<Text>("Route Stage"),32,Ink,true);
            Type(v.Get<Text>("Route Resources"),25,Cyan,true);Type(v.Get<Text>("Route Inspection"),27,Paper,true);
            Back(v,"Route Resources Panel",48,734,1824,57,new Color(.035f,.055f,.08f,.97f),skin.panel);
            Back(v,"Route Inspection Panel",48,799,1824,131,new Color(.075f,.115f,.16f,.98f),skin.panel);
            Deco(v,"Route Inspection Accent",48,817,4,92,Gold);
            ButtonStyle(v.Get<Button>("Enter Route Node"),true);
            // Keep the supplied radar artwork, dimmed so graph connections and symbols read first.
            var radar=v.transform.Find("Radar Route Background");if(radar!=null)radar.GetComponent<Image>().color=new Color(.58f,.7f,.65f);
        }
        static void Card(UIView v,bool small)
        {
            Common(v);var root=v.Get<Image>("$root");var shadow=root.GetComponent<Shadow>()??root.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.4f);shadow.effectDistance=new Vector2(0,-5);
            var inside=v.transform.Find("Inside");if(inside!=null)inside.GetComponent<Image>().color=Panel;
            Type(v.Get<Text>("Title"),small?24:30,Paper,true);Type(v.Get<Text>("Effect"),small?18:24,Paper);
            v.Get<Text>("Effect").lineSpacing=1;
            if(!small)
            {
                float width=((RectTransform)v.transform).sizeDelta.x;
                Place(v.Get<RectTransform>("Artwork"),(width-218)/2,26,218,298);
                Deco(v,"Card Divider",28,395,width-56,1,Line);
            }
            else Place(v.Get<RectTransform>("Artwork"),127,15,110,100);
            ButtonStyle(v.Get<Button>("Action"));
        }
        static void Result(UIView v,bool defeat)
        {
            Back(v,"Result Panel",280,205,1360,746,Panel,skin.panel);
            Deco(v,"Result Accent",880,162,160,3,Gold);
            var caption=Caption(v,"Result Caption",defeat?"AFTER ACTION REPORT":"MISSION COMPLETE",400,240,1120,40,21,Gold);caption.alignment=TextAnchor.MiddleCenter;
            var detail=v.Get<Text>("Result Detail");Place(detail.rectTransform,380,305,1160,320);Type(detail,27,Paper);detail.lineSpacing=1.15f;
            if(defeat)
            {
                Place(v.Get<RectTransform>("Play Again"),400,708,530,94);Place(v.Get<RectTransform>("Result Garage"),990,708,530,94);
                ButtonStyle(v.Get<Button>("Result Garage"),true);
            }
            ButtonStyle(v.Get<Button>("Play Again"),true);ButtonStyle(v.Get<Button>("Result Home"));
        }
        static void Modal(UIView v)
        {
            foreach(var image in v.GetComponentsInChildren<Image>(true))
            {
                if(image.name=="Popup Panel"||image.name=="Pause Panel")image.color=Panel;
                if(image.name=="Popup Header")image.color=new Color(.12f,.2f,.27f);
                if(image.name=="Dismiss Backdrop"||image.name=="Pause Shade")image.color=new Color(.015f,.025f,.045f,.86f);
            }
            foreach(var t in v.GetComponentsInChildren<Text>(true).Where(t=>t.name.EndsWith("Label")))if(t.GetComponentInParent<Button>()==null)Type(t,28,Paper,true);
            var resume=v.transform.Find("Resume");if(resume!=null){resume.GetComponent<Button>().image.sprite=skin.buttonYellow;ButtonStyle(resume.GetComponent<Button>(),true);}
            if(v.name.StartsWith("Settings"))
            {
                Deco(v,"Sound Row Rule",480,438,960,1,Line);Deco(v,"Music Row Rule",480,588,960,1,Line);
                var note=v.Get<Text>("Audio Note");Type(note,20,Muted);
                Type(v.Get<Text>("Vibration Toggle/Toggle Hint"),22,Ink,true);
            }
        }
    }
}
