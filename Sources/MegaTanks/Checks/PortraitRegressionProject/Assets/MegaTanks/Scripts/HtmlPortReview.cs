using UnityEngine;
namespace MegaTanks
{
    public sealed partial class HtmlBattleScreen
    {
        bool reviewFreeze;
        public void ReviewState(string state)
        {
            StartStage(0);reviewFreeze=true;paused=false;Simulation.ConfigureProfile(20);
            foreach(var f in Simulation.fighters)f.locked=false;
            Simulation.phase="wave";Simulation.encounter="";
            if(state=="tank-level"){Simulation.ConfigureProfile(new PlayerTankProfile(config).Level);Simulation.phase="wave";}
            else if(state=="boss"){var boss=Simulation.Spawn("boss");boss.x=1450;boss.weapons=new[]{true,true,false};}
            else if(state=="upgrade"){Simulation.phase="upgrade";Simulation.wave=4;Simulation.encounter="upgrade";Simulation.landmarkX=1080;Simulation.AddEffect("UpgradeAura",Simulation.player.x,config.groundY,580,2.6f);Simulation.effects[Simulation.effects.Count-1].age=.6f;}
            else if(state=="gameplay"||state=="vfx")
            {var a=Simulation.Spawn("cannon_car");a.x=1250;var b=Simulation.Spawn("helicopter");b.x=1630;b.y=400;var c=Simulation.Spawn("trojan");c.x=1850;if(state=="vfx"){reviewFreeze=false;Simulation.AddEffect("Explosion",1550,780,200,.5f);Simulation.AddEffect("GroundFire",1500,config.groundY,240,2.5f,true);}}
            else if(state=="legacy"){var a=Simulation.Spawn("car");a.x=1250;var b=Simulation.Spawn("plane");b.x=1600;b.y=400;}
            else if(state=="trojan"){var carrier=Simulation.Spawn("trojan");carrier.x=1350;Simulation.RevealTrojan(carrier);for(int i=0;i<12;i++)Simulation.Tick(.05f);}
            else if(state=="melee"){Simulation.player.weapons=new[]{false,false,false};for(int i=0;i<3;i++)Simulation.ammo[i]=0;var boss=Simulation.Spawn("boss");Simulation.bossPhase="approach";Simulation.player.x=config.bossDuelCenter-config.bossContactFactor*Simulation.player.width;boss.x=config.bossDuelCenter+config.bossContactFactor*boss.width;for(int i=0;i<95;i++)Simulation.Tick(.05f);}
            else if(state=="win"){var boss=Simulation.Spawn("boss");Simulation.Damage(boss,100000);for(int i=0;i<300&&!Simulation.Terminal;i++)Simulation.Tick(.05f);}
            else {Simulation.phase="choice";Simulation.wave=2;Simulation.encounter=state;Simulation.encounterIndex=1;Simulation.offeredSkin="speed";Simulation.landmarkX=1080;}
            modalKey="";Render();UpdateHUD();UpdateModal();
        }
        public void ResumeReviewSimulation(){reviewFreeze=false;}
        public string ReviewInteractionChecks()
        {
            foreach(string encounter in new[]{"ammo","supply","medical","wreck"})
            {
                ReviewState(encounter);
                foreach(var illustration in modal.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    if(illustration.name=="Illustration"&&illustration.sprite==null)
                        throw new System.InvalidOperationException("Missing original card illustration: "+encounter+" / "+illustration.transform.parent.name);
            }
            ReviewState("ammo");var selected=modal.Find("Card cannon_ap").GetComponent<UnityEngine.UI.Button>();selected.onClick.Invoke();UpdateModal();
            if(modal.gameObject.activeSelf||Simulation.phase!="depart"||Simulation.weaponVariants[1]!="cannon_ap")throw new System.InvalidOperationException("Choice must apply once and close the modal.");
            ReviewState("gameplay");hud.Find("Pause").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();UpdateModal();
            float time=Simulation.time,road=Simulation.road;Update();if(!paused||Simulation.time!=time||Simulation.road!=road)throw new System.InvalidOperationException("Pause must stop the simulation.");
            modal.Find("Primary").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();UpdateModal();if(paused||modal.gameObject.activeSelf)throw new System.InvalidOperationException("Resume must hide the modal.");
            var provider=RequestRewardedAd;bool ads=config.rewardedAdsEnabled;
            try{config.rewardedAdsEnabled=true;RequestRewardedAd=done=>{done(false);done(true);};ReviewState("ammo");ChooseDesign("mg_plasma");if(Simulation.weaponVariants[0]!="mg"||Simulation.phase!="choice")throw new System.InvalidOperationException("Cancelled AD may not accept a late success callback.");}
            finally{config.rewardedAdsEnabled=ads;RequestRewardedAd=provider;}
            ReviewMainVfx();
            ReviewOriginalMortarAndEnemyDeath();ReviewHtmlTravel();
            return "PASS: original illustrations on all four choice screens; actual card/pause listeners, choice closes, pause freezes road/time, resume hides overlay; cancelled AD rejects duplicate callbacks.";
        }
        void ReviewMainVfx()
        {
            ReviewState("gameplay");var s=Simulation;s.effects.Clear();s.bullets.Clear();nativeWorld.Vfx.Clear();
            if(!nativeWorld.Vfx.Available)throw new System.InvalidOperationException("Main BattleVfx catalog/prefabs must resolve.");
            var bullet=new HtmlBattleSimulation.Bullet{weapon="cannon",x=550,y=600,tx=1650,ty=600,life=4};s.bullets.Add(bullet);
            s.AddEffect("GroundFire",1200,config.groundY,240,2.5f,true);
            for(int i=0;i<30;i++){bullet.x+=8;nativeWorld.Draw(s,.05f);}
            if(nativeWorld.Vfx.ProjectileCount!=1||nativeWorld.Vfx.TailPointCount<2||nativeWorld.Vfx.ParticleCount==0)throw new System.InvalidOperationException("Main FireBolt must follow a live projectile beyond prefab lifetime with visible trail/particles.");
            float clock=nativeWorld.Vfx.Clock;int points=nativeWorld.Vfx.TailPointCount,particles=nativeWorld.Vfx.ParticleCount,count=nativeWorld.Vfx.ActiveCount;
            for(int i=0;i<12;i++)nativeWorld.Draw(s,0);
            if(nativeWorld.Vfx.Clock!=clock||nativeWorld.Vfx.TailPointCount!=points||nativeWorld.Vfx.ParticleCount!=particles||nativeWorld.Vfx.ActiveCount!=count)throw new System.InvalidOperationException("Pause must freeze particle/trail time and must not duplicate one-shot effects.");
            var fire=nativeWorld.Vfx.GroundFirePositions;if(fire.Length==0)throw new System.InvalidOperationException("Main ground fire frames must render.");
            s.road+=100;nativeWorld.Draw(s,.02f);
            if(Mathf.Abs(nativeWorld.Vfx.GroundFirePositions[0].x-fire[0].x+1)>.001f)throw new System.InvalidOperationException("Ground fire must move with the road.");
            s.bullets.Clear();nativeWorld.Draw(s,.01f);
            if(nativeWorld.Vfx.ProjectileCount!=0||nativeWorld.Vfx.TailPointCount==0)throw new System.InvalidOperationException("Projectile impact must detach and preserve its existing tail.");
            for(int i=0;i<65;i++)nativeWorld.Draw(s,.05f);
            if(nativeWorld.Vfx.ActiveCount!=0||nativeWorld.Vfx.TailPointCount!=0)throw new System.InvalidOperationException("Detached particles/trails and ground fire must finish and release: "+nativeWorld.Vfx.DebugState);
            System.IO.File.AppendAllText("Checks/HtmlPort/validation.txt","PASS: main VFX catalog, long-lived projectile follow, particle/trail pause, one-shot guard, road-anchored fire, detached tail drain and release.\n");
        }
        void ReviewOriginalMortarAndEnemyDeath()
        {
            ReviewState("gameplay");
            if(Simulation.MissileStats!=null||!nativeWorld.PartVisible(Simulation.player,"tank_base_09_mortar_barrel"))throw new System.InvalidOperationException("HTML battle must keep the original green mortar, including with a saved legacy missile loadout.");
            foreach(string kind in new[]{"car","plane","dog","cannon_car","helicopter","trojan","boss"})
            {
                ReviewState("gameplay");var actor=Simulation.Spawn(kind);actor.x=1250;nativeWorld.Draw(Simulation,.01f);
                Simulation.Damage(actor,100000);nativeWorld.Draw(Simulation,.01f);int loot=Simulation.loot.Count,kills=Simulation.kills;
                if(!actor.dead||nativeWorld.ActorVisible(actor)||Simulation.corpses.Count!=0||!Simulation.effects.Exists(f=>f.clip=="Explosion"))throw new System.InvalidOperationException("Dead enemy must disappear with an explosion immediately: "+kind);
                Simulation.Damage(actor,100000);if(loot!=Simulation.loot.Count||kills!=Simulation.kills)throw new System.InvalidOperationException("Enemy death must not duplicate kill or loot: "+kind);
            }
            System.IO.File.AppendAllText("Checks/HtmlPort/validation.txt","PASS: original green mortar; every enemy/boss disappears on death with explosion, no corpse fade, no duplicate kill/loot.\n");
        }
        void ReviewHtmlTravel()
        {
            var s=new HtmlBattleSimulation(config,0);foreach(var f in s.fighters)f.locked=true;s.player.weapons=new[]{false,false,false};
            var enemy=s.Spawn("boss");enemy.bossCrew=true;float x=enemy.x;
            for(int i=0;i<20;i++)s.Tick(.05f);
            if(Mathf.Abs((x-enemy.x)-260)>.05f||Mathf.Abs(s.player.x-config.playerX)>.01f)throw new System.InvalidOperationException("HTML boss entry must use 260 px/s before the duel.");
            for(int i=0;i<100&&s.bossPhase=="entry";i++)s.Tick(.05f);float left=s.player.x,right=enemy.x;
            for(int i=0;i<20;i++)s.Tick(.05f);
            if(Mathf.Abs(s.player.x-left-60)>.05f||Mathf.Abs(right-enemy.x-60)>.05f)throw new System.InvalidOperationException("Both tanks must approach at HTML duel speed 60 px/s.");
            for(int i=0;i<180&&!s.BossContact;i++)s.Tick(.05f);
            if(!s.BossContact)throw new System.InvalidOperationException("Both tanks must reach their HTML contact positions.");
            System.IO.File.AppendAllText("Checks/HtmlPort/validation.txt","PASS: HTML boss entry 260 px/s, both tanks approach at 60 px/s and meet at the configured center/contact spacing.\n");
        }
    }
}
