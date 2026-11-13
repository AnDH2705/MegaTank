import * as T from '/vendor/three.module.js';
import {createWolfRig} from './wolf-rig.js';
const status=document.querySelector('#status');
try{
  const mount=document.querySelector('#stage');
  const renderer=new T.WebGLRenderer({antialias:true,alpha:true});renderer.setPixelRatio(Math.min(devicePixelRatio,2));renderer.outputColorSpace=T.SRGBColorSpace;mount.append(renderer.domElement);
  const scene=new T.Scene(),camera=new T.OrthographicCamera(-330,330,450,-30,.1,1000);camera.position.z=500;
  const texture=await new T.TextureLoader().loadAsync('/assets/wolf-rig-atlas-v1.png');texture.colorSpace=T.SRGBColorSpace;texture.minFilter=T.LinearFilter;
  const rig=createWolfRig(T,texture);scene.add(rig.root);
  const helper=new T.SkeletonHelper(rig.root);helper.material.depthTest=false;helper.renderOrder=30;helper.visible=false;scene.add(helper);
  const dots=Object.values(rig.bones).map(b=>{const d=new T.Mesh(new T.CircleGeometry(3,12),new T.MeshBasicMaterial({color:0x60ffed,depthTest:false}));d.renderOrder=31;d.visible=false;b.add(d);return d;});
  const shadow=new T.Mesh(new T.CircleGeometry(1,40),new T.MeshBasicMaterial({color:0x060a13,transparent:true,opacity:.25,depthWrite:false}));shadow.scale.set(210,13,1);shadow.position.set(-10,-8,-1);shadow.renderOrder=-1;scene.add(shadow);
  function resize(){const w=mount.clientWidth,h=mount.clientHeight;renderer.setSize(w,h);const halfHeight=Math.max(240,325*h/w);camera.left=-halfHeight*w/h;camera.right=halfHeight*w/h;camera.top=210+halfHeight;camera.bottom=210-halfHeight;camera.updateProjectionMatrix();}
  new ResizeObserver(resize).observe(mount);resize();
  let state='idle',paused=false,time=0,previous=performance.now();
  document.querySelectorAll('[data-state]').forEach(b=>b.onclick=()=>{state=b.dataset.state;time=0;document.querySelectorAll('[data-state]').forEach(x=>x.setAttribute('aria-pressed',String(b===x)));});
  document.querySelector('#pause').onclick=e=>{paused=!paused;e.currentTarget.setAttribute('aria-pressed',String(paused));e.currentTarget.textContent=paused?'Tiếp tục':'Tạm dừng';};
  document.querySelector('#bones').onchange=e=>{helper.visible=e.target.checked;dots.forEach(d=>d.visible=helper.visible);};
  document.querySelector('#light').onchange=e=>mount.classList.toggle('light',e.target.checked);
  function renderPose(){rig.pose(state,time,Number(document.querySelector('#explode').value));renderer.render(scene,camera);}
  function frame(now){const dt=Math.min((now-previous)/1000,.05);previous=now;if(!paused)time+=dt*Number(document.querySelector('#speed').value);renderPose();requestAnimationFrame(frame);}
  requestAnimationFrame(frame);
  window.monsterPreview={rig,renderer,scene,camera,get state(){return state;},get time(){return time;},setTime(t){time=t;renderPose();}};
  status.textContent='Ảnh vẽ lại · 6 bộ phận · 9 khớp · 4 động tác';
}catch(error){status.textContent=`Không mở được mẫu: ${error.message}`;console.error(error);}
