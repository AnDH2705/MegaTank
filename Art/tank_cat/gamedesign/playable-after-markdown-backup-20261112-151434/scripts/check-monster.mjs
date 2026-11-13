import assert from 'node:assert/strict';
import {chromium} from 'file:///C:/Users/trieu/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright/index.mjs';
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
try{
  const page=await browser.newPage({viewport:{width:1200,height:1000}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:5173/monster-preview.html');
  await page.waitForFunction(()=>window.monsterPreview);
  await page.click('#pause');
  assert.equal(await page.evaluate(()=>Object.keys(monsterPreview.rig.bones).length),9);
  await page.evaluate(()=>monsterPreview.setTime(.25));
  await page.screenshot({path:'test-results/wolf-idle-dark.png'});
  await page.check('#light');
  const poses=[],frames=[];
  for(const state of ['idle','run','attack','hit']){
    await page.click(`[data-state="${state}"]`);
    await page.evaluate(t=>monsterPreview.setTime(t),state==='attack'?.52:state==='hit'?.06:.14);
    poses.push(await page.evaluate(()=>Object.values(monsterPreview.rig.bones).map(b=>[b.position.x,b.position.y,b.rotation.z,b.scale.y])));
    await page.screenshot({path:`test-results/wolf-${state}-light.png`});
  }
  assert.equal(new Set(poses.map(p=>JSON.stringify(p))).size,4);
  // Sample the entire gait and lunge, including extrema, for visual inspection.
  for(const [state,times]of [['run',[0,.175,.35,.525]],['attack',[.2,.4,.52,.75]]]){
    await page.click(`[data-state="${state}"]`);
    for(const t of times){
      const url=await page.evaluate(t=>{monsterPreview.setTime(t);return monsterPreview.renderer.domElement.toDataURL();},t);
      frames.push({url,label:`${state} ${t}s`});
    }
  }
  const frozen=await page.evaluate(()=>monsterPreview.time);await page.waitForTimeout(100);
  assert.equal(await page.evaluate(()=>monsterPreview.time),frozen);
  await page.check('#bones');await page.locator('#explode').fill('0.6');
  await page.screenshot({path:'test-results/wolf-bones.png'});
  await page.locator('#explode').fill('0');await page.uncheck('#bones');
  await page.setViewportSize({width:740,height:480});await page.screenshot({path:'test-results/wolf-small.png',fullPage:true});
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);
  const gallery=await browser.newPage({viewport:{width:1280,height:600}});
  await gallery.setContent('<body style="margin:0;background:#e3e6e9;display:grid;grid-template-columns:repeat(4,1fr);font:14px sans-serif"></body>');
  await gallery.evaluate(frames=>{for(const f of frames){const box=document.createElement('div'),img=new Image();img.src=f.url;img.style.cssText='width:100%;display:block';box.append(img,document.createTextNode(f.label));document.body.append(box);}},frames);
  await gallery.locator('img').evaluateAll(imgs=>Promise.all(imgs.map(i=>i.decode())));
  await gallery.screenshot({path:'test-results/wolf-motion-contact.png',fullPage:true});
  assert.deepEqual(errors,[]);
  console.log('PASS: 9 bones, 4 distinct poses, pause, mobile overflow, no browser errors; 8 gait/attack samples captured.');
}finally{await browser.close();}
