using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace MegaTanks.Editor
{
    public static class CrewSetup
    {
        public const string CatalogPath="Assets/MegaTanks/Data/Crew/CrewCatalog.asset";
        public const string PrefabPath="Assets/MegaTanks/Prefabs/UI/Crew.prefab";
        static readonly Color Paper=new Color(.92f,.95f,.96f),Muted=new Color(.62f,.72f,.78f),Gold=new Color(1,.77f,.34f),Ink=new Color(.035f,.055f,.08f),Panel=new Color(.075f,.115f,.16f);
        static EquipmentUISkin skin;static Font font;
        [MenuItem("MegaTanks/Crew/Configure Scrollable Stats")]
        public static void ConfigureStats()
        {
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var view=root.GetComponent<UIView>();var text=view.Get<Text>("Detail Description");
                if(text.GetComponentInParent<ScrollRect>()==null)
                {
                    var original=text.rectTransform;
                    var viewport=new GameObject("Crew Stats Scroll",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));
                    var rect=(RectTransform)viewport.transform;rect.SetParent(original.parent,false);
                    rect.anchorMin=original.anchorMin;rect.anchorMax=original.anchorMax;rect.pivot=original.pivot;rect.anchoredPosition=original.anchoredPosition;rect.sizeDelta=original.sizeDelta;
                    var image=viewport.GetComponent<Image>();image.color=new Color(.05f,.1f,.15f,.2f);image.raycastTarget=true;
                    original.SetParent(rect,false);original.anchorMin=new Vector2(0,1);original.anchorMax=new Vector2(1,1);original.pivot=new Vector2(.5f,1);original.anchoredPosition=Vector2.zero;original.sizeDelta=new Vector2(0,rect.rect.height);
                    text.fontSize=18;text.resizeTextForBestFit=false;text.alignment=TextAnchor.UpperLeft;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;
                    text.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                    var scroll=viewport.GetComponent<ScrollRect>();scroll.viewport=rect;scroll.content=original;scroll.horizontal=false;scroll.vertical=true;scroll.scrollSensitivity=24;scroll.movementType=ScrollRect.MovementType.Clamped;
                }
                view.Get<Text>("Crew Instructions").text="Select crew, then click its highlighted station. Select a station to filter.";
                for(int i=0;i<5;i++)view.Get<Text>("Slot "+i+"/Station").text=CrewDefinition.RoleLabel((CrewRole)i).ToUpperInvariant();
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            Debug.Log("CREW_STATS_UI_CONFIGURED: editable scrollable stats and fixed role labels.");
        }
        static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);Place(r,x,y,w,h);return r;
        }
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static Image Image(Transform parent,string name,float x,float y,float w,float h,Color color,Sprite sprite=null)
        {
            var img=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();img.color=color;img.sprite=sprite;img.type=sprite!=null?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;img.raycastTarget=false;return img;
        }
        static Text Label(Transform parent,string name,string text,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var label=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Text>();label.text=text;label.font=font;label.fontSize=size;label.color=color;label.alignment=align;label.raycastTarget=false;label.fontStyle=FontStyle.Bold;return label;
        }
        static Button Button(Transform parent,string name,string text,float x,float y,float w,float h,Sprite sprite=null)
        {
            var image=Image(parent,name,x,y,w,h,Color.white,sprite??skin.button);image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            Label(image.transform,"Label",text,12,4,w-24,h-8,25,Paper,TextAnchor.MiddleCenter);return button;
        }
        static void SelectionOutline(GameObject target)
        {
            var o=target.AddComponent<Outline>();o.effectColor=Gold;o.effectDistance=new Vector2(4,-4);o.enabled=false;
        }
        [MenuItem("MegaTanks/Configure Sample Crew")]
        public static void Install()
        {
            Directory.CreateDirectory("Assets/MegaTanks/Data/Crew");AssetDatabase.Refresh();
            string[] names={"Milo","Pip","Ember","Mochi","Olive"};string[] roles={"Gunner","Driver","Sub gunner","Weapon specialist","Commando"};
            string[] descriptions={"Calm under pressure, Milo keeps the crew focused on the mission.","Pip loves an open road and knows every sound the engine makes.","Patient and sharp-eyed, Ember waits for the perfect shot.","Mochi brings a steady pair of paws and boundless enthusiasm.","Olive is happiest with a toolbox nearby and a problem to solve."};
            var samples=new CrewDefinition[names.Length];
            for(int i=0;i<names.Length;i++)
            {
                string imagePath="Assets/MegaTanks/Art/Crew/"+names[i]+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(imagePath);if(importer==null)throw new InvalidOperationException("Missing crew portrait: "+imagePath);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;importer.maxTextureSize=512;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
                string path="Assets/MegaTanks/Data/Crew/"+names[i]+".asset";var member=AssetDatabase.LoadAssetAtPath<CrewDefinition>(path);
                if(member==null)
                {
                    member=ScriptableObject.CreateInstance<CrewDefinition>();member.id=names[i].ToLowerInvariant();member.displayName=names[i];member.role=(CrewRole)i;member.specialty=roles[i];member.description=descriptions[i];member.portrait=AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);AssetDatabase.CreateAsset(member,path);
                }
                samples[i]=member;
            }
            var catalog=AssetDatabase.LoadAssetAtPath<CrewCatalog>(CatalogPath);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<CrewCatalog>();catalog.members=samples;AssetDatabase.CreateAsset(catalog,CatalogPath);}
            var frontend=AssetDatabase.LoadAssetAtPath<FrontendCatalog>(CanvasUISetup.CatalogPath);frontend.crew=catalog;EditorUtility.SetDirty(frontend);
            skin=AssetDatabase.LoadAssetAtPath<EquipmentUISkin>("Assets/MegaTanks/Data/UI/EquipmentSkin.asset");font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                // A repeat install must not reset layouts edited in Prefab Mode.
                if(root.transform.Find("Tank Preview")==null)
                {
                    foreach(Transform child in root.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
                    Build(root,catalog);root.GetComponent<UIView>().CaptureBindings();PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                }
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            var home=PrefabUtility.LoadPrefabContents("Assets/MegaTanks/Prefabs/UI/Home.prefab");
            try
            {
                var badge=home.transform.Find("Crew Availability");if(badge!=null)badge.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(home,"Assets/MegaTanks/Prefabs/UI/Home.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(home);}
            AssetDatabase.SaveAssets();Debug.Log("CREW_CONFIGURED: five sample cats, crew catalog, editable crew prefab and Home navigation.");
        }
        static void Build(GameObject root,CrewCatalog catalog)
        {
            var r=(RectTransform)root.transform;Place(r,0,0,1920,1080);var t=root.transform;
            Image(t,"Background",0,0,1920,1080,Color.white,skin.background).raycastTarget=true;
            Image(t,"Title Plate",174,0,520,108,Color.white,skin.title);
            Button(t,"Close Popup","",0,0,180,108,skin.back);Label(t,"Popup Title","CREW",216,0,430,108,42,Ink,TextAnchor.MiddleCenter);
            Label(t,"Crew Summary","0 / 5 STATIONS FILLED",1350,22,520,55,28,Gold,TextAnchor.MiddleRight);
            Label(t,"Crew Instructions","Select crew, then click its highlighted station. Select a station to filter.",470,104,1000,45,24,Paper,TextAnchor.MiddleCenter);
            var tank=Rect(t,"Tank Preview",520,158,880,530);tank.gameObject.AddComponent<RectMask2D>();
            var raw=Rect(tank,"Tank Avatar",0,-285,880,1100).gameObject.AddComponent<RawImage>();raw.raycastTarget=false;
            float[] sx={58,1450,1450,58,1450},sy={202,478,202,478,340};
            Vector2[] markerPositions={new Vector2(246,150),new Vector2(548,395),new Vector2(548,150),new Vector2(418,305),new Vector2(170,305)};
            for(int i=0;i<catalog.stations.Length;i++)
            {
                var button=Button(t,"Slot "+i,"",sx[i],sy[i],412,126,skin.slot);SelectionOutline(button.gameObject);
                var slotPortrait=Image(button.transform,"Portrait",12,12,102,102,Color.white);slotPortrait.preserveAspect=true;slotPortrait.gameObject.SetActive(false);
                Label(button.transform,"Empty","+",12,12,102,102,46,Muted,TextAnchor.MiddleCenter);
                Label(button.transform,"Station",catalog.stations[i].ToUpperInvariant(),130,12,265,40,23,Gold);
                Label(button.transform,"Name","Empty",130,54,265,52,30,Paper);
                var p=markerPositions[i];var marker=Button(tank,"Tank Slot "+i,"",p.x,p.y,80,80);marker.image.sprite=null;marker.image.color=new Color(.85f,.16f,.17f);SelectionOutline(marker.gameObject);
                Image(marker.transform,"Inside",3,3,74,74,Ink);
                var markerPortrait=Image(marker.transform,"Portrait",5,5,70,70,Color.white);markerPortrait.preserveAspect=true;markerPortrait.gameObject.SetActive(false);
                Label(marker.transform,"Empty","+",5,5,70,70,34,Muted,TextAnchor.MiddleCenter);
                var tag=Image(marker.transform,"Tag",-3,58,27,26,Ink);Label(marker.transform,"Station Number",(i+1).ToString(),-3,58,27,26,18,Gold,TextAnchor.MiddleCenter);
                Image(button.transform,"Station Tag",377,0,35,32,Ink);Label(button.transform,"Station Number",(i+1).ToString(),377,0,35,32,18,Gold,TextAnchor.MiddleCenter);
            }
            Image(t,"Inventory Panel",20,728,1880,332,Panel,skin.panel);
            Button(t,"Inventory Filter","ALL CREW",48,748,340,64,skin.dropdownField);Button(t,"Sort Direction","NAME: A–Z",415,748,280,64,skin.dropdownField);
            Label(t,"Roster Caption","CREW ROSTER",725,748,245,64,25,Gold);
            var viewport=Image(t,"Inventory",48,830,906,207,Ink);viewport.raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();var content=Rect(viewport.transform,"Content",0,0,906,200);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport.rectTransform;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            var grid=content.gameObject.AddComponent<GridLayoutGroup>();grid.cellSize=new Vector2(132,158);grid.spacing=new Vector2(14,16);grid.padding=new RectOffset(8,8,8,8);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=6;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            // Editor roster examples are replaced by catalog-driven prefab instances at runtime.
            var cellPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MegaTanks/Prefabs/UI/InventoryItem.prefab");
            foreach(var member in catalog.members)
            {
                var cell=(GameObject)PrefabUtility.InstantiatePrefab(cellPrefab,content);cell.name="Crew "+member.id;var view=cell.GetComponent<UIView>();view.Get<Image>("Icon").sprite=member.portrait;view.Get<Text>("Item Name").text=member.displayName;view.Get<RectTransform>("Equipped Indicator").gameObject.SetActive(false);cell.GetComponent<Outline>().enabled=false;
            }
            Label(t,"Empty Inventory","No crew match this filter.",65,850,850,150,26,Muted,TextAnchor.MiddleCenter).gameObject.SetActive(false);
            Image(t,"Detail Panel",988,742,890,300,Ink);
            var detail=Image(t,"Detail Portrait",1005,763,120,120,Color.white);detail.preserveAspect=true;detail.gameObject.SetActive(false);
            Label(t,"Detail Name","CREW ASSIGNMENT",1140,752,710,40,28,Paper);Label(t,"Detail Role","",1140,793,710,34,22,Gold);
            Label(t,"Detail Description","Select a cat from your crew roster.",1140,833,710,66,22,Paper);
            Label(t,"Detail Assignment","Target station: Gunner",1005,906,520,110,22,Muted);
            Button(t,"Assign Crew","ASSIGN",1548,918,310,54);Button(t,"Unequip Crew","UNEQUIP",1548,983,310,54,skin.dropdownField);
        }
    }
}
