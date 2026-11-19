(function(host){
 'use strict';
 const G=host.TankGeometry||(typeof module!=='undefined'?require('./tank-geometry.js'):null);
 const GROUND=930;
 const positions={mg:{x:670,y:244,scale:.52,weapon:'mg',seated:true},cannon:{x:735,y:614,scale:.53,weapon:'cannon',seated:true},mortar:{x:386,y:661,scale:.49,weapon:'mortar'},driver:{x:494,y:974,scale:.52,seated:true}};
 const lanes={cannon:-1,driver:1,mortar:2,mg:-2};
 function slot(p,c){const station={...positions[c.role],...G.station(p,c.role)},z=p.w/1351;return {x:p.x+(station.x-652)*z,y:GROUND+(station.y+(station.seated?station.scale*100:0)-1258)*z};}
 function pickup(p,c){return p.x+(G.profile(p).frontX-652)*p.w/1351+30+(c?c.lane*5:0);}
 function ground(c){return GROUND+(c.lane||0)*46;}
 function beginJump(c,phase,to){c.phase=phase;c.time=0;c.fromX=c.x;c.fromY=c.y;c.toX=to.x;c.toY=to.y;c.motion='jump';c.facing=phase==='jump_out'?1:-1;c.attackAge=-1;}
 function usable(actor,target,weapon,face,api){return actor.weapons.includes(weapon)&&api.inRange(actor,target,weapon,face)&&(actor.kind==='boss'||!api.canFire||api.canFire(weapon));}
 function prepare(s,api){
  s.alliedCrew||=[];const boss=s.enemies.find(e=>e.kind==='boss'&&!e.dead);if(!boss||!s.bossContact||s.bossExtraCrewDeployed)return;
  s.bossExtraCrewDeployed=true;boss.deployedRoles={front:true};
  for(const [role,template]of Object.entries(positions)){const p={...template,...G.station(s.player,role)};
   if(p.weapon&&usable(s.player,boss,p.weapon,1,api))continue;
   const c={id:'ally-'+role,role,skin:'cat',stationScale:p.scale,lane:lanes[role],phase:'aboard',x:0,y:0,time:0,motion:'idle',facing:1,attackAge:-1,attackHit:false,hurtAge:99,deathAge:0,jumpDuration:p.y<800?.85:.65,stats:{attacks:0,hits:0,returns:0}};
   Object.assign(c,slot(s.player,c));beginJump(c,'jump_out',{x:pickup(s.player,c),y:ground(c)});s.alliedCrew.push(c);
  }
  for(const role of ['cannon','driver','mg']){
   const p=positions[role];if(p.weapon&&!boss.weapons.includes(p.weapon))continue;if(p.weapon&&usable(boss,s.player,p.weapon,-1,api))continue;
   const z=boss.w/1351,hard=s.config.index===1,hp=role==='driver'?(hard?110:90):(hard?130:110),lane=lanes[role];
   const dog={id:s.nextID++,kind:'dog',crewRole:role,skin:role==='cannon'?'pilot':'jeep',isBossCrew:true,ownerBossID:boss.id,x:boss.x-(p.x-652)*z,y:GROUND+(p.y+(p.seated?p.scale*100:0)-1258)*z,w:140,t:0,speed:180,hp,maxHP:hp,hurt:0,hurtAge:99,attackAge:-1,attackCD:0,attackHit:false,motion:'jump',facing:-1,dead:false,damage:hard?5:4,phase:'jump_out',jumpAge:0,jumpDuration:p.y<800?.85:.65,groundY:GROUND+lane*46,lane,spriteScale:p.scale*z,attackDuration:1.25,hitTime:.59};
   dog.fromX=dog.x;dog.fromY=dog.y;dog.toX=boss.x-(1150-652)*z-30-lane*5;dog.toY=dog.groundY;s.enemies.push(dog);boss.deployedRoles[role]=true;s.stats.bossMeleeDeployments++;
  }
 }
 function active(s){return (s.alliedCrew||[]).some(c=>c.phase!=='aboard');}
 function friends(s){return [s.companion,...(s.alliedCrew||[])].filter(Boolean);}
 function target(s,e){return friends(s).filter(c=>['combat','return'].includes(c.phase)&&Math.abs(c.y-ground(c))<22).sort((a,b)=>Math.abs((a.lane||0)-(e.lane||0))*90+Math.abs(a.x-e.x)-Math.abs((b.lane||0)-(e.lane||0))*90-Math.abs(b.x-e.x))[0];}
 function age(s,dt){for(const c of s.alliedCrew||[])c.hurtAge+=dt;}
 function update(s,dt,api){
  for(const c of s.alliedCrew||[]){
   if(s.phase==='lost'){if(c.phase!=='aboard'){c.phase='dead';c.y=ground(c);c.deathAge+=dt;}continue;}
   if(c.phase==='aboard'){Object.assign(c,slot(s.player,c));continue;}
   if(c.phase==='jump_out'||c.phase==='jump_in'){
    c.time+=dt;const u=Math.min(1,c.time/c.jumpDuration),to=c.phase==='jump_in'?slot(s.player,c):{x:c.toX,y:c.toY};c.x=c.fromX+(to.x-c.fromX)*u;c.y=c.fromY+(to.y-c.fromY)*u-4*95*u*(1-u);
    if(u===1){if(c.phase==='jump_in'){c.phase='aboard';c.motion='idle';c.facing=1;c.stats.returns++;}else{c.phase='combat';c.motion='idle';}}continue;
   }
   const reach=112+Math.abs(c.lane)*16;
   if(c.attackAge>=0){
    const before=c.attackAge;c.attackAge+=dt;c.motion='attack';
    if(!c.attackHit&&before<.59&&c.attackAge>=.59){c.attackHit=true;const e=s.enemies.find(e=>e.id===c.targetID&&!e.dead);if(e&&Math.abs((e.kind==='dog'?e.x:e.x-e.w*.36)-c.x)<reach+48){api.damageEnemy(s,e,24,'melee');api.fx(s,'impact_sparks',e.x,c.y-67,65);c.stats.hits++;}}
    if(c.attackAge<1.25)continue;c.attackAge=-1;
   }
   const candidates=s.enemies.filter(e=>!e.dead&&e.kind!=='plane'&&e.x-s.player.x<=750&&e.x>=s.player.x).sort((a,b)=>(a.kind==='dog'?0:1000)+(Math.abs(c.lane-(a.lane||0))*75)+Math.abs(a.x-c.x)-(b.kind==='dog'?0:1000)-(Math.abs(c.lane-(b.lane||0))*75)-Math.abs(b.x-c.x));
   const e=candidates[0];
   if(e){const dx=(e.kind==='dog'?e.x:e.x-e.w*.36)-c.x;c.phase='combat';c.facing=dx>=0?1:-1;c.y=ground(c);if(Math.abs(dx)>reach){c.x+=Math.sign(dx)*Math.min(Math.abs(dx)-reach+2,250*dt);c.motion='walk';}else{c.attackAge=0;c.attackHit=false;c.targetID=e.id;c.motion='attack';c.stats.attacks++;}}
   else{c.phase='return';const dx=pickup(s.player,c)-c.x;c.facing=dx>=0?1:-1;c.motion='walk';if(Math.abs(dx)<=8)beginJump(c,'jump_in',slot(s.player,c));else c.x+=Math.sign(dx)*Math.min(Math.abs(dx),290*dt);}
  }
 }
 const api={positions,slot,pickup,prepare,active,friends,target,age,update};host.TankBossCrew=api;if(typeof module!=='undefined'&&module.exports)module.exports=api;
})(globalThis);
