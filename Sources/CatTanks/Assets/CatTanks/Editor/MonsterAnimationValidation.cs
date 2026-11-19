using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatTanks.Editor
{
    public static class MonsterAnimationValidation
    {
        [MenuItem("CatTanks/Combat/Validate Latest Monster Animations")]
        public static void Validate()
        {
            var library=Resources.Load<MonsterAnimationLibrary>("MonsterAnimations");if(!library)throw new Exception("Import animations first.");
            var checks=new List<string>();Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);checks.Add(label);};
            var db=Catalog.Load();check(library.entries.Count==db.monsters.Count,"All 20 game monsters have animation entries");
            var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                foreach(var entry in library.entries)
                {
                    check(entry.idle.Length==6&&entry.run.Length==6&&entry.attack.Length==6&&entry.hit.Length==6&&entry.enterRun.Length==2,entry.species+" has four complete clips and transition");
                    var sprites=entry.idle.Concat(entry.run).Concat(entry.attack).Concat(entry.hit).Concat(entry.enterRun).ToArray();
                    check(sprites.All(s=>s&&s.rect.width==entry.cellWidth&&s.rect.height==entry.cellWidth),entry.species+" keeps full animation cells");
                    check(sprites.All(s=>Vector2.Distance(s.pivot,sprites[0].pivot)<.01f),entry.species+" keeps a shared foot anchor across poses");
                    var go=new GameObject(entry.species,typeof(SpriteRenderer),typeof(MonsterSpriteAnimator));SceneManager.MoveGameObjectToScene(go,preview);
                    var animator=go.GetComponent<MonsterSpriteAnimator>();animator.library=library;
                    var actor=new Actor{asset=entry.species,def=db.monsters.First(m=>m.asset==entry.species),position=new Vector2(1000,300),age=0};
                    animator.Sample(actor);check(animator.Pose=="enter-run",entry.species+" plays movement entry once");
                    actor.age=.18f;actor.position+=Vector2.left*10;animator.Sample(actor);var first=go.GetComponent<SpriteRenderer>().sprite;
                    check(animator.Pose=="run",entry.species+" enters the run loop");
                    actor.age=.29f;actor.position+=Vector2.left*10;animator.Sample(actor);
                    check(go.GetComponent<SpriteRenderer>().sprite!=first,entry.species+" advances run frames");
                    var paused=go.GetComponent<SpriteRenderer>().sprite;animator.Sample(actor);check(go.GetComponent<SpriteRenderer>().sprite==paused,entry.species+" freezes when battle age is unchanged");
                    actor.age=.34f;actor.attack=2;animator.Sample(actor);check(animator.Pose=="attack",entry.species+" responds to an actual attack cooldown reset");
                    actor.age=.39f;actor.flash=.1f;animator.Sample(actor);check(animator.Pose=="hit",entry.species+" responds to hit flash");
                    actor.age=2;actor.flash=0;actor.attack=1;animator.Sample(actor);check(animator.Pose=="idle",entry.species+" returns to idle when stationary");
                    actor.age=2.1f;actor.dead=true;animator.Sample(actor);check(animator.Pose=="hit"&&animator.Frame==5,entry.species+" holds final hit pose for death fade");
                    UnityEngine.Object.DestroyImmediate(go);
                }
                check(library.Find("bug-0").version==4,"Ant uses V4");check(library.entries.Where(e=>e.species!="bug-0").All(e=>e.version>=3),"Other species use V3 or newer");
                Directory.CreateDirectory("Logs");File.WriteAllText("Logs/monster-animation-validation.json",JsonConvert.SerializeObject(new{verdict="PASS",passed=checks.Count,checks},Formatting.Indented));
                Debug.Log("MONSTER_ANIMATION_VALIDATION_PASS: "+checks.Count);
            }
            finally{EditorSceneManager.ClosePreviewScene(preview);}
        }
        public static int CaptureCombatGallery()
        {
            if(!Application.isPlaying)throw new Exception("Capture monster combat gallery in Play Mode.");
            var cameraObject=new GameObject("Monster Gallery Camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.06f,.1f);camera.farClipPlane=3000;camera.cullingMask=1<<29;
            var target=new RenderTexture(1600,1000,24);camera.targetTexture=target;var old=RenderTexture.active;var view=new BattleView(camera);
            try
            {
                var library=Resources.Load<MonsterAnimationLibrary>("MonsterAnimations");var db=Catalog.Load();var battle=new Battle(db,Profile.New(db));battle.units.Clear();battle.enemies.Clear();
                for(int i=0;i<library.entries.Count;i++)
                {
                    var actor=battle.Spawn(library.entries[i].species,200+i%4*400);
                    actor.def=JsonConvert.DeserializeObject<MonsterDef>(JsonConvert.SerializeObject(actor.def));actor.def.size=96;
                    actor.position=new Vector2(200+i%4*400,800-i/4*175+actor.def.size*.43f);actor.age=0;
                }
                view.Render(battle);
                foreach(var actor in battle.enemies){actor.age=.2f;actor.position+=Vector2.left*2;}
                view.Render(battle);
                foreach(Transform t in view.root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
                view.root.transform.Find("Modular Cat Fortress").gameObject.SetActive(false);
                var animators=view.root.GetComponentsInChildren<MonsterSpriteAnimator>();
                if(animators.Length!=20||animators.Any(a=>!a.GetComponent<SpriteRenderer>().sprite))throw new Exception("Missing animated monster in combat gallery");
                camera.Render();RenderTexture.active=target;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
                try{image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();Directory.CreateDirectory("Captures/MonsterAnimations");File.WriteAllBytes("Captures/MonsterAnimations/combat-gallery.png",image.EncodeToPNG());}
                finally{UnityEngine.Object.DestroyImmediate(image);}
                return animators.Length;
            }
            finally{view.Dispose();RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(cameraObject);target.Release();UnityEngine.Object.DestroyImmediate(target);}
        }
    }
}
