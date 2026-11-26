using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    [CustomEditor(typeof(BossTankProfile))]
    public sealed class BossTankProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "crew", "secondArmor", "secondAccessory");
            var profile = (BossTankProfile)target;
            if(profile.tank!=null&&profile.tank.ArmorSlots>1)EditorGUILayout.PropertyField(serializedObject.FindProperty("secondArmor"));
            if(profile.tank!=null&&profile.tank.AccessorySlots>1)EditorGUILayout.PropertyField(serializedObject.FindProperty("secondAccessory"));
            EditorGUILayout.LabelField("Crew slots",EditorStyles.boldLabel);
            var crew=serializedObject.FindProperty("crew");crew.arraySize=5;
            for(int i=0;i<5;i++)
            {
                var slot=crew.GetArrayElementAtIndex(i);string station=CrewDefinition.RoleLabel((CrewRole)i);
                slot.FindPropertyRelative("station").stringValue=station;
                var member=slot.FindPropertyRelative("member");
                EditorGUILayout.PropertyField(member,new GUIContent(station));
                var definition=member.objectReferenceValue as CrewDefinition;
                if(definition!=null&&definition.role!=(CrewRole)i)EditorGUILayout.HelpBox("Assign a "+station+" crew member to this station.",MessageType.Warning);
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Reusable boss template: choose its hull, equipment and crew here. Set levels and stars on each Stage Definition's Boss encounter.", MessageType.Info);
            if (profile.tank == null || profile.equipment == null || profile.equipment.machineGun == null || profile.equipment.cannon == null || profile.equipment.mortar == null) return;
            EditorGUILayout.HelpBox($"Hull slots: {profile.tank.ArmorSlots} armor, {profile.tank.AccessorySlots} accessories. Drag player inventory definitions into the slots above. Extra slots on smaller hulls are ignored. Accessory stars follow the player's rules (bonuses use level).", MessageType.Info);
            if (GUILayout.Button("Select Boss Hull")) Selection.activeObject = profile.tank;
        }
    }
}
