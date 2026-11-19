using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatTanks.Editor
{
    public static class PerformanceBuild
    {
        public static void Build()
        {
            if(!Application.isBatchMode)throw new Exception("Use a dedicated build editor.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-benchmarkBuildName");string name=at>=0?args[at+1]:"After";
            if(name!="Before"&&name!="After"&&name!="HitchBefore"&&name!="HitchAfter")throw new Exception("Unknown benchmark build name.");
            string folder="Builds/Performance"+name;Directory.CreateDirectory(folder);
            bool previous=PlayerSettings.enableFrameTimingStats;PlayerSettings.enableFrameTimingStats=true;
            try
            {
                EditorSceneManager.OpenScene("Assets/CatTanks/Scenes/Main.unity",OpenSceneMode.Single);
                var report=BuildPipeline.BuildPlayer(new[]{"Assets/CatTanks/Scenes/Main.unity"},folder+"/CatTanks.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Benchmark build failed: "+report.summary.result);
                Debug.Log("PERFORMANCE_BUILD_COMPLETE: "+folder);
            }
            finally{PlayerSettings.enableFrameTimingStats=previous;AssetDatabase.SaveAssets();}
        }
    }
}
