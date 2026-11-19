
(function (host) {
 'use strict';
 const STAGES = [
  { index:0, title:'THÀNH PHỐ BỎ HOANG', name:'Ngoại ô bỏ hoang', background:'streetGray', total:20, planes:6, dogs:6, interval:1.55, maxActive:3, bossHP:520, bossWeapons:['cannon'] },
  { index:1, title:'KHU CÔNG NGHIỆP', name:'Thành phố hoàng hôn', background:'street', total:25, planes:10, dogs:8, interval:1.4, maxActive:4, bossHP:820, bossWeapons:['cannon','mg'] }
 ];
 const TANK_WIDTH=580, CANNON_PUSH=16, DUEL_SPEED=60;
 const M=host.TankMeleeSystem,L=host.TankLootSystem;
 const RANGES={mortar:{min:1050,max:1500},cannon:{min:650,max:1200},mg:{min:0,max:750}};
 function inRange(actor,target,weapon,face){
  const r=RANGES[weapon],distance=(target.x-actor.x)*face;
  return !!r&&distance>=r.min&&distance<=r.max;
 }
 function random(seed) { let a=seed>>>0; return function(){a+=0x6D2B79F5;let t=a;t=Math.imul(t^t>>>15,t|1);t^=t+Math.imul(t^t>>>7,t|61);return ((t^t>>>14)>>>0)/4294967296;}; }
 function create(stageIndex, weapons, seed) {
  const config=STAGES[stageIndex===1?1:0];const rng=random(seed===undefined?20261111+config.index:seed);
  const queue=Array.from({length:config.total},(_,i)=>i<config.planes?'plane':i<config.planes+config.dogs?'dog':'car');
  for(let i=queue.length-1;i>0;i--){const j=Math.floor(rng()*(i+1));[queue[i],queue[j]]=[queue[j],queue[i]];}
  const state={ config, rng, queue, queueIndex:0, nextID:1, time:0, distance:0, scroll:{road:0,sky:0}, travelSpeed:0, bossContact:false, duel:null, rangeStall:0, phase:'wave', spawned:0, normalKills:0, planeKills:0, carKills:0, bossSpawned:false, bossKilled:false, alert:2.4, shake:0, spawnTimer:1.3, enemies:[], bullets:[], effects:[], fires:[], player:{x:340,goalX:400,ground:930,w:TANK_WIDTH,wheelDistance:0,groundSpeed:0,hp:360,maxHP:360,hurt:0,weapons:(weapons||['cannon','mg','mortar']).slice(),cooldowns:{cannon:.4,mg:.1,mortar:1.1}}, corpses:[], dogKills:0, bossMeleeStarted:false, victoryTravel:0, stats:{bossMeleeDeployments:0,bossMeleeKills:0,spawnedDogs:0,spawnedCars:0,spawnedPlanes:0,shots:{cannon:0,mg:0,mortar:0},bossWeaponShots:{cannon:0,mg:0},bossCount:0,contactCount:0,playerKnockbacks:0,bossKnockbacks:0} };
  state.companion=M.create(state.player);L.init(state);return state;
 }
 function center(e) { return {x:e.x,y:e.kind==='plane'?e.y:e.kind==='boss'||!e.kind?930-e.w*.5:e.kind==='dog'?e.y-66:930-88}; }
 function fx(s,type,x,y,size,anchor) {s.effects.push({type,x,y,size,age:0,...(anchor?{anchor,worldX:x+s.scroll[anchor]}:{})});if(s.effects.length>90)s.effects.shift();}
 function groundFire(s,x,source){s.fires.push({x,worldX:x+s.scroll.road,age:0,source,life:source==='wreck'?4.2:2.8,damage:source==='mortar'?4:0});}
 function ageEffects(s,dt){
  M.age(s,dt);
  s.effects.forEach(e=>{e.age+=dt;if(e.anchor)e.x=e.worldX-s.scroll[e.anchor];});
  s.effects=s.effects.filter(e=>e.age<(e.type==='explosion'?.7:e.type==='muzzle_flash'?.17:.25));
  s.fires.forEach(f=>{f.age+=dt;f.x=f.worldX-s.scroll.road;});s.fires=s.fires.filter(f=>f.age<f.life);
 }
 function muzzle(actor,weapon,face,target){
  const scale=actor.w/1351;
  if(weapon==='mortar')return {x:actor.x+face*(166-652)*scale,y:930+(235-1258)*scale};
  const cannon=weapon==='cannon',pivot=cannon?[1125,496]:[910,231],tip=cannon?[1348,496]:[1160,156];
  const x=actor.x+face*(pivot[0]-652)*scale,y=930+(pivot[1]-1258)*scale;
  const a=Math.max(-.85,Math.min(.55,Math.atan2(target.y-y,(target.x-x)*face)));
  const dx=(tip[0]-pivot[0])*scale,dy=(tip[1]-pivot[1])*scale;
  return {x:x+face*(Math.cos(a)*dx-Math.sin(a)*dy),y:y+Math.sin(a)*dx+Math.cos(a)*dy};
 }
 function kill(s,e) {
  if(e.dead)return;e.dead=true;e.hp=0;L.drop(s,e);
  if(e.kind==='dog'){if(e.isBossCrew)s.stats.bossMeleeKills++;else{s.normalKills++;s.dogKills++;}s.corpses.push({...e,worldX:e.x+s.scroll.road,deathAge:0});fx(s,'impact_sparks',e.x,930-65,70);return;}
  const c=center(e);fx(s,'explosion',c.x,e.kind==='plane'?c.y+75:930,e.kind==='boss'?e.w:230,e.kind==='plane'?'sky':'road');
  if(e.kind!=='plane')groundFire(s,e.x,'wreck');
  s.shake=Math.max(s.shake,e.kind==='boss'?7:2.5);
  if(e.kind==='boss'){s.bossKilled=true;s.phase='boss_cleanup';s.bullets=[];s.alert=0;}
  else {s.normalKills++;if(e.kind==='plane')s.planeKills++;else s.carKills++;}
 }
 function damageEnemy(s,e,amount,kind) {
  if(e.dead||s.phase==='lost'||s.phase==='won'||s.phase==='blocked')return;e.hp-=amount;e.hurt=.1;e.hurtAge=0;
  if(kind==='cannon'&&!(e.kind==='boss'&&s.bossMeleeStarted&&M.active(s))){e.x+=CANNON_PUSH;if(e.kind==='boss'){s.stats.bossKnockbacks++;s.bossContact=false;}}
  if(e.hp<=0)kill(s,e);
 }
 function spawn(s,kind) {
  const hard=s.config.index===1;const id=s.nextID++;
  if(kind==='dog')return M.spawnDog(s,id,hard);
  const e={id,kind,x:kind==='boss'?2230:2110+s.rng()*80,y:kind==='plane'?350+s.rng()*170:930,baseY:0,t:0,w:kind==='boss'?TANK_WIDTH:kind==='plane'?330:300,speed:kind==='boss'?DUEL_SPEED:kind==='plane'?205:155,stopX:kind==='boss'?0:s.player.goalX+(kind==='plane'?460+s.rng()*190:420+s.rng()*100),hp:kind==='boss'?s.config.bossHP:kind==='plane'?(hard?48:36):(hard?78:55),maxHP:0,fire:1.3+s.rng(),mgFire:.4,dead:false,hurt:0,weapons:kind==='boss'?s.config.bossWeapons.slice():['mg']};
  e.maxHP=e.hp;e.baseY=e.y;s.enemies.push(e);
  if(kind==='boss'){
   s.bossSpawned=true;s.stats.bossCount++;s.phase='boss';s.alert=4;s.bullets=[];
   s.duel={phase:'entry',center:960,leftStart:s.player.x,rightStart:1920-s.player.x,leftTarget:960-.52*s.player.w,rightTarget:960+.52*e.w,speed:DUEL_SPEED,playerTravel:0,bossTravel:0};
  }
  else{s.spawned++;if(kind==='plane')s.stats.spawnedPlanes++;else s.stats.spawnedCars++;}
  return e;
 }
 function choose(s,weapon) {
  const all=s.enemies.filter(e=>!e.dead&&!(e.kind==='dog'&&weapon!=='mg')&&e.x-e.w*.25<1920&&(e.kind!=='boss'||s.duel.phase!=='entry')&&inRange(s.player,e,weapon,1));if(!all.length)return null;
  all.sort((a,b)=>{
   const pa=weapon==='mg'?(a.kind==='plane'?0:1):(a.kind==='plane'?1:0);
   const pb=weapon==='mg'?(b.kind==='plane'?0:1):(b.kind==='plane'?1:0);
   return pa-pb||a.x-b.x;
  });return all[0];
 }
 function fire(s,faction,weapon,origin,target,damage,owner,actor) {
  const speed=weapon==='mg'?1500:940;
  const dist=Math.hypot(target.x-origin.x,target.y-origin.y);
  const advancing=faction==='player'&&owner&&(owner.kind==='boss'?owner.x>s.duel.rightTarget:owner.x>owner.stopX);
  const lead=advancing?dist/(speed+owner.speed):0;
  let tx=target.x-(advancing?owner.speed*lead:0);const ty=target.y;
  if(weapon==='mortar'){
   const face=faction==='player'?1:-1,r=RANGES.mortar,d=(tx-actor.x)*face;
   tx=actor.x+face*Math.max(r.min,Math.min(r.max,d));
  }
  const angle=Math.atan2(ty-origin.y,tx-origin.x);
  const b={faction,weapon,x:origin.x,y:origin.y,px:origin.x,py:origin.y,vx:Math.cos(angle)*speed,vy:Math.sin(angle)*speed,damage,age:0,angle,actorX:actor.x,maxRange:RANGES[weapon].max};
  if(weapon==='mortar'){Object.assign(b,{sx:origin.x,sy:origin.y,tx,ty,duration:1.35,arc:380,groundImpact:owner&&owner.kind!=='plane'});}
  s.bullets.push(b);fx(s,'muzzle_flash',origin.x,origin.y,weapon==='mg'?60:110);Object.assign(s.effects[s.effects.length-1],{angle,weapon,faction,actor});
  if(faction==='player')s.stats.shots[weapon]++;
 }
 function hitPlayer(s,amount,weapon) {
  s.player.hp=Math.max(0,s.player.hp-amount);s.player.hurt=.12;
  if(weapon==='cannon'&&!(s.bossMeleeStarted&&M.active(s))){s.player.x-=CANNON_PUSH;s.stats.playerKnockbacks++;s.bossContact=false;}
  if(s.player.hp<=0){s.phase='lost';s.bullets=[];fx(s,'explosion',s.player.x,930,s.player.w,'road');}
 }
 function step(s,dt) {
  if(s.phase==='won'||s.phase==='lost'||s.phase==='blocked'){dt=Math.max(0,Math.min(.05,dt));ageEffects(s,dt);M.update(s,dt,{damageEnemy,hitPlayer,fx});L.step(s,dt);return s;}
  dt=Math.max(0,Math.min(.05,dt));s.time+=dt;
  const crewHold=M.prepare(s,{inRange});
  const boss=s.enemies.find(e=>e.kind==='boss'&&!e.dead);
  let bossAdvance=0;
  if(boss){
   const d=s.duel;
   if(d.phase==='entry'){
    boss.x=Math.max(d.rightStart,boss.x-260*dt);
    if(boss.x===d.rightStart){d.phase='approach';s.alert=1.2;}
   }else{
    bossAdvance=crewHold?0:Math.min(d.speed*dt,Math.max(0,d.leftTarget-s.player.x));
    const enemyAdvance=crewHold&&s.bossMeleeStarted?0:Math.min(d.speed*dt,Math.max(0,boss.x-d.rightTarget));
    s.player.x+=bossAdvance;boss.x-=enemyAdvance;
    d.playerTravel+=bossAdvance;d.bossTravel+=enemyAdvance;
    const touching=boss.x-s.player.x-.52*(boss.w+s.player.w)<.001;
    if(touching&&!s.bossContact)s.stats.contactCount++;
    s.bossContact=touching;
   }
  }
  const targetSpeed=boss||crewHold?0:67;
  if(crewHold)s.travelSpeed=0;
  const acceleration=90*dt;
  s.travelSpeed+=Math.max(-acceleration,Math.min(acceleration,targetSpeed-s.travelSpeed));
  s.distance+=dt*s.travelSpeed;s.scroll.road=s.distance*1.2;s.scroll.sky=s.distance*.38;s.alert=Math.max(0,s.alert-dt);s.shake=Math.max(0,s.shake-dt*18);
  if(!boss&&s.player.x<s.player.goalX)s.player.x=Math.min(s.player.goalX,s.player.x+dt*s.travelSpeed*3);
  s.player.groundSpeed=boss?(dt?bossAdvance/dt:0):s.travelSpeed;
  s.player.wheelDistance+=s.player.groundSpeed*dt;s.player.hurt=Math.max(0,s.player.hurt-dt);
  ageEffects(s,dt);
  if(s.phase==='wave') {
   s.spawnTimer-=dt;
   if(s.spawnTimer<=0&&s.queueIndex<s.queue.length&&s.enemies.filter(e=>!e.dead).length<s.config.maxActive){spawn(s,s.queue[s.queueIndex++]);s.spawnTimer=s.config.interval;}
  }
  s.enemies.forEach(e=>{
   if(e.dead)return;e.t+=dt;e.hurt=Math.max(0,e.hurt-dt);
   if(e.kind==='dog'){M.stepDog(s,e,dt,{hitPlayer,fx});return;}
   if(e.kind!=='boss'&&e.x>e.stopX)e.x=Math.max(e.stopX,e.x-e.speed*dt);
   else if(e.kind==='plane')e.x=e.stopX+Math.sin(e.t*1.2)*50;
   if(e.kind==='plane')e.y=e.baseY+Math.sin(e.t*2.1+e.id)*17;
   if(e.x<1865){
    e.fire-=dt;e.mgFire-=dt;
    const target=center(s.player);
    if(e.kind==='boss'){
     if(s.duel.phase==='entry')return;
     if(e.fire<=0&&inRange(e,s.player,'cannon',-1)){fire(s,'enemy','cannon',muzzle(e,'cannon',-1,target),target,13,null,e);e.fire=2.1;s.stats.bossWeaponShots.cannon++;}
     if(e.weapons.includes('mg')&&e.mgFire<=0&&inRange(e,s.player,'mg',-1)){fire(s,'enemy','mg',muzzle(e,'mg',-1,target),target,1.3,null,e);e.mgFire=.28;s.stats.bossWeaponShots.mg++;}
    }else if(e.fire<=0&&inRange(e,s.player,'mg',-1)){fire(s,'enemy','mg',{x:e.x-(e.kind==='plane'?90:120),y:e.kind==='plane'?e.y+65:815},target,e.kind==='plane'?3:4,null,e);e.fire=e.kind==='plane'?2.2:2.8;}
   }
   if(e.kind!=='plane')s.fires.forEach(f=>{if(f.damage&&!e.dead&&Math.abs(e.x-f.x)<95)damageEnemy(s,e,dt*f.damage,'fire');});
  });
  for(const weapon of s.player.weapons){
   if((s.alliedCrew||[]).some(c=>c.role===weapon&&c.phase!=='aboard'))continue;
   s.player.cooldowns[weapon]-=dt;
   if(s.player.cooldowns[weapon]>0)continue;
   const target=choose(s,weapon);if(!target)continue;const c=center(target);
   s.player['aim_'+weapon]=c;
   const origin=muzzle(s.player,weapon,1,c);
   fire(s,'player',weapon,origin,c,weapon==='mg'?5:weapon==='mortar'?32:38,target,s.player);
   s.player.cooldowns[weapon]=weapon==='mg'?.16:weapon==='mortar'?2.4:.95;
  }
  for(const b of s.bullets){
   if(s.phase==='won'||s.phase==='lost')break;
   b.px=b.x;b.py=b.y;b.age+=dt;
   if(b.weapon==='mortar'){
    const u=Math.min(1,b.age/b.duration);b.x=b.sx+(b.tx-b.sx)*u;b.y=b.sy+(b.ty-b.sy)*u-4*b.arc*u*(1-u);b.angle=Math.atan2(b.y-b.py,b.x-b.px);
    if(u>=1){
     s.enemies.forEach(e=>{const c=center(e);if(!e.dead&&Math.hypot(c.x-b.tx,c.y-b.ty)<180)damageEnemy(s,e,b.damage,'mortar');});
     fx(s,'explosion',b.tx,b.groundImpact?930:b.ty+50,195,b.groundImpact?'road':'sky');if(b.groundImpact)groundFire(s,b.tx,'mortar');b.dead=true;
    }
   }else{
    b.x+=b.vx*dt;b.y+=b.vy*dt;
    if(b.faction==='player'){
     for(const e of s.enemies){
      if(e.dead)continue;const c=center(e);const rx=e.kind==='boss'?e.w*.3:e.kind==='plane'?105:e.kind==='dog'?39:108,ry=e.kind==='boss'?e.w*.34:e.kind==='plane'?68:e.kind==='dog'?65:77;
      if(Math.pow((b.x-c.x)/rx,2)+Math.pow((b.y-c.y)/ry,2)<1){damageEnemy(s,e,b.damage,b.weapon);fx(s,'impact_sparks',b.x,b.y,b.weapon==='mg'?65:105);b.dead=true;break;}
     }
    }else{
     const c=center(s.player);
     if(Math.pow((b.x-c.x)/(s.player.w*.3),2)+Math.pow((b.y-c.y)/(s.player.w*.34),2)<1){hitPlayer(s,b.damage,b.weapon);fx(s,'impact_sparks',b.x,b.y,65);b.dead=true;}
    }
   }
   if(b.age>3||b.x<-450||b.x>2350||b.y>1200||b.weapon!=='mortar'&&Math.abs(b.x-b.actorX)>b.maxRange+30)b.dead=true;
  }
  M.update(s,dt,{damageEnemy,hitPlayer,fx});
  s.bullets=s.bullets.filter(b=>!b.dead);s.enemies=s.enemies.filter(e=>!e.dead);L.step(s,dt);
  if(s.bossKilled&&!s.enemies.some(e=>!e.dead)&&!M.active(s)&&!s.loot.some(item=>item.boss)){s.victoryTravel+=dt;if(s.victoryTravel>=.65){s.phase='won';L.openChest(s);}}
  if(s.phase==='wave'&&s.normalKills===s.config.total&&s.queueIndex===s.queue.length&&s.enemies.length===0)spawn(s,'boss');
  if(s.phase==='boss'&&s.bossContact&&boss&&!M.active(s)&&!s.player.weapons.some(w=>inRange(s.player,boss,w,1))&&!boss.weapons.some(w=>inRange(boss,s.player,w,-1))&&!s.bullets.length&&!s.fires.some(f=>f.damage&&Math.abs(f.x-boss.x)<95)){
   s.rangeStall+=dt;if(s.rangeStall>=4)s.phase='blocked';
  }else s.rangeStall=0;
  return s;
 }
 const api={STAGES,TANK_WIDTH,CANNON_PUSH,DUEL_SPEED,RANGES,create,step,center,damageEnemy,spawn,muzzle,inRange,choose};
 if(typeof module!=='undefined'&&module.exports)module.exports=api;else host.TankBattleEngine=api;
})(globalThis);

