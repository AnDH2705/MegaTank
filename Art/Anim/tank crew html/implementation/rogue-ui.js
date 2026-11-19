(function(host){
 'use strict';
 const R=host.TankRogueSystem;
 const svg=body=>'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 220 140" aria-hidden="true"><g stroke="#292733" stroke-width="4" stroke-linejoin="round" stroke-linecap="round">'+body+'</g></svg>';
 function tent(medical=false){return svg('<ellipse cx="112" cy="126" rx="101" ry="8" fill="#8f795c" opacity=".3" stroke="none"/><path d="M15 118 81 31 117 15 209 119Z" fill="'+(medical?'#eee5cc':'#8c9b55')+'"/><path d="M81 31 117 15 181 118H15Z" fill="'+(medical?'#d1c5a6':'#61764c')+'"/><path d="M81 31 121 119H42Z" fill="#303738"/><path d="M81 31 67 111 40 119Z" fill="'+(medical?'#eee5cc':'#8c9b55')+'"/><path d="m82 31 38 88-22-4Z" fill="'+(medical?'#f6eed9':'#a8b970')+'"/><path d="M17 116v14m190-13v13M118 16l10-10M19 116 2 130m201-12 15 12" fill="none"/><path d="M147 46h20v14h14v20h-14v14h-20V80h-14V60h14Z" fill="'+(medical?'#e96355':'#d7db9d')+'" stroke-width="3"/><path d="M143 119v-19h43v24h-43m10-24v24m22-24v24" fill="#c2924a"/>' );}
 function icon(name){
  if(name==='heal')return svg('<rect x="51" y="36" width="118" height="85" rx="12" fill="#eae5d2"/><path d="M86 35V22h48v13" fill="none"/><path d="M99 52h22v19h19v23h-19v19H99V94H80V71h19Z" fill="#e96658"/>');
  if(name==='gears')return svg('<path d="M100 17h24l5 16 14 8 18-4 12 21-13 13v17l13 13-12 21-18-5-14 9-5 13h-24l-5-13-14-9-18 5-12-21 13-13V71L51 58l12-21 18 4 14-8Z" fill="#90d3d8"/><circle cx="112" cy="78" r="24" fill="#436473"/><circle cx="112" cy="78" r="13" fill="#dbe9dc"/>');
  if(name==='base')return svg('<path d="M31 63h132l17 44H24Z" fill="#268d75"/><rect x="29" y="101" width="147" height="28" rx="14" fill="#343949"/><path d="M161 66h43v18h-39M72 63V40h70l14 23Z" fill="#36b59a"/><circle cx="53" cy="115" r="10" fill="#e9be63"/><circle cx="84" cy="115" r="10" fill="#e9be63"/><circle cx="116" cy="115" r="10" fill="#e9be63"/><circle cx="148" cy="115" r="10" fill="#e9be63"/>');
  const color=name==='plasma'?'#80e7f0':name==='explosive'?'#ed9c4a':'#c8d7dc',count=name==='mg'?3:1,parts=[];
  for(let i=0;i<count;i++){const x=count===3?55+i*40:88,w=count===3?27:44;parts.push('<path d="M'+x+' 112V48Q'+(x+w/2)+' 9 '+(x+w)+' 48v64Z" fill="'+color+'"/><path d="M'+x+' 87h'+w+'v25h-'+w+'Z" fill="#e7b65b"/><path d="M'+x+' 49h'+w+'" fill="none"/>');}
  if(name==='explosive')parts.push('<path d="m145 32 12-18 3 17 22-4-14 16 12 10-23-1-6 16-4-21-15-2Z" fill="#ffdc55"/>');
  if(name==='plasma')parts.push('<path d="m133 22-21 35h20l-13 31 35-44h-22l16-22Z" fill="#fff5b0" stroke="#59828f" stroke-width="2"/>');
  if(name==='mortar')parts.push('<path d="m87 97-20 26 23-5m42-21 20 26-23-5" fill="#757c58"/>');return svg(parts.join(''));
 }
 function create(root,getState){
  const overlay=root.querySelector('[data-rogue-overlay]'),options=root.querySelector('[data-rogue-options]'),hud=root.querySelector('[data-rogue-hud]'),notice=root.querySelector('[data-rogue-notice]'),assets=JSON.parse(document.getElementById('tank-game-assets').textContent);let activeID=null,busy=false;
  const escape=text=>String(text).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  function art(name){const src=assets['tank_preview_'+name];return src?'<img src="'+src+'" alt="">':icon(name);}
  async function select(id){const s=getState(),event=s&&s.rogue&&s.rogue.encounter;if(!event||busy)return;const option=event.options.find(o=>o.id===id);busy=true;overlay.dataset.busy='true';
   try{if(option&&option.ad&&typeof root.tankRewardedAdHandler==='function'){const ok=await root.tankRewardedAdHandler({eventID:event.id,reward:id});if(ok!==true||s!==getState()||s.rogue.encounter!==event)return;}
    if(R.resolve(s,id,event.id)){root.tankResources.creditBattle(s);render(s);root.querySelector('[data-bt-pause]').focus();}
   }finally{busy=false;overlay.dataset.busy='false';}
  }
  function render(s){
   if(!s||!s.rogue){hud.hidden=true;overlay.hidden=true;notice.hidden=true;return;}hud.hidden=false;const r=s.rogue,p=s.player;
   root.querySelector('[data-rogue-wave]').textContent=s.phase.startsWith('boss')?'BOSS':(s.phase==='wave_break'?'XONG WAVE ':'WAVE ')+String(r.wave).padStart(2,'0')+' / '+String(r.waveCount).padStart(2,'0');
   for(const w of R.WEAPONS){const node=root.querySelector('[data-rogue-ammo="'+w+'"]');node.querySelector('strong').textContent=p.ammo[w];node.classList.toggle('is-empty',p.ammo[w]===0);node.classList.toggle('is-unequipped',!p.weapons.includes(w));}
   root.querySelector('[data-rogue-ammo-type]').textContent=R.TYPES[r.ammoType].name;notice.hidden=r.noticeLeft<=0;notice.textContent=r.notice;
   const event=r.encounter;overlay.hidden=s.phase!=='encounter'||!event;root.querySelector('[data-bt-pause]').disabled=!overlay.hidden;if(overlay.hidden){activeID=null;return;}if(activeID===event.id)return;activeID=event.id;
   root.querySelector('[data-rogue-kicker]').textContent='ĐIỂM DỪNG · SAU WAVE '+String(event.wave).padStart(2,'0');root.querySelector('[data-rogue-title]').textContent=event.title;root.querySelector('[data-rogue-copy]').textContent=event.copy;
   const offered=event.options.find(o=>['speed','heavy'].includes(o.id));root.querySelector('[data-rogue-scene]').innerHTML=event.type==='wreck'?art(offered.id):tent(event.type==='medical');
   options.classList.toggle('is-two',event.options.length===2);options.innerHTML=event.options.map(o=>'<button type="button" class="rg-card'+(o.ad?' is-rare':'')+'" data-rogue-choice="'+o.id+'"'+(o.disabled?' disabled':'')+'>'+(o.ad?'<span class="rg-ad" aria-label="Lựa chọn quảng cáo">▶ AD</span>':'')+'<div class="rg-card-art">'+art(o.icon)+'</div><small>'+escape(o.tag)+'</small><strong>'+escape(o.name)+'</strong><p>'+escape(o.copy)+'</p><em>'+escape(o.detail)+'</em><span class="rg-card-action">'+escape(o.disabled?'Không khả dụng':o.action)+'</span></button>').join('');
   options.querySelectorAll('[data-rogue-choice]').forEach(button=>button.addEventListener('click',()=>select(button.dataset.rogueChoice)));options.querySelector('button:not(:disabled)')?.focus();
  }
  root.querySelector('[data-rogue-skip]').addEventListener('click',()=>select('skip'));
  overlay.addEventListener('keydown',event=>{if(event.key==='Escape'){event.preventDefault();event.stopPropagation();}if(event.key==='Tab'){const nodes=[...overlay.querySelectorAll('button:not(:disabled)')],first=nodes[0],last=nodes[nodes.length-1];if(event.shiftKey&&document.activeElement===first){event.preventDefault();last.focus();}else if(!event.shiftKey&&document.activeElement===last){event.preventDefault();first.focus();}}});
  root.tankRogue={choose:select};return{render};
 }
 host.TankRogueUI={create,icon,tent};
})(globalThis);
