using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatTanks.Editor
{
    [InitializeOnLoad]
    public static class StudioUIAssets
    {
        const string Root = "Assets/CatTanks/Layer Lab/GUI Pro-SuperCasual/ResourcesData/Sprites/Components/";
        const string Output = "Assets/CatTanks/Resources/StudioUITheme.asset";
        static StudioUIAssets() { EditorApplication.delayCall += Ensure; }
        static void Ensure() { if (!AssetDatabase.LoadAssetAtPath<StudioUITheme>(Output)) Rebuild(); }
        [MenuItem("CatTanks/UI/Rebuild LayerLab Theme")]
        public static void Rebuild()
        {
            var theme = AssetDatabase.LoadAssetAtPath<StudioUITheme>(Output);
            if (!theme) { theme = ScriptableObject.CreateInstance<StudioUITheme>(); AssetDatabase.CreateAsset(theme, Output); }
            theme.assets.Clear();
            Add(theme, "panel", "Frame/BasicFrame_Round20.png");
            Add(theme, "card", "Frame/BorderFrame_Round20_Single_Dark.png");
            Add(theme, "button", "Button/Button_Round03_Dark.png");
            Add(theme, "primary", "Button/Button_Round01_Yellow.png");
            Add(theme, "selected", "Button/Button_Round02_Blue.png");
            Add(theme, "input", "UI_Etc/InputField01_Bg_White1.png");
            Add(theme, "bar", "Slider/Slider_Basic01_Bg_Single.png");
            Add(theme, "fill", "Slider/Slider_Basic01_Fill_White.png");
            string[] icons = { "Map", "Battle", "Bag_Brown", "Chest_Gold", "Calendar_Check", "Book_1_Purple", "Money_Coin", "Gem_Diamond_Purple", "Ticket_Gold", "Gear", "Badge_Crown", "Skull", "Chest_Premium", "Damage", "Gear_Sword", "Gem_Star_Blue", "Gem_Circle" };
            foreach (string icon in icons) Add(theme, icon, "Icon_ItemIcons/256/ItemIcon_" + icon + ".Png");
            foreach (string icon in new[] { "Lock_1", "Check_1", "Arrow_Right_1", "Pause_1", "Play_1", "Setting" })
                Add(theme, icon.Split('_')[0], "Icon_PictoIcons/128/PictoIcon_" + icon + ".Png");
            EditorUtility.SetDirty(theme); AssetDatabase.SaveAssets();
            Debug.Log("CatTanks LayerLab theme ready: " + theme.assets.Count + " assets.");
        }
        static void Add(StudioUITheme theme, string key, string relative)
        {
            string path = Root + relative;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!sprite && File.Exists(path))
            {
                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path)) if (sub is Sprite found) { sprite = found; break; }
            }
            if (!sprite) { Debug.LogWarning("CatTanks UI asset missing: " + path); return; }
            theme.assets.Add(new StudioUITheme.Asset { key = key, sprite = sprite });
        }
    }
}
