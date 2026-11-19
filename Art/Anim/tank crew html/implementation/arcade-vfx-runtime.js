(function(host){
 'use strict';
 const model=JSON.parse(document.getElementById('tank-arcade-vfx-data').textContent),scenes=new WeakMap();
 const random=n=>{const v=Math.sin(n*127.1+311.7)*43758.5453;return v-Math.floor(v);};
 const clamp=(x,a,b)=>Math.max(a,Math.min(b,x));
 const api={model,lastStats:{}};
 function stateFor(s){
  let v=scenes.get(s);if(!v){v={now:s.time,terminalTime:0,lastClock:performance.now(),seen:new WeakSet(),bullets:new WeakMap(),traces:[],bursts:[],particles:[],impulses:[],seq:0,prevPhase:s.companion&&s.companion.phase,lastDust:-99,stats:{explosions:0,cannonShots:0,cannonMuzzles:0,mortarShots:0,muzzles:0,hits:0,smokeEmitted:0,shakes:0}};scenes.set(s,v);}return v;
 }
 function particle(v,s,type,x,y,size,life,delay=0,vx=0,vy=-25,anchor='road',angle=0){
  v.particles.push({type,worldX:x+s.scroll[anchor],y,size,life,start:v.now+delay,vx,vy,anchor,angle,seed:v.seq++});
  if(type==='Smoke')v.stats.smokeEmitted++;if(v.particles.length>160)v.particles.splice(0,v.particles.length-160);
 }
 function impulse(v,start,strength,life){v.impulses.push({start,strength,life,seed:v.seq++});v.stats.shakes++;}
 function explosion(v,s,e,x,y,size,delay=0){
  const start=v.now-e.age+delay,anchor=e.anchor||'road';
  v.bursts.push({type:'explosion',worldX:x+s.scroll[anchor],y,size,start,life:1.08,anchor});v.stats.explosions++;
  particle(v,s,'Smoke',x,y-size*.04,size*.94,.85,delay+.12-e.age,0,-35,anchor);
  const count=size>280?26:18;
  for(let i=0;i<count;i++){
   const seed=++v.seq,a=random(seed)*Math.PI*2,speed=size*(.55+random(seed+12)*.85),life=.18+random(seed+51)*.3;
   particle(v,s,'Spark',x,y,10+random(seed+21)*16,life,delay+.025-e.age,Math.cos(a)*speed,Math.sin(a)*speed,anchor,a);
  }
 }
 function sync(s){
  const v=stateFor(s),clock=performance.now(),terminal=['won','lost','blocked'].includes(s.phase);
  if(terminal)v.terminalTime+=clamp((clock-v.lastClock)/1000,0,.05);else v.terminalTime=0;
  v.lastClock=clock;v.now=s.time+v.terminalTime;
  for(const e of s.effects){
   if(v.seen.has(e))continue;v.seen.add(e);const anchor=e.anchor||'road',start=v.now-e.age;
   if(e.type==='explosion'){
    const y=e.y>900?e.y-e.size*.3:e.y;
    if(e.size>=450){
     for(const [dx,dy,k,delay]of [[-.2,.02,.66,0],[.12,-.17,.72,.08],[.31,.01,.59,.17],[-.05,-.32,.62,.25]])explosion(v,s,e,e.x+e.size*dx,y+e.size*dy,e.size*k,delay);
    }else explosion(v,s,e,e.x,y,e.size);
    impulse(v,start,e.size>=450?14:clamp(e.size*.033,5,9),e.size>=450?.34:.23);
   }else if(e.type==='muzzle_flash'){
    const cannon=e.weapon==='cannon';v.bursts.push({type:cannon?'cannon_muzzle':'muzzle',worldX:e.x+s.scroll.road,y:e.y,size:e.size,angle:e.angle||0,start,life:cannon?.26:.12,anchor:'road',actor:e.actor,faction:e.faction});v.stats.muzzles++;
    if(cannon){
     v.stats.cannonMuzzles++;
     for(let i=0;i<4;i++){const d=12+i*24,angle=(e.angle||0)+(random(++v.seq)-.5)*.4;particle(v,s,'Smoke',e.x+Math.cos(angle)*d,e.y+Math.sin(angle)*d,58+i*8,.64,.02+i*.025-e.age,Math.cos(angle)*10,-22,'road');}
     for(let i=0;i<6;i++){const angle=(e.angle||0)+(random(++v.seq)-.5)*.85,speed=190+random(v.seq+18)*160;particle(v,s,'Spark',e.x,e.y,10+random(v.seq+9)*8,.19,-e.age,Math.cos(angle)*speed,Math.sin(angle)*speed,'road',angle);}
    }
    if(e.size>=100){impulse(v,start,5,.14);if(!cannon)particle(v,s,'Smoke',e.x,e.y,48,.42,-e.age,0,-18);}
   }else{
    v.bursts.push({type:'hit',worldX:e.x+s.scroll[anchor],y:e.y,size:e.size,start,life:.17,anchor});v.stats.hits++;
    for(let i=0;i<4;i++){const seed=++v.seq,a=random(seed)*Math.PI*2;particle(v,s,'Spark',e.x,e.y,9+random(seed+13)*9,.22,-e.age,Math.cos(a)*110,Math.sin(a)*110,anchor,a);}
   }
  }
  for(const b of s.bullets){
   if(b.weapon==='mg')continue;
   if(b.weapon==='cannon'){if(!v.bullets.has(b)){v.bullets.set(b,true);v.stats.cannonShots++;}continue;}
   let trail=v.bullets.get(b);
   if(!trail){trail={weapon:b.weapon,points:[],lastX:b.px,lastY:b.py,lastSmokeX:b.px,lastSmokeY:b.py,lastSeen:v.now};v.bullets.set(b,trail);v.traces.push(trail);v.stats.mortarShots++;}
   const moved=Math.hypot(b.x-trail.lastX,b.y-trail.lastY);
   if(moved>.5){trail.points.push({worldX:b.x+s.scroll.road,y:b.y,time:v.now});trail.lastX=b.x;trail.lastY=b.y;}
   const distance=Math.hypot(b.x-trail.lastSmokeX,b.y-trail.lastSmokeY),spacing=57;
   if(distance>=spacing){
    const count=Math.min(5,Math.floor(distance/spacing)),dx=b.x-trail.lastSmokeX,dy=b.y-trail.lastSmokeY;
    for(let i=1;i<=count;i++){const u=i/count;particle(v,s,'Smoke',trail.lastSmokeX+dx*u,trail.lastSmokeY+dy*u,35,.55,0,0,-19,'road',random(v.seq)*.4-.2);}
    trail.lastSmokeX=b.x;trail.lastSmokeY=b.y;
   }
   trail.lastSeen=v.now;trail.points=trail.points.filter(p=>v.now-p.time<.15).slice(-14);
  }
  const c=s.companion;
  if(c&&v.prevPhase==='jump_out'&&c.phase==='combat')particle(v,s,'Dust',c.x,925,100,.46,0,0,-10);
  v.prevPhase=c&&c.phase;
  if(s.player.groundSpeed>5&&v.now-v.lastDust>.19){const wheel=host.TankGeometry.profile(s.player).wheelY;particle(v,s,'Dust',s.player.x-s.player.w*.28,930+(wheel-1258)*s.player.w/1351,155,.55,0,-8,-5);v.particles[v.particles.length-1].overlay=true;v.lastDust=v.now;}
  v.particles=v.particles.filter(p=>v.now-p.start<p.life);v.bursts=v.bursts.filter(e=>v.now-e.start<e.life);v.traces=v.traces.filter(t=>v.now-t.lastSeen<.15);v.impulses=v.impulses.filter(k=>v.now-k.start<k.life);
  api.lastStats={...v.stats,activeParticles:v.particles.length,activeBursts:v.bursts.length,activeTraces:v.traces.length,cannonProjectileTrails:0,activeCannonMuzzles:v.bursts.filter(e=>e.type==='cannon_muzzle'&&v.now>=e.start&&v.now-e.start<e.life).length};return v;
 }
 function create(ctx,images){
  function sprite(name,time,x,y,w,h=w,angle=0,alpha=1,pivot=[.5,.5],loop=false,blend){
   const clip=model.clips[name],im=clip&&images[clip.key];if(!im||alpha<=0||time<0)return;
   let frame=Math.floor(time*clip.fps);if(loop)frame%=clip.count;else if(frame>=clip.count&&clip.count>1)return;
   frame=clamp(frame,0,clip.count-1);const sw=im.width/clip.cols,sh=im.height/clip.rows;
   ctx.save();ctx.globalAlpha*=clamp(alpha,0,1);ctx.globalCompositeOperation=blend||(clip.blend==='Additive'?'lighter':'source-over');ctx.translate(x,y);if(angle)ctx.rotate(angle);
   ctx.drawImage(im,frame%clip.cols*sw,Math.floor(frame/clip.cols)*sh,sw,sh,-w*pivot[0],-h*pivot[1],w,h);ctx.restore();
  }
  function camera(s){
   const v=stateFor(s);let x=0,y=0,strength=0;
   for(const kick of v.impulses){const age=v.now-kick.start;if(age<0)continue;const power=kick.strength*Math.pow(1-age/kick.life,2);x+=Math.cos(age*113+kick.seed)*power;y+=Math.sin(age*137+kick.seed*1.7)*power*.62;strength+=power;}
   if(s.shake>0){x+=Math.sin(s.time*83)*s.shake*.4;y+=Math.cos(s.time*71)*s.shake*.18;}
   api.lastStats.cameraStrength=strength;return {x:clamp(x,-16,16),y:clamp(y,-11,11)};
  }
  function particles(s,groundOnly=false){
   const v=stateFor(s);
   for(const p of v.particles){
    if(groundOnly?(p.type!=='Dust'||p.overlay):(p.type==='Dust'&&!p.overlay))continue;const age=v.now-p.start;if(age<0)continue;const u=age/p.life,x=p.worldX-s.scroll[p.anchor]+p.vx*age,y=p.y+p.vy*age+(p.type==='Spark'?240*age*age:0);
    if(p.type==='Spark')sprite('Spark',0,x,y,p.size*(1-u*.75),p.size*(1-u*.75),p.angle,Math.pow(1-u,1.1));
    else if(p.type==='Smoke'){const size=p.size*(.65+u*.6),alpha=.73*(u<.6?1:(1-u)/.4);sprite('Smoke',u*(7/12),x,y,size,size,p.angle,alpha);}
    else sprite('Dust',0,x,y,p.size*(.8+u*.55),p.size*(.6+u*.45),0,(1-u)*.42,[.5,.5]);
   }
  }
  function projectile(b,s){
   ctx.save();if(b.ammoType==='plasma')ctx.filter='hue-rotate(140deg) saturate(1.35)';
   if(b.weapon==='cannon'){
    const im=images.ammo_cannon;if(im){const w=78,h=w*im.height/im.width;ctx.save();ctx.translate(b.x,b.y);ctx.rotate(b.angle);ctx.drawImage(im,-w*.5,-h*.5,w,h);ctx.restore();}
   }else if(b.weapon==='mg'){
    sprite('Glow',0,b.x,b.y,30,30,0,.15);sprite('BeamTrail',0,b.x,b.y,92,11,b.angle,1,[.95,.5]);
   }else{
    sprite('Glow',0,b.x,b.y,88,88,0,.22);
    const im=images.ammo_mortar;if(im){const w=92,h=w*im.height/im.width;ctx.save();ctx.translate(b.x,b.y);ctx.rotate(b.angle);ctx.drawImage(im,-w*.5,-h*.5,w,h);ctx.restore();}
   }
   ctx.restore();
  }
  function effects(s){
   const v=stateFor(s);
   for(const e of v.bursts){
    const age=v.now-e.start;if(age<0)continue;const x=e.worldX-s.scroll[e.anchor],y=e.y;
    if(e.type==='explosion'){
     if(age<.23)sprite('Glow',0,x,y,e.size*1.4,e.size*1.4,0,.5*(1-age/.23));
     sprite('Explosion',age,x,y,e.size,e.size,0,age<.38?1:Math.max(0,(.48-age)/.1),[.5,.5],false,'source-over');
     if(age>.025&&age<.285)sprite('Shockwave',age-.025,x,y,e.size*1.22,e.size*1.22,0,.7*(1-(age-.025)/.26));
    }else if(e.type==='cannon_muzzle'){
     const actor=e.actor,face=e.faction==='enemy'?-1:1;
     let mx=x,my=y,angle=e.angle;
     if(actor){const target=face===1?actor.aim_cannon:host.TankGeometry.center(s.player);if(target){const mouth=host.TankGeometry.muzzle(actor,'cannon',face,target);mx=mouth.x;my=mouth.y;angle=mouth.angle;}}
     const u=age/.26,grow=age<.035?.55+.45*age/.035:1-.65*(age-.035)/.225,alpha=clamp(1-u*u,0,1);
     sprite('Glow',0,mx+Math.cos(angle)*24,my+Math.sin(angle)*24,170,170,0,.3*alpha);
     sprite('Muzzle',age,mx,my,150*grow,125*grow,angle,alpha,[.12,.55],true);
     sprite('FireBoltHead',age,mx,my,275*grow,165*grow,angle,alpha,[.12,.5],true);
    }else if(e.type==='muzzle'){
     sprite('Muzzle',age,x,y,e.size,e.size,e.angle,1,[.12,.55]);if(age<.07)sprite('Glow',0,x,y,e.size*.7,e.size*.7,0,.22*(1-age/.07));
    }else sprite('Hit',age,x,y,e.size,e.size,0,1);
   }
  }
  return {sync,camera,particles,projectile,effects};
 }
 api.create=create;host.TankArcadeVFX=api;
})(globalThis);
