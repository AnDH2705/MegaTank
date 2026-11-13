using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace CatTanks.Editor
{
    public static class CombatVfxValidation
    {
        [MenuItem("CatTanks/Combat/Test Effects and Capture Preview")]
        public static void Run()
        {
            if(!Application.isPlaying)throw new Exception("Enter Play Mode before testing combat effects.");
            CombatVfxSetup.Validate();
            var checks=new List<string>();var counts=new Dictionary<string,int>();
            Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);checks.Add(name);};
            var root=new GameObject("Combat VFX validation");
            var random=UnityEngine.Random.state;
            try
            {
                var db=Catalog.Load();var battle=new Battle(db,Profile.New(db));
                var player=new CombatVfxPlayer(root.transform);var lib=Resources.Load<CombatVfxLibrary>("CombatVfxLibrary");int id=10000;
                foreach(var entry in lib.entries)
                {
                    battle.effects.Clear();var e=new BattleEffect{id=id++,kind="impact",visual=entry.key,size=40,maxLife=1,life=.7f};battle.effects.Add(e);player.Render(battle);
                    var systems=root.GetComponentsInChildren<ParticleSystem>();int count=systems.Sum(p=>p.particleCount);counts.Add(entry.key,count);
                    check(count>0,entry.key+" emits visible particles");
                    float time=systems.Sum(p=>p.time);player.Render(battle);
                    check(count==systems.Sum(p=>p.particleCount)&&Mathf.Approximately(time,systems.Sum(p=>p.time)),entry.key+" freezes with battle time");
                    e.life=.5f;player.Render(battle);check(systems.Sum(p=>p.time)>time,entry.key+" advances with battle time");
                }
                int warmed=0;
                for(int round=0;round<3;round++)
                {
                    battle.effects.Clear();player.Render(battle);
                    for(int i=0;i<180;i++)battle.effects.Add(new BattleEffect{id=id++,kind="impact",visual=lib.entries[i%lib.entries.Count].key,size=40,maxLife=1,life=.7f});
                    player.Render(battle);check(player.ActiveCount<=lib.maxActive,"Burst "+round+" respects global cap");
                    foreach(var entry in lib.entries)check(root.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Combat VFX • "+entry.key)<=entry.maxInstances,"Burst "+round+" caps "+entry.key);
                    if(round==0)warmed=player.CreatedCount;else check(player.CreatedCount==warmed,"Burst "+round+" reuses pooled instances");
                }
                battle.effects.Clear();player.Render(battle);check(player.ActiveCount==0,"Expired events release all active particles");
                Directory.CreateDirectory("Logs");File.WriteAllText("Logs/combat-vfx-runtime-validation.json",JsonConvert.SerializeObject(new{verdict="PASS",passed=checks.Count,counts,pooledInstances=player.CreatedCount,checks},Formatting.Indented));
                Capture(db,lib);
                Debug.Log("COMBAT_VFX_TESTS_PASSED "+checks.Count);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Random.state=random;}
        }
        static void Capture(Catalog db,CombatVfxLibrary lib)
        {
            var cameraObject=new GameObject("VFX Preview Camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.07f,.12f);camera.orthographic=true;camera.orthographicSize=450;camera.farClipPlane=3000;camera.cullingMask=1<<30;
            var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;var old=RenderTexture.active;
            var view=new BattleView(camera);
            try
            {
                var b=new Battle(db,Profile.New(db),1,true);for(int i=0;i<4;i++)b.Spawn("bug-0",900+i*170);b.Step(.05f);
                b.effects.Clear();b.Effect("flash",new Vector2(410,450),new Vector2(355,450),"#ffffff",24,.1f);
                b.Effect("impact",new Vector2(960,240),Vector2.zero,"#ffffff",50,.32f,visual:"blast");
                b.Effect("impact",new Vector2(1140,240),Vector2.zero,"#ffffff",30,.32f,visual:"fire");
                b.Effect("impact",new Vector2(1310,240),Vector2.zero,"#ffffff",35,.32f,visual:"ice");
                foreach(var e in b.effects)e.life=e.maxLife*.7f;
                view.Render(b);SetLayer(view.root);Save(camera,rt,"combat-effects-preview");
                view.root.SetActive(false);
                var gallery=new GameObject("Effect Gallery");
                try
                {
                    var player=new CombatVfxPlayer(gallery.transform);b.effects.Clear();
                    for(int i=0;i<lib.entries.Count;i++)b.effects.Add(new BattleEffect{id=20000+i,kind="impact",visual=lib.entries[i].key,position=new Vector2(200+i%4*400,750-i/4*300),size=65,maxLife=1,life=.65f});
                    player.Render(b);SetLayer(gallery);Save(camera,rt,"combat-effects-gallery");
                }
                finally{UnityEngine.Object.DestroyImmediate(gallery);}
            }
            finally
            {
                view.Dispose();camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
        static void SetLayer(GameObject root){foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;}
        static void Save(Camera camera,RenderTexture rt,string name)
        {
            camera.Render();RenderTexture.active=rt;var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();Directory.CreateDirectory("Captures/CombatVFX");File.WriteAllBytes("Captures/CombatVFX/"+name+".png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
