using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MegaTanks
{
    public sealed class EquipmentHtmlScreen : MonoBehaviour
    {
        public CrewHtmlScreen template;
        public HtmlBattleArt art;
        public EquipmentHtmlCatalog catalog;
        public const int HullPart=3,WheelsPart=4;
        public int ActivePart { get; private set; }=1;
        public Object Selected { get; private set; }
        public int VisibleCount=>eligible.Length;
        public Button EquipButton=>Find("Equip").GetComponent<Button>();
        public Button UpgradeButton=>Find("Level Up").GetComponent<Button>();
        BattleGame game;
        Object[] eligible=Array.Empty<Object>();
        Transform wrapper,leftTank,heroTank;
        HtmlBattleConfig config;
        bool bound;
        static readonly string[] StatKeys={"atk","hp","accuracy","defense","move_speed","crit_rate"};
        static readonly string[] PartNames={"MG","CANNON","MORTAR","TANK HULL","WHEELS"};
        Transform Find(string path)=>wrapper.Find(path);
        Sprite S(string key)=>EquipmentHtmlCatalog.Sprite(art,key);
        CrewBitmapText TextAt(string path)=>Find(path).GetComponent<CrewBitmapText>();
        void Set(string path,string text)=>TextAt(path).Value=text;
        static RectTransform R(Transform p,string n,float x,float y,float w,float h)
        {var r=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        static Image I(Transform p,string n,Sprite sprite,float x,float y,float w,float h)
        {var i=R(p,n,x,y,w,h).gameObject.AddComponent<Image>();i.sprite=sprite;i.raycastTarget=false;return i;}
        CrewBitmapText T(Transform p,string n,string value,float x,float y,float w,float h,float size,bool center=true)
        {var t=R(p,n,x,y,w,h).gameObject.AddComponent<CrewBitmapText>();t.atlas=template.nameLabel.atlas;t.metrics=template.nameLabel.metrics;t.Value=value;t.fontSize=size;t.centered=center;t.color=Color.white;t.raycastTarget=false;return t;}
        void Ensure(){if(wrapper==null)wrapper=transform.Find("Equipment Design 770x436");if(config==null)config=HtmlBattleConfig.Load();}
        // Called by the editor migration on the existing Equipment prefab.
        public void BuildLayout()
        {
            Ensure();
            foreach(var s in template.stations)Find(s.button.name).gameObject.SetActive(false);
            Find("Tank").gameObject.SetActive(false);Find("Skill Tooltip").gameObject.SetActive(false);
            foreach(var b in wrapper.GetComponentsInChildren<Button>(true))b.onClick.RemoveAllListeners();
            float[] x={149,209,94,111,94},y={94,182,182,374,282},width={89,94,94,183,94};
            for(int i=0;i<5;i++)
            {
                var image=I(wrapper,"Part "+i,template.grayButton,x[i],y[i],width[i],25);image.type=Image.Type.Sliced;image.raycastTarget=true;
                image.gameObject.AddComponent<Button>().targetGraphic=image;
                T(image.transform,"Label",PartNames[i],5,4,width[i]-10,17,12);
            }
            var counter=I(wrapper,"Gear Balance Frame",S("counter_counter_frame"),232,16,105,20);counter.color=new Color(1,1,1,.8f);counter.type=Image.Type.Sliced;
            I(wrapper,"Gear Balance Icon",S("counter_gear"),221,10,29,29).preserveAspect=true;
            T(wrapper,"Gear Balance Amount","0",256,19,73,15,12);
            for(int j=0;j<6;j++){int col=j%2,row=j/2;var t=T(wrapper,"Stat Caption "+j,"",356+col*94+27,143+row*36-4,66,10,5.6f,false);t.color=new Color(.13f,.14f,.18f);}
            Find("Ascend/Progress").gameObject.SetActive(false);Find("Ascend/Fragment Icon").gameObject.SetActive(false);
            Object.DestroyImmediate(Find("Ascend").GetComponent<Button>());
            Find("Ascend").GetComponent<Image>().sprite=template.grayButton;
            I(Find("Ascend"),"Upgrade Gears",S("counter_gear"),6,8,21,21).preserveAspect=true;
            Set("Ascend/Label","100");Set("Level Up/Label","LEVEL UP!");
        }
        void Awake()
        {
            Ensure();
            if(bound)return;bound=true;
            Find("Back").GetComponent<Button>().onClick.AddListener(()=>game?.UI.ClosePopup());
            for(int i=0;i<5;i++){int part=i;Find("Part "+i).GetComponent<Button>().onClick.AddListener(()=>SelectPart(part));}
            var cells=Find("Roster/Content");
            for(int i=0;i<cells.childCount;i++){int index=i;cells.GetChild(i).GetComponent<Button>().onClick.AddListener(()=>SelectCandidate(index));}
            EquipButton.onClick.AddListener(Equip);UpgradeButton.onClick.AddListener(Upgrade);
            for(int i=0;i<3;i++){int n=i;Find("Skill "+i).GetComponent<Button>().onClick.AddListener(()=>ShowInfo(n));}
            Find("Skill Tooltip").GetComponent<Button>().onClick.AddListener(()=>Find("Skill Tooltip").gameObject.SetActive(false));
        }
        public void Show(BattleGame owner)
        {Ensure();game=owner;if(Selected==null)SelectPart(FirstUnlockedPart());else Refresh();}
        void OnDisable(){Selected=null;if(wrapper!=null)Find("Skill Tooltip").gameObject.SetActive(false);}
        public void SelectPart(int part)
        {
            if(game==null||part<0||part>4||!PartUnlocked(part))return;ActivePart=part;
            eligible=part==HullPart?catalog.hulls.Cast<Object>().ToArray():part==WheelsPart?catalog.wheels.Cast<Object>().ToArray():catalog.weapons.Where(w=>w.slot==part).Select(w=>(Object)w.definition).ToArray();
            var current=Equipped();Selected=eligible.Contains(current)?current:eligible.FirstOrDefault();
            Find("Roster").GetComponent<ScrollRect>().verticalNormalizedPosition=1;
            Find("Skill Tooltip").gameObject.SetActive(false);Refresh();
        }
        public void SelectCandidate(int index)
        {if(index<0||index>=eligible.Length)return;Selected=eligible[index];Find("Skill Tooltip").gameObject.SetActive(false);Refresh();}
        Object Equipped()=>ActivePart==HullPart?(Object)game.TankProfile:ActivePart==WheelsPart?game.Equipment.Wheels:ActivePart==0?game.Equipment.MachineGun:ActivePart==1?game.Equipment.Cannon:game.Equipment.Mortar;
        int PartRequiredLevel(int part)=>config.equipmentUnlock.LevelForPart(part);
        bool PartUnlocked(int part)=>new PlayerTankProfile(config).Level>=PartRequiredLevel(part);
        int FirstUnlockedPart()=>PartUnlocked(1)?1:Enumerable.Range(0,5).First(PartUnlocked);
        int RequiredLevel=>PartRequiredLevel(ActivePart);
        public void Equip()
        {
            if(game==null||game.Deployed||Selected==null||new PlayerTankProfile(config).Level<RequiredLevel)return;
            bool changed=Selected is TankDefinition hull?game.SelectTank(hull):Selected is EquipmentDefinition item&&game.Equipment.Equip(item,item.Type);
            if(changed){game.UI.RefreshEquipmentArtwork();Refresh();}
        }
        public void Upgrade()
        {if(game!=null&&!game.Deployed&&Selected!=null&&PartUnlocked(ActivePart))game.Progression.TryLevelUp(Selected);}
        string Title(Object value)=>value is TankDefinition h?(EquipmentHtmlCatalog.Skin(h)=="base"?"NORMAL HULL":EquipmentHtmlCatalog.Skin(h)=="speed"?"SPEED HULL":"HEAVY HULL"):value is WheelsDefinition w?w.displayName:catalog.Weapon((WeaponDefinition)value).title;
        Sprite Icon(Object value)=>value is WheelsDefinition w?S(w.skin=="offroad"?"offroad_base_02_offroad_wheel_rear":"tank_base_02_wheel_01"):value is WeaponDefinition gun?S(catalog.Weapon(gun).artKey):null;
        void ClearArt(Transform root)
        {foreach(Transform child in root){child.gameObject.SetActive(false);Destroy(child.gameObject);}}
        void DrawTank(Transform p,string hull,string wheels,float x,float y,float w,float h,bool contain)
        {
            var parts=EquipmentHtmlCatalog.Parts(art,hull,wheels);var root=R(p,"Tank Parts",x,y,w,h);
            float minX=contain?parts.Min(a=>a.position[0]):0,minY=contain?parts.Min(a=>a.position[1]):0;
            float sx=contain?parts.Max(a=>a.position[0]+a.size[0])-minX:1351,sy=contain?parts.Max(a=>a.position[1]+a.size[1])-minY:1277;
            float z=Mathf.Min(w/sx,h/sy),dx=(w-sx*z)*.5f,dy=contain?(h-sy*z)*.5f:0;
            foreach(var a in parts.OrderBy(a=>a.order))I(root,a.name,S(a.key),dx+(a.position[0]-minX)*z,dy+(a.position[1]-minY)*z,a.size[0]*z,a.size[1]*z);
        }
        public void Refresh()
        {
            if(game==null||Selected==null)return;Ensure();
            int tankLevel=new PlayerTankProfile(config).Level;
            if(tankLevel<RequiredLevel){SelectPart(FirstUnlockedPart());return;}
            for(int i=0;i<5;i++)
            {
                bool unlocked=tankLevel>=PartRequiredLevel(i),selected=i==ActivePart;
                var button=Find("Part "+i).GetComponent<Button>();button.interactable=unlocked;
                button.image.sprite=selected?template.yellowButton:template.grayButton;
                var colors=button.colors;colors.disabledColor=new Color(.48f,.48f,.52f,1);button.colors=colors;
                var label=TextAt("Part "+i+"/Label");
                label.Value=unlocked?PartNames[i]:PartNames[i]+"  LV."+PartRequiredLevel(i);
                label.fontSize=unlocked?12:8.5f;
                var labelRect=(RectTransform)label.transform;labelRect.anchoredPosition=new Vector2(unlocked?5:23,-4);labelRect.sizeDelta=new Vector2(((RectTransform)button.transform).sizeDelta.x-(unlocked?10:28),17);
                label.color=!unlocked?new Color(.62f,.64f,.69f):selected?Color.black:Color.white;
                var icon=button.transform.Find("Lock Icon");
                if(icon==null){icon=R(button.transform,"Lock Icon",5,3,17,19);var graphic=icon.gameObject.AddComponent<UILockIcon>();graphic.color=new Color(1,.91f,.62f);graphic.raycastTarget=false;}
                icon.gameObject.SetActive(!unlocked);
            }
            Set("Gear Balance Amount",game.Wallet.PetFood.ToString("N0"));
            if(leftTank==null){leftTank=R(wrapper,"Equipment Tank",0,0,770,436);leftTank.SetSiblingIndex(2);}
            ClearArt(leftTank);
            string skin=EquipmentHtmlCatalog.Skin(ActivePart==HullPart?(TankDefinition)Selected:game.TankProfile);
            string wheelStyle=ActivePart==WheelsPart?((WheelsDefinition)Selected).skin:game.Equipment.Wheels?.skin;
            DrawTank(leftTank,skin,wheelStyle,75,91,275,277,false);
            Set("Role Title",ActivePart==HullPart?"Tank Hull":ActivePart==WheelsPart?"Tank Wheels":ActivePart==0?"Machine Gun":ActivePart==1?"Tank Cannon":"Tank Mortar");
            Set("Roster Title",ActivePart==HullPart?"Select tank hull:":ActivePart==WheelsPart?"Select your wheels:":"Select your gun:");
            Set("Crew Name",Title(Selected));TextAt("Crew Name").fontSize=12;
            var state=game.Progression.Get(Selected);Set("Crew Level","LV."+state.Level);Find("Crew Stars").GetComponent<UIStarStrip>().Stars=state.Stars;
            var hero=Find("Hero Portrait").GetComponent<Image>();hero.gameObject.SetActive(ActivePart!=HullPart);hero.sprite=Icon(Selected);hero.preserveAspect=true;
            if(heroTank==null)heroTank=R(wrapper,"Hull Hero",355,63,68,65);ClearArt(heroTank);
            if(ActivePart==HullPart)DrawTank(heroTank,skin,wheelStyle,0,0,68,65,true);
            DrawStats(state);
            var cells=Find("Roster/Content");
            for(int i=0;i<cells.childCount;i++)
            {
                var cell=cells.GetChild(i);var value=i<eligible.Length?eligible[i]:null;cell.GetComponent<Image>().sprite=template.emptyPortrait;cell.GetComponent<Button>().interactable=value!=null;
                var generated=cell.Find("Item Art");if(generated==null)generated=R(cell,"Item Art",0,0,65,65);ClearArt(generated);
                if(value is TankDefinition h)DrawTank(generated,EquipmentHtmlCatalog.Skin(h),game.Equipment.Wheels?.skin,3,3,59,59,true);
                else if(value!=null)I(generated,"Icon",Icon(value),4,4,57,56).preserveAspect=true;
                cell.Find("Selected Outline").gameObject.SetActive(value!=null&&value==Selected);cell.Find("Equipped Tick").gameObject.SetActive(value!=null&&value==Equipped());
                cell.Find("Name").GetComponent<CrewBitmapText>().Value=value!=null?Title(value):"";
                cell.Find("Selected Outline").SetAsLastSibling();cell.Find("Equipped Tick").SetAsLastSibling();cell.Find("Name").SetAsLastSibling();
            }
            bool locked=new PlayerTankProfile(config).Level<RequiredLevel,assigned=Selected==Equipped();
            EquipButton.interactable=!game.Deployed&&!locked&&!assigned;
            Set("Equip/Label",locked?"TANK LV. "+RequiredLevel:assigned?"Equipped":ActivePart==HullPart?"USE HULL":"Equip");
            int cost=ProgressionRules.LevelCost(state.Level);Set("Ascend/Label",cost>0?cost.ToString("N0"):"MAX");
            UpgradeButton.interactable=!game.Deployed&&!locked&&game.Progression.CanLevelUp(Selected);
            Set("Level Up/Label",cost>0?"LEVEL UP!":"MAX LEVEL");
            Find("Back").SetAsLastSibling();Canvas.ForceUpdateCanvases();
        }
        void DrawStats(ProgressionState state)
        {
            string[] labels,values,skillNames;
            Sprite[] icons=new Sprite[6];
            for(int i=0;i<6;i++)icons[i]=template.transform.Find("Crew Design 770x436/Stat Icon "+StatKeys[i]).GetComponent<Image>().sprite;
            var baseIcons=(Sprite[])icons.Clone();
            if(Selected is WeaponDefinition weapon)
            {
                var entry=catalog.Weapon(weapon);var gun=config.weapons[ActivePart];var member=game.WeaponCrew(weapon.Type);var p=game.Progression.Get(member);
                float bonus=member!=null?member.stats.damageBonus.AtLevel(p.Level,p.Stars):0,reload=member!=null?member.stats.reloadSpeed.AtLevel(p.Level,p.Stars):0,crit=member!=null?member.stats.criticalChance.AtLevel(p.Level,p.Stars):0;
                float multiplier=EquipmentHtmlCatalog.VariantMultiplier(config,entry.variant)*(1+bonus/100),damage=EquipmentHtmlCatalog.WeaponBaseDamage(config,game.Progression,entry)*multiplier;
                labels=new[]{"DAMAGE","AMMO","ACCURACY","RELOAD","RANGE","CRIT"};
                values=new[]{damage.ToString("0.#"),config.stages[0].startingAmmo[ActivePart].ToString(),"100%",(gun.cooldown/Mathf.Max(.1f,1+reload/100)).ToString("0.##")+"s",(gun.maxRange/100).ToString("0.#"),crit.ToString("0.#")+"%"};
                icons[1]=S("ammoicon_ammo_"+gun.id);icons[3]=S("counter_gear");icons[4]=baseIcons[2];
                skillNames=new[]{"Base\nDamage","Reload\nTime","Ammo\nReserve"};
                Set("Cost and Feedback","NEXT LEVEL: DAMAGE "+damage.ToString("0.#")+" > "+(damage+config.permanentWeaponDamagePerLevel*(1+.1f*state.Stars)*multiplier).ToString("0.#"));
            }
            else if(Selected is TankDefinition hull)
            {
                float hp=EquipmentHtmlCatalog.HullHP(config,art,game.Progression,hull);var driver=game.Crew.Get(CrewRole.Driver);if(driver!=null){var p=game.Progression.Get(driver);hp+=driver.stats.hp.AtLevel(p.Level,p.Stars);}
                float speed=art.Profile(EquipmentHtmlCatalog.Skin(hull)).speed*EquipmentHtmlCatalog.WheelSpeed(config,game.Progression,game.Equipment.Wheels);
                labels=new[]{"HULL HP","SPEED","DEFENSE","CREW","GUN SLOTS","HULL LEVEL"};values=new[]{hp.ToString("0.#"),(speed*100).ToString("0.#")+"%",EquipmentHtmlCatalog.WheelDefense(config,game.Equipment.Wheels).ToString("0.#"),"5","3",state.Level.ToString()};
                icons=new[]{baseIcons[1],baseIcons[4],baseIcons[3],baseIcons[3],baseIcons[0],baseIcons[5]};skillNames=new[]{"Hull\nHealth","Move\nSpeed","All\nWeapons"};
                Set("Cost and Feedback","NEXT LEVEL: HULL HP "+hp.ToString("0.#")+" > "+(hp+config.permanentHullHPPerLevel).ToString("0.#"));
            }
            else
            {
                var wheels=(WheelsDefinition)Selected;float speed=EquipmentHtmlCatalog.WheelSpeed(config,game.Progression,wheels)*100;
                labels=new[]{"SPEED","DEFENSE","WHEELS","HULL FITS","LEVEL","GEAR COST"};values=new[]{speed.ToString("0.#")+"%",EquipmentHtmlCatalog.WheelDefense(config,wheels).ToString("0.#"),wheels.skin=="offroad"?"2":"4","3",state.Level.ToString(),ProgressionRules.LevelCost(state.Level).ToString("N0")};
                icons=new[]{baseIcons[4],baseIcons[3],Icon(wheels),baseIcons[1],baseIcons[5],S("counter_gear")};skillNames=new[]{"Road\nSpeed","Wheel\nSet","All\nHulls"};
                Set("Cost and Feedback","NEXT LEVEL: SPEED "+speed.ToString("0.#")+"% > "+(speed+config.permanentWheelSpeedPerLevel*100).ToString("0.#")+"%");
            }
            for(int i=0;i<6;i++){Set("Stat Caption "+i,labels[i]);Set("Stat "+StatKeys[i],values[i]);TextAt("Stat "+StatKeys[i]).color=Color.black;var im=Find("Stat Icon "+StatKeys[i]).GetComponent<Image>();im.sprite=icons[i];im.preserveAspect=true;}
            for(int i=0;i<3;i++){Set("Skill Name "+i,skillNames[i]);var im=Find("Skill "+i+"/Icon").GetComponent<Image>();im.material=null;im.sprite=icons[ActivePart<3?new[]{0,3,1}[i]:new[]{0,1,2}[i]];im.preserveAspect=true;}
        }
        void ShowInfo(int index)
        {
            var box=Find("Skill Tooltip");box.Find("Text").GetComponent<Text>().text=Title(Selected)+"\n"+TextAt("Skill Name "+index).Value.Replace("\n"," ")+"\n"+TextAt("Cost and Feedback").Value+"\nUpgrade cost: "+ProgressionRules.LevelCost(game.Progression.Get(Selected).Level).ToString("N0")+" gears";box.gameObject.SetActive(true);box.SetAsLastSibling();
        }
    }
}
