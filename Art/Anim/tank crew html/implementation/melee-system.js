(function(host){
 'use strict';
 const G=host.TankGeometry||(typeof module!=='undefined'?require('./tank-geometry.js'):null);
 const B=host.TankBossCrew||(typeof module!=='undefined'&&module.exports?require('./boss-crew-system.js'):null);
 const CORPSE_LIFE=7,GROUND=930,DETECT=750,ATTACK_TIME=1.25,HIT_TIME=.59;
 function slot(p){const z=p.w/1351,station=G.station(p,'front');return {x:p.x+(station.x-652)*z,y:GROUND+(station.y-1258)*z};}
 function pickup(p){return p.x+(G.profile(p).frontX-652)*p.w/1351+30;}
 function surface(e){return e.kind==='dog'?e.x:e.x-e.w*.36;}
 function threats(s){return s.enemies.filter(e=>!e.dead&&e.kind!=='plane'&&e.x>=s.player.x&&e.x-s.player.x<=DETECT&&(e.kind!=='boss'||s.bossContact||s.bossMeleeStarted));}
 function create(p){const q=slot(p);return {phase:'aboard',x:q.x,y:q.y,facing:1,motion:'idle',time:0,attackAge:-1,attackHit:false,targetID:null,hurtAge:99,deathAge:0,stats:{deployments:0,returns:0,attacks:0,hits:0}};}
 function jump(c,phase,to){c.phase=phase;c.time=0;c.fromX=c.x;c.fromY=c.y;c.toX=to.x;c.toY=to.y;c.motion='jump';c.facing=phase==='jump_out'?1:-1;c.attackAge=-1;}
 function prepare(s,api){const c=s.companion;if(!c)return false;c.stationScale=G.station(s.player,'front').scale;
  const boss=s.enemies.find(e=>e.kind==='boss'&&!e.dead);
  if(boss&&s.bossContact&&!boss.meleeDeployed){
   s.bossMeleeStarted=true;boss.meleeDeployed=true;const z=boss.w/1351;
   const dog={id:s.nextID++,kind:'dog',isBossCrew:true,ownerBossID:boss.id,x:boss.x-(835-652)*z,y:GROUND+(1023-1258)*z,w:140,t:0,speed:180,hp:s.config.index===1?220:180,maxHP:s.config.index===1?220:180,hurt:0,hurtAge:99,attackAge:-1,attackCD:0,attackHit:false,motion:'jump',facing:-1,dead:false,damage:s.config.index===1?7:6,phase:'jump_out',jumpAge:0,spriteScale:.49*z};
   dog.fromX=dog.x;dog.fromY=dog.y;dog.toX=boss.x-(1150-652)*z-30;dog.toY=GROUND;s.enemies.push(dog);s.stats.bossMeleeDeployments++;
  }
  if(c.phase==='aboard'){const q=slot(s.player);c.x=q.x;c.y=q.y;if(threats(s).length){jump(c,'jump_out',{x:pickup(s.player),y:GROUND});c.stats.deployments++;}}
  if(B)B.prepare(s,api);return active(s);
 }
 function age(s,dt){s.corpses=s.corpses.filter(e=>{e.deathAge+=dt;e.x=e.worldX-s.scroll.road;return e.deathAge<CORPSE_LIFE;});s.companion.hurtAge+=dt;if(B)B.age(s,dt);}
 function active(s){return !!s.companion&&s.companion.phase!=='aboard'||!!(B&&B.active(s));}
 function update(s,dt,api){const c=s.companion;if(!c)return;
  if(s.phase==='lost'){if(c.phase!=='aboard'){c.phase='dead';c.y=GROUND;c.deathAge+=dt;}return;}
  if(c.phase==='aboard'){const q=slot(s.player);c.x=q.x;c.y=q.y;return;}
  if(c.phase==='jump_out'||c.phase==='jump_in'){
   c.time+=dt;const u=Math.min(1,c.time/.6),to=c.phase==='jump_in'?slot(s.player):{x:c.toX,y:c.toY};
   c.x=c.fromX+(to.x-c.fromX)*u;c.y=c.fromY+(to.y-c.fromY)*u-4*118*u*(1-u);
   if(u>=1){if(c.phase==='jump_in'){c.phase='aboard';c.facing=1;c.motion='idle';c.stats.returns++;}else{c.phase='combat';c.motion='idle';c.y=GROUND;}}
   return;
  }
  if(c.attackAge>=0){
   const before=c.attackAge;c.attackAge+=dt;c.motion='attack';
   if(!c.attackHit&&before<HIT_TIME&&c.attackAge>=HIT_TIME){
    c.attackHit=true;let target=s.enemies.find(e=>e.id===c.targetID&&!e.dead);
    if(!target)target=threats(s).find(e=>Math.abs(surface(e)-c.x)<145);
    if(target&&Math.abs(surface(target)-c.x)<145){api.damageEnemy(s,target,36,'melee');api.fx(s,'impact_sparks',surface(target),GROUND-65,82);c.stats.hits++;}
   }
   if(c.attackAge<ATTACK_TIME)return;c.attackAge=-1;c.motion='idle';
  }
  const candidates=threats(s).sort((a,b)=>(a.kind==='dog'?0:1)-(b.kind==='dog'?0:1)||Math.abs(surface(a)-c.x)-Math.abs(surface(b)-c.x));
  const target=candidates[0];
  if(target){
   c.phase='combat';const dx=surface(target)-c.x;c.facing=dx>=0?1:-1;
   if(Math.abs(dx)>110){c.x+=Math.sign(dx)*Math.min(Math.abs(dx)-106,270*dt);c.motion='walk';}
   else{c.attackAge=0;c.attackHit=false;c.targetID=target.id;c.motion='attack';c.stats.attacks++;}
   c.y=GROUND;
  }else{
   c.phase='return';const dx=pickup(s.player)-c.x;c.facing=dx>=0?1:-1;c.motion='walk';
   if(Math.abs(dx)<=8)jump(c,'jump_in',slot(s.player));else c.x+=Math.sign(dx)*Math.min(Math.abs(dx),300*dt);
  }
 }
 function spawnDog(s,id,hard){const e={id,kind:'dog',x:2110+s.rng()*90,y:GROUND,w:140,t:0,speed:hard?166:150,hp:hard?92:76,maxHP:hard?92:76,hurt:0,hurtAge:99,attackAge:-1,attackCD:0,attackHit:false,motion:'walk',facing:-1,dead:false,damage:hard?4:3};s.enemies.push(e);s.spawned++;s.stats.spawnedDogs++;return e;}
 function stepDog(s,e,dt,api){
  if(e.phase==='jump_out'){
   e.jumpAge+=dt;const u=Math.min(1,e.jumpAge/(e.jumpDuration||.6));e.x=e.fromX+(e.toX-e.fromX)*u;e.y=e.fromY+((e.groundY||GROUND)-e.fromY)*u-4*118*u*(1-u);e.motion='jump';
   if(u>=1){e.phase='combat';e.y=e.groundY||GROUND;e.motion='idle';}return;
  }
  e.hurtAge+=dt;e.attackCD=Math.max(0,e.attackCD-dt);
  const c=(B&&B.target(s,e))||s.companion,groundBuddy=active(s)&&['combat','return'].includes(c.phase)&&Math.abs(c.y-(GROUND+(c.lane||0)*46))<22;
  const targetX=groundBuddy?c.x:s.player.x+(G.profile(s.player).frontX-49-652)*s.player.w/1351+15;
  const dx=targetX-e.x,reach=108+e.id%3*8;e.facing=dx>=0?1:-1;
  if(e.attackAge>=0){
   const before=e.attackAge;e.attackAge+=dt;e.motion='attack';
   if(!e.attackHit&&before<(e.hitTime||.47)&&e.attackAge>=(e.hitTime||.47)){e.attackHit=true;if(Math.abs(dx)<145){api.hitPlayer(s,e.damage,'dog');if(groundBuddy)c.hurtAge=0;api.fx(s,'impact_sparks',(e.x+targetX)/2,(e.y+c.y)/2-70,48);}}
   if(e.attackAge>=(e.attackDuration||.95)){e.attackAge=-1;e.motion='idle';}return;
  }
  if(Math.abs(dx)>reach+3){e.x+=Math.sign(dx)*Math.min(Math.abs(dx)-reach,e.speed*dt);e.motion='walk';}
  else{e.motion='idle';if(e.attackCD<=0){e.attackAge=0;e.attackCD=1.1;e.attackHit=false;e.motion='attack';}}
 }
 const updateAll=(s,dt,api)=>{update(s,dt,api);if(B)B.update(s,dt,api);};
 const api={CORPSE_LIFE,GROUND,DETECT,create,slot,pickup,surface,threats,prepare,age,active,update:updateAll,spawnDog,stepDog};host.TankMeleeSystem=api;
 if(typeof module!=='undefined'&&module.exports)module.exports=api;
})(globalThis);
