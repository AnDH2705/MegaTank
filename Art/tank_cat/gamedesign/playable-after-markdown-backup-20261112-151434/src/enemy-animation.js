export const WOLF_CLIPS={idle:{frames:6,duration:.18},run:{frames:6,duration:.09},attack:{frames:6,duration:.12},hit:{frames:6,duration:.12}};
export const WOLF_FRAME_NAMES=Object.entries(WOLF_CLIPS).flatMap(([state,clip])=>Array.from({length:clip.frames},(_,i)=>`wolf-frames/${state}-${i}`));
export function createEnemyAnimation(enemy){return{state:'idle',clock:0,lastAge:enemy.age,started:enemy.age,until:0,nextHitAt:0,frame:0};}
export function triggerEnemyAnimation(animation,enemy,state){
 if(enemy.dead)return;
 // Let bites finish, and let a hit reaction play past frame zero under rapid fire.
 if(state==='hit'&&(enemy.age<animation.nextHitAt||animation.state==='attack'&&enemy.age<animation.until))return;
 animation.state=state;animation.started=enemy.age;animation.clock=0;animation.frame=0;
 animation.until=enemy.age+WOLF_CLIPS[state].duration*WOLF_CLIPS[state].frames;
 if(state==='hit')animation.nextHitAt=enemy.age+1.05;
}
export function sampleEnemyAnimation(animation,enemy,moving,rate=1){
 const dt=Math.max(0,enemy.age-animation.lastAge);animation.lastAge=enemy.age;
 if(!enemy.dead){
  if(enemy.age<animation.until){animation.clock=Math.max(0,enemy.age-animation.started);}
  else{
   const state=moving&&enemy.stun<=0?'run':'idle';
   if(animation.state!==state){animation.state=state;animation.clock=0;}
   else animation.clock+=dt*(state==='run'?Math.max(.25,Math.min(1.8,rate)):1);
  }
  const clip=WOLF_CLIPS[animation.state];
  animation.frame=['hit','attack'].includes(animation.state)?Math.min(clip.frames-1,Math.floor(animation.clock/clip.duration)):Math.floor(animation.clock/clip.duration)%clip.frames;
 }
 return{state:animation.state,frame:animation.frame,asset:`wolf-frames/${animation.state}-${animation.frame}`};
}
