(function(){
 'use strict';
 const model=JSON.parse(document.getElementById('heli-model').textContent),pilotModel=JSON.parse(document.getElementById('pilot-model').textContent),effects=JSON.parse(document.getElementById('vfx-model').textContent),sources=JSON.parse(document.getElementById('heli-images').textContent),canvas=document.querySelector('canvas'),ctx=canvas.getContext('2d'),images={};
 let mode='combined',time=0,playing=true,last=0,onMap=false,pivots=false;
 const o=model.origin_px,gunPart=model.layers.find(p=>p.id==='machine_gun'),getShots=view=>view==='fly'?[]:Array.from({length:view==='fire'?25:16},(_,i)=>view==='fire'?.1+i*.15:i<9?1.2+i*.13:2.9+(i-9)*.11);
 function pose(at){return{bob:Math.sin(at*Math.PI*2/2.1)*11,bank:Math.sin(at*Math.PI*2/4.2)*.018,aim:model.gun.demo_angle_radians+Math.sin(at*2)*.018};}
 function nativeMuzzle(p){const q=model.gun.pivot_px,d=model.gun.muzzle_px,dx=d[0]-q[0],dy=d[1]-q[1];return{x:q[0]+Math.cos(p.aim)*dx-Math.sin(p.aim)*dy,y:q[1]+Math.sin(p.aim)*dx+Math.cos(p.aim)*dy};}
 function worldPoint(pt,p){const x=pt.x-o[0],y=pt.y-o[1];return{x:1600-(o[0]+Math.cos(p.bank)*x-Math.sin(p.bank)*y),y:o[1]+p.bob+Math.sin(p.bank)*x+Math.cos(p.bank)*y};}
 function part(p,angle=0,sx=1,alpha=1){const q=p.pivot_canvas_px;ctx.save();ctx.globalAlpha*=alpha;ctx.translate(q[0],q[1]);ctx.rotate(angle);ctx.scale(sx,1);ctx.drawImage(images[p.part],p.position_px[0]-q[0],p.position_px[1]-q[1],p.size_px[0],p.size_px[1]);ctx.restore();}
 function pilot(t,firing){const name=firing?'fire':'fly',clip=pilotModel.clips[name],im=images['pilot_'+name],frame=Math.floor((t%clip.duration)/clip.duration*clip.frames),sw=im.width/clip.cols,sh=im.height/clip.rows,z=model.pilot.preview_scale,q=model.pilot.socket_px;ctx.drawImage(im,frame%clip.cols*sw,Math.floor(frame/clip.cols)*sh,sw,sh,q[0]-clip.pivot[0]*z,q[1]-clip.pivot[1]*z,clip.cell[0]*z,clip.cell[1]*z);}
 function sprite(name,t,x,y,w,h,angle=0,alpha=1,pivot=[.5,.5]){const clip=effects[name],im=images['vfx_'+name],frame=Math.min(clip.count-1,Math.max(0,Math.floor(t*clip.fps))),sw=im.width/clip.cols,sh=im.height/clip.rows;ctx.save();ctx.translate(x,y);ctx.rotate(angle);ctx.globalAlpha*=alpha;ctx.drawImage(im,frame%clip.cols*sw,Math.floor(frame/clip.cols)*sh,sw,sh,-w*pivot[0],-h*pivot[1],w,h);ctx.restore();}
 function render(at=time,view=mode,{transparent=false,map=onMap,debug=pivots}={}){
  const p=pose(at),shots=getShots(view),occurred=shots.filter(s=>s<=at),age=occurred.length?at-occurred[occurred.length-1]:99,flash=age<.065,firing=occurred.length>0&&age<.18,mainScale=.2+.8*Math.abs(Math.cos(at*48)),tailAngle=at*36,mouth=nativeMuzzle(p),enemyMouth=worldPoint(mouth,p);
  ctx.clearRect(0,0,1600,1000);ctx.save();
  if(!transparent){if(map){const w=1000*images.background.width/images.background.height;ctx.drawImage(images.background,(1600-w)/2,0,w,1000);}else{ctx.fillStyle='#f5efdf';ctx.fillRect(0,0,1600,1000);}}
  if(map){ctx.translate(1000,450);ctx.scale(.34,.34);ctx.translate(-o[0],-o[1]);}
  ctx.save();ctx.translate(1600,0);ctx.scale(-1,1);ctx.translate(o[0],o[1]+p.bob);ctx.rotate(p.bank);ctx.translate(-o[0],-o[1]);
  let pilotDrawn=false;
  for(const item of model.layers){
   if(!pilotDrawn&&item.z_order>=model.pilot.draw_order){pilot(at,firing);pilotDrawn=true;}
   if(item.id==='main_rotor'){part(item,0,.2+.8*Math.abs(Math.cos(at*48-1)),.12);part(item,0,.2+.8*Math.abs(Math.cos(at*48+1)),.12);part(item,0,mainScale);}
   else if(item.id==='tail_rotor')part(item,tailAngle);
   else if(item.id==='machine_gun')part(item,p.aim);
   else part(item);
  }
  if(flash){const u=age/.065;sprite('Muzzle',age,mouth.x,mouth.y,98*(1-u*.25),65*(1-u*.25),p.aim,1,[.1,.5]);sprite('Glow',0,mouth.x,mouth.y,65,65,0,.23);}
  for(let i=0;i<occurred.length;i+=3){const u=(at-occurred[i])/.5;if(u<0||u>1)continue;sprite('Smoke',u*.5,mouth.x+u*8,mouth.y-u*28,37+u*28,37+u*28,0,.35*(1-u));}
  if(debug){ctx.strokeStyle='#22a6b4';ctx.lineWidth=2;ctx.font='17px sans-serif';ctx.fillStyle='#146675';for(const item of model.layers){const q=item.pivot_canvas_px;ctx.beginPath();ctx.moveTo(q[0]-8,q[1]);ctx.lineTo(q[0]+8,q[1]);ctx.moveTo(q[0],q[1]-8);ctx.lineTo(q[0],q[1]+8);ctx.stroke();}ctx.beginPath();ctx.arc(mouth.x,mouth.y,9,0,Math.PI*2);ctx.stroke();}
  ctx.restore();
  let tracerCount=0;
  for(let i=0;i<occurred.length;i++){const shot=occurred[i],a=at-shot;if(a>.72)continue;const sp=pose(shot),start=worldPoint(nativeMuzzle(sp),sp),angle=Math.PI-sp.aim-sp.bank,travel=48+a*1120,x=start.x+Math.cos(angle)*travel,y=start.y+Math.sin(angle)*travel;sprite('Beam',0,x,y,68,7,angle,.95);tracerCount++;}
  ctx.restore();
  const result={mode:view,time:at,pilot_count:1,firing,flash,shot_age:age,tracers:tracerCount,main_rotor_scale:mainScale,tail_rotor_angle:tailAngle,muzzle:enemyMouth,gun_angle:p.aim,aim_direction:{x:-Math.cos(p.aim+p.bank),y:Math.sin(p.aim+p.bank)}};window.HelicopterPreview.last=result;return result;
 }
 function ui(){document.querySelector('[data-time]').value=String(time);document.querySelector('[data-clock]').textContent=time.toFixed(2)+'s';document.querySelector('[data-state]').textContent=window.HelicopterPreview.last.firing?'BẮN LOẠT MG · CHÉO XUỐNG PHÍA TRƯỚC':'BAY LƠ LỬNG · CÁNH QUẠT QUAY';}
 const ready=Promise.all(Object.entries(sources).map(([key,src])=>new Promise((resolve,reject)=>{const im=new Image();im.onload=()=>{images[key]=im;resolve();};im.onerror=()=>reject(Error('Missing '+key));im.src=src;})));
 window.HelicopterPreview={ready,model,last:null,render,set:(view,at=0)=>{mode=view;time=at;playing=false;document.querySelector('[data-play]').textContent='Chạy tiếp';for(const b of document.querySelectorAll('[data-mode]'))b.classList.toggle('selected',b.dataset.mode===view);render();ui();},capture:(at,view)=>{render(at,view,{transparent:true,map:false,debug:false});return canvas.toDataURL('image/png');}};
 for(const b of document.querySelectorAll('[data-mode]'))b.addEventListener('click',()=>{mode=b.dataset.mode;time=0;playing=true;document.querySelector('[data-play]').textContent='Tạm dừng';for(const n of document.querySelectorAll('[data-mode]'))n.classList.toggle('selected',n===b);});
 document.querySelector('[data-play]').addEventListener('click',()=>{playing=!playing;document.querySelector('[data-play]').textContent=playing?'Tạm dừng':'Chạy tiếp';});document.querySelector('[data-time]').addEventListener('input',e=>{time=Number(e.target.value);playing=false;render();ui();});document.querySelector('[data-map]').addEventListener('change',e=>{onMap=e.target.checked;render();});document.querySelector('[data-pivot]').addEventListener('change',e=>{pivots=e.target.checked;render();});
 ready.then(()=>{document.body.dataset.ready='true';function tick(now){if(playing)time=(time+Math.min(.05,(now-(last||now))/1000))%4.2;last=now;render();ui();requestAnimationFrame(tick);}requestAnimationFrame(tick);});
})();
