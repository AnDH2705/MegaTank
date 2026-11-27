using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MegaTanks
{
    // Simulation coordinates match the 1920x1080 HTML reference; rendering is native uGUI.
    public sealed class HtmlBattleSimulation
    {
        public sealed class Actor
        {
            public int id;public string kind,skin="base";public float x,y,width,hp,maxHP,speed,attack,age,wheel;
            public bool dead,bossCrew;public bool[] weapons={true,true,true};public float[] cooldown=new float[3];
            public UnitDefenseStats defenses=new UnitDefenseStats();
        }
        public sealed class Fighter
        {
            public string role,skin;public float x,y,hp,attack,jump;public bool aboard=true,dead;public Actor target;
        }
        public sealed class Bullet
        {
            public string weapon;public bool enemy;public float x,y,sx,sy,tx,ty,age,life,damage;public Actor target,owner;
            public bool homing,airOnly;public Vector2 velocity;public float turnRate,lockRange,smokeTimer;
            public DamageAttack attack;
        }
        public sealed class Effect { public string clip;public float x,y,size,age,life;public bool road; }
        public sealed class Corpse { public Actor actor;public float age; }
        public readonly HtmlBattleConfig config;public readonly HtmlStageConfig stage;public readonly int stageIndex;
        public readonly Actor player;public readonly List<Actor> enemies=new List<Actor>();
        public readonly List<Fighter> fighters=new List<Fighter>();public readonly List<Bullet> bullets=new List<Bullet>();
        public readonly List<Effect> effects=new List<Effect>();public readonly List<Corpse> corpses=new List<Corpse>();
        public readonly int[] ammo;public string phase="wave",ammoType="standard",encounter="",offeredSkin="base";
        public float[] weaponDamageBonus=new float[3],weaponReloadBonus=new float[3];
        public float driverSpeedBonus;
        public GunStats MissileStats { get; private set; }
        int missileSalvo;
        public void EquipMissile(GunStats stats)
        {
            MissileStats=stats!=null&&stats.homingMissile?stats.Copy():null;missileSalvo=0;
            if(MissileStats!=null)ammo[2]=MissileStats.reserveCapacity;
        }
        public Actor MissileTarget(Vector2 origin,bool airOnly,float range)
            =>enemies.Where(e=>!e.dead&&(e.kind=="plane")==airOnly&&Vector2.Distance(origin,Center(e))<=range)
                .OrderBy(e=>(Center(e)-origin).sqrMagnitude).FirstOrDefault();
        public int wave=1,spawned,kills,money,gears,encounterIndex;public float time,road,travelSpeed,landmarkX,shake;
        public bool bossSpawned,bossKilled,recovered,chestOpened;public Actor boss;
        public bool[] offeredWeapons={true,true,true};
        readonly Queue<string> queue=new Queue<string>();readonly System.Random rng;
        float spawnTimer=1.3f,waitTimer,stalled,dustTimer;int nextId;
        public bool Terminal=>phase=="won"||phase=="lost";
        public HtmlBattleSimulation(HtmlBattleConfig c,int index,int? seed=null)
        {
            config=c;c.Validate();stageIndex=Mathf.Clamp(index,0,c.stages.Length-1);stage=c.stages[stageIndex];rng=new System.Random(seed??c.seed+stageIndex);
            player=new Actor{kind="player",x=c.playerX,y=c.groundY,width=c.playerWidth,hp=c.playerHP,maxHP=c.playerHP,speed=c.cruiseSpeed};
            ammo=(int[])stage.startingAmmo.Clone();
            var list=Enumerable.Range(0,stage.total).Select(i=>i<stage.planes?"plane":i<stage.planes+stage.dogs?"dog":"car").ToList();
            for(int i=list.Count-1;i>0;i--){int j=rng.Next(i+1);var value=list[i];list[i]=list[j];list[j]=value;}foreach(var kind in list)queue.Enqueue(kind);
            string[] roles={"mg","cannon","mortar","driver","front"};
            for(int i=0;i<5;i++)fighters.Add(new Fighter{role=roles[i],skin=i==4?"fat":"cat",x=c.playerX-100-i*30,y=c.groundY+(i%2)*25,hp=c.meleeHP});
        }
        public Vector2 Center(Actor a)=>new Vector2(a.x,a.kind=="plane"?a.y:a.kind=="dog"?a.y-65:config.groundY-a.width*(a.kind=="car"?.3f:.5f));
        public bool CanFire(int w)=>player.weapons[w]&&ammo[w]>0;
        public void Tick(float dt)
        {
            if(Terminal)return;dt=Mathf.Clamp(dt,0,.05f);time+=dt;shake=Mathf.Max(0,shake-dt*25);
            UpdateEffects(dt);
            if(phase=="choice")return;
            if(phase=="approach"||phase=="depart")
            {
                Travel(config.stationTravelSpeed,dt);landmarkX-=travelSpeed*dt;
                foreach(var f in fighters.Where(f=>!f.aboard&&!f.dead))Follow(f,dt);
                if(phase=="approach"&&landmarkX<=config.encounterStopX){landmarkX=config.encounterStopX;travelSpeed=0;phase="choice";}
                else if(phase=="depart"&&landmarkX<-600)NextWave();return;
            }
            if(phase=="break") { Travel(config.cruiseSpeed,dt);waitTimer-=dt;if(waitTimer<=0)NextWave();return; }
            if(phase=="settle")
            {
                Travel(config.cruiseSpeed,dt);foreach(var f in fighters.Where(f=>!f.aboard&&!f.dead))Follow(f,dt);
                waitTimer-=dt;if(waitTimer<=0)AfterWave();return;
            }
            float nearest=enemies.Where(e=>!e.dead&&e.kind!="plane").Select(e=>e.x-player.x).DefaultIfEmpty(2000).Min();
            bool close=nearest<600;Travel(bossSpawned||close?0:config.cruiseSpeed,dt);
            if(!bossSpawned)
            {
                int waveEnd=Mathf.CeilToInt((float)wave*stage.total/config.wavesPerStage);
                spawnTimer-=dt;if(spawned<waveEnd&&enemies.Count(e=>!e.dead)<stage.maxActive&&spawnTimer<=0){Spawn(queue.Dequeue());spawned++;spawnTimer=stage.spawnInterval;}
            }
            foreach(var e in enemies.Where(e=>!e.dead).ToArray())UpdateEnemy(e,dt);
            UpdateFighters(dt);
            for(int w=0;w<3;w++)
            {
                player.cooldown[w]-=dt;if(!CanFire(w)||player.cooldown[w]>0)continue;
                if(w==2&&MissileStats!=null)
                {
                    var lockTarget=MissileTarget(Center(player),MissileStats.preferredTarget==EnemyKind.Flyer,MissileStats.range*100);
                    if(lockTarget==null)continue;
                    FireMissile(lockTarget);ammo[w]--;
                    if(missileSalvo<=0)missileSalvo=Mathf.Max(1,MissileStats.salvoSize);
                    missileSalvo--;player.cooldown[w]=missileSalvo>0?MissileStats.shotDelay:MissileStats.EffectiveReloadTime;
                    continue;
                }
                var gun=config.weapons[w];
                var targets=enemies.Where(e=>!e.dead&&e.x-player.x>=gun.minRange&&e.x-player.x<=gun.maxRange&&e.x-e.width*.25f<1920&&(e.kind!="dog"||w==0));
                var target=(w==0?targets.OrderBy(e=>e.kind=="plane"?0:1).ThenBy(e=>e.x):targets.OrderBy(e=>e.kind=="plane"?1:0).ThenBy(e=>e.x)).FirstOrDefault();
                if(target==null)continue;
                Fire(player,target,w,false);ammo[w]--;player.cooldown[w]=gun.cooldown*(player.skin=="speed"?.82f:1)/Mathf.Max(.1f,1+weaponReloadBonus[w]/100);
            }
            UpdateBullets(dt);enemies.RemoveAll(e=>e.dead);
            if(player.hp<=0){player.hp=0;phase="lost";travelSpeed=0;return;}
            if(bossKilled&&enemies.Count==0){waitTimer+=dt;if(waitTimer>.8f)Win();return;}
            if(!bossSpawned&&spawned>=Mathf.CeilToInt((float)wave*stage.total/config.wavesPerStage)&&enemies.Count==0){phase="settle";waitTimer=.9f;bullets.Clear();return;}
            if(boss!=null&&!boss.dead&&Mathf.Abs(boss.x-player.x)<590&&ammo[0]==0)
            {
                stalled+=dt;if(stalled>15){player.hp=Mathf.Max(0,player.hp-dt*12);}
            }else stalled=0;
        }
        void Travel(float speed,float dt)
        {
            float multiplier=player.skin=="speed"?1.25f:player.skin=="heavy"?.8f:1;
            travelSpeed=Mathf.MoveTowards(travelSpeed,(speed+driverSpeedBonus)*multiplier,(phase=="approach"||phase=="depart"?480:180)*dt);
            if(speed==0)travelSpeed=0;road+=travelSpeed*dt;player.wheel+=travelSpeed*dt;
            foreach(var corpse in corpses)corpse.actor.x-=travelSpeed*dt;
            foreach(var effect in effects)if(effect.road)effect.x-=travelSpeed*dt;
            dustTimer-=dt;if(travelSpeed>5&&dustTimer<=0){AddEffect("Dust",player.x-120,config.groundY-25,130,.65f,true);dustTimer=.15f;}
        }
        public Actor Spawn(string kind)
        {
            var def=config.enemies.FirstOrDefault(e=>e.id==kind);bool isBoss=kind=="boss";
            var a=new Actor{id=++nextId,kind=kind,x=isBoss?2230:2110+rng.Next(90),y=kind=="plane"?350+rng.Next(170):config.groundY,width=isBoss?config.playerWidth:def.width,
                hp=isBoss?stage.bossHP:stageIndex>0?def.hardHP:def.hp,speed=isBoss?60:def.speed,attack=1.3f+(float)rng.NextDouble()};a.maxHP=a.hp;
            if(isBoss){a.weapons=new[]{stage.bossWeapons.Contains("mg"),stage.bossWeapons.Contains("cannon"),false};boss=a;bossSpawned=true;phase="boss";}
            enemies.Add(a);return a;
        }
        void UpdateEnemy(Actor e,float dt)
        {
            e.age+=dt;e.attack-=dt;float stop=player.x+(e.kind=="boss"?550:e.kind=="dog"?90:e.kind=="plane"?650:540);
            float movement=e.x>stop?Mathf.Min(e.x-stop,(e.speed+travelSpeed)*dt):0;e.x-=movement;e.wheel+=movement;
            if(e.kind=="plane")e.y+=Mathf.Sin(e.age*2)*dt*12;
            if(e.kind=="boss")
            {
                for(int w=0;w<2;w++)
                {
                    e.cooldown[w]-=dt;float distance=e.x-player.x;var gun=config.weapons[w];
                    if(e.weapons[w]&&e.cooldown[w]<=0&&distance>=gun.minRange&&distance<=gun.maxRange){Fire(e,player,w,true);e.cooldown[w]=w==0?.28f:2.1f;}
                }
                if(e.x-player.x<=560&&!e.bossCrew)
                {
                    e.bossCrew=true;for(int i=0;i<(stageIndex==0?3:4);i++){var dog=Spawn("dog");dog.bossCrew=true;dog.x=e.x-80-i*30;dog.hp=dog.maxHP=stageIndex==0?180:220;dog.y=config.groundY+(i%2)*24;}
                }
            }
            else if(e.kind=="dog")
            {
                var ally=fighters.Where(f=>!f.aboard&&!f.dead).OrderBy(f=>Mathf.Abs(e.x-f.x)).FirstOrDefault();
                if(ally!=null&&Mathf.Abs(ally.x-e.x)<100){if(e.attack<=0){ally.hp-=e.bossCrew?6:config.enemies.First(d=>d.id=="dog").damage;e.attack=1.05f;if(ally.hp<=0)ally.dead=true;}return;}
                if(e.x-player.x<120&&e.attack<=0){HitPlayer(e.bossCrew?6:config.enemies.First(d=>d.id=="dog").damage);e.attack=1.05f;}
            }
            else if(e.attack<=0&&e.x-player.x<config.enemies.First(d=>d.id==e.kind).range)
            {Fire(e,player,0,true);e.attack=config.enemies.First(d=>d.id==e.kind).cooldown;}
        }
        void UpdateFighters(float dt)
        {
            for(int i=0;i<fighters.Count;i++)
            {
                var f=fighters[i];if(f.dead)continue;f.attack-=dt;
                int wi=f.role=="mg"?0:f.role=="cannon"?1:f.role=="mortar"?2:-1;
                bool weaponMissing=wi>=0&&(!player.weapons[wi]||ammo[wi]<=0);
                var target=enemies.Where(e=>!e.dead&&e.kind!="plane"&&e.x-player.x<800).OrderBy(e=>e.kind=="dog"?0:1).ThenBy(e=>Mathf.Abs(e.x-f.x)).FirstOrDefault();
                bool deploy=weaponMissing||f.role=="front"&&target!=null||boss!=null&&boss.x-player.x<580&&f.role!="driver";
                if(deploy&&f.aboard){f.aboard=false;f.jump=.7f;f.x=player.x+10+i*25;f.y=config.groundY-130;}
                if(f.aboard)continue;
                if(f.jump>0){f.jump=Mathf.Max(0,f.jump-dt);f.y=Mathf.Lerp(config.groundY+(i%2)*24-110,config.groundY+(i%2)*24,1-f.jump/.7f);continue;}
                if(target!=null)
                {
                    f.target=target;float goal=target.x-(target.kind=="dog"?0:target.width*.35f)-config.meleeRange;
                    f.x=Mathf.MoveTowards(f.x,goal,config.meleeSpeed*dt);f.y=config.groundY+(i%2)*24;
                    if(Mathf.Abs(f.x-goal)<8&&f.attack<=0){Damage(target,config.meleeDamage);f.attack=config.meleeCooldown;AddEffect("Hit",target.x,target.y-70,65,.2f);}
                }
                else
                {
                    f.target=null;
                    if(!weaponMissing){f.x=Mathf.MoveTowards(f.x,player.x,config.meleeSpeed*dt);if(Mathf.Abs(f.x-player.x)<25)f.aboard=true;}
                    else Follow(f,dt);
                }
            }
        }
        void Follow(Fighter f,float dt){f.x=Mathf.MoveTowards(f.x,player.x-95-fighters.IndexOf(f)*40,config.meleeSpeed*dt);f.y=config.groundY+(fighters.IndexOf(f)%2)*24;if(f.role=="front"&&Mathf.Abs(f.x-(player.x-95-fighters.IndexOf(f)*40))<3)f.aboard=true;}
        void Fire(Actor owner,Actor target,int w,bool enemy)
        {
            var gun=config.weapons[w];var center=Center(target);int face=enemy?-1:1;
            var origin=new Vector2(owner.x+face*(w==0?220:w==1?290:-210),config.groundY-owner.width*(w==0?.8f:w==1?.56f:.83f));
            float damage=enemy?(owner.kind=="boss"?w==0?4:13:config.enemies.First(e=>e.id==owner.kind).damage):gun.damage*(1+weaponDamageBonus[w]/100)*(ammoType=="piercing"?config.piercingMultiplier:ammoType=="explosive"?config.explosiveMultiplier:ammoType=="plasma"?config.plasmaMultiplier:1);
            bullets.Add(new Bullet{weapon=gun.id,enemy=enemy,x=origin.x,y=origin.y,sx=origin.x,sy=origin.y,tx=center.x,ty=center.y,target=target,owner=owner,damage=damage,life=w==2?1.35f:Mathf.Max(.1f,Vector2.Distance(origin,center)/gun.projectileSpeed)});
            AddEffect("Muzzle",origin.x,origin.y,w==1?110:65,.12f);
            if(w==1)shake=2;
        }
        void UpdateBullets(float dt)
        {
            foreach(var b in bullets.ToArray())
            {
                if(b.homing){UpdateMissile(b,dt);continue;}
                b.age+=dt;float t=Mathf.Clamp01(b.age/b.life);b.x=Mathf.Lerp(b.sx,b.tx,t);b.y=Mathf.Lerp(b.sy,b.ty,t)-(b.weapon=="mortar"?Mathf.Sin(t*Mathf.PI)*380:0);
                if(b.age<b.life)continue;
                if(b.enemy)HitPlayer(b.damage);
                else if(b.weapon=="mortar")
                {
                    foreach(var e in enemies.Where(e=>!e.dead&&Vector2.Distance(Center(e),new Vector2(b.tx,b.ty))<config.splashRadius).ToArray())Damage(e,b.damage);
                    AddEffect("Explosion",b.tx,b.ty,195,.5f);AddEffect("GroundFire",b.tx,config.groundY,300,2.8f,true);shake=4;
                }
                else if(b.target!=null&&!b.target.dead)
                {
                    Damage(b.target,b.damage);if(b.weapon=="cannon"&&!b.target.dead){b.target.x+=config.bossKnockback;shake=4;}
                    if(ammoType=="explosive")foreach(var e in enemies.Where(e=>e!=b.target&&!e.dead&&Vector2.Distance(Center(e),Center(b.target))<config.splashRadius).ToArray())Damage(e,b.damage*config.splashMultiplier);
                }
                bullets.Remove(b);
            }
        }
        void HitPlayer(float damage){player.hp=Mathf.Max(0,player.hp-damage);shake=2;AddEffect("Hit",player.x+100,config.groundY-200,75,.16f);}
        void FireMissile(Actor target)
        {
            var origin=new Vector2(player.x-120,config.groundY-player.width*.83f);var direction=(Center(target)-origin).normalized;
            float multiplier=ammoType=="piercing"?config.piercingMultiplier:ammoType=="explosive"?config.explosiveMultiplier:ammoType=="plasma"?config.plasmaMultiplier:1;
            bullets.Add(new Bullet{weapon="missile",homing=true,airOnly=MissileStats.preferredTarget==EnemyKind.Flyer,
                x=origin.x,y=origin.y,sx=origin.x,sy=origin.y,target=target,owner=player,velocity=direction*MissileStats.projectileSpeed*100,
                turnRate=MissileStats.missileTurnRate,lockRange=MissileStats.range*100,life=MissileStats.missileLifetime,
                attack=new DamageAttack(MissileStats.damage*multiplier,MissileStats),
                damage=new DamageAttack(MissileStats.damage,MissileStats).WeaponDamage*multiplier});
            AddEffect("Muzzle",origin.x,origin.y,100,.12f);
        }
        void UpdateMissile(Bullet b,float dt)
        {
            b.age+=dt;if(b.age>=b.life){bullets.Remove(b);return;}
            float remaining=dt;
            while(remaining>0)
            {
                float step=Mathf.Min(remaining,1f/120);remaining-=step;var origin=new Vector2(b.x,b.y);
                if(b.target==null||b.target.dead||(b.target.kind=="plane")!=b.airOnly)b.target=MissileTarget(origin,b.airOnly,b.lockRange);
                if(b.target!=null)b.velocity=Projectile2D.Steer(b.velocity,Center(b.target)-origin,b.turnRate,step);
                var end=origin+(b.velocity-Vector2.right*travelSpeed)*step;
                float groundFraction=end.y>=config.groundY&&end.y>origin.y?Mathf.Clamp01((config.groundY-origin.y)/(end.y-origin.y)):1;
                end=Vector2.Lerp(origin,end,groundFraction);var delta=end-origin;Actor hit=null;float nearest=float.PositiveInfinity;
                foreach(var enemy in enemies.Where(e=>!e.dead&&(e.kind=="plane")==b.airOnly))
                {
                    float t=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(Center(enemy)-origin,delta)/delta.sqrMagnitude):0;
                    float radius=enemy.width*(enemy.kind=="plane"?.18f:enemy.kind=="dog"?.15f:.22f)+7;
                    if(Vector2.Distance(Center(enemy),origin+delta*t)<=radius&&t<nearest){hit=enemy;nearest=t;}
                }
                if(hit!=null)
                {
                    b.x=origin.x+delta.x*nearest;b.y=origin.y+delta.y*nearest;
                    var result=CombatDamage.Calculate(b.attack,hit.defenses,(float)rng.NextDouble(),(float)rng.NextDouble());
                    if(result.Hit)Damage(hit,result.Damage);
                    AddEffect("Explosion",b.x,b.y,140,.5f);bullets.Remove(b);return;
                }
                b.x=end.x;b.y=end.y;
                if(end.y>=config.groundY){AddEffect("Explosion",b.x,b.y,140,.5f);bullets.Remove(b);return;}
            }
            b.smokeTimer-=dt;if(b.smokeTimer<=0){AddEffect("Smoke",b.x,b.y,45,.35f);b.smokeTimer=.06f;}
        }
        public void Damage(Actor target,float amount)
        {
            if(target.dead)return;target.hp-=amount;AddEffect("Hit",target.x,Center(target).y,65,.15f);
            if(target.hp>0)return;target.hp=0;target.dead=true;
            if(target.kind=="boss"){bossKilled=true;money+=config.bossMoney;gears+=config.bossGears;shake=8;}
            else if(!target.bossCrew){kills++;if(target.id%5<3)money+=config.enemyMoney;else gears+=config.enemyGears;}
            corpses.Add(new Corpse{actor=target});AddEffect("Explosion",target.x,Center(target).y,target.kind=="boss"?320:140,.5f,true);
        }
        public void AddEffect(string clip,float x,float y,float size,float life,bool onRoad=false){effects.Add(new Effect{clip=clip,x=x,y=y,size=size,life=life,road=onRoad});}
        void UpdateEffects(float dt){foreach(var e in effects)e.age+=dt;effects.RemoveAll(e=>e.age>e.life);foreach(var c in corpses)c.age+=dt;corpses.RemoveAll(c=>c.age>10||c.actor.x<-500);}
        void AfterWave()
        {
            if(wave%config.wavesPerEncounter==0)
            {
                encounter=stage.requireRecovery&&encounterIndex==0?"wreck":config.encounterDeck[encounterIndex%config.encounterDeck.Length];encounterIndex++;
                offeredSkin=stage.requireRecovery&&!recovered?"base":rng.Next(2)==0?"speed":"heavy";
                offeredWeapons=stage.requireRecovery&&!recovered?new[]{true,true,false}:rng.Next(3)==0?new[]{true,true,false}:rng.Next(2)==0?new[]{false,true,true}:new[]{false,true,false};
                landmarkX=2150;phase="approach";
            }
            else {phase="break";waitTimer=config.waveBreakSeconds;}
        }
        void NextWave(){if(wave>=config.wavesPerStage){Spawn("boss");return;}wave++;phase="wave";spawnTimer=.8f;}
        public bool Choose(string choice,bool rewardedAdCompleted=false)
        {
            if(phase!="choice")return false;
            if(choice!="skip")
            {
                if(encounter=="ammo")
                {
                    if(choice!="piercing"&&choice!="explosive"&&choice!="plasma")return false;
                    if(choice=="plasma"&&!rewardedAdCompleted)return false;ammoType=choice;
                }
                else if(encounter=="supply")
                {
                    int i=Array.FindIndex(config.weapons,w=>w.id==choice);if(i<0||!player.weapons[i])return false;ammo[i]=Math.Min(i==0?999:99,ammo[i]+stage.supply[i]);
                }
                else if(encounter=="medical")
                {if(choice=="heal"){if(player.hp>=player.maxHP)return false;player.hp=Mathf.Min(player.maxHP,player.hp+config.medicalHP);}else if(choice=="gears")gears+=config.medicalGears;else return false;}
                else if(encounter=="wreck")
                {
                    if(choice!="keep"&&choice!="swap")return false;
                    if(choice=="swap")
                    {
                        float ratio=player.hp/player.maxHP;player.skin=offeredSkin;player.maxHP=offeredSkin=="speed"?300:offeredSkin=="heavy"?480:config.playerHP;player.hp=Mathf.Max(1,player.maxHP*ratio);player.weapons=(bool[])offeredWeapons.Clone();
                        if(stage.requireRecovery&&!recovered){for(int i=0;i<3;i++)ammo[i]=stage.recoveredAmmo[i];recovered=true;}
                    }
                }
                else return false;
            }
            phase="depart";return true;
        }
        void Win(){if(chestOpened)return;chestOpened=true;money+=config.chestMoney;gears+=config.chestGears;phase="won";travelSpeed=0;}
    }
}
