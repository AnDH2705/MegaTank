using UnityEditor;
using UnityEngine;
namespace MegaTanks.Editor
{
    [CustomEditor(typeof(EnemyStatsProfile))]
    public sealed class EnemyStatsProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();var stats=serializedObject.FindProperty("stats");
            EditorGUILayout.HelpBox("Battle stat = floor(Base + Difficulty × Grow), except movement Speed, which keeps fractions. Difficulty = Stage Difficulty + round number. Grow starts at 0. Percentages may exceed 100. All three waves of a hard battle use the same snapshot.",MessageType.Info);
            foreach(string key in new[]{"id","kind"})EditorGUILayout.PropertyField(stats.FindPropertyRelative(key));
            EditorGUILayout.LabelField("Unit stats",EditorStyles.boldLabel);Header();
            foreach(string key in new[]{"baseHealth","armor","evasion","baseSpeed","criticalChanceReduction","criticalDamageReduction","advanceInsideRange","score","baseScraps"})Pair(stats,key);
            var weapon=stats.FindPropertyRelative("weapon");EditorGUILayout.Space();EditorGUILayout.LabelField("Weapon stats",EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(weapon.FindPropertyRelative("type"));EditorGUILayout.PropertyField(weapon.FindPropertyRelative("damageType"));Header();
            foreach(string key in new[]{"damage","damageBonus","accuracy","armorPenetration","criticalChance","criticalDamage","reserveCapacity","range","salvoSize","shotDelay","reloadTime","projectileSpeed","gravity"})Pair(weapon,key);
            EditorGUILayout.Space();EditorGUILayout.LabelField("Artwork (does not grow)",EditorStyles.boldLabel);
            foreach(string key in new[]{"color","artworkPrefab","artworkWidth","scale"})EditorGUILayout.PropertyField(stats.FindPropertyRelative(key));
            serializedObject.ApplyModifiedProperties();
        }
        static void Header(){using(new EditorGUILayout.HorizontalScope()){GUILayout.Label("Property / Base",EditorStyles.miniBoldLabel);GUILayout.Label("Grow per difficulty",EditorStyles.miniBoldLabel,GUILayout.Width(125));}}
        static void Pair(SerializedProperty parent,string key)
        {
            using(new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(parent.FindPropertyRelative(key),true);
                EditorGUILayout.PropertyField(parent.FindPropertyRelative(key+"Grow"),GUIContent.none,true,GUILayout.Width(125));
            }
        }
    }
}
