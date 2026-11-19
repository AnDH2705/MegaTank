#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.Profiling;
using UnityEngine;

namespace CatTanks
{
    [DefaultExecutionOrder(10000)]
    public sealed class PerformanceBenchmark:MonoBehaviour
    {
        public static bool Active{get;private set;}
        public static bool Ready{get;private set;}
        static string output,label;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-catTanksBenchmark");if(index<0)return;
            Active=true;Ready=false;
            try
            {
                label=args[index+1];string logs=Path.GetFullPath("Logs")+Path.DirectorySeparatorChar;
                string path=Path.GetFullPath(args[Array.IndexOf(args,"-catTanksTestProfile")+1]);
                output=Path.GetFullPath(args[Array.IndexOf(args,"-benchmarkOutput")+1]);
                if(!path.StartsWith(logs,StringComparison.OrdinalIgnoreCase)||!output.StartsWith(logs,StringComparison.OrdinalIgnoreCase))throw new Exception("Benchmark files must be inside project Logs.");
                Profile.TestSavePath=path;Ready=true;Application.runInBackground=true;
                new GameObject("Performance Benchmark").AddComponent<PerformanceBenchmark>();
            }
            catch(Exception e){Debug.LogError(e);Application.Quit(2);}
        }
        public static void RequireSafeProfile(){if(Active&&!Ready)throw new InvalidOperationException("Benchmark profile isolation was not configured.");}
        struct Sample
        {
            public double frame,update,simulation,world,ui,cpu,gpu,canvas,willRender,at;public long allocated,draws,batches;public int gc;
        }
        readonly List<Sample> samples=new List<Sample>(65536);
        readonly List<object> results=new List<object>();
        readonly FrameTiming[] timings=new FrameTiming[1];
        ProfilerRecorder allocations,draws,batches;
        ProfilerRecorder canvasBuild,canvasRender;
        bool exercise;float nextBurst,nextSkill;int exerciseIndex,lastGc;
        CatTanksGame game;bool collecting;int configuredTarget,configuredVsync,widgetsBefore;
        double started,previous;
        IEnumerator Start()
        {
            allocations=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);
            batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);
            canvasBuild=ProfilerRecorder.StartNew(ProfilerCategory.Gui,"Canvas.BuildBatch",1);
            canvasRender=ProfilerRecorder.StartNew(ProfilerCategory.Gui,"Canvas.SendWillRenderCanvases",1);
            yield return null;
            game=FindObjectOfType<CatTanksGame>();
            if(!game||!Ready||Profile.TestSavePath==null){Fail("No isolated live game");yield break;}
            game.profile=Profile.New(game.db);game.profile.accountLevel=100;game.profile.highestCleared=20;game.muted=true;
            configuredTarget=Application.targetFrameRate;configuredVsync=QualitySettings.vSyncCount;
            if(Environment.GetCommandLineArgs().Contains("-catTanksHitchTest"))
            {
                ConfigureCombat(60,true);exercise=true;
                yield return Measure("sustained_combat",5,90);
                exercise=false;
            }
            else
            {
            yield return Measure("configured_hub",2,4);
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            yield return Measure("unlimited_hub",2,6);
            ConfigureCombat(12,false);yield return Measure("combat_12",4,12);
            ConfigureCombat(60,true);yield return Measure("stress_60",4,12);
            QualitySettings.vSyncCount=configuredVsync;Application.targetFrameRate=configuredTarget;
            ConfigureCombat(12,false);yield return Measure("configured_combat_12",4,8);
            ConfigureCombat(60,true);yield return Measure("configured_stress_60",4,12);
            }
            collecting=false;Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output,JsonConvert.SerializeObject(new{label,unity=Application.unityVersion,editor=Application.isEditor,batch=Application.isBatchMode,screen=new{Screen.width,Screen.height},configuredTarget,configuredVsync,hardware=new{SystemInfo.processorType,SystemInfo.processorCount,SystemInfo.graphicsDeviceName,SystemInfo.graphicsDeviceType,SystemInfo.graphicsMemorySize,SystemInfo.systemMemorySize},frameTimingsAvailable=FrameTimingManager.IsFeatureEnabled(),results},Formatting.Indented));
            Debug.Log("PERFORMANCE_BENCHMARK_COMPLETE: "+output);
            if(!Application.isBatchMode)
            {
                ScreenCapture.CaptureScreenshot(Path.ChangeExtension(output,".png"));
                yield return new WaitForSecondsRealtime(.3f);
            }
#if UNITY_EDITOR
            if(Application.isEditor){UnityEditor.EditorApplication.Exit(0);yield break;}
