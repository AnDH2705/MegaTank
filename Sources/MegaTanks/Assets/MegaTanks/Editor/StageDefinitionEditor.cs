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
            if (stage.spawnableEnemies == null || !stage.spawnableEnemies.Any(profile => profile != null))
                EditorGUILayout.HelpBox("Assign enemy prefab profiles to Spawnable Enemies to populate this stage's regular waves.", MessageType.Warning);
            EditorGUILayout.HelpBox("Rounds counts battles through the final boss. Hard battles contain three waves. The vendor tent appears every five battles and before the boss.",MessageType.Info);
            EditorGUILayout.HelpBox("Regular enemy difficulty = Stage Difficulty + round (1-based). New stages default to (Stage Number − 1) × 10. Boss power uses only its template and the levels/stars configured on this stage.",MessageType.Info);
            var boss = stage.boss;
            if (boss?.template == null)
                EditorGUILayout.HelpBox("Select a Boss Template for this stage's final battle.", MessageType.Warning);
            else
            {
                try
                {
                    boss.Validate(); var stats = boss.CalculateTankStats();
                    EditorGUILayout.HelpBox($"Boss totals: HP {stats.maxHull:0.##}, armor {stats.armor:0.##}, speed {stats.advanceSpeed:0.##}\nCrew bonuses apply only to their matching weapon or hull.\nMG / Cannon / Mortar damage: {boss.WeaponDamage(boss.template.equipment.machineGun.stats.damage, boss.machineGun):0.##} / {boss.WeaponDamage(boss.template.equipment.cannon.stats.damage, boss.cannon):0.##} / {boss.WeaponDamage(boss.template.equipment.mortar.stats.damage, boss.mortar):0.##}\nCrew progression matches template stations by name. These totals do not scale with stage difficulty or round.", MessageType.Info);
                }
                catch (System.InvalidOperationException e) { EditorGUILayout.HelpBox(e.Message, MessageType.Warning); }
                if (GUILayout.Button("Select Boss Template")) Selection.activeObject = boss.template;
            }
            var repeat=stage.RepeatCompletionRewards;var first=stage.FirstCompletionRewards;
            EditorGUILayout.HelpBox("Every completion: "+repeat.parts+" Parts, "+repeat.petFood+" Pet Food. First completion adds: "+first.parts+" Parts, "+first.petFood+" Pet Food, "+first.gems+" Gems. Keep Stage Number unique and stable to preserve first-clear history.",MessageType.Info);
            EditorGUILayout.HelpBox($"First completion also awards {stage.FirstCompletionAccountExperience:N0} account XP.", MessageType.Info);
            if(GUILayout.Button("Use Default Stage Difficulty"))
            {
                Undo.RecordObject(stage,"Default stage difficulty");stage.stageDifficulty=StageDefinition.DefaultDifficulty(stage.stageNumber);EditorUtility.SetDirty(stage);
            }
        }
    }
}
