using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    // Projects the simulation route without changing wave/spawn/reward state.
    public sealed class HtmlEventTimeline : MonoBehaviour
    {
        sealed class Node { public string type;public int first,last; }
        readonly List<Node> route=new List<Node>();
        readonly RectTransform[] outer=new RectTransform[3],inner=new RectTransform[3];
        readonly Image[] icons=new Image[3],cores=new Image[3];
        readonly Text[] ticks=new Text[2];
        HtmlBattleArt art;HtmlBattleSimulation simulation;Text heading,caption,bossLabel;
        int cursor=-1,oldCursor;float clock=1,entryRoad;float[] oldX=new float[3],visualX=new float[3];
        const float scale=.84f;
        RectTransform R(Transform p,string n,float x,float y,float w=0,float h=0)
        {var r=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x*scale,-y*scale);r.sizeDelta=new Vector2(w*scale,h*scale);return r;}
        Image I(Transform p,string key,float x,float y,float w,float h)
        {var i=R(p,key,x,y,w,h).gameObject.AddComponent<Image>();i.sprite=art.Image("timeline_"+key)?.sprite;i.raycastTarget=false;return i;}
        Text T(Transform p,string n,float x,float y,float w,float h,int size)
        {var t=R(p,n,x,y,w,h).gameObject.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=Mathf.RoundToInt(size*scale);t.fontStyle=FontStyle.BoldAndItalic;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(1,.973f,.918f);t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;var o=t.gameObject.AddComponent<Outline>();o.effectColor=new Color(.09f,.08f,.11f);o.effectDistance=new Vector2(2,-2);return t;}
        public void Initialize(HtmlBattleArt source)
        {
            art=source;
            // Ordinary nodes use the source SVG clip (0,26,580,118); boss remains outside it.
            var outerClip=R(transform,"Outer Node Clip",0,26,580,118);outerClip.gameObject.AddComponent<RectMask2D>();
            for(int i=0;i<3;i++){outer[i]=R(i==2?transform:outerClip,"Outer "+i,0,i==2?82:56);I(outer[i],i==2?"boss_outer":"node_outer",i==2?-54:-40,i==2?-54:-40,i==2?108:80,i==2?108:80);}
            I(transform,"rail_empty",60,68,520,28).type=Image.Type.Sliced;
            var progress=I(transform,"rail_progress",60,79,260,6);progress.color=new Color(1,.81f,.235f);progress.type=Image.Type.Sliced;
            var innerClip=R(transform,"Inner Node Clip",0,26,580,118);innerClip.gameObject.AddComponent<RectMask2D>();
            for(int i=0;i<3;i++)
            {
                inner[i]=R(i==2?transform:innerClip,"Inner "+i,0,i==2?82:56);cores[i]=I(inner[i],i==2?"boss_inner":"node_inner",i==2?-46:-32,i==2?-46:-32,i==2?92:64,i==2?92:64);
                icons[i]=I(inner[i],"event_battle",i==2?-51:-38,i==2?-51:-38,i==2?102:76,i==2?102:76);
                if(i<2){ticks[i]=T(inner[i],"Completed",8,13,34,34,28);ticks[i].text="✓";ticks[i].color=new Color(.44f,.91f,.64f);}
            }
            I(transform,"player_pointer",306,15,28,20);bossLabel=T(transform,"Boss Caption",510,136,140,32,22);bossLabel.text="BOSS";
            heading=T(transform,"Heading",190,132,260,30,22);caption=T(transform,"Caption",190,158,260,30,22);
        }
        void Route(HtmlBattleSimulation s)
        {
            simulation=s;route.Clear();cursor=-1;clock=1;
            if(s.HasEntryUpgrade)route.Add(new Node{type="upgrade"});
            int stop=0;for(int start=1;start<=s.config.wavesPerStage;start+=s.config.wavesPerEncounter)
            {
                int end=Math.Min(s.config.wavesPerStage,start+s.config.wavesPerEncounter-1);route.Add(new Node{type=s.stageIndex>0?"hard_battle":"battle",first=start,last=end});
                if(end%s.config.wavesPerEncounter==0){string type=s.stage.requireRecovery&&stop==0?"wreck":s.config.encounterDeck[stop%s.config.encounterDeck.Length];if(++stop%s.config.upgradeEveryGroups==0)type="upgrade";route.Add(new Node{type=Alias(type),last=end});}
            }
            route.Add(new Node{type="boss"});
        }
        static string Alias(string type)=>type=="medical"?"hp":type=="wreck"?"unknown":type=="supply"?"ammo":type;
        public void Render(HtmlBattleSimulation s,bool paused)
        {
            if(simulation!=s)Route(s);int n=route.Count-1,next;
            if(s.bossSpawned)next=n;
            else if(s.IsEntryUpgrade)next=0;
            else if(s.phase=="approach"||s.phase=="choice"||s.phase=="depart"||s.phase=="upgrade")next=route.FindIndex(e=>e.first==0&&e.last==s.wave);
            else next=route.FindIndex(e=>e.first>0&&e.first<=s.wave&&e.last>=s.wave);
            next=Math.Max(0,next);
            if(next!=cursor){oldCursor=cursor;Array.Copy(visualX,oldX,3);cursor=next;clock=oldCursor<0?1:0;entryRoad=s.road;}
            if(!paused)clock+=Time.unscaledDeltaTime;
            float progress=1-Mathf.Exp(-Mathf.Max(0,s.road-entryRoad)/520),t=1-Mathf.Pow(1-Mathf.Clamp01(clock/.32f),3);
            int first=Math.Min(cursor,Math.Max(0,n-2));int[] indices={first,first+1,n};
            float[] targets=cursor>=n?new[]{72f,180f,320f}:cursor==n-1?new[]{112f,320f,580f}:new[]{320f,Mathf.Lerp(580,460,progress),580f};
            if(clock<.32f&&cursor==oldCursor+1&&cursor<n-1){indices[0]=cursor-1;indices[1]=cursor;targets[0]=-64;targets[1]=320;}
            for(int i=0;i<3;i++)
            {
                float from=oldX[i];if(i<2&&cursor==oldCursor+1&&cursor<n-1)from=i==0?oldX[0]:oldX[1];
                float x=clock<.32f?Mathf.Lerp(from,targets[i],t):targets[i];
                if(clock>=.32f&&clock<.5f&&i==1&&cursor<n-2)x=Mathf.Lerp(580,targets[i],1-Mathf.Pow(1-Mathf.Clamp01((clock-.32f)/.18f),3));
                visualX[i]=x;outer[i].anchoredPosition=inner[i].anchoredPosition=new Vector2(x*scale,-(i==2?82:56)*scale);
                bool complete=indices[i]<cursor;icons[i].sprite=art.Image("timeline_event_"+route[indices[i]].type)?.sprite;
                icons[i].color=new Color(1,1,1,complete?.42f:1);cores[i].color=indices[i]==cursor?new Color(1,.855f,.376f):new Color(1,.96f,.875f);
                if(i<2)ticks[i].gameObject.SetActive(complete);
            }
            bossLabel.rectTransform.anchoredPosition=new Vector2((visualX[2]-70)*scale,-136*scale);
            var current=route[cursor];bool battle=current.first>0;
            heading.text=cursor==n?"":battle?"WAVE":current.type.ToUpperInvariant();caption.text=battle?(s.wave-current.first+1).ToString("00")+" / "+(current.last-current.first+1).ToString("00"):"";
        }
    }
}
