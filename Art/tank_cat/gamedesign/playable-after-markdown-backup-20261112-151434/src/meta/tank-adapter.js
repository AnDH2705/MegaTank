import {Battle} from '../battle.js';
// The existing simulation remains isolated: this adapter receives only a snapshot.
export function createTankBattle(request,onEvent,random=Math.random){
 const model=new Battle(onEvent,random),m=request.modifiers;
 model.maxHp=m.max_hp;model.hp=m.max_hp;
 const mods=request.team.map(h=>h.skill_mods),mean=key=>mods.reduce((sum,x)=>sum+(x[key]??0),0)/Math.max(1,mods.length);
 const ult=mods.reduce((max,x)=>Math.max(max,x.ult_damage_mult??1),1),heal=mean('ult_heal_pct');
 const artifactReduction=mods.reduce((out,x)=>out*(x.damage_taken_mult??1),1);
 const reduction=Math.max(1-m.max_damage_reduction,1/(1+Math.max(0,m.defense-1)*m.defense_ratio_weight))*artifactReduction;
 const originalSpawn=model.spawn.bind(model);model.spawn=(...args)=>{const enemy=originalSpawn(...args);enemy.hp*=m.enemy_scale;enemy.maxHp*=m.enemy_scale;return enemy;};
 const originalHurt=model.hurtPlayer.bind(model);model.hurtPlayer=amount=>originalHurt(amount*m.enemy_scale*reduction);
 let skillFactor=1;
 const lifesteal=Math.min(.4,request.team.reduce((sum,h)=>sum+h.stats.lifesteal,0)/Math.max(1,request.team.length));
 const originalDamage=model.damage.bind(model);model.damage=(enemy,amount,kind)=>{const hp=Math.max(0,enemy.hp);originalDamage(enemy,amount*m.attack*skillFactor,kind);if(lifesteal>0)model.hp=Math.min(model.maxHp,model.hp+Math.min(hp,Math.max(0,hp-enemy.hp))*lifesteal);};
 const originalBullet=model.makeBullet.bind(model);model.makeBullet=(x,y,angle,weapon,target,enemy=false,delay=0)=>originalBullet(x,y,angle,skillFactor!==1&&!enemy?{...weapon,damage:weapon.damage*skillFactor}:weapon,target,enemy,delay);
 const originalSkill=model.useSkill.bind(model);model.useSkill=id=>{const factor=['freeze','barrage'].includes(id)?ult:1;skillFactor=factor;let success;try{success=originalSkill(id);}finally{skillFactor=1;}if(success&&id==='heal')model.hp=Math.min(model.maxHp,model.hp+Math.max(0,9000*(model.maxHp/m.base_hp-1))+model.maxHp*heal);return success;};
 // Existing battles start at the energy cap; artifact start-energy bonuses clamp there.
 model.energy=Math.min(600,model.energy+mods.reduce((sum,x)=>sum+(x.start_energy??0)*m.energy_conversion,0));
 const originalStep=model.step.bind(model);model.step=dt=>{if(model.state==='playing'&&m.speed_ratio!==1)for(const unit of model.units)unit.timer-=Math.min(.05,dt)*(m.speed_ratio-1);originalStep(dt);};
 return model;
}
