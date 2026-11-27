(function(host){
 'use strict';
 function routeFor(s){
  const route=[],r=s.rogue;let stop=0;if(r.entryUpgrade)route.push({id:"stop-0",type:"upgrade",wave:0,encounterType:"upgrade"});
  for(let start=1;start<=r.waveCount;start+=2){
   const end=Math.min(start+1,r.waveCount);
   route.push({id:'battle-'+Math.ceil(start/2),type:s.config.index>=1?'hard_battle':'battle',waveStart:start,waveEnd:end});
   if(end%2===0){const type=r.deck[stop++%r.deck.length];route.push({id:'stop-'+end,type:type==='upgrade'?'upgrade':type==='medical'?'hp':type==='wreck'?'unknown':'ammo',wave:end,encounterType:type});}
  }
  route.push({id:'boss',type:'boss'});return route;
 }
 function create(root){
  const assets=JSON.parse(root.ownerDocument.getElementById('tank-comic-hud-assets').textContent),battle=root.querySelector('[data-battle-view]');
  const layer=root.ownerDocument.createElement('div');layer.className='comic-combat-hud';
  layer.innerHTML='<div class="comic-event-hud" data-comic-event-hud></div><div class="comic-bottom-hud"><div class="comic-player-hp"><svg viewBox="0 0 1000 112" role="progressbar" aria-label="Máu tank" aria-valuemin="0" data-comic-health><defs><clipPath id="comic-health-clip"><path d="M20 39H980L970 57H20Z"/></clipPath></defs><image href="'+assets.hp_panel+'" width="1000" height="112"/><image href="'+assets.hp_rail+'" x="20" y="39" width="960" height="18"/><g clip-path="url(#comic-health-clip)"><rect data-comic-hp-chip x="20" y="39" width="960" height="18" fill="#ffe5a0"/><rect data-comic-hp-fill x="20" y="39" width="960" height="18" fill="#54dc91"/></g><text data-comic-hp-number x="500" y="86" text-anchor="middle" fill="#fff8e8" font-family="Arial,sans-serif" font-weight="900" font-style="italic" font-size="27"></text><image href="'+assets.hp_frame+'" width="1000" height="112"/><image href="'+assets.hp_label+'" width="118" height="42"/></svg></div><div class="comic-ammo-hud" aria-label="Đạn còn lại">'+['cannon','mg','mortar'].map(w=>'<div class="comic-ammo-counter" data-comic-ammo="'+w+'"><img src="'+assets['ammo_'+w]+'" alt="'+({cannon:'Đạn pháo',mg:'Đạn MG',mortar:'Đạn mortar'}[w])+'"><strong>0</strong></div>').join('')+'</div></div>';
  battle.append(layer);root.querySelector('[data-bt-pause]').innerHTML='<img src="'+assets.menuButton+'" alt="">';const timeline=new host.TankEventTimeline.EventTimeline(layer.querySelector('[data-comic-event-hud]'),assets.events),$=s=>layer.querySelector(s);
  let stateRef=null,route=null,cursor=-1,entryDistance=0,chip=1,lastHP=1,hold=0,lastTime=0;
  function render(s,paused){
   if(!s||!s.rogue)return;const r=s.rogue,p=s.player,ratio=Math.max(0,Math.min(1,p.hp/(p.maxHP||1)));
   if(s!==stateRef){stateRef=s;route=routeFor(s);timeline.setRoute(route);cursor=-1;entryDistance=s.distance;chip=ratio;lastHP=ratio;lastTime=s.time;}
   const dt=Math.max(0,Math.min(.05,s.time-lastTime));lastTime=s.time;
   if(ratio<lastHP){chip=Math.max(chip,lastHP);hold=s.time+.18;}else if(ratio>lastHP)chip=ratio;
   if(s.time>=hold&&chip>ratio)chip=Math.max(ratio,chip-dt*.7);lastHP=ratio;
   $('[data-comic-hp-fill]').setAttribute('width',String(960*ratio));$('[data-comic-hp-fill]').setAttribute('fill',ratio<=.25?'#ff655c':ratio<=.5?'#ffd43b':'#54dc91');$('[data-comic-hp-chip]').setAttribute('width',String(960*Math.max(ratio,chip)));
   const health=$('[data-comic-health]');health.setAttribute('aria-valuemax',p.maxHP);health.setAttribute('aria-valuenow',Math.ceil(p.hp));$('[data-comic-hp-number]').textContent=Math.ceil(p.hp)+' / '+p.maxHP+' HP';
   for(const w of ['cannon','mg','mortar']){const el=layer.querySelector('[data-comic-ammo="'+w+'"]');el.querySelector('strong').textContent=p.ammo[w];el.classList.toggle('is-empty',p.ammo[w]===0);el.classList.toggle('is-unequipped',!p.weapons.includes(w));}
   const isBoss=r.bossReady||s.bossSpawned||s.phase.startsWith('boss')||s.phase==='won';let nextCursor;
   if(isBoss)nextCursor=route.length-1;
   else if(s.phase==='encounter_travel'||s.phase==='encounter'||s.phase==='encounter_depart'||s.phase==='upgrade_stop')nextCursor=route.findIndex(e=>e.id==='stop-'+r.wave);
   else nextCursor=route.findIndex(e=>e.waveStart<=r.wave&&e.waveEnd>=r.wave);
   if(nextCursor<0)nextCursor=0;
   if(nextCursor!==cursor){cursor=nextCursor;entryDistance=s.distance;}
   // Cosmetic spacing only. Read real travelled distance; no wall-clock drift while the tank is held.
   const progress=1-Math.exp(-Math.max(0,s.distance-entryDistance)/520);
   const event=route[cursor],heading=isBoss?'BOSS':event.type==='battle'||event.type==='hard_battle'?'WAVE':host.TankEventTimeline.LABELS[event.type],caption=heading==='WAVE'?String(r.wave-event.waveStart+1).padStart(2,'0')+' / '+String(event.waveEnd-event.waveStart+1).padStart(2,'0'):'';
   timeline.render({cursor,progress,heading,caption,paused:!!paused});
   layer.dataset.cursor=cursor;layer.dataset.travel=String(s.distance);layer.dataset.stopped=String(!s.travelSpeed);
  }
  root.tankComicHUD={timeline,getRoute:()=>route,render:s=>render(s,false)};return{render};
 }
 host.TankComicHUD={create,routeFor};
})(globalThis);
