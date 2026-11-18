using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks.Editor
{
    public static class CurrencySetup
    {
        const string Art="Assets/MegaTanks/Art/Currencies/";
        static readonly string[] Names={"Parts","PetFood","Gems"};
        static string Caption(string name)=>name=="PetFood"?"PET FOOD":name.ToUpperInvariant();
        static Sprite Import(string name)
        {
            string path=Art+name+".png";AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=256;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static void Bind(UIView view,string key,RectTransform rect)
        {
            var so=new SerializedObject(view);var list=so.FindProperty("bindings");
            for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==key)return;
            list.InsertArrayElementAtIndex(list.arraySize);var b=list.GetArrayElementAtIndex(list.arraySize-1);
            b.FindPropertyRelative("key").stringValue=key;b.FindPropertyRelative("target").objectReferenceValue=rect;so.ApplyModifiedPropertiesWithoutUndo();
        }
        static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
        }
        static Text Label(Transform parent,string name,Font font,string value,float x,float y,float w,float h,int size,Color color)
        {
            var label=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Text>();label.font=font;label.fontSize=size;
            label.fontStyle=FontStyle.Bold;label.text=value;label.color=color;label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;
            label.resizeTextForBestFit=true;label.resizeTextMinSize=14;label.resizeTextMaxSize=size;return label;
        }
        [MenuItem("MegaTanks/Configure Currencies")]
        public static void Install()
        {
            var sprites=Names.Select(Import).ToArray();
            foreach(string screen in new[]{"Home","Equipment","Crew"})
            {
                string path="Assets/MegaTanks/Prefabs/UI/"+screen+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var view=root.GetComponent<UIView>();var child=root.transform.Find("Currency Balances");
                    var legacy=child!=null?child.GetComponent<Text>():null;
                    if(legacy==null)
                    {
                        var font=root.GetComponentsInChildren<Text>(true).First().font;
                        legacy=Label(root.transform,"Currency Balances",font,"PARTS  0     PET FOOD  0     GEMS  0",680,25,screen=="Crew"?660:1000,58,26,Color.white);
                        Bind(view,"Currency Balances",legacy.rectTransform);
                    }
                    // Preserve the aggregate text binding for existing tools; only the labeled icon counters render.
                    legacy.enabled=false;legacy.raycastTarget=false;
                    float width=(legacy.rectTransform.rect.width-12)/3;
                    for(int i=0;i<Names.Length;i++)
                    {
                        string name=Names[i],key="Currency Balances/"+name;
                        var badge=legacy.transform.Find(name) as RectTransform;
                        if(badge==null)
                        {
                            badge=Rect(legacy.transform,name,i*(width+6),0,width,58);
                            var bg=badge.gameObject.AddComponent<Image>();bg.color=new Color(.025f,.04f,.065f,.9f);bg.raycastTarget=false;
                            var icon=Rect(badge,"Icon",6,6,46,46).gameObject.AddComponent<Image>();icon.preserveAspect=true;icon.raycastTarget=false;
                            Label(badge,"Caption",legacy.font,Caption(name),58,3,width-64,21,15,new Color(.67f,.78f,.84f));
                            Label(badge,"Amount",legacy.font,"0",58,23,width-64,33,26,new Color(.86f,.96f,1));
                        }
                        badge.Find("Icon").GetComponent<Image>().sprite=sprites[i];
                        foreach(string part in new[]{"Icon","Caption","Amount"})Bind(view,key+"/"+part,(RectTransform)badge.Find(part));
                    }
                    if(screen!="Home")
                    {
                        var button=view.Get<Button>("Level Up");var icon=button.transform.Find("Currency Icon")?.GetComponent<Image>();
                        if(icon==null)
                        {
                            icon=Rect(button.transform,"Currency Icon",12,8,40,40).gameObject.AddComponent<Image>();
                            icon.preserveAspect=true;icon.raycastTarget=false;
                            var label=view.Get<Text>("Level Up/Label").rectTransform;
                            label.anchoredPosition=new Vector2(58,-2);label.sizeDelta=new Vector2(((RectTransform)button.transform).rect.width-66,52);
                        }
                        icon.sprite=sprites[screen=="Crew"?1:0];Bind(view,"Level Up/Currency Icon",icon.rectTransform);
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Debug.Log("CURRENCY_ICONS_INSTALLED");
        }
    }
}
