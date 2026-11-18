using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class EnemyDataSetup
    {
        public const string AssetPath = "Assets/MegaTanks/Data/EnemyDataManager.asset";
        [MenuItem("MegaTanks/Set Up Enemy Data")]
        public static void Import()
        {
            System.IO.Directory.CreateDirectory("Assets/MegaTanks/Data");
            AssetDatabase.Refresh();
            var database = AssetDatabase.LoadAssetAtPath<EnemyDataManager>(AssetPath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<EnemyDataManager>();
                AssetDatabase.CreateAsset(database, AssetPath);
            }
            var scene = EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var game = Object.FindObjectOfType<BattleGame>();
            var serialized = new SerializedObject(game);
            serialized.FindProperty("enemyDataManager").objectReferenceValue = database;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_ENEMY_DATA_OK: FlyingEnemy1 and GroundEnemy1 data asset linked to scene.");
        }
    }
}
