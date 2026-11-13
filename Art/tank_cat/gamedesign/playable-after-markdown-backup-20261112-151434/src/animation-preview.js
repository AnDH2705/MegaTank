import * as T from '/vendor/three.module.js';
import {createCatRig} from './cat-rig.js';

const status=document.querySelector('#status');
try {
  const mount=document.querySelector('#stage');
  const renderer=new T.WebGLRenderer({antialias:true,alpha:true});
  renderer.setPixelRatio(Math.min(devicePixelRatio,2));
  renderer.outputColorSpace=T.SRGBColorSpace;mount.append(renderer.domElement);
  const scene=new T.Scene(), camera=new T.OrthographicCamera(-400,400,400,-40,.1,1000);camera.position.z=500;
  const texture=await new T.TextureLoader().loadAsync('/assets/cat-0.png');texture.colorSpace=T.SRGBColorSpace;
  const rig=createCatRig(T,texture);scene.add(rig.root);
  const helper=new T.SkeletonHelper(rig.root);helper.material.depthTest=false;helper.renderOrder=20;helper.visible=false;scene.add(helper);
  const dots=[];
  for(const bone of Object.values(rig.bones)){
    const dot=new T.Mesh(new T.CircleGeometry(3,12),new T.MeshBasicMaterial({color:0x66ffff,depthTest:false}));
    dot.renderOrder=21;dot.visible=false;bone.add(dot);dots.push(dot);
  }
  const shadow=new T.Mesh(new T.CircleGeometry(1,40),new T.MeshBasicMaterial({color:0x030c17,transparent:true,opacity:.4}));shadow.scale.set(135,13,1);shadow.position.set(0,-4,-1);shadow.renderOrder=-1;scene.add(shadow);
  function resize(){const w=mount.clientWidth,h=mount.clientHeight;renderer.setSize(w,h);camera.left=-220*w/h;camera.right=220*w/h;camera.updateProjectionMatrix();}
  new ResizeObserver(resize).observe(mount);resize();
  let state='idle',paused=false,time=0,previous=performance.now();
  document.querySelectorAll('[data-state]').forEach(button=>button.onclick=()=>{
    state=button.dataset.state;time=0;
    document.querySelectorAll('[data-state]').forEach(b=>b.setAttribute('aria-pressed',String(b===button)));
  });
  document.querySelector('#pause').onclick=event=>{paused=!paused;event.currentTarget.setAttribute('aria-pressed',String(paused));event.currentTarget.textContent=paused?'Tiếp tục':'Tạm dừng';};
  document.querySelector('#bones').onchange=event=>{helper.visible=event.target.checked;dots.forEach(d=>d.visible=helper.visible);};
  function frame(now){
    const dt=Math.min((now-previous)/1000,.05);previous=now;if(!paused)time+=dt;
    rig.pose(state,time);
    const amount=Number(document.querySelector('#explode').value);
    const offsets={head:[15,70],tail:[-85,0],rearPaw:[-40,-25],frontPaw:[20,-35],farPaw:[70,-10],chest:[0,10]};
    for(const [name,[x,y]] of Object.entries(offsets)){rig.bones[name].position.x+=x*amount;rig.bones[name].position.y+=y*amount;}
    renderer.render(scene,camera);requestAnimationFrame(frame);
  }
  requestAnimationFrame(frame);
  status.textContent='Đã tải sprite gốc · 7 khớp · 4 động tác · Three.js local';
  window.animationPreview={rig,renderer,scene,camera,get state(){return state;},get time(){return time;}};
} catch(error) {status.textContent=`Không mở được mẫu: ${error.message}`;console.error(error);}
