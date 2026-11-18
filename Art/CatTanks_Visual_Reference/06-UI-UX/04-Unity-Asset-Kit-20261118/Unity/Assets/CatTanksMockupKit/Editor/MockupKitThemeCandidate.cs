using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CatTanks.MockupKit.Editor
{
    public static class MockupKitThemeCandidate
    {
        [MenuItem("CatTanks/Mockup Kit/6 - Create Existing Game Theme Candidate")]
        public static void Create()
        {
            var original = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/CatTanks/Resources/StudioUITheme.asset");
            var library = AssetDatabase.LoadAssetAtPath<MockupKitLibrary>(MockupKitImporter.Root + "/Generated/MockupKitLibrary.asset");
            if (!original || !library) throw new InvalidOperationException("This optional step needs the existing CatTanks theme and imported kit library.");
            var candidate = UnityEngine.Object.Instantiate(original);
            candidate.name = "MockupKitTheme";
            var serialized = new SerializedObject(candidate);
            var assets = serialized.FindProperty("assets");
            if (assets == null || !assets.isArray) throw new InvalidOperationException("The existing theme has a different schema.");
            var mapping = new Dictionary<string, string>
            {
                {"panel", "panel_main"}, {"card", "panel_card"}, {"button", "button_blue"},
                {"primary", "button_gold"}, {"selected", "button_cyan"}, {"input", "input_field"},
                {"bar", "bar_track"}, {"fill", "bar_fill_cyan"}, {"Map", "icon_map"},
                {"Battle", "icon_battle"}, {"Bag_Brown", "icon_town"}, {"Chest_Gold", "icon_chest"},
                {"Calendar_Check", "icon_quests"}, {"Book_1_Purple", "icon_book"}, {"Money_Coin", "icon_coin"},
                {"Gem_Diamond_Purple", "icon_gem"}, {"Ticket_Gold", "icon_ticket"}, {"Gear", "icon_settings"},
                {"Badge_Crown", "icon_crown"}, {"Skull", "icon_skull"}, {"Chest_Premium", "icon_chest"},
                {"Damage", "icon_attack"}, {"Gear_Sword", "skill_repair"}, {"Gem_Star_Blue", "icon_skill_core"},
                {"Gem_Circle", "icon_dust"}, {"Lock", "icon_lock"}, {"Check", "icon_check"},
                {"Arrow", "icon_arrow_right"}, {"Pause", "icon_pause"}, {"Play", "icon_play"}, {"Setting", "icon_settings"}
            };
            // Keep any legacy keys outside this mapping so old screens retain their references.
            foreach (var pair in mapping)
            {
                var sprite = library.Get(pair.Value);
                if (!sprite) throw new InvalidOperationException("Missing sprite for theme candidate: " + pair.Value);
                SerializedProperty target = null;
                for (int i = 0; i < assets.arraySize; i++)
                    if (assets.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == pair.Key)
                    { target = assets.GetArrayElementAtIndex(i); break; }
                if (target == null) { assets.InsertArrayElementAtIndex(assets.arraySize); target = assets.GetArrayElementAtIndex(assets.arraySize - 1); }
                target.FindPropertyRelative("key").stringValue = pair.Key;
                target.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            string path = AssetDatabase.GenerateUniqueAssetPath(MockupKitImporter.Root + "/Generated/MockupKitTheme.asset");
            AssetDatabase.CreateAsset(candidate, path); AssetDatabase.SaveAssets();
            Debug.Log("Theme candidate saved: " + path + ". Existing game theme was not changed.");
        }
    }
}
