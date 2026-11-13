import { Battle } from './battle.js';
import { WEAPONS, MONSTERS, WAVES, SKILLS, CAT_NAMES } from './data.js';
import { createVisuals } from './visuals.js';
import { AudioEngine } from './audio.js';
import { WOLF_FRAME_NAMES } from './enemy-animation.js';
import { createWorkshop } from './workshop.js';
import {loadCatalog} from './meta/catalog.js';
import {GameStore} from './meta/store.js';
import {createTankBattle} from './meta/tank-adapter.js';
const $=s=>document.querySelector(s),fmt=n=>Math.round(n).toLocaleString('vi-VN');
export async function launch(THREE){
 const catalog=await loadCatalog(),store=await new GameStore(catalog,localStorage).init();
 const manifest=await fetch('/assets/manifest.json').then(r=>{if(!r.ok)throw new Error('Thiếu manifest sprite');return r.json();});
 const names=[...Object.keys(manifest),...WOLF_FRAME_NAMES],assets=new Map();let count=0;
 await Promise.all(names.map(name=>new Promise((resolve,reject)=>{const img=new Image();img.onload=()=>{assets.set(name,img);count++;$('#loading-message').textContent=`Đang tải tài nguyên ${count} / ${names.length}`;resolve();};img.onerror=()=>reject(new Error(`Không tải được ${name}`));img.src=`/assets/${name}.png`;})));
 const view=createVisuals(THREE,$('#viewport'),assets);const audio=new AudioEngine();let model,paused=false,bookWasPlaying=false,toastTimeout,announceTimeout,started=false,workshop,request=null,settling=false,preparing=false;
 function announce(title,subtitle='',small='CHIẾN DỊCH TRĂNG XANH'){clearTimeout(announceTimeout);const el=$('#announcement');el.querySelector('small').textContent=small;el.querySelector('b').textContent=title;el.querySelector('span').textContent=subtitle;el.classList.add('show');announceTimeout=setTimeout(()=>el.classList.remove('show'),3200);}
 function toast(text){$('#toast').textContent=text;$('#toast').classList.add('show');clearTimeout(toastTimeout);toastTimeout=setTimeout(()=>$('#toast').classList.remove('show'),2300);}
 function onEvent(e){if(!document.hidden&&!document.body.classList.contains('in-workshop'))view.event(e);audio.event(e);
  if(e.type==='wave')announce(WAVES[e.wave].name,`${e.wave===5?'Trùm cuối · ':''}Giữ xe sống sót và tiêu diệt toàn bộ quái`,`ĐỢT ${e.wave+1} / 6`);
  if(e.type==='clear')announce('PHÒNG TUYẾN AN TOÀN','+1.600 máu · +90 năng lượng','HOÀN THÀNH ĐỢT');
  if(e.type==='message')toast(e.text);
  if(e.type==='bank'&&started)toast(`Đổi cấu hình ${e.bank+1}: vũ khí sẵn sàng`);
  if(e.type==='skill')toast(`${SKILLS[e.id].name.toUpperCase()} ĐÃ KÍCH HOẠT`);
  if(e.type==='end')settleBattle(e.won);
 }
 function fresh(preview=false){view.clear();model=request&&!preview?createTankBattle(request,onEvent):new Battle(onEvent);if(preview){for(const[type,x]of [[0,870],[1,1090],[2,1260],[3,1440]])model.spawn(type,x);for(const e of model.enemies)e.age=2;model.units.forEach(u=>u.visible=u.role!=='infantry'||u.index<5);}}
 fresh(true);
 const dots=$('#wave-dots');for(let i=0;i<6;i++)dots.appendChild(document.createElement('i'));
 $('#start').disabled=false;$('#start').textContent='XUẤT KÍCH →';$('#loading-message').textContent='Súng tự ngắm từng mục tiêu. Bấm vào quái để toàn đội tập trung hỏa lực.';$('#status').textContent='SẴN SÀNG XUẤT KÍCH';
 async function start(){audio.unlock();preparing=true;try{request=await store.command('begin_battle',{index:store.state.campaign.selected_index});fresh();started=true;paused=false;acc=0;last=performance.now();model.start();$('#overlay').classList.add('hidden');$('#pause').textContent='Ⅱ';$('.mission .eyebrow').textContent=`ẢI ${request.stage_index} · ${catalog.stages[request.stage_index-1].name}`;}finally{preparing=false;}}
 $('#start').onclick=()=>start().catch(error=>workshop.message(error.message));
 async function settleBattle(victory){if(settling||!request)return;settling=true;const id=request.id;try{const receipt=await store.command('finish_battle',{id,result:{victory}});workshop.show();workshop.showResult(receipt,{retry:()=>start().then(()=>document.body.classList.remove('in-workshop')).catch(e=>workshop.message(e.message)),next:async()=>{try{await store.command('select_stage',{index:Math.min(20,receipt.stage_index+1)});await start();document.body.classList.remove('in-workshop');}catch(error){workshop.message(error.message);}}});}catch(error){workshop.show();workshop.showResult({error:error.message},{settleAgain:()=>settleBattle(victory)});}finally{settling=false;}}

 function togglePause(){if(!started||['won','lost'].includes(model.state))return;paused=!paused;$('#pause').textContent=paused?'▶':'Ⅱ';$('#pause').setAttribute('aria-label',paused?'Tiếp tục':'Tạm dừng');if(paused)announce('TẠM DỪNG','Bấm P hoặc nút tiếp tục để chiến đấu');else $('#announcement').classList.remove('show');}
 $('#pause').onclick=togglePause;
 $('#sound').onclick=()=>{$('#sound').textContent=audio.toggle()?'♫':'♪';$('#sound').style.opacity=audio.enabled?'1':'.45';};
 async function fullscreen(){try{if(document.fullscreenElement)await document.exitFullscreen();else await $('#game').requestFullscreen();}catch{toast('Trình duyệt chưa cho phép toàn màn hình');}}
 $('#fullscreen').onclick=fullscreen;
 const keys=new Set();const keySkills={'1':'rally','2':'heal','3':'freeze','4':'drone','q':'barrage','e':'shield'};
 addEventListener('keydown',e=>{if(e.repeat)return;const k=e.key.toLowerCase();if(['a','d','arrowleft','arrowright',' ','p','q','e','1','2','3','4','b','f'].includes(k))e.preventDefault();if($('#book').open){if(k==='b')$('#book').close();return;}keys.add(k);if(k==='p'||k===' ')togglePause();if(k==='b')openBook();if(k==='f')fullscreen();if(keySkills[k]&&!paused)model.useSkill(keySkills[k]);});
 addEventListener('keyup',e=>keys.delete(e.key.toLowerCase()));
 addEventListener('blur',()=>{keys.clear();touchMove=0;});
 document.querySelectorAll('[data-skill]').forEach(el=>el.onclick=()=>{audio.unlock();if(!paused)model.useSkill(el.dataset.skill);});
 let touchMove=0;document.querySelectorAll('[data-move]').forEach(el=>{el.onpointerdown=e=>{el.setPointerCapture(e.pointerId);touchMove=el.dataset.move==='left'?-1:1;};el.onpointerup=el.onpointercancel=()=>touchMove=0;});
 $('#viewport').addEventListener('pointerdown',e=>{if(!started||paused)return;audio.unlock();const p=view.screenToWorld(e.clientX,e.clientY);const target=model.enemies.filter(o=>!o.dead&&Math.hypot(o.x-p.x,o.y-p.y)<o.size*.6+30).sort((a,b)=>Math.hypot(a.x-p.x,a.y-p.y)-Math.hypot(b.x-p.x,b.y-p.y))[0];model.targetId=target?.id||null;toast(target?`TẬP TRUNG HỎA LỰC: ${target.def.name}`:'ĐÃ CHUYỂN SANG NGẮM TỰ ĐỘNG');});
 let tab='weapons';
 function renderBook(){
  $('#book-title').textContent=tab==='weapons'?'Kho vũ khí':tab==='monsters'?'Quái vật dưới trăng':'Đội mèo chiến đấu';
  document.querySelectorAll('[data-tab]').forEach(el=>el.classList.toggle('active',el.dataset.tab===tab));
  let html='';
  if(tab==='weapons')html=WEAPONS.map(w=>`<article class="entry"><img src="/assets/${w.asset}.png" alt="${w.name}"><small>${w.family} ${model.fired.has(w.id)?'· ĐÃ TRIỂN KHAI':''}</small><h3>${w.name}</h3><p>${w.description}</p><small>${w.kind==='shield'?'KHIÊN 2 GIÂY':w.kind==='heal'?'HỒI 240 MÁU':`${w.damage} SÁT THƯƠNG · ${w.rate.toFixed(2)}s / PHÁT`}</small></article>`).join('');
  if(tab==='monsters')html=MONSTERS.map((m,i)=>`<article class="entry"><img src="/assets/${m.asset}.png" alt="${m.name}"><small>${m.boss?'TRÙM CUỐI':m.flying?'QUÁI BAY':'QUÁI MẶT ĐẤT'} ${model.seen.has(i)?'· ĐÃ GẶP':''}</small><h3>${m.name}</h3><p>${m.description}</p><small>${fmt(m.hp)} MÁU · ${m.size} KÍCH THƯỚC · ${m.damage} CÔNG</small></article>`).join('');
  if(tab==='crew'){
   html=CAT_NAMES.slice(0,5).map((name,i)=>`<article class="entry"><img src="/assets/cat-${i}.png" alt="${name}"><small>TẦNG ${i+1} · XẠ THỦ CỐ ĐỊNH</small><h3>${name}</h3><p>Gắn cố định vào tầng ${i+1}. Cụm súng xoay độc lập, ưu tiên mục tiêu được chọn. Luân phiên hai họ tháp pháo.</p></article>`).join('');
   html+=CAT_NAMES.map((name,i)=>`<article class="entry"><img src="/assets/cat-${i}.png" alt="${name}"><small>MÈO BỘ BINH · TỔ ${i<5?'ALPHA':'BRAVO'}</small><h3>${name} · ${WEAPONS[10+i].name}</h3><p>Cầm vũ khí cá nhân, chạy theo xe và tự ngắm mục tiêu. Hai tổ thay phiên mỗi 18 giây.</p></article>`).join('');
  }
  $('#book-content').innerHTML=html;
 }
 function openBook(){bookWasPlaying=started&&!paused&&model.state==='playing';if(bookWasPlaying)paused=true;keys.clear();renderBook();$('#book').showModal();}
 $('#codex').onclick=openBook;$('#close-book').onclick=()=>$('#book').close();$('#book').addEventListener('close',()=>{if(bookWasPlaying)paused=false;});document.querySelectorAll('[data-tab]').forEach(el=>el.onclick=()=>{tab=el.dataset.tab;renderBook();});

 function hud(){
  $('#coins').textContent=fmt(model.coins);$('#kills').textContent=model.kills;$('#hp-text').textContent=`${fmt(model.hp)} / ${fmt(model.maxHp)}`;$('#hp-fill').style.width=`${model.hp/model.maxHp*100}%`;$('#energy-text').textContent=`${Math.floor(model.energy)} / 600`;$('#energy-fill').style.width=`${model.energy/6}%`;
  $('#wave-label').innerHTML=`ĐỢT ${model.wave+1} <i>/ 6</i>`;[...dots.children].forEach((el,i)=>el.className=i<model.wave?'done':i===model.wave?'active':'');
  const live=model.enemies.filter(e=>!e.dead),boss=live.find(e=>e.def.boss);
  $('#enemy-title').firstChild.textContent=boss?'BẠO LONG HẮC NGUYỆT ':'QUÁI VẬT ĐANG TIẾN ĐẾN ';$('#enemy-count').textContent=boss?`${fmt(boss.hp)} / ${fmt(boss.maxHp)}`:`${live.length} MỤC TIÊU`;
  $('#enemy-fill').style.width=`${boss?boss.hp/boss.maxHp*100:Math.min(100,live.length*9)}%`;
  $('#aim-label').textContent=model.targetId?'ĐANG KHÓA MỤC TIÊU':'NGẮM TỰ ĐỘNG';$('#bank-label').textContent=`CẤU HÌNH VŨ KHÍ ${String(model.bank+1).padStart(2,'0')} · ${model.fired.size}/40 ĐÃ TRIỂN KHAI`;
  $('#status').textContent=paused?'ĐÃ TẠM DỪNG':!started?'SẴN SÀNG XUẤT KÍCH':`${Math.floor(model.time/60)}:${String(Math.floor(model.time%60)).padStart(2,'0')} · ${model.buffs.shield>0?'KHIÊN ĐANG BẬT':model.buffs.frost>0?'ĐANG BỊ LÀM CHẬM':'HỆ THỐNG SẴN SÀNG'}`;
  document.querySelectorAll('[data-skill]').forEach(el=>{const id=el.dataset.skill,cd=model.cooldowns[id]||0;el.classList.toggle('unavailable',model.energy<SKILLS[id].cost||cd>0);el.querySelector('.cooldown').style.top=cd>0?'0':'100%';el.querySelector('.cooldown').textContent=cd>0?Math.ceil(cd):'';});
 }
 workshop=createWorkshop({
  store,
  async deploy(resume){if(resume&&started&&model.state==='playing'&&store.state.active_battle?.id===request?.id){paused=false;last=performance.now();$('#pause').textContent='Ⅱ';$('#pause').setAttribute('aria-label','Tạm dừng');$('#announcement').classList.remove('show');}else await start();},
  park(){keys.clear();touchMove=0;if(started&&model.state==='playing'&&store.state.active_battle?.id===request?.id){paused=true;return true;}return false;},
  setVolume(value){audio.enabled=value>0;audio.volume=value;if(audio.master)audio.master.gain.value=.22*value;$('#sound').textContent=value>0?'♫':'♪';},
  getVolume(){return audio.enabled?(audio.volume??1):0;}
 });
 let last=performance.now(),acc=0,hudTimer=0;function tick(now,background=false){const elapsed=Math.max(0,(now-last)/1000),dt=background?elapsed:Math.min(elapsed,.1);last=now;model.move=touchMove+(keys.has('d')||keys.has('arrowright')?1:0)-(keys.has('a')||keys.has('arrowleft')?1:0);
  if(!paused&&model.state==='playing'){acc+=dt;while(acc>=1/60){model.step(1/60);acc-=1/60;}}
  if(!document.hidden&&!document.body.classList.contains('in-workshop'))view.update(paused?0:dt,model);
  hudTimer+=dt;if(hudTimer>.1){hud();hudTimer=0;}
 }
 // Hidden tabs stop receiving animation frames. Keep the battle clock moving
 // with a timer, including elapsed time when the browser throttles that timer.
 function loop(now){if(!document.hidden)tick(now);requestAnimationFrame(loop);}
 setInterval(()=>{if(document.hidden)tick(performance.now(),true);},250);
 document.addEventListener('visibilitychange',()=>{keys.clear();touchMove=0;tick(performance.now(),true);});
 requestAnimationFrame(loop);
 store.subscribe(()=>{if(!preparing&&started&&model.state==='playing'&&request&&store.state.active_battle?.id!==request.id){paused=true;started=false;model.state='abandoned';workshop.show();workshop.message('Trận đã được thay thế ở cửa sổ khác.');}});
 addEventListener('storage',event=>{if(event.key==='meow-bastion-profile-v1')try{store.sync();}catch(error){paused=true;workshop.message(error.message);}});
 // Read-only diagnostics for browser QA and future balancing.
 window.__MEOW__={get state(){return{state:model.state,wave:model.wave,time:model.time,hp:model.hp,energy:model.energy,kills:model.kills,enemyCount:model.enemies.filter(e=>!e.dead).length,fired:[...model.fired],seen:[...model.seen],units:model.units.map(u=>({id:u.id,x:u.x,y:u.y,angle:u.angle,target:u.target?.id,weapon:u.weapon.id,visible:u.visible})),animations:view.enemyAnimations(),paused};},renderer:view.renderer};
}
