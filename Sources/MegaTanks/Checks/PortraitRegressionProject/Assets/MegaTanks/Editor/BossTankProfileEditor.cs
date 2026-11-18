using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    [CustomEditor(typeof(BossTankProfile))]
    public sealed class BossTankProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var profile = (BossTankProfile)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Reusable boss template: choose its hull, equipment and crew here. Set levels and stars on each Stage Definition's Boss encounter.", MessageType.Info);
            if (profile.tank == null || profile.equipment == null || profile.equipment.machineGun == null || profile.equipment.cannon == null || profile.equipment.mortar == null) return;
            if (GUILayout.Button("Select Boss Equipment")) Selection.activeObject = profile.equipment;
            if (GUILayout.Button("Select Boss Hull")) Selection.activeObject = profile.tank;
        }
    }
}
