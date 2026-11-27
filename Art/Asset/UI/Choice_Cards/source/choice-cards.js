(function(host){
 'use strict';
 const assets=/*__CARD_ASSETS__*/{};
 const definitions=[
  {id:'cannon_ap',group:'ammo',slot:'cannon',name:'ARMOR-PIERCING CANNON',nameLines:['ARMOR-PIERCING','CANNON'],tag:'REPLACES CANNON',copy:'Penetrating fire for armored targets.',action:'INSTALL',color:'#377bea',ink:'#225abb',icon:'cannon_ap'},
  {id:'mortar_blast',group:'ammo',slot:'mortar',name:'BLAST MORTAR',tag:'REPLACES MORTAR',copy:'Lob explosive shells into enemy groups.',action:'INSTALL',color:'#ff9b24',ink:'#cb5b0e',icon:'mortar_blast'},
  {id:'mg_plasma',group:'ammo',slot:'mg',name:'PLASMA MG',tag:'REPLACES MACHINE GUN',copy:'Charged plasma fire from your MG slot.',action:'INSTALL',color:'#c84ce9',ink:'#9521b2',icon:'mg_plasma',ad:true},
  {id:'mg',group:'supply',name:'MG SUPPLY',tag:'MACHINE GUN AMMO',copy:'Restock your machine gun.',action:'TAKE AMMO',color:'#ffca2d',ink:'#a06c08',icon:'supply_mg'},
  {id:'cannon',group:'supply',name:'CANNON SUPPLY',tag:'CANNON AMMO',copy:'Restock your cannon.',action:'TAKE AMMO',color:'#ff9c37',ink:'#b85c10',icon:'supply_cannon'},
  {id:'mortar',group:'supply',name:'MORTAR SUPPLY',tag:'MORTAR AMMO',copy:'Restock your mortar.',action:'TAKE AMMO',color:'#8cbf39',ink:'#497918',icon:'supply_mortar'},
  {id:'heal',group:'medical',name:'FIRST AID KIT',tag:'RESTORE HP',copy:"Restore your tank's health.",action:'HEAL',color:'#57c779',ink:'#238943',icon:'medical_kit'},
  {id:'gears',group:'medical',name:'SPARE GEARS',tag:'RESOURCE PICKUP',copy:'Take spare parts for your journey.',action:'TAKE GEARS',color:'#62c5df',ink:'#247c99',icon:'gears'},
  {id:'speed',group:'wreck',name:'SCOUT TANK',tag:'QUICK & NIMBLE',copy:'Switch to the lightweight red tank.',action:'SWAP TANK',color:'#f46650',ink:'#bc392b',icon:'speed'},
  {id:'heavy',group:'wreck',name:'HEAVY TANK',tag:'HEAVY ARMOR',copy:'Switch to the blue armored tank.',action:'SWAP TANK',color:'#538fe5',ink:'#285fae',icon:'heavy'},
  {id:'base-cannon-mg',group:'wreck',name:'CANNON + MG',tag:'TWO WEAPON SLOTS',copy:'No mortar. Its crew member follows on foot.',action:'SWAP TANK',color:'#56c6a5',ink:'#207f67',icon:'base-cannon-mg'},
  {id:'base-cannon-mortar',group:'wreck',name:'CANNON + MORTAR',tag:'TWO WEAPON SLOTS',copy:'No MG. Its crew member follows on foot.',action:'SWAP TANK',color:'#9d8bde',ink:'#6553a4',icon:'base-cannon-mortar'},
  {id:'base-cannon',group:'wreck',name:'CANNON TANK',tag:'ONE WEAPON SLOT',copy:'Cannon only. Spare crew members follow on foot.',action:'SWAP TANK',color:'#d4ae54',ink:'#8e6a20',icon:'base-cannon'},
  {id:'keep',group:'wreck',name:'KEEP MY TANK',tag:'STAY WITH YOUR CREW',copy:'Keep your current tank and loadout.',action:'KEEP TANK',color:'#9eabb9',ink:'#596a7b',icon:'base'}
 ];
 const rarities={common:{name:'COMMON',color:'#b2bdca',ink:'#526071'},rare:{name:'RARE',color:'#39a3ff',ink:'#226cae'},epic:{name:'EPIC',color:'#b56cf3',ink:'#8044af'},legendary:{name:'LEGENDARY',color:'#ffc52c',ink:'#a16a08'},ad:{name:'REWARDED AD',color:'#cb58ed',ink:'#8c3aa8'}};
 const cardRarity={cannon_ap:'common',mortar_blast:'rare',mg_plasma:'ad',mg:'common',cannon:'common',mortar:'common',heal:'rare',gears:'common',speed:'epic',heavy:'legendary','base-cannon-mg':'rare','base-cannon-mortar':'rare','base-cannon':'common',keep:'common'};
 definitions.forEach(d=>d.rarity=cardRarity[d.id]);
 const escape=t=>String(t).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
 const svg=(body,box='0 0 220 220')=>'<svg xmlns="http://www.w3.org/2000/svg" viewBox="'+box+'" aria-hidden="true"><g stroke="#17151d" stroke-width="8" stroke-linejoin="round" stroke-linecap="round">'+body+'</g></svg>';
 function art(name){
  if(assets[name])return '<img src="'+assets[name]+'" alt="" draggable="false">';
  if(name==='heal')return svg('<path d="M78 66V42q0-9 9-9h46q9 0 9 9v24" fill="none"/><path d="m38 64 147-7 8 125-156 4Z" fill="#f8fcff"/><path d="m38 64 15 8-2 101-14 13Z" fill="#a7c8df" stroke="none"/><path d="m66 101 31-2-1-27 34-2 2 28 29-1 2 32-29 2 2 29-34 1-1-29-32 1Z" fill="#ff5369"/><path d="m49 78 23-1" stroke="#fff"/><path d="m158 164 19-1" stroke="#b5cbd7"/>');
  if(name==='piercing')return svg('<path d="m145 87 43-6 7 55-27 31-34-27Z" fill="#a9cddd"/><path d="m158 96 16-2 2 39-10 11-11-12Z" fill="#eef7ff" stroke="none"/><path d="m47 175 38-78 37-40 24 57-38 76Z" fill="#d5e9ff"/><path d="m85 97 37-40 24 57Z" fill="#ecf6ff"/><path d="m47 175 24-46 44 21-7 40Z" fill="#ffca31"/><path d="m55 160 45 22M95 103l15-18" fill="none" stroke-width="6"/><path d="m158 43 8-21 5 19 22-4-14 17 14 9-22 2-7 17-6-20-17-1Z" fill="#fff" stroke="none"/>');
  if(name==='explosive')return svg('<path d="m155 31 7 32 29-9-11 29 25 15-29 13 8 30-28-12-17 29-9-33-33 1 18-26-20-22 31-2 6-32 17 24Z" fill="#ffe946"/><path d="m34 174 34-63 38-39 24 55-37 72Z" fill="#ff6b36"/><path d="m68 111 38-39 24 55Z" fill="#ffe77b"/><path d="m34 174 17-31 59 27-17 29Z" fill="#ffbe27"/><path d="m48 160 50 24m27-117 5 17" fill="none" stroke-width="6"/><path d="m24 95-12-9m32-10-2-17m154 78 10 8" fill="none"/>');
  if(name==='plasma')return svg('<path d="m40 164 26-61 47-46 30 56-37 76Z" fill="#7ceaff"/><path d="m66 103 47-46 30 56Z" fill="#eee3ff"/><path d="m40 164 14-27 65 25-13 27Z" fill="#914be3"/><path d="m54 154 54 20" stroke="#ddb8ff" stroke-width="6"/><path d="m155 28-31 58h24l-21 44 58-65h-29l20-37Z" fill="#fff888"/><path d="m39 69-4-14m132 111 9 10m-25 14 2 12" stroke="#fff" stroke-width="6"/><path d="m182 110 3-11m9 8 10 3" stroke="#9afcff" stroke-width="5"/>');
  return '';
 }
 function swirl(rarity,id){const fill=rarity==='ad'?'url(#cc-prism-'+id+')':'currentColor';return '<svg class="cc-swirl" viewBox="0 0 240 240" aria-hidden="true">'+(rarity==='ad'?'<defs><linearGradient id="cc-prism-'+id+'" x1="0" y1="1" x2="1" y2="0"><stop stop-color="#7064ff"/><stop offset=".27" stop-color="#3ad5ee"/><stop offset=".49" stop-color="#61e1b4"/><stop offset=".67" stop-color="#ffe56b"/><stop offset=".84" stop-color="#ff85ac"/><stop offset="1" stop-color="#d563ef"/></linearGradient></defs>':'')+'<circle cx="120" cy="120" r="91" fill="'+fill+'"/>'+Array.from({length:7},(_,i)=>'<path d="M120 29C156 3 196 30 211 61 176 40 158 48 150 60 182 58 211 76 219 103 199 84 173 90 159 101L120 120Z" transform="rotate('+i*360/7+' 120 120)" fill="'+fill+'"/>').join('')+'<path d="m58 71 4 8 8 3-8 4-4 9-3-9-9-4 9-3Zm117 99 3 6 7 3-7 3-3 7-3-7-7-3 7-3Z" fill="#fff"/></svg>';}
 function scenery(group){const scene=group==='wreck'?assets['scene-city']:assets['scene-tent'];return '<div class="cc-scenery" aria-hidden="true"><img src="'+scene+'" alt=""></div>';}
 function renderCard(input,context={}){
  const def=definitions.find(d=>d.id===input.id)||definitions[0],o={...def,...input},id=escape(o.id),isTank=def.group==='wreck',hero=o.id==='keep'?(assets[o.currentSkin]||assets.base):assets[def.icon],name=def.name,rarity=rarities[o.rarity];
  const desc=o.disabled?(o.id==='heal'?'Your tank is already at full health.':'This weapon is not installed.'):def.copy;
  return '<button type="button" class="cc-card cc-rarity--'+o.rarity+(isTank?' cc-card--tank':'')+(def.group==='ammo'?' cc-card--weapon cc-weapon--'+def.slot:'')+'" data-rogue-choice="'+id+'" style="--cc-color:'+rarity.color+';--cc-ink:'+rarity.ink+'" aria-label="'+escape(name+'. '+desc+(o.ad?' Rewarded ad.':''))+'"'+(o.disabled?' disabled':'')+'>'+
   '<span class="cc-paper" aria-hidden="true"></span>'+scenery(def.group)+'<span class="cc-orbit">'+swirl(o.rarity,id)+'</span><span class="cc-rarity-label">'+rarity.name+'</span>'+
   '<span class="cc-info" title="'+escape(desc)+'" aria-hidden="true">i</span>'+
   '<span class="cc-hero" aria-hidden="true">'+(hero?'<img src="'+hero+'" alt="" draggable="false">':art(def.icon))+'</span>'+
   '<strong class="cc-name"><span>'+(def.nameLines?def.nameLines.map(escape).join('<br>'):escape(name))+'</span></strong><span class="cc-tag">'+escape(def.tag)+'</span><span class="cc-copy">'+escape(desc)+'</span>'+
   '<span class="cc-card-action">'+(o.ad?'<span class="cc-ad">▶ AD</span>':'')+escape(o.disabled?'UNAVAILABLE':def.action)+'<b aria-hidden="true">›</b></span></button>';
 }
 function render(o,event){return renderCard({...o,currentSkin:event.currentSkin});}
 function heading(e){const map={ammo:['MILITARY CAMP','CHOOSE YOUR WEAPON','Replace one weapon in its matching slot.'],supply:['SUPPLY STOP','RESTOCK & ROLL','Choose one weapon to resupply.'],medical:['MEDICAL STOP','PATCH UP OR STOCK UP','Restore your health or collect spare parts.'],wreck:['ABANDONED TANK','TIME FOR A NEW RIDE?','Swap tanks or keep your current ride.']};return map[e.type]||map.ammo;}
 function renderBase(id){const r=rarities[id];return '<div class="cc-base-wrap"><div class="cc-card cc-rarity--'+id+' cc-base" style="--cc-color:'+r.color+';--cc-ink:'+r.ink+'"><span class="cc-paper"></span><span class="cc-orbit">'+swirl(id,'base-'+id)+'</span><span class="cc-rarity-label">'+r.name+'</span><span class="cc-base-title-lines" aria-hidden="true"></span><span class="cc-card-action">'+(id==='ad'?'<span class="cc-ad">▶ AD</span>':'')+'<b>›</b></span></div><h3>'+r.name+'</h3></div>';}
 host.TankChoiceCards={definitions,rarities,renderCard,render,heading,renderBase};
})(globalThis);
