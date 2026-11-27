using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    public sealed partial class HtmlBattleScreen : MonoBehaviour
    {
        public static Action<Action<bool>> RequestRewardedAd;
        public static bool Available=>Resources.Load<HtmlBattleArt>("HtmlBattleArt")!=null;
        public static HtmlBattleConfig ReadConfig()=>HtmlBattleConfig.Load();
        public HtmlBattleSimulation Simulation{get;private set;}
        BattleGame game;HtmlBattleArt art;HtmlBattleConfig config;RectTransform world,hud,modal;
        readonly List<Image> images=new List<Image>();readonly List<RawImage> raws=new List<RawImage>();
        readonly Dictionary<string,HtmlArtImage> artwork=new Dictionary<string,HtmlArtImage>();
        readonly Dictionary<string,HtmlArtClip> clips=new Dictionary<string,HtmlArtClip>();
        int imageCursor,rawCursor,stageIndex,creditedMoney,creditedGears;bool paused,leaving;
        Text stageLabel,progressLabel,hpLabel,moneyLabel,gearsLabel,phaseLabel;Text[] ammoLabels;
        Image hpFill;string modalKey="";Font font;
        public static HtmlBattleScreen Begin(BattleGame game,int stage)
        {
            var root=new GameObject("HTML Reference Battle",typeof(RectTransform));var r=(RectTransform)root.transform;
            r.SetParent(game.UI.GetComponent<UICanvasLayout>().safeArea,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.sizeDelta=new Vector2(1920,1080);
            var screen=root.AddComponent<HtmlBattleScreen>();screen.game=game;screen.art=Resources.Load<HtmlBattleArt>("HtmlBattleArt");screen.config=ReadConfig();
            foreach(var a in screen.art.data.images)screen.artwork[a.key]=a;foreach(var c in screen.art.data.clips)screen.clips[c.id]=c;
            screen.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");screen.Build();screen.StartStage(stage);return screen;
        }
        RectTransform Rect(Transform p,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer)).GetComponent<RectTransform>();r.SetParent(p,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        Sprite Sprite(string key)=>artwork.TryGetValue(key,out var a)?a.sprite:null;
        Image StaticImage(Transform p,string name,string key,float x,float y,float w,float h,Color? color=null)
        {
            var image=Rect(p,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=Sprite(key);image.color=color??Color.white;image.raycastTarget=false;return image;
        }
        Text Label(Transform p,string name,string value,float x,float y,float w,float h,int size=26,Color? color=null,TextAnchor alignment=TextAnchor.MiddleLeft)
        {
            var t=Rect(p,name,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.fontStyle=FontStyle.Bold;t.color=color??Color.white;
            t.alignment=alignment;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;return t;
        }
        Button Button(Transform p,string name,string text,float x,float y,float w,float h,Action action,string sprite="ui_button")
        {
            var image=StaticImage(p,name,sprite,x,y,w,h);image.raycastTarget=true;var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>action());
            Label(b.transform,"Label",text,8,4,w-16,h-8,24,new Color(.13f,.14f,.18f),TextAnchor.MiddleCenter);return b;
        }
        void Build()
        {
            BuildDesign();
        }
        void Panel(Transform p,string name,float x,float y,float w,float h)=>StaticImage(p,name,"",x,y,w,h,new Color(.13f,.15f,.21f,.95f));
        void StartStage(int index)
        {
            stageIndex=Mathf.Clamp(index,0,config.stages.Length-1);Simulation=new HtmlBattleSimulation(config,stageIndex);paused=false;leaving=false;creditedMoney=creditedGears=0;modalKey="";
            Simulation.Geometry=art;
            CrewRole[] weaponRoles={CrewRole.SubGunner,CrewRole.Gunner,CrewRole.WeaponSpecialist};
            for(int i=0;i<3;i++)
            {
                var member=game.Crew.Get(weaponRoles[i]);if(member==null)continue;var state=game.Progression.Get(member);
                Simulation.weaponDamageBonus[i]=member.stats.damageBonus.AtLevel(state.Level,state.Stars);
                Simulation.weaponReloadBonus[i]=member.stats.reloadSpeed.AtLevel(state.Level,state.Stars);
            }
            var driver=game.Crew.Get(CrewRole.Driver);
            // Keep the mounted gun parts from the HTML art; saved legacy loadouts never replace the mortar.
            if(driver!=null){var state=game.Progression.Get(driver);Simulation.player.hp=Simulation.player.maxHP=config.playerHP+driver.stats.hp.AtLevel(state.Level,state.Stars);Simulation.driverSpeedBonus=driver.stats.speed.AtLevel(state.Level,state.Stars);}
            EquipmentHtmlCatalog.Apply(game,Simulation,true);
            Simulation.fighters.RemoveAll(f=>game.Crew.Get(f.role=="mg"?CrewRole.SubGunner:f.role=="cannon"?CrewRole.Gunner:f.role=="mortar"?CrewRole.WeaponSpecialist:f.role=="driver"?CrewRole.Driver:CrewRole.Commando)==null);
            Simulation.ConfigureCrew(game.Crew,game.Progression);Simulation.ConfigureProfile(new PlayerTankProfile(config).Level);modal.gameObject.SetActive(false);Render();
        }
        void Update()
        {
            if(Simulation==null)return;
            if(Input.GetKeyDown(KeyCode.Escape)){paused=!paused;leaving=false;modalKey="";}
            if(!paused&&!reviewFreeze)Simulation.Tick(Time.unscaledDeltaTime);
            CreditLoot();Render();UpdateHUD();UpdateModal();
        }
        void CreditLoot()
        {
            int money=Simulation.money-creditedMoney,gears=Simulation.gears-creditedGears;
            if(money>0&&game.Wallet.TryAdd(CurrencyType.Parts,money))creditedMoney+=money;
            if(gears>0&&game.Wallet.TryAdd(CurrencyType.PetFood,gears))creditedGears+=gears;
        }
        void UpdateHUD()
        {
            UpdateDesign();
        }
        Image DrawSprite(string key,float x,float y,float w,float h,Color? tint=null,float face=1,float angle=0)
        {
            Image image;if(imageCursor<images.Count)image=images[imageCursor];else {image=StaticImage(world,"Sprite "+images.Count,key,x,y,w,h);images.Add(image);}
            imageCursor++;image.gameObject.SetActive(true);image.sprite=Sprite(key);image.color=tint??Color.white;
            var r=image.rectTransform;r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);r.localScale=new Vector3(face,1,1);r.localRotation=Quaternion.Euler(0,0,angle);r.SetAsLastSibling();return image;
        }
        void DrawClip(string name,float age,float x,float y,float scale=1,float face=1,float opacity=1,bool loop=true)
        {
            if(!clips.TryGetValue(name,out var clip)||!artwork.TryGetValue(clip.key,out var a)||a.texture==null)return;
            RawImage raw;if(rawCursor<raws.Count)raw=raws[rawCursor];else {raw=Rect(world,"Animation "+raws.Count,0,0,1,1).gameObject.AddComponent<RawImage>();raw.raycastTarget=false;raws.Add(raw);}rawCursor++;
            raw.gameObject.SetActive(true);raw.texture=a.texture;raw.color=new Color(1,1,1,opacity);
            float t=clip.duration>0?age/clip.duration:0;t=loop?Mathf.Repeat(t,1):Mathf.Clamp01(t);int frame=Math.Min(clip.frames-1,Mathf.FloorToInt(t*clip.frames));
            raw.uvRect=new Rect((float)(frame%clip.cols)/clip.cols,1f-(float)(frame/clip.cols+1)/clip.rows,1f/clip.cols,1f/clip.rows);
            float factor=scale/Mathf.Max(.01f,clip.worldScale);
            var r=raw.rectTransform;r.pivot=new Vector2(clip.pivot[0]/clip.cell[0],1-clip.pivot[1]/clip.cell[1]);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(clip.cell[0]*factor,clip.cell[1]*factor);r.localScale=new Vector3(face,1,1);r.SetAsLastSibling();
        }
        void Render()
        {
            RenderDesign();
        }
        void DrawBackground(string key,float factor)
        {
            float offset=Simulation.road*factor;int first=Mathf.FloorToInt(offset/1920);float shift=offset-first*1920;
            for(int i=0;i<2;i++){bool flip=(first+i)%2!=0;DrawSprite(key,i*1920-shift+(flip?1920:0),0,1920,1080,null,flip?-1:1);}
        }
        void DrawActor(HtmlBattleSimulation.Actor a,bool enemy,float opacity=1)
        {
            if(a.kind=="dog"){string name=a.dead?"dog_death":a.attack>.5f?"dog_attack":"dog_walk";DrawClip(name,a.age,a.x,a.y,.29f,-1,opacity);return;}
            if(a.kind=="car"||a.kind=="plane")
            {
                var parts=art.data.enemies.First(e=>e.id==a.kind).parts;float factor=a.width/(a.kind=="car"?1536:1536);
                foreach(var p in parts)
                {
                    float x=a.x-(p.position[0]-768)*factor,y=(a.kind=="plane"?a.y:a.y-90)+(p.position[1]-600)*factor;
                    DrawSprite(p.key,x,y,p.size[0]*factor,p.size[1]*factor,new Color(1,1,1,opacity),-1);
                }
                HealthBar(a,Center(a).y-110);return;
            }
            var profile=art.Profile(a.skin);float z=a.width/1351,face=enemy?-1:1;
            bool crewDrawn=false;
            foreach(var part in profile.parts.OrderBy(p=>p.order))
            {
                if(part.order>=7&&!crewDrawn){if(a.kind!="parked"&&!a.dead)DrawTankCrew(a,profile,z,enemy);crewDrawn=true;}
                int wi=part.name.StartsWith("06_")?1:part.name.StartsWith("08_")?0:part.name.StartsWith("09_")||part.name.StartsWith("10_")?2:-1;
                if(wi>=0&&!a.weapons[wi])continue;
                float bob=a.kind=="player"&&Simulation.travelSpeed>1&&Simulation.phase!="approach"&&Simulation.phase!="depart"?Mathf.Sin(Simulation.time*10)*1.6f:0;
                float x=a.x+face*(part.position[0]-652)*z,y=config.groundY+(part.position[1]-1258)*z+bob;
                var image=DrawSprite(part.key,x,y,part.size[0]*z,part.size[1]*z,new Color(1,1,1,opacity),face);
                if(part.name.Contains("wheel")&&a.kind!="parked"&&!a.dead)
                {
                    image.rectTransform.pivot=new Vector2((part.pivot[0]-part.position[0])/part.size[0],1-(part.pivot[1]-part.position[1])/part.size[1]);
                    image.rectTransform.anchoredPosition=new Vector2(a.x+face*(part.pivot[0]-652)*z,-(config.groundY+(part.pivot[1]-1258)*z+bob));image.rectTransform.localRotation=Quaternion.Euler(0,0,-face*a.wheel*.7f);
                }
                else if(wi==0||wi==1)
                {
                    var target=enemy?Simulation.player:Simulation.enemies.Where(e=>!e.dead&&e.x-a.x>=config.weapons[wi].minRange&&e.x-a.x<=config.weapons[wi].maxRange).OrderBy(e=>e.x).FirstOrDefault();
                    if(target!=null)
                    {
                        float px=a.x+face*(part.pivot[0]-652)*z,py=config.groundY+(part.pivot[1]-1258)*z;var c=Center(target);
                        float angle=Mathf.Clamp(Mathf.Atan2(c.y-py,Mathf.Abs(c.x-px)),-.85f,.55f);
                        image.rectTransform.pivot=new Vector2((part.pivot[0]-part.position[0])/part.size[0],1-(part.pivot[1]-part.position[1])/part.size[1]);image.rectTransform.anchoredPosition=new Vector2(px,-py);image.rectTransform.localRotation=Quaternion.Euler(0,0,-face*angle*Mathf.Rad2Deg);
                    }
                }
            }
            if(enemy&&!a.dead)HealthBar(a,config.groundY-a.width*.87f);
            if(a.kind=="boss"&&!a.dead)
            {DrawSprite("",a.x-a.width*.52f,config.groundY-a.width-80,a.width*1.04f,18,new Color(.15f,.04f,.05f));DrawSprite("",a.x-a.width*.52f,config.groundY-a.width-80,a.width*1.04f*Mathf.Clamp01(a.hp/a.maxHP),18,new Color(1,.2f,.25f));}
        }
        Vector2 Center(HtmlBattleSimulation.Actor a)=>Simulation.Center(a);
        void HealthBar(HtmlBattleSimulation.Actor a,float y)
        {DrawSprite("",a.x-70,y,140,10,new Color(.1f,.08f,.1f));DrawSprite("",a.x-70,y,140*Mathf.Clamp01(a.hp/Mathf.Max(1,a.maxHP)),10,new Color(1,.3f,.4f));}
        void DrawTankCrew(HtmlBattleSimulation.Actor a,HtmlTankProfile p,float scale,bool enemy)
        {
            foreach(var seat in p.seats)
            {
                int w=seat.id=="mg"?0:seat.id=="cannon"?1:seat.id=="mortar"?2:-1;
                if(enemy){if(seat.id=="mortar"||a.bossCrew||w>=0&&!a.weapons[w])continue;}
                else if(Simulation.fighters.Any(f=>f.role==seat.id&&!f.aboard))continue;
                else if(!Simulation.fighters.Any(f=>f.role==seat.id))continue;
                string clip=seat.id=="front"?(enemy?"dog_idle":"fat_idle"):enemy?(seat.id=="mg"?"dog_mg_drive":seat.id=="cannon"?"dog_pilot_drive":"dog_driver_drive"):seat.id=="mg"?"mg_drive":seat.id=="mortar"?"driver_idle":"driver_drive";
                float face=enemy?-1:1;DrawClip(clip,Simulation.time,a.x+face*(seat.x-652)*scale,config.groundY+(seat.y-1258)*scale,seat.scale*scale,face);
            }
        }
        void ClearModal(){foreach(Transform child in modal)Destroy(child.gameObject);modal.gameObject.SetActive(true);}
        void UpdateModal()
        {
            var s=Simulation;string key=leaving?"leave":paused?"pause":s.phase=="choice"?"choice:"+s.encounterIndex:s.Terminal?s.phase:"";
            if(key==modalKey){if(key=="")modal.gameObject.SetActive(false);return;}modalKey=key;if(key==""){modal.gameObject.SetActive(false);return;}ClearModal();
            var dim=StaticImage(modal,"Backdrop","",0,0,1920,1080,new Color(0,0,0,.55f));dim.raycastTarget=true;
            if(s.phase=="choice"&&!paused&&!leaving)dim.color=new Color(.11f,.07f,.18f,.76f);
            if(leaving||paused||s.Terminal)
            {
                StaticImage(modal,"Panel","",500,230,920,600,new Color(.188f,.212f,.286f));
                Label(modal,"Title",leaving?"VỀ MÀN CHÍNH?":paused?"TẠM DỪNG":s.phase=="won"?"HOÀN THÀNH STAGE":"THẤT BẠI",560,275,800,90,44,Color.white,TextAnchor.MiddleCenter);
                if(s.phase=="won")BossChest();
                else if(s.phase=="lost")Label(modal,"Summary","Đã hạ "+s.kills+" / "+s.stage.total+" quái\nWave "+s.wave+" / "+config.wavesPerStage,600,390,720,100,30,new Color(.2f,.2f,.25f),TextAnchor.MiddleCenter);
                Button(modal,"Primary",leaving?"TIẾP TỤC CHƠI":paused?"TIẾP TỤC":s.phase=="won"&&stageIndex+1<config.stages.Length?"STAGE TIẾP THEO":"CHƠI LẠI",680,s.phase=="won"?640:590,560,80,()=>{if(paused){paused=false;leaving=false;modalKey="";}else StartStage(s.phase=="won"&&stageIndex+1<config.stages.Length?stageIndex+1:stageIndex);});
                Button(modal,"Home","MÀN CHÍNH",680,s.phase=="won"?735:690,560,70,Exit);return;
            }
            DrawEncounter();
        }
        void DrawEncounter()
        {
            EncounterDesign();
        }
        void Choose(string choice)
        {
            ChooseDesign(choice);
        }
        void TankPreview(Transform parent,string skin,bool[] weapons,float x,float y,float w,float h)
        {
            var profile=art.Profile(skin);var root=Rect(parent,"Tank Preview "+skin,x,y,w,h);
            float factor=Mathf.Min(w/1351,h/1277),offsetX=(w-1351*factor)*.5f,offsetY=(h-1277*factor)*.5f;
            foreach(var part in profile.parts.OrderBy(p=>p.order))
            {
                int wi=part.name.StartsWith("06_")?1:part.name.StartsWith("08_")?0:part.name.StartsWith("09_")||part.name.StartsWith("10_")?2:-1;
                if(wi>=0&&!weapons[wi])continue;
                StaticImage(root,part.name,part.key,offsetX+part.position[0]*factor,offsetY+part.position[1]*factor,part.size[0]*factor,part.size[1]*factor);
            }
        }
        bool exiting;
        public void Exit(){if(exiting)return;exiting=true;paused=true;CreditLoot();game.UI.ReturnFromHtmlBattle(gameObject);}
    }
}


