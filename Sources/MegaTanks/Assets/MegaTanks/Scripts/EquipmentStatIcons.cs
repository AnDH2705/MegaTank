using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    // Adapts the existing detail strings; gameplay calculations remain in CanvasEquipmentUI.
    public sealed class EquipmentStatIcons : MonoBehaviour
    {
        public string[] iconIds;
        public Sprite[] icons;
        public Text sourceStyle;
        readonly List<GameObject> pool=new List<GameObject>();
        public int VisibleStatCount{get;private set;}
        public int MissingIconCount{get;private set;}
        public float ContentHeight{get;private set;}
        public struct Row
        {
            public string icon,label,value;
            public Row(string icon,string label,string value){this.icon=icon;this.label=label;this.value=value;}
        }
        static readonly string[] Prefixes={"BASE RELOAD TIME","BONUS RELOAD SPEED","BATTLE RELOAD","RELOAD SPEED","TANK EVASION","CRIT DMG","CRIT","DMG","BASE","BONUS","HIT","PEN","RANGE","AMMO","HP","ARMOR","ARM","SPD","SPEED","EVASION","EVA","ACCURACY","TARGET","TURN","FLIGHT"};
        public static List<Row> Parse(EquipmentDefinition selected,string source)
        {
            var rows=new List<Row>();
            foreach(string original in source.Split('\n'))
            {
                string line=original.Trim();if(line.Length==0)continue;
                if(line.StartsWith("ARMOR BONUSES: ",StringComparison.Ordinal)){rows.Add(new Row(null,"ARMOR BONUSES",null));line=line.Substring(15);}
                if(line.StartsWith("EQUIPPED ",StringComparison.Ordinal)||line.StartsWith("TANK TOTALS",StringComparison.Ordinal)){rows.Add(new Row(null,LoadoutArtPresentation.Ascii(line),null));continue;}
                foreach(string part in line.Split(new[]{" | "," · "},StringSplitOptions.RemoveEmptyEntries))
                {
                    string text=part.Trim();string upper=text.ToUpperInvariant();
                    if(upper.StartsWith("CRIT ")&&text.Contains(" / "))
                    {
                        var pair=text.Substring(5).Split(new[]{" / "},StringSplitOptions.None);rows.Add(new Row("crit_rate","CRIT RATE",pair[0]));rows.Add(new Row("crit_damage","CRIT DAMAGE",pair[1]));continue;
                    }
                    if(upper=="NORMAL"||upper=="FIRE"){rows.Add(new Row("atk","DAMAGE TYPE",text));continue;}
                    if(upper.StartsWith("ACCURACY ABOVE")||upper.StartsWith("REDUCES ")){rows.Add(new Row(null,text,null));continue;}
                    bool matched=false;
                    foreach(string prefix in Prefixes)
                    {
                        if(!upper.StartsWith(prefix+" ",StringComparison.Ordinal))continue;
                        string id=Id(prefix,selected),label=Label(prefix,selected);rows.Add(new Row(id,label,text.Substring(prefix.Length).TrimStart()));matched=true;break;
                    }
                    if(matched)continue;
                    var accessory=selected as AccessoryDefinition;
                    if(accessory!=null&&(upper.StartsWith("MG ")||upper.StartsWith("CANNON ")||upper.StartsWith("MORTAR ")||upper.StartsWith("MISSILES ")))
                    {
                        int space=text.IndexOf(' ');string id=accessory.bonusType==AccessoryBonusType.Accuracy?"accuracy":accessory.bonusType==AccessoryBonusType.Evasion?"evasion":"reload_time";
                        rows.Add(new Row(id,text.Substring(0,space)+(id=="accuracy"?" ACCURACY":" RELOAD"),text.Substring(space+1)));continue;
                    }
                    if(accessory!=null&&text.StartsWith("+",StringComparison.Ordinal))
                    {
                        string id=accessory.bonusType==AccessoryBonusType.Accuracy?"accuracy":accessory.bonusType==AccessoryBonusType.Evasion?"evasion":"reload_speed";
                        rows.Add(new Row(id,"PER LEVEL",text.Replace(" percentage points per level"," pp")));continue;
                    }
                    rows.Add(new Row(null,text,null));
                }
            }
            return DisplayRows(rows);
        }
        static List<Row> DisplayRows(List<Row> rows)
        {
            var result=new List<Row>();Row? section=null;
            foreach(var row in rows)
            {
                if(row.icon==null)
                {
                    // Keep comparison/group headings only when the group has a visible stat.
                    section=row.label.StartsWith("EQUIPPED ",StringComparison.Ordinal)||row.label.StartsWith("TANK TOTALS",StringComparison.Ordinal)||row.label=="ARMOR BONUSES"?(Row?)row:null;
                    continue;
                }
                if(row.label=="PER LEVEL")continue;
                if(section.HasValue){result.Add(section.Value);section=null;}
                result.Add(row);
            }
            return result;
        }
        static string Id(string prefix,EquipmentDefinition selected)
        {
            switch(prefix)
            {
                case "TARGET":return "accuracy";case "TURN":return "move_speed";case "FLIGHT":return "reload_time";
                case "BASE RELOAD TIME":case "BATTLE RELOAD":return "reload_time";
                case "RELOAD SPEED":case "BONUS RELOAD SPEED":return "reload_speed";
                case "TANK EVASION":case "EVASION":case "EVA":return "evasion";
                case "CRIT DMG":return "crit_damage";case "CRIT":return "crit_rate";
                case "HIT":case "ACCURACY":return "accuracy";case "PEN":return "penetration";
                case "RANGE":return "range";case "AMMO":return "ammo";case "HP":return "hp";
                case "ARMOR":case "ARM":return "defense";case "SPD":return "move_speed";
                case "SPEED":return selected is ArmorDefinition?"move_speed":"reload_speed";
                default:return "atk";
            }
        }
        static string Label(string prefix,EquipmentDefinition selected)
        {
            switch(prefix)
            {
                case "BASE":return "BASE DAMAGE";case "BONUS":return "DAMAGE BONUS";case "DMG":return "DAMAGE";
                case "HIT":return "ACCURACY";case "PEN":return "PENETRATION";case "CRIT":return "CRIT RATE";case "CRIT DMG":return "CRIT DAMAGE";
                case "BASE RELOAD TIME":return "BASE RELOAD";case "BATTLE RELOAD":return "BATTLE RELOAD";
                case "BONUS RELOAD SPEED":return "RELOAD SPEED";case "ARM":case "ARMOR":return "ARMOR";case "SPD":return "MOVE SPEED";
                case "EVA":return "EVASION";case "SPEED":return selected is ArmorDefinition?"MOVE SPEED":"RELOAD SPEED";
                default:return prefix;
            }
        }
        static void Place(RectTransform r,float x,float y,float w,float h)
        {r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        Text Text(Transform parent,string name,int size)
        {
            var text=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<Text>();text.transform.SetParent(parent,false);text.font=sourceStyle.font;text.fontSize=size;text.color=Color.white;text.raycastTarget=false;text.alignment=TextAnchor.UpperLeft;text.verticalOverflow=VerticalWrapMode.Overflow;return text;
        }
        GameObject CreateRow()
        {
            var root=new GameObject("Stat",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));root.transform.SetParent(transform,false);root.GetComponent<Image>().color=Color.clear;
            var icon=new GameObject("Icon",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();icon.transform.SetParent(root.transform,false);icon.preserveAspect=true;icon.raycastTarget=false;
            Text(root.transform,"Label",7);Text(root.transform,"Value",9);return root;
        }
        public static string ComparisonValue(string value)
        {
            string text=LoadoutArtPresentation.Ascii(value??"");int arrow=text.IndexOf(" > ",StringComparison.Ordinal);
            if(arrow<0)return text;
            int start=arrow+3,end=text.IndexOf(" (",start,StringComparison.Ordinal);if(end<0)end=text.Length;
            return text.Substring(0,start)+"<color=#60FF60>"+text.Substring(start,end-start)+"</color>"+text.Substring(end);
        }
        public float Draw(EquipmentDefinition selected,string source)
        {
            var rows=Parse(selected,source);VisibleStatCount=MissingIconCount=0;float y=0,width=((RectTransform)transform).rect.width;
            sourceStyle.enabled=false;
            for(int i=0;i<rows.Count;i++)
            {
                if(i==pool.Count)pool.Add(CreateRow());var root=pool[i];root.SetActive(true);var row=rows[i];root.name=row.icon!=null?"Stat "+row.label:"Section "+row.label;
                var icon=root.transform.Find("Icon").GetComponent<Image>();var label=root.transform.Find("Label").GetComponent<Text>();var value=root.transform.Find("Value").GetComponent<Text>();
                label.text=LoadoutArtPresentation.Ascii(row.label);value.supportRichText=true;value.text=ComparisonValue(row.value);bool stat=row.icon!=null;icon.gameObject.SetActive(stat);value.gameObject.SetActive(stat);
                float height;
                if(stat)
                {
                    int index=Array.IndexOf(iconIds,row.icon);icon.sprite=index>=0&&index<icons.Length?icons[index]:null;if(icon.sprite==null)MissingIconCount++;VisibleStatCount++;
                    Place(label.rectTransform,30,2,width-32,11);Place(value.rectTransform,30,14,width-32,20);height=Mathf.Max(34,18+value.preferredHeight);Place(icon.rectTransform,0,(height-25)*.5f,25,25);label.color=new Color(.81f,.86f,.92f);
                }
                else{Place(label.rectTransform,0,2,width,14);height=Mathf.Max(19,label.preferredHeight+7);label.color=new Color(1,.83f,.3f);}
                Place((RectTransform)root.transform,0,y,width,height);y+=height+3;
            }
            for(int i=rows.Count;i<pool.Count;i++)pool[i].SetActive(false);
            ContentHeight=y;((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(153,y));return y;
        }
    }
}
