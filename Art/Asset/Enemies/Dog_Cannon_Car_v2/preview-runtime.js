(function(){
 'use strict';
 const model=JSON.parse(document.getElementById('car-model').textContent),driverModel=JSON.parse(document.getElementById('driver-model').textContent),effects=JSON.parse(document.getElementById('vfx-model').textContent),sources=JSON.parse(document.getElementById('car-images').textContent),canvas=document.querySelector('canvas'),ctx=canvas.getContext('2d'),images={};
 const clamp=(v,a=0,b=1)=>Math.max(a,Math.min(b,v)),shots=[1.2,2.45];let mode='combined',time=0,playing=true,last=0,onMap=false,pivots=false;
 function drawPart(p,angle=0){const q=p.pivot_canvas_px;ctx.save();ctx.translate(q[0],q[1]);ctx.rotate(angle);ctx.drawImage(images[p.part],p.position_px[0]-q[0],p.position_px[1]-q[1],p.size_px[0],p.size_px[1]);ctx.restore();}
 function driver(t){const clip=driverModel.clips.drive,im=images.driver_drive,index=Math.floor((t%clip.duration)/clip.duration*clip.frames),sw=im.width/clip.cols,sh=im.height/clip.rows,z=model.driver.preview_scale,q=model.driver.socket_px;ctx.drawImage(im,index%clip.cols*sw,Math.floor(index/clip.cols)*sh,sw,sh,q[0]-clip.pivot[0]*z,q[1]-clip.pivot[1]*z,clip.cell[0]*z,clip.cell[1]*z);}
 function sprite(name,t,x,y,w,h,rotation=0,alpha=1,pivot=[.5,.5]){const clip=effects[name],im=images['vfx_'+name],frame=Math.min(clip.count-1,Math.max(0,Math.floor(t*clip.fps))),sw=im.width/clip.cols,sh=im.height/clip.rows;ctx.save();ctx.translate(x,y);ctx.rotate(rotation);ctx.globalAlpha*=alpha;ctx.drawImage(im,frame%clip.cols*sw,Math.floor(frame/clip.cols)*sh,sw,sh,-w*pivot[0],-h*pivot[1],w,h);ctx.restore();}
 function muzzle(angle,recoil,hullY){const p=model.gun.pivot_px,tip=model.gun.muzzle_px,dx=tip[0]-p[0]-recoil,dy=tip[1]-p[1];return{x:p[0]+Math.cos(angle)*dx-Math.sin(angle)*dy,y:p[1]+Math.sin(angle)*dx+Math.cos(angle)*dy+hullY};}
 function render(at=time,view=mode,{transparent=false,map=onMap,debug=pivots}={}){
  const firing=view!=='drive',shot=firing?shots.filter(s=>at>=s).pop():undefined,age=shot===undefined?99:at-shot,recoil=age<.35?Math.sin(Math.PI*clamp(age/.35))*18:0,hullY=age<.22?Math.sin(Math.PI*age/.22)*4:0,aim=0,drive=view==='drive'||view==='combined'&&(at<.9||at>3.2),wheelAngle=(view==='drive'?at:Math.min(at,.9)+Math.max(0,at-3.2))*6;
  ctx.clearRect(0,0,1600,1000);ctx.save();if(age<.14){const kick=5*Math.pow(1-age/.14,2);ctx.translate(Math.sin(age*110)*kick,Math.cos(age*130)*kick*.5);}
  if(!transparent){if(map){const w=1000*images.background.width/images.background.height;ctx.drawImage(images.background,(1600-w)/2,0,w,1000);}else{ctx.fillStyle='#f5efdf';ctx.fillRect(0,0,1600,1000);ctx.strokeStyle='#d4c8af';ctx.beginPath();ctx.moveTo(40,model.origin_px[1]);ctx.lineTo(1560,model.origin_px[1]);ctx.stroke();}}
  ctx.save();if(map){ctx.translate(1000,865);ctx.scale(.34,.34);ctx.translate(-model.origin_px[0],-model.origin_px[1]);}ctx.translate(1600,0);ctx.scale(-1,1);
  if(!transparent){ctx.fillStyle='rgba(35,31,26,.16)';ctx.beginPath();ctx.ellipse(690,model.origin_px[1]-1,490,18,0,0,Math.PI*2);ctx.fill();}
  let dogDrawn=false;
  for(const p of model.layers){
   if(!dogDrawn&&p.z_order>=model.driver.draw_order){ctx.save();ctx.translate(0,hullY);driver(at);ctx.restore();dogDrawn=true;}
   ctx.save();if(p.parent!=='root')ctx.translate(0,hullY);
   if(p.id==='barrel'){const q=model.gun.pivot_px;ctx.translate(q[0],q[1]);ctx.rotate(aim);ctx.translate(-q[0]-recoil,-q[1]);drawPart(p);}
   else drawPart(p,p.id.startsWith('wheel_')?wheelAngle:0);ctx.restore();
  }
  const mouth=muzzle(aim,recoil,hullY),origin=muzzle(aim,0,0);
  if(firing)for(const shot of shots){const age=at-shot;if(age<0)continue;
   if(age<.26){const u=age/.26;sprite('Muzzle',age,mouth.x,mouth.y,210*(1-u*.6),145*(1-u*.6),aim,1-u*u,[.12,.5]);sprite('Glow',0,mouth.x,mouth.y,115,115,0,.24*(1-u));}
   for(let i=0;i<4;i++){const u=(age-i*.025)/.65;if(u<0||u>1)continue;const size=(75+i*15)*(.7+u*.7);sprite('Smoke',u*.6,origin.x+i*15+u*30,origin.y-i*8-u*48,size,size,0,.65*(1-u));}
   if(age<1.1){const im=images.cannon_round,w=82,h=w*im.height/im.width,x=origin.x+Math.cos(aim)*(90+age*930),y=origin.y+Math.sin(aim)*(90+age*930);ctx.save();ctx.translate(x,y);ctx.rotate(aim);ctx.drawImage(im,-w/2,-h/2,w,h);ctx.restore();}
  }
  if(debug){ctx.strokeStyle='#24b7c6';ctx.fillStyle='#176777';ctx.font='17px sans-serif';ctx.lineWidth=2;for(const p of model.layers){const q=p.pivot_canvas_px;ctx.beginPath();ctx.moveTo(q[0]-8,q[1]);ctx.lineTo(q[0]+8,q[1]);ctx.moveTo(q[0],q[1]-8);ctx.lineTo(q[0],q[1]+8);ctx.stroke();}ctx.beginPath();ctx.arc(mouth.x,mouth.y,10,0,Math.PI*2);ctx.stroke();}
  ctx.restore();ctx.restore();
  const result={mode:view,time:at,driver_count:1,driving:drive,shotAge:age,muzzle:mouth,recoil,flash:age>=0&&age<.26};window.DogCannonPreview.last=result;return result;
 }
 function ui(){document.querySelector('[data-time]').value=String(time);document.querySelector('[data-clock]').textContent=time.toFixed(2)+'s';const s=window.DogCannonPreview.last;document.querySelector('[data-state]').textContent=s.flash?'KHAI HỎA · GIẬT NÒNG':s.driving?'CHÓ LÁI XE · BÁNH LĂN':'DỪNG XE · NGẮM VÀ NẠP PHÁO';}
 const ready=Promise.all(Object.entries(sources).map(([key,src])=>new Promise((resolve,reject)=>{const im=new Image();im.onload=()=>{images[key]=im;resolve();};im.onerror=()=>reject(Error('Missing '+key));im.src=src;})));
 window.DogCannonPreview={ready,model,last:null,render,set:(view,at=0)=>{mode=view;time=at;playing=false;render();ui();},capture:(at,view)=>{render(at,view,{transparent:true,map:false,debug:false});return canvas.toDataURL('image/png');}};
 for(const b of document.querySelectorAll('[data-mode]'))b.addEventListener('click',()=>{mode=b.dataset.mode;time=0;playing=true;for(const p of document.querySelectorAll('[data-mode]'))p.classList.toggle('selected',p===b);});
 document.querySelector('[data-play]').addEventListener('click',()=>{playing=!playing;document.querySelector('[data-play]').textContent=playing?'Tạm dừng':'Chạy tiếp';});document.querySelector('[data-time]').addEventListener('input',e=>{time=Number(e.target.value);playing=false;render();ui();});document.querySelector('[data-map]').addEventListener('change',e=>{onMap=e.target.checked;render();});document.querySelector('[data-pivot]').addEventListener('change',e=>{pivots=e.target.checked;render();});
 ready.then(()=>{document.body.dataset.ready='true';function tick(now){if(playing)time=(time+Math.min(.05,(now-(last||now))/1000))%4.2;last=now;render();ui();requestAnimationFrame(tick);}requestAnimationFrame(tick);});
})();
