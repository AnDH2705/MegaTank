using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    // Visual adapter: existing HUD bindings, buttons, ammo warnings and game rules stay authoritative.
    public sealed class CombatHudPresentation : MonoBehaviour
    {
        public Sprite cannonAmmo, mgAmmo, mortarAmmo, partsIcon;
        readonly List<RectTransform> stops=new List<RectTransform>();
        UIView view;
        RouteUISkin route;
        Text tankName, parts, scraps;
        RectTransform strip;
        Image progress;
        bool built;
        int runId=-1;
        readonly Dictionary<int,TentType> revealedCamps=new Dictionary<int,TentType>();
        static readonly Color Ink=new Color(.14f,.16f,.21f,.94f);
        static readonly Color Gold=new Color(1,.8f,.22f);

        static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false);Place(r,x,y,w,h);return r;
        }
        static void Place(RectTransform r,float x,float y,float w,float h)
        {r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static Image Image(Transform parent,string name,Sprite sprite,Color color,float x,float y,float w,float h)
        {
            var i=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();i.sprite=sprite;i.color=color;i.raycastTarget=false;i.preserveAspect=sprite!=null;return i;
        }
        static void Plate(Transform parent,string name,Color color,float x,float y,float w,float h)
        {var p=Rect(parent,name,x,y,w,h).gameObject.AddComponent<CombatHudPlate>();p.color=color;p.raycastTarget=false;}
        Text Label(Transform parent,string name,string value,float x,float y,float w,float h,int size,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var t=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Text>();t.font=view.Get<Text>("Hull Anchor/Hull").font;
            t.text=value;t.fontSize=size;t.fontStyle=FontStyle.Bold;t.alignment=align;t.color=Color.white;t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;return t;
        }
        void Move(string key,float x,float y,float w,float h)
        {var r=view.Get<RectTransform>(key);r.SetParent(transform,false);Place(r,x,y,w,h);}
        void Hide(string key)=>view.Get<RectTransform>(key).gameObject.SetActive(false);
        public void Build(UIView bindings,CanvasUISkin skin,RouteUISkin routeSkin)
        {
            if(built)return;built=true;view=bindings;route=routeSkin;
            foreach(string key in new[]{"Header","Brand","Round","Encounter","Score","Pan Hint","scraps Counter","scraps Title","scraps Value"})Hide(key);
            foreach(Transform child in transform)if(child.name.StartsWith("Decor "))child.gameObject.SetActive(false);
            Move("Pause",30,40,100,62);view.Get<Image>("Pause").sprite=skin.buttonDark;
            Hide("Pause/Label");view.Get<Image>("Pause/Control Icon").sprite=skin.menu;
            Place(view.Get<RectTransform>("Pause/Control Icon"),30,15,40,32);
            tankName=Label(transform,"Tank identity","TANK",150,40,370,60,29);

            var track=Rect(transform,"Stage horizon",755,32,505,118);
            Image(track,"Track border",null,Ink,0,39,505,13);
            Image(track,"Track rail",null,new Color(.93f,.89f,.77f),3,42,499,7);
            progress=Image(track,"Travelled rail",null,Gold,3,43,66,5);
            strip=Rect(track,"Upcoming stops",0,0,505,118);strip.gameObject.AddComponent<RectMask2D>();
            for(int i=0;i<6;i++)
            {
                var stop=Rect(strip,"Stop "+i,0,0,100,112);stops.Add(stop);
                Image(stop,"Icon",null,Color.white,19,14,62,62);
                var caption=Label(stop,"Caption","",-8,80,116,27,17,TextAnchor.MiddleCenter);
                var outline=caption.gameObject.AddComponent<Outline>();outline.effectColor=Ink;outline.effectDistance=new Vector2(2,-2);
            }
            Label(track,"Tank pointer","▼",53,-17,32,30,23,TextAnchor.MiddleCenter).color=Gold;

            var money=Rect(transform,"Battle resources",1350,40,540,66);
            Plate(money,"Scraps plate",Ink,0,7,252,49);
            Image(money,"Scraps icon",route.shop,Color.white,0,0,62,62);
            scraps=Label(money,"Scraps amount","0",67,10,170,34,29,TextAnchor.MiddleRight);
            Label(money,"Scraps caption","SCRAPS",68,44,170,21,13,TextAnchor.MiddleRight);
            Plate(money,"Parts plate",Ink,278,7,252,49);
            Image(money,"Parts icon",partsIcon,Color.white,278,0,62,62);
            parts=Label(money,"Parts amount","0",345,10,170,34,29,TextAnchor.MiddleRight);
            Label(money,"Parts caption","PARTS",345,44,170,21,13,TextAnchor.MiddleRight);

            Move("Hull Anchor",30,968,825,82);
            var anchor=view.Get<RectTransform>("Hull Anchor");
            var rootImage=anchor.GetComponent<Image>();if(rootImage!=null)rootImage.color=Color.clear;
            Hide("Hull Anchor/Hull Empty");
            Plate(anchor,"HP outer border",new Color(.1f,.09f,.09f),0,12,825,70);anchor.Find("HP outer border").SetAsFirstSibling();
            Plate(anchor,"HP rim",new Color(.96f,.91f,.78f),4,16,815,62);anchor.Find("HP rim").SetSiblingIndex(1);
            Plate(anchor,"HP plate",Ink,8,20,805,54);anchor.Find("HP plate").SetSiblingIndex(2);
            var bar=view.Get<Image>("Hull Anchor/Hull Bar");Place(bar.rectTransform,12,23,789,15);bar.sprite=null;bar.type=UnityEngine.UI.Image.Type.Filled;bar.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;bar.fillOrigin=0;
            var hp=view.Get<Text>("Hull Anchor/Hull");Place(hp.rectTransform,18,42,765,30);hp.fontSize=23;hp.alignment=TextAnchor.MiddleCenter;hp.color=Color.white;hp.transform.SetAsLastSibling();
            Label(anchor,"HP title","HP",24,-12,100,40,34).color=Gold;
            Ammo("cannon",cannonAmmo,910);Ammo("mg",mgAmmo,1110);Ammo("mortar",mortarAmmo,1310);
        }
        void Ammo(string key,Sprite sprite,float x)
        {
            Hide(key+" Counter");Hide(key+" Title");Hide(key+" Empty Bar");Hide(key+" Bar");
            Move(key+" Value",x+49,984,145,45);
            var value=view.Get<Text>(key+" Value");value.fontSize=29;value.alignment=TextAnchor.MiddleLeft;
            var ammo=Rect(transform,key+" Ammo icon",x,979,40,52);
            int count=key=="mg"?3:1;
            for(int i=0;i<count;i++)
            {
                var shell=Image(ammo,"Shell "+i,sprite,Color.white,count==1?-7:-12+i*13,count==1?43:38,count==1?54:43,count==1?25:17);
                shell.rectTransform.localRotation=Quaternion.Euler(0,0,90);
            }
            Move(key+" Status",x,1032,190,26);view.Get<Text>(key+" Status").fontSize=17;
            Move(key+" Empty Alert",x-7,974,195,61);
        }
        public void Refresh(BattleGame game)
        {
            if(!built)return;
            tankName.text=game.TankProfile.displayName.ToUpperInvariant();
            parts.text=game.Wallet.Parts.ToString("N0");scraps.text=game.Rewards.Scraps.ToString("N0");
            view.Get<RectTransform>("Hull Anchor").gameObject.SetActive(true);
            view.Get<Text>("Hull Anchor/Hull").text=$"{Mathf.CeilToInt(game.Hull)} / {Mathf.CeilToInt(game.MaxHull)} HP";
            var hp=view.Get<Image>("Hull Anchor/Hull Bar");hp.color=hp.fillAmount<=.3f?new Color(1,.24f,.24f):hp.fillAmount<=.6f?Gold:new Color(.3f,.85f,.56f);
            AmmoValue("cannon",game.MainGun.Ammo,game.MainGun.ReserveCapacity);
            AmmoValue("mg",game.MachineGun.Ammo,game.MachineGun.ReserveCapacity);
            AmmoValue("mortar",game.Mortar.Ammo,game.Mortar.Stats.reserveCapacity);
            var stage=game.Stage;if(stage.Current==null||stage.Route==null)return;
            if(runId!=game.RunId){runId=game.RunId;revealedCamps.Clear();}
            if(stage.Mode!=StageMode.Combat&&stage.Mode!=StageMode.WaveBreak&&stage.Mode!=StageMode.Victory)
                revealedCamps[stage.Current.round]=stage.CampType;
            float position=stage.HudPosition;int first=Mathf.Max(0,Mathf.FloorToInt(position)-1);
            for(int i=0;i<stops.Count;i++)
            {
                int index=first+i,round=index/2;bool camp=index%2==1;
                var stop=stops[i];bool valid=round<stage.Route.rounds.Count&&(!camp||round<stage.Route.rounds.Count-1);
                float x=69+(index-position)*172;
                stop.gameObject.SetActive(valid&&x>-65&&x<565);if(!valid)continue;
                Place(stop,x-50,0,100,112);
                var node=stage.Route.rounds[round][0];
                Sprite icon;string caption;
                if(camp)
                {
                    bool known=revealedCamps.TryGetValue(round,out var revealed);
                    bool vendor=round==stage.Route.rounds.Count-2||(round+1)%5==0;
                    var type=known?revealed:vendor?TentType.Vendor:TentType.Upgrade;
                    icon=!known&&!vendor?route.unknown:type==TentType.Repair?route.repair:type==TentType.Resupply?route.supply:type==TentType.Vendor?route.shop:partsIcon;
                    caption=!known&&!vendor?"EVENT":type.ToString().ToUpperInvariant();
                }
                else{icon=route.IconFor(node.type);caption=node.type==StageNodeType.Boss?"BOSS":node.type==StageNodeType.HardBattle?"HARD BATTLE":"BATTLE";}
                var art=stop.Find("Icon").GetComponent<Image>();art.sprite=icon;art.color=!camp&&node.type==StageNodeType.Boss?new Color(1,.4f,.46f):Color.white;
                stop.Find("Caption").GetComponent<Text>().text=caption;
            }
        }
        void AmmoValue(string key,int amount,int capacity)
        {
            var value=view.Get<Text>(key+" Value");value.text=amount.ToString();
            value.color=amount<=0?new Color(.6f,.62f,.64f):capacity>0&&amount/(float)capacity<=.2f?Gold:Color.white;
        }
    }
}
