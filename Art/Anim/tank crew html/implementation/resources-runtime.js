(function(host){
 'use strict';
 const root=document.getElementById('tank-autobattle-flow');
 const initial=JSON.parse(document.getElementById('tank-resource-data').textContent);
 const balances={money:0,gears:0,chests:0},credited=new WeakMap();let readyResolve;
 const labels={money:'Tiền',gears:'Bánh răng',chests:'Rương quà'},format=new Intl.NumberFormat('vi-VN');
 function amount(value){const n=Number(value);return Number.isFinite(n)?Math.min(Number.MAX_SAFE_INTEGER,Math.max(0,Math.floor(n))):0;}
 function set(next,persist=true){
  if(!next||typeof next!=='object')return {...balances};
  for(const key of ['money','gears','chests']){
   if(Object.prototype.hasOwnProperty.call(next,key))balances[key]=amount(next[key]);
   for(const node of root.querySelectorAll('[data-resource-value="'+key+'"]'))node.textContent=format.format(balances[key]);
   for(const card of root.querySelectorAll('[data-resource-card="'+key+'"]'))card.setAttribute('aria-label',labels[key]+': '+format.format(balances[key]));
  }
  if(persist&&parent!==window)parent.postMessage({type:'tank:resources:save',balances:{...balances}},'*');
  return {...balances};
 }
 function creditBattle(state){
  if(!state.lootWallet)return;const before=credited.get(state)||{money:0,gears:0,chests:0},next={...balances};let changed=false;
  for(const key of ['money','gears','chests']){const delta=state.lootWallet[key]-before[key];next[key]+=delta;changed||=delta!==0;}
  credited.set(state,{...state.lootWallet});if(changed)set(next);
 }
 const api={get:()=>({...balances}),set,creditBattle,ready:new Promise(resolve=>readyResolve=resolve)};
 root.tankResources=api;host.TankResources=api;
 root.addEventListener('tank:resources',event=>set(event.detail));
 set(initial,false);
 addEventListener('message',event=>{if(event.source===parent&&event.data?.type==='tank:resources:restore'){set(event.data.balances,false);readyResolve();}});
 if(parent!==window)parent.postMessage({type:'tank:resources:load'},'*');else readyResolve();
})(globalThis);
