(function(){
 const frame=document.getElementById('codex-visualization'),key='megatank:resources:v1:'+location.pathname;
 const clean=data=>Object.fromEntries(['money','gears','chests'].map(k=>[k,Number.isFinite(Number(data?.[k]))?Math.max(0,Math.min(Number.MAX_SAFE_INTEGER,Math.floor(Number(data[k])))):0]));
 let saved={money:0,gears:0,chests:0};try{saved=clean(JSON.parse(localStorage.getItem(key)||'{}'));}catch{}
 addEventListener('message',event=>{
  if(event.source!==frame.contentWindow)return;
  if(event.data?.type==='tank:resources:load')frame.contentWindow.postMessage({type:'tank:resources:restore',balances:saved},'*');
  if(event.data?.type==='tank:resources:save'){saved=clean(event.data.balances);try{localStorage.setItem(key,JSON.stringify(saved));}catch{}}
 });
})();
