using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace MegaTanks
{
    public sealed partial class CanvasGameUI
    {
        readonly List<Text> damagePool=new List<Text>();
        void BuildHud()
        {
            var surface=Fill(hud,"Battlefield Pan Surface",0,180,1920,720,Color.clear);
            surface.gameObject.AddComponent<BattlefieldPanInput>().cameraPan=game.CameraPan;
            Fill(hud,"Header",0,0,1920,180,ink);Fill(hud,"Footer",0,900,1920,180,ink);Label(hud,"Brand","MEGATANKS",35,15,590,80,52);
            live["round"]=Label(hud,"Round","",35,105,630,55,25,muted);
            live["encounter"]=Label(hud,"Encounter","",720,118,770,42,23,muted);
            live["score"]=Label(hud,"Score","",1530,118,355,42,26,Color.white,TextAnchor.MiddleRight);
            for(int i=0;i<4;i++)
            {
                string key=new[]{"mg","cannon","mortar","scraps"}[i];float x=690+i*258;
                Fill(hud,key+" Counter",x,20,240,90,panel);Label(hud,key+" Title",new[]{"MG BULLETS","CANNON ROUNDS","MORTAR SHELLS","SCRAPS"}[i],x+15,24,210,30,21,muted);
                live[key]=Label(hud,key+" Value","",x+15,54,210,40,32,i==0?cyan:gold);
                if(i<3){Fill(hud,key+" Empty Bar",x+15,99,210,5,ink);bars[key]=Fill(hud,key+" Bar",x+15,99,210,5,i==0?cyan:gold);}
            }
            Button(hud,"Pause","II",1760,22,125,90,()=>game.SetPaused(!game.Paused));Label(hud,"Pan Hint","BATTLEFIELD / DRAG TO PAN",30,192,1250,45,25);
            live["tactics"]=Label(hud,"Tactics","",30,910,1830,40,24,muted);
            defensive=Button(hud,"Defensive","DEFENSIVE",390,963,560,78,()=>game.SetStance(TankStance.Defensive));
            aggressive=Button(hud,"Aggressive","AGGRESSIVE",975,963,560,78,()=>game.SetStance(TankStance.Aggressive));
            live["hull"]=Label(hud,"Hull","",105,827,500,40,26,Color.white,TextAnchor.MiddleCenter);
            Fill(hud,"Hull Empty",185,870,340,22,panel).raycastTarget=false;bars["hull"]=Fill(hud,"Hull Bar",185,870,340,22,cyan);bars["hull"].raycastTarget=false;
        }
        void UpdateHud()
        {
            live["round"].text="ROUND "+((game.Stage.Current?.round??0)+1)+" / "+game.Stage.Definition.rounds+"    "+game.EnemyCount+" HOSTILES";
            live["encounter"].text=game.Stage.Current==null?"":StageNodeIcons.Name(game.Stage.Current.EncounterType).ToUpperInvariant()+" / WAVE "+game.Stage.EncounterWave+" OF "+game.Stage.EncounterWaves;
            live["score"].text=game.Score.ToString("000000")+" PTS";
            Counter("mg",game.MachineGun.Ammo,game.MachineGun.ReserveCapacity);Counter("cannon",game.MainGun.Ammo,game.MainGun.ReserveCapacity);Counter("mortar",game.Mortar.Ammo,game.Mortar.Stats.reserveCapacity);
            live["scraps"].text=game.Rewards.Scraps.ToString("N0");
            var pixels=game.TankCamera.pixelRect;var top=ScreenPoint(new Vector3(pixels.xMin,pixels.yMax));var bottom=ScreenPoint(new Vector3(pixels.xMax,pixels.yMin));
            var surface=hud.Find("Battlefield Pan Surface").GetComponent<RectTransform>();surface.anchoredPosition=new Vector2(top.x,-top.y);surface.sizeDelta=bottom-top;
            live["hull"].text="HULL  "+Mathf.CeilToInt(game.Hull)+" / "+Mathf.CeilToInt(game.MaxHull);bars["hull"].rectTransform.sizeDelta=new Vector2(340*Mathf.Clamp01(game.Hull/game.MaxHull),22);
            var p=ScreenPoint(game.TankCamera.WorldToScreenPoint(new Vector3(game.Artwork.transform.position.x,BattleGame.GroundHeight-.65f,0)));
            var tankViewport=game.TankCamera.WorldToViewportPoint(game.Artwork.transform.position);
            bool hullVisible=tankViewport.z>0&&tankViewport.x>=0&&tankViewport.x<=1&&tankViewport.y>=0&&tankViewport.y<=1;
            live["hull"].gameObject.SetActive(hullVisible);bars["hull"].gameObject.SetActive(hullVisible);hud.Find("Hull Empty").gameObject.SetActive(hullVisible);
            float x=Mathf.Clamp(p.x,190,1730),y=Mathf.Min(p.y,852);live["hull"].rectTransform.anchoredPosition=new Vector2(x-250,-y+28);
            var empty=hud.Find("Hull Empty").GetComponent<RectTransform>();empty.anchoredPosition=new Vector2(x-170,-y-14);bars["hull"].rectTransform.anchoredPosition=empty.anchoredPosition;
            bars["hull"].color=game.Hull/game.MaxHull<=.3f?Color.red:game.Hull/game.MaxHull<=.6f?gold:cyan;
            live["tactics"].text="TACTICS / "+(game.Stance==TankStance.Defensive?"HOLDING POSITION":game.AllWeaponsInRange?"ALL WEAPONS IN RANGE":"ADVANCING");
            SkinStance(defensive,game.Stance==TankStance.Defensive,false);SkinStance(aggressive,game.Stance==TankStance.Aggressive,true);
            defensive.interactable=aggressive.interactable=game.Running;
        }
        void Counter(string key,int amount,int capacity){live[key].text=amount+" / "+capacity;bars[key].rectTransform.sizeDelta=new Vector2(210*(capacity>0?Mathf.Clamp01((float)amount/capacity):0),5);}
        Vector2 ScreenPoint(Vector3 p){RectTransformUtility.ScreenPointToLocalPointInRectangle(root,p,null,out var local);return new Vector2(local.x+960,540-local.y);}
        void LateUpdate()
        {
            if(game==null)return;if(lastSafe!=Screen.safeArea||lastSize!=new Vector2(Screen.width,Screen.height))FitSafeArea();
            if(Input.GetKeyDown(KeyCode.Escape)){if(popup!="")ClosePopup();else if(game.Deployed)game.SetPaused(!game.Paused);}
            Refresh();if(game.Deployed){UpdateHud();UpdateDamage();}
        }
        void Refresh()
        {
            home.gameObject.SetActive(!game.Deployed);hud.gameObject.SetActive(game.Deployed);
            damageLayer.gameObject.SetActive(game.Deployed&&game.Stage.Mode==StageMode.Combat&&!game.Rewards.IsOpen);
            string key=game.Deployed?game.Stage.Mode+"/"+game.Paused+"/"+(game.Hull<=0)+"/"+game.Rewards.Mode+"/"+game.Rewards.Scraps+"/"+game.Stage.Current?.id+"/"+game.Stage.FreeClaimed+"/"+game.Stage.PaidPurchased+"/"+string.Join(",",game.Rewards.Offers.Select(o=>o.purchased.ToString())):"Home";
            if(key==screenKey)return;screenKey=key;Clear(overlay);if(!game.Deployed)return;
            if(game.Rewards.IsOpen){DrawRewards();return;}if(game.Hull<=0){Result("HULL DESTROYED","Regroup and prepare another run.");return;}if(game.Paused){PauseScreen();return;}
            switch(game.Stage.Mode)
            {
                case StageMode.Map:DrawMap();break;
                case StageMode.Service:DrawService();break;
                case StageMode.Reveal:FullPanel("UNKNOWN NODE REVEALED");Label(overlay,"Reveal Type",StageNodeIcons.Name(game.Stage.Current.EncounterType).ToUpperInvariant(),300,360,1320,180,60,cyan,TextAnchor.MiddleCenter);Button(overlay,"Enter Node","ENTER NODE",660,800,600,90,()=>game.Stage.ContinueReveal());break;
                case StageMode.WaveBreak:FullPanel("NEXT WAVE APPROACHING");Label(overlay,"Wave Break","Wave "+(game.Stage.EncounterWave+1)+" of "+game.Stage.EncounterWaves,500,420,920,110,48,cyan,TextAnchor.MiddleCenter);break;
                case StageMode.Victory:Result("VICTORY",game.Stage.Definition.stageName+" cleared.\n"+game.Score+" POINTS / "+game.Wave+" WAVES");break;
            }
        }
        void FullPanel(string title){Fill(overlay,"Screen Background",0,0,1920,1080,ink);Label(overlay,"Screen Title",title,60,45,1800,90,52,Color.white,TextAnchor.MiddleCenter);}
        void PauseScreen()
        {
            Fill(overlay,"Pause Shade",0,0,1920,1080,new Color(0,0,0,.8f));Fill(overlay,"Pause Panel",520,210,880,670,panel);
            Label(overlay,"Pause Title","BATTLE PAUSED",590,250,740,100,48,Color.white,TextAnchor.MiddleCenter);
            Button(overlay,"Resume","RESUME",660,410,600,85,()=>game.SetPaused(false));Button(overlay,"Battle Settings","SETTINGS",660,530,600,85,()=>OpenPopup("Settings"));Button(overlay,"Return Home","RETURN HOME",660,650,600,85,ConfirmHome);
        }
        void ConfirmHome()
        {
            popup="Leave run";Clear(modal);PopupShell("LEAVE THIS RUN?",410,270,1100,550,ClosePopup);
            Label(modal,"Leave Warning","Current run progress will be discarded. Your equipped items and settings will be kept.",480,380,960,145,32,muted,TextAnchor.MiddleCenter);
            Button(modal,"Cancel Leave","CANCEL",480,630,430,90,ClosePopup);Button(modal,"Confirm Leave","RETURN HOME",1000,630,430,90,()=>{ClosePopup();game.ReturnHome();});
        }
        void Result(string title,string subtitle)
        {
            FullPanel(title);Label(overlay,"Result Detail",subtitle,260,300,1400,250,42,muted,TextAnchor.MiddleCenter);
            Button(overlay,"Play Again","PLAY AGAIN",660,710,600,95,()=>game.StartBattle());Button(overlay,"Result Home","HOME",660,850,600,85,()=>game.ReturnHome());
        }
        void DrawRewards()
        {
            bool shop=game.Rewards.Mode==IntermissionMode.Shop;FullPanel(shop?"CARD SHOP":"CHOOSE YOUR REWARD");Label(overlay,"Reward Caption",shop?game.Rewards.Scraps+" SCRAPS":"Take one card, then choose your next route.",200,140,1520,50,28,muted,TextAnchor.MiddleCenter);
            var offers=game.Rewards.Offers;int cols=shop?4:3;float w=shop?365:460,h=shop?350:640;
            for(int i=0;i<offers.Length;i++)
            {
                int index=i;var offer=offers[i];float x=(1920-cols*w-(cols-1)*32)/2+i%cols*(w+32),y=215+i/cols*(h+20);
                Card(overlay,offer.card,x,y,w,h,offer.purchased?"PURCHASED":shop?"BUY / "+offer.card.price+" SCRAPS":"TAKE CARD",()=>{if(shop)game.Rewards.Buy(index);else game.Rewards.ChooseReward(index);},!offer.purchased&&(!shop||game.Rewards.Scraps>=offer.card.price));
            }
            if(shop)Button(overlay,"Leave Shop","CONTINUE",680,975,560,75,()=>game.Rewards.LeaveShop());
        }
        void Card(Transform parent,CardDefinition card,float x,float y,float w,float h,string action,Action click,bool enabled)
        {
            var frame=Fill(parent,"Card "+card.id,x,y,w,h,CardRewardSystem.RarityColor(card.rarity));Fill(frame.transform,"Inside",4,4,w-8,h-8,panel);
            if(card.art!=null){var art=Fill(frame.transform,"Artwork",20,20,w-40,h*.50f,Color.white);art.sprite=card.art;art.preserveAspect=true;art.raycastTarget=false;}
            Label(frame.transform,"Title",card.title,20,h*.53f,w-40,60,28,Color.white,TextAnchor.MiddleCenter);Label(frame.transform,"Effect",card.EffectText,20,h*.64f,w-40,h*.17f,24,muted,TextAnchor.MiddleCenter);
            Button(frame.transform,"Action",action,20,h-80,w-40,60,click,new Color(.13f,.37f,.41f)).interactable=enabled;
        }
        void DrawService()
        {
            var stage=game.Stage;FullPanel(StageNodeIcons.Name(stage.Current.EncounterType).ToUpperInvariant());Label(overlay,"Scraps",game.Rewards.Scraps+" SCRAPS",660,130,600,60,30,gold,TextAnchor.MiddleCenter);
            Card(overlay,stage.FreeServiceCard,400,220,520,630,stage.FreeClaimed?"CLAIMED":"TAKE FREE CARD",()=>stage.ClaimFreeService(),!stage.FreeClaimed);
            Card(overlay,stage.PaidServiceCard,1000,220,520,630,stage.PaidPurchased?"PURCHASED":"BUY / "+stage.PaidServiceCard.price+" SCRAPS",()=>stage.BuyService(),!stage.PaidPurchased&&game.Rewards.Scraps>=stage.PaidServiceCard.price);
            Button(overlay,"Leave Service","CONTINUE TO MAP",660,930,600,90,()=>stage.LeaveService());
        }
        void DrawMap()
        {
            var stage=game.Stage;FullPanel(stage.Definition.stageName.ToUpperInvariant()+" / CHOOSE YOUR ROUTE");Label(overlay,"Map Hint","Select a connected node to advance. Drag horizontally to explore the route.",200,145,1520,55,27,muted,TextAnchor.MiddleCenter);
            var scroll=Scroll(overlay,"Route Scroll",60,230,1800,660,out var content,true);float width=Mathf.Max(1800,stage.Route.rounds.Count*210);content.sizeDelta=new Vector2(width,660);
            Func<StageNode,Vector2> point=n=>new Vector2(100+n.round*(width-200)/(stage.Route.rounds.Count-1),120+(1-n.height)*390);
            foreach(var column in stage.Route.rounds)foreach(var node in column)foreach(int next in node.next)Line(content,"Route Connection",point(node),point(stage.Route.Get(next)),node==stage.Current&&node.completed?cyan:edge,5);
            foreach(var column in stage.Route.rounds)foreach(var node in column)
            {
                var p=point(node);var type=node.type==StageNodeType.Random&&!node.entered?StageNodeType.Random:node.EncounterType;
                int id=node.id;var nodeButton=Button(content,"Node "+id,"",p.x-43,p.y-43,86,86,()=>stage.SelectNode(id),node==stage.Current?new Color(.18f,.4f,.43f):panel);nodeButton.interactable=stage.CanSelect(id);
                NodeIcon(nodeButton.transform,type);
                Label(content,"Node Name",StageNodeIcons.Name(type),p.x-85,p.y+48,170,65,20,muted,TextAnchor.UpperCenter);
                if(node==stage.Current){var tank=Box(content,"Current Player Tank",p.x-45,p.y-132,90,85).gameObject.AddComponent<RawImage>();tank.texture=portrait;tank.raycastTarget=false;}
            }
            Canvas.ForceUpdateCanvases();scroll.horizontalNormalizedPosition=Mathf.Clamp01((float)stage.Current.round/Mathf.Max(1,stage.Route.rounds.Count-3));Label(overlay,"Legend","B BATTLE     HB HARD BATTLE     S SUPPLY     R REPAIR     ? UNKNOWN     BOSS FINAL BATTLE",100,945,1720,75,25,muted,TextAnchor.MiddleCenter);
        }
        void NodeIcon(Transform parent,StageNodeType type)
        {
            Color color=StageNodeIcons.ColorFor(type);
            void Bar(float x,float y,float w,float h,Color? c=null){Fill(parent,"Icon",x,y,w,h,c??color).raycastTarget=false;}
            void Disc(float x,float y,float size,Color? c=null){var d=Box(parent,"Icon",x,y,size,size).gameObject.AddComponent<CanvasCircleImage>();d.color=c??color;d.raycastTarget=false;}
            if(type==StageNodeType.Battle||type==StageNodeType.HardBattle)
            {
                Line(parent,"Crossed Guns",new Vector2(20,63),new Vector2(65,17),color,7);Line(parent,"Crossed Guns",new Vector2(66,63),new Vector2(21,17),color,7);
                Line(parent,"Guard",new Vector2(12,45),new Vector2(35,68),color,6);Line(parent,"Guard",new Vector2(51,68),new Vector2(74,45),color,6);
                if(type==StageNodeType.HardBattle)for(int i=0;i<3;i++)Disc(25+i*15,73,7);
            }
            else if(type==StageNodeType.Supply){Bar(12,49,62,25);Bar(9,42,68,6);for(int i=0;i<3;i++){Bar(22+i*20,20,9,20);Disc(22+i*20,15,9);}}
            else if(type==StageNodeType.Repair){Bar(34,13,18,60);Bar(13,34,60,18);}
            else if(type==StageNodeType.Random)Label(parent,"Unknown Icon","?",0,0,86,86,62,color,TextAnchor.MiddleCenter);
            else{Disc(22,25,43);Bar(31,56,26,15);Disc(31,40,9,ink);Disc(49,40,9,ink);Bar(25,15,37,8);Bar(25,9,7,10);Bar(40,7,7,12);Bar(55,9,7,10);}
        }
        void UpdateDamage()
        {
            int used=0;var camera=game.TankCamera;foreach(var number in game.DamageNumbers)
            {
                var viewport=camera.WorldToViewportPoint(number.Position);if(viewport.z<0||viewport.x<0||viewport.x>1||viewport.y<0||viewport.y>1)continue;
                if(used>=damagePool.Count){var t=Label(damageLayer,"Damage Number","",0,0,160,50,32,Color.white,TextAnchor.MiddleCenter);t.gameObject.AddComponent<Shadow>();damagePool.Add(t);}
                var label=damagePool[used++];label.gameObject.SetActive(true);var at=ScreenPoint(camera.WorldToScreenPoint(number.Position));label.rectTransform.anchoredPosition=new Vector2(at.x-80,-at.y+25);
                label.text=number.DisplayText;var c=number.IsTank?new Color(1,.35f,.3f):gold;c.a=1-Mathf.SmoothStep(0,1,number.Age/BattleGame.DamageNumberLifetime);label.color=c;
            }
            for(int i=used;i<damagePool.Count;i++)damagePool[i].gameObject.SetActive(false);
        }
    }
}
