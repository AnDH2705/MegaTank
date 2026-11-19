#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace CatTanks
{
    // Explicit command-line opt-in for editor automation, before any scene Awake/Profile.Load.
    static class EditorTestProfile
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Configure()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-catTanksTestProfile");
            if(at<0)return;
            if(at+1>=args.Length)throw new InvalidOperationException("Missing test profile path");
            string path=Path.GetFullPath(args[at+1]);string logs=Path.GetFullPath("Logs")+Path.DirectorySeparatorChar;
            if(!path.StartsWith(logs,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Test profiles must be inside this project's Logs folder.");
            Profile.TestSavePath=path;
        }
    }
}
#endif
