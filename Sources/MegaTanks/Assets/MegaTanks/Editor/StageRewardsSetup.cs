using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks.Editor
{
    public static class StageRewardsSetup
    {
        static void Bind(UIView view,string key,RectTransform rect)
        {
            var so=new SerializedObject(view);var list=so.FindProperty("bindings");
            for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==key)return;
            list.InsertArrayElementAtIndex(list.arraySize);var b=list.GetArrayElementAtIndex(list.arraySize-1);
            b.FindPropertyRelative("key").stringValue=key;b.FindPropertyRelative("target").objectReferenceValue=rect;so.ApplyModifiedPropertiesWithoutUndo();
        }
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var existing=parent.Find(name) as RectTransform;if(existing!=null)return existing;
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);Place(rect,x,y,w,h);return rect;
        }
        static Text Label(UIView view,string key,string text,float x,float y,float w,float h,int size,Color color)
        {
            var r=Rect(view.transform,key,x,y,w,h);var label=r.GetComponent<Text>()??r.gameObject.AddComponent<Text>();
            label.font=view.Get<Text>("Result Detail").font;label.text=text;label.fontSize=size;label.fontStyle=FontStyle.Bold;
            label.alignment=TextAnchor.MiddleLeft;label.color=color;label.raycastTarget=false;label.resizeTextForBestFit=true;label.resizeTextMinSize=18;label.resizeTextMaxSize=size;
            Bind(view,key,r);return label;
        }
        [MenuItem("MegaTanks/Configure Stage Rewards")]
        public static void Install()
        {
            string path="Assets/MegaTanks/Prefabs/UI/Victory.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var v=root.GetComponent<UIView>();bool fresh=root.transform.Find("Repeat Rewards")==null;
                if(fresh)
                {
                    Place(v.Get<RectTransform>("Result Detail"),380,292,1160,70);
                    var detail=v.Get<Text>("Result Detail");detail.fontSize=26;detail.alignment=TextAnchor.UpperLeft;
                    Place(v.Get<RectTransform>("Play Again"),660,750,600,85);Place(v.Get<RectTransform>("Result Home"),660,860,600,70);
                    Place(v.Get<Text>("Play Again/Label").rectTransform,10,2,580,81);Place(v.Get<Text>("Result Home/Label").rectTransform,10,2,580,66);
                }
                var paper=new Color(.9f,.95f,1);var gold=new Color(1,.79f,.35f);
                Label(v,"Repeat Completion Title","EVERY COMPLETION",380,401,345,58,25,paper);
                Label(v,"First Completion Title","FIRST COMPLETION BONUS",380,490,345,68,24,gold);
                Label(v,"Total Rewards Title","TOTAL EARNED",380,595,345,58,28,gold);
                Label(v,"Completion Reward Status","REWARDS ADDED TO YOUR BALANCE",380,680,1160,35,22,paper);
                string[] names={"Parts","PetFood","Gems"};
                for(int i=0;i<3;i++)Label(v,names[i]+" Reward Heading",i==1?"PET FOOD":names[i].ToUpperInvariant(),740+i*265,365,250,35,21,paper);
                string[] groups={"Repeat Rewards","First Completion Rewards","Total Rewards"};
                for(int row=0;row<3;row++)
                {
                    var parent=Rect(v.transform,groups[row],740,row==0?410:row==1?500:605,795,58);
                    for(int i=0;i<3;i++)
                    {
                        string key=groups[row]+"/"+names[i];var cell=Rect(parent,names[i],i*265,0,250,58);
                        var iconRect=Rect(cell,"Icon",0,3,48,48);var icon=iconRect.GetComponent<Image>()??iconRect.gameObject.AddComponent<Image>();
                        icon.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MegaTanks/Art/Currencies/"+names[i]+".png");icon.preserveAspect=true;icon.raycastTarget=false;
                        var amountRect=Rect(cell,"Amount",60,0,180,58);var amount=amountRect.GetComponent<Text>()??amountRect.gameObject.AddComponent<Text>();
                        amount.font=v.Get<Text>("Result Detail").font;amount.fontSize=row==2?30:28;amount.color=row==2?gold:paper;amount.fontStyle=FontStyle.Bold;
                        amount.alignment=TextAnchor.MiddleLeft;amount.raycastTarget=false;amount.resizeTextForBestFit=true;amount.resizeTextMinSize=18;amount.resizeTextMaxSize=amount.fontSize;
                        amount.text=row==0?(i==2?"+0":"+100"):row==1?(i==2?"+100":"+500"):(i==2?"+100":"+600");
                        Bind(v,key+"/Amount",amountRect);Bind(v,key+"/Icon",iconRect);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            // Serialize defaults into existing stage assets while preserving any per-stage edits.
            foreach(string guid in AssetDatabase.FindAssets("t:StageDefinition"))
                EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<StageDefinition>(AssetDatabase.GUIDToAssetPath(guid)));
            AssetDatabase.SaveAssets();Debug.Log("STAGE_REWARDS_SETUP_PASS");
        }
    }
}
