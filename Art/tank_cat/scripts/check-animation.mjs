import assert from 'node:assert/strict';
import {chromium} from 'file:///C:/Users/trieu/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright/index.mjs';
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
try {
  const page=await browser.newPage({viewport:{width:1200,height:900}});
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:5173/animation-preview.html');
  await page.waitForFunction(()=>window.animationPreview);
  const dimensions=await page.evaluate(()=>{const m=animationPreview.rig.bones.head.children[0].material.map;return [m.image.width,m.image.height];});
  console.log('Texture:',dimensions);
  await page.screenshot({path:'test-results/rig-idle.png'});
  const poses=[];
  for(const state of ['idle','walk','shoot','hit']){
    await page.click(`[data-state="${state}"]`);
    await page.waitForTimeout(150);
    poses.push(await page.evaluate(()=>{
      const p=window.animationPreview;
      p.rig.pose(p.state,.08);
      return Object.values(p.rig.bones).map(b=>[b.position.x,b.position.y,b.rotation.z,b.scale.y]);
    }));
    await page.screenshot({path:`test-results/rig-${state}.png`});
  }
  assert.equal(new Set(poses.map(p=>JSON.stringify(p))).size,4,'Animations must differ');
  await page.click('#pause');
  const frozen=await page.evaluate(()=>animationPreview.time);await page.waitForTimeout(150);
  assert.equal(await page.evaluate(()=>animationPreview.time),frozen);
  await page.check('#bones');await page.locator('#explode').fill('0.5');
  await page.screenshot({path:'test-results/rig-bones.png'});
  await page.setViewportSize({width:740,height:480});
  await page.screenshot({path:'test-results/rig-small.png'});
  assert.deepEqual(errors,[]);
  console.log('PASS: 4 distinct poses, pause, controls and browser runtime.');
} finally {await browser.close();}