#endif
            Application.Quit(0);
        }
        void ConfigureCombat(int count,bool classic)
        {
            UnityEngine.Random.InitState(137+count);game.StartBattle(classic);
            game.battle.hp=game.battle.maxHp=1e9f;
            foreach(var actor in game.battle.units)actor.hp=actor.maxHp=1e9f;
            for(int i=0;i<count;i++)
            {
                var actor=game.battle.Spawn(game.db.monsters[i%4].asset,850+(i%9)*75);actor.hp=actor.maxHp=1e9f;
            }
        }
        IEnumerator Measure(string name,float warmup,float duration)
        {
            collecting=false;yield return new WaitForSecondsRealtime(warmup);
            samples.Clear();widgetsBefore=game.CanvasRoot.GetComponentsInChildren<StudioWidgetBinding>(true).Length;
            previous=started=Time.realtimeSinceStartupAsDouble;GamePerformance.Reset();collecting=true;
            while(Time.realtimeSinceStartupAsDouble-started<duration)yield return null;
            collecting=false;double elapsed=Time.realtimeSinceStartupAsDouble-started;
            var sorted=samples.Select(s=>s.frame).OrderBy(v=>v).ToArray();
            double Percent(double p)=>sorted.Length>0?sorted[Mathf.Clamp((int)(sorted.Length*p),0,sorted.Length-1)]:0;
            // DX11 can return an invalid GPU timestamp after a disjoint timing query.
            // Exclude unavailable/nonphysical readings rather than reporting them as GPU work.
            var cpu=samples.Where(s=>s.cpu>0&&s.cpu<1000).Select(s=>s.cpu).ToArray();
            var gpu=samples.Where(s=>s.gpu>0&&s.gpu<1000).Select(s=>s.gpu).ToArray();
            results.Add(new{name=name+"_hitches",over16ms=samples.Count(s=>s.frame>16.67),over33ms=samples.Count(s=>s.frame>33.33),over50ms=samples.Count(s=>s.frame>50),maxFrameMs=samples.Max(s=>s.frame),gcCollections=samples.Sum(s=>s.gc),canvasBuildAvailable=canvasBuild.Valid,canvasRenderAvailable=canvasRender.Valid,canvasBuildMaxMs=samples.Max(s=>s.canvas),canvasRenderMaxMs=samples.Max(s=>s.willRender),slowest=samples.OrderByDescending(s=>s.frame).Take(30).ToArray()});
            results.Add(new{name,frames=samples.Count,elapsed,fps=samples.Count/elapsed,frameMs=samples.Average(s=>s.frame),p50Ms=Percent(.5),p95Ms=Percent(.95),p99Ms=Percent(.99),over10ms=samples.Count(s=>s.frame>10),updateMs=samples.Average(s=>s.update),simulationMs=samples.Average(s=>s.simulation),worldMs=samples.Average(s=>s.world),uiMs=samples.Average(s=>s.ui),uiMaxMs=samples.Max(s=>s.ui),cpuFrameMs=cpu.DefaultIfEmpty(0).Average(),gpuFrameMs=gpu.DefaultIfEmpty(0).Average(),cpuTimingSamples=cpu.Length,gpuTimingSamples=gpu.Length,gcBytesPerFrame=samples.Average(s=>(double)s.allocated),drawCalls=samples.Average(s=>(double)s.draws),batches=samples.Average(s=>(double)s.batches),widgetsBefore,widgetsAfter=game.CanvasRoot.GetComponentsInChildren<StudioWidgetBinding>(true).Length,enemies=game.battle.enemies.Count,projectiles=game.battle.bullets.Count,effects=game.battle.effects.Count,vfxActive=game.view.combatVfx.ActiveCount});
        }
        void LateUpdate()
        {
            if(exercise&&game&&game.inBattle)
            {
                if(Time.unscaledTime>=nextBurst)
                {
                    nextBurst=Time.unscaledTime+3;
                    for(int i=0;i<4;i++)game.battle.Spawn(game.db.monsters[(exerciseIndex++)%game.db.monsters.Count].asset,800+i*90);
                }
                if(Time.unscaledTime>=nextSkill)
                {
                    nextSkill=Time.unscaledTime+1;game.battle.energy=600;
                    foreach(var skill in game.battle.skills)game.battle.UseSkill(skill.id);
                }
            }
            int collections=GC.CollectionCount(0),gcDelta=collections-lastGc;lastGc=collections;
            if(collecting)
            {
                double now=Time.realtimeSinceStartupAsDouble;double cpu=0,gpu=0;
                if(FrameTimingManager.IsFeatureEnabled())
                {
                    FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timings)>0){cpu=timings[0].cpuFrameTime;gpu=timings[0].gpuFrameTime;}
                }
                samples.Add(new Sample{at=now-started,frame=(now-previous)*1000,update=GamePerformance.Read(GamePerformance.Area.Update),simulation=GamePerformance.Read(GamePerformance.Area.Simulation),world=GamePerformance.Read(GamePerformance.Area.World),ui=GamePerformance.Read(GamePerformance.Area.UI),cpu=cpu,gpu=gpu,allocated=allocations.Valid?allocations.LastValue:0,draws=draws.Valid?draws.LastValue:0,batches=batches.Valid?batches.LastValue:0,canvas=canvasBuild.Valid?canvasBuild.LastValue/1e6:0,willRender=canvasRender.Valid?canvasRender.LastValue/1e6:0,gc=gcDelta});previous=now;
            }
            GamePerformance.Reset();
        }
        void Fail(string message){Debug.LogError("PERFORMANCE_BENCHMARK_FAILED: "+message);Application.Quit(2);}
        void OnDestroy(){allocations.Dispose();draws.Dispose();batches.Dispose();canvasBuild.Dispose();canvasRender.Dispose();}
    }
}
#endif
