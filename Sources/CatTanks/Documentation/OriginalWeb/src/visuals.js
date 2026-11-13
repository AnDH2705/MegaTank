import {resolveSprite} from './catalog.js';
import {createEnemyAnimation,triggerEnemyAnimation,sampleEnemyAnimation} from './enemy-animation.js';
export function createVisuals(T, mount, assets) {
 const scene=new T.Scene();const camera=new T.OrthographicCamera(0,1600,900,0,.1,3000);camera.position.z=1000;
 const renderer=new T.WebGLRenderer({antialias:true,alpha:true,powerPreference:'high-performance'});
 renderer.setPixelRatio(Math.min(devicePixelRatio,2));renderer.outputColorSpace=T.SRGBColorSpace;mount.appendChild(renderer.domElement);
 const textures=new Map(), materials=new Map(), plane=new T.PlaneGeometry(1,1), circle=new T.CircleGeometry(1,32);
 const orbViews=new Map();
 const enemies=new Map(),bullets=new Map(),units=new Map(),effects=[],particles=[];let battle,shake=0,totalTime=0;
 function resize(){const width=mount.clientWidth,height=mount.clientHeight;renderer.setSize(width,height);const aspect=width/height;const worldH=Math.max(900,1600/aspect);const worldW=worldH*aspect;camera.left=800-worldW/2;camera.right=800+worldW/2;camera.top=450+worldH/2;camera.bottom=450-worldH/2;camera.updateProjectionMatrix();particleMat.uniforms.pixelScale.value=height/worldH*renderer.getPixelRatio();}
 const mat=(color,opacity=1)=>{const key=color+':'+opacity;if(!materials.has(key))materials.set(key,new T.MeshBasicMaterial({color,transparent:opacity<1,opacity,depthWrite:false}));return materials.get(key);};
 function rect(parent,x,y,w,h,color,z=0,opacity=1){const mesh=new T.Mesh(plane,mat(color,opacity));mesh.position.set(x,y,z);mesh.scale.set(w,h,1);parent.add(mesh);return mesh;}
 function disk(parent,x,y,r,color,z=0,opacity=1){const mesh=new T.Mesh(circle,mat(color,opacity));mesh.position.set(x,y,z);mesh.scale.setScalar(r);parent.add(mesh);return mesh;}
 function canvasTexture(w,h,draw){const c=document.createElement('canvas');c.width=w;c.height=h;draw(c.getContext('2d'),w,h);const tx=new T.CanvasTexture(c);tx.colorSpace=T.SRGBColorSpace;return tx;}
 const glowTexture=canvasTexture(128,128,(c)=>{const g=c.createRadialGradient(64,64,0,64,64,64);g.addColorStop(0,'#ffffff');g.addColorStop(.13,'#ffffffe0');g.addColorStop(.4,'#ffffff55');g.addColorStop(1,'#ffffff00');c.fillStyle=g;c.fillRect(0,0,128,128);});
 function glow(parent,x,y,size,color,z=30){const m=new T.Mesh(plane,new T.MeshBasicMaterial({map:glowTexture,color,transparent:true,blending:T.AdditiveBlending,depthWrite:false}));m.position.set(x,y,z);m.scale.set(size,size,1);parent.add(m);return m;}
 for(const [name,img]of assets){const tx=new T.Texture(img);tx.needsUpdate=true;tx.colorSpace=T.SRGBColorSpace;tx.minFilter=T.LinearFilter;textures.set(name,tx);}
 function sprite(name,width,parent=scene,x=0,y=0,z=0,flip=false){const tx=textures.get(resolveSprite(textures,name,name?.startsWith('beast-')||name?.startsWith('bug-')?'beast-0':name?.startsWith('turret')?'turret-0':'cat-0'));const m=new T.Mesh(plane,new T.MeshBasicMaterial({map:tx,transparent:true,depthWrite:false}));const img=tx.image;m.scale.set(flip?-width:width,width*img.height/img.width,1);m.position.set(x,y,z);parent.add(m);return m;}
 function label(text,color='#fff',size=22){const tx=canvasTexture(256,64,c=>{c.font=`900 ${size}px Nunito,Arial`;c.textAlign='center';c.textBaseline='middle';c.lineWidth=5;c.strokeStyle='#08283bd0';c.strokeText(text,128,32);c.fillStyle=color;c.fillText(text,128,32);});const m=new T.Mesh(plane,new T.MeshBasicMaterial({map:tx,transparent:true,depthWrite:false}));m.scale.set(128,32,1);m.userData.disposeTexture=true;return m;}
 // All scenery is drawn procedurally. Original character and weapon sheets stay untouched.
 const backdrop=canvasTexture(2400,1350,(c,w,h)=>{
  c.scale(1.5,1.5);let g=c.createLinearGradient(0,0,0,900);g.addColorStop(0,'#0872b1');g.addColorStop(.42,'#079cc5');g.addColorStop(.73,'#07537b');g.addColorStop(1,'#062b48');c.fillStyle=g;c.fillRect(0,0,1600,900);
  g=c.createRadialGradient(790,322,90,790,322,420);g.addColorStop(0,'#39f5ed65');g.addColorStop(.55,'#32dde520');g.addColorStop(1,'#20c8f000');c.fillStyle=g;c.fillRect(0,0,1600,900);
  g=c.createRadialGradient(755,275,0,790,322,177);g.addColorStop(0,'#77f8ec');g.addColorStop(.7,'#47e3df');g.addColorStop(1,'#26bbcf');c.beginPath();c.arc(790,322,177,0,Math.PI*2);c.fillStyle=g;c.fill();
  c.save();c.beginPath();c.arc(790,322,176,0,Math.PI*2);c.clip();for(let i=0;i<20;i++){const x=680+Math.sin(i*13.2)*160,y=235+(i*59)%270;c.fillStyle=i%2?'#087ea115':'#a4fff418';c.beginPath();c.ellipse(x,y,20+(i*7)%50,18+(i*3)%35,i,0,Math.PI*2);c.fill();}c.restore();
  let seed=19;const rand=()=>{seed=(seed*16807)%2147483647;return seed/2147483647;};
  for(let i=0;i<90;i++){const x=rand()*1600,y=rand()*570,r=rand()*1.7+.4;c.fillStyle=`rgba(255,235,152,${rand()*.6+.2})`;c.beginPath();c.arc(x,y,r,0,6.28);c.fill();}
  for(let layer=0;layer<3;layer++){
   c.fillStyle=['#0c679080','#0b5477b0','#103e5fed'][layer];c.beginPath();c.moveTo(0,780);let x=-50;while(x<1700){const y=400+layer*90-rand()*160;c.lineTo(x,y+80);c.lineTo(x+15,y+30);c.lineTo(x+40,y);c.lineTo(x+65,y+25);c.lineTo(x+80,y+100);x+=90+rand()*90;}c.lineTo(1700,900);c.lineTo(0,900);c.fill();
  }
  g=c.createLinearGradient(0,540,0,765);g.addColorStop(0,'#1bc7de00');g.addColorStop(.55,'#5ed1de18');g.addColorStop(1,'#2b819852');c.fillStyle=g;c.fillRect(0,510,1600,300);
  c.fillStyle='#5086a1';c.fillRect(0,722,1600,55);c.fillStyle='#32627f';c.fillRect(0,765,1600,135);
  for(let i=0;i<260;i++){let x=rand()*1600,y=728+rand()*160;c.fillStyle=['#224d6c','#6698aa','#477b94','#1e4562'][i%4];c.beginPath();c.ellipse(x,y,4+rand()*27,1+rand()*4,0,0,6.28);c.fill();}
  c.fillStyle='#0b324f';c.beginPath();c.moveTo(0,810);for(let x=0;x<=1700;x+=70)c.lineTo(x,820+rand()*35);c.lineTo(1600,900);c.lineTo(0,900);c.fill();
 });
 const bg=new T.Mesh(plane,new T.MeshBasicMaterial({map:backdrop}));bg.scale.set(1600,900,1);bg.position.set(800,450,-110);scene.add(bg);
 const scenery=new T.Group();scene.add(scenery);
 const rockGeometry=new T.IcosahedronGeometry(1,0);
 for(let i=0;i<58;i++){const x=(i*157.13)%1700-50;const y=174+(i%4)*7;const r=9+(i*7)%30;const mesh=new T.Mesh(rockGeometry,mat(['#284963','#37627a','#244057','#4d7a8d'][i%4]));mesh.position.set(x,y-r*.1,-25);mesh.scale.set(r*1.4,r,12);mesh.rotation.z=i*3;scenery.add(mesh);}
 for(const x of [42,610,1000,1480]){const g=new T.Group();g.position.set(x,187,-15);scenery.add(g);for(let i=0;i<3;i++){const shape=new T.Shape();shape.moveTo(-5,0);shape.lineTo(-7,30+i*12);shape.lineTo(0,46+i*9);shape.lineTo(8,25);shape.lineTo(6,0);const m=new T.Mesh(new T.ShapeGeometry(shape),mat(i%2?'#42b8d2':'#70e5e4'));m.position.x=(i-1)*10;m.rotation.z=(i-1)*.2;g.add(m);}glow(g,0,25,100,'#39e6ff',-1);}
 // A single point cloud draws every spark, shell casing, ember and dust particle.
 const particleCount=2400,positions=new Float32Array(particleCount*3),colors=new Float32Array(particleCount*3),sizes=new Float32Array(particleCount),alphas=new Float32Array(particleCount);
 const particleGeo=new T.BufferGeometry();particleGeo.setAttribute('position',new T.BufferAttribute(positions,3));particleGeo.setAttribute('pColor',new T.BufferAttribute(colors,3));particleGeo.setAttribute('pSize',new T.BufferAttribute(sizes,1));particleGeo.setAttribute('pAlpha',new T.BufferAttribute(alphas,1));
 const particleMat=new T.ShaderMaterial({transparent:true,depthWrite:false,blending:T.AdditiveBlending,uniforms:{pixelScale:{value:1}},vertexShader:'attribute vec3 pColor; attribute float pSize; attribute float pAlpha; varying vec3 vColor; varying float vAlpha; uniform float pixelScale; void main(){vColor=pColor;vAlpha=pAlpha;gl_Position=projectionMatrix*modelViewMatrix*vec4(position,1.);gl_PointSize=pSize*pixelScale;}',fragmentShader:'varying vec3 vColor; varying float vAlpha; void main(){float d=length(gl_PointCoord-.5)*2.;float a=pow(max(0.,1.-d),1.7)*vAlpha;gl_FragColor=vec4(vColor,a);}' });
 const points=new T.Points(particleGeo,particleMat);points.frustumCulled=false;points.renderOrder=100;scene.add(points);
 function burst(x,y,color,n=18,force=140,size=14,life=.65){const col=new T.Color(color);for(let i=0;i<n&&particles.length<particleCount;i++){const a=Math.random()*Math.PI*2,s=force*(.15+Math.random());particles.push({x,y,z:40,vx:Math.cos(a)*s,vy:Math.sin(a)*s+force*.2,life:life*(.5+Math.random()),maxLife:life,size:size*(.3+Math.random()),color:col,gravity:150});}}
 function temp(mesh,life=.2,extra={}){mesh.traverse(m=>{if(m.material){if([...materials.values()].includes(m.material))m.material=m.material.clone();m.material.transparent=true;}});scene.add(mesh);effects.push({mesh,life,maxLife:life,...extra});return mesh;}
 function ring(x,y,r,color,life=.5){const m=new T.Mesh(new T.RingGeometry(.88,1,48),new T.MeshBasicMaterial({color,transparent:true,opacity:1,blending:T.AdditiveBlending,depthWrite:false}));m.position.set(x,y,45);m.scale.set(r,r,1);temp(m,life,{expand:2});}
 // Layer a hot flash, rolling fire and rising soot instead of a single spark puff.
 const smokeTexture=canvasTexture(128,128,c=>{const g=c.createRadialGradient(64,64,3,64,64,64);g.addColorStop(0,'#ffffffbb');g.addColorStop(.45,'#ffffff77');g.addColorStop(1,'#ffffff00');c.fillStyle=g;c.fillRect(0,0,128,128);});
 function explosion(x,y,r,color){
  temp(glow(scene,x,y,r*2.5,'#fff2be',47),.16,{expand:3});
  ring(x,y,r*.7,color,.55);
  for(let i=0;i<4;i++){
   const dx=(Math.random()-.5)*r*.75,dy=(Math.random()-.5)*r*.5;
   temp(glow(scene,x+dx,y+dy,r*(1.1+Math.random()*.6),i%2?'#ff5a18':color,44),.45+Math.random()*.3,{expand:1.2,float:45+i*15,fire:true});
   const smoke=new T.Mesh(plane,new T.MeshBasicMaterial({map:smokeTexture,color:'#28313c',transparent:true,opacity:.6,depthWrite:false}));
   smoke.position.set(x+dx,y+dy+r*.2,42);smoke.scale.set(r*1.2,r,1);smoke.rotation.z=Math.random()*6;
   temp(smoke,.95+Math.random()*.45,{expand:.65,float:65,opacity:.6});
  }
  burst(x,y,'#ff922f',36,r*2.1,42,.85);burst(x,y,'#ffe6a1',22,r*3,15,.8);
  shake=Math.max(shake,Math.min(12,r*.065));
 }
 function beam(x,y,tx,ty,color,kind='laser'){
  const g=new T.Group();const dx=tx-x,dy=ty-y,l=Math.hypot(dx,dy);g.position.set(x,y,48);g.rotation.z=Math.atan2(dy,dx);rect(g,l/2,0,l,kind==='sniper'?6:16,color,0,.65);rect(g,l/2,0,l,kind==='sniper'?2:6,'#efffff',1);glow(g,0,0,150,color,2);temp(g,kind==='sniper'?.14:.24);
 }
 function lightning(x,y,tx,ty,color){
  const pts=[];const steps=12;for(let i=0;i<=steps;i++){const t=i/steps;pts.push(new T.Vector3(x+(tx-x)*t+(i&&i<steps?(Math.random()-.5)*28:0),y+(ty-y)*t+(i&&i<steps?(Math.random()-.5)*30:0),49));}
  const line=new T.Line(new T.BufferGeometry().setFromPoints(pts),new T.LineBasicMaterial({color,transparent:true,blending:T.AdditiveBlending}));temp(line,.22);burst(tx,ty,color,12,90,18,.35);
 }
 function towerTexture(){return canvasTexture(768,1536,(c)=>{
  c.scale(2,2.5);
  const rr=(x,y,w,h,r,fill,stroke='#102536',lw=5)=>{c.beginPath();c.roundRect(x,y,w,h,r);c.fillStyle=fill;c.fill();if(stroke){c.lineWidth=lw;c.strokeStyle=stroke;c.stroke();}};
  // Five open modules, one seat and one cat per module.
  for(let i=0;i<5;i++){const y=111+i*85;
   rr(70,y,250,78,9,'#1a2e3d');rr(75,y+3,239,20,4,'#3e4c50',null);
   rr(65,y+3,64,78,7,'#ed9d25');rr(69,y+8,54,68,5,'#ffbe39','#c48222',2);
   rr(78,y+19,35,31,4,['#f19035','#5cc571','#f18c32','#5eb7e7','#ff9344'][i],'#885e2b',3);
   c.font='bold 25px Arial';c.fillStyle='#fff3c8';c.textAlign='center';c.fillText(['ϟ','+','➤','❄','✦'][i],96,y+44);
   rr(87,y+56,17,19,3,'#80572c','#533d2b',2);rr(91,y+59,8,8,2,'#ffe480',null);
   rr(128,y+67,202,14,4,'#ffbf37');rr(134,y+69,192,4,2,'#ffe59c',null);
   rr(305,y+24,16,37,4,'#a9b9bf');rr(311,y+33,8,13,2,'#ffe071',null);
   rr(51,y+28,11,31,4,'#f7ac2d');rr(52,y+32,5,21,2,'#ffe080',null);
   // Seat, rail, rear light.
   rr(154,y+44,46,24,7,'#465e6a','#182b3a',3);rr(145,y+30,16,37,5,'#344d5c','#172c3d',3);
   c.fillStyle='#163348';c.fillRect(136,y+64,183,4);
  }
  // Roof and command deck.
  rr(62,96,274,18,6,'#ffbd3a');rr(68,98,261,5,2,'#ffe795',null);
  rr(74,64,14,36,4,'#5f7481');rr(299,56,13,41,4,'#5e7785');
  c.beginPath();c.moveTo(75,64);c.lineTo(97,39);c.lineTo(267,38);c.lineTo(316,58);c.lineTo(316,67);c.closePath();c.fillStyle='#ed9b29';c.fill();c.lineWidth=5;c.strokeStyle='#132c3f';c.stroke();
  c.beginPath();c.moveTo(101,43);c.lineTo(258,43);c.lineTo(290,55);c.lineTo(91,55);c.closePath();c.fillStyle='#ffcc52';c.fill();rr(94,52,32,8,3,'#5ee9ef','#5f6c56',2);
  // Track and hull.
  rr(49,540,292,54,24,'#263a48');rr(54,545,282,42,20,'#475a63','#0e2638',3);rr(63,532,266,24,8,'#d68b25');rr(79,535,236,6,3,'#ffdb66',null);
  for(let i=0;i<4;i++){const x=83+i*74;c.beginPath();c.arc(x,565,23,0,6.28);c.fillStyle='#2b3d4d';c.fill();c.lineWidth=4;c.strokeStyle='#11293a';c.stroke();c.beginPath();c.arc(x,565,15,0,6.28);c.fillStyle='#ae8242';c.fill();c.lineWidth=4;c.strokeStyle='#edae3c';c.stroke();c.beginPath();c.arc(x,565,7,0,6.28);c.fillStyle='#37546b';c.fill();}
  rr(141,544,94,18,2,'#f8bd35','#142b3a',2);c.save();c.beginPath();c.rect(142,545,91,15);c.clip();for(let i=0;i<6;i++){c.fillStyle='#213c4d';c.beginPath();c.moveTo(140+i*22,544);c.lineTo(150+i*22,544);c.lineTo(162+i*22,561);c.lineTo(152+i*22,561);c.fill();}c.restore();
  // Flag pole and paw flag.
  rr(38,94,6,145,3,'#a8c1c8','#153348',2);c.beginPath();c.moveTo(39,100);c.lineTo(6,103);c.lineTo(15,120);c.lineTo(3,139);c.lineTo(39,135);c.closePath();c.fillStyle='#f58d2e';c.fill();c.strokeStyle='#193348';c.lineWidth=4;c.stroke();c.font='bold 22px Arial';c.fillStyle='#fff2d4';c.fillText('✦',25,129);
 });}
 const tank=new T.Group();tank.position.set(290,175,10);scene.add(tank);
 const tankFrame=new T.Mesh(plane,new T.MeshBasicMaterial({map:towerTexture(),transparent:true,depthWrite:false}));tankFrame.scale.set(354,566,1);tankFrame.position.set(-14,267,0);tank.add(tankFrame);
 const towerCrew=[];
 for(let i=0;i<5;i++){const g=new T.Group();g.position.set(0,92+i*80,2);tank.add(g);sprite(`cat-${i}`,64,g,-8,10,0);sprite(`uniform-${[2,1,0,7,4][i]}`,43,g,-8,-6,1);sprite(`hat-${[2,1,0,7,4][i]}`,48,g,-8,34,2);const paws=[];for(const x of [-2,13])paws.push(disk(g,x,-5,6,'#fff1d3',4));towerCrew.push(g);}
 const wheels=[];for(let i=0;i<4;i++){const g=new T.Group();g.position.set(-114+i*68,27,4);tank.add(g);for(let j=0;j<4;j++){const spoke=rect(g,0,0,4,20,'#899492',0,.6);spoke.rotation.z=j*Math.PI/4;}wheels.push(g);}
 const exhaust=glow(tank,-130,37,70,'#ffb75a',-1);
 const shield=new T.Mesh(new T.CircleGeometry(1,80),new T.MeshBasicMaterial({color:'#5deaff',transparent:true,opacity:.07,blending:T.AdditiveBlending,depthWrite:false}));shield.scale.set(205,330,1);shield.position.set(290,440,28);scene.add(shield);shield.visible=false;
 const shieldEdge=new T.Mesh(new T.RingGeometry(.985,1,90),new T.MeshBasicMaterial({color:'#79efff',transparent:true,opacity:.6,blending:T.AdditiveBlending,depthWrite:false}));shieldEdge.scale.set(205,330,1);shieldEdge.position.set(290,440,29);scene.add(shieldEdge);shieldEdge.visible=false;
 const supportDrone=new T.Group();sprite('vehicle-9',105,supportDrone,0,0,2);glow(supportDrone,0,-25,80,'#66eaff',0);scene.add(supportDrone);supportDrone.visible=false;
 function createUnit(u){const g=new T.Group();scene.add(g);let body,legs=[];
  if(u.role==='infantry'){
   for(const x of [-11,12]){const leg=new T.Group();leg.position.set(x,2,1);g.add(leg);rect(leg,0,-5,13,20,'#dfd9c5',0);disk(leg,0,-14,8,'#243e4e',1);legs.push(leg);}
   body=new T.Group();g.add(body);sprite(u.asset??`cat-${u.index}`,52,body,-5,17,2);sprite(`uniform-${u.index}`,39,body,-4,0,3);sprite(`hat-${u.index}`,43,body,-4,39,4);
  }
  if(u.role==='companion'){body=new T.Group();g.add(body);sprite(u.asset,110*(1+.12*(u.evo??0)),body,0,25,2);}
  const hpBack=battle?.unitHp?rect(g,0,u.role==='companion'?85:48,60,7,'#10263b',50):null;const hpFill=hpBack?rect(g,0,u.role==='companion'?85:48,58,4,'#8ef587',51):null;
  const pivot=new T.Group();g.add(pivot);pivot.position.z=u.role==='infantry'?7:18;
  const view={g,pivot,body,legs,hpBack,hpFill,weapon:null,mesh:null,muzzle:null};units.set(u.id,view);return view;
 }
 function setUnitWeapon(view,u){if(view.mesh){view.pivot.remove(view.mesh);view.mesh.material.dispose();}const tower=u.role==='tower';const width=tower?117:u.role==='aux'?58:57;const asset=tower&&battle.classic!==false&&Math.floor(battle.time/10)%2?u.weapon.asset.replace('turret-','turret_alt-'):(u.weapon.asset??'ammo-0');
  view.mesh=sprite(asset,width,view.pivot,tower?20:u.role==='infantry'?13:0,0,0,u.weapon.flip);view.weapon=u.weapon.id;view.skin=asset;view.mesh.visible=u.role!=='companion';
  if(!view.muzzle)view.muzzle=glow(view.pivot,tower?76:42,0,65,u.weapon.color,2);
  view.muzzle.material.color.set(u.weapon.color);view.muzzle.visible=false;
 }
 function createEnemy(e){const g=new T.Group();scene.add(g);const shadow=disk(g,0,-e.size*.4,e.size*.45,'#062a3e',-2,.3);shadow.scale.y*=.18;
  const animated=e.def.asset==='beast-0';
  // 235px is the wolf's drawn width inside the shared 272px frame canvas.
  // Keep that one scale for every pose and anchor the 217px paw line to ground.
  const body=sprite(animated?'wolf-frames/idle-0':e.def.asset,animated?e.size*272/235:e.size,g,0,animated?-e.size*.43+89*e.size/235:0,1,true);
  const back=rect(g,0,e.size*.52,e.size*.64,6,'#10263b',4);const fill=rect(g,0,e.size*.52,e.size*.64-2,3,e.def.boss?'#ff6a76':'#e8b46a',5);const catchBadge=label('BẮT','#9bffe0',20);catchBadge.position.set(0,e.size*.52+25,50);catchBadge.visible=false;g.add(catchBadge);const v={g,body,shadow,back,fill,catchBadge,death:0,animation:animated?createEnemyAnimation(e):null,lastX:e.x,lastAge:e.age,moving:false,rate:1};enemies.set(e.id,v);return v;}
 function createBullet(b){const g=new T.Group();scene.add(g);const w=b.weapon;const color=w.color||'#ffbf6c';let mesh;
  if(w.kind==='rocket'){mesh=sprite('ammo-2',31,g,0,0,1);mesh.rotation.z=-.48;}
  else if(w.kind==='arrow'){mesh=sprite('ammo-8',39,g,0,0,1);mesh.rotation.z=-.48;}
  else if(w.kind==='saw'){mesh=new T.Mesh(new T.CircleGeometry(13,14),mat('#c3d8df'));g.add(mesh);disk(g,0,0,5,'#ffce70',2);}
  else if(w.kind==='mortar'||w.kind==='bomb'){mesh=disk(g,0,0,10,'#30374a',1);glow(g,0,0,39,color,0);}
  else if(['plasma','gravity','poison','ice','drone'].includes(w.kind)){mesh=glow(g,0,0,w.kind==='plasma'?51:34,color,1);disk(g,0,0,5,color,2);}
  else{mesh=rect(g,0,0,w.kind==='flame'?22:w.kind==='enemy'?18:20,w.kind==='flame'?12:4,color,1);glow(g,0,0,w.kind==='flame'?55:25,color,0);}
  g.scale.set(2,2,1);g.position.z=35;bullets.set(b.id,{g,mesh});
 }
 function event(ev){
  if(['damage','melee'].includes(ev.type)&&ev.enemy.def.asset==='beast-0'){
   const v=enemies.get(ev.enemy.id)||createEnemy(ev.enemy);
   triggerEnemyAnimation(v.animation,ev.enemy,ev.type==='melee'?'attack':'hit');
  }
  if(ev.type==='spawn'&&ev.enemy.x<1000){ring(ev.enemy.x,185,85,'#a18aff',.8);burst(ev.enemy.x,190,'#9eafff',25,100,24,.9);}
  if(ev.type==='fire'){const v=units.get(ev.unit.id);if(v){v.flash=.12;const length=ev.unit.role==='tower'?76:42;burst(ev.unit.x+Math.cos(ev.unit.angle)*length,ev.unit.y+Math.sin(ev.unit.angle)*length,ev.weapon.color,ev.weapon.kind==='rapid'?4:9,150,28,.3);if(ev.unit.role==='tower')burst(ev.unit.x-20,ev.unit.y-5,'#ffcf70',2,65,5,.5);}}
  if(ev.type==='beam')beam(ev.x,ev.y,ev.tx,ev.ty,ev.color,ev.kind);
  if(ev.type==='lightning')lightning(ev.x,ev.y,ev.tx,ev.ty,ev.color);
  if(ev.type==='impact'){if(ev.size>40)explosion(ev.x,ev.y,ev.size*1.65,ev.color);else{burst(ev.x,ev.y,ev.color,14,190,32,.5);temp(glow(scene,ev.x,ev.y,ev.size*4,ev.color,46),.18,{expand:2});}}
  if(ev.type==='kill'){const e=ev.enemy;explosion(e.x,e.y,Math.max(65,e.size*(e.def.boss?.9:.6)),'#ffad37');burst(e.x,e.y,e.def.color,24,240,30,.9);shake=Math.max(shake,e.def.boss?16:4);const coin=label(`+${Math.round(e.maxHp/15)} ✦`,'#ffdb75',23);coin.position.set(e.x,e.y+45,60);temp(coin,1.2,{float:65});}
  if(ev.type==='damage'&&Math.random()<.35){const m=label(String(ev.amount),ev.color,ev.amount>150?29:22);m.position.set(ev.enemy.x+(Math.random()-.5)*35,ev.enemy.y+ev.enemy.size*.3,60);temp(m,.7,{float:57});}
  if(ev.type==='hurt'){shake=Math.max(shake,battle?.buffs.shield>0?1:4);}
  if(ev.type==='melee'){burst(battle.player.x+100,ev.enemy.y,'#ffd79a',12,130,20,.5);}
  if(ev.type==='aura'){ring(ev.enemy.x,ev.enemy.y,140,'#9bffc0',.8);burst(ev.enemy.x,ev.enemy.y,'#bbffac',15,70,28,1);}
  if(ev.type==='repair'||ev.type==='skill'&&ev.id==='heal'){ring(battle.player.x,370,200,'#83ffb0',.9);burst(battle.player.x,350,'#9bffb1',50,220,26,1.3);}
  if(ev.type==='skill'&&ev.id==='freeze'){for(const e of battle.enemies){ring(e.x,e.y,100,'#a2f6ff',.6);burst(e.x,e.y,'#c2ffff',20,160,20,.8);}}
  if(ev.type==='skill'&&ev.id==='barrage'){shake=7;}
 }
 function update(dt,model){battle=model;totalTime+=dt;
  tank.position.x=model.player.x;tank.rotation.z=Math.sin(totalTime*7)*.0025;tank.position.y=175+Math.sin(totalTime*9)*.8;
  for(const wheel of wheels)wheel.rotation.z-=dt*(model.move?4:1);
  towerCrew.forEach((g,i)=>{g.rotation.z=Math.sin(totalTime*2+i)*.025;const u=model.units.find(u=>u.role==='tower'&&u.index===i);g.visible=model.classic!==false||!!(u&&u.unlocked&&u.alive&&u.catId);if(model.classic===false&&u?.catId){g.children[0].material.map=textures.get(u.catId)??textures.get('cat-0');}});
  shield.position.x=shieldEdge.position.x=model.player.x;shield.visible=shieldEdge.visible=model.buffs.shield>0;shield.material.opacity=.06+Math.sin(totalTime*8)*.025;
  supportDrone.visible=model.buffs.drone>0;supportDrone.position.set(model.player.x+190,660+Math.sin(totalTime*4)*14,22);
  exhaust.material.opacity=.3+Math.random()*.3;
  for(const u of model.units){const v=units.get(u.id)||createUnit(u);v.g.position.set(u.x,u.y,u.role==='infantry'?22:u.role==='aux'?15:18);v.g.visible=u.visible!==false&&u.alive!==false;if(v.hpFill){v.hpFill.scale.x=Math.max(.001,58*u.hp/u.maxHp);v.hpFill.position.x=-29*(1-u.hp/u.maxHp);}
   const skin=u.role==='tower'&&model.classic!==false&&Math.floor(model.time/10)%2?u.weapon.asset.replace('turret-','turret_alt-'):(u.weapon.asset??'ammo-0');
   if(v.weapon!==u.weapon.id||v.skin!==skin)setUnitWeapon(v,u);
   v.pivot.rotation.z=u.angle;v.mesh.position.x=(u.role==='tower'?20:u.role==='infantry'?13:0)-u.recoil;
   if(u.role==='infantry'){v.body.position.y=Math.abs(Math.sin(totalTime*9+u.index))*2;v.legs.forEach((g,j)=>g.rotation.z=Math.sin(totalTime*9+u.index+j*Math.PI)*.43);}
   if(v.flash>0){v.flash-=dt;v.muzzle.visible=true;v.muzzle.scale.set(100+Math.random()*60,65+Math.random()*45,1);}else v.muzzle.visible=false;
  }
  const activeEnemies=new Set();
  for(const e of model.enemies){activeEnemies.add(e.id);const v=enemies.get(e.id)||createEnemy(e);v.g.position.set(e.x,e.y,12+e.id%5);const bounce=Math.sin(e.age*(e.def.speed/10+3)+e.phase);
   if(v.animation){
    const elapsed=e.age-v.lastAge;
    if(elapsed>0){const moved=Math.abs(e.x-v.lastX);v.moving=moved>.001;v.rate=moved/elapsed/e.def.speed;}
    v.lastX=e.x;v.lastAge=e.age;
    const pose=sampleEnemyAnimation(v.animation,e,v.moving,v.rate);
    v.body.material.map=textures.get(resolveSprite(textures,pose.asset,'beast-0'));
    v.body.scale.x=(model.player.x<e.x?-1:1)*e.size*272/235;
    v.body.position.y=-e.size*.43+89*e.size/235;v.body.rotation.z=0;
   }else{
    v.body.rotation.z=e.dead?e.deathTime?-.15:0:e.def.flying?Math.sin(totalTime*5+e.phase)*.06:bounce*.035;
    v.body.position.y=e.def.flying?0:Math.abs(bounce)*3;v.body.scale.y=(e.size*textures.get(resolveSprite(textures,e.def.asset,'beast-0')).image.height/textures.get(resolveSprite(textures,e.def.asset,'beast-0')).image.width)*(1+Math.sin(e.age*5)*.012);
   }
   v.body.material.color.set(e.flash>0?'#ffffff':e.slow>0||model.buffs.freeze>0?'#8ad7ff':e.poison>0?'#bbf78b':e.burn>0?'#ffb479':'#ffffff');
   v.body.material.opacity=e.dead?Math.max(0,1-v.death*2):e.burrowed?.25:1;
   if(e.dead){v.death+=dt;v.body.position.y-=v.death*40;v.body.rotation.z=-v.death*.8;}
   v.back.visible=v.fill.visible=!e.dead;v.fill.scale.x=Math.max(0,(e.size*.64-2)*e.hp/e.maxHp);v.fill.position.x=-(e.size*.64-2)*(1-e.hp/e.maxHp)/2;
   v.catchBadge.visible=!!e.catchable&&!e.dead;if(e.catchable)v.fill.material=mat('#8fffd6');
   v.shadow.position.y=175-e.y;v.shadow.visible=!e.dead;
   if(e.warning&&Math.random()<dt*8)burst(e.x,e.y+e.size*.3,'#ff685f',2,35,22,.4);
   if(e.burn>0&&Math.random()<dt*35)burst(e.x+(Math.random()-.5)*e.size*.65,e.y-e.size*.15,'#ff7b24',3,100,64,.7);
  }
  for(const[id,v]of enemies)if(!activeEnemies.has(id)){disposeGroup(v.g);enemies.delete(id);}
  const orbIds=new Set();for(const orb of model.orbs??[]){orbIds.add(orb.id);if(!orbViews.has(orb.id)){const g=new T.Group();scene.add(g);disk(g,0,0,9,'#ffd477',40);orbViews.set(orb.id,{g});}orbViews.get(orb.id).g.position.set(orb.x,orb.y+Math.sin(totalTime*3)*5,40);}for(const [id,v]of orbViews)if(!orbIds.has(id)){disposeGroup(v.g);orbViews.delete(id);}
  const activeBullets=new Set();for(const b of model.bullets){activeBullets.add(b.id);if(!bullets.has(b.id))createBullet(b);const v=bullets.get(b.id);v.g.position.set(b.x,b.y,36);v.g.rotation.z=b.angle;if(b.weapon.kind==='saw')v.mesh.rotation.z+=dt*20;if(['rocket','plasma','flame','poison'].includes(b.weapon.kind)&&Math.random()<dt*45)burst(b.x-Math.cos(b.angle)*25,b.y-Math.sin(b.angle)*25,b.weapon.color,2,45,42,.4);}
  for(const[id,v]of bullets)if(!activeBullets.has(id)){disposeGroup(v.g);bullets.delete(id);}
  for(let i=effects.length-1;i>=0;i--){const e=effects[i];e.life-=dt;const alpha=Math.max(0,e.life/e.maxLife);e.mesh.traverse(m=>{if(m.material&&m.material.transparent){m.material.opacity=alpha*(e.opacity??1);if(e.fire)m.material.color.setRGB(1,.12+alpha*.48,.015+alpha*.08);}});if(e.expand)e.mesh.scale.multiplyScalar(1+dt*e.expand);if(e.float)e.mesh.position.y+=e.float*dt;if(e.life<=0){disposeGroup(e.mesh,true);effects.splice(i,1);}}
  if(Math.random()<dt*14)burst(model.player.x-125,189,'#d3cbb1',2,34,28,.8);
  for(let i=particles.length-1;i>=0;i--){const p=particles[i];p.life-=dt;if(p.life<=0){particles.splice(i,1);continue;}p.x+=p.vx*dt;p.y+=p.vy*dt;p.vy-=p.gravity*dt;}
  sizes.fill(0);for(let i=0;i<particles.length;i++){const p=particles[i];positions[i*3]=p.x;positions[i*3+1]=p.y;positions[i*3+2]=p.z;colors[i*3]=p.color.r;colors[i*3+1]=p.color.g;colors[i*3+2]=p.color.b;sizes[i]=p.size;alphas[i]=Math.max(0,p.life/p.maxLife);}
  for(const name of ['position','pColor','pSize','pAlpha'])particleGeo.attributes[name].needsUpdate=true;
  shake=Math.max(0,shake-dt*20);camera.position.x=(Math.random()-.5)*shake;camera.position.y=(Math.random()-.5)*shake*.6;
  renderer.render(scene,camera);
 }
 function disposeGroup(g,uniqueGeometry=false){scene.remove(g);g.traverse(m=>{if(m.material&&!materials.has(m.material.color?.getStyle()+':'+m.material.opacity)){if(m.material.map&&m.userData.disposeTexture)m.material.map.dispose();if(![...materials.values()].includes(m.material))m.material.dispose();}if(uniqueGeometry&&m.geometry&&m.geometry!==plane&&m.geometry!==circle)m.geometry.dispose();});}
 function screenToWorld(clientX,clientY){const r=renderer.domElement.getBoundingClientRect();const vector=new T.Vector3((clientX-r.left)/r.width*2-1,-(clientY-r.top)/r.height*2+1,0);vector.unproject(camera);return{x:vector.x,y:vector.y};}
 function clear(){for(const v of [...enemies.values(),...bullets.values(),...units.values(),...orbViews.values()])disposeGroup(v.g);enemies.clear();bullets.clear();units.clear();orbViews.clear();for(const e of effects)disposeGroup(e.mesh,true);effects.length=particles.length=0;}
 addEventListener('resize',resize);resize();
 function enemyAnimations(){return [...enemies.entries()].filter(([,v])=>v.animation).map(([id,v])=>({id,state:v.animation.state,frame:v.animation.frame,x:v.g.position.x,y:v.g.position.y,width:Math.abs(v.body.scale.x),height:v.body.scale.y,ground:v.g.position.y+v.body.position.y-89*Math.abs(v.body.scale.x)/272}));}
 return{update,event,screenToWorld,clear,renderer,scene,camera,enemyAnimations};
}
