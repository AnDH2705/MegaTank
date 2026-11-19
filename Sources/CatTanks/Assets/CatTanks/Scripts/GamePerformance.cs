using System;
using System.Diagnostics;
using Unity.Profiling;

namespace CatTanks
{
    public static class GamePerformance
    {
        public enum Area{Update,Simulation,World,UI}
        public struct Scope:IDisposable
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            readonly int area;readonly long start;readonly ProfilerMarker.AutoScope marker;
            public Scope(Area value){area=(int)value;marker=Markers[area].Auto();start=Stopwatch.GetTimestamp();}
            public void Dispose(){Ticks[area]+=Stopwatch.GetTimestamp()-start;marker.Dispose();}
#else
            public Scope(Area value){}
            public void Dispose(){}
#endif
        }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        static readonly ProfilerMarker[] Markers={new ProfilerMarker("CatTanks.Update"),new ProfilerMarker("CatTanks.Simulation"),new ProfilerMarker("CatTanks.World"),new ProfilerMarker("CatTanks.UI")};
        static readonly long[] Ticks=new long[4];
        public static double Read(Area area)=>Ticks[(int)area]*1000.0/Stopwatch.Frequency;
        public static void Reset(){Array.Clear(Ticks,0,Ticks.Length);}
#endif
    }
}
