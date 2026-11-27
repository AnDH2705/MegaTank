using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    public sealed class CrewRecruitmentRewardUI : MonoBehaviour
    {
        static CrewRecruitmentRewardUI active;
        public static bool IsOpen=>active!=null;
        BattleGame game;HtmlBattleArt art;RescueStageStory story;CrewDefinition member;int line;
        public int DialogueIndex=>line;
        public bool ShowingCard { get;private set; }
        public static void ShowPending(BattleGame owner)
        {
            if(IsOpen||owner.Crew.Pending==null)return;
            var pending=owner.Crew.Pending;var member=owner.Crew.Member(pending.crewId);var story=RescueEventConfig.Load().Stage(pending.stageIndex);
            if(member==null||story==null)return;
            var root=new GameObject("Crew Story and Common Reward",typeof(RectTransform));var r=(RectTransform)root.transform;r.SetParent(owner.UI.GetComponent<UICanvasLayout>().safeArea,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.sizeDelta=new Vector2(1920,1080);r.SetAsLastSibling();
            active=root.AddComponent<CrewRecruitmentRewardUI>();active.game=owner;active.art=Resources.Load<HtmlBattleArt>("HtmlBattleArt");active.story=story;active.member=member;active.DrawDialogue();
        }
        RectTransform R(Transform p,string n,float x,float y,float w,float h)
        {var r=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        Image I(Transform p,string n,Sprite sprite,float x,float y,float w,float h,Color? tint=null)
        {var i=R(p,n,x,y,w,h).gameObject.AddComponent<Image>();i.sprite=sprite;i.color=tint??Color.white;i.raycastTarget=false;return i;}
        CrewBitmapText C(Transform p,string n,string value,float x,float y,float w,float h,float size,Color? color=null)
        {var t=R(p,n,x,y,w,h).gameObject.AddComponent<CrewBitmapText>();t.atlas=art.comicFontAtlas;t.metrics=art.comicFontMetrics;t.fontSize=size;t.centered=true;t.Value=value;t.color=color??Color.white;t.raycastTarget=false;var outline=t.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.07f,.06f,.09f);outline.effectDistance=new Vector2(2,-2);return t;}
        Text T(Transform p,string n,string value,float x,float y,float w,float h,int size,Color color)
        {var t=R(p,n,x,y,w,h).gameObject.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.text=value;t.color=color;t.fontStyle=FontStyle.Bold;t.raycastTarget=false;t.alignment=TextAnchor.MiddleLeft;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
        void Clear()
        {
            foreach(Transform child in transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            I(transform,"Story Backdrop",null,0,0,1920,1080,new Color(.065f,.045f,.1f,.6f)).raycastTarget=true;
        }
        void DrawDialogue()
        {
            Clear();ShowingCard=false;var entry=story.dialogue[line];
            C(transform,"Story Chapter","STAGE "+(story.stageIndex+1)+" · CREW STORY",420,360,1080,60,34,new Color(1,.86f,.4f));
            var panel=I(transform,"Dialogue Panel",null,290,480,1340,300,new Color(.1f,.1f,.15f,.97f));panel.gameObject.AddComponent<ComicSkew>().slope=.0524f;
            I(transform,"Speaker Portrait",RescueEventArt.CrewPortrait,325,405,245,310).preserveAspect=true;
            C(transform,"Speaker",entry.speaker.ToUpperInvariant(),590,505,790,55,34,new Color(1,.86f,.4f));
            T(transform,"Dialogue",entry.text,590,570,940,120,31,new Color(.98f,.97f,.94f));
            var image=I(transform,"Next",game.UI.Skin.homeYellowPanel,1370,705,180,57);image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(Next);
            var label=T(image.transform,"Label","NEXT >",5,8,170,40,26,new Color(.1f,.1f,.14f));label.alignment=TextAnchor.MiddleCenter;label.fontStyle=FontStyle.BoldAndItalic;
        }
        public void Next(){if(ShowingCard)return;if(++line<story.dialogue.Length)DrawDialogue();else DrawCard();}
        void DrawCard()
        {
            Clear();ShowingCard=true;C(transform,"Reward Title","NEW CREWMATE",420,120,1080,80,55);
            C(transform,"Reward Rarity","COMMON CREW CARD",520,210,880,40,23,new Color(1,.86f,.4f));
            var root=R(transform,"Common Crew Card",780,300,360,580);
            var bg=I(root,"Common Card Base",art.Image("card_base_common").sprite,15,0,360,580);bg.gameObject.AddComponent<ComicSkew>().slope=.0524f;
            T(root,"Rarity","COMMON",30,16,260,30,18,new Color(.14f,.14f,.19f));
            I(root,"Crew Portrait",RescueEventArt.CrewPortrait,30,55,300,245).preserveAspect=true;
            C(root,"Name",member.displayName.ToUpperInvariant(),12,300,336,70,43);
            T(root,"Role",story.weaponSlot==0?"MG GUNNER":"MORTAR GUNNER",55,385,270,35,21,new Color(.2f,.35f,.47f));
            T(root,"Description","Joins your permanent crew.",35,430,295,65,22,new Color(.14f,.12f,.18f));
            var image=I(root,"Claim",art.Image("card_action_plate").sprite,32,515,296,48);image.type=Image.Type.Sliced;image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(Claim);C(image.transform,"Label","CLAIM >",5,3,286,42,23);
        }
        public void Claim()
        {
            if(!ShowingCard||!game.Crew.ClaimCommonRecruit(member.id))return;
            gameObject.SetActive(false);Destroy(gameObject);active=null;
        }
    }
}
