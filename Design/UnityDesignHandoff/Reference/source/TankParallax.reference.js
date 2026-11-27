// HTML visual implementation reference. Port to C#; do not add a JavaScript runtime.
(function(host){
 'use strict';
 const model=JSON.parse(document.getElementById('tank-parallax-data').textContent);
 function create(ctx,images){
  let lastLayers=[];
  function draw(s,front=false){
   const background=model.backgrounds[s.config.background];if(!background)return;
   if(!front)lastLayers=[];
   for(const layer of background.layers){
    if(layer.front!==front)continue;const im=images[layer.key];if(!im)continue;
    const scroll=s.scroll.road*layer.factor;
    lastLayers.push({key:layer.key,name:layer.name,scroll,factor:layer.factor,front});
    if(layer.repeat==='none'){ctx.drawImage(im,0,0,1920,1080);continue;}
    const first=Math.floor(scroll/1920);
    for(let i=first-1;i<=first+1;i++){
     const x=i*1920-scroll,flip=Math.abs(i%2)===1;ctx.save();ctx.translate(flip?x+1920:x,0);ctx.scale(flip?-1:1,1);ctx.drawImage(im,0,0,1920,1080);ctx.restore();
    }
   }
   host.TankParallax.lastLayers=lastLayers;
  }
  return{draw};
 }
 host.TankParallax={model,create,lastLayers:[]};
})(globalThis);
