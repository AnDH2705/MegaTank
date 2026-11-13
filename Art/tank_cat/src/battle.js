import { TURRETS, HANDHELD, PACKS, MONSTERS, MONSTER_BALANCE, WAVES, SKILLS } from './data.js';
const clamp=(v,a,b)=>Math.max(a,Math.min(b,v));
const dist=(a,b)=>Math.hypot(a.x-b.x,a.y-b.y);
const ENERGY_KINDS=new Set(['laser','plasma','tesla','ice','gravity']);
export class Battle {
 constructor(onEvent=()=>{}, random=Math.random) {
  this.emit=onEvent;this.random=random;this.time=0;this.state='ready';this.hp=48000;this.maxHp=48000;this.energy=600;this.coins=0;this.kills=0;
  this.player={x:290,y:175};this.wave=0;this.spawned=0;this.spawnTimer=.8;this.waveGap=0;this.enemies=[];this.bullets=[];this.units=[];this.nextId=1;
  this.cooldowns={};this.buffs={shield:0,rally:0,drone:0,freeze:0,slow:0,frost:0,poison:0};this.targetId=null;this.move=0;this.fired=new Set();this.seen=new Set();this.bank=-1;
  for(let i=0;i<5;i++)this.units.push({id:`tower-${i}`,role:'tower',index:i,x:290,y:258+i*80,timer:.15+i*.24,angle:0,recoil:0});
  for(let i=0;i<10;i++)this.units.push({id:`cat-${i}`,role:'infantry',index:i,x:110+i*35,y:196,timer:.5+i*.15,angle:0,recoil:0});
  for(let i=0;i<4;i++)this.units.push({id:`aux-${i}`,role:'aux',index:i,x:240,y:200,timer:1+i*.3,angle:0,recoil:0});
  this.assignWeapons();
 }
 assignWeapons() {
  const bank=Math.floor(this.time/10)%10; if(bank===this.bank)return;
  this.bank=bank;
  for(const u of this.units){
   if(u.role==='tower')u.weapon=TURRETS[(u.index+bank*5)%10];
   else if(u.role==='infantry')u.weapon=HANDHELD[u.index];
   else u.weapon=PACKS[(bank*4+u.index)%20];
  }
  this.emit({type:'bank',bank});
 }
 start(){this.state='playing';this.emit({type:'wave',wave:0});}
 spawn(type,x=1590+this.random()*100,extra={}) {
  const def=MONSTERS[type]; const e={id:this.nextId++,type,def,x,y:175+def.size*.43+(def.flying?200:0),hp:def.hp,maxHp:def.hp,size:def.size,phase:this.random()*6.28,age:0,attack:1.6+this.random()*2,slow:0,burn:0,poison:0,stun:0,flash:0,summon:9,dead:false,...extra};
  this.enemies.push(e);this.seen.add(type);this.emit({type:'spawn',enemy:e});return e;
 }
 chooseTarget(unit) {
  let options=this.enemies.filter(e=>!e.dead && e.x>-100 && e.x<1720 && dist(unit,e)<unit.weapon.range);
  const locked=options.find(e=>e.id===this.targetId);if(locked)return locked;
  options.sort((a,b)=>{
   if(unit.weapon.kind==='sniper')return (b.def.boss?900:0)+(b.def.armor?200:0)-(a.def.boss?900:0)-(a.def.armor?200:0) || a.hp-b.hp;
   if(unit.role==='infantry'&&unit.index%3===1 && a.def.flying!==b.def.flying)return a.def.flying?-1:1;
   return dist(unit,a)-dist(unit,b);
  });return options[0];
 }
 step(dt) {
  if(this.state!=='playing')return;
  dt=Math.min(dt,.05);this.time+=dt;this.assignWeapons();
  this.energy=Math.min(600,this.energy+dt*9);
  for(const k in this.cooldowns)this.cooldowns[k]=Math.max(0,this.cooldowns[k]-dt);
  for(const k in this.buffs)this.buffs[k]=Math.max(0,this.buffs[k]-dt);
  if(this.buffs.poison>0)this.hurtPlayer(dt*65*MONSTER_BALANCE.damage);
  this.player.x=clamp(this.player.x+this.move*110*dt*(this.buffs.slow>0?.42:1),175,550);
  const wave=WAVES[this.wave];this.spawnTimer-=dt;
  if(this.spawned<wave.count&&this.spawnTimer<=0){
   const ambush=this.spawned%3===2&&this.wave<5;
   this.spawn(wave.types[this.spawned%wave.types.length],ambush?650+this.random()*95:1590+this.random()*100);this.spawned++;this.spawnTimer=wave.spacing;
  }
  if(this.spawned===wave.count&&!this.enemies.some(e=>!e.dead)){
   if(!this.waveGap){this.waveGap=4;this.emit({type:'clear',wave:this.wave});this.energy=Math.min(600,this.energy+90);this.hp=Math.min(this.maxHp,this.hp+1600);}
   this.waveGap-=dt;
   if(this.waveGap<=0){
    if(this.wave===WAVES.length-1){this.state='won';this.emit({type:'end',won:true});return;}
    this.wave++;this.spawned=0;this.spawnTimer=1;this.waveGap=0;this.emit({type:'wave',wave:this.wave});
   }
  }
  for(const u of this.units){
   if(u.role==='tower'){u.x=this.player.x+35;u.y=258+u.index*80;u.visible=true;}
   else if(u.role==='aux'){u.x=this.player.x-88+(u.index%2)*172;u.y=232+Math.floor(u.index/2)*214;u.visible=true;}
   else {
    const group=Math.floor(this.time/18)%2;u.visible=Math.floor(u.index/5)===group;
    const goal=this.player.x-180+(u.index%5)*88;
    u.x+=(goal-u.x)*Math.min(1,dt*2.8);u.y=194+Math.abs(Math.sin(this.time*9+u.index*2))*(Math.abs(goal-u.x)>3?7:2);
   }
   u.recoil=Math.max(0,u.recoil-dt*55);u.timer-=dt*(this.buffs.rally>0?1.8:1)*(this.buffs.frost>0?.65:1);
   const target=this.chooseTarget(u);u.target=target;
   if(target){const desired=Math.atan2(target.y-u.y,target.x-u.x);let delta=desired-u.angle;while(delta>Math.PI)delta-=Math.PI*2;while(delta<-Math.PI)delta+=Math.PI*2;u.angle+=delta*Math.min(1,dt*7);}
   if(!u.visible||u.timer>0)continue;
   if(u.weapon.kind==='heal'){this.fired.add(u.weapon.id);this.hp=Math.min(this.maxHp,this.hp+u.weapon.damage);this.energy=Math.min(600,this.energy+15);this.emit({type:'repair',unit:u});u.timer=u.weapon.rate;continue;}
   if(u.weapon.kind==='shield'){this.fired.add(u.weapon.id);this.buffs.shield=Math.max(this.buffs.shield,2);this.emit({type:'shield'});u.timer=u.weapon.rate;continue;}
   if(!target)continue;
   this.fire(u,target);u.timer=u.weapon.rate;u.recoil=u.role==='tower'?10:5;
  }
  if(this.buffs.drone>0){this.droneTimer=(this.droneTimer||0)-dt;if(this.droneTimer<=0){const unit={x:this.player.x+190,y:650,weapon:{...HANDHELD[9],id:'skill-drone'},role:'drone',index:0};const t=this.chooseTarget(unit);if(t)this.fire(unit,t);this.droneTimer=.3;}}
  for(const e of this.enemies){
   e.age+=dt;if(e.dead)continue;e.flash=Math.max(0,e.flash-dt);e.attack-=dt;e.summon-=dt;
   for(const k of ['slow','burn','poison','stun'])e[k]=Math.max(0,e[k]-dt);
   if(e.burn>0)this.damage(e,24*dt,'flame');if(e.poison>0)this.damage(e,30*dt,'poison');if(e.dead)continue;
   const d=e.def;const ratio=e.hp/e.maxHp;
   e.burrowed=d.behavior==='burrow'&&e.age%7<2.4;
   e.charging=['charge','rhino'].includes(d.behavior)&&e.age%7>5.3;
   e.warning=['charge','rhino'].includes(d.behavior)&&e.age%7>4&&e.age%7<=5.3;
   const leap=d.behavior==='leap'?Math.max(0,Math.sin(e.age*2.3))*95:0;
   e.y=175+e.size*.43+(d.flying?190+Math.sin(e.age*2+e.phase)*58:leap);
   if(d.behavior==='bee')e.y=265+Math.sin(e.age*1.4+e.phase)*140;
   let speed=d.speed;if(e.charging)speed*=5;if(d.behavior==='tiger'&&e.age%5>3.8)speed*=3.5;
   if(d.behavior==='wolf'&&this.enemies.some(o=>o!==e&&!o.dead&&dist(o,e)<180))speed*=1.45;
   if(d.behavior==='enrage'||d.behavior==='boss')speed*=1+(1-ratio)*1.5;
   if(e.slow>0||this.buffs.freeze>0)speed*=.35;if(e.stun>0)speed=0;
   const stop=this.player.x+(d.range||150)+e.size*.25;
   if(e.x>stop)e.x-=speed*dt;
   if(e.attack<=0&&e.x<stop+25&&e.stun<=0){
    e.attack=d.flying?2.4:1.8;
    if(d.range||d.flying){
     const count=['moth','griffin','boss'].includes(d.behavior)?3:1;
     for(let i=0;i<count;i++){const a=Math.atan2(300+this.random()*280-e.y,this.player.x-e.x)+(i-(count-1)/2)*.14;this.makeBullet(e.x-e.size*.25,e.y,a,{kind:d.behavior==='frost'?'ice':d.behavior==='venom'||d.behavior==='queen'||d.behavior==='moth'?'poison':d.behavior==='web'?'web':d.behavior==='boss'?'flame':'enemy',color:d.color,damage:d.damage,speed:280,range:1900},null,true);}
    } else {this.hurtPlayer(d.damage*(e.charging?2:1)*(d.behavior==='enrage'?2-ratio:1));this.emit({type:'melee',enemy:e});}
    if(d.behavior==='lava'&&e.x<this.player.x+350)this.hurtPlayer(150*MONSTER_BALANCE.damage);
   }
   if(d.behavior==='healer'&&e.summon<=0){for(const o of this.enemies)if(!o.dead&&dist(o,e)<460)o.hp=Math.min(o.maxHp,o.hp+180);e.summon=3;this.emit({type:'aura',enemy:e});}
   if((d.behavior==='queen'||d.behavior==='boss')&&e.summon<=0){this.spawn(0,e.x-100);this.spawn(d.behavior==='boss'&&ratio<.35?2:0,e.x+30);e.summon=d.behavior==='boss'&&ratio<.35?6:11;this.emit({type:'summon',enemy:e});}
  }
  for(const b of this.bullets){
   if(b.dead)continue;b.age+=dt;b.prevX=b.x;b.prevY=b.y;
   if(b.weapon.kind==='rocket'||b.weapon.kind==='drone'){if(b.target&&!b.target.dead){const desired=Math.atan2(b.target.y-b.y,b.target.x-b.x);let delta=desired-b.angle;while(delta>Math.PI)delta-=Math.PI*2;while(delta<-Math.PI)delta+=Math.PI*2;b.angle+=clamp(delta,-dt*3.5,dt*3.5);}}
   const speed=b.weapon.speed||780;b.x+=Math.cos(b.angle)*speed*dt;b.y+=Math.sin(b.angle)*speed*dt;
   if(b.weapon.kind==='mortar'||b.weapon.kind==='bomb'){const fraction=Math.min(1,b.age/b.flight);b.x=b.startX+(b.endX-b.startX)*fraction;b.y=b.startY+(b.endY-b.startY)*fraction+Math.sin(fraction*Math.PI)*220;if(fraction>=1)this.impact(b,b.target);}
   if(b.enemy){
    if(b.x<this.player.x+115&&b.x>this.player.x-145&&b.y>175&&b.y<700){this.hurtPlayer(b.weapon.damage);if(b.weapon.kind==='poison')this.buffs.poison=4;if(b.weapon.kind==='web')this.buffs.slow=4;if(b.weapon.kind==='ice')this.buffs.frost=4;b.dead=true;this.emit({type:'impact',x:b.x,y:b.y,color:b.weapon.color,size:35});}
   } else if(!b.dead&&b.weapon.kind!=='mortar'&&b.weapon.kind!=='bomb'){
    for(const e of this.enemies){if(e.dead||b.hit.has(e.id))continue;const dx=b.x-b.prevX,dy=b.y-b.prevY;const q=clamp(((e.x-b.prevX)*dx+(e.y-b.prevY)*dy)/(dx*dx+dy*dy||1),0,1);if(Math.hypot(e.x-(b.prevX+dx*q),e.y-(b.prevY+dy*q))<e.size*.36+7){this.impact(b,e);if(b.dead)break;}}
   }
   if(b.age>6||Math.hypot(b.x-b.startX,b.y-b.startY)>(b.weapon.range||1600)||b.x< -100||b.x>1850||b.y<120||b.y>980)b.dead=true;
  }
  this.enemies=this.enemies.filter(e=>!e.dead||e.age-(e.deathTime||0)<.6);this.bullets=this.bullets.filter(b=>!b.dead);
  if(this.hp<=0&&this.state==='playing'){this.state='lost';this.emit({type:'end',won:false});}
 }
 fire(unit,target){
  const w=unit.weapon;this.fired.add(w.id);this.emit({type:'fire',unit,target,weapon:w});
  if(w.kind==='laser'||w.kind==='sniper'){
   const angle=Math.atan2(target.y-unit.y,target.x-unit.x);const dx=Math.cos(angle),dy=Math.sin(angle);
   const line=this.enemies.filter(e=>!e.dead&&(e.x-unit.x)*dx+(e.y-unit.y)*dy>0&&Math.abs((e.x-unit.x)*dy-(e.y-unit.y)*dx)<e.size*.38).sort((a,b)=>dist(unit,a)-dist(unit,b));
   for(const e of line.slice(0,w.pierce||1))this.damage(e,w.damage,w.kind);
   this.emit({type:'beam',x:unit.x,y:unit.y,tx:unit.x+dx*1500,ty:unit.y+dy*1500,color:w.color,kind:w.kind});return;
  }
  if(w.kind==='tesla'){
   let current=target,origin=unit;const hit=new Set();
   for(let i=0;i<(w.chain||3)&&current;i++){this.emit({type:'lightning',x:origin.x,y:origin.y,tx:current.x,ty:current.y,color:w.color});this.damage(current,w.damage*(1-i*.14),'tesla');current.stun=w.stun||.4;hit.add(current.id);origin=current;current=this.enemies.filter(e=>!e.dead&&!hit.has(e.id)&&dist(e,origin)<350).sort((a,b)=>dist(a,origin)-dist(b,origin))[0];}return;
  }
  const count=w.count||(w.kind==='double'?2:1);
  for(let i=0;i<count;i++){
   let angle=Math.atan2(target.y-unit.y,target.x-unit.x);
   if(['shotgun','crystal','claw'].includes(w.kind))angle+=(i-(count-1)/2)*.09;
   else if(w.kind==='rocket')angle+=(i-(count-1)/2)*.3;
   else angle+=(this.random()-.5)*.035;
   const length=unit.role==='tower'?70:unit.role==='infantry'?42:40;
   this.makeBullet(unit.x+Math.cos(angle)*length,unit.y+Math.sin(angle)*length,angle,w,target,false,i*.025);
  }
 }
 makeBullet(x,y,angle,weapon,target,enemy=false,delay=0){
  const b={id:this.nextId++,x,y,prevX:x,prevY:y,startX:x,startY:y,angle,weapon,target,enemy,age:-delay,hit:new Set(),pierce:weapon.pierce||1,dead:false,endX:target?.x||x,endY:target?.y||y};
  b.flight=target?Math.max(.5,dist(b,target)/(weapon.speed||780)):1;this.bullets.push(b);this.emit({type:'bullet',bullet:b});return b;
 }
 impact(b,e){
  const w=b.weapon;if(e&&!e.dead){b.hit.add(e.id);this.damage(e,w.damage,w.kind);if(w.slow)e.slow=w.slow;if(w.burn)e.burn=w.burn;if(w.poison)e.poison=w.poison;}
  if(w.splash){for(const o of this.enemies){if(o.dead||o===e||Math.hypot(o.x-b.x,o.y-b.y)>w.splash+o.size*.2)continue;this.damage(o,w.damage*.6,w.kind);if(w.poison)o.poison=w.poison;if(w.slow)o.slow=w.slow;if(w.kind==='gravity')o.x+=(b.x-o.x)*.22;}b.dead=true;}
  if(--b.pierce<=0)b.dead=true;
  this.emit({type:'impact',x:b.x,y:b.y,color:w.color,size:w.splash?Math.min(100,w.splash*.55):w.kind==='flame'?26:15});
 }
 damage(e,amount,kind){
  if(e.dead)return;
  let defense=e.def.armor||0;if(['sniper','drill','laser','poison','arrow'].includes(kind))defense*=.2;
  if(e.def.behavior==='crystal'&&ENERGY_KINDS.has(kind))defense=.58;
  if(e.def.behavior==='frost'&&kind==='ice')defense=.8;
  if(e.burrowed)defense=.9;if(e.def.behavior==='tiger'&&e.age%5>3.8)defense=Math.max(defense,.55);
  const dealt=amount*(1-defense);e.hp-=dealt;e.flash=.1;
  if(amount>=12)this.emit({type:'damage',enemy:e,amount:Math.round(dealt),color:kind==='ice'?'#a1f5ff':kind==='poison'?'#baff79':'#ffe7a2'});
  if(e.hp<=0){e.dead=true;e.deathTime=e.age;this.kills++;this.coins+=Math.round(e.maxHp/15);this.energy=Math.min(600,this.energy+6);this.emit({type:'kill',enemy:e});if(this.targetId===e.id)this.targetId=null;}
 }
 hurtPlayer(amount){if(this.state!=='playing')return;this.hp=Math.max(0,this.hp-amount*(this.buffs.shield>0?.06:1));if(amount>40)this.emit({type:'hurt',amount});}
 useSkill(id){
  if(this.state!=='playing')return false;const skill=SKILLS[id];if(!skill)return false;
  if(this.cooldowns[id]>0){this.emit({type:'message',text:'Kỹ năng đang hồi chiêu'});return false;}
  if(this.energy<skill.cost){this.emit({type:'message',text:'Chưa đủ năng lượng'});return false;}
  this.energy-=skill.cost;this.cooldowns[id]=skill.cooldown;
  if(skill.duration)this.buffs[id]=skill.duration;
  if(id==='heal'){this.hp=Math.min(this.maxHp,this.hp+9000);this.buffs.poison=0;}
  if(id==='freeze')for(const e of this.enemies)if(!e.dead){e.slow=6;this.damage(e,120,'ice');}
  if(id==='barrage'){
   const targets=this.enemies.filter(e=>!e.dead);
   for(let i=0;i<18&&targets.length;i++){const t=targets[i%targets.length];this.makeBullet(this.player.x+80+i*40,950+i*18,-Math.PI/2,{...TURRETS[2],damage:190,splash:145,speed:650},t);}
  }
  this.emit({type:'skill',id});return true;
 }
}
