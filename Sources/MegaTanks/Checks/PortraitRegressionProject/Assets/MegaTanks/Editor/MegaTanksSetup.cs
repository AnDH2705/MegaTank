using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class MegaTanksSetup
    {
        public const string ScenePath = "Assets/MegaTanks/Scenes/MainBattle.unity";

        [MenuItem("MegaTanks/Create Main Battle Scene")]
        public static void CreateProject()
        {
            System.IO.Directory.CreateDirectory("Assets/MegaTanks/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var game = new GameObject("MegaTanks 2D Battle").AddComponent<BattleGame>();
            var serializedGame = new SerializedObject(game);
            serializedGame.FindProperty("tankArtworkPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TankArtwork>(TankArtImporter.PrefabPath);
            serializedGame.FindProperty("tankDefinition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TankDefinition>(StatsProfileImporter.TankPath);
            serializedGame.FindProperty("startingLoadout").objectReferenceValue = AssetDatabase.LoadAssetAtPath<WeaponLoadout>(StatsProfileImporter.LoadoutPath);
            serializedGame.FindProperty("enemyDataManager").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyDataManager>(EnemyDataSetup.AssetPath);
            serializedGame.FindProperty("cardCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CardCatalog>(CardDataImporter.CatalogPath);
            serializedGame.FindProperty("stageDefinition").objectReferenceValue = AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/MegaTanks/Data/Stages/Stage1.asset");
            serializedGame.FindProperty("backgroundCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BattleBackgroundCatalog>(BattleBackgroundImporter.CatalogPath);
            serializedGame.FindProperty("frontendCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<FrontendCatalog>(CanvasUISetup.CatalogPath);
            serializedGame.ApplyModifiedPropertiesWithoutUndo();
            var uiPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MegaTanks/Prefabs/UI/MegaTanksCanvas.prefab");
            var eventPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MegaTanks/Prefabs/UI/UIEventSystem.prefab");
            if(uiPrefab!=null)PrefabUtility.InstantiatePrefab(uiPrefab);
            if(eventPrefab!=null)PrefabUtility.InstantiatePrefab(eventPrefab);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            PlayerSettings.companyName = "SeagullGame";
            PlayerSettings.productName = "MegaTanks";
            PlayerSettings.bundleVersion = "0.1.0";
            ApplyLandscapeSettings();
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.seagullgame.megatanks");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.seagullgame.megatanks");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            QualitySettings.vSyncCount = 0;
            AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_SETUP_OK: 2D scene and mobile settings saved.");
        }

        [MenuItem("MegaTanks/Apply Landscape Settings")]
        public static void ApplyLandscapeSettings()
        {
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_LANDSCAPE_OK: 1920x1080 reference resolution, landscape orientation.");
        }

        internal static void SetLandscapeGameView(EditorWindow gameView)
        {
            // Render at the phone resolution while keeping the window small enough for a desktop.
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { (int)GameViewSizeGroupType.Standalone });
            var groupType = group.GetType();
            int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            int index = -1;
            for (int i = 0; i < count; i++)
            {
                var size = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if ((int)size.GetType().GetProperty("width").GetValue(size) == 1920 && (int)size.GetType().GetProperty("height").GetValue(size) == 1080)
                { index = i; break; }
            }
            if (index < 0)
            {
                var sizeType = assembly.GetType("UnityEditor.GameViewSize");
                var kind = assembly.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType, new object[] { Enum.ToObject(kind, 1), 1920, 1080, "MegaTanks Landscape" });
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                index = count;
            }
            gameView.GetType().GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).SetValue(gameView, index);
        }

        public static void SmokeTest() { CameraPanAudit.Run(); }
        public static void CapturePreview() { CameraPanAudit.Capture(); }
        public static void CaptureBallisticsPreview() { CameraPanAudit.Capture(); }
    }
}
