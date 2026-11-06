using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace MegaTanks.Editor
{
    public static class CanvasUISetup
    {
        public const string CatalogPath="Assets/MegaTanks/Data/UI/Frontend.asset";
        [MenuItem("MegaTanks/Configure Canvas UI")]
        public static void Configure()
        {
            System.IO.Directory.CreateDirectory("Assets/MegaTanks/Data/UI");AssetDatabase.Refresh();EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var game=Object.FindObjectOfType<BattleGame>();var settings=new SerializedObject(game);var catalog=AssetDatabase.LoadAssetAtPath<FrontendCatalog>(CatalogPath);bool created=catalog==null;
            if(Object.FindObjectOfType<CanvasGameUI>()==null)
            {
                var root=new GameObject("MegaTanks UI Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGameUI));
                root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            }
            if(Object.FindObjectOfType<EventSystem>()==null)new GameObject("UI EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            if(created){catalog=ScriptableObject.CreateInstance<FrontendCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
            if(created||catalog.stages==null||catalog.stages.Length==0)catalog.stages=new[]{(StageDefinition)settings.FindProperty("stageDefinition").objectReferenceValue};
            if(created||catalog.inventory==null||catalog.inventory.Length==0)catalog.inventory=AssetDatabase.FindAssets("t:WeaponDefinition",new[]{"Assets/MegaTanks/Data/Weapons"}).Select(id=>AssetDatabase.LoadAssetAtPath<EquipmentDefinition>(AssetDatabase.GUIDToAssetPath(id))).Where(item=>item!=null).ToArray();
            var loadout=(WeaponLoadout)settings.FindProperty("startingLoadout").objectReferenceValue;
            if(!catalog.inventory.Contains(loadout.mortar))catalog.inventory=catalog.inventory.Concat(new EquipmentDefinition[]{loadout.mortar}).ToArray();
            foreach(var item in catalog.inventory)
            {
                if(item.icon!=null)continue;
                if(item.Type==EquipmentType.Mortar){item.icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MegaTanks/Art/Mortar/parts/09_mortar_barrel.png");EditorUtility.SetDirty(item);continue;}
                if(item.Type!=EquipmentType.MachineGun&&item.Type!=EquipmentType.Cannon)continue;
                string name=item.Type==EquipmentType.MachineGun?"roof_gun_barrel":"main_gun_barrel";
                var id=AssetDatabase.FindAssets(name+" t:Sprite",new[]{"Assets/MegaTanks/Art/Tank"}).FirstOrDefault();
                if(id!=null){item.icon=AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(id));EditorUtility.SetDirty(item);}
            }
            EditorUtility.SetDirty(catalog);settings.FindProperty("frontendCatalog").objectReferenceValue=catalog;settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_CANVAS_CONFIGURED: Home, six equipment slots, settings, battle HUD, rewards and stage route use uGUI Canvas.");
        }
    }
}
