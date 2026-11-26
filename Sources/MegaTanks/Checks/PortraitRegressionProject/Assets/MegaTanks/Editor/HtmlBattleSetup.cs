using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    [InitializeOnLoad]
    public static class HtmlBattleSetup
    {
        const string Art="Assets/MegaTanks/Art/HtmlBattle/";
        static HtmlBattleSetup()
        {
            EditorApplication.delayCall+=()=>{
                const string marker="Checks/html-battle-apply.pending";
                if(!EditorApplication.isPlaying&&File.Exists(marker)){File.Delete(marker);Apply();}
            };
        }
        [MenuItem("MegaTanks/HTML Battle/Import Reference Assets")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before importing battle assets.");
            var manifest=JsonUtility.FromJson<HtmlArtManifest>(File.ReadAllText(Art+"manifest.json"));
            foreach(var entry in manifest.images)
            {
                string path=Art+entry.file;var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)throw new InvalidOperationException("Missing reference artwork: "+entry.key);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=100;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;
                importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.filterMode=FilterMode.Bilinear;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
                entry.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);entry.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            Directory.CreateDirectory("Assets/MegaTanks/Resources");
            const string output="Assets/MegaTanks/Resources/HtmlBattleArt.asset";
            var asset=AssetDatabase.LoadAssetAtPath<HtmlBattleArt>(output);
            if(asset==null){asset=ScriptableObject.CreateInstance<HtmlBattleArt>();AssetDatabase.CreateAsset(asset,output);}
            asset.data=manifest;EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
            HtmlBattleConfig.Load();Debug.Log("HTML_BATTLE_SETUP_OK: 3 configurable stages, native layered rendering and roadside encounters enabled.");
            Audit();
        }
        static void Require(bool result,string message){if(!result)throw new InvalidOperationException(message);}
        [MenuItem("MegaTanks/HTML Battle/Verify Simulation")]
        public static void Audit()
        {
            var c=HtmlBattleConfig.Load();var report=new System.Text.StringBuilder();report.AppendLine("Native HTML battle validation");
            // Drive full state transitions without modifying player saves, tuning or scene assets.
            for(int stage=0;stage<c.stages.Length;stage++)
            {
                var s=new HtmlBattleSimulation(c,stage);int decisions=0;bool travelObserved=false,choiceObserved=false;
                for(int frame=0;frame<30000&&!s.Terminal;frame++)
                {
                    if(s.phase=="wave"||s.phase=="boss")foreach(var e in s.enemies.ToArray())s.Damage(e,100000);
                    if(s.phase=="approach"||s.phase=="depart")
                    {int count=s.spawned;s.Tick(.05f);Require(count==s.spawned,"Enemies must not spawn while travelling to or leaving a station");travelObserved=true;}
                    else if(s.phase=="choice")
                    {
                        int count=s.spawned;float road=s.road;s.Tick(.05f);Require(count==s.spawned&&Math.Abs(road-s.road)<.001f,"Choices stop travel and enemy spawning");
                        if(s.encounter=="ammo"){Require(!s.Choose("plasma"),"AD must not grant an unearned reward");Require(s.Choose("piercing"),"Ammo choice");}
                        else if(s.encounter=="wreck")Require(s.Choose("swap"),"Tank swap choice");
                        else if(s.encounter=="medical")Require(s.Choose("gears"),"Medical choice");
                        else Require(s.Choose(c.weapons[Array.FindIndex(s.player.weapons,w=>w)].id),"Supply choice");decisions++;choiceObserved=true;
                    }
                    else s.Tick(.05f);
                }
                Require(s.phase=="won"&&s.bossKilled&&s.chestOpened,"Stage "+stage+" must reach boss and chest");
                Require(travelObserved&&choiceObserved&&decisions==c.wavesPerStage/c.wavesPerEncounter,"All roadside choices are reachable");
                int money=s.money,gears=s.gears;for(int i=0;i<30;i++)s.Tick(.05f);
                Require(money==s.money&&gears==s.gears,"Victory and chest must credit exactly once");
                Require(s.kills==s.stage.total,"Enemy queue must match configured total");
                if(s.stage.requireRecovery)Require(s.recovered&&s.fighters.Count==5,"Recovery preserves the team while removing the mortar mount");
            }
            var preview=new HtmlBattleSimulation(c,0);preview.phase="choice";preview.encounter="wreck";preview.offeredSkin="heavy";preview.offeredWeapons=new[]{true,true,false};int reserve=preview.ammo[2];
            Require(preview.Choose("swap")&&preview.ammo[2]==reserve,"Missing weapons keep ammo reserves");
            preview.phase="choice";preview.encounter="supply";Require(!preview.Choose("mortar"),"Missing weapon resupply is disabled");
            var invalid=JsonUtility.FromJson<HtmlBattleConfig>(JsonUtility.ToJson(c));invalid.weapons[0].cooldown=0;bool rejected=false;try{invalid.Validate();}catch(InvalidOperationException){rejected=true;}Require(rejected,"Invalid zero shot timing rejected");
            Debug.Log("HTML_BATTLE_AUDIT_OK: all stages, travelling/choice spawn gates, boss/chest once, partial tanks, reserves and config validation.");
            for(int stage=0;stage<c.stages.Length;stage++)
            {
                var run=new HtmlBattleSimulation(c,stage);int frames=0;
                while(!run.Terminal&&frames++<36000)
                {
                    if(run.phase=="choice")run.Choose(run.encounter=="ammo"?"piercing":run.encounter=="wreck"?(run.offeredWeapons[0]?"swap":"keep"):run.encounter=="medical"?(run.player.hp<run.player.maxHP?"heal":"gears"):c.weapons[Array.FindIndex(run.player.weapons,w=>w)].id);
                    run.Tick(.05f);
                }
                string result="HTML_BATTLE_BALANCE_STAGE_"+(stage+1)+": "+run.phase+", kills="+run.kills+", hp="+run.player.hp.ToString("0.0")+", time="+run.time.ToString("0.0");Debug.Log(result);report.AppendLine(result);
            }
        }
    }
}


