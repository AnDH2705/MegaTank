using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    public sealed partial class HtmlBattleScreen
    {
        [Serializable] sealed class Cards { public Card[] cards; }
        [Serializable] sealed class Card { public string id,group,slot,name,tag,copy,action,icon,rarity;public string[] nameLines;public bool ad; }
        [Serializable] sealed class LayoutRect {public float[] rect1920x1080;}
        [Serializable] sealed class EventLayout {public LayoutRect title,options,skip;}
        [Serializable] sealed class EventLayouts {public EventLayout ammo,supply,medical,wreck;}
        [Serializable] sealed class LayoutRoot {public EventLayouts cards;}
        LayoutRoot designLayout;
        CrewBitmapText upgradeMessage;
        HtmlBattleWorld nativeWorld;RawImage nativeView;
        RectTransform timeline,bossPanel;RectTransform[] markers=new RectTransform[3];Image[] markerIcons=new Image[3];Text timelineCaption,runLevel,runProgress;Image bossFill,runExperience;
        string awardedRun;int awardedTankXP;bool adPending;int adTicket;Card[] cardCatalog;HtmlEventTimeline routeTimeline;Image hpChip;float chip=1,lastHP=1,chipHold;
        void BuildDesign()
        {
            artwork["menu"]=new HtmlArtImage{key="menu",sprite=game.UI.Skin.homeMenu};
            artwork["ui_button"]=new HtmlArtImage{key="ui_button",sprite=game.UI.Skin.homeYellowPanel};
            var blocker=StaticImage(transform,"Battle Input Blocker","",0,0,1920,1080,Color.black);blocker.raycastTarget=true;
            world=Rect(transform,"Native world viewport",0,0,1920,1080);nativeView=world.gameObject.AddComponent<RawImage>();nativeView.raycastTarget=false;
            nativeWorld=gameObject.AddComponent<HtmlBattleWorld>();nativeWorld.Initialize(art,nativeView,Sprite);
            hud=Rect(transform,"Battle HUD",0,0,1920,1080);
            Button(hud,"Pause","",29,43,107,60,()=>{paused=true;modalKey="";},"menu");
            BuildRunLevelHUD();
            upgradeMessage=ComicLabel(hud,"Automatic Upgrade Message","TANK LEVEL UP!",80,375,540,80,42);upgradeMessage.gameObject.SetActive(false);
            Counter(hud,1344,47,"counter_coin",out moneyLabel);Counter(hud,1626,47,"counter_gear",out gearsLabel);
            timeline=Rect(hud,"Event Timeline",691,4,538,155);
            routeTimeline=timeline.gameObject.AddComponent<HtmlEventTimeline>();routeTimeline.Initialize(art);
            StaticImage(hud,"HP Panel","hp_hp_panel",27,958,854,96).type=Image.Type.Sliced;
            StaticImage(hud,"HP Rail","hp_hp_rail",44,991,820,15);
            hpChip=StaticImage(hud,"Damage Chip","hp_hp_fill",44,991,820,15,new Color(1,.898f,.627f));hpChip.type=Image.Type.Filled;hpChip.fillMethod=Image.FillMethod.Horizontal;
            hpFill=StaticImage(hud,"HP Fill","hp_hp_fill",44,991,820,15,new Color(.329f,.863f,.569f));hpFill.type=Image.Type.Filled;hpFill.fillMethod=Image.FillMethod.Horizontal;
            StaticImage(hud,"HP Frame","hp_hp_frame",27,958,854,96).type=Image.Type.Sliced;StaticImage(hud,"HP Label","hp_hp_label",50,960,101,36);
            hpLabel=Label(hud,"HP Value","",270,1013,360,30,23,null,TextAnchor.MiddleCenter);
            ammoLabels=new Text[3];float[] xs={1038,904,1194};string[] keys={"ammoicon_ammo_mg","ammoicon_ammo_cannon","ammoicon_ammo_mortar"};
            for(int i=0;i<3;i++){var icon=StaticImage(hud,"Ammo Icon "+i,keys[i],xs[i],982,55,60);icon.preserveAspect=true;ammoLabels[i]=Label(hud,"Ammo "+i,"",xs[i]+55,982,75,60,30);}
            bossPanel=Rect(hud,"Boss HP Comic Frame",480,248,960,73);bossPanel.gameObject.SetActive(false);
            var shadow=StaticImage(bossPanel,"Black Offset","",120,12,833,55,Color.black);shadow.gameObject.AddComponent<ComicSkew>().slope=.23f;
            var frame=StaticImage(bossPanel,"White Outline","",145,8,800,48,Color.white);frame.gameObject.AddComponent<ComicSkew>().slope=.2679f;
            var track=StaticImage(bossPanel,"Track","",151,14,788,36,new Color(.5f,.3f,.15f));track.gameObject.AddComponent<ComicSkew>().slope=.2679f;
            bossFill=StaticImage(bossPanel,"Blood","",151,14,788,36,new Color(1,.392f,.42f));bossFill.gameObject.AddComponent<ComicSkew>().slope=.2679f;
            var burst=Rect(bossPanel,"Boss Comic Burst",-50,-42,250,155).gameObject.AddComponent<NativeComicBurst>();burst.color=Color.white;
            var title=ComicLabel(bossPanel,"BOSS","BOSS",-10,-8,170,80,49);title.rectTransform.localRotation=Quaternion.Euler(0,0,6);
            modal=Rect(transform,"Choice and Pause",0,0,1920,1080);modal.gameObject.SetActive(false);
            cardCatalog=JsonUtility.FromJson<Cards>(art.cards.text).cards;
            designLayout=JsonUtility.FromJson<LayoutRoot>(art.layout.text);
        }
        void Counter(Transform parent,float x,float y,string key,out Text label)
        {
            StaticImage(parent,"Resource Frame "+key,"counter_counter_frame",x+45,y+2,220,48,new Color(1,1,1,.8f)).type=Image.Type.Sliced;
            var icon=StaticImage(parent,key,key,x,y-10,66,66);icon.preserveAspect=true;
            label=Label(parent,"Amount "+key,"0",x+60,y,190,51,32,null,TextAnchor.MiddleRight);
        }
        void BuildRunLevelHUD()
        {
            // .tank-level-panel.tp-battle: cqw values use the 1920px game width.
            const float width=288,height=71.04f,slope=.0699268f;
            var panel=Rect(hud,"Tank Level HUD",153.6f,36.48f,width,height);
            float centerOffset=height*slope*.5f;
            StaticImage(panel,"Panel Shadow","",centerOffset+2,3,width,height,new Color(22/255f,19/255f,30/255f,128/255f)).gameObject.AddComponent<ComicSkew>().slope=slope;
            StaticImage(panel,"Panel Border","",centerOffset,0,width,height,new Color(22/255f,19/255f,30/255f)).gameObject.AddComponent<ComicSkew>().slope=slope;
            StaticImage(panel,"Panel Background","",centerOffset+2-2*slope,2,width-4,height-4,new Color(39/255f,35/255f,50/255f,220/255f)).gameObject.AddComponent<ComicSkew>().slope=slope;
            float contentX=14.48f;
            runLevel=Label(panel,"Tank Run Level","TANK LV. 1",contentX+slope*(height*.5f-8.72f),8.72f,259.04f,24,24,Color.white,TextAnchor.UpperLeft);
            runLevel.fontStyle=FontStyle.BoldAndItalic;runLevel.gameObject.AddComponent<ComicSkew>().slope=slope;
            runProgress=Label(panel,"Tank EXP Detail","EXP 0 / 100",contentX+slope*(height*.5f-37.52f),37.52f,259.04f,12.48f,12,new Color(224/255f,216/255f,237/255f),TextAnchor.UpperLeft);
            runProgress.gameObject.AddComponent<ComicSkew>().slope=slope;
            float railX=contentX+slope*(height*.5f-55.76f);
            StaticImage(panel,"Tank EXP Rail","",railX,55.76f,259.04f,5.76f,new Color(18/255f,16/255f,28/255f)).gameObject.AddComponent<ComicSkew>().slope=slope;
            runExperience=StaticImage(panel,"Tank EXP Progress","",railX,55.76f,0,5.76f,new Color(110/255f,231/255f,215/255f));runExperience.gameObject.AddComponent<ComicSkew>().slope=slope;
        }
        void UpdateRunLevelHUD()
        {
            var profile=new PlayerTankProfile(config);bool temporary=Simulation.EffectiveLevel>profile.Level;
            runLevel.text="TANK LV. "+(temporary?Simulation.EffectiveLevel:profile.Level);
            runProgress.text=temporary?"RUN LEVEL · BASE LV. "+profile.Level:"EXP "+profile.IntoLevel+" / "+profile.Cost(profile.Level);
            runExperience.rectTransform.sizeDelta=new Vector2(259.04f*Mathf.Clamp01((float)profile.IntoLevel/profile.Cost(profile.Level)),5.76f);
        }
        void UpdateDesign()
        {
            var s=Simulation;
            UpdateCrewNotice();
            AnimateChest();
            upgradeMessage.gameObject.SetActive(s.phase=="upgrade"||s.effects.Any(f=>f.clip=="UpgradeAura"));
            hpLabel.text=Mathf.CeilToInt(s.player.hp)+" / "+Mathf.CeilToInt(s.player.maxHP)+" HP";float ratio=Mathf.Clamp01(s.player.hp/Mathf.Max(1,s.player.maxHP));hpFill.fillAmount=ratio;
            if(ratio<lastHP){chip=Mathf.Max(chip,lastHP);chipHold=s.time+.18f;}else if(ratio>lastHP)chip=ratio;
            if(!paused&&s.time>=chipHold)chip=Mathf.MoveTowards(chip,ratio,Time.unscaledDeltaTime*.7f);lastHP=ratio;hpChip.fillAmount=Mathf.Max(chip,ratio);
            hpFill.color=ratio<=.25f?new Color(1,.396f,.36f):ratio<=.5f?new Color(1,.831f,.231f):new Color(.329f,.863f,.569f);
            moneyLabel.text=game.Wallet.Parts.ToString("N0");gearsLabel.text=game.Wallet.PetFood.ToString("N0");
            for(int i=0;i<3;i++){ammoLabels[i].text=s.ammo[i].ToString();ammoLabels[i].color=!s.SlotUnlocked(i)||!s.player.weapons[i]||!s.HasAssignedOperator(i)?Color.gray:s.ammo[i]<=0?Color.red:Color.white;}
            routeTimeline.Render(s,paused);
            bossPanel.gameObject.SetActive(s.boss!=null&&!s.boss.dead);if(s.boss!=null)bossFill.rectTransform.sizeDelta=new Vector2(788*Mathf.Clamp01(s.boss.hp/s.boss.maxHP),36);
            if(s.phase=="won"&&awardedRun!=s.runId)
            {
                var profile=new PlayerTankProfile(config);awardedTankXP=s.StageExperienceReward(profile);profile.Award(s.runId,awardedTankXP);
                if(s.rescuedMember!=null)game.Crew.QueueCommonRecruit(s.rescuedMember,s.runId,s.stageIndex);
                awardedRun=s.runId;
            }
            UpdateRunLevelHUD();
        }
        void RenderDesign(){nativeWorld.Draw(Simulation,paused?0:Time.unscaledDeltaTime);}
        CrewBitmapText ComicLabel(Transform parent,string name,string value,float x,float y,float width,float height,float size,bool outlined=true)
        {
            var text=Rect(parent,name,x,y,width,height).gameObject.AddComponent<CrewBitmapText>();text.atlas=art.comicFontAtlas;text.metrics=art.comicFontMetrics;text.fontSize=size;text.centered=true;text.color=Color.white;text.Value=value;text.raycastTarget=false;
            if(outlined){var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.09f,.075f,.11f);outline.effectDistance=new Vector2(3,-3);}return text;
        }
        void EncounterDesign()
        {
            var s=Simulation;
            if(s.encounter=="rescue"){RescueDesign();return;}
            var layout=s.encounter=="ammo"?designLayout.cards.ammo:s.encounter=="supply"?designLayout.cards.supply:s.encounter=="medical"?designLayout.cards.medical:designLayout.cards.wreck;
            var titleRect=layout.title.rect1920x1080;var optionsRect=layout.options.rect1920x1080;var skipRect=layout.skip.rect1920x1080;
            string title=s.encounter=="ammo"?"CHOOSE YOUR WEAPON":s.encounter=="supply"?"RESTOCK & ROLL":s.encounter=="medical"?"PATCH UP OR STOCK UP":"TIME FOR A NEW RIDE?";
            string eyebrow=s.encounter=="ammo"?"MILITARY CAMP":s.encounter=="supply"?"SUPPLY STOP":s.encounter=="medical"?"MEDICAL STOP":"ABANDONED TANK";
            string subtitle=s.encounter=="ammo"?"Replace one weapon in its matching slot.":s.encounter=="supply"?"Choose one weapon to resupply.":s.encounter=="medical"?"Restore your health or collect spare parts.":"Swap tanks or keep your current ride.";
            Label(modal,"Event Eyebrow",eyebrow,titleRect[0],titleRect[1]-24,titleRect[2],25,15,new Color(.96f,.85f,.46f),TextAnchor.MiddleCenter);
            ComicLabel(modal,"Event Title",title,titleRect[0],titleRect[1],titleRect[2],titleRect[3],61);
            Label(modal,"Event Subtitle",subtitle,titleRect[0],235,titleRect[2],30,19,new Color(.86f,.84f,.93f),TextAnchor.MiddleCenter);
            string[] ids=s.encounter=="ammo"?new[]{"cannon_ap","mortar_blast","mg_plasma"}:s.encounter=="supply"?new[]{"mg","cannon","mortar"}:s.encounter=="medical"?new[]{"heal","gears"}:new[]{"swap","keep"};
            float gap=55.68f,width=(optionsRect[2]-gap*(ids.Length-1))/ids.Length,height=optionsRect[3],start=optionsRect[0];
            for(int i=0;i<ids.Length;i++)
            {
                string choice=ids[i];var data=cardCatalog.FirstOrDefault(c=>c.id==choice&&c.group==s.encounter);
                if(choice=="swap"){string id=s.offeredSkin=="base"?s.offeredWeapons[0]?"base-cannon-mg":s.offeredWeapons[2]?"base-cannon-mortar":"base-cannon":s.offeredSkin;data=cardCatalog.FirstOrDefault(c=>c.id==id);}
                if(choice=="keep")data=cardCatalog.FirstOrDefault(c=>c.id=="keep");
                string rarity=data?.rarity??"common",name=data?.name??(choice=="swap"?"SCOUT TANK":choice=="keep"?"KEEP MY TANK":choice.ToUpperInvariant());
                float x=start+i*(width+gap),y=optionsRect[1];
                var root=Rect(modal,"Card "+choice,x,y,width,height);var bg=StaticImage(root,"Base","card_base_"+rarity,height*.0524078f*.5f,0,width,height);bg.gameObject.AddComponent<ComicSkew>().slope=.0524078f;bg.raycastTarget=true;
                var button=root.gameObject.AddComponent<Button>();button.targetGraphic=bg;button.onClick.AddListener(()=>ChooseDesign(choice));
                var colors=button.colors;colors.disabledColor=new Color(.62f,.62f,.62f,1);button.colors=colors;
                var motion=root.gameObject.AddComponent<HtmlChoiceCardMotion>();motion.Initialize(button,bg);
                var rarityText=Label(root,"Rarity",rarity=="ad"?"REWARDED AD":rarity.ToUpperInvariant(),30,17,width-60,24,14,new Color(.16f,.13f,.2f));rarityText.fontStyle=FontStyle.BoldAndItalic;
                StaticImage(root,"Info Badge","card_info_badge",width-22,-16,53,53);Label(root,"Info","i",width-22,-17,53,53,34,Color.white,TextAnchor.MiddleCenter).fontStyle=FontStyle.BoldAndItalic;
                if(choice=="swap"||choice=="keep")TankPreview(root,choice=="swap"?s.offeredSkin:s.player.skin,choice=="swap"?s.offeredWeapons:s.player.weapons,20,70,width-40,210);
                else {var icon=StaticImage(root,"Illustration",choice=="gears"?"counter_gear":"card_"+(data?.icon??choice),20,50,width-40,245);icon.preserveAspect=true;if(s.encounter=="ammo"&&choice!="mortar_blast"){icon.rectTransform.pivot=Vector2.one*.5f;icon.rectTransform.anchoredPosition=new Vector2(width*.5f,-172.5f);icon.rectTransform.localRotation=Quaternion.Euler(0,0,14);}}
                ComicLabel(root,"Name",data?.nameLines!=null?string.Join("\n",data.nameLines):name,12,285,width-24,90,41);
                var tag=Label(root,"Tag",data?.tag??"",12,373,width-24,35,17,new Color(.2f,.35f,.47f),TextAnchor.MiddleCenter);tag.fontStyle=FontStyle.BoldAndItalic;
                Label(root,"Description",data?.copy??"Keep your crew and reserves.",25,420,width-50,65,22,new Color(.14f,.12f,.18f),TextAnchor.MiddleCenter);
                var action=StaticImage(root,"Action Plate","card_action_plate",32,515,width-64,48);action.type=Image.Type.Sliced;
                var actionLabel=Label(root,"Action",data?.action??"SELECT",37,516,width-74,43,20,Color.white,TextAnchor.MiddleCenter);actionLabel.fontStyle=FontStyle.BoldAndItalic;
                Label(root,"Action Arrow",">",width-72,516,32,43,32,new Color(.2f,.65f,1),TextAnchor.MiddleCenter);
                int wi=choice=="cannon_ap"?1:choice=="mortar_blast"?2:choice=="mg_plasma"?0:Array.FindIndex(config.weapons,w=>w.id==choice);
                bool enabled=wi<0||s.SlotUnlocked(wi)&&s.player.weapons[wi];if(choice=="heal")enabled=s.player.hp<s.player.maxHP;
                if(choice=="mg_plasma")enabled&=config.rewardedAdsEnabled&&RequestRewardedAd!=null;
                button.interactable=enabled&&!adPending;
                if(!enabled){motion.opacity=.7f;actionLabel.text="UNAVAILABLE";}
                if(data?.ad==true){StaticImage(root,"AD Badge","",45,529,47,17,new Color(1,.88f,.33f));Label(root,"AD","AD",45,526,47,22,13,new Color(.13f,.08f,.17f),TextAnchor.MiddleCenter);}
            }
            Label(modal,"Event Hint","Choose one option to continue your journey.",s.encounter=="ammo"?377.966f:333.117f,916,1020,35,16,new Color(.77f,.74f,.83f));
            var skip=Button(modal,"Skip","SKIP",skipRect[0],skipRect[1],skipRect[2],skipRect[3],()=>ChooseDesign("skip"),"");skip.image.color=new Color(.96f,.933f,.89f);
        }
        void ChooseDesign(string choice)
        {
            if(adPending)return;
            if(choice=="mg_plasma")
            {
                if(!config.rewardedAdsEnabled||RequestRewardedAd==null)return;
                adPending=true;int ticket=++adTicket;var run=Simulation;modalKey="";
                bool completed=false;
                RequestRewardedAd(success=>{if(completed||!this||ticket!=adTicket||Simulation!=run)return;completed=true;adPending=false;if(success)run.Choose(choice,true);modalKey="";});return;
            }
            if(Simulation.Choose(choice))modalKey="";
        }
    }
    public sealed class ComicSkew : BaseMeshEffect
    {
        public float slope;
        public override void ModifyMesh(VertexHelper vh)
        {if(!IsActive())return;var v=new UIVertex();for(int i=0;i<vh.currentVertCount;i++){vh.PopulateUIVertex(ref v,i);v.position.x+=v.position.y*slope;vh.SetUIVertex(v,i);}}
    }
}



