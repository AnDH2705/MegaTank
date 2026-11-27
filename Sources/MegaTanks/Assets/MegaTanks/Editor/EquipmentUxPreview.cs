using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    // Approval-only mockup. It never changes the Equipment prefab, loadout, upgrade logic or saves.
    [InitializeOnLoad]
    public static class EquipmentUxPreview
    {
        const string Output="C:/Users/seagu/OneDrive/Documents/ChatGPT/megatank/Checks/EquipmentUXPreview";
        static readonly string[] Modes={"cannon","mortar","hull"};
        static int index,wait,lastFrame;static GameObject preview;static CrewHtmlScreen source;static HtmlBattleArt art;
        static StageRewardSaveScope saves;static bool oldOptionsEnabled;static EnterPlayModeOptions oldOptions;
        static EquipmentUxPreview(){EditorApplication.update+=Pending;}
        static void Pending()
        {
            const string marker="Checks/equipment-ux-preview.pending";
            if(!File.Exists(marker)||EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating||!OriginalPortSetup.AssembliesCurrent())return;
            File.Delete(marker);Run();
        }
        [MenuItem("MegaTanks/Preview Equipment UX for approval")]
        public static void Run()
        {
            if(EditorApplication.isPlaying)return;Directory.CreateDirectory(Output);saves=new StageRewardSaveScope();
            index=0;wait=20;lastFrame=-1;
            oldOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;oldOptions=EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));MegaTanksSetup.SetLandscapeGameView(view);view.Show();
            EditorApplication.playModeStateChanged+=State;EditorApplication.isPlaying=true;
        }
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.update+=Tick;
            if(state!=PlayModeStateChange.EnteredEditMode)return;
            EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=State;saves?.Dispose();
            EditorSettings.enterPlayModeOptionsEnabled=oldOptionsEnabled;EditorSettings.enterPlayModeOptions=oldOptions;
        }
        static void Tick()
        {
            try
            {
                if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;if(--wait>0)return;
                if(index>=Modes.Length*2){EditorApplication.isPlaying=false;return;}
                if(index%2==0){Build(Modes[index/2]);wait=15;}
                else{ScreenCapture.CaptureScreenshot(Output+"/"+Modes[index/2]+".png");wait=8;}
                index++;
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.isPlaying=false;}
        }
        static RectTransform R(Transform p,string n,float x,float y,float w,float h)
        {var r=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        static Image I(Transform p,string n,Sprite sprite,float x,float y,float w,float h)
        {var i=R(p,n,x,y,w,h).gameObject.AddComponent<Image>();i.sprite=sprite;i.raycastTarget=false;return i;}
        static CrewBitmapText T(Transform p,string n,string value,float x,float y,float w,float h,float size=12,bool center=true,Color? color=null)
        {var t=R(p,n,x,y,w,h).gameObject.AddComponent<CrewBitmapText>();t.atlas=source.nameLabel.atlas;t.metrics=source.nameLabel.metrics;t.Value=value;t.fontSize=size;t.centered=center;t.color=color??Color.white;t.raycastTarget=false;return t;}
        static Sprite S(string key)=>art.Image(key)?.sprite;
        static Transform Find(Transform wrapper,string path)=>wrapper.Find(path);
        static void Set(Transform wrapper,string path,string text,float? size=null)
        {var t=Find(wrapper,path).GetComponent<CrewBitmapText>();t.Value=text;if(size.HasValue)t.fontSize=size.Value;}
        static void Tank(Transform p,string skin,float x,float y,float w,float h,bool contain=false)
        {
            var parts=art.Profile(skin).parts;var root=R(p,"Tank parts "+skin,x,y,w,h);
            float minX=contain?parts.Min(a=>a.position[0]):0,minY=contain?parts.Min(a=>a.position[1]):0;
            float spanX=contain?parts.Max(a=>a.position[0]+a.size[0])-minX:1351,spanY=contain?parts.Max(a=>a.position[1]+a.size[1])-minY:1277;
            float fit=Mathf.Min(w/spanX,h/spanY),dx=(w-spanX*fit)*.5f,dy=contain?(h-spanY*fit)*.5f:0;
            foreach(var part in parts.OrderBy(a=>a.order))I(root,part.name,S(part.key),dx+(part.position[0]-minX)*fit,dy+(part.position[1]-minY)*fit,part.size[0]*fit,part.size[1]*fit);
        }
        static void PartButton(Transform p,string value,float x,float y,float w,bool selected)
        {
            I(p,"Choose "+value,selected?source.yellowButton:source.grayButton,x,y,w,25).type=Image.Type.Sliced;
            T(p,"Part name",value,x+5,y+4,w-10,17,12,true,selected?Color.black:Color.white);
        }
        static void Build(string mode)
        {
            if(preview!=null){preview.SetActive(false);Object.Destroy(preview);}
            source=Resources.FindObjectsOfTypeAll<CrewHtmlScreen>().First(c=>c.gameObject.scene.IsValid());art=Resources.Load<HtmlBattleArt>("HtmlBattleArt");
            preview=R(source.transform.parent,"Equipment UX Approval Preview",0,0,1920,1080).gameObject;
            var wrapper=Object.Instantiate(source.transform.Find("Crew Design 770x436").gameObject,preview.transform,false).transform;
            foreach(var b in wrapper.GetComponentsInChildren<Button>(true))b.onClick.RemoveAllListeners();
            foreach(var station in source.stations)Find(wrapper,station.button.name).gameObject.SetActive(false);
            Find(wrapper,"Tank").gameObject.SetActive(false);Find(wrapper,"Skill Tooltip").gameObject.SetActive(false);
            string skin=mode=="hull"?"heavy":"base";Tank(wrapper,skin,75,91,275,277);wrapper.GetChild(wrapper.childCount-1).SetSiblingIndex(2);
            PartButton(wrapper,"MG",149,94,89,false);PartButton(wrapper,"MORTAR",94,182,94,mode=="mortar");PartButton(wrapper,"CANNON",209,182,94,mode=="cannon");PartButton(wrapper,"TANK HULL",111,374,183,mode=="hull");
            var frame=I(wrapper,"Gear balance",S("counter_counter_frame"),232,16,105,20);frame.type=Image.Type.Sliced;frame.color=new Color(1,1,1,.8f);
            I(wrapper,"Gear icon",S("counter_gear"),221,10,29,29).preserveAspect=true;T(wrapper,"Gear amount","320",256,19,73,15,12);
            Set(wrapper,"Role Title",mode=="hull"?"Tank Hull":mode=="mortar"?"Tank Mortar":"Tank Cannon");
            Set(wrapper,"Crew Name",mode=="hull"?"HEAVY HULL":mode=="mortar"?"MORTAR":"AP CANNON",12);
            Set(wrapper,"Crew Level",mode=="cannon"?"LV.12":"LV.1");Find(wrapper,"Crew Stars").GetComponent<UIStarStrip>().Stars=mode=="cannon"?2:0;
            var hero=Find(wrapper,"Hero Portrait").GetComponent<Image>();hero.sprite=S(mode=="cannon"?"card_cannon_ap":"tank_base_09_mortar_barrel");hero.preserveAspect=true;
            if(mode=="hull"){hero.gameObject.SetActive(false);Tank(wrapper,"heavy",355,63,68,65,true);}
            string[] names={"atk","hp","accuracy","defense","move_speed","crit_rate"};
            var originalIcons=names.Select(n=>Find(wrapper,"Stat Icon "+n).GetComponent<Image>().sprite).ToArray();
            string[] labels=mode=="hull"?new[]{"HULL HP","SPEED","DEFENSE","CREW","GUN SLOTS","HULL LEVEL"}:new[]{"DAMAGE","AMMO","ACCURACY","RELOAD","RANGE","CRIT"};
            string[] values=mode=="hull"?new[]{"480","85%","0","5","3","1"}:mode=="mortar"?new[]{"32","16","100%","2.4s","15","0%"}:new[]{"24","24","96%","1.2s","12","10%"};
            for(int j=0;j<6;j++)
            {
                var value=Find(wrapper,"Stat "+names[j]).GetComponent<CrewBitmapText>();value.Value=values[j];value.color=Color.black;
                int col=j%2,row=j/2;float x=356+col*94,y=143+row*36;
                T(wrapper,"Stat caption "+j,labels[j],x+27,y-4,66,10,5.6f,false,new Color(.13f,.14f,.18f));
                var icon=Find(wrapper,"Stat Icon "+names[j]).GetComponent<Image>();
                if(mode=="hull"){icon.sprite=originalIcons[new[]{1,4,3,3,0,5}[j]];icon.preserveAspect=true;}
                if(j==1&&mode!="hull"){icon.sprite=S("ammoicon_ammo_"+(mode=="mortar"?"mortar":"cannon"));icon.preserveAspect=true;}
                if(j==3&&mode!="hull"){icon.sprite=S("counter_gear");icon.preserveAspect=true;}
                if(j==4&&mode!="hull")icon.sprite=Find(wrapper,"Stat Icon accuracy").GetComponent<Image>().sprite;
            }
            string[] skillText=mode=="hull"?new[]{"More\nHP","Heavy\nArmor","Lower\nSpeed"}:new[]{"Base\nDamage","Reload\nTime","Ammo\nReserve"};
            for(int j=0;j<3;j++)
            {
                Set(wrapper,"Skill Name "+j,skillText[j]);var icon=Find(wrapper,"Skill "+j+"/Icon").GetComponent<Image>();icon.material=null;
                icon.sprite=j==0?Find(wrapper,"Stat Icon atk").GetComponent<Image>().sprite:j==1?S("counter_gear"):S("ammoicon_ammo_"+(mode=="mortar"?"mortar":"cannon"));icon.preserveAspect=true;
                if(mode=="hull")icon.sprite=originalIcons[new[]{1,3,4}[j]];
            }
            Set(wrapper,"Cost and Feedback",mode=="hull"?"NEXT LEVEL: HULL HP 480 > 492":"NEXT LEVEL: DAMAGE "+(mode=="mortar"?"32 > 34":"24 > 26"));
            Set(wrapper,"Level Up/Label","LEVEL UP!");
            Find(wrapper,"Ascend/Progress").gameObject.SetActive(false);Find(wrapper,"Ascend/Fragment Icon").gameObject.SetActive(false);
            Find(wrapper,"Ascend").GetComponent<Image>().sprite=source.grayButton;
            I(Find(wrapper,"Ascend"),"Upgrade gears",S("counter_gear"),6,8,21,21).preserveAspect=true;
            Set(wrapper,"Ascend/Label",mode=="cannon"?"120":"100");
            Set(wrapper,"Equip/Label",mode=="mortar"?"Equipped":mode=="hull"?"USE HULL":"Equip");
            Set(wrapper,"Roster Title",mode=="hull"?"Select tank hull:":"Select your gun:",19);
            for(int j=0;j<source.candidates.Length;j++)
            {
                string path="Roster/Content/Candidate "+j;var cell=Find(wrapper,path);var image=cell.GetComponent<Image>();image.sprite=source.emptyPortrait;
                Find(cell,"Selected Outline").gameObject.SetActive(j==(mode=="cannon"?1:mode=="hull"?2:0));Find(cell,"Equipped Tick").gameObject.SetActive(j==0);
                Set(cell,"Name","");
                if(mode=="hull"&&j<3){Tank(cell,new[]{"base","speed","heavy"}[j],3,3,59,59,true);Set(cell,"Name",new[]{"NORMAL","SPEED","HEAVY"}[j]);}
                else if(mode=="cannon"&&j<2){I(cell,"Gun art",S(j==0?"tank_base_06_main_cannon_barrel":"card_cannon_ap"),4,4,57,56).preserveAspect=true;Set(cell,"Name",j==0?"CANNON":"ARMOR PIERCING");}
                else if(mode=="mortar"&&j==0){I(cell,"Green mortar",S("tank_base_09_mortar_barrel"),5,5,55,55).preserveAspect=true;Set(cell,"Name","MORTAR");}
                // Keep selection/tick above the illustrated item, like the Crew screen.
                Find(cell,"Selected Outline").SetAsLastSibling();Find(cell,"Equipped Tick").SetAsLastSibling();Find(cell,"Name").SetAsLastSibling();
            }
            Find(wrapper,"Back").SetAsLastSibling();Canvas.ForceUpdateCanvases();
        }
    }
}
