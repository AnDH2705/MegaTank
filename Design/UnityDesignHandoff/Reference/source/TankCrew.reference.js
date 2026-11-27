// HTML visual implementation reference. Port to C#; do not add a JavaScript runtime.
(function (host) {
 'use strict';
 const model=JSON.parse(document.getElementById('tank-crew-data').textContent);
 function selected(station,state,actor,enemy){
  let name=station.pose,time=(state.time||0)+(station.phase||0);
  if(station.weapon&&actor&&actor.weapons.includes(station.weapon)&&(enemy||actor['aim_'+station.weapon])){
   const period=enemy?(station.weapon==='mg'?.28:2.1):(station.weapon==='mg'?.16:.95),remaining=enemy?(station.weapon==='mg'?actor.mgFire:actor.fire):(actor.cooldowns[station.weapon]||0),elapsed=period-remaining;
   if(elapsed>=0&&elapsed<.27){name=station.firePose||'driver_fire';time=elapsed;}
  }
  const clip=model.clips[name],index=Math.floor(((time%clip.duration+clip.duration)%clip.duration)/clip.duration*clip.frames);
  return {clip,index,name};
 }
 function geometry(station,clip){
  const scale=station.scale/clip.worldScale;
  return {x:station.x-clip.pivot[0]*scale,y:station.y-clip.pivot[1]*scale,w:clip.cell[0]*scale,h:clip.cell[1]*scale};
 }
 const api={model,lastCanvasRoles:[],lastBossRoles:[]};
 api.figure=function(ctx,images,name,time,x,y,scale,facing=1,opacity=1,loop=true,rotation=0){
  const clip=model.clips[name],image=clip&&images[clip.key];if(!image)return false;
  const progress=loop?((time%clip.duration+clip.duration)%clip.duration)/clip.duration:Math.max(0,Math.min(1,time/clip.duration));
  const index=Math.min(clip.frames-1,Math.floor(progress*clip.frames)),sw=image.width/clip.cols,sh=image.height/clip.rows,factor=scale/clip.worldScale;
  ctx.save();ctx.translate(x,y);ctx.scale(facing,1);if(rotation)ctx.rotate(rotation);ctx.globalAlpha*=opacity;
  ctx.drawImage(image,index%clip.cols*sw,Math.floor(index/clip.cols)*sh,sw,sh,-clip.pivot[0]*factor,-clip.pivot[1]*factor,clip.cell[0]*factor,clip.cell[1]*factor);ctx.restore();return true;
 };
 api.draw=function(ctx,images,actor,state,enemy){
  const drawn=[];
  const stations=enemy?model.bossStations:model.stations;
  for(const original of stations){const station={...original,...host.TankGeometry.station(actor,original.id)};
   if(station.requires&&!actor.weapons.includes(station.requires))continue;if(!enemy&&(state.crewAssignments?.[station.id==='front'?'melee':station.id]===null||!host.TankProgress.unlocked(station.id==='front'?'melee':station.id,state)))continue;
   if(enemy?actor.deployedRoles&&actor.deployedRoles[station.id]:(state.alliedCrew||[]).some(c=>c.role===station.id&&c.phase!=='aboard'))continue;
   if(station.id==='front'&&(enemy?actor.meleeDeployed:state.companion&&state.companion.phase!=='aboard'))continue;
   const pose=selected(station,state,actor,enemy),clip=pose.clip,image=images[clip.key];if(!image)continue;
   const g=geometry(station,clip),sw=image.width/clip.cols,sh=image.height/clip.rows;
   ctx.save();if(station.clip){ctx.beginPath();ctx.rect(...station.clip);ctx.clip();}
   ctx.drawImage(image,pose.index%clip.cols*sw,Math.floor(pose.index/clip.cols)*sh,sw,sh,g.x,g.y,g.w,g.h);ctx.restore();
   drawn.push({id:station.id,pose:pose.name,frame:pose.index});
  }
  if(enemy)api.lastBossRoles=drawn;else api.lastCanvasRoles=drawn;
 };
 api.mountEquipment=function(tank,assets){
  const items=model.stations.map(station=>{
   const clip=model.clips[station.pose],g=geometry(station,clip),node=document.createElement('span');
   node.className='eq-crew-station';node.dataset.crewRole=station.id;node.setAttribute('role','img');node.setAttribute('aria-label',station.label);
   Object.assign(node.style,{position:'absolute',pointerEvents:'none',left:g.x/1351*100+'%',top:g.y/1277*100+'%',width:g.w/1351*100+'%',height:g.h/1277*100+'%',zIndex:'65',backgroundImage:'url("'+assets[clip.key]+'")',backgroundRepeat:'no-repeat',backgroundSize:clip.cols*100+'% '+clip.rows*100+'%'});
   if(station.clip){const [x,y,w,h]=station.clip,inset=[Math.max(0,y-g.y)/g.h,Math.max(0,g.x+g.w-x-w)/g.w,Math.max(0,g.y+g.h-y-h)/g.h,Math.max(0,x-g.x)/g.w];node.style.clipPath='inset('+inset.map(n=>n*100+'%').join(' ')+')';}
   tank.appendChild(node);return {station,node};
  });
  let request=0;
  function render(time){for(const item of items){item.node.hidden=!(globalThis.TankProgress?.unlocked(item.station.id==='front'?'melee':item.station.id)??true);const {clip,index}=selected(item.station,{time},null),x=index%clip.cols,y=Math.floor(index/clip.cols);item.node.style.backgroundPosition=(clip.cols>1?x/(clip.cols-1)*100:0)+'% '+(clip.rows>1?y/(clip.rows-1)*100:0)+'%';}}
  function stop(){if(request)cancelAnimationFrame(request);request=0;}
  function start(){stop();function tick(now){if(tank.closest('[data-equipment-view]').hidden){request=0;return;}render(now/1000);request=requestAnimationFrame(tick);}render(performance.now()/1000);request=requestAnimationFrame(tick);}
  render(0);return {start,stop,render};
 };
 host.TankCrew=api;
})(globalThis);
