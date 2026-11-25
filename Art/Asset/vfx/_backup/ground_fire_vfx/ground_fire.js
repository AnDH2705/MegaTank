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
   this.time=0;this.age=0;this.active=true;
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
   ctx.save();ctx.globalCompositeOperation='source-over';
   ctx.beginPath();ctx.rect(this.xStart-cameraX,0,this.width,viewH);ctx.clip();
   const innerLeft=this.xStart+capW-join,innerRight=this.xEnd-capW+join;
   if(innerRight>innerLeft){
    ctx.save();ctx.beginPath();ctx.rect(innerLeft-cameraX,0,innerRight-innerLeft,viewH);ctx.clip();
    const stride=(META.tileWidth-META.seamOverlap)*s;
    // Anchor middle tiles to the world so changing an endpoint does not move existing flames.
    const first=Math.floor(Math.max(innerLeft,cameraX-META.tileWidth*s)/stride);
    const last=Math.floor(Math.min(innerRight,cameraX+viewW)/stride);
    for(let index=first;index<=last;index++){
     const variant=mod(index,3),tileFrame=mod(frame+mod(index,8)*3,8);
     ctx.drawImage(this.textures.middle,variant*META.tileWidth,tileFrame*META.frameHeight,META.tileWidth,META.frameHeight,index*stride-cameraX,top,META.tileWidth*s,h);
    }
    ctx.restore();
   }
   ctx.drawImage(this.textures.left,0,frame*META.frameHeight,META.capWidth,META.frameHeight,capLeft,top,capW,h);
   ctx.drawImage(this.textures.right,0,frame*META.frameHeight,META.capWidth,META.frameHeight,capRight,top,capW,h);
   ctx.restore();
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
