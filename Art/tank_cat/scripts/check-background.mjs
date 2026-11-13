import assert from 'node:assert/strict';
import {chromium} from 'file:///C:/Users/trieu/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright/index.mjs';

const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
try{
 const page=await browser.newPage(),errors=[];
 page.on('pageerror',e=>errors.push(e.message));
 // Emulate a hidden tab with suspended animation frames; browser timers remain
 // real, so this also catches accidentally relying only on requestAnimationFrame.
 await page.addInitScript(()=>{
  window.qaHidden=false;
  Object.defineProperty(document,'hidden',{get:()=>window.qaHidden});
  const raf=window.requestAnimationFrame.bind(window);
  window.requestAnimationFrame=callback=>raf(function frame(now){if(window.qaHidden)raf(frame);else callback(now);});
 });
 await page.goto('http://127.0.0.1:5173/');
 await page.waitForFunction(()=>window.__MEOW__);
 await page.click('#start');
 const state=()=>page.evaluate(()=>window.__MEOW__.state);
 const beforeBlur=await state();
 await page.evaluate(()=>{dispatchEvent(new KeyboardEvent('keydown',{key:'d'}));dispatchEvent(new Event('blur'));});
 await page.waitForTimeout(500);
 assert.equal((await state()).paused,false,'Losing focus must not pause');
 assert((await state()).time>beforeBlur.time+.3);
 const x=(await state()).units[0].x;
 await page.waitForTimeout(300);
 assert.equal((await state()).units[0].x,x,'Blur releases held movement');
 await page.evaluate(()=>{window.qaHidden=true;document.dispatchEvent(new Event('visibilitychange'));});
 const hiddenStart=await state();
 await page.waitForTimeout(1800);
 assert((await state()).time>hiddenStart.time+1.4,'Battle continues without animation frames');
 // Delayed timer callbacks must account for elapsed time, not just 0.1 seconds.
 const delayedStart=(await state()).time;
 await page.evaluate(()=>{const until=performance.now()+1200;while(performance.now()<until){};});
 await page.waitForTimeout(300);
 assert((await state()).time>delayedStart+1.2,'Throttled timers preserve battle time');
 await page.evaluate(()=>{window.qaHidden=false;document.dispatchEvent(new Event('visibilitychange'));});
 await page.click('#pause');
 const pausedTime=(await state()).time;
 await page.evaluate(()=>{window.qaHidden=true;document.dispatchEvent(new Event('visibilitychange'));});
 await page.waitForTimeout(600);
 assert.equal((await state()).time,pausedTime,'Manual pause survives hidden tab');
 await page.evaluate(()=>{window.qaHidden=false;document.dispatchEvent(new Event('visibilitychange'));});
 await page.click('#pause');
 await page.waitForTimeout(400);
 assert((await state()).time>pausedTime+.2);
 await page.click('#codex');
 const bookTime=(await state()).time;
 await page.evaluate(()=>{window.qaHidden=true;document.dispatchEvent(new Event('visibilitychange'));});
 await page.waitForTimeout(600);
 assert.equal((await state()).time,bookTime,'Encyclopedia still pauses battle');
 await page.evaluate(()=>{window.qaHidden=false;document.dispatchEvent(new Event('visibilitychange'));});
 await page.click('#close-book');
 await page.waitForTimeout(400);
 assert((await state()).time>bookTime+.2);
 assert.deepEqual(errors,[]);
 console.log('PASS: focus loss, released movement, hidden tab without RAF, delayed timers, manual pause/resume, encyclopedia, no page errors.');
}finally{await browser.close();}
