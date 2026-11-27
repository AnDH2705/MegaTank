// Geometry/rotor/muzzle registration reference. Resolve models from original Art manifests.
const models = {}; // Runtime definitions assembled from Art/Asset/Enemies/*/metadata.
function native(e,type){const model=models[type],scale=e.w/(type==='helicopter'?1400:1200),origin=type==='trojan'?[800,952]:model.origin_px;return{model,scale,origin,face:type==='trojan'?1:-1,baseY:type==='helicopter'?e.y:930};}
 function recoil(e){const age=2.8-e.fire;return e.enemyType==='cannon_car'&&age>=0&&age<.35?Math.sin(Math.PI*age/.35)*16:0;}
 function muzzle(e,target){if(!e.enemyType||e.enemyType==='trojan')return null;const g=native(e,e.enemyType),gun=g.model.gun,p=gun.pivot_px,tip=gun.muzzle_px,wx=e.x+g.face*(p[0]-g.origin[0])*g.scale,wy=g.baseY+(p[1]-g.origin[1])*g.scale,rest=Math.atan2(tip[1]-p[1],tip[0]-p[0]),aim=Math.max(-.6,Math.min(.65,Math.atan2(target.y-wy,(target.x-wx)*g.face))),angle=aim-rest,dx=tip[0]-p[0]-recoil(e),dy=tip[1]-p[1];e.gunAngle=angle;const x=wx+g.face*(Math.cos(angle)*dx-Math.sin(angle)*dy)*g.scale,y=wy+(Math.sin(angle)*dx+Math.cos(angle)*dy)*g.scale;return{x,y,angle:Math.atan2(target.y-y,target.x-x)};}
 function draw(ctx,images,e,s,wreck=false){if(!e.enemyType)return false;const type=e.enemyType,g=native(e,type),model=g.model;ctx.save();ctx.translate(e.x,g.baseY);ctx.scale(g.face*g.scale,g.scale);ctx.translate(-g.origin[0],-g.origin[1]);if(e.hurt>0)ctx.filter='brightness(1.5)';
  const layers=model.layers.slice().sort((a,b)=>a.z_order-b.z_order),seat=model.driver||model.pilot;let pilot=false,dogs=false;
  const part=(p,angle=0)=>{const q=p.pivot_canvas_px;ctx.save();ctx.translate(q[0],q[1]);ctx.rotate(angle);if(p.id==='barrel')ctx.translate(-recoil(e),0);const im=images['enemy_'+type+'_'+p.part];if(im)ctx.drawImage(im,p.position_px[0]-q[0],p.position_px[1]-q[1],...p.size_px);ctx.restore();};
  const pilotDraw=()=>{const clipKey=type==='helicopter'?(e.fire<.15?'pilot_fire':'pilot_fly'):'driver_drive',clip=model.actorClips.clips[type==='helicopter'?(e.fire<.15?'fire':'fly'):'drive'],im=images['enemy_'+type+'_'+clipKey];if(!im||!clip)return;const n=Math.floor((e.t%clip.duration)/clip.duration*clip.frames),sw=im.width/clip.cols,sh=im.height/clip.rows,z=seat.preview_scale,p=seat.socket_px;ctx.drawImage(im,n%clip.cols*sw,Math.floor(n/clip.cols)*sh,sw,sh,p[0]-clip.pivot[0]*z,p[1]-clip.pivot[1]*z,clip.cell[0]*z,clip.cell[1]*z);};
  const insideDogs=()=>{ctx.save();ctx.beginPath();ctx.rect(...model.feet_aperture_px);ctx.clip();for(let i=0;i<3;i++){const q=model.dog_sockets[i].position_px;host.TankCrew.figure(ctx,images,'dog_walk',s.time+i*.19,q[0],q[1],.22/g.scale,-1);}ctx.restore();};
  for(const p of layers){if(seat&&!pilot&&p.z_order>=seat.draw_order){pilotDraw();pilot=true;}if(type==='trojan'&&!dogs&&p.z_order>=50){if(!wreck)insideDogs();dogs=true;}
   if(type==='trojan'&&p.parent==='shell_root'&&wreck){const t=e.releaseAge,pivot=model.shell_root.pivot_px;ctx.save();ctx.translate(pivot[0]+t*400,pivot[1]-950*t+750*t*t);ctx.rotate(t*.85);ctx.translate(-pivot[0],-pivot[1]);ctx.globalAlpha*=Math.max(0,1-(t-1.55)/.45);part(p);ctx.restore();}
   else if(p.id==='main_rotor'){const im=images['enemy_'+type+'_'+p.part],q=p.pivot_canvas_px,w=p.size_px[0];ctx.save();ctx.translate(...q);ctx.scale(1,p.size_px[1]/w);ctx.rotate(e.t*30);ctx.globalAlpha*=model.rotors.main.opacity;if(im)ctx.drawImage(im,-w/2,-w/2,w,w);ctx.restore();}
   else if(p.id==='tail_rotor')part(p,e.t*36);
   else if(p.id.startsWith('wheel_'))part(p,wreck?0:-e.x/30);
   else if(p.id==='barrel'||p.id==='machine_gun')part(p,e.gunAngle||0);
   else part(p);
  }ctx.restore();return true;
 }
