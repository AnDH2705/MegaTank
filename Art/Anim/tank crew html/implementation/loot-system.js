(function(host){
 'use strict';
 const types=['money','gears','chests'],CHEST_REWARD={money:30,gears:6};
 function init(s){s.loot=[];s.lootToasts=[];s.nextLootID=1;s.lootWallet={money:0,gears:0,chests:0};s.lootStats={drops:0,pickups:0,bossRewards:0};s.chestOpening=null;}
 function openChest(s){
  if(s.phase!=='won'||s.chestOpening||s.lootWallet.chests<1)return false;
  s.chestOpening={...CHEST_REWARD};s.lootWallet.chests--;s.lootWallet.money+=CHEST_REWARD.money;s.lootWallet.gears+=CHEST_REWARD.gears;return true;
 }
 function drop(s,e){
  const boss=e.kind==='boss',x=boss?e.x-e.w*.36:e.x;
  const entries=boss?[['money',100],['gears',20],['chests',1]]:[((e.id*17+s.config.index*11)%5<3)?['money',10]:['gears',2]];
  entries.forEach(([type,amount],i)=>{
   const px=x+(i-(entries.length-1)/2)*54;
   s.loot.push({id:s.nextLootID++,type,amount,boss,worldX:px+s.scroll.road,x:px,y:e.kind==='plane'?e.y:850,fromY:e.kind==='plane'?e.y:850,age:0,phase:'fall',flyAge:0});s.lootStats.drops++;
  });
 }
 function step(s,dt){
  s.lootToasts=s.lootToasts.filter(t=>{t.age+=dt;t.x=t.worldX-s.scroll.road;return t.age<.9;});
  for(const item of s.loot){
   item.age+=dt;
   if(item.phase==='fly'){
    item.flyAge+=dt;const u=Math.min(1,item.flyAge/.5),v=1-Math.pow(1-u,3);
    const targetX=item.type==='chests'?s.player.x:({money:1160,gears:1415}[item.type]),targetY=item.type==='chests'?930-s.player.w*.35:72;
    item.x=item.flyX+(targetX-item.flyX)*v;
    item.y=item.flyY+(targetY-item.flyY)*v-90*Math.sin(u*Math.PI);
    if(u===1){s.lootWallet[item.type]+=item.amount;s.lootStats.pickups++;if(item.boss)s.lootStats.bossRewards++;item.done=true;}
    continue;
   }
   item.x=item.worldX-s.scroll.road;
   const u=Math.min(1,item.age/.62);item.y=item.fromY+(910-item.fromY)*u-110*Math.sin(u*Math.PI);
   if(u<1)continue;item.phase='ground';
   const c=s.companion,nearTank=Math.abs(item.x-s.player.x)<s.player.w*.48+65;
   const nearCat=c&&['combat','return'].includes(c.phase)&&Math.abs(item.x-c.x)<130;
   const sweep=s.bossKilled&&c.phase==='aboard';
   if(s.phase!=='lost'&&(nearTank||nearCat||sweep)){
    item.phase='fly';item.flyX=item.x;item.flyY=item.y;item.flyAge=0;
    s.lootToasts.push({type:item.type,amount:item.amount,x:item.x,worldX:item.x+s.scroll.road,y:item.y,age:0});
   }
  }
  s.loot=s.loot.filter(item=>!item.done);
 }
 function icon(ctx,type,x,y,size){
  ctx.save();ctx.translate(x,y);ctx.scale(size/48,size/48);ctx.lineWidth=3;ctx.strokeStyle='#282735';ctx.lineJoin='round';
  if(type==='money'){
   ctx.fillStyle='#b87312';ctx.beginPath();ctx.ellipse(0,3,19,22,0,0,Math.PI*2);ctx.fill();ctx.stroke();
   ctx.fillStyle='#ffdc42';ctx.beginPath();ctx.ellipse(0,0,18,20,0,0,Math.PI*2);ctx.fill();ctx.stroke();
   ctx.strokeStyle='#fff2a0';ctx.lineWidth=2;ctx.beginPath();ctx.ellipse(0,0,13,15,0,0,Math.PI*2);ctx.stroke();
   ctx.fillStyle='#b97a14';ctx.font='bold 26px sans-serif';ctx.textAlign='center';ctx.textBaseline='middle';ctx.fillText('$',0,1);
  }else if(type==='gears'){
   ctx.fillStyle='#8fd7dc';ctx.beginPath();for(let i=0;i<32;i++){const a=i*Math.PI/16,r=i%4<2?23:17;const px=Math.cos(a)*r,py=Math.sin(a)*r;i?ctx.lineTo(px,py):ctx.moveTo(px,py);}ctx.closePath();ctx.fill();ctx.stroke();
   ctx.fillStyle='#3c5260';ctx.beginPath();ctx.arc(0,0,9,0,Math.PI*2);ctx.fill();ctx.stroke();
  }else{
   ctx.fillStyle='#a74d24';ctx.beginPath();ctx.roundRect(-24,-14,48,33,5);ctx.fill();ctx.stroke();
   ctx.fillStyle='#e3a134';ctx.beginPath();ctx.roundRect(-24,-23,48,24,8);ctx.fill();ctx.stroke();
   ctx.fillStyle='#ffd954';ctx.fillRect(-17,-21,7,37);ctx.fillRect(10,-21,7,37);ctx.strokeRect(-6,-5,12,15);ctx.fillRect(-6,-5,12,15);ctx.fillStyle='#503727';ctx.fillRect(-2,0,4,6);
  }ctx.restore();
 }
 function draw(ctx,s){
  for(const item of s.loot){
   const flying=item.phase==='fly',bob=flying?0:Math.sin(item.age*5+item.id)*4;
   if(!flying){ctx.fillStyle='rgba(30,26,20,.23)';ctx.beginPath();ctx.ellipse(item.x,927,22,5,0,0,Math.PI*2);ctx.fill();}
   icon(ctx,item.type,item.x,item.y+bob,item.type==='chests'?56:39);
   if(!flying){ctx.save();ctx.fillStyle='#fff5b2';ctx.strokeStyle='#33303a';ctx.lineWidth=4;ctx.font='900 17px sans-serif';ctx.textAlign='center';ctx.strokeText('×'+item.amount,item.x,item.y+35+bob);ctx.fillText('×'+item.amount,item.x,item.y+35+bob);ctx.restore();}
  }
  for(const t of s.lootToasts){ctx.save();ctx.globalAlpha=1-t.age/.9;ctx.font='900 24px sans-serif';ctx.textAlign='center';ctx.lineWidth=4;ctx.strokeStyle='#252633';ctx.fillStyle=t.type==='money'?'#ffdf55':t.type==='gears'?'#a3f2ef':'#ffe39b';const text='+'+t.amount+(t.type==='chests'?' RƯƠNG':'');ctx.strokeText(text,t.x,t.y-32-t.age*60);ctx.fillText(text,t.x,t.y-32-t.age*60);ctx.restore();}
 }
 const api={types,CHEST_REWARD,init,drop,step,openChest,draw,icon};host.TankLootSystem=api;if(typeof module!=='undefined'&&module.exports)module.exports=api;
})(globalThis);
