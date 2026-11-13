using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace MegaTanks
{
    public sealed partial class CanvasGameUI
    {
        readonly List<UIView> damagePool=new List<UIView>();
        readonly UIScreen[] encounterScreens={UIScreen.Pause,UIScreen.Defeat,UIScreen.Victory,UIScreen.Rewards,UIScreen.Shop,UIScreen.Service,UIScreen.Reveal,UIScreen.WaveBreak,UIScreen.Route};
        void BindHud()
        {
            var v=View(UIScreen.BattleHUD);v.Get<BattlefieldPanInput>("Battlefield Pan Surface").cameraPan=game.CameraPan;
            Click(v,"Pause",()=>game.SetPaused(!game.Paused));
        }
        void UpdateHud()
        {
            var v=View(UIScreen.BattleHUD);
            Text(v,"Round","ROUND "+((game.Stage.Current?.round??0)+1)+" / "+game.Stage.Definition.rounds+"    "+game.EnemyCount+" HOSTILES");
            Text(v,"Encounter",game.Stage.Current==null?"":StageNodeIcons.Name(game.Stage.Current.EncounterType).ToUpperInvariant()+" / WAVE "+game.Stage.EncounterWave+" OF "+game.Stage.EncounterWaves);
            Text(v,"Score",game.Score.ToString("000000")+" PTS");
            Counter("mg",game.MachineGun.Ammo,game.MachineGun.ReserveCapacity);Counter("cannon",game.MainGun.Ammo,game.MainGun.ReserveCapacity);Counter("mortar",game.Mortar.Ammo,game.Mortar.Stats.reserveCapacity);
            Text(v,"scraps Value",game.Rewards.Scraps.ToString("N0"));
            Text(v,"Hull Anchor/Hull","HULL  "+Mathf.CeilToInt(game.Hull)+" / "+Mathf.CeilToInt(game.MaxHull));
            var bar=v.Get<Image>("Hull Anchor/Hull Bar");bar.fillAmount=game.MaxHull>0?Mathf.Clamp01(game.Hull/game.MaxHull):0;
            bar.color=bar.fillAmount<=.3f?layout.criticalHull:bar.fillAmount<=.6f?layout.hurtHull:layout.healthyHull;
            float tankBottom=game.Artwork.transform.position.y-102f/180*game.Artwork.transform.lossyScale.y;
            var at=ScreenPoint(game.TankCamera.WorldToScreenPoint(new Vector3(game.Artwork.transform.position.x+layout.hullOffset.x,tankBottom+layout.hullOffset.y,0)));
            var anchor=v.Get<RectTransform>("Hull Anchor");anchor.anchoredPosition=new Vector2(at.x,-at.y);
            var p=game.TankCamera.WorldToViewportPoint(game.Artwork.transform.position);anchor.gameObject.SetActive(p.z>0&&p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1);
        }
        void Counter(string key,int amount,int capacity)
        {
            var v=View(UIScreen.BattleHUD);Text(v,key+" Value",amount+" / "+capacity);v.Get<Image>(key+" Bar").fillAmount=capacity>0?Mathf.Clamp01((float)amount/capacity):0;
        }
        Vector2 ScreenPoint(Vector3 p){RectTransformUtility.ScreenPointToLocalPointInRectangle(root,p,null,out var local);return new Vector2(local.x+960,540-local.y);}
        void LateUpdate()
        {
            if(game==null)return;if(lastSafe!=Screen.safeArea||lastSize!=new Vector2(Screen.width,Screen.height))FitSafeArea();
            if(Input.GetKeyDown(KeyCode.Escape)){if(popup!="")ClosePopup();else if(game.Deployed)game.SetPaused(!game.Paused);}
            Refresh();if(game.Deployed){UpdateHud();UpdateDamage();}
        }
        void Refresh()
        {
            View(UIScreen.Home).gameObject.SetActive(!game.Deployed);View(UIScreen.BattleHUD).gameObject.SetActive(game.Deployed);
            layout.damageLayer.gameObject.SetActive(game.Deployed&&game.Stage.Mode==StageMode.Combat&&!game.Rewards.IsOpen);
            string key=game.Deployed?game.Stage.Mode+"/"+game.Paused+"/"+(game.Hull<=0)+"/"+game.Rewards.Mode+"/"+game.Rewards.Scraps+"/"+game.Stage.Current?.id+"/"+game.Stage.FreeClaimed+"/"+game.Stage.PaidPurchased+"/"+string.Join(",",game.Rewards.Offers.Select(o=>o.purchased.ToString())):"Home";
            if(key==screenKey)return;screenKey=key;foreach(var s in encounterScreens)View(s).gameObject.SetActive(false);if(!game.Deployed)return;
            if(game.Rewards.IsOpen){DrawRewards();return;}
            if(game.Hull<=0){Result(UIScreen.Defeat,"Regroup and prepare another run.");return;}
            if(game.Paused)
            {
                var v=Show(UIScreen.Pause);Click(v,"Resume",()=>game.SetPaused(false));Click(v,"Battle Settings",()=>OpenPopup("Settings"));Click(v,"Return Home",ConfirmHome);return;
            }
            switch(game.Stage.Mode)
            {
                case StageMode.Map:DrawMap();break;
                case StageMode.Service:DrawService();break;
                case StageMode.Reveal:var v=Show(UIScreen.Reveal);Text(v,"Reveal Type",StageNodeIcons.Name(game.Stage.Current.EncounterType).ToUpperInvariant());Click(v,"Enter Node",()=>game.Stage.ContinueReveal());break;
                case StageMode.WaveBreak:Text(Show(UIScreen.WaveBreak),"Wave Break","Wave "+(game.Stage.EncounterWave+1)+" of "+game.Stage.EncounterWaves);break;
                case StageMode.Victory:Result(UIScreen.Victory,game.Stage.Definition.stageName+" cleared.\n"+game.Score+" POINTS / "+game.Wave+" WAVES");break;
            }
        }
        UIView Show(UIScreen screen){var v=View(screen);v.gameObject.SetActive(true);return v;}
        void Result(UIScreen screen,string detail)
        {
            var v=Show(screen);Text(v,"Result Detail",detail);Click(v,"Play Again",()=>game.StartBattle());Click(v,"Result Home",()=>game.ReturnHome());
        }
        void DrawRewards()
        {
            bool shop=game.Rewards.Mode==IntermissionMode.Shop;var v=Show(shop?UIScreen.Shop:UIScreen.Rewards);
            Text(v,"Reward Caption",shop?game.Rewards.Scraps+" SCRAPS":"Take one card, then choose your next route.");
            var offers=game.Rewards.Offers;
            for(int i=0;i<offers.Length;i++)
            {
                int index=i;var offer=offers[i];BindCard(v,"Card "+i,offer.card,offer.purchased?"PURCHASED":shop?"BUY / "+offer.card.price+" SCRAPS":"TAKE CARD",()=>{if(shop)game.Rewards.Buy(index);else game.Rewards.ChooseReward(index);},!offer.purchased&&(!shop||game.Rewards.Scraps>=offer.card.price));
            }
            if(shop)Click(v,"Leave Shop",()=>game.Rewards.LeaveShop());
        }
        void BindCard(UIView v,string key,CardDefinition card,string action,Action click,bool enabled)
        {
            v.Get<Image>(key).color=CardRewardSystem.RarityColor(card.rarity);
            var image=v.Get<Image>(key+"/Artwork");image.sprite=card.art;image.gameObject.SetActive(card.art!=null);
            Text(v,key+"/Title",card.title);Text(v,key+"/Effect",card.EffectText);Text(v,key+"/Action/Label",action);
            Click(v,key+"/Action",click);v.Get<Button>(key+"/Action").interactable=enabled;
        }
        void DrawService()
        {
            var stage=game.Stage;var v=Show(UIScreen.Service);Text(v,"Screen Title",StageNodeIcons.Name(stage.Current.EncounterType).ToUpperInvariant());Text(v,"Scraps",game.Rewards.Scraps+" SCRAPS");
            BindCard(v,"Card 0",stage.FreeServiceCard,stage.FreeClaimed?"CLAIMED":"TAKE FREE CARD",()=>stage.ClaimFreeService(),!stage.FreeClaimed);
            BindCard(v,"Card 1",stage.PaidServiceCard,stage.PaidPurchased?"PURCHASED":"BUY / "+stage.PaidServiceCard.price+" SCRAPS",()=>stage.BuyService(),!stage.PaidPurchased&&game.Rewards.Scraps>=stage.PaidServiceCard.price);
            Click(v,"Leave Service",()=>stage.LeaveService());
        }
        void UpdateDamage()
        {
            int used=0;var camera=game.TankCamera;
            foreach(var number in game.DamageNumbers)
            {
                var p=camera.WorldToViewportPoint(number.Position);if(p.z<0||p.x<0||p.x>1||p.y<0||p.y>1)continue;
                if(used>=damagePool.Count)damagePool.Add(Instantiate(layout.damageNumber,layout.damageLayer,false));
                var view=damagePool[used++];view.gameObject.SetActive(true);var at=ScreenPoint(camera.WorldToScreenPoint(number.Position));
                view.Get<RectTransform>("$root").anchoredPosition=new Vector2(at.x,-at.y);
                var label=view.Get<Text>("Label");label.text=number.DisplayText;var color=number.IsTank?layout.playerDamage:layout.enemyDamage;
                color.a=1-Mathf.SmoothStep(0,1,number.Age/BattleGame.DamageNumberLifetime);label.color=color;
            }
            for(int i=used;i<damagePool.Count;i++)damagePool[i].gameObject.SetActive(false);
        }
    }
}
