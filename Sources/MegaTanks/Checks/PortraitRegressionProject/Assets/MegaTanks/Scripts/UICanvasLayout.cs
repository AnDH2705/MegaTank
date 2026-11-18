using System;
using UnityEngine;
namespace MegaTanks
{
    public enum UIScreen { Home, BattleHUD, Equipment, Settings, Crew, Tech, LeaveRun, Pause, Defeat, Victory, Rewards, Shop, Service, Reveal, WaveBreak, Route }
    public sealed class UICanvasLayout : MonoBehaviour
    {
        [Serializable] public struct ScreenEntry { public UIScreen screen; public UIView view; }
        public RectTransform safeArea, damageLayer;
        public ScreenEntry[] screens;
        [Header("Repeated element prefabs")]
        public UIView inventoryItem, routeNode, routeConnection, routeTank, damageNumber;
        [Header("Procedural route layout (canvas units)")]
        [Min(1)] public float roundSpacing=230;
        [Min(0)] public float routeHorizontalPadding=120, routeVerticalPadding=140;
        [Tooltip("Offset of the tank marker's top-left corner from the current node.")]
        public Vector2 markerOffset=new Vector2(-36,-136);
        public Color routeCurrentText=new Color(1,.72f,.3f), routeAvailableText=Color.white, routeFutureText=new Color(.59f,.68f,.74f);
        public Color routeAvailableConnection=new Color(.32f,.88f,.95f), routeCompletedConnection=new Color(1,.72f,.3f), routeFutureConnection=new Color(.32f,.43f,.48f);
        [Header("World UI")]
        public Vector2 hullOffset=new Vector2(0,-.65f);
        public Color healthyHull=new Color(.32f,.88f,.95f), hurtHull=new Color(1,.72f,.3f), criticalHull=Color.red;
        public Color enemyDamage=new Color(1,.72f,.3f), playerDamage=new Color(1,.35f,.3f);
        public UIView View(UIScreen screen)
        {
            foreach(var entry in screens)if(entry.screen==screen)return entry.view;
            throw new InvalidOperationException("No prefab screen assigned for "+screen);
        }
        public void HideScreens(){foreach(var entry in screens)entry.view.gameObject.SetActive(false);}
        public void Preview(UIScreen screen)
        {
            HideScreens();if(screen==UIScreen.Pause||screen==UIScreen.LeaveRun||screen==UIScreen.Settings)View(UIScreen.BattleHUD).gameObject.SetActive(true);
            View(screen).gameObject.SetActive(true);damageLayer.gameObject.SetActive(false);
        }
    }
}
