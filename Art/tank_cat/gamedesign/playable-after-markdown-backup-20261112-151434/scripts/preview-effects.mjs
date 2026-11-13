import assert from 'node:assert/strict';
import {chromium} from 'file:///C:/Users/trieu/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright/index.mjs';
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
try{
 const page=await browser.newPage({viewport:{width:1600,height:900}}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));
 await page.route('**/effects-qa',route=>route.fulfill({contentType:'text/html',body:'<body style="margin:0;background:#072b48"><div id="mount" style="width:100vw;height:100vh"></div></body>'}));
 await page.goto('http://127.0.0.1:5173/effects-qa');
 await page.evaluate(async()=>{
  const T=await import('/vendor/three.module.js'),{Battle}=await import('/src/battle.js'),{createVisuals}=await import('/src/visuals.js'),{WOLF_FRAME_NAMES}=await import('/src/enemy-animation.js'),{TURRETS}=await import('/src/data.js');
  const manifest=await fetch('/assets/manifest.json').then(r=>r.json()),assets=new Map();
  await Promise.all([...Object.keys(manifest),...WOLF_FRAME_NAMES].map(async name=>{const im=new Image();im.src=`/assets/${name}.png`;await im.decode();assets.set(name,im);}));
  let view;const battle=new Battle(e=>view?.event(e),()=>.5);view=createVisuals(T,document.querySelector('#mount'),assets);battle.start();battle.spawnTimer=999;
  battle.spawn(1,1090,{hp:999999,maxHp:999999});battle.spawn(4,1360,{hp:999999,maxHp:999999});view.update(0,battle);
  const baseline=view.renderer.info.memory.geometries;
  const rocket=TURRETS.find(w=>w.kind==='rocket');
  battle.makeBullet(690,520,-.25,rocket);battle.makeBullet(870,410,-.4,TURRETS.find(w=>w.kind==='rapid'));
  view.event({type:'fire',unit:battle.units[0],weapon:battle.units[0].weapon});
  view.event({type:'impact',x:1160,y:260,size:90,color:'#ffad37'});
  view.event({type:'impact',x:1440,y:340,size:60,color:'#ffad37'});
  view.update(.08,battle);
  window.qa={view,battle,baseline};
 });
 await page.screenshot({path:'test-results/effects-large.png'});
 const scales=await page.evaluate(()=>qa.view.scene.children.filter(m=>m.isGroup&&m.scale.x===2).map(m=>m.scale.x));assert.equal(scales.length,2);
 await page.setViewportSize({width:900,height:506});await page.screenshot({path:'test-results/effects-small.png'});
 await page.evaluate(()=>{for(let i=0;i<35;i++)qa.view.update(1/60,qa.battle);});
 await page.screenshot({path:'test-results/effects-smoke.png'});
 await page.evaluate(()=>{qa.battle.bullets=[];for(let i=0;i<180;i++)qa.view.update(1/60,qa.battle);});
 assert.equal(await page.evaluate(()=>qa.view.renderer.info.memory.geometries),await page.evaluate(()=>qa.baseline),'Transient explosion geometry is released');
 assert.deepEqual(errors,[]);
 console.log('PASS: Chrome WebGL, 2x projectiles, explosions at 1600x900 / 900x506, rising smoke, transient geometry cleanup, no page errors.');
}finally{await browser.close();}
