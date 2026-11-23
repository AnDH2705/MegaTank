(function(){
 'use strict';
 const model=JSON.parse(document.getElementById('trojan-model').textContent),dogModel=JSON.parse(document.getElementById('trojan-dogs').textContent),sources=JSON.parse(document.getElementById('trojan-images').textContent),canvas=document.querySelector('canvas'),ctx=canvas.getContext('2d'),images={};
 const clamp=(v,a=0,b=1)=>Math.max(a,Math.min(b,v));let mode='closed',time=0,playing=true,last=0,onMap=false,showPivots=false;
 const fullDuration=4.2;
 function image(part,x,y,w,h){ctx.drawImage(images[part],x,y,w,h);}
 function part(layer,rotation=0,alpha=1){ctx.save();ctx.globalAlpha*=alpha;const p=layer.pivot_canvas_px;ctx.translate(p[0],p[1]);ctx.rotate(rotation);image(layer.part,layer.position_px[0]-p[0],layer.position_px[1]-p[1],layer.size_px[0],layer.size_px[1]);ctx.restore();}
 function dog(action,clock,x,y,phase=0){
  const clip=dogModel.clips[action],im=images['dog_'+action],sw=im.width/clip.cols,sh=im.height/clip.rows,progress=((clock+phase)%clip.duration+clip.duration)%clip.duration,frame=Math.min(clip.frames-1,Math.floor(progress/clip.duration*clip.frames)),z=1.3;
  ctx.save();ctx.translate(x,y);ctx.scale(-1,1);ctx.drawImage(im,frame%clip.cols*sw,Math.floor(frame/clip.cols)*sh,sw,sh,-clip.pivot[0]*z,-clip.pivot[1]*z,clip.cell[0]*z,clip.cell[1]*z);ctx.restore();
 }
 function drawKit(at,view,{transparent=false,debug=false}={}){
  const reveal=view==='shell'||view==='hatch',release=view==='hatch'?.72:.45,exposed=view==='cutaway'||(reveal&&at>.35),cartDrift=view==='shell'?clamp((at-.65)/1.1)*350:0;
  const poses=[];
  if(!transparent){ctx.fillStyle='rgba(37,31,25,.12)';ctx.beginPath();ctx.ellipse(790+cartDrift*.5,951,580,22,0,0,Math.PI*2);ctx.fill();}
  // Tail is part of the removable shell, not the chassis.
  for(const layer of model.layers.filter(p=>p.parent!=='shell_root')){
   ctx.save();ctx.translate(cartDrift,0);
   if(layer.id.startsWith('wheel_'))part(layer,view==='closed'?at*5:view==='shell'?clamp(at)*5:0);else part(layer);
   ctx.restore();
  }
  for(let i=0;i<3;i++){
   const socket=model.dog_sockets[i],land=model.landing_points[i],u=reveal?clamp((at-release-i*.14)/.65):0,x=socket.position_px[0]+(land.position_px[0]-socket.position_px[0])*u,y=socket.position_px[1]+(land.position_px[1]-socket.position_px[1])*u-4*175*u*(1-u),landed=reveal&&u===1;
   poses.push({id:socket.id,x,y,phase:landed?'melee':reveal&&u>0?'jump':exposed?'inside':'hidden'});
   ctx.save();if(!exposed){ctx.beginPath();ctx.rect(...model.feet_aperture_px);ctx.clip();}
   dog(landed?'attack':reveal&&u>0?'walk':view==='closed'?'walk':'idle',landed?Math.max(0,at-release-i*.14-.65):at,x,y,i*.19);ctx.restore();
  }
  const members=model.layers.filter(p=>p.parent==='shell_root');
  if(view==='shell'){
   const v=Math.max(0,at-.35),pivot=model.shell_root.pivot_px;
   ctx.save();ctx.translate(pivot[0]+v*400,pivot[1]-950*v+750*v*v);ctx.rotate(v*.85);ctx.translate(-pivot[0],-pivot[1]);ctx.globalAlpha=1-clamp((v-1.55)/.45);for(const layer of members)part(layer);ctx.restore();
   if(at>.5){const p=model.parts['08_scrap_fragment'],v=at-.5;ctx.save();ctx.translate(1000+v*340,630-v*450+v*v*460);ctx.rotate(v*3);ctx.globalAlpha=1-clamp((v-1.4)/.5);image('08_scrap_fragment',-95,-50,190,190*p.size_px[1]/p.size_px[0]);ctx.restore();}
  }else for(const layer of members){
   if(layer.id==='outer_body'&&view==='hatch'){const u=clamp((at-.25)/.45);part(layer,-u*1.65,1-clamp((at-1.25)/.6));}
   else part(layer,0,view==='cutaway'?(layer.id==='outer_body'?.08:layer.id==='head'?.18:1):1);
  }
  if(debug){ctx.save();ctx.strokeStyle='#29b8c8';ctx.fillStyle='#126778';ctx.lineWidth=2;ctx.font='17px sans-serif';for(const layer of model.layers){const p=layer.pivot_canvas_px;ctx.beginPath();ctx.moveTo(p[0]-9,p[1]);ctx.lineTo(p[0]+9,p[1]);ctx.moveTo(p[0],p[1]-9);ctx.lineTo(p[0],p[1]+9);ctx.stroke();ctx.fillText(layer.id,p[0]+11,p[1]-6);}for(const p of model.landing_points){ctx.strokeRect(p.position_px[0]-14,p.position_px[1]-4,28,8);ctx.fillText(p.id,p.position_px[0]+16,p.position_px[1]);}ctx.restore();}
  return poses;
 }
 function render(at=time,view=mode,{transparent=false,map=onMap,debug=showPivots}={}){
  ctx.clearRect(0,0,canvas.width,canvas.height);
  if(!transparent){
   if(map){const width=1000*images.background.width/images.background.height;ctx.drawImage(images.background,(1600-width)/2,0,width,1000);}
   else{ctx.fillStyle='#f5efdf';ctx.fillRect(0,0,1600,1000);ctx.strokeStyle='#d3c6aa';ctx.lineWidth=2;ctx.beginPath();ctx.moveTo(40,952);ctx.lineTo(1560,952);ctx.stroke();}
  }
  ctx.save();if(map){ctx.translate(970,865);ctx.scale(.4,.4);ctx.translate(-800,-952);}const dogs=drawKit(at,view,{transparent,debug});ctx.restore();
  window.TrojanPreview.last={mode:view,time:at,dogs,visibleDogCount:dogs.filter(d=>d.phase!=='hidden').length};return window.TrojanPreview.last;
 }
 function updateUI(){document.querySelector('[data-time]').value=String(time);document.querySelector('[data-clock]').textContent=time.toFixed(2)+'s';document.querySelector('[data-state]').textContent=mode==='closed'?'VỎ ĐÓNG · BÁNH XE LĂN':mode==='cutaway'?'BÊN TRONG: 3 CHÓ MELEE':time<.35?'VỎ RUNG · CHUẨN BỊ BẤT NGỜ':time<1.5?'MỞ VỎ · CHÓ NHẢY XUỐNG':'3 CHÓ TIẾP QUẢN CẬN CHIẾN';}
 const ready=Promise.all(Object.entries(sources).map(([key,src])=>new Promise((resolve,reject)=>{const im=new Image();im.onload=()=>{images[key]=im;resolve();};im.onerror=()=>reject(Error('Missing '+key));im.src=src;})));
 window.TrojanPreview={ready,render,capture:(at,view)=>{render(at,view,{transparent:true,map:false,debug:false});return canvas.toDataURL('image/png');},model,last:null,set:(view,at=0)=>{mode=view;time=at;playing=false;render();updateUI();}};
 for(const b of document.querySelectorAll('[data-mode]'))b.addEventListener('click',()=>{mode=b.dataset.mode;time=0;playing=true;for(const other of document.querySelectorAll('[data-mode]'))other.classList.toggle('selected',other===b);render();updateUI();});
 document.querySelector('[data-play]').addEventListener('click',()=>{playing=!playing;document.querySelector('[data-play]').textContent=playing?'Tạm dừng':'Chạy tiếp';});
 document.querySelector('[data-time]').addEventListener('input',e=>{playing=false;time=Number(e.target.value);render();updateUI();});
 document.querySelector('[data-map]').addEventListener('change',e=>{onMap=e.target.checked;render();});document.querySelector('[data-pivot]').addEventListener('change',e=>{showPivots=e.target.checked;render();});
 ready.then(()=>{document.body.dataset.ready='true';function tick(now){if(playing)time=(time+Math.min(.05,(now-(last||now))/1000))%fullDuration;last=now;render();updateUI();requestAnimationFrame(tick);}requestAnimationFrame(tick);});
})();
