using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MegaTanks.Editor
{
    [CustomEditor(typeof(UICanvasLayout))]
    public sealed class UICanvasLayoutEditor : UnityEditor.Editor
    {
        UIScreen preview;
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Choose a screen and click Show in Editor. Edit its RectTransforms, text and images in the Hierarchy, or open its prefab. Preview selection does not change the game's starting screen.",MessageType.Info);
            preview=(UIScreen)EditorGUILayout.EnumPopup("Preview screen",preview);
            using(new EditorGUI.DisabledScope(Application.isPlaying))
                if(GUILayout.Button("Show in Editor"))
                {
                    var layout=(UICanvasLayout)target;Undo.RegisterFullObjectHierarchyUndo(layout.gameObject,"Preview UI screen");layout.Preview(preview);
                    EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);
                    Selection.activeGameObject=layout.View(preview).gameObject;SceneView.RepaintAll();
                }
            DrawDefaultInspector();
        }
    }
}
