using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    public sealed partial class CanvasGameUI
    {
        int inspectedNode=-1;
        readonly Dictionary<string,int> previousAmmo=new Dictionary<string,int>();
        readonly Dictionary<string,float> emptyAlertUntil=new Dictionary<string,float>();
        int ammoRunId=-1;

        public static int RestoredAmmo(int amount,int capacity,float fraction)=>Mathf.Max(0,Mathf.Min(capacity-amount,Mathf.CeilToInt(capacity*Mathf.Max(0,fraction))));

        string CardPreview(CardDefinition card)
        {
            if(card.effect==CardEffect.Resupply)
            {
                int mg=RestoredAmmo(game.MachineGun.Ammo,game.MachineGun.ReserveCapacity,card.fraction);
                int cannon=RestoredAmmo(game.MainGun.Ammo,game.MainGun.ReserveCapacity,card.fraction);
                int mortar=RestoredAmmo(game.Mortar.Ammo,game.Mortar.Stats.reserveCapacity,card.fraction);
                return "Restore "+Mathf.RoundToInt(card.fraction*100)+"% ammo\nMG +"+mg+" · CANNON +"+cannon+"\nMORTAR +"+mortar+
                    (mg+cannon+mortar==0?"\nFULL AMMO — no benefit":
                    (game.MachineGun.Ammo==game.MachineGun.ReserveCapacity||game.MainGun.Ammo==game.MainGun.ReserveCapacity||game.Mortar.Ammo==game.Mortar.Stats.reserveCapacity?"\nFull reserves get no ammo":"\nCapped to reserve space"));
            }
            if(card.effect==CardEffect.Repair)
            {
                float after=Mathf.Min(game.MaxHull,game.Hull+Mathf.Ceil(game.MaxHull*card.fraction));
                return "Restore "+Mathf.RoundToInt(card.fraction*100)+"% hull\nHULL "+game.Hull.ToString("0.#")+" → "+after.ToString("0.#")+
                    (game.Hull>=game.MaxHull?"\nFULL HULL — no benefit":"\n+"+(after-game.Hull).ToString("0.#")+" HP (capped at "+game.MaxHull.ToString("0.#")+")");
            }
            if(card.effect==CardEffect.MaxHull)
            {
                // Use the same starting maximum as ApplyCardEffect, independent of prior upgrades.
                float baseHull=game.StartingMaxHull;
                float gain=baseHull*Mathf.Max(0,card.fraction);
                return card.EffectText+"\nMAX HP "+game.MaxHull.ToString("0.#")+" → "+(game.MaxHull+gain).ToString("0.#")+"\nHULL "+game.Hull.ToString("0.#")+" → "+(game.Hull+gain).ToString("0.#");
            }
            var gun=card.effect==CardEffect.CannonDamage?game.MainGun:game.MachineGun;
            return card.EffectText+"\nDAMAGE "+gun.DamagePerShot.ToString("0.##")+" → "+(gun.DamagePerShot+gun.Stats.damage*Mathf.Max(0,card.fraction)).ToString("0.##");
        }

        void UpdateAmmoWarning(UIView v,string key,int amount,int capacity)
        {
            if(game.RunId!=ammoRunId){previousAmmo.Clear();emptyAlertUntil.Clear();ammoRunId=game.RunId;}
            int before;float until;
            if(previousAmmo.TryGetValue(key,out before)&&before>0&&amount==0)emptyAlertUntil[key]=Time.unscaledTime+3;
            previousAmmo[key]=amount;
            bool empty=amount<=0,low=!empty&&capacity>0&&(float)amount/capacity<=.2f;
            Text(v,key+" Status",empty?"! EMPTY":low?"! LOW AMMO":"");
            var status=v.Get<Text>(key+" Status");status.color=empty?layout.criticalHull:layout.hurtHull;
            v.Get<Image>(key+" Bar").color=empty?layout.criticalHull:low?layout.hurtHull:layout.healthyHull;
            bool alert=empty&&emptyAlertUntil.TryGetValue(key,out until)&&Time.unscaledTime<until&&game.Stage.Mode==StageMode.Combat&&!game.Paused;
            Active(v,key+" Empty Alert",alert);
        }

        string RunResources()=>"HULL "+game.Hull.ToString("0.#")+" / "+game.MaxHull.ToString("0.#")+
            "     AMMO  MG "+game.MachineGun.Ammo+" / "+game.MachineGun.ReserveCapacity+
            " · CANNON "+game.MainGun.Ammo+" / "+game.MainGun.ReserveCapacity+
            " · MORTAR "+game.Mortar.Ammo+" / "+game.Mortar.Stats.reserveCapacity;

        void DrawRouteInspection()
        {
            var v=View(UIScreen.Route);Text(v,"Route Resources",RunResources());
            bool valid=inspectedNode>=0&&game.Stage.CanSelect(inspectedNode);
            v.Get<Button>("Enter Route Node").interactable=valid;
            var node=valid?game.Stage.Route.Get(inspectedNode):null;
            foreach(Transform child in v.Get<ScrollRect>("Route Scroll").content)
            {
                var outline=child.GetComponent<Outline>();if(outline!=null)outline.enabled=valid&&child.name=="Node "+inspectedNode;
            }
            Text(v,"Route Inspection",valid?"ROUND "+(node.round+1)+" · "+StageNodeIcons.Name(node.type).ToUpperInvariant()+"\n"+EncounterDescription(node.type):"Select a connected node to inspect it, then press Enter.");
            Click(v,"Enter Route Node",()=>{if(inspectedNode>=0&&game.Stage.CanSelect(inspectedNode)){int id=inspectedNode;inspectedNode=-1;game.Stage.SelectNode(id);}});
        }

        public static string EncounterDescription(StageNodeType type)
        {
            switch(type)
            {
                case StageNodeType.HardBattle:return "3 waves · Choose one reward after all three waves.";
                case StageNodeType.Battle:return "1 wave · Choose one reward after victory.";
                case StageNodeType.Supply:return "Ammo supply · One free card and an optional paid card.";
                case StageNodeType.Repair:return "Hull repair · One free card and an optional paid card.";
                case StageNodeType.Boss:return "Final battle · Defeat the boss to clear the stage.";
                default:return "Unknown encounter · Revealed after entering.";
            }
        }

        string DefeatSummary()=>"ROUND "+game.Stage.CurrentRoundNumber+" / "+game.Stage.Definition.rounds+
            "     ENEMIES DEFEATED "+game.EnemiesDefeated+"     SCORE "+game.Score+
            "\n\n"+RunResources()+"\n\nLOADOUT\nMG: "+game.Equipment.MachineGun.displayName+
            " · CANNON: "+game.Equipment.Cannon.displayName+" · MORTAR: "+game.Equipment.Mortar.displayName;
    }
}
