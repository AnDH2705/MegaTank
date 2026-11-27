using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    public sealed class CrewHtmlScreen : MonoBehaviour
    {
        [Serializable] public sealed class Station { public CrewRole role; public Button button; public Image portrait, outline, plate; public CrewBitmapText label; [NonSerialized] public GameObject lockBadge; }
        [Serializable] public sealed class Candidate { public Button button; public Image portrait, selected, equipped; public CrewBitmapText name; }
        [Serializable] public sealed class Skill { public string role, name, purpose; public Sprite icon; }
        public Station[] stations;
        public Candidate[] candidates;
        public Skill[] skills;
        public Sprite normalPortrait, meleePortrait, emptyPortrait, yellowButton, grayButton, normalPlate, selectedPlate;
        public Material lockedMaterial;
        public Image hero, ascendProgress, fragmentIcon;
        public Image[] skillIcons;
        public Button back, equip, levelUp, ascend;
        public CrewBitmapText roleLabel, nameLabel, levelLabel, equipLabel, levelUpLabel, ascendLabel, feedback;
        public CrewBitmapText[] stats, skillNames;
        public UIStarStrip stars;
        public GameObject tooltip;
        public Text tooltipText;
        public ScrollRect rosterScroll;
        BattleGame game;
        CrewRole activeRole = CrewRole.Gunner;
        CrewDefinition selected;
        CrewDefinition[] eligible = Array.Empty<CrewDefinition>();
        float feedbackUntil;
        public CrewRole ActiveRole => activeRole;
        public CrewDefinition Selected => selected;
        public int VisibleCount => eligible.Length;
        public void Show(BattleGame owner)
        {
            bool opening = game != owner || selected == null;
            game = owner;
            if(opening) SelectStation(CrewRole.Gunner); else Refresh();
        }
        void Awake()
        {
            tooltip.GetComponent<Button>().onClick.AddListener(()=>tooltip.SetActive(false));
            back.onClick.AddListener(()=>game?.UI.ClosePopup());
            foreach(var s in stations) { var role=s.role; s.button.onClick.AddListener(()=>SelectStation(role)); }
            for(int i=0;i<candidates.Length;i++) { int index=i; candidates[i].button.onClick.AddListener(()=>SelectCandidate(index)); }
            for(int i=0;i<skillIcons.Length;i++) { int index=i; skillIcons[i].GetComponentInParent<Button>().onClick.AddListener(()=>ShowSkill(index)); }
            equip.onClick.AddListener(Equip);
            levelUp.onClick.AddListener(()=> { if(game!=null && selected!=null && !game.Deployed && game.Progression.TryLevelUp(selected)) { Refresh(); Message("LEVEL UP!"); } });
            ascend.onClick.AddListener(()=> { if(game!=null && selected!=null && !game.Deployed && game.Progression.TryStarUp(selected)) { Refresh(); Message("ASCENDED!"); } });
        }
        void OnDisable() { if(tooltip!=null) tooltip.SetActive(false); selected=null; }
        void Update()
        {
            if(feedback != null && feedback.gameObject.activeSelf && Time.unscaledTime>feedbackUntil) feedback.gameObject.SetActive(false);
            if(Input.GetKeyDown(KeyCode.Escape) && tooltip.activeSelf) tooltip.SetActive(false);
        }
        public void SelectStation(CrewRole role)
        {
            if(game==null)return; activeRole=role;
            eligible=(game.Frontend.crew?.members??Array.Empty<CrewDefinition>()).Where(m=>game.Crew.CanEquip(m,(int)role)).ToArray();
            selected=game.Crew.Get(role)??eligible.FirstOrDefault();
            rosterScroll.verticalNormalizedPosition=1; tooltip.SetActive(false); Refresh();
        }
        public void SelectCandidate(int index)
        {
            if(index<0||index>=eligible.Length)return;
            selected=eligible[index];tooltip.SetActive(false);Refresh();
        }
        public void Equip()
        {
            if(game==null||game.Deployed||selected==null||new PlayerTankProfile(HtmlBattleConfig.Load()).Level<HtmlBattleConfig.Load().unlockLevels[activeRole==CrewRole.SubGunner?0:activeRole==CrewRole.Gunner?1:activeRole==CrewRole.WeaponSpecialist?2:activeRole==CrewRole.Driver?3:4])return;
            if(game.Crew.Equip(selected,(int)activeRole)) { Refresh(); Message("CREW EQUIPPED"); }
        }
        Sprite Portrait(CrewDefinition member)=>member==null?emptyPortrait:member.role==CrewRole.Commando?meleePortrait:normalPortrait;
        static int UnlockIndex(CrewRole role)=>role==CrewRole.SubGunner?0:role==CrewRole.Gunner?1:role==CrewRole.WeaponSpecialist?2:role==CrewRole.Driver?3:4;
        void StationLock(Station station,int required,bool locked)
        {
            station.portrait.material=locked?lockedMaterial:null;
            if(locked&&station.lockBadge==null)
            {
                var badge=new GameObject("Tank Level Lock",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));badge.transform.SetParent(station.portrait.transform,false);station.lockBadge=badge;
                var rect=(RectTransform)badge.transform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-3);rect.sizeDelta=new Vector2(-6,20);
                var image=badge.GetComponent<Image>();image.color=new Color(.11f,.11f,.15f,.95f);image.raycastTarget=false;
                var textObject=new GameObject("Unlock Level",typeof(RectTransform),typeof(CanvasRenderer),typeof(CrewBitmapText));textObject.transform.SetParent(rect,false);
                var textRect=(RectTransform)textObject.transform;textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=textRect.offsetMax=Vector2.zero;
                var text=textObject.GetComponent<CrewBitmapText>();text.atlas=nameLabel.atlas;text.metrics=nameLabel.metrics;text.fontSize=10;text.centered=true;text.color=new Color(1,.86f,.24f);text.raycastTarget=false;text.Value="LV."+required;
            }
            if(station.lockBadge!=null)station.lockBadge.SetActive(locked);
        }
        public void Refresh()
        {
            if(game==null)return;
            var config=HtmlBattleConfig.Load();int tankLevel=new PlayerTankProfile(config).Level;
            foreach(var s in stations)
            {
                s.portrait.sprite=Portrait(game.Crew.Get(s.role));
                int required=config.unlockLevels[UnlockIndex(s.role)];StationLock(s,required,tankLevel<required);
                s.outline.gameObject.SetActive(s.role==activeRole);
                s.plate.sprite=s.role==activeRole?selectedPlate:normalPlate;
                s.label.color=s.role==activeRole?new Color(.06f,.07f,.09f):Color.white;
            }
            for(int i=0;i<candidates.Length;i++)
            {
                var c=candidates[i];var member=i<eligible.Length?eligible[i]:null;
                c.portrait.sprite=Portrait(member);c.button.interactable=member!=null;
                c.selected.gameObject.SetActive(member!=null&&member==selected);
                c.equipped.gameObject.SetActive(member!=null&&game.Crew.Get(activeRole)==member);
                c.name.Value=member!=null?member.displayName:"";
            }
            roleLabel.Value=RoleTitle(activeRole);
            bool has=selected!=null;hero.gameObject.SetActive(has);hero.sprite=Portrait(selected);
            nameLabel.Value=has?selected.displayName:"EMPTY";
            var p=game.Progression.Get(selected);stars.Stars=has?p.Stars:0;levelLabel.Value=has?"LV."+p.Level:"";
            if(has)
            {
                var s=selected.stats;
                var resolved=new ResolvedCrewStats(selected,p,HtmlBattleConfig.Load());float[] values={resolved.attack,resolved.hp,resolved.accuracy,resolved.defense,resolved.speed,resolved.crit};
                string[] suffix={"","","%","","","%"};
                for(int i=0;i<stats.Length;i++){stats[i].Value=values[i].ToString("0.##")+suffix[i];stats[i].color=Color.black;}
            }
            else foreach(var s in stats)s.Value="--";
            var roleSkills=skills.Where(s=>s.role==RoleKey(activeRole)).ToArray();
            for(int i=0;i<skillIcons.Length;i++)
            {
                bool exists=i<roleSkills.Length;skillIcons[i].gameObject.SetActive(exists);
                if(exists){skillIcons[i].sprite=roleSkills[i].icon;skillIcons[i].material=p.Stars<new[]{1,2,4}[i]?lockedMaterial:null;skillNames[i].Value=roleSkills[i].name.Replace(" ","\n");}
            }
            bool assigned=has&&game.Crew.Get(activeRole)==selected;
            bool unlocked=new PlayerTankProfile(HtmlBattleConfig.Load()).Level>=HtmlBattleConfig.Load().unlockLevels[(int)(activeRole==CrewRole.SubGunner?0:activeRole==CrewRole.Gunner?1:activeRole==CrewRole.WeaponSpecialist?2:activeRole==CrewRole.Driver?3:4)];equip.interactable=has&&!game.Deployed&&!assigned&&unlocked;
            equipLabel.Value=!unlocked?"TANK LV. "+HtmlBattleConfig.Load().unlockLevels[activeRole==CrewRole.SubGunner?0:activeRole==CrewRole.Gunner?1:activeRole==CrewRole.WeaponSpecialist?2:activeRole==CrewRole.Driver?3:4]:assigned?"Equipped":"Equip";
            int cost=ProgressionRules.LevelCost(p.Level), fragments=ProgressionRules.StarCost(p.Stars);
            levelUp.interactable=has&&!game.Deployed&&game.Progression.CanLevelUp(selected);
            levelUpLabel.Value=cost>0?"LEVEL UP!":"MAX LEVEL";
            levelUp.GetComponent<Image>().color=levelUp.interactable?Color.white:new Color(.7f,.7f,.7f);
            bool ready=has&&game.Progression.CanStarUp(selected);
            ascend.interactable=ready&&!game.Deployed;ascend.image.sprite=ready?yellowButton:grayButton;
            ascendLabel.Value=!has?"--":fragments<=0?"MAX":ready?"ASCEND":p.Fragments+"/"+fragments;
            ascendLabel.color=ready?Color.black:Color.white;
            fragmentIcon.gameObject.SetActive(has&&!ready&&fragments>0);
            ascendProgress.gameObject.SetActive(has&&!ready&&fragments>0);
            ascendProgress.fillAmount=fragments>0?Mathf.Clamp01((float)p.Fragments/fragments):0;
            // Costs are live values; the reference's placeholder 9999 stats are never used.
            feedback.Value=has?"LEVEL UP: "+(cost>0?cost.ToString("N0")+" COINS":"MAX")+"   |   PET TAGS: "+p.Fragments:"Select a crew member";
            feedback.gameObject.SetActive(true);feedbackUntil=float.PositiveInfinity;
        }
        void Message(string value){feedback.Value=value;feedback.gameObject.SetActive(true);feedbackUntil=Time.unscaledTime+2;}
        void ShowSkill(int index)
        {
            var list=skills.Where(s=>s.role==RoleKey(activeRole)).ToArray();if(index>=list.Length)return;
            var s=list[index];int threshold=new[]{1,2,4}[index];bool locked=game.Progression.Get(selected).Stars<threshold;
            tooltipText.text=s.name+"\n"+s.purpose+"\n"+(locked?"Unlock preview: "+threshold+" stars":"Skill preview")+"\nSkill artwork is informational; active bonuses use crew stats.";
            tooltip.SetActive(true);
        }
        public static string RoleKey(CrewRole role)=>role==CrewRole.Gunner?"cannon":role==CrewRole.SubGunner?"mg":role==CrewRole.WeaponSpecialist?"mortar":role==CrewRole.Driver?"driver":"melee";
        public static string RoleTitle(CrewRole role)=>role==CrewRole.Gunner?"Tank Cannon":role==CrewRole.SubGunner?"Machine Gun":role==CrewRole.WeaponSpecialist?"Tank Mortar":role==CrewRole.Driver?"Tank Driver":"Melee Fighter";
    }
}
