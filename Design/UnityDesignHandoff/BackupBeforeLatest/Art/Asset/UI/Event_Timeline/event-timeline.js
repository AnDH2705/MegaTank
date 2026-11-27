(function(host){
 'use strict';
 const TYPES=['battle','hard_battle','ammo','unknown','hp','boss'],LABELS={battle:'BATTLE',hard_battle:'HARD BATTLE',ammo:'AMMO',unknown:'UNKNOWN',hp:'HP',boss:'BOSS'};
 const X={current:320,middle:460,boss:580},NS='http://www.w3.org/2000/svg';let instance=0;
 const clamp=v=>Math.max(0,Math.min(1,Number(v)||0)),ease=t=>1-Math.pow(1-clamp(t),3),mix=(a,b,t)=>a+(b-a)*t;
 function validate(route){if(!Array.isArray(route)||route.length<3||route.at(-1).type!=='boss'||route.slice(0,-1).some(e=>e.type==='boss')||route.some(e=>!e.id||!TYPES.includes(e.type))||new Set(route.map(e=>e.id)).size!==route.length)throw Error('Route needs at least two unique event IDs and one final boss.');return route;}
 function layout(route,cursor=0,progress=0){
  validate(route);const n=route.length-1,c=Math.max(0,Math.min(n,Math.floor(Number(cursor)||0))),p=clamp(progress),first=Math.min(c,n-2);
  const nodes=[first,first+1].map((index,slot)=>{const completed=index<c;let x;if(c>=n)x=slot?180:72;else if(c===n-1)x=slot?X.current:112;else x=slot?mix(X.boss,X.middle,p):X.current;return {...route[index],index,x,y:82,completed,active:index===c};});
  const bossX=c===n?X.current:X.boss;nodes.push({...route[n],index:n,x:bossX,y:82,completed:false,active:c===n});return {nodes,cursor:c,progress:p,bossX,arrowX:X.current};
 }
 function el(tag,attrs={}){const e=host.document.createElementNS(NS,tag);for(const[k,v]of Object.entries(attrs))e.setAttribute(k,v);return e;}
 class EventTimeline{
  constructor(container,assets){
   this.container=container;this.assets=assets;this.route=null;this.slots=[];this.clock=0;this.lastNow=null;this.lastCursor=null;this.visual=null;this.transition=null;
   const id='event-route-clip-'+ ++instance,svg=el('svg',{viewBox:'0 0 640 184',role:'img','aria-label':'Sự kiện tiếp theo'});svg.style.cssText='display:block;width:100%;height:auto;overflow:visible';
   svg.innerHTML='<defs><clipPath id="'+id+'"><rect x="0" y="26" width="580" height="118"/></clipPath></defs><path d="M60 82H580" fill="none" stroke="#17151d" stroke-width="28" stroke-linecap="round"/>';
   const outer=el('g',{'clip-path':'url(#'+id+')'});svg.append(outer);this.outer=outer;
   for(let i=0;i<2;i++){const edge=el('g');edge.append(el('circle',{r:40,fill:'#17151d'}));outer.append(edge);this.slots.push({edge,id:null});}
   const bossEdge=el('g');bossEdge.append(el('circle',{r:54,fill:'#17151d'}));svg.append(bossEdge);this.bossEdge=bossEdge;
   svg.append(el('path',{d:'M60 82H580',fill:'none',stroke:'#fff5df','stroke-width':16,'stroke-linecap':'round'}));
   svg.append(el('path',{d:'M60 82H320',fill:'none',stroke:'#ffcf3c','stroke-width':6,'stroke-linecap':'round'}));
   const moving=el('g',{'clip-path':'url(#'+id+')'});svg.append(moving);
   for(let i=0;i<2;i++){
    const slot=this.slots[i],g=el('g',{'data-event-node':'','data-slot':i}),ring=el('circle',{r:32,fill:'#fff5df'}),image=el('image',{x:-38,y:-38,width:76,height:76}),tick=el('path',{d:'m13 24 7 7 13-17',fill:'none',stroke:'#70e7a2','stroke-width':7,'stroke-linecap':'round','stroke-linejoin':'round'});g.append(ring,image,tick);moving.append(g);Object.assign(slot,{g,ring,image,tick});
   }
   const boss=el('g',{'data-event-node':'','data-event-boss':''}),bossRing=el('circle',{r:46,fill:'#fff5df'}),image=el('image',{x:-51,y:-51,width:102,height:102});image.setAttribute('href',assets.boss);boss.append(bossRing,image);svg.append(boss);this.boss=boss;this.bossRing=bossRing;
   const bossLabel=el('text',{y:158,'text-anchor':'middle',fill:'#fff7e9',stroke:'#17151d','stroke-width':5,'paint-order':'stroke','font-family':'Arial,sans-serif','font-style':'italic','font-weight':900,'font-size':22});bossLabel.textContent='BOSS';svg.append(bossLabel);this.bossLabel=bossLabel;
   svg.append(el('path',{'data-event-pointer':'',d:'m306 15h28L320 35Z',fill:'#ffd43b',stroke:'#17151d','stroke-width':4,'stroke-linejoin':'round'}));
   const textAttrs={x:320,'text-anchor':'middle',fill:'#fff8ea',stroke:'#17151d','paint-order':'stroke','font-family':'Arial,sans-serif','font-size':22,'font-weight':900,'font-style':'italic'};
   this.heading=el('text',{...textAttrs,y:150,'stroke-width':5});this.caption=el('text',{...textAttrs,y:176,'stroke-width':4});svg.append(this.heading,this.caption);container.append(svg);this.svg=svg;
  }
  setRoute(route){validate(route);this.route=route.map(e=>({...e}));this.lastCursor=null;this.visual=null;this.transition=null;this.lastNow=null;this.slots.forEach(s=>{s.id=null;});}
  positions(next){
   const tr=this.transition;if(!tr)return next;
   const n=this.route.length-1,t=(this.clock-tr.start)/.32,e=ease(t),old=tr.from;
   if(t<1&&tr.kind==='advance'){
    const outgoing={...this.route[tr.cursor-1],index:tr.cursor-1,x:mix(old.nodes.find(v=>v.index===tr.cursor-1)?.x??320,-64,e),y:82,completed:true,active:false};
    const incoming={...this.route[tr.cursor],index:tr.cursor,x:mix(old.nodes.find(v=>v.index===tr.cursor)?.x??580,320,e),y:82,completed:false,active:true};
    return {...next,nodes:[outgoing,incoming,next.nodes[2]]};
   }
   if(t<1){return {...next,nodes:next.nodes.map(node=>({...node,x:mix(old.nodes.find(v=>v.id===node.id)?.x??580,node.x,e)})),bossX:mix(old.bossX,next.bossX,e)};}
   if(tr.kind==='advance'&&t<1.56){const born=(this.clock-tr.start-.32)/.18;return {...next,nodes:next.nodes.map(node=>node.index===tr.cursor+1&&node.index<n?{...node,x:mix(580,node.x,ease(born))}:node)};}
   this.transition=null;return next;
  }
  render(state){
   if(!this.route)throw Error('Set route first');const now=(host.performance?.now()??Date.now())/1000;if(this.lastNow!==null&&!state.paused)this.clock+=Math.max(0,Math.min(.05,now-this.lastNow));this.lastNow=now;
   const next=layout(this.route,state.cursor,state.progress),n=this.route.length-1;
   if(this.lastCursor!==null&&this.lastCursor!==next.cursor&&this.visual){this.transition={start:this.clock,from:this.visual,cursor:next.cursor,kind:next.cursor===this.lastCursor+1&&next.cursor<n-1?'advance':'settle'};}
   this.lastCursor=next.cursor;
   if(host.matchMedia?.('(prefers-reduced-motion: reduce)').matches)this.transition=null;
   const visual=this.positions(next),used=new Set();
   for(const node of visual.nodes.slice(0,2)){
    let slot=this.slots.find(s=>s.id===node.id&&!used.has(s));if(!slot)slot=this.slots.find(s=>!used.has(s)&&!visual.nodes.slice(0,2).some(v=>v.id===s.id));if(!slot)slot=this.slots.find(s=>!used.has(s));used.add(slot);
    if(slot.id!==node.id)slot.image.setAttribute('href',this.assets[node.type]);
    const transform='translate('+node.x+' 82)';slot.g.setAttribute('transform',transform);slot.edge.setAttribute('transform',transform);slot.g.style.opacity=node.completed?'.42':'1';
    slot.ring.setAttribute('fill',node.active?'#ffda60':'#fff5df');slot.tick.style.display=node.completed?'':'none';
    slot.g.dataset.eventId=node.id;slot.g.dataset.eventType=node.type;slot.g.dataset.eventX=node.x;slot.g.setAttribute('aria-label',LABELS[node.type]+(node.completed?' · đã qua':''));slot.id=node.id;
   }
   const bx=visual.nodes[2].x;this.boss.setAttribute('transform','translate('+bx+' 82)');this.bossEdge.setAttribute('transform','translate('+bx+' 82)');this.boss.dataset.eventId=next.nodes[2].id;this.boss.dataset.eventType='boss';this.boss.dataset.eventX=bx;
   this.bossRing.setAttribute('fill',next.nodes[2].active?'#ffda60':'#fff5df');this.bossLabel.setAttribute('x',bx);
   this.heading.textContent=next.cursor===n?'':state.heading||'WAVE';this.caption.textContent=next.cursor===n?'':state.caption||'';
   this.svg.setAttribute('aria-label',(state.heading||'Sự kiện')+' '+(state.caption||'')+'. '+next.nodes.map(v=>LABELS[v.type]).join(', '));this.visual={...visual,bossX:bx};this.state=next;return next;
  }
  destroy(){this.svg.remove();}
 }
 const api={TYPES,LABELS,X,layout,EventTimeline};host.TankEventTimeline=api;if(typeof module!=='undefined'&&module.exports)module.exports=api;
})(globalThis);
