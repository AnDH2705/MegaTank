using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace CatTanks.Editor
{
    public static class PerformanceRegression
    {
        public static void Validate(CatTanksGame game)
        {
            if(!Application.isBatchMode||Profile.TestSavePath==null)throw new Exception("Use an isolated test editor.");
            game.modal="";game.StartBattle();game.enabled=false;
            var random=new System.Random(9021);var battle=game.battle;
            for(int i=0;i<60;i++)battle.Spawn(game.db.monsters[i%game.db.monsters.Count].asset,800+i*10);
            var unit=battle.units[0];string originalKind=unit.weapon.kind;int targetCases=0;
            for(int i=0;i<200;i++)
            {
                unit.weapon.kind=i%2==0?"sniper":"gun";
                battle.targetId=i%3==0?battle.enemies[i%battle.enemies.Count].id:-1;
                foreach(var enemy in battle.enemies)
                {enemy.dead=random.Next(5)==0;enemy.position=new Vector2(random.Next(-150,1800),random.Next(100,750));enemy.hp=random.Next(1,1000);}
                var candidates=battle.enemies.Where(e=>!e.dead&&e.position.x>-100&&e.position.x<1720&&Vector2.Distance(e.position,unit.position)<unit.weapon.range);
                var expected=candidates.FirstOrDefault(e=>e.id==battle.targetId);
                if(expected==null)expected=unit.weapon.kind=="sniper"?candidates.OrderByDescending(e=>(e.def.boss?900:0)+(e.def.armor>0?200:0)).ThenBy(e=>e.hp).FirstOrDefault():candidates.OrderBy(e=>Vector2.Distance(e.position,unit.position)).FirstOrDefault();
                if(battle.Target(unit)!=expected)throw new Exception("Target selection changed in case "+i);
                targetCases++;
            }
            unit.weapon.kind=originalKind;battle.enemies.Clear();battle.effects.Clear();
            int warmed=0,final=0;
            for(int frame=0;frame<400;frame++)
            {
                battle.effects.Clear();
                for(int slot=0;slot<64;slot++)battle.effects.Add(new BattleEffect{kind="text",position=new Vector2(600+slot*8+frame*.1f,300+frame*.3f),life=.4f,maxLife=1,text=(frame+slot).ToString(),color=Color.white});
                game.RefreshCanvas();
                if(frame==20)warmed=game.CanvasRoot.GetComponentsInChildren<StudioWidgetBinding>(true).Length;
            }
            final=game.CanvasRoot.GetComponentsInChildren<StudioWidgetBinding>(true).Length;
            int pooled=game.CanvasRoot.GetComponentsInChildren<StudioWidgetBinding>(true).Count(b=>b.id.StartsWith("text:floating:"));
            if(final!=warmed||pooled!=64)throw new Exception("Damage text pool grew with movement: "+warmed+" -> "+final+", pooled="+pooled);
            battle.effects.Clear();battle.bullets.Clear();battle.orbs.Clear();game.view.Render(battle);
            int projectileWarmup=0;
            for(int cycle=0;cycle<100;cycle++)
            {
                battle.bullets.Clear();
                for(int i=0;i<8;i++)battle.MakeBullet(new Vector2(600+i*10,400),0,game.db.Weapon("gun-0"),null);
                game.view.Render(battle);
                if(cycle==4)projectileWarmup=game.view.CreatedProjectileViews;
            }
            if(game.view.CreatedProjectileViews!=projectileWarmup)throw new Exception("Projectile visuals keep allocating after pool warmup.");
            var overlay=UnityEngine.Object.FindObjectOfType<FrameRateOverlay>();
            if(!overlay||!overlay.Label||overlay.Label.raycastTarget||overlay.GetComponent<UnityEngine.UI.GraphicRaycaster>())throw new Exception("FPS overlay missing or intercepting combat input.");
            File.WriteAllText("Logs/performance-regression.json",JsonConvert.SerializeObject(new{verdict="PASS",targetCases,damageEvents=400*64,widgetsAfterWarmup=warmed,widgetsAfterMovement=final,pooledTexts=pooled,projectileEvents=800,projectileViewsAfterWarmup=projectileWarmup,projectileViewsAfterCombat=game.view.CreatedProjectileViews,fpsOverlayVisible=true},Formatting.Indented));
            Debug.Log("PERFORMANCE_REGRESSION_PASS: target equivalence and bounded damage text pool.");
        }
    }
}
