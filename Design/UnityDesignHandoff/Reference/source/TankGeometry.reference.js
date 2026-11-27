// HTML visual implementation reference. Port to C#; do not add a JavaScript runtime.
(function(host){
 'use strict';
 const data=typeof document!=='undefined'?JSON.parse(document.getElementById('tank-skin-data').textContent):require('./tank-variants.json');
 function profile(actor){return data.profiles[actor&&actor.skin||'base']||data.profiles.base;}
 function station(actor,id){return profile(actor).stations[id];}
 function rear(actor){return profile(actor).layers.find(p=>p.name==='11_front_hull_frame').position_px[0];}
 function muzzle(actor,weapon,face,target){const g=profile(actor).guns[weapon],z=actor.w/1351,p=g.pivot,t=g.tip,x=actor.x+face*(p[0]-652)*z,y=930+(p[1]-1258)*z;if(weapon==='mortar')return{x:actor.x+face*(t[0]-652)*z,y:930+(t[1]-1258)*z,angle:-Math.PI/2};const a=Math.max(-.85,Math.min(.55,Math.atan2(target.y-y,(target.x-x)*face))),dx=(t[0]-p[0])*z,dy=(t[1]-p[1])*z;return{x:x+face*(Math.cos(a)*dx-Math.sin(a)*dy),y:y+Math.sin(a)*dx+Math.cos(a)*dy,angle:Math.atan2(Math.sin(a),face*Math.cos(a))};}
 const center=actor=>({x:actor.x,y:930+(profile(actor).centerY-1258)*actor.w/1351});
 const api={data,profile,station,rear,muzzle,center};host.TankGeometry=api;if(typeof module!=='undefined'&&module.exports)module.exports=api;
})(globalThis);
