using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    public sealed class HtmlBattleScreen : MonoBehaviour
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
            t.alignment=alignment;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;return t;
        }
        Button Button(Transform p,string name,string text,float x,float y,float w,float h,Action action,string sprite="ui_button")
        {
            var image=StaticImage(p,name,sprite,x,y,w,h);image.raycastTarget=true;var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>action());
            Label(b.transform,"Label",text,8,4,w-16,h-8,24,new Color(.13f,.14f,.18f),TextAnchor.MiddleCenter);return b;
        }
        void Build()
        {
            var backdrop=StaticImage(transform,"Battle Input Blocker","",0,0,1920,1080,Color.black);backdrop.raycastTarget=true;
            world=Rect(transform,"Layered Battlefield",0,0,1920,1080);world.gameObject.AddComponent<RectMask2D>();
            hud=Rect(transform,"Battle HUD",0,0,1920,1080);
            Panel(hud,"Stage",28,25,338,93);stageLabel=Label(hud,"Stage Name","",45,33,307,75,23);
            Panel(hud,"Progress",398,25,200,93);progressLabel=Label(hud,"Progress Text","",411,33,175,75,23,null,TextAnchor.MiddleCenter);
            Panel(hud,"Hull",632,25,285,93);hpLabel=Label(hud,"Hull Text","TANK",650,32,250,30,22);
            StaticImage(hud,"Hull Empty","",650,80,250,13,new Color(.08f,.12f,.16f));hpFill=StaticImage(hud,"Hull Fill","",650,80,250,13,new Color(.22f,.88f,.7f));
            StaticImage(hud,"Money","ui_resource_money",1003,29,70,70);Panel(hud,"Money Panel",1084,40,250,48);moneyLabel=Label(hud,"Money Amount","0",1095,42,225,43,30,null,TextAnchor.MiddleRight);
            StaticImage(hud,"Gears","ui_resource_gears",1350,29,70,70);Panel(hud,"Gears Panel",1430,40,235,48);gearsLabel=Label(hud,"Gears Amount","0",1440,42,210,43,30,null,TextAnchor.MiddleRight);
            Button(hud,"Pause","Ⅱ",1700,25,75,85,()=>{paused=!paused;modalKey="";});
            Button(hud,"Main","MAIN",1790,25,105,85,()=>{leaving=true;paused=true;modalKey="";});
            ammoLabels=new Text[3];string[] names={"MG","CANNON","MORTAR"};
            for(int i=0;i<3;i++){Panel(hud,"Ammo "+i,230+i*185,991,175,70);ammoLabels[i]=Label(hud,"Ammo Count "+i,names[i],242+i*185,995,150,62,24);}
            Panel(hud,"Ammo Type",28,991,185,70);phaseLabel=Label(hud,"Ammo Type Text","STANDARD",40,998,160,52,19);
            modal=Rect(transform,"Choice and Pause",0,0,1920,1080);modal.gameObject.SetActive(false);
        }
        void Panel(Transform p,string name,float x,float y,float w,float h)=>StaticImage(p,name,"",x,y,w,h,new Color(.13f,.15f,.21f,.95f));
        void StartStage(int index)
        {
            stageIndex=Mathf.Clamp(index,0,config.stages.Length-1);Simulation=new HtmlBattleSimulation(config,stageIndex);paused=false;leaving=false;creditedMoney=creditedGears=0;modalKey="";
            CrewRole[] weaponRoles={CrewRole.SubGunner,CrewRole.Gunner,CrewRole.WeaponSpecialist};
            for(int i=0;i<3;i++)
            {
                var member=game.Crew.Get(weaponRoles[i]);if(member==null)continue;var state=game.Progression.Get(member);
                Simulation.weaponDamageBonus[i]=member.stats.damageBonus.AtLevel(state.Level,state.Stars);
                Simulation.weaponReloadBonus[i]=member.stats.reloadSpeed.AtLevel(state.Level,state.Stars);
            }
            var driver=game.Crew.Get(CrewRole.Driver);
            if(driver!=null){var state=game.Progression.Get(driver);Simulation.player.hp=Simulation.player.maxHP=config.playerHP+driver.stats.hp.AtLevel(state.Level,state.Stars);Simulation.driverSpeedBonus=driver.stats.speed.AtLevel(state.Level,state.Stars);}
            Simulation.fighters.RemoveAll(f=>game.Crew.Get(f.role=="mg"?CrewRole.SubGunner:f.role=="cannon"?CrewRole.Gunner:f.role=="mortar"?CrewRole.WeaponSpecialist:f.role=="driver"?CrewRole.Driver:CrewRole.Commando)==null);
            modal.gameObject.SetActive(false);Render();
        }
        void Update()
        {
            if(Simulation==null)return;
            if(Input.GetKeyDown(KeyCode.Escape)){paused=!paused;leaving=false;modalKey="";}
            if(!paused)Simulation.Tick(Time.unscaledDeltaTime);
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
            var s=Simulation;stageLabel.text="STAGE "+(stageIndex+1)+"\n"+s.stage.title;
            progressLabel.text="QUÁI "+s.kills+" / "+s.stage.total+"\n"+(s.bossSpawned?"BOSS":s.phase=="approach"?"TỚI ĐIỂM DỪNG":s.phase=="depart"?"ĐI TIẾP":"WAVE "+s.wave+" / "+config.wavesPerStage);
            hpLabel.text="TANK                  "+Mathf.CeilToInt(s.player.hp);hpFill.rectTransform.sizeDelta=new Vector2(250*Mathf.Clamp01(s.player.hp/s.player.maxHP),13);
            moneyLabel.text=s.money.ToString("N0");gearsLabel.text=s.gears.ToString("N0");
            for(int i=0;i<3;i++){ammoLabels[i].text=config.weapons[i].id.ToUpperInvariant()+"  "+s.ammo[i];ammoLabels[i].color=!s.player.weapons[i]?Color.gray:s.ammo[i]<=0?new Color(1,.4f,.3f):Color.white;}
            phaseLabel.text=s.ammoType.ToUpperInvariant();
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
            imageCursor=rawCursor=0;var s=Simulation;var bg=art.data.backgrounds.First(b=>b.id==s.stage.background);
            for(int layer=0;layer<bg.keys.Length;layer++)
            {
                if(s.stage.background=="parallax_bg"&&layer>=5)continue;
                DrawBackground(bg.keys[layer],layer==0?0:layer==1?.12f:layer==2?.3f:1);
            }
            foreach(var c in s.corpses)DrawActor(c.actor,true,Mathf.Clamp01(1-c.age/10));
            if(s.phase=="approach"||s.phase=="choice"||s.phase=="depart")
            {
                if(s.encounter=="wreck")DrawActor(new HtmlBattleSimulation.Actor{kind="parked",skin=s.offeredSkin,x=s.landmarkX,y=config.groundY,width=config.playerWidth,weapons=s.offeredWeapons},false);
                else {DrawSprite("tent",s.landmarkX-230,config.groundY-300,460,300);if(s.encounter=="medical"){DrawSprite("",s.landmarkX-28,config.groundY-225,56,18,Color.red);DrawSprite("",s.landmarkX-9,config.groundY-244,18,56,Color.red);}}
            }
            DrawActor(s.player,false);
            foreach(var e in s.enemies.Where(e=>!e.dead))DrawActor(e,true);
            foreach(var f in s.fighters.Where(f=>!f.aboard))
            {
                string prefix=f.skin=="fat"?"fat":"cat";string motion=f.dead?"death":f.target!=null?(f.attack>config.meleeCooldown-.9f?"attack":"ready"):"walk";
                DrawClip(prefix+"_"+motion,s.time+f.attack,f.x,f.y,.24f,f.target!=null&&f.target.x<f.x?-1:1,f.dead?.5f:1);
            }
            foreach(var b in s.bullets)
            {
                string key=b.weapon=="mg"?"":"ammo_"+b.weapon;float w=b.weapon=="mg"?28:b.weapon=="cannon"?78:92,h=b.weapon=="mg"?8:w*.45f;
                float angle=-Mathf.Atan2(b.ty-b.sy,b.tx-b.sx)*Mathf.Rad2Deg;DrawSprite(key,b.x,b.y,w,h,b.weapon=="mg"?new Color(1,.85f,.3f):Color.white,1,angle);
            }
            foreach(var fx in s.effects)
            {
                if(clips.TryGetValue(fx.clip,out var clip))DrawClip(fx.clip,fx.age,fx.x,fx.y,fx.size/Mathf.Max(1,clip.cell[0]),1,Mathf.Clamp01(1-fx.age/fx.life),fx.clip=="GroundFire");
            }
            if(s.stage.background=="parallax_bg")for(int i=5;i<bg.keys.Length;i++)DrawBackground(bg.keys[i],i==5?1.15f:1.3f);
            for(int i=imageCursor;i<images.Count;i++)images[i].gameObject.SetActive(false);for(int i=rawCursor;i<raws.Count;i++)raws[i].gameObject.SetActive(false);
            world.anchoredPosition=s.shake>0?new Vector2(Mathf.Sin(s.time*99)*s.shake,Mathf.Cos(s.time*111)*s.shake):Vector2.zero;
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
            if(key==modalKey)return;modalKey=key;if(key==""){modal.gameObject.SetActive(false);return;}ClearModal();
            var dim=StaticImage(modal,"Backdrop","",0,0,1920,1080,new Color(0,0,0,.55f));dim.raycastTarget=true;
            if(leaving||paused||s.Terminal)
            {
                StaticImage(modal,"Panel","ui_panel",500,230,920,600);
                Label(modal,"Title",leaving?"VỀ MÀN CHÍNH?":paused?"TẠM DỪNG":s.phase=="won"?"HOÀN THÀNH STAGE":"THẤT BẠI",560,275,800,90,44,new Color(.15f,.15f,.2f),TextAnchor.MiddleCenter);
                if(s.phase=="won")Label(modal,"Chest","RƯƠNG ĐÃ MỞ\n+"+config.chestMoney+" TIỀN     +"+config.chestGears+" BÁNH RĂNG",600,390,720,125,30,new Color(.2f,.2f,.25f),TextAnchor.MiddleCenter);
                else if(s.phase=="lost")Label(modal,"Summary","Đã hạ "+s.kills+" / "+s.stage.total+" quái\nWave "+s.wave+" / "+config.wavesPerStage,600,390,720,100,30,new Color(.2f,.2f,.25f),TextAnchor.MiddleCenter);
                Button(modal,"Primary",leaving?"TIẾP TỤC CHƠI":paused?"TIẾP TỤC":s.phase=="won"&&stageIndex+1<config.stages.Length?"STAGE TIẾP THEO":"CHƠI LẠI",680,590,560,80,()=>{if(paused){paused=false;leaving=false;modalKey="";}else StartStage(s.phase=="won"&&stageIndex+1<config.stages.Length?stageIndex+1:stageIndex);});
                Button(modal,"Home","MÀN CHÍNH",680,690,560,70,Exit);return;
            }
            DrawEncounter();
        }
        void DrawEncounter()
        {
            var s=Simulation;StaticImage(modal,"Encounter Panel","ui_panel",265,180,1390,740);
            string title=s.encounter=="ammo"?"LỀU QUÂN DỤNG":s.encounter=="supply"?"TIẾP TẾ QUÂN DỤNG":s.encounter=="medical"?"TRẠM Y TẾ":"CHIẾC TANK BỎ LẠI";
            Label(modal,"Kicker","ĐIỂM DỪNG · SAU WAVE "+s.wave.ToString("00"),300,208,1100,40,20,new Color(.4f,.35f,.25f));
            Label(modal,"Encounter Title",title,300,252,1100,65,43,new Color(.16f,.16f,.2f));
            Label(modal,"Description",s.encounter=="wreck"?"Đổi xe hoặc giữ xe hiện tại. Giữ nguyên đội mèo và đạn dự trữ.":"Chỉ chọn một phương án. Lựa chọn có hiệu lực trong lượt chơi.",300,322,1200,50,24,new Color(.25f,.24f,.24f));
            string[] choices=s.encounter=="ammo"?new[]{"piercing","explosive","plasma"}:s.encounter=="supply"?new[]{"mg","cannon","mortar"}:s.encounter=="medical"?new[]{"heal","gears"}:new[]{"swap","keep"};
            for(int i=0;i<choices.Length;i++)
            {
                string choice=choices[i];float x=300+i*450,w=425;
                StaticImage(modal,"Choice "+i,choice=="plasma"?"ui_card_rare":"ui_card",x,410,w,405);
                string icon=choice=="swap"?"tank_"+s.offeredSkin+"_07_body_static":choice=="keep"?"tank_"+s.player.skin+"_07_body_static":"ui_"+choice;
                if(choice=="swap"||choice=="keep")TankPreview(modal,choice=="swap"?s.offeredSkin:s.player.skin,choice=="swap"?s.offeredWeapons:s.player.weapons,x+120,430,160,165);
                else StaticImage(modal,"Choice Icon "+i,icon,x+110,438,180,150);
                string name=choice=="piercing"?"Đạn xuyên giáp":choice=="explosive"?"Đạn nổ mạnh":choice=="plasma"?"Đạn plasma · AD":choice=="heal"?"Sửa chữa tank":choice=="gears"?"Lấy bánh răng":choice=="swap"?art.Profile(s.offeredSkin).name:choice=="keep"?"Giữ xe hiện tại":"Đạn "+choice.ToUpperInvariant();
                Label(modal,"Choice Name "+i,name,x+22,599,w-44,55,28,new Color(.14f,.15f,.2f));
                string detail=choice=="piercing"?"Sát thương +"+((config.piercingMultiplier-1)*100).ToString("0")+"%":choice=="explosive"?"Sát thương +"+((config.explosiveMultiplier-1)*100).ToString("0")+"% và nổ lan":choice=="plasma"?"Cần quảng cáo thưởng được kết nối":choice=="heal"?"Hồi tối đa "+config.medicalHP+" HP":choice=="gears"?"+"+config.medicalGears+" bánh răng":choice=="swap"?"Vũ khí: "+string.Join(" + ",config.weapons.Where((g,j)=>s.offeredWeapons[j]).Select(g=>g.id.ToUpperInvariant())):choice=="keep"?Mathf.CeilToInt(s.player.hp)+" / "+s.player.maxHP+" HP":"+"+s.stage.supply[Array.FindIndex(config.weapons,g=>g.id==choice)]+" viên";
                Label(modal,"Choice Detail "+i,detail,x+22,657,w-44,65,21,new Color(.3f,.3f,.32f));
                var button=Button(modal,"Choose "+choice,choice=="swap"?"ĐỔI XE":choice=="keep"?"GIỮ XE":"CHỌN",x+24,734,w-48,57,()=>Choose(choice));
                bool enabled=choice!="plasma"||config.rewardedAdsEnabled&&RequestRewardedAd!=null;
                if(s.encounter=="supply")enabled=s.player.weapons[Array.FindIndex(config.weapons,g=>g.id==choice)];
                if(choice=="heal")enabled=s.player.hp<s.player.maxHP;button.interactable=enabled;
                if(choice=="plasma")StaticImage(modal,"Ad Badge","ui_ad_badge",x+w-80,424,60,36);
            }
            Button(modal,"Skip","BỎ QUA",1390,838,215,55,()=>Choose("skip"));
        }
        void Choose(string choice)
        {
            if(choice=="plasma"){if(config.rewardedAdsEnabled&&RequestRewardedAd!=null)RequestRewardedAd(success=>{if(success&&this!=null)Simulation.Choose(choice,true);modalKey="";});return;}
            if(Simulation.Choose(choice))modalKey="";
        }
        void TankPreview(Transform parent,string skin,bool[] weapons,float x,float y,float w,float h)
        {
            var profile=art.Profile(skin);var root=Rect(parent,"Tank Preview "+skin,x,y,w,h);
            foreach(var part in profile.parts.OrderBy(p=>p.order))
            {
                int wi=part.name.StartsWith("06_")?1:part.name.StartsWith("08_")?0:part.name.StartsWith("09_")||part.name.StartsWith("10_")?2:-1;
                if(wi>=0&&!weapons[wi])continue;
                StaticImage(root,part.name,part.key,part.position[0]/1351*w,part.position[1]/1277*h,part.size[0]/1351*w,part.size[1]/1277*h);
            }
        }
        public void Exit(){CreditLoot();Destroy(gameObject);}
    }
}
