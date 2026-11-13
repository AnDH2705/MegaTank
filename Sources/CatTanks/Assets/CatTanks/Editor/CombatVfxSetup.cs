using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace CatTanks.Editor
{
    public static class CombatVfxSetup
    {
        const string Folder="Assets/CatTanks/Resources/CombatVFX";
        [MenuItem("CatTanks/Combat/Build Selected Combat Effects")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            const string asset="Assets/CatTanks/Resources/CombatVfxLibrary.asset";
            var library=AssetDatabase.LoadAssetAtPath<CombatVfxLibrary>(asset);
            if(!library){library=ScriptableObject.CreateInstance<CombatVfxLibrary>();AssetDatabase.CreateAsset(library,asset);}
            library.entries.Clear();
            Add(library,"muzzle","Assets/118 sprite effects bundle/MuzzleFlashes/ef_1_red.prefab",.10f,2,10);
            Add(library,"impact","Assets/Epic Toon FX/Prefabs 2D/Explosions/MetalHit2D.prefab",.55f,2.8f,10);
            Add(library,"blast","Assets/FX Quest/_Mobile/Prefabs/2D/Explosions/M_2D_AirExplode_01.prefab",.8f,2.8f,8);
            Add(library,"death","Assets/FX Quest/_Mobile/Prefabs/2D/Misc/M_2D_SmokePuff_01.prefab",.75f,3,8);
            Add(library,"fire","Assets/FX Quest/_Mobile/Prefabs/2D/Magics/M_2D_FireHit_01.prefab",.65f,3,8);
            Add(library,"ice","Assets/Epic Toon FX/Prefabs/Combat/Explosions/FrostExplosion/FrostExplosionBlue.prefab",.85f,3,6);
            Add(library,"poison","Assets/Epic Toon FX/Prefabs 2D/Blood/Green/GreenBloodExplosion2D.prefab",.8f,2.8f,6);
            Add(library,"electric","Assets/FX Quest/_Mobile/Prefabs/2D/Magics/M_2D_ThunderHit_01.prefab",.5f,9,8);
            Add(library,"heal","Assets/FX Quest/_Mobile/Prefabs/2D/Abilities/M_2D_Recovery_01.prefab",1.2f,1.15f,3);
            Add(library,"shield","Assets/FX Quest/_Mobile/Prefabs/2D/Abilities/M_2D_BoostDefense_01.prefab",.9f,1.1f,3);
            Add(library,"buff","Assets/FX Quest/_Mobile/Prefabs/2D/Abilities/M_2D_Buff_01.prefab",.9f,.85f,4);
            Add(library,"capture","Assets/FX Quest/_Mobile/Prefabs/2D/Misc/M_2D_PickItem_02.prefab",.8f,1.6f,3);
            EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();Validate();
        }
        static void Add(CombatVfxLibrary library,string key,string source,float duration,float size,int cap)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(source);if(!prefab)throw new Exception("Missing effect: "+source);
            var wrapper=new GameObject("VFX_"+key);var child=UnityEngine.Object.Instantiate(prefab,wrapper.transform,false);child.name="Effect";child.transform.localPosition=Vector3.zero;
            foreach(var script in wrapper.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
            foreach(var collider in wrapper.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
            foreach(var audio in wrapper.GetComponentsInChildren<AudioSource>(true))UnityEngine.Object.DestroyImmediate(audio);
            foreach(var light in wrapper.GetComponentsInChildren<Light>(true))UnityEngine.Object.DestroyImmediate(light);
            var systems=wrapper.GetComponentsInChildren<ParticleSystem>(true);
            foreach(var ps in systems)
            {
                var main=ps.main;main.loop=false;main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.None;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.maxParticles=Mathf.Min(main.maxParticles,256);
                ps.useAutoRandomSeed=false;ps.randomSeed=17;
                var collision=ps.collision;collision.enabled=false;var trigger=ps.trigger;trigger.enabled=false;
                ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            foreach(var renderer in wrapper.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sortingLayerName="Default";renderer.sortingOrder=46;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                foreach(var material in renderer.sharedMaterials)
                    if(material&&(!material.shader||!material.shader.isSupported))throw new Exception("Effect material unsupported by Built-in RP: "+source);
            }
            float extent=Measure(wrapper,systems);child.transform.localScale/=Mathf.Max(.01f,extent);
            foreach(var ps in systems)ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var result=PrefabUtility.SaveAsPrefabAsset(wrapper,Folder+"/VFX_"+key+".prefab");UnityEngine.Object.DestroyImmediate(wrapper);
            library.entries.Add(new CombatVfxLibrary.Entry{key=key,prefab=result,source=source,duration=duration,sizeMultiplier=size,maxInstances=cap});
        }
        static float Measure(GameObject root,ParticleSystem[] systems)
        {
            var bounds=new Bounds(Vector3.zero,Vector3.zero);bool any=false;
            foreach(float sample in new[]{.02f,.08f,.16f,.3f,.45f})foreach(var ps in systems)
            {
                ps.Simulate(sample,false,true,true);var particles=new ParticleSystem.Particle[Mathf.Min(4096,ps.main.maxParticles)];int count=ps.GetParticles(particles);
                for(int i=0;i<count;i++)
                {
                    var p=particles[i];Vector3 pos=ps.main.simulationSpace==ParticleSystemSimulationSpace.World?p.position:ps.transform.TransformPoint(p.position);
                    float radius=p.GetCurrentSize(ps)*Mathf.Max(ps.transform.lossyScale.x,ps.transform.lossyScale.y)*.5f;
                    bounds.Encapsulate(pos+Vector3.one*radius);bounds.Encapsulate(pos-Vector3.one*radius);any=true;
                }
            }
            return any?Mathf.Max(bounds.size.x,bounds.size.y):2;
        }
        [MenuItem("CatTanks/Combat/Validate Combat Effects")]
        public static void Validate()
        {
            var library=Resources.Load<CombatVfxLibrary>("CombatVfxLibrary");var issues=new List<string>();
            if(!library)throw new Exception("Missing combat VFX library");
            foreach(var entry in library.entries)
            {
                if(!entry.prefab){issues.Add("Missing prefab: "+entry.key);continue;}
                var systems=entry.prefab.GetComponentsInChildren<ParticleSystem>(true);if(systems.Length==0)issues.Add("No particles: "+entry.key);
                foreach(var ps in systems)if(ps.main.loop||ps.main.stopAction!=ParticleSystemStopAction.None)issues.Add("Not pool safe: "+entry.key);
                if(entry.prefab.GetComponentsInChildren<MonoBehaviour>(true).Length>0)issues.Add("Unexpected effect script: "+entry.key);
            }
            File.WriteAllText("Logs/combat-vfx-validation.json",JsonConvert.SerializeObject(new{verdict=issues.Count==0?"PASS":"FAIL",effects=library.entries.Count,issues},Formatting.Indented));
            if(issues.Count>0)throw new Exception(string.Join("\n",issues));Debug.Log("COMBAT_VFX_VALIDATED "+library.entries.Count);
        }
    }
}
