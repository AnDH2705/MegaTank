import assert from 'node:assert/strict';
import {existsSync,readFileSync} from 'node:fs';
import {createEnemyAnimation,triggerEnemyAnimation,sampleEnemyAnimation,WOLF_CLIPS,WOLF_FRAME_NAMES} from '../src/enemy-animation.js';
import {chromium} from 'file:///C:/Users/trieu/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright/index.mjs';
const manifest=JSON.parse(readFileSync('public/assets/wolf-frames/manifest.json','utf8'));
for(const name of WOLF_FRAME_NAMES)assert(existsSync(`public/assets/${name}.png`));
for(const [state,clip]of Object.entries(WOLF_CLIPS)){assert.equal(clip.duration*1000,manifest.states[state].durationMs);assert.equal(clip.frames,manifest.states[state].frames.length);}
const enemy={age:0,stun:0,dead:false},animation=createEnemyAnimation(enemy);
triggerEnemyAnimation(animation,enemy,'hit');enemy.age=.25;
triggerEnemyAnimation(animation,enemy,'hit');assert.equal(sampleEnemyAnimation(animation,enemy,true).frame,2,'Rapid hits must not pin frame zero');
triggerEnemyAnimation(animation,enemy,'attack');enemy.age=.5;triggerEnemyAnimation(animation,enemy,'hit');assert.equal(sampleEnemyAnimation(animation,enemy,true).state,'attack','Bite finishes under fire');
enemy.age=1.2;assert.equal(sampleEnemyAnimation(animation,enemy,true).state,'run');
enemy.stun=1;enemy.age=1.3;assert.equal(sampleEnemyAnimation(animation,enemy,true).state,'idle');
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
try{
 const page=await browser.newPage({viewport:{width:1600,height:900}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto('http://127.0.0.1:5174/');await page.waitForFunction(()=>window.__MEOW__);
 await page.click('#workshop-deploy');await page.waitForFunction(()=>__MEOW__.state.animations.some(a=>a.state==='run'),{},{timeout:15000});
 await page.screenshot({path:'test-results/game-wolf-natural.png'});
 await page.click('#pause');const paused=await page.evaluate(()=>__MEOW__.state.animations);await page.waitForTimeout(200);
 assert.deepEqual(await page.evaluate(()=>__MEOW__.state.animations),paused);
 await page.setViewportSize({width:900,height:506});await page.screenshot({path:'test-results/game-wolf-small.png'});
 assert.deepEqual(errors,[]);

 // Exercise actual Battle events and the production WebGL renderer with durable
 // enemies and disabled friendly fire, solely inside this browser test fixture.
 const fixture=await browser.newPage({viewport:{width:1600,height:900}});fixture.on('pageerror',e=>errors.push(e.message));
 await fixture.route('**/animation-qa',route=>route.fulfill({contentType:'text/html',body:'<body style="margin:0"><div id="mount" style="width:100vw;height:100vh"></div></body>'}));
 await fixture.goto('http://127.0.0.1:5174/animation-qa');
 await fixture.evaluate(async()=>{
  const T=await import('/vendor/three.module.js'),{Battle}=await import('/src/battle.js'),{createVisuals}=await import('/src/visuals.js'),{WOLF_FRAME_NAMES}=await import('/src/enemy-animation.js');
  const manifest=await fetch('/assets/manifest.json').then(r=>r.json()),assets=new Map();
  await Promise.all([...Object.keys(manifest),...WOLF_FRAME_NAMES].map(async name=>{const im=new Image();im.src=`/assets/${name}.png`;await im.decode();assets.set(name,im);}));
  let view;
  const battle=new Battle(e=>{if(e.type!=='spawn')view?.event(e);},()=>.5);view=createVisuals(T,document.querySelector('#mount'),assets);battle.start();battle.spawnTimer=999;battle.units.forEach(u=>u.timer=999);
  const run=battle.spawn(1,1100,{hp:999999,maxHp:999999,attack:99});
  const idle=battle.spawn(1,850,{hp:999999,maxHp:999999,stun:999,attack:99});
  const bite=battle.spawn(1,473,{hp:999999,maxHp:999999,attack:0});
  const hit=battle.spawn(1,670,{hp:999999,maxHp:999999,attack:99});
  const other=battle.spawn(0,1350,{hp:999999,maxHp:999999,attack:99});
  view.update(0,battle);battle.damage(hit,30,'rapid');
  function step(seconds){for(let i=0;i<Math.round(seconds*60);i++){battle.step(1/60);view.update(1/60,battle);}}
  step(.3);
  window.qa={view,battle,run,idle,bite,hit,other,step};
 });
 let poses=await fixture.evaluate(()=>qa.view.enemyAnimations());
 assert.deepEqual(new Set(poses.map(p=>p.state)),new Set(['idle','run','attack','hit']));
 const maps=await fixture.evaluate(()=>{const src=[];qa.view.scene.traverse(m=>{if(m.material?.map?.image?.src?.includes('/wolf-frames/'))src.push(m.material.map.image.src.split('/').pop());});return src;});
 assert.deepEqual(new Set(maps),new Set(poses.map(p=>`${p.state}-${p.frame}.png`)),'Production materials must display the sampled PNG frames');
 for(const p of poses){assert(Math.abs(p.ground-175)<.001);assert(Math.abs(p.height/p.width-256/272)<.0001);}
 await fixture.screenshot({path:'test-results/game-wolf-states.png'});
 const dimensions=poses.map(p=>[p.id,p.width,p.height]);
 await fixture.evaluate(()=>qa.step(.15));poses=await fixture.evaluate(()=>qa.view.enemyAnimations());assert.deepEqual(poses.map(p=>[p.id,p.width,p.height]),dimensions);
 await fixture.screenshot({path:'test-results/game-wolf-states-next.png'});
 await fixture.evaluate(()=>qa.step(.6));
 const recovery=await fixture.evaluate(()=>qa.view.enemyAnimations());assert.equal(recovery.find(p=>p.id===3).state,'idle');
 await fixture.evaluate(()=>{qa.battle.damage(qa.run,1e9,'laser');qa.step(.7);});assert(!(await fixture.evaluate(()=>qa.view.enemyAnimations())).some(p=>p.id===1));
 await fixture.evaluate(()=>{qa.view.clear();qa.view.update(0,qa.battle);});assert.equal((await fixture.evaluate(()=>qa.view.enemyAnimations())).length,3,'Clear/recreate does not retain dead wolves');
 assert.deepEqual(errors,[]);
 console.log('PASS: real gameplay WebGL, pause, 900x506, all 4 states from Battle events, fixed proportions/ground, recovery, death cleanup, renderer reset, 24 assets, rapid-hit/attack priority.');
}finally{await browser.close();}
