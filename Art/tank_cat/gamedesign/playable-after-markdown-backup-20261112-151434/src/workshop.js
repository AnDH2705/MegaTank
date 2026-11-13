import { WEAPONS, CAT_NAMES } from './data.js';
import { createMetaUI } from './meta-ui.js';

const image=(name,alt='')=>`<img src="/assets/${name}.png" alt="${alt}">`;
const tank=()=>`<div class="workshop-tank"><div class="tank-roof"></div><div class="tank-captain">${image('cat-0','Mướp chỉ huy')}</div>${[4,3,2,1,0].map((n)=>`<div class="tank-floor"><span class="floor-number">${n+1}</span>${image(`cat-${n}`,CAT_NAMES[n])}${image(`turret-${n}`,WEAPONS[n].name)}</div>`).join('')}<div class="tank-tracks">${'<i></i>'.repeat(5)}</div><span class="tank-shadow"></span></div>`;
const bolt='<i class="bolt" aria-hidden="true"></i>';

export function createWorkshop(actions){
 const link=document.createElement('link');link.rel='stylesheet';link.href='/workshop.css';document.head.append(link);
 const shell=document.createElement('section');shell.id='workshop';shell.setAttribute('aria-label','Xưởng chiến xa');
 shell.innerHTML=`<div class="workshop-backdrop" aria-hidden="true"><div class="gantry"></div><div class="lamp lamp-left"></div><div class="lamp lamp-right"></div><div class="crate crate-left">✦</div><div class="crate crate-right">✦</div><div class="workshop-ground"></div></div>
  <header class="workshop-header"><button id="workshop-settings" class="steel-button menu-button" aria-label="Cài đặt">☰<small>MENU</small></button><div class="workshop-brand"><h1><span>⚙</span> MEOW BASTION</h1><p>CHUẨN BỊ CHIẾN XA. GIỮ VỮNG PHÒNG TUYẾN. <i class="hazard-mark"></i></p></div><div class="workshop-badges"><span>🐾 <b>15</b><small>MÈO TINH NHUỆ</small></span><span>✦ <b>40</b><small>VŨ KHÍ</small></span></div></header>
  <main id="workshop-content"></main>
  <nav class="workshop-nav" aria-label="Điều hướng xưởng"><button data-screen="campaign" class="steel-button active">⚑ <span>CHIẾN DỊCH<small>Thung lũng trăng</small></span></button><button data-screen="equipment" class="steel-button">⚒ <span>TRANG BỊ<small>Khám phá hỏa lực</small></span></button><button data-screen="crew" class="steel-button">🐾 <span>ĐỘI MÈO<small>Gặp gỡ đồng đội</small></span></button></nav>
  <p class="workshop-footer">XƯỞNG TRĂNG XANH <span>5 tầng chiến xa · 6 đợt tấn công</span></p>`;
 document.querySelector('#game').append(shell);
 const back=document.createElement('button');back.id='workshop-return';back.className='steel-button';back.textContent='⌂ VỀ XƯỞNG';document.querySelector('#game').append(back);
 const dialog=document.createElement('dialog');dialog.id='workshop-options';dialog.setAttribute('aria-labelledby','options-title');
 dialog.innerHTML=`<header><h2 id="options-title">⚙ CÀI ĐẶT</h2><button id="options-close" class="red-button" aria-label="Đóng cài đặt">×</button></header><div class="option-row"><label for="workshop-volume">♫ ÂM THANH</label><input id="workshop-volume" type="range" min="0" max="100" step="5"><output id="workshop-volume-label"></output></div><div class="option-row"><span>⛶ TOÀN MÀN HÌNH</span><button id="options-fullscreen" class="steel-button">BẬT / TẮT</button></div><div class="options-tip">🐾 Âm lượng được lưu trên thiết bị này.<br><small>A / D: di chuyển · Chuột: chọn mục tiêu<br>1–4 / Q / E: kỹ năng · P: tạm dừng</small></div><div class="hazard-strip"></div>`;
 document.querySelector('#game').append(dialog);
 let screen='campaign',category='all',selected=WEAPONS[0],resume=false;
 let volume=100;try{const saved=localStorage.getItem('meow-workshop-volume');if(saved!==null&&Number.isFinite(Number(saved)))volume=Math.max(0,Math.min(100,Number(saved)));}catch{}
 actions.setVolume(volume/100);
 const content=shell.querySelector('main');
 function show(){resume=actions.park();document.body.classList.add('in-workshop');render();}
 async function deploy(restart=false){if(shell.classList.contains('deploying'))return;shell.classList.add('deploying');try{await actions.deploy(resume&&!restart);document.body.classList.remove('in-workshop');}catch(error){meta.message(error.message);}finally{shell.classList.remove('deploying');}}
 const meta=createMetaUI(shell,actions.store,{navigate(next){screen=next;render();},deploy,isResuming:()=>resume,tank});
 function render(){
  shell.querySelectorAll('[data-screen]').forEach(b=>{const active=b.dataset.screen===screen;b.classList.toggle('active',active);b.setAttribute('aria-current',active?'page':'false');});
  if(meta.render(screen))return;
  if(screen==='equipment'){
   content.innerHTML=`<section class="equipment-screen"><div class="equipment-top"><div class="loadout-side"><h2>⚒ TRANG BỊ</h2><p>Kho hỏa lực của đội mèo</p>${[0,2,6].map(n=>`<button class="loadout-card" data-weapon="${WEAPONS[n].id}">${image(WEAPONS[n].asset)}<span>${WEAPONS[n].name}<small>${WEAPONS[n].family}</small></span></button>`).join('')}</div><div class="tank-display">${tank()}<span class="tank-display-label">PHÁO ĐÀI MÈO · 5 TẦNG</span></div><div class="loadout-side right"><div class="equipment-note"><strong>40 VŨ KHÍ</strong><p>Tháp pháo, súng cá nhân<br>và mô-đun hỗ trợ.</p></div>${[4,8,13].map(n=>`<button class="loadout-card" data-weapon="${WEAPONS[n].id}">${image(WEAPONS[n].asset)}<span>${WEAPONS[n].name}<small>${WEAPONS[n].family}</small></span></button>`).join('')}</div></div><div class="inventory steel-panel"><div class="inventory-list"><div class="inventory-toolbar"><h3>KHO VŨ KHÍ</h3><div class="inventory-filters">${[['all','TẤT CẢ'],['THÁP PHÁO','THÁP PHÁO'],['VŨ KHÍ CÁ NHÂN','CÁ NHÂN'],['VŨ KHÍ PHỤ','HỖ TRỢ']].map(([key,label])=>`<button data-filter="${key}" class="${category===key?'active':''}">${label}</button>`).join('')}</div></div><div class="inventory-grid"></div></div><aside id="weapon-detail" aria-live="polite"></aside></div></section>`;
   renderInventory();renderDetail();bindWeaponButtons();
   const owned=document.createElement('button');owned.id='owned-gear';owned.className='steel-button';owned.textContent='TÚI TRANG BỊ ('+actions.store.state.inventory.gear.length+')';owned.onclick=meta.showInventory;content.querySelector('.inventory-toolbar').append(owned);
   content.querySelectorAll('[data-filter]').forEach(button=>button.onclick=()=>{category=button.dataset.filter;render();});
  }
  if(screen==='crew'){
   content.innerHTML=`<section class="crew-screen"><div class="screen-heading"><span>BIỆT ĐỘI TRĂNG XANH</span><h2>NHỮNG NGƯỜI BẠN ĐỒNG HÀNH</h2><p>5 xạ thủ cố định trên xe · 10 mèo bộ binh chia hai tổ</p></div><div class="crew-grid">${CAT_NAMES.map((name,n)=>`<article class="crew-card steel-panel">${image(`cat-${n}`,name)}<h3>${name}</h3><span>${n<5?`XẠ THỦ TẦNG ${n+1} · TỔ ALPHA`:'BỘ BINH · TỔ BRAVO'}</span><p>${n<5?'Giữ vị trí trên xe, ngắm bắn độc lập.':'Mang súng cá nhân, di chuyển theo xe.'}</p></article>`).join('')}</div><p class="crew-caption">Mỗi tầng một xạ thủ. Bộ binh Alpha / Bravo thay phiên mỗi 18 giây.</p></section>`;
  }
 }
 function bindWeaponButtons(){content.querySelectorAll('[data-weapon]').forEach(button=>button.onclick=()=>{selected=WEAPONS.find(w=>w.id===button.dataset.weapon);renderDetail();content.querySelectorAll('[data-weapon]').forEach(b=>{const active=b.dataset.weapon===selected.id;b.classList.toggle('selected',active);b.setAttribute('aria-pressed',String(active));});});}
 function renderInventory(){const list=WEAPONS.filter(w=>category==='all'||w.family===category);content.querySelector('.inventory-grid').innerHTML=list.map(w=>`<button class="weapon-tile ${w.id===selected.id?'selected':''}" data-weapon="${w.id}" aria-pressed="${w.id===selected.id}" title="${w.name}">${image(w.asset,w.name)}<small>${w.name}</small></button>`).join('');}
 function renderDetail(){content.querySelector('#weapon-detail').innerHTML=`<div class="weapon-detail-title">${image(selected.asset)}<div><small>${selected.family}</small><h3>${selected.name}</h3></div></div><p>${selected.description}</p><dl><div><dt>✹ Sát thương</dt><dd>${selected.damage}</dd></div><div><dt>◷ Nhịp bắn</dt><dd>${selected.rate.toFixed(2)} giây</dd></div><div><dt>⌖ Tầm bắn</dt><dd>${selected.range}</dd></div></dl><small class="detail-note">Hệ thống tự luân phiên cấu hình trong trận.<br>Chọn thẻ để xem thông số vũ khí.</small>`;}
 shell.querySelectorAll('[data-screen]').forEach(button=>button.onclick=()=>{screen=button.dataset.screen;render();});
 back.onclick=show;
 const slider=dialog.querySelector('input'),output=dialog.querySelector('output');
 shell.querySelector('#workshop-settings').onclick=()=>{volume=Math.round(actions.getVolume()*100);slider.value=volume;output.value=`${volume}%`;dialog.querySelector('.profile-settings')?.remove();meta.profileSettings(dialog);dialog.showModal();};
 dialog.querySelector('#options-close').onclick=()=>dialog.close();
 slider.oninput=()=>{volume=Number(slider.value);output.value=`${volume}%`;actions.setVolume(volume/100);try{localStorage.setItem('meow-workshop-volume',String(volume));}catch{}};
 dialog.querySelector('#options-fullscreen').onclick=()=>document.querySelector('#fullscreen').click();
 dialog.addEventListener('click',event=>{if(event.target===dialog){const r=dialog.getBoundingClientRect();if(event.clientX<r.left||event.clientX>r.right||event.clientY<r.top||event.clientY>r.bottom)dialog.close();}});
 // Keep gameplay hotkeys from firing while browsing the workshop or its dialog.
 addEventListener('keydown',event=>{if(!document.body.classList.contains('in-workshop'))return;if(event.key==='Escape'&&dialog.open)return;if(['a','d','p','q','e','b','f','1','2','3','4',' ','arrowleft','arrowright'].includes(event.key.toLowerCase())){event.stopImmediatePropagation();}},true);
 show();meta.boot();return {show,showResult:meta.showResult,message:meta.message};
}
