using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    // Explicit, repeatable migration of authored prefabs. Never rebuild UI at runtime.
    public static class UXImprovementsSetup
    {
        const string Folder="Assets/MegaTanks/Prefabs/UI/";
        static void Edit(string name,Action<UIView> edit)
        {
            string path=Folder+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try{edit(root.GetComponent<UIView>());PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void Rect(RectTransform r,float x,float y,float w,float h)
        {
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
        }
        static void Bind(UIView v,string key,RectTransform target)
        {
            var so=new SerializedObject(v);var list=so.FindProperty("bindings");
            for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==key)return;
            list.InsertArrayElementAtIndex(list.arraySize);var b=list.GetArrayElementAtIndex(list.arraySize-1);
            b.FindPropertyRelative("key").stringValue=key;b.FindPropertyRelative("target").objectReferenceValue=target;so.ApplyModifiedPropertiesWithoutUndo();
        }
        static Text Label(UIView v,string key,string value,float x,float y,float w,float h,int font,Text template)
        {
            var t=v.transform.Find(key);Text text;
            if(t!=null)text=t.GetComponent<Text>();
            else{var go=new GameObject(key,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(v.transform,false);text=go.GetComponent<Text>();}
            text.font=template.font;text.fontSize=font;text.fontStyle=FontStyle.Bold;text.color=Color.white;
            text.alignment=TextAnchor.MiddleLeft;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            text.raycastTarget=false;text.supportRichText=true;text.text=value;Rect(text.rectTransform,x,y,w,h);Bind(v,key,text.rectTransform);return text;
        }
        static void Button(UIView v,string key,string caption,float x,float y,float w,float h,Button template)
        {
            var t=v.transform.Find(key);var b=t!=null?t.GetComponent<Button>():Object.Instantiate(template,v.transform,false);
            b.name=key;b.gameObject.SetActive(true);b.onClick=new Button.ButtonClickedEvent();Rect((RectTransform)b.transform,x,y,w,h);
            var label=b.GetComponentInChildren<Text>(true);label.text=caption;label.fontSize=30;label.alignment=TextAnchor.MiddleCenter;
            Rect(label.rectTransform,10,3,w-20,h-6);Bind(v,key,(RectTransform)b.transform);Bind(v,key+"/Label",label.rectTransform);
        }
        [MenuItem("MegaTanks/Apply UX Improvements")]
        public static void Install()
        {
            Edit("Home",v=>{
                var template=v.Get<Text>("Stage Name Plate/Selected Stage");int i=0;
                foreach(string key in new[]{"Equipment","Crew","Tech"})
                {
                    var r=v.Get<RectTransform>(key);Rect(r,1750,240+i*200,150,125);
                    var label=Label(v,key+" Caption",i==0?"GARAGE":key.ToUpperInvariant(),1715,368+i*200,190,38,28,template);label.alignment=TextAnchor.MiddleCenter;
                    var outline=label.GetComponent<Outline>()??label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(0,0,0,.9f);outline.effectDistance=new Vector2(1,-1);
                    if(i>0){var badge=Label(v,key+" Availability","COMING SOON",1695,403+i*200,210,28,20,template);badge.alignment=TextAnchor.MiddleCenter;badge.color=new Color(1,.8f,.4f);badge.gameObject.SetActive(key!="Crew");}
                    i++;
                }
                var play=v.Get<Text>("Play/Label");play.text="START BATTLE";play.fontSize=36;play.alignment=TextAnchor.MiddleCenter;Rect(play.rectTransform,10,20,310,110);
                v.Get<Image>("Stage Swipe Area").preserveAspect=false;
            });
            Edit("Equipment",v=>{var t=v.Get<Text>("Item Stats");t.fontSize=20;t.lineSpacing=1;t.alignment=TextAnchor.UpperLeft;Rect(t.rectTransform,1180,882,440,168);});
            Edit("BattleHUD",v=>{
                var template=v.Get<Text>("mg Value");int i=0;
                Rect(v.Get<RectTransform>("Encounter"),720,153,770,38);
                foreach(string key in new[]{"mg","cannon","mortar"})
                {
                    var r=v.Get<RectTransform>(key+" Value");
                    var status=Label(v,key+" Status","",r.anchoredPosition.x,112,230,32,23,template);
                    var statusOutline=status.GetComponent<Outline>()??status.gameObject.AddComponent<Outline>();statusOutline.effectColor=Color.black;statusOutline.effectDistance=new Vector2(1,-1);
                    var alert=Label(v,key+" Empty Alert","! "+key.ToUpperInvariant()+" EMPTY — resupply at next stop",550+i*300,200,290,68,24,template);
                    alert.color=Color.white;var outline=alert.GetComponent<Outline>()??alert.gameObject.AddComponent<Outline>();outline.effectColor=Color.black;outline.effectDistance=new Vector2(1,-1);alert.gameObject.SetActive(false);i++;
                }
            });
            var source=PrefabUtility.LoadPrefabContents(Folder+"Defeat.prefab");
            try
            {
                var button=source.GetComponent<UIView>().Get<Button>("Play Again");
                Edit("Route",v=>{
                    Rect(v.Get<RectTransform>("Route Scroll"),46,130,1828,600);
                    var template=v.Get<Text>("Route Stage");
                    Label(v,"Route Resources","Hull and ammunition",65,740,1790,46,26,template);
                    Label(v,"Route Inspection","Select a connected node to inspect it, then press Enter.",65,802,1320,130,28,template);
                    Button(v,"Enter Route Node","ENTER",1480,820,350,100,button);
                });
            }
            finally{PrefabUtility.UnloadPrefabContents(source);}
            Edit("RouteNode",v=>{
                var root=v.Get<RectTransform>("$root");root.sizeDelta=new Vector2(84,84);
                foreach(var image in v.GetComponentsInChildren<Image>(true))
                {
                    if(image.transform==v.transform)continue;
                    // Set absolute geometry so applying the migration twice never shrinks the icons again.
                    var r=image.rectTransform;r.anchoredPosition=new Vector2(42,-42);r.pivot=new Vector2(.5f,.5f);
                    r.sizeDelta=new Vector2(image.name=="Route Node Icon"?64:80,image.name=="Route Node Icon"?64:80);
                }
                Rect(v.Get<RectTransform>("Node Name"),-58,90,200,30);v.Get<Text>("Node Name").fontSize=22;
                var outline=v.GetComponent<Outline>()??v.gameObject.AddComponent<Outline>();outline.effectColor=new Color(1,.75f,.2f);outline.effectDistance=new Vector2(4,-4);outline.enabled=false;
            });
            var canvasRoot=PrefabUtility.LoadPrefabContents(Folder+"MegaTanksCanvas.prefab");
            try{var layout=canvasRoot.GetComponent<UICanvasLayout>();layout.routeVerticalPadding=90;layout.markerOffset=new Vector2(-112,-96);PrefabUtility.SaveAsPrefabAsset(canvasRoot,Folder+"MegaTanksCanvas.prefab");}
            finally{PrefabUtility.UnloadPrefabContents(canvasRoot);}
            Edit("Defeat",v=>{
                Rect(v.Get<RectTransform>("Result Detail"),160,230,1600,380);v.Get<Text>("Result Detail").fontSize=30;
                Rect(v.Get<RectTransform>("Play Again"),390,710,540,95);Rect(v.Get<Text>("Play Again/Label").rectTransform,10,3,520,89);
                Button(v,"Result Garage","GARAGE",990,710,540,95,v.Get<Button>("Play Again"));
            });
            foreach(string card in new[]{"RewardCard","ShopCard","ServiceCard"})Edit(card,v=>{
                var t=v.Get<Text>("Effect");t.fontSize=24;t.alignment=TextAnchor.UpperCenter;t.resizeTextForBestFit=false;
                float bottom=-v.Get<RectTransform>("Action").anchoredPosition.y-12;
                var r=t.rectTransform;r.sizeDelta=new Vector2(r.sizeDelta.x,bottom+r.anchoredPosition.y);
                if(card=="ShopCard")
                {
                    Rect(v.Get<RectTransform>("Artwork"),117,10,130,100);
                    Rect(v.Get<RectTransform>("Title"),20,110,325,42);v.Get<Text>("Title").fontSize=23;
                    Rect(r,20,155,325,103);t.fontSize=18;
                }
            });
            UIVisualPolish.Apply();
            AssetDatabase.SaveAssets();Debug.Log("UX_PREFABS_INSTALLED");
        }
    }
}
