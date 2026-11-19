using UnityEditor;
using UnityEngine;
namespace MegaTanks.Editor
{
    [CustomEditor(typeof(CrewDefinition))]
    public sealed class CrewDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            foreach(string field in new[]{"id","displayName","role","description","portrait"})EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
            var role=(CrewRole)serializedObject.FindProperty("role").enumValueIndex;
            EditorGUILayout.LabelField("Associated slot",CrewDefinition.AssociatedSlot(role));
            EditorGUILayout.HelpBox("Each stat = Value + Gain × (Level − 1). Weapon bonuses use percentage points; HP and speed are added directly. Stars retain +10% per star after level gains. Integrated base reload time uses only its level gain.",MessageType.Info);
            var stats=serializedObject.FindProperty("stats");
            if(role==CrewRole.Driver||role==CrewRole.Commando)
                foreach(string field in new[]{"hp","evasion","speed"})EditorGUILayout.PropertyField(stats.FindPropertyRelative(field),true);
            if(role!=CrewRole.Driver)
                foreach(string field in new[]{"accuracy","damageBonus","reloadSpeed","criticalChance","criticalDamage"})EditorGUILayout.PropertyField(stats.FindPropertyRelative(field),true);
            if(role==CrewRole.Commando)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("commandoPrefab"));
                EditorGUILayout.LabelField("Independent hull and integrated weapon",EditorStyles.boldLabel);
                foreach(string field in new[]{"damage","armor","armorPenetration","criticalChanceReduction","criticalDamageReduction","weaponRange","baseReloadTime","projectileSpeed","activationRange"})EditorGUILayout.PropertyField(stats.FindPropertyRelative(field),true);
                EditorGUILayout.HelpBox("Activation Range is measured from the tank. Zero HP or zero activation range disables deployment. The commando keeps its HP when it returns to the tank; defeat lasts until the next battle. Weapon Range 0 selects melee: it walks beside its target and strikes during the attack animation. Positive Range selects ranged attacks and requires positive base reload time and projectile speed. Reload Speed and Base Reload Time also control melee cooldown.",MessageType.Info);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
