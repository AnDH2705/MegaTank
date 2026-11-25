/* Ground fire: animated end caps + repeated middle tiles. World units are pixels. */
(function(root){
 'use strict';
 const META=Object.freeze({frames:8,fps:12,frameHeight:256,groundAnchorY:224,capWidth:80,tileWidth:192,tileVariants:3,seamOverlap:32});
 const mod=(n,m)=>((n%m)+m)%m;
 function finite(n,label){if(!Number.isFinite(n))throw new TypeError(label+' must be finite');return n;}
 class GroundFireVFX{
  constructor(textures,options={}){
   this.textures=textures;
   this.scale=options.scale??1;this.fps=options.fps??META.fps;
   if(!Number.isFinite(this.scale)||this.scale<=0)throw new RangeError('scale must be positive');
   if(!Number.isFinite(this.fps)||this.fps<=0)throw new RangeError('fps must be positive');
   this.glowIntensity=options.glowIntensity??0.7;this.particles=options.particles??true;this.time=0;this.age=0;this.active=true;
   this.setRange(options.xStart??320,options.xEnd??1080,options.groundY??1000);
   this.restart(options.durationSeconds??Infinity);
  }
  setRange(xStart,xEnd,groundY=this.groundY){
   finite(xStart,'xStart');finite(xEnd,'xEnd');finite(groundY,'groundY');
   this.xStart=Math.min(xStart,xEnd);this.xEnd=Math.max(xStart,xEnd);this.groundY=groundY;
   return this;
  }
  get width(){return this.xEnd-this.xStart;}
  get frame(){return mod(Math.floor(this.time*this.fps),META.frames);}
  restart(durationSeconds=Infinity){
   if(durationSeconds!==Infinity&&(!Number.isFinite(durationSeconds)||durationSeconds<0))throw new RangeError('durationSeconds must be nonnegative');
   this.durationSeconds=durationSeconds;this.time=0;this.age=0;this.active=durationSeconds>0;return this;
  }
  setActive(active){this.active=!!active&&this.age<this.durationSeconds;return this;}
  update(deltaSeconds){
   finite(deltaSeconds,'deltaSeconds');if(deltaSeconds<0)throw new RangeError('deltaSeconds must be nonnegative');
   if(this.active){this.time+=deltaSeconds;this.age+=deltaSeconds;if(this.age>=this.durationSeconds)this.active=false;}
  }
  getDamageRect(height=32){
   finite(height,'height');if(height<0)throw new RangeError('height must be nonnegative');
   return this.active&&this.width>0?{x:this.xStart,y:this.groundY-height,width:this.width,height}:null;
  }
  draw(ctx,cameraX=0,cameraY=0){
   if(!this.active||this.width<=0)return;
   const s=this.scale,frame=this.frame,top=this.groundY-META.groundAnchorY*s-cameraY;
   const h=META.frameHeight*s,viewW=ctx.canvas.width,viewH=ctx.canvas.height;
   if(this.xEnd<=cameraX||this.xStart>=cameraX+viewW||top>=viewH||top+h<=0)return;
   const capW=Math.min(META.capWidth*s,this.width/2),join=Math.min(16*s,capW/2);
   const capLeft=this.xStart-cameraX,capRight=this.xEnd-capW-cameraX;
   this.drawGlow(ctx,cameraX,cameraY);ctx.save();ctx.globalCompositeOperation='source-over';
   // Mask the complete middle layer at both outer endpoints, so a tall tongue is never hard-cut at a cap join.
   if(!this.layer)this.layer=typeof root.OffscreenCanvas==='function'?new root.OffscreenCanvas(viewW,Math.ceil(h)):ctx.canvas.ownerDocument?ctx.canvas.ownerDocument.createElement('canvas'):new ctx.canvas.constructor(viewW,Math.ceil(h));
   if(this.layer.width!==viewW)this.layer.width=viewW;if(this.layer.height!==Math.ceil(h))this.layer.height=Math.ceil(h);
   const layer=this.layer.getContext('2d');layer.clearRect(0,0,viewW,Math.ceil(h));layer.globalCompositeOperation='source-over';
   const stride=(META.tileWidth-META.seamOverlap)*s;
   const first=Math.floor(Math.max(this.xStart,cameraX-META.tileWidth*s)/stride),last=Math.floor(Math.min(this.xEnd,cameraX+viewW)/stride);
   for(let index=first;index<=last;index++){
    const variant=mod(index,3),tileFrame=mod(frame+mod(index,8)*3,8);
    layer.drawImage(this.textures.middle,variant*META.tileWidth,tileFrame*META.frameHeight,META.tileWidth,META.frameHeight,index*stride-cameraX,0,META.tileWidth*s,h);
   }
   const fade=Math.min(.45,Math.max(1,capW)/this.width),mask=layer.createLinearGradient(this.xStart-cameraX,0,this.xEnd-cameraX,0);
   mask.addColorStop(0,'#ffffff00');mask.addColorStop(fade,'#ffffffff');mask.addColorStop(1-fade,'#ffffffff');mask.addColorStop(1,'#ffffff00');
   layer.globalCompositeOperation='destination-in';layer.fillStyle=mask;layer.fillRect(0,0,viewW,Math.ceil(h));layer.globalCompositeOperation='source-over';
   ctx.drawImage(this.layer,0,top);
   ctx.drawImage(this.textures.left,0,frame*META.frameHeight,META.capWidth,META.frameHeight,capLeft,top,capW,h);
   ctx.drawImage(this.textures.right,0,frame*META.frameHeight,META.capWidth,META.frameHeight,capRight,top,capW,h);
   ctx.restore();this.drawParticles(ctx,cameraX,cameraY);
  }

  drawGlow(ctx,cameraX,cameraY){
   if(this.glowIntensity<=0)return;
   const s=this.scale,x=(this.xStart+this.xEnd)/2-cameraX,y=this.groundY-cameraY;
   const pulse=.91+.06*Math.sin(this.time*7.2)+.03*Math.sin(this.time*13.1);
   ctx.save();ctx.globalCompositeOperation='lighter';
   const oval=(cy,rx,ry,alpha)=>{ctx.save();ctx.translate(x,cy);ctx.scale(rx,ry);const g=ctx.createRadialGradient(0,0,0,0,0,1);g.addColorStop(0,'rgba(255,173,28,'+alpha+')');g.addColorStop(.35,'rgba(255,105,6,'+(alpha*.48)+')');g.addColorStop(1,'rgba(255,75,0,0)');ctx.fillStyle=g;ctx.fillRect(-1,-1,2,2);ctx.restore();};
   oval(y-54*s,this.width*.56,142*s,.23*this.glowIntensity*pulse);
   oval(y+4*s,this.width*.53,28*s,.38*this.glowIntensity*pulse);ctx.restore();
  }
  drawParticles(ctx,cameraX,cameraY){
   if(!this.particles)return;const s=this.scale;ctx.save();ctx.globalCompositeOperation='lighter';
   const first=Math.floor(this.xStart/(42*s)),last=Math.ceil(this.xEnd/(42*s));
   for(let i=first;i<=last;i++){
    const seed=mod(Math.sin(i*127.1+311.7)*43758.5453,1),life=.8+seed*.7;
    const phase=mod(this.time/life+seed*7.3,1),x=(i+.5)*42*s+Math.sin(phase*5+i)*12*s;
    if(x<this.xStart||x>this.xEnd)continue;
    const y=this.groundY-(18+phase*(145+seed*90))*s,alpha=Math.sin(Math.PI*phase)*(.46+.3*seed);
    const size=(2+seed*2.1)*s;ctx.globalAlpha=alpha;
    const g=ctx.createRadialGradient(x-cameraX,y-cameraY,0,x-cameraX,y-cameraY,size*4);g.addColorStop(0,'#fff7bb');g.addColorStop(.2,'#ffd348');g.addColorStop(1,'#ff7b0000');ctx.fillStyle=g;ctx.beginPath();ctx.arc(x-cameraX,y-cameraY,size*4,0,Math.PI*2);ctx.fill();
    ctx.fillStyle='#fff5bc';ctx.beginPath();ctx.ellipse(x-cameraX,y-cameraY,size*.44,size*1.12,-.3,0,Math.PI*2);ctx.fill();
    if(seed>.68){const blink=Math.pow(Math.max(0,Math.sin(this.time*10+i)),12);ctx.globalAlpha=alpha*blink;ctx.fillRect(x-cameraX-size*3,y-cameraY-size*.18,size*6,size*.36);ctx.fillRect(x-cameraX-size*.18,y-cameraY-size*3,size*.36,size*6);}
   }ctx.restore();
  }
 }
 async function loadGroundFireTextures(basePath='assets'){
  if(typeof Image==='undefined')throw new Error('In Node, pass decoded images directly to the constructor.');
  const load=name=>new Promise((resolve,reject)=>{const image=new Image();image.onload=()=>resolve(image);image.onerror=()=>reject(new Error('Cannot load '+name));image.src=basePath+'/'+name;});
  const [left,middle,right]=await Promise.all([load('fire_left_8f.png'),load('fire_middle_3variants_8f.png'),load('fire_right_8f.png')]);return {left,middle,right};
 }
 const api={GroundFireVFX,loadGroundFireTextures,GROUND_FIRE_META:META};
 if(typeof module!=='undefined'&&module.exports)module.exports=api;else Object.assign(root,api);
})(typeof globalThis!=='undefined'?globalThis:this);
