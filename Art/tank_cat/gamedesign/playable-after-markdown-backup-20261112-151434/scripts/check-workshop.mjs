import assert from 'node:assert/strict';
import {mkdir,readFile} from 'node:fs/promises';
import {chromium} from 'file:///C:/Users/trieu/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright/index.mjs';

await mkdir('test-results',{recursive:true});
for(const name of ['battle.js','data.js','visuals.js','enemy-animation.js']){
 assert((await readFile(`src/${name}`)).equals(await readFile(`../../src/${name}`)),`${name} must preserve existing combat exactly`);
}
const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
try{
 const page=await browser.newPage({viewport:{width:1600,height:900}}),errors=[],failures=[];
 page.on('pageerror',error=>errors.push(error.message));
 page.on('response',r=>{if(r.status()>=400&&!r.url().endsWith('favicon.ico'))failures.push(`${r.status()} ${r.url()}`);});
 await page.goto('http://127.0.0.1:5174/');
 await page.waitForFunction(()=>window.__MEOW__&&document.querySelector('#workshop-deploy'));
 await page.screenshot({path:'test-results/workshop-campaign.png'});
 await page.click('[data-screen="equipment"]');
 assert.equal(await page.locator('.weapon-tile').count(),40);
 await page.click('.weapon-tile[data-weapon="turret-6"]');
 assert.match(await page.locator('#weapon-detail').textContent(),/160/);
 await page.click('[data-filter="VŨ KHÍ PHỤ"]');
 assert.equal(await page.locator('.weapon-tile').count(),20);
 await page.click('[data-filter="VŨ KHÍ CÁ NHÂN"]');
 assert.equal(await page.locator('.weapon-tile').count(),10);
 await page.click('[data-filter="all"]');
 await page.screenshot({path:'test-results/workshop-equipment.png'});
 await page.click('[data-screen="crew"]');
 assert.equal(await page.locator('.crew-card').count(),3);
 await page.screenshot({path:'test-results/workshop-crew.png'});
 await page.click('#workshop-settings');
 await page.locator('#workshop-volume').fill('35');
 assert.equal(await page.locator('#workshop-volume-label').textContent(),'35%');
 await page.screenshot({path:'test-results/workshop-settings.png'});
 await page.keyboard.press('Escape');
 await page.reload();
 await page.waitForFunction(()=>window.__MEOW__&&document.querySelector('#workshop-deploy'));
 await page.click('#workshop-settings');
 assert.equal(await page.locator('#workshop-volume').inputValue(),'35');
 await page.click('#options-close');
 await page.click('#workshop-deploy');
 await page.waitForFunction(()=>window.__MEOW__.state.time>.4);
 await page.click('#workshop-return');
 const paused=await page.evaluate(()=>__MEOW__.state.time);
 await page.keyboard.press('p');
 await page.waitForTimeout(400);
 assert.equal(await page.evaluate(()=>__MEOW__.state.time),paused,'Workshop freezes battle and captures hotkeys');
 await page.click('[data-screen="equipment"]');
 await page.click('[data-screen="campaign"]');
 assert.match(await page.locator('#workshop-deploy').textContent(),/TIẾP TỤC/);
 await page.click('#workshop-deploy');
 await page.waitForFunction(t=>__MEOW__.state.time>t+.3,paused);
 await page.click('#pause');
 await page.screenshot({path:'test-results/workshop-combat.png'});
 await page.click('#workshop-return');
 await page.click('#workshop-restart');
 assert((await page.evaluate(()=>__MEOW__.state.time))<1,'Restart begins a fresh battle');
 await page.click('#workshop-return');
 for(const viewport of [{width:900,height:506},{width:390,height:844}]){
  await page.setViewportSize(viewport);
  for(const screen of ['campaign','equipment','crew']){
   await page.click(`[data-screen="${screen}"]`);
   await page.screenshot({path:`test-results/workshop-${screen}-${viewport.width}.png`});
   assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'No horizontal document overflow');
   const box=await page.locator(`[data-screen="${screen}"]`).boundingBox();assert(box&&box.y>=0&&box.y+box.height<=viewport.height,'Navigation remains reachable');
  }
 }
 assert.deepEqual(errors,[]);assert.deepEqual(failures,[]);
 console.log('PASS: unchanged combat files; inventory, filters, details, crew, saved volume, battle pause/resume/restart, hotkeys, responsive layouts, no runtime or asset errors.');
}finally{await browser.close();}
