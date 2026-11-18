using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MegaTanks.Editor
{
    public static class ProgressionSetup
    {
        const string Folder = "Assets/MegaTanks/Prefabs/UI/";
        static void Bind(UIView view, string key, RectTransform rect)
        {
            var so = new SerializedObject(view); var bindings = so.FindProperty("bindings");
            for (int i=0;i<bindings.arraySize;i++) if (bindings.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==key) return;
            bindings.InsertArrayElementAtIndex(bindings.arraySize);
            var b=bindings.GetArrayElementAtIndex(bindings.arraySize-1);
            b.FindPropertyRelative("key").stringValue=key; b.FindPropertyRelative("target").objectReferenceValue=rect;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        static void Place(RectTransform rect,float x,float y,float w,float h)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);
        }
        static void Edit(string name, Action<UIView> edit)
        {
            string path=Folder+name+".prefab"; var root=PrefabUtility.LoadPrefabContents(path);
            try { edit(root.GetComponent<UIView>()); PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static Text Label(UIView view,string name,float x,float y,float w,float h,int size)
        {
            var t=view.transform.Find(name);if(t!=null)return t.GetComponent<Text>();
            var label=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<Text>(); label.transform.SetParent(view.transform,false);
            label.font=view.GetComponentsInChildren<Text>(true).First(l=>l!=label).font;
            label.fontSize=size;label.color=new Color(.91f,.96f,1);label.fontStyle=FontStyle.Bold;label.alignment=TextAnchor.MiddleLeft;
            label.raycastTarget=false;label.resizeTextForBestFit=true;label.resizeTextMinSize=14;label.resizeTextMaxSize=size;
            Place(label.rectTransform,x,y,w,h);Bind(view,name,label.rectTransform);return label;
        }
        static void Stars(UIView view,string key,RectTransform icon,float height)
        {
            string name=icon.name+" Stars";
            var existing=icon.parent.Find(name);
            var strip=existing!=null?existing.GetComponent<UIStarStrip>():new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(UIStarStrip)).GetComponent<UIStarStrip>();
            strip.transform.SetParent(icon.parent,false);
            var rect=strip.rectTransform;rect.anchorMin=icon.anchorMin;rect.anchorMax=icon.anchorMax;rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=icon.anchoredPosition+icon.rect.center+Vector2.down*(icon.rect.height*.5f+height*.5f+1);
            rect.sizeDelta=new Vector2(Mathf.Max(60,icon.rect.width),height);strip.raycastTarget=false;Bind(view,key,rect);
        }
        static void Button(UIView view,string key,float x,float y,float w,Button template)
        {
            if(view.transform.Find(key)!=null)return;
            var button=Object.Instantiate(template,view.transform,false);button.name=key;button.onClick=new Button.ButtonClickedEvent();button.gameObject.SetActive(true);
            Place((RectTransform)button.transform,x,y,w,56);
            var label=button.GetComponentInChildren<Text>(true);label.text=key.ToUpperInvariant();label.alignment=TextAnchor.MiddleCenter;label.fontSize=23;
            label.resizeTextForBestFit=true;label.resizeTextMinSize=17;label.resizeTextMaxSize=23;Place(label.rectTransform,8,2,w-16,52);
            Bind(view,key,(RectTransform)button.transform);Bind(view,key+"/Label",label.rectTransform);
        }
        static void Controls(UIView view,Button template)
        {
            if(view.transform.Find("Progression Panel")==null)
            {
                var panel=new GameObject("Progression Panel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
                panel.transform.SetParent(view.transform,false);panel.color=new Color(.035f,.055f,.085f,.96f);panel.raycastTarget=false;
                Place(panel.rectTransform,48,638,1824,82);Bind(view,"Progression Panel",panel.rectTransform);
            }
            Label(view,"Progression Summary",68,644,900,66,23).text="Select an item to upgrade.";
            Button(view,"Level Up",990,651,420,template);Button(view,"Star Up",1428,651,420,template);
            foreach(string key in new[]{"Level Up/Label","Star Up/Label"}) view.Get<Text>(key).color=new Color(.035f,.055f,.08f);
        }
        [MenuItem("MegaTanks/Configure Progression")]
        public static void Install()
        {
            foreach(string guid in AssetDatabase.FindAssets("t:WeaponDefinition"))
            {
                var definition=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if(string.IsNullOrWhiteSpace(definition.id)) definition.id=guid;
                EditorUtility.SetDirty(definition);
            }
            foreach(string guid in AssetDatabase.FindAssets("t:CrewDefinition"))
            {
                var definition=AssetDatabase.LoadAssetAtPath<CrewDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if(string.IsNullOrWhiteSpace(definition.id)) definition.id=guid;
                EditorUtility.SetDirty(definition);
            }
            foreach(string guid in AssetDatabase.FindAssets("t:ArmorDefinition"))
            {
                var definition=AssetDatabase.LoadAssetAtPath<ArmorDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if(string.IsNullOrWhiteSpace(definition.id)) definition.id=guid;
                EditorUtility.SetDirty(definition);
            }
            Edit("InventoryItem",v=>{
                var icon=v.Get<RectTransform>("Icon");icon.sizeDelta=new Vector2(116,82);icon.anchoredPosition=new Vector2(66,-56);
                Stars(v,"Stars",icon,17);Label(v,"Progression Level",8,3,85,24,17).text="Lv. 1";
            });
            Edit("Equipment",v=>{
                Controls(v,v.Get<Button>("Equip Item"));
                foreach(string slot in new[]{"Mortar","Accessory 2","Accessory 3","MG","Cannon","Armor"})
                {
                    var icon=v.Get<RectTransform>("Slot "+slot+"/Equipped Item Icon");
                    icon.sizeDelta=new Vector2(icon.sizeDelta.x,Mathf.Min(80,icon.sizeDelta.y));
                    Stars(v,"Slot "+slot+"/Stars",icon,20);
                }
                Stars(v,"Detail Stars",v.Get<RectTransform>("Detail Item Icon"),20);
            });
            Edit("Crew",v=>{
                Controls(v,v.Get<Button>("Assign Crew"));
                for(int i=0;i<5;i++)
                {
                    var icon=v.Get<RectTransform>("Slot "+i+"/Portrait");Place(icon,18,7,90,90);Stars(v,"Slot "+i+"/Stars",icon,19);
                    var marker=v.Get<RectTransform>("Tank Preview/Tank Slot "+i+"/Portrait");Stars(v,"Tank Preview/Tank Slot "+i+"/Stars",marker,15);
                }
                var detail=v.Get<RectTransform>("Detail Portrait");Place(detail,1005,755,115,110);Stars(v,"Detail Stars",detail,18);
            });
            AssetDatabase.SaveAssets();
            Debug.Log("PROGRESSION_SETUP_PASS");
        }
    }
}
