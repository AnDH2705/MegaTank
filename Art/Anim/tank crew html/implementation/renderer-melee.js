
(function(host){
 'use strict';
 const W=1920,H=1080;
 function create(ctx,images,tankMeta,enemyMeta){
  const tankLayers=tankMeta.layers.slice().sort((a,b)=>a.z_order-b.z_order),arcade=host.TankArcadeVFX.create(ctx,images);
  function bitmap(key,x,y,w,h){const im=images[key];if(im)ctx.drawImage(im,x,y,w,h);}
  function background(s){
   const im=images[s.config.background];if(!im)return;
   function strip(y,h,scroll){
    const offset=-(scroll%W);
    for(let i=-1;i<2;i++){
     const x=i*W+offset;
     ctx.save();ctx.translate(x,y);
     ctx.drawImage(im,0,y/H*im.height,im.width,h/H*im.height,0,0,W,h);ctx.restore();
    }
   }
   strip(0,818,s.scroll.sky);strip(818,H-818,s.scroll.road);
  }
  function actorBar(e,y,width=124){
   ctx.fillStyle='rgba(21,25,35,.7)';ctx.fillRect(e.x-width/2,y,width,9);
   ctx.fillStyle='#f1d000';ctx.fillRect(e.x-width/2,y,width*Math.max(0,e.hp/e.maxHP),9);
  }

  function companionPose(s,c=s.companion){const prefix=c.skin||'fat';
   if(c.phase==='dead')return [prefix+'_death',c.deathAge,false];
   if(c.phase==='jump_out'||c.phase==='jump_in')return [prefix+'_walk',.28,false];
   if(c.attackAge>=0)return [prefix+'_attack',c.attackAge,false];
   if(c.hurtAge<.22)return [prefix+'_hurt',c.hurtAge,false];
   return [prefix+(c.motion==='walk'?'_walk':'_ready'),s.time,true];
  }
  function dogPose(e){const prefix=e.skin||'dog';
   if(e.dead)return [prefix+'_death',e.deathAge,false];
   if(e.phase==='jump_out')return [prefix+'_walk',.28,false];
   if(e.hurtAge<.2)return [prefix+'_hurt',e.hurtAge,false];
   if(e.attackAge>=0)return [prefix+'_attack',e.attackAge,false];
   return [prefix+(e.motion==='walk'?'_walk':e.skin?'_ready':'_idle'),e.t,true];
  }
  function companionInside(s,c=s.companion){return c&&['jump_out','jump_in'].includes(c.phase)&&c.x<=s.player.x+(host.TankGeometry.profile(s.player).frontX-652)*s.player.w/1351+12;}
  function dogInside(e,s){const boss=e.isBossCrew&&s.enemies.find(b=>b.id===e.ownerBossID&&!b.dead);return !!boss&&e.phase==='jump_out'&&e.x>=boss.x-(host.TankGeometry.profile(boss).frontX-652)*boss.w/1351-12;}
  function figure(pose,x,y,size,face,alpha=1,rotation=0){if(host.TankCrew)host.TankCrew.figure(ctx,images,pose[0],pose[1],x,y,size,face,alpha,pose[2],rotation);}
  function companion(s,c=s.companion){if(!c||c.phase==='aboard'||companionInside(s))return;ctx.save();if(c.hurtAge<.08)ctx.filter='brightness(1.5)';figure(companionPose(s,c),c.x,c.y,(c.stationScale||.49)*s.player.w/1351,c.facing,1,c.motion==='jump'?.1*Math.sin(Math.PI*c.time/.6):0);ctx.restore();}
  function dog(e,s){if(dogInside(e,s))return;ctx.save();if(e.hurt>0)ctx.filter='brightness(1.6)';figure(dogPose(e),e.x,e.y,e.spriteScale||.22,e.facing,1);ctx.restore();actorBar(e,e.y-150,e.isBossCrew?88:68);}
  function corpse(e){figure([(e.skin||'dog')+'_death',e.deathAge,false],e.x,e.groundY||930,e.spriteScale||.22,e.facing,Math.min(1,(host.TankMeleeSystem.CORPSE_LIFE-e.deathAge)/.65));}

  function tank(actor,s,enemy){
   const skin=host.TankGeometry.profile(actor),scale=actor.w/1351,face=enemy?-1:1;
   const bob=!enemy&&s.phase!=='lost'?Math.sin(s.player.wheelDistance*.09)*1.6*(s.player.groundSpeed/67):0;
   ctx.save();ctx.translate(actor.x,930+bob);ctx.scale(face*scale,scale);ctx.translate(-652,-1258);
   if(actor.hurt>0)ctx.filter='brightness(1.55)';
   let crewDrawn=false;
   for(const part of skin.layers){
    if(!crewDrawn&&part.z_order>=7&&host.TankCrew){
     host.TankCrew.draw(ctx,images,actor,s,enemy);crewDrawn=true;
     if(!enemy){for(const c of [s.companion,...(s.alliedCrew||[])])if(companionInside(s,c))figure(companionPose(s,c),652+(c.x-actor.x)/scale,1258+(c.y-930-bob)/scale,c.stationScale||.49,c.facing,1,.1*Math.sin(Math.PI*c.time/(c.jumpDuration||.6)));}
     if(enemy){for(const e of s.enemies)if(e.ownerBossID===actor.id&&dogInside(e,s))figure(dogPose(e),652+(e.x-actor.x)/(face*scale),1258+(e.y-930)/scale,(e.spriteScale||.22)/scale,e.facing/face);}
    }
    const name=part.name,weapon=name.startsWith('06_')?'cannon':name.startsWith('08_')?'mg':name.startsWith('09_')||name.startsWith('10_')?'mortar':null;
    if(weapon&&!actor.weapons.includes(weapon))continue;
    const [x,y]=part.position_px,[w,h]=part.size_px,pivot=part.pivot_canvas_px||[x+w/2,y+h/2];
    ctx.save();
    if(/^0[2-5]_wheel/.test(name)){
     ctx.translate(pivot[0],pivot[1]);ctx.rotate((enemy?-actor.x:s.player.wheelDistance)/20);bitmap((skin.prefix||'')+name,x-pivot[0],y-pivot[1],w,h);
    }else if(weapon==='cannon'||weapon==='mg'){
     const target=enemy?host.TankBattleEngine.center(s.player):actor['aim_'+weapon];
     let angle=0;
     if(target){const wx=actor.x+face*(pivot[0]-652)*scale,wy=930+(pivot[1]-1258)*scale;angle=Math.atan2(target.y-wy,(target.x-wx)*face);angle=Math.max(-.85,Math.min(.55,angle));}
     ctx.translate(pivot[0],pivot[1]);ctx.rotate(angle);bitmap((skin.prefix||'')+name,x-pivot[0],y-pivot[1],w,h);
    }else bitmap((skin.prefix||'')+name,x,y,w,h);
    ctx.restore();
   }
   ctx.restore();
  }
  function vehicle(e,s){
   const plane=e.kind==='plane',kind=plane?'plane':'car',scale=e.w/1536;
   ctx.save();ctx.translate(e.x,plane?e.y:930);ctx.scale(-scale,scale);ctx.translate(-768,plane?-550:-1002);
   if(e.hurt>0)ctx.filter='brightness(1.65)';
   for(const part of enemyMeta[kind].layers){
    const [x,y]=part.position_px,[w,h]=part.size_px,p=part.pivot_canvas_px;
    ctx.save();
    if(part.name.includes('wheel')){ctx.translate(p[0],p[1]);ctx.rotate(plane?e.t*2:-e.x/26);bitmap(kind+'_'+part.name,x-p[0],y-p[1],w,h);}
    else if(part.name.includes('propeller')){ctx.translate(p[0],p[1]);ctx.scale(1,Math.cos(e.t*25));bitmap(kind+'_'+part.name,x-p[0],y-p[1],w,h);}
    else bitmap(kind+'_'+part.name,x,y,w,h);
    ctx.restore();
   }
   ctx.restore();actorBar(e,plane?e.y-127:707);
  }
  function sheet(key,index,cols,rows,x,y,size,pivot,angle){
   const im=images[key];if(!im)return;const sw=im.width/cols,sh=im.height/rows;
   ctx.save();ctx.translate(x,y);if(angle)ctx.rotate(angle);
   ctx.drawImage(im,(index%cols)*sw,Math.floor(index/cols)*sh,sw,sh,-size*pivot[0],-size*(1-pivot[1]),size,size);ctx.restore();
  }
  function render(s){
   ctx.clearRect(0,0,W,H);ctx.fillStyle='#303745';ctx.fillRect(0,0,W,H);if(host.TankCrew)host.TankCrew.lastBossRoles=[];
   arcade.sync(s);const kick=arcade.camera(s);ctx.save();ctx.translate(kick.x,kick.y);
   background(s);arcade.particles(s,true);
   for(const f of s.fires)sheet('vfx_ground_fire',Math.floor(f.age*8)%8,4,2,f.x,930,175,[.5,.125],0);
   for(const e of s.corpses||[])corpse(e);
   for(const e of s.enemies)if(!e.dead){if(e.kind==='boss')tank(e,s,true);else if(e.kind!=='dog')vehicle(e,s);}
   tank(s.player,s,false);
   const fighters=[...s.enemies.filter(e=>!e.dead&&e.kind==='dog').map(e=>({e,enemy:true})),...[s.companion,...(s.alliedCrew||[])].filter(c=>c.phase!=='aboard').map(e=>({e,enemy:false}))].sort((a,b)=>a.e.y-b.e.y);
   for(const f of fighters)if(f.enemy)dog(f.e,s);else companion(s,f.e);
   host.TankLootSystem.draw(ctx,s);
   arcade.particles(s);
   for(const b of s.bullets)arcade.projectile(b,s);
   arcade.effects(s);
   ctx.restore();
  }
  return {render,tank,vehicle};
 }
 if(typeof module!=='undefined'&&module.exports)module.exports={create};else host.TankBattleRenderer={create};
})(globalThis);

