using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    public sealed class HtmlMainPresentation : MonoBehaviour
    {
        HtmlBattleWorld world;HtmlBattleSimulation preview;HtmlBattleArt art;Text level,xp,money,gears;Image exp;BattleGame game;
        RectTransform R(Transform p,string n,float x,float y,float w,float h)
        {var r=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        Image I(Transform p,string n,string key,float x,float y,float w,float h)
        {var im=R(p,n,x,y,w,h).gameObject.AddComponent<Image>();im.sprite=art.Image(key)?.sprite;im.raycastTarget=false;return im;}
        Text T(Transform p,string n,float x,float y,float w,float h,int size)
        {var t=R(p,n,x,y,w,h).gameObject.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontStyle=FontStyle.BoldAndItalic;t.fontSize=size;t.color=Color.white;t.raycastTarget=false;return t;}
        void Start()
        {
            game=BattleGame.Instance;if(game==null)return;art=Resources.Load<HtmlBattleArt>("HtmlBattleArt");if(art==null)return;
            var rect=R(transform,"Original Art Main World",0,0,1920,1080);rect.SetAsFirstSibling();var raw=rect.gameObject.AddComponent<RawImage>();raw.raycastTarget=false;
            world=gameObject.AddComponent<HtmlBattleWorld>();world.Initialize(art,raw);
            var c=HtmlBattleConfig.Load();preview=new HtmlBattleSimulation(c,0);preview.Geometry=art;preview.ConfigureProfile(new PlayerTankProfile(c).Level);preview.ConfigureCrew(game.Crew,game.Progression);preview.phase="main";
            I(transform,"Player Profile Frame","profile_profile_frame",25,20,430,106).type=Image.Type.Sliced;
            var avatar=I(transform,"Player Avatar","profile_avatar_default",38,28,90,90);avatar.preserveAspect=true;
            level=T(transform,"Permanent Tank Level",155,27,275,42,32);xp=T(transform,"Permanent Tank EXP",155,71,260,24,18);
            I(transform,"EXP Rail","profile_exp_rail",153,98,275,9);exp=I(transform,"EXP Progress","profile_exp_fill",153,98,275,9);exp.type=Image.Type.Filled;exp.fillMethod=Image.FillMethod.Horizontal;
            Counter(1213,"counter_coin",out money);Counter(1495,"counter_gear",out gears);
        }
        void Counter(float x,string icon,out Text text)
        {var frame=I(transform,"Main Counter Frame "+icon,"counter_counter_frame",x+45,49,220,48);frame.color=new Color(1,1,1,.8f);frame.type=Image.Type.Sliced;I(transform,icon,icon,x,37,66,66).preserveAspect=true;text=T(transform,"Amount "+icon,x+60,47,190,52,32);text.alignment=TextAnchor.MiddleRight;}
        void LateUpdate()
        {
            if(game==null||preview==null)return;var c=preview.config;var profile=new PlayerTankProfile(c);
            preview.baseTankLevel=preview.temporaryTankLevel=profile.Level;preview.time+=Time.unscaledDeltaTime;
            foreach(var f in preview.fighters)f.locked=!preview.RoleUnlocked(f.role);
            world.Draw(preview,Time.unscaledDeltaTime);level.text="TANK LV. "+profile.Level;xp.text="EXP "+profile.IntoLevel+" / "+profile.Cost(profile.Level);exp.fillAmount=(float)profile.IntoLevel/profile.Cost(profile.Level);
            money.text=game.Wallet.Parts.ToString("N0");gears.text=game.Wallet.PetFood.ToString("N0");
        }
    }
}
