using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    public sealed partial class HtmlBattleScreen
    {
        RectTransform chestLid;float chestAge;
        void BossChest()
        {
            chestAge=0;var root=Rect(modal,"Automatic Boss Chest",710,405,500,210);
            var glow=Rect(root,"Comic Burst",150,-15,200,150).gameObject.AddComponent<NativeComicBurst>();glow.color=new Color(1,.9f,.52f,.35f);
            StaticImage(root,"Chest Border","",194,46,112,65,new Color(.15f,.15f,.2f));
            StaticImage(root,"Chest Wood","",200,50,100,55,new Color(.67f,.35f,.17f));
            for(int x=211;x<=275;x+=64)StaticImage(root,"Gold Band","",x,50,13,55,new Color(1,.83f,.35f));
            StaticImage(root,"Lock Border","",239,63,24,28,new Color(.32f,.25f,.17f));StaticImage(root,"Lock","",243,67,16,20,new Color(1,.9f,.55f));
            chestLid=Rect(root,"Opening Lid",192,28,116,44);chestLid.pivot=new Vector2(.06f,.15f);
            StaticImage(chestLid,"Lid Outline","",0,0,116,44,new Color(.15f,.15f,.2f));StaticImage(chestLid,"Lid Wood","",4,4,108,36,new Color(.82f,.55f,.21f));
            for(int x=18;x<=82;x+=64)StaticImage(chestLid,"Lid Band","",x,4,13,36,new Color(1,.83f,.35f));
            StaticImage(root,"Coins","counter_coin",68,130,44,44).preserveAspect=true;Label(root,"Coin Reward","+"+config.chestMoney,120,130,105,44,27);
            StaticImage(root,"Gears","counter_gear",270,130,44,44).preserveAspect=true;Label(root,"Gear Reward","+"+config.chestGears,325,130,100,44,27);
            Label(root,"Permanent EXP","TANK EXP +"+awardedTankXP,50,175,400,35,22,new Color(1,.9f,.55f),TextAnchor.MiddleCenter);
        }
        void AnimateChest()
        {
            if(chestLid==null||Simulation.phase!="won")return;if(!paused)chestAge+=Time.unscaledDeltaTime;
            float t=Mathf.Clamp01((chestAge-.2f)/.6f);chestLid.localRotation=Quaternion.Euler(0,0,t*17);chestLid.anchoredPosition=new Vector2(192,-28+t*16);
        }
    }
    // Native vector version of the quadratic comic burst in the design source.
    public sealed class NativeComicBurst : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var points=new List<Vector2>{new Vector2(18,39),new Vector2(5,24)};
            float[] q={25,32,31,12,40,28,53,4,59,25,79,9,86,28,105,4,103,27,126,15,124,36,151,25,133,42,158,49,137,54,150,76,128,64,126,92,110,76,94,99,87,80,67,94,65,75,43,92,45,72,18,79,29,61,4,59,25,56,18,39};
            for(int i=0;i<q.Length;i+=4){var start=points[points.Count-1];var c=new Vector2(q[i],q[i+1]);var end=new Vector2(q[i+2],q[i+3]);for(int step=1;step<=5;step++){float t=step/5f;points.Add((1-t)*(1-t)*start+2*(1-t)*t*c+t*t*end);}}
            var rect=rectTransform.rect;var center=new Vector2(80,52);int count=points.Count;
            for(int layer=0;layer<2;layer++)
            {
                int offset=vh.currentVertCount;Color tint=layer==0?new Color(.067f,.067f,.086f,color.a):color;
                vh.AddVert(new Vector3(rect.xMin+rect.width*.5f,rect.yMin+rect.height*.5f),tint,Vector2.zero);
                foreach(var point in points){var p=center+(point-center)*(layer==0?1.06f:1);vh.AddVert(new Vector3(rect.xMin+(p.x+6)/172*rect.width,rect.yMax-(p.y+6)/112*rect.height),tint,Vector2.zero);}
                for(int i=0;i<count;i++)vh.AddTriangle(offset,offset+1+i,offset+1+(i+1)%count);
            }
        }
    }
}
