using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace MegaTanks.Editor
{
    public static class CommandoMeleeAudit
    {
        static StageRewardSaveScope saves;
        static string crewSave;
        static bool hadCrewSave,errors,oldOptions;
        static EnterPlayModeOptions options;
        static readonly Dictionary<CrewDefinition,string> tuning=new Dictionary<CrewDefinition,string>();
        static EnemyEntity boss,enemy;
        static CrewDefinition playerMember;
        static int phase,frame;
        static double deadline;
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static void Near(float actual,float expected,string message)=>Require(Mathf.Abs(actual-expected)<.001f,message+": "+actual+" expected "+expected);
        public static void Run()
        {
            saves=new StageRewardSaveScope();hadCrewSave=PlayerPrefs.HasKey("MegaTanks.Crew.Loadout");crewSave=PlayerPrefs.GetString("MegaTanks.Crew.Loadout","");
            PlayerPrefs.DeleteKey(PlayerProgression.DefaultSaveKey);PlayerPrefs.DeleteKey("MegaTanks.Crew.Loadout");
            foreach(string guid in AssetDatabase.FindAssets("t:CrewDefinition"))
            {var member=AssetDatabase.LoadAssetAtPath<CrewDefinition>(AssetDatabase.GUIDToAssetPath(guid));tuning[member]=JsonUtility.ToJson(member.stats);}
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);oldOptions=EditorSettings.enterPlayModeOptionsEnabled;options=EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            Application.logMessageReceived+=Log;EditorApplication.playModeStateChanged+=State;deadline=EditorApplication.timeSinceStartup+100;EditorApplication.isPlaying=true;
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.update+=Tick;
            if(state==PlayModeStateChange.EnteredEditMode){EditorApplication.playModeStateChanged-=State;EditorSettings.enterPlayModeOptionsEnabled=oldOptions;EditorSettings.enterPlayModeOptions=options;EditorApplication.Exit(errors?1:0);}
        }
        static void ChasePlayer(CommandoUnit unit,int attacks)
        {
            for(int i=0;i<150&&unit.MeleeAttacks<attacks;i++)unit.Tick(.05f);
            Require(unit.MeleeAttacks==attacks,"Player reaches target and starts requested melee swing");
        }
        static void ChaseEnemy(int attacks)
        {
            for(int i=0;i<150&&enemy.Weapon.MeleeAttacks<attacks;i++){enemy.Tick(.05f);enemy.Weapon.Tick(.05f);}
            Require(enemy.Weapon.MeleeAttacks==attacks,"Boss commando reaches target and starts requested melee swing");
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Melee audit timeout");
                if(phase>0&&Time.frameCount<=frame+2)return;
                var game=BattleGame.Instance;var ally=game.Commando;
                if(phase==0)
                {
                    playerMember=game.Frontend.crew.members.First(m=>m.role==CrewRole.Commando);playerMember.stats=new CrewStats();var stats=playerMember.stats;
                    stats.hp.value=300;stats.healthRegeneration.value=125;stats.speed.value=4;stats.activationRange.value=40;stats.damage.value=8;stats.damageBonus.value=25;
                    stats.accuracy.value=1000;stats.criticalChance.value=100;stats.criticalDamage.value=50;stats.armorPenetration.value=100;stats.baseReloadTime.value=.7f;
                    // Range and projectile speed remain zero: no ranged setup is required for melee.
                    Require(game.Crew.Equip(playerMember,(int)CrewRole.Commando),"Player commando equips");
                    var gunner = game.Frontend.crew.members.First(m => m.role == CrewRole.Gunner);
                    Require(game.Crew.Equip(gunner, (int)CrewRole.Gunner), "Player gunner equips");
                    game.AlliedCrew.First(unit => unit.Role == CrewRole.Gunner).enabled = false;
                    var stage=Object.Instantiate(game.Stage.Definition);stage.boss=stage.boss.Copy();var template=Object.Instantiate(stage.boss.template);stage.boss.template=template;
                    template.crew=Array.ConvertAll(template.crew,s=>new BossCrewStation{station=s.station,member=s.member});
                    var slot=template.crew.First(s=>s.member!=null&&s.member.role==CrewRole.Commando);slot.member=Object.Instantiate(slot.member);slot.member.stats=new CrewStats();var foe=slot.member.stats;
                    foe.hp.value=500;foe.speed.value=4;foe.activationRange.value=100;foe.damage.value=5;foe.accuracy.value=1000;foe.armorPenetration.value=100;foe.baseReloadTime.value=.7f;
                    foreach(var progression in stage.boss.crew)if(progression.station=="Commando"){progression.level=1;progression.stars=0;}
                    game.SelectStage(stage);game.StartBattle();game.SetStance(TankStance.Defensive);StageProgressionAudit.DisableWeapons(game);ally.enabled=false;
                    foreach(var regular in Object.FindObjectsOfType<EnemyEntity>()){game.RemoveEnemy(regular,false);regular.gameObject.SetActive(false);Object.Destroy(regular.gameObject);}
                    game.Stage.Current.type=StageNodeType.Boss;boss=game.SpawnBoss();Require(boss!=null,"Boss fixture spawns");boss.enabled=boss.Boss.enabled=false;
                    boss.transform.position+=Vector3.right*(game.TankAimPoint.x+12-boss.transform.position.x);Physics2D.SyncTransforms();boss.Boss.TickCommando();enemy=boss.Boss.Commando;
                    Require(enemy!=null,"Boss commando deploys with zero weapon range");enemy.enabled=enemy.Weapon.enabled=false;
                    ally.Tick(.01f);Require(ally.Active,"Player deploys with zero weapon range");
                    Require(ally.HealthBar!=null&&ally.HealthBar.gameObject.activeInHierarchy,"Player HP bar appears on deployment");
                    Require(enemy.HealthBar!=null&&enemy.HealthBar.gameObject.activeInHierarchy,"Boss commando HP bar appears on deployment");
                    Near(ally.HealthBar.HealthFraction,1,"Player spawns with a full HP bar");Near(enemy.HealthBar.HealthFraction,1,"Enemy spawns with a full HP bar");
                    Require(ally.HealthBar.FillColor.b>ally.HealthBar.FillColor.r&&enemy.HealthBar.FillColor.r>enemy.HealthBar.FillColor.b,"HP bar colors distinguish factions");
                    Require(ally.HealthBar.GetComponent<Canvas>().sortingOrder>35&&enemy.HealthBar.GetComponent<Canvas>().sortingOrder>35,"HP bars render above commando artwork");
                    enemy.transform.position=new Vector3(ally.AimPoint.x+4,BattleGame.GroundHeight+.55f,0);Physics2D.SyncTransforms();
                    float before=ally.transform.position.x;ally.Tick(.05f);Require(ally.transform.position.x>before&&ally.Artwork.CurrentAnimation=="walk","Player walks toward melee target");
                    Near(enemy.Health,500,"No remote melee damage");Require(ally.MeleeAttacks==0,"No melee swing before contact");
                    ChasePlayer(ally,1);Require(CommandoMelee.InReach(ally.GetComponent<Collider2D>(),enemy.GetComponent<Collider2D>()),"Player stands beside enemy collider");
                    Require(ally.transform.position.x<enemy.GetComponent<Collider2D>().bounds.min.x,"Player stops outside target body");
                    Near(enemy.Health,500,"Damage waits for animation impact");ally.Artwork.Advance(2);Near(enemy.Health,485,"Melee uses damage bonus, critical chance, critical damage and armor penetration");
                    Near(enemy.HealthBar.HealthFraction,485f/500,"Enemy HP fill updates after damage");
                    ally.Artwork.Advance(2);Near(enemy.Health,485,"One hit per swing even with hit event and completion");
                    ally.Tick(3);Require(ally.MeleeAttacks==2,"Reload cooldown permits another swing");enemy.transform.position+=Vector3.right*2;Physics2D.SyncTransforms();
                    ally.Artwork.Advance(2);Near(enemy.Health,485,"Target moving out of contact avoids the swing");
                    ChasePlayer(ally,3);game.SetPaused(true);float time=ally.Artwork.Animation.AnimationState.GetCurrent(0).TrackTime;ally.Tick(5);
                    Near(ally.Artwork.Animation.AnimationState.GetCurrent(0).TrackTime,time,"Pause freezes melee animation");Near(enemy.Health,485,"Paused attack cannot damage target");game.SetPaused(false);
                    ally.Artwork.Advance(2);Near(enemy.Health,470,"Attack resumes correctly after pause");
                    Require(ally.ShotsFired==0&&Object.FindObjectsOfType<Projectile2D>().Length==0,"Player melee emits no projectiles");
                    game.SetBossCommandoDeployed(enemy,false);boss.transform.position+=Vector3.right*80;
                    Require(!enemy.HealthBar.gameObject.activeInHierarchy,"Stowed enemy hides its HP bar");
                    var flyer=game.SpawnEnemy(EnemyKind.Flyer,new Vector2(ally.AimPoint.x+1,BattleGame.GroundHeight+10));flyer.enabled=flyer.Weapon.enabled=false;
                    int attacks=ally.MeleeAttacks;ally.Tick(1);Require(ally.MeleeAttacks==attacks&&ally.Velocity==Vector2.zero,"Ground melee ignores unreachable flying targets");
                    game.RemoveEnemy(flyer,false);flyer.gameObject.SetActive(false);Object.Destroy(flyer.gameObject);
                    boss.transform.position-=Vector3.right*80;enemy.transform.position=new Vector3(ally.AimPoint.x+4,BattleGame.GroundHeight+.55f,0);game.SetBossCommandoDeployed(enemy,true);
                    Near(enemy.HealthBar.HealthFraction,470f/500,"Enemy HP bar preserves damage when redeployed");
                    Require(enemy.HealthBar.gameObject.activeInHierarchy,"Enemy HP bar returns on deployment");
                    phase++;frame=Time.frameCount;
                }
                else if(phase==1)
                {
                    float hp=ally.Health,hull=game.Hull;ChaseEnemy(1);
                    Require(CommandoMelee.PlayerTarget(game,enemy.GetComponent<Collider2D>())==ally.GetComponent<Collider2D>(),"Enemy melee chooses the nearby player commando");
                    Near(ally.Health,hp,"Enemy damage waits for impact");enemy.CommandoArtwork.Advance(2);Near(ally.Health,hp-5,"Enemy commando directly damages player commando");Near(game.Hull,hull,"Commando duel does not hurt main hull");
                    Near(ally.HealthBar.HealthFraction,(hp-5)/ally.MaxHealth,"Player HP fill updates after damage");
                    game.UI.SendMessage("Refresh");game.UI.SendMessage("UpdateHud");ItemProgressionAudit.Capture(game,"Commando-HP-Bars");
                    enemy.CommandoArtwork.Advance(2);Near(ally.Health,hp-5,"Enemy swing cannot hit twice");
                    enemy.Weapon.Tick(3);Require(enemy.Weapon.MeleeAttacks==2,"Enemy begins second swing");ally.ResetForRun();enemy.CommandoArtwork.Advance(2);Near(game.Hull,hull,"A stowed target cancels contact damage instead of redirecting it");
                    Require(!ally.HealthBar.gameObject.activeInHierarchy,"Resetting or stowing the player hides its HP bar");
                    var body=game.HullHitbox.GetComponent<Collider2D>();enemy.transform.position=new Vector3(body.bounds.max.x+3,BattleGame.GroundHeight+.55f,0);Physics2D.SyncTransforms();
                    ChaseEnemy(3);Require(enemy.transform.position.x>body.bounds.max.x,"Boss melee stops outside large main hull");
                    enemy.CommandoArtwork.Advance(2);Near(game.Hull,hull-5,"Enemy melee directly damages player tank");
                    Require(enemy.Weapon.ShotsFired==0&&Object.FindObjectsOfType<Projectile2D>().Length==0,"Enemy melee emits no projectiles");
                    enemy.Weapon.Stats.reloadTime=0;enemy.Weapon.Tick(3);Require(enemy.Weapon.MeleeAttacks==4,"Zero base reload time still permits melee");
                    for(int i=0;i<10;i++)enemy.Weapon.Tick(.1f);
                    Require(enemy.Weapon.MeleeAttacks==4,"A fast cooldown cannot interrupt or multiply a pending melee swing");enemy.CommandoArtwork.Advance(2);Near(game.Hull,hull-10,"Zero-reload melee still resolves once per animation");
                    // Positive range must retain the original projectile behavior on both sides.
                    ally.Tick(.01f);ally.Weapon.range=12;ally.Weapon.projectileSpeed=25;ally.Weapon.reloadTime=1;ally.Tick(2);
                    Require(ally.ShotsFired==1,"Player positive range remains ranged");enemy.Weapon.Stats.range=12;enemy.Weapon.Stats.projectileSpeed=25;enemy.Weapon.Stats.reloadTime=1;enemy.Weapon.Tick(3);
                    Require(enemy.Weapon.ShotsFired==1&&Object.FindObjectsOfType<Projectile2D>().Length==2,"Enemy positive range remains ranged");
                    Near(ally.HealthBar.HealthFraction,1,"New player deployment resets HP bar to full");
                    var accurate = new WeaponCombatStats { accuracy=1000,armorPenetration=100 };
                    ally.ReceiveAttack(new DamageAttack(150,accurate));
                    ally.Tick(2);
                    Near(ally.Health,150,"Commando cannot heal while deployed");
                    game.SetBossCommandoDeployed(enemy,false);
                    boss.transform.position+=Vector3.right*80;
                    ally.Tick(0);
                    Require(!ally.Active,"Commando boards when the threat leaves range");
                    Require(ally.HealthBar.gameObject.activeInHierarchy,"Wounded commando shows its HP bar aboard");
                    Near(ally.HealthBar.HealthFraction,.5f,"Aboard HP bar shows the commando's remaining HP");
                    ally.Tick(.75f);
                    Near(ally.Health,150,"A partial second does not heal");
                    game.SetPaused(true);
                    ally.Tick(5);
                    Near(ally.Health,150,"Pause freezes recovery");
                    game.SetPaused(false);
                    ally.Tick(.25f);
                    Near(ally.Health,275,"One full second aboard restores the authored amount");
                    Near(ally.HealthBar.HealthFraction,275f/300,"Commando's HP bar updates during regeneration");
                    ally.Tick(1);
                    Near(ally.Health,300,"Regeneration caps at max HP");
                    Require(!ally.HealthBar.gameObject.activeInHierarchy,"Aboard HP bar hides at full health");
                    boss.transform.position-=Vector3.right*80;
                    game.SetBossCommandoDeployed(enemy,true);
                    ally.Tick(.01f);
                    Require(ally.Active,"Healed commando redeploys");
                    ally.ReceiveAttack(new DamageAttack(10000,accurate));
                    Require(ally.IsUnconscious&&!ally.Active&&ally.HealthBar.gameObject.activeInHierarchy,"Knocked-out commando remains untargetable with a visible HP bar");
                    Near(ally.HealthBar.HealthFraction,0,"Unconscious commando shows zero HP initially");
                    Require(game.HullHitbox.GetComponent<Collider2D>().bounds.Contains(ally.transform.position),"Knocked-out commando is aboard the tank");
                    Require(ally.Artwork.CurrentAnimation=="death","Commando plays the collapse animation aboard");
                    ally.Tick(2);
                    Near(ally.Health,250,"Unconscious commando heals while a threat is nearby");
                    Require(ally.IsUnconscious&&!ally.Active,"Partial recovery cannot redeploy the commando");
                    ally.Tick(1);
                    Near(ally.Health,300,"Unconscious recovery caps at max HP");
                    Require(!ally.IsUnconscious,"Full HP ends unconsciousness");
                    ally.Tick(.01f);
                    Require(ally.Active&&ally.HealthBar.gameObject.activeInHierarchy,"Fully recovered commando can fight again");
                    CheckSupportingCrewRecovery(game, accurate);
                    enemy.ReceiveAttack(new DamageAttack(10000,new WeaponCombatStats{accuracy=1000,armorPenetration=100}));
                    Require(!enemy.HealthBar.gameObject.activeInHierarchy,"Enemy HP bar disappears on death");
                    Debug.Log("COMMANDO_HP_BARS_PASS: both factions; visible above artwork on spawn; full and damaged fill; faction colors; stow/death visibility; preserved HP on enemy redeployment; player reset; editable shared prefab.");
                    Debug.Log("COMMANDO_REGENERATION_PASS: onboard second ticks, no field healing, pause, knockout protection, and redeployment at full HP.");
                    game.ReturnHome();phase++;frame=Time.frameCount;
                }
                else
                {
                    Require(!errors,"No runtime errors during melee audit");
                    Debug.Log("COMMANDO_MELEE_PASS: zero range on both sides; edge-based approach; native attack impacts; full combat stats; one hit per swing; moving-target whiffs; pause; commando duels and tank melee; no projectiles; unreachable flyers excluded; positive range preserves ranged combat.");Finish(0);
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }

        static void CheckSupportingCrewRecovery(BattleGame game, WeaponCombatStats accurate)
        {
            var gunner = game.AlliedCrew.First(unit => unit.Role == CrewRole.Gunner);
            while (game.MainGun.TryConsumeRound()) { }
            gunner.Tick(0);
            gunner.Tick(1);
            Require(gunner.Active, "Gunner deploys with empty cannon and a nearby enemy");
            Require(gunner.HealthBar.gameObject.activeInHierarchy, "Gunner shows its HP bar during combat");
            Near(gunner.MaxHealth, 150, "Gunner uses half the commando's max HP");
            gunner.ReceiveAttack(new DamageAttack(100, accurate));
            gunner.Tick(1);
            Near(gunner.Health, 50, "Gunner does not heal in melee");
            gunner.ReceiveAttack(new DamageAttack(10000, accurate));
            Require(gunner.IsUnconscious && !gunner.Active && gunner.HealthBar.gameObject.activeInHierarchy,
                "Knocked-out gunner remains untargetable with a visible HP bar");
            Require(game.HullHitbox.GetComponent<Collider2D>().bounds.Contains(gunner.transform.position),
                "Knocked-out gunner returns to the tank");
            gunner.Tick(.75f);
            Near(gunner.Health, 0, "Gunner waits for a full second aboard");
            gunner.Tick(.25f);
            Near(gunner.Health, 62.5f, "Gunner heals at half the commando's rate");
            Near(gunner.HealthBar.HealthFraction, 62.5f / 150, "Gunner's HP bar updates during regeneration");
            gunner.Tick(1);
            Require(gunner.IsUnconscious && !gunner.Active, "Nearby enemies and empty ammo cannot deploy an unconscious gunner");
            gunner.Tick(1);
            Near(gunner.Health, 150, "Gunner recovery caps at full HP");
            Require(!gunner.IsUnconscious && !gunner.Active && !gunner.HealthBar.gameObject.activeInHierarchy,
                "Full HP wakes the gunner aboard the tank and hides its HP bar");
            game.MainGun.Resupply(1);
            Require(game.MainGun.Ammo > 0, "Cannon resupplied");
            gunner.Tick(1);
            gunner.Tick(1);
            Require(!gunner.Active, "Recovered gunner stays aboard with cannon ammo available");
            while (game.MainGun.TryConsumeRound()) { }
            gunner.Tick(0);
            gunner.Tick(1);
            Require(gunner.Active, "Recovered gunner jumps back into melee when the cannon is empty");
            Require(gunner.HealthBar.gameObject.activeInHierarchy, "Gunner's HP bar returns on redeployment");
            Debug.Log("SUPPORT_CREW_REGENERATION_PASS: half-rate onboard recovery, knockout, and ammo-gated redeployment.");
        }

        static void Finish(int code)
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
            foreach(var pair in tuning)if(pair.Key!=null)pair.Key.stats=JsonUtility.FromJson<CrewStats>(pair.Value);
            if(hadCrewSave)PlayerPrefs.SetString("MegaTanks.Crew.Loadout",crewSave);else PlayerPrefs.DeleteKey("MegaTanks.Crew.Loadout");saves?.Dispose();PlayerPrefs.Save();
            if(code==0){EditorApplication.isPlaying=false;return;}
            EditorApplication.playModeStateChanged-=State;EditorSettings.enterPlayModeOptionsEnabled=oldOptions;EditorSettings.enterPlayModeOptions=options;EditorApplication.Exit(code);
        }
    }
}
