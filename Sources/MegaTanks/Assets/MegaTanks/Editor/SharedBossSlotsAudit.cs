using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MegaTanks.Editor
{
    public static class SharedBossSlotsAudit
    {
        static StageRewardSaveScope saves;
        public static void Run()
        {
            SharedBossSlotsSetup.Run();
            foreach(var guid in AssetDatabase.FindAssets("t:BossTankProfile"))
            {
                var boss=AssetDatabase.LoadAssetAtPath<BossTankProfile>(AssetDatabase.GUIDToAssetPath(guid));
                var inventory=AssetDatabase.LoadAssetAtPath<FrontendCatalog>(CanvasUISetup.CatalogPath).inventory;
                Require(inventory.Contains(boss.equipment.machineGun)&&inventory.Contains(boss.equipment.cannon)&&inventory.Contains(boss.equipment.mortar),"Boss weapons must reference player inventory definitions.");
                new BossEncounterSettings {template=boss}.Validate();
            }
            saves=new StageRewardSaveScope();
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Check;};
            EditorApplication.isPlaying=true;
        }
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static void Check()
        {
            int result=0;
            try
            {
                var game=BattleGame.Instance;game.StartBattle();StageProgressionAudit.DisableWeapons(game);
                var source=game.BossProfile;
                string weaponBefore=JsonUtility.ToJson(source.equipment.cannon);
                var settings=game.BossSettings.Copy(); settings.cannon.level=7;settings.cannon.stars=3;
                var entity=new GameObject("Shared boss slot probe").AddComponent<EnemyEntity>();entity.transform.position=game.TankAimPoint+Vector2.right*20;
                var boss=entity.gameObject.AddComponent<BossTank>();boss.Configure(settings);entity.Initialize(settings.CreateEnemy(),game.Wave);
                Require(Mathf.Approximately(boss.CannonStats.damage,ProgressionRules.Damage(source.equipment.cannon.stats.damage,7,3)),"Boss weapon damage must use stage level/stars.");
                Require(!ReferenceEquals(boss.CannonStats,source.equipment.cannon.stats),"Boss must copy shared weapon stats.");
                boss.CannonStats.damage+=123;Require(JsonUtility.ToJson(source.equipment.cannon)==weaponBefore,"Boss combat must not mutate shared player definitions.");
                var accessory=AssetDatabase.LoadAssetAtPath<FrontendCatalog>(CanvasUISetup.CatalogPath).inventory.OfType<AccessoryDefinition>().First();
                var template=UnityEngine.Object.Instantiate(source);template.accessory=accessory;template.secondAccessory=null;
                settings.template=template;settings.accessory.level=5;
                Require(Mathf.Approximately(settings.AccessoryBonus(accessory.bonusType),accessory.Bonus(5)),"Accessory modifiers must use stage levels, independent of player saves.");
                var testWeapon=new WeaponCombatStats();float baseReload=testWeapon.reloadSpeed,baseAccuracy=testWeapon.accuracy;settings.ApplyAccessories(testWeapon);
                if(accessory.bonusType==AccessoryBonusType.ReloadSpeed)Require(testWeapon.reloadSpeed==baseReload+accessory.Bonus(5),"Reload accessory must modify weapon stats.");
                if(accessory.bonusType==AccessoryBonusType.Accuracy)Require(testWeapon.accuracy==baseAccuracy+accessory.Bonus(5),"Accuracy accessory must modify weapon stats.");
                Require(!ReferenceEquals(settings.Copy().accessory,settings.accessory),"Accessory progression must be copied.");
                entity.gameObject.SetActive(false);UnityEngine.Object.Destroy(entity.gameObject);UnityEngine.Object.Destroy(template);
                Debug.Log("BOSS_SHARED_SLOTS_AUDIT_OK: shared inventory links, typed inline slots, stage progression, accessory bonuses, runtime copies and no shared asset mutation.");
            }
            catch(Exception e){Debug.LogException(e);result=1;}
            finally{saves?.Dispose();EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(result);}
        }
    }
}
