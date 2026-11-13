import assert from 'node:assert/strict';
import {chromium} from 'file:///C:/Users/trieu/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright/index.mjs';
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
try{
  const page=await browser.newPage({viewport:{width:1200,height:1000}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:5173/wolf-frames-preview.html');await page.waitForFunction(()=>window.wolfFramesPreview);
  const count=await page.evaluate(()=>Object.values(wolfFramesPreview.images).flat().length);assert.equal(count,24);
  await page.evaluate(()=>wolfFramesPreview.setFrame(0));
  await page.screenshot({path:'test-results/wolf-frames-dark.png',fullPage:true});
  const hashes=[];
  for(const state of ['idle','run','attack','hit']){
    await page.click(`[data-state="${state}"]`);
    for(let i=0;i<6;i++){
      await page.evaluate(i=>wolfFramesPreview.setFrame(i),i);
      hashes.push(await page.evaluate(()=>wolfFramesPreview.canvas.toDataURL()));
      assert.equal(await page.evaluate(()=>wolfFramesPreview.index),i);
    }
    await page.evaluate(()=>wolfFramesPreview.setFrame(3));await page.check('#light');
    await page.screenshot({path:`test-results/wolf-frames-${state}.png`,fullPage:true});
  }
  assert.equal(new Set(hashes).size,24);
  const frozen=await page.evaluate(()=>wolfFramesPreview.index);await page.waitForTimeout(180);assert.equal(await page.evaluate(()=>wolfFramesPreview.index),frozen);
  await page.click('#next');assert.equal(await page.evaluate(()=>wolfFramesPreview.index),(frozen+1)%6);
  await page.click('#previous');assert.equal(await page.evaluate(()=>wolfFramesPreview.index),frozen);
  await page.click('#pause');await page.waitForTimeout(190);assert.notEqual(await page.evaluate(()=>wolfFramesPreview.index),frozen);
  await page.click('#pause');await page.check('#guide');
  await page.screenshot({path:'test-results/wolf-frames-guides.png',fullPage:true});
  await page.setViewportSize({width:740,height:480});await page.screenshot({path:'test-results/wolf-frames-small.png',fullPage:true});
  assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);
  const gif=await page.request.get('http://127.0.0.1:5173/assets/wolf-frames/wolf-actions.gif');assert.equal(gif.status(),200);assert.match(gif.headers()['content-type'],/image\/gif/);
  await page.goto('http://127.0.0.1:5173/assets/wolf-frames/wolf-actions.gif');
  await page.screenshot({path:'test-results/wolf-frames-gif.png'});
  assert.deepEqual(errors,[]);
  console.log('PASS: 24 distinct complete frames, 4 clips, step/previous, pause/play, mobile layout, GIF served; no page errors.');
}finally{await browser.close();}
