using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class BattleVfxAudit
    {
        static int step,frame;
        static double since;
        static bool errors;
        static EnemyEntity target;
        static BattleVfxEffect pausedEffect;
        static float pausedAge;
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Next(){step++;since=EditorApplication.timeSinceStartup;frame=Time.frameCount;}
        static BattleVfxEffect[] Effects()=>UnityEngine.Object.FindObjectsOfType<BattleVfxEffect>();
        static void CaptureBattle(Camera camera,string name)
        {
            var previous=camera.targetTexture;var active=RenderTexture.active;
            var texture=new RenderTexture(1920,1080,24);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
                image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                File.WriteAllBytes("Checks/VFX/"+name+".png",image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=previous;RenderTexture.active=active;
                texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(image);
            }
        }
        public static void Run()
        {
            BattleVfxImporter.Import();
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var window=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
            window.position=new Rect(20,20,1000,600);MegaTanksSetup.SetLandscapeGameView(window);window.Show();
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged+=Entered;EditorApplication.isPlaying=true;
        }
        static void Entered(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            step=0;errors=false;since=EditorApplication.timeSinceStartup;
            Application.logMessageReceived+=Log;EditorApplication.update+=Check;
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Check()
        {
            try
            {
                var game=BattleGame.Instance;var vfx=game.Vfx;
                if(step>0&&(Time.frameCount<=frame+1||EditorApplication.timeSinceStartup-since<.08))return;
                switch(step)
                {
                    case 0:
                        Require(vfx.Catalog!=null,"Battle must load its VFX catalog in the shipped scene.");
                        Require(Enum.GetValues(typeof(BattleVfxKind)).Cast<BattleVfxKind>().Sum(kind=>vfx.Catalog.Get(kind).frames.Length)==48,"All 48 supplied frames must be bound.");
                        foreach(BattleVfxKind kind in Enum.GetValues(typeof(BattleVfxKind)))
                        {
                            var clip=vfx.Catalog.Get(kind);
                            Require(clip.frames.All(sprite=>sprite!=null&&sprite.rect.size==new Vector2(320,320)),"Every frame must retain its full 320px cell.");
                        }
                        Require(vfx.Catalog.groundFire.frames[0].pivot==new Vector2(160,40)&&vfx.Catalog.tracer.frames[0].pivot==new Vector2(272,160),"Ground and projectile attachment pivots must match the manifest.");
                        game.StartBattle();game.SetStance(TankStance.Defensive);StageProgressionAudit.DisableWeapons(game);
                        foreach(var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())
                        {game.RemoveEnemy(enemy,false);enemy.gameObject.SetActive(false);UnityEngine.Object.Destroy(enemy.gameObject);}
                        target=game.SpawnEnemy(EnemyKind.Ground,(Vector2)game.Artwork.transform.position+new Vector2(5,1));
                        target.enabled=false;target.Weapon.enabled=false;
                        game.MachineGun.Stats.damage=game.MainGun.Stats.damage=game.Mortar.Stats.damage=0;
                        game.MachineGun.SendMessage("Update");game.MainGun.SendMessage("Update");game.Mortar.SendMessage("Update");
                        Require(game.MachineGun.ShotsFired>0&&game.MainGun.ShotsFired>0&&game.Mortar.ShotsFired>0,"Real MG, cannon and mortar firing must still produce projectiles: "+game.MachineGun.ShotsFired+"/"+game.MainGun.ShotsFired+"/"+game.Mortar.ShotsFired);
                        Require(Effects().Any(effect=>effect.Kind==BattleVfxKind.Tracer)&&Effects().Any(effect=>effect.Kind==BattleVfxKind.Smoke)
                            &&Effects().Count(effect=>effect.Kind==BattleVfxKind.Muzzle)==3,"Real shots must attach tracers, shell smoke and all three muzzle effects.");
                        Directory.CreateDirectory("Checks/VFX");
                        Next();break;
                    case 1:
                        CaptureBattle(game.TankCamera,"Firing");
                        foreach(var heavy in new[]{false,true})
                        {
                            var probe=new GameObject("VFX ground contact probe").AddComponent<Projectile2D>();
                            probe.Initialize(new Vector2(game.Artwork.transform.position.x+3,BattleGame.GroundHeight+.3f),Vector2.down,100,0,Color.white,heavy,10);
                            probe.SendMessage("Update");
                        }
                        var mortar=new GameObject("VFX mortar contact probe").AddComponent<Projectile2D>();
                        mortar.InitializeMortar(new Vector2(game.Artwork.transform.position.x+5,BattleGame.GroundHeight+.3f),new Vector2(1,-100),0,12,1,2,.45f);
                        mortar.SendMessage("Update");
                        pausedEffect=Effects().First(effect=>effect.Kind==BattleVfxKind.Explosion);pausedAge=pausedEffect.Age;
                        game.SetPaused(true);
                        Next();break;
                    case 2:
                        Require(Effects().Any(effect=>effect.Kind==BattleVfxKind.Impact),"Real light projectile contact must play impact sparks.");
                        Require(!Effects().Any(effect=>effect.Kind==BattleVfxKind.GroundFire),"Mortar ground impacts must not leave lingering fire.");
                        Require(Effects().Any(effect=>effect.Kind==BattleVfxKind.Explosion),"Mortar impacts must retain their explosion effect.");
                        CaptureBattle(game.TankCamera,"GroundImpacts");
                        Next();break;
                    case 3:
                        Require(pausedEffect!=null&&pausedEffect.Age==pausedAge,"Pause must freeze flipbook animation and lifetime.");
                        game.SetPaused(false);
                        int ammo=game.MachineGun.Ammo;
                        target.transform.position+=Vector3.right*200;
                        game.MachineGun.SendMessage("Update");game.MainGun.SendMessage("Update");
                        Require(game.MachineGun.Target==null&&game.MainGun.Target==null&&Quaternion.Angle(game.MachineGun.Artwork.transform.localRotation,Quaternion.identity)<.001f
                            &&Quaternion.Angle(game.MainGun.Artwork.transform.localRotation,Quaternion.identity)<.001f,"Losing targets must still restore default gun poses.");
                        Require(!game.MachineGun.Artwork.EffectPlaying&&game.MachineGun.Ammo==ammo,"Idle reset must stop the new muzzle effect without consuming ammo.");
                        target.transform.position=(Vector2)game.Artwork.transform.position+Vector2.right*8;
                        target.Damage(1000000);
                        Require(game.Rewards.IsOpen&&Effects().Any(effect=>effect.Kind==BattleVfxKind.Explosion),"The last enemy's destruction effect must survive opening rewards.");
                        Next();break;
                    case 4:
                        Require(Effects().Any(effect=>effect.Kind==BattleVfxKind.Explosion&&effect.Age>0),"Final explosions must animate while rewards are displayed.");
                        Next();break;
                    case 5:
                        CaptureBattle(game.TankCamera,"Destruction");
                        game.ClearEncounterTransient();Require(vfx.ActiveCount==0,"Encounter transitions must clear every effect.");
                        game.StartBattle();StageProgressionAudit.DisableWeapons(game);
                        int limit=vfx.Catalog.maxEffects;
                        try
                        {
                            vfx.Catalog.maxEffects=8;
                            for(int i=0;i<20;i++)vfx.Play(BattleVfxKind.Impact,Vector2.zero,1,Vector2.right);
                            Require(vfx.ActiveCount==8,"Effect count must remain bounded under burst load.");
                        }
                        finally{vfx.Catalog.maxEffects=limit;}
                        game.ReturnHome();Require(vfx.ActiveCount==0,"Home must clear VFX.");
                        Require(!errors,"VFX audit must have no runtime errors.");
                        Debug.Log("MEGATANKS_VFX_OK: 48 frames/6 clips, native pivots, real MG/cannon/mortar firing, tracer/smoke alignment, ground impacts, sparks/explosions/fire, pause, idle reset, final-kill animation, bounded effects and transition cleanup.");
                        Finish(0);break;
                }
                if(EditorApplication.timeSinceStartup-since>30)throw new Exception("VFX audit timed out.");
            }
            catch(Exception exception){Debug.LogException(exception);Finish(1);}
        }
        static void Finish(int code)
        {
            EditorApplication.update-=Check;EditorApplication.playModeStateChanged-=Entered;Application.logMessageReceived-=Log;
            EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);
        }
    }
}
