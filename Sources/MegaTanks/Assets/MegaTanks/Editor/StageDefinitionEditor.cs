using System.Linq;
using UnityEditor;
using UnityEngine;
namespace MegaTanks.Editor
{
    [CustomEditor(typeof(StageDefinition))]
    public sealed class StageDefinitionEditor : UnityEditor.Editor
    {
        [MenuItem("Assets/Create/MegaTanks/Stage Definition")]
        public static void CreateStage()
        {
            int next=AssetDatabase.FindAssets("t:StageDefinition").Select(id=>AssetDatabase.LoadAssetAtPath<StageDefinition>(AssetDatabase.GUIDToAssetPath(id)).stageNumber).DefaultIfEmpty(0).Max()+1;
            var stage=ScriptableObject.CreateInstance<StageDefinition>();stage.InitializeStage(next);
            ProjectWindowUtil.CreateAsset(stage,"Stage"+next+".asset");
        }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();var stage=(StageDefinition)target;
            EditorGUILayout.HelpBox("Battle difficulty = Stage Difficulty + round (1-based). New stages default to (Stage Number − 1) × 10. Stage Difficulty can be edited independently.",MessageType.Info);
            if(GUILayout.Button("Use Default Stage Difficulty"))
            {
                Undo.RecordObject(stage,"Default stage difficulty");stage.stageDifficulty=StageDefinition.DefaultDifficulty(stage.stageNumber);EditorUtility.SetDirty(stage);
            }
        }
    }
}
