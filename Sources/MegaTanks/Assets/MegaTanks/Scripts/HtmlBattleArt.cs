using System;
using System.Linq;
using UnityEngine;

namespace MegaTanks
{
    [Serializable] public sealed class HtmlWeaponConfig { public string id; public float minRange,maxRange,damage,cooldown,projectileSpeed; public int startingAmmo,supply; }
    [Serializable] public sealed class HtmlEnemyConfig { public string id;public float hp,hardHP,speed,width,damage,cooldown,range; }
    [Serializable] public sealed class HtmlStageConfig { public string title,background;public int total,planes,dogs,maxActive;public float spawnInterval,bossHP;public string[] bossWeapons;public int[] startingAmmo,supply,recoveredAmmo;public bool requireRecovery;public int cannonCars,helicopters,trojans; }
    [Serializable] public sealed class HtmlBattleConfig
    {
        public int[] unlockLevels={5,1,15,1,10}; public int tankXPBase=100,maxTankLevel=100; public float tankXPGrowth=1.2f; public int tankXPReward=100; public bool initialUpgradeStation=true; public float crewDefaultAccuracy=100,stationSpawnX=2150,upgradeApplySeconds=.5f,upgradeDepartSeconds=2.1f,upgradeAuraSeconds=2.6f,stationHPBonus=.12f,stationDamageBonus=10; public int upgradeEveryGroups=2;public float trojanRevealRange=700;
        public int version,seed,wavesPerStage,wavesPerEncounter,medicalGears,bossMoney,bossGears,chestMoney,chestGears,enemyMoney,enemyGears;
        public float playerHP,playerWidth,playerX,groundY,cruiseSpeed,stationTravelSpeed,waveBreakSeconds,encounterStopX,medicalHP,bossKnockback,meleeDamage,meleeHP,meleeSpeed,meleeCooldown,meleeRange;
        public float piercingMultiplier,explosiveMultiplier,plasmaMultiplier,splashRadius,splashMultiplier;
        public bool rewardedAdsEnabled; public string[] encounterDeck;
        public bool grantNewSlotWeapon=true;public int[] newSlotAmmo={90,12,8};public float stationDefenseBonus=2,stationCritBonus=2;
        public float crewLaneSpacing=46;public bool legacyCrewStatBonuses=true;
        public bool useMainBattleVfx=true;
        public float bossEntrySpeed=260,bossDuelSpeed=60,bossDuelCenter=960,bossContactFactor=.52f,playerGoalX=400,travelAcceleration=90,stationAcceleration=480,roadScrollFactor=1.2f;
        public HtmlWeaponConfig[] weapons;public HtmlEnemyConfig[] enemies;public HtmlStageConfig[] stages;
        public static HtmlBattleConfig Load()
        {
            var text=Resources.Load<TextAsset>("HtmlBattleConfig");if(text==null)throw new InvalidOperationException("Missing HtmlBattleConfig.json");
            var c=JsonUtility.FromJson<HtmlBattleConfig>(text.text);c.Validate();return c;
        }
        public void Validate()
        {
            if(version!=1||stages==null||stages.Length==0||weapons==null||weapons.Length!=3||enemies==null||enemies.Length<3||wavesPerStage<1||wavesPerEncounter<1||playerHP<=0||meleeCooldown<=0||cruiseSpeed<=0||stationTravelSpeed<=0||encounterDeck==null||encounterDeck.Length==0)throw new InvalidOperationException("Invalid battle config: check version, stages, weapon/enemy arrays and positive timings.");
            if(!weapons.Select(w=>w.id).SequenceEqual(new[]{"mg","cannon","mortar"}))throw new InvalidOperationException("Weapon order must be mg, cannon, mortar (ammo arrays use this order).");
            if(unlockLevels==null||unlockLevels.Length!=5||unlockLevels.Any(n=>n<1)||tankXPBase<1||tankXPGrowth<1||maxTankLevel<1||upgradeEveryGroups<1||upgradeApplySeconds<0||upgradeDepartSeconds<upgradeApplySeconds||newSlotAmmo?.Length!=3)throw new InvalidOperationException("Invalid progression/station config.");
            foreach(var w in weapons)if(w.minRange<0||w.maxRange<=w.minRange||w.cooldown<=0||w.projectileSpeed<=0||w.damage<0)throw new InvalidOperationException("Invalid weapon: "+w.id);
            foreach(var e in enemies)if(e.hp<=0||e.hardHP<=0||e.width<=0||e.speed<=0||e.cooldown<=0)throw new InvalidOperationException("Invalid enemy: "+e.id);
            foreach(var s in stages)if(s.total<1||s.planes<0||s.dogs<0||s.planes+s.dogs>s.total||s.maxActive<1||s.spawnInterval<=0||s.bossHP<=0||s.startingAmmo?.Length!=3||s.supply?.Length!=3||s.recoveredAmmo?.Length!=3||s.startingAmmo.Any(n=>n<0))throw new InvalidOperationException("Invalid stage: "+s.title);
        }
    }
    [Serializable] public sealed class HtmlArtImage { public string key,file,source;public Sprite sprite;public Texture2D texture;public float[] border; }
    [Serializable] public sealed class HtmlArtPart { public string key,name;public float[] position,size,pivot;public int order;public string parent; }
    [Serializable] public sealed class HtmlArtSeat { public string id;public float x,y,scale; }
    [Serializable] public sealed class HtmlArtGun { public string id;public float[] pivot,tip; }
    [Serializable] public sealed class HtmlTankProfile { public string id,name;public float hp,speed;public HtmlArtPart[] parts;public HtmlArtSeat[] seats;public HtmlArtGun[] guns; }
    [Serializable] public sealed class HtmlEnemyArt { public string id;public HtmlArtPart[] parts;public float[] origin,canvas,muzzle; }
    [Serializable] public sealed class HtmlBackgroundArt { public string id;public string[] keys; }
    [Serializable] public sealed class HtmlArtClip { public string id,key;public int cols,rows,frames;public float duration,worldScale;public float[] cell,pivot; }
    [Serializable] public sealed class HtmlArtManifest { public HtmlArtImage[] images;public HtmlTankProfile[] profiles;public HtmlEnemyArt[] enemies;public HtmlBackgroundArt[] backgrounds;public HtmlArtClip[] clips; }
    public sealed class HtmlBattleArt : ScriptableObject
    {
        public HtmlArtManifest data;
        public Spine.Unity.SkeletonDataAsset catDriver,catMelee,dogMelee,dogDriver,dogPilot;
        public TextAsset layout,cards;
        public Texture2D comicFontAtlas;public TextAsset comicFontMetrics;
        public Shader worldBlurShader,worldClipShader;
        public HtmlArtImage Image(string key)=>data.images.FirstOrDefault(i=>i.key==key);
        public HtmlTankProfile Profile(string id)=>data.profiles.FirstOrDefault(p=>p.id==id)??data.profiles[0];
        public HtmlArtClip Clip(string id)=>data.clips.FirstOrDefault(c=>c.id==id);
    }
}
