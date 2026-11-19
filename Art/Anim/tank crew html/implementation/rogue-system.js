(function(host){
 'use strict';
 const G=host.TankGeometry||(typeof module!=='undefined'?require('./tank-geometry.js'):null),WEAPONS=['mg','cannon','mortar'];
 const labels={mg:'MG',cannon:'Pháo',mortar:'Mortar'},SUPPLY={mg:120,cannon:12,mortar:8},TYPES={standard:{name:'Đạn tiêu chuẩn',damage:1},piercing:{name:'Đạn xuyên giáp',damage:1.25},explosive:{name:'Đạn nổ mạnh',damage:1.1},plasma:{name:'Đạn plasma',damage:1.5}};
 let runCounter=0;
 function rng(seed){let n=seed>>>0;return()=>{n=Math.imul(n,1664525)+1013904223|0;return(n>>>0)/4294967296;};}
 function init(s,seed){
  const random=rng(seed===undefined?(Date.now()+ ++runCounter*7919):seed^0x52ad717),deck=['supply','medical','wreck'];for(let i=2;i>0;i--){const j=Math.floor(random()*(i+1));[deck[i],deck[j]]=[deck[j],deck[i]];}
  s.player.skin='base';s.player.ammo={mg:220,cannon:24,mortar:16};s.rogue={wave:1,waveCount:8,waveEnd:Math.ceil(s.config.total/8),cleared:0,events:0,deck:['ammo',...deck],random,encounter:null,decisions:[],ammoType:'standard',bossReady:false,breakLeft:0,notice:'',noticeLeft:0};
 }
 function stop(s){s.travelSpeed=0;s.player.groundSpeed=0;}
 function age(s,dt){if(s.rogue)s.rogue.noticeLeft=Math.max(0,s.rogue.noticeLeft-dt);}
 function canFire(s,weapon){return !s.rogue||s.player.ammo[weapon]>0;}
 function consume(s,weapon){if(s.rogue)s.player.ammo[weapon]=Math.max(0,s.player.ammo[weapon]-1);}
 function damage(s,base){return base*(s.rogue?TYPES[s.rogue.ammoType].damage:1);}
 function cooldown(s,base){return base*(s.player.skin==='speed'?.9:1);}
 function speed(s){return G.profile(s.player).speed;}
 function ammoType(s){return s.rogue?s.rogue.ammoType:'standard';}
 function event(s){
  const r=s.rogue,type=r.deck[r.events%r.deck.length],id='event-'+r.events+'-wave-'+r.wave;let title,copy,options;
  if(type==='ammo'){
   title='LỀU QUÂN DỤNG';copy='Chọn một loại đạn cho cả đội súng trong lượt chơi này.';
   options=[{id:'piercing',icon:'piercing',tag:'XUYÊN GIÁP',name:'Đạn xuyên giáp',copy:'Sát thương súng +25%.',detail:'Hỏa lực ổn định',action:'Trang bị'},{id:'explosive',icon:'explosive',tag:'NỔ LAN',name:'Đạn nổ mạnh',copy:'Sát thương +10%, gây thêm nổ lan.',detail:'Hiệu quả với nhóm địch',action:'Trang bị'},{id:'plasma',icon:'plasma',tag:'HIẾM',name:'Đạn plasma',copy:'Sát thương súng +50%.',detail:'Lựa chọn đạn đặc biệt',action:'Trang bị',ad:true}];
  }else if(type==='supply'){
   title='TIẾP TẾ QUÂN DỤNG';copy='Kho tiếp tế còn một thùng. Chọn đạn cho một loại vũ khí.';
   options=WEAPONS.map(w=>({id:w,icon:w,tag:'TIẾP ĐẠN',name:'Đạn '+labels[w],copy:'Nhận thêm '+SUPPLY[w]+' viên.',detail:'Đang có '+s.player.ammo[w]+' viên',action:'Lấy đạn',disabled:!s.player.weapons.includes(w)}));
  }else if(type==='medical'){
   title='TRẠM Y TẾ';copy='Chọn cứu chữa cho chiếc tank hoặc lấy phụ tùng dự phòng.';
   options=[{id:'heal',icon:'heal',tag:'SỬA CHỮA',name:'Hồi 90 máu',copy:'Phục hồi tối đa 90 HP cho tank.',detail:Math.ceil(s.player.hp)+' / '+s.player.maxHP+' HP',action:'Hồi máu',disabled:s.player.hp>=s.player.maxHP},{id:'gears',icon:'gears',tag:'PHỤ TÙNG',name:'Lấy 30 bánh răng',copy:'Cộng vào tài nguyên đang có.',detail:'Mang theo cho hành trình',action:'Lấy bánh răng'}];
  }else{
   const offered=r.random()<.5?'speed':'heavy',p=G.data.profiles[offered];title='CHIẾC TANK BỎ LẠI';copy='Đội tìm được một chiếc tank còn chạy được. Đổi xe hoặc giữ xe hiện tại.';
   options=[{id:offered,icon:offered,tag:'ĐỔI TANK',name:p.name,copy:offered==='speed'?'300 HP · di chuyển +25% · bắn nhanh hơn.':'480 HP · bọc thép dày · di chuyển chậm hơn.',detail:'Giữ đội mèo, vũ khí và đạn hiện có',action:'Đổi sang xe này'},{id:'keep',icon:s.player.skin,tag:'XE HIỆN TẠI',name:'Giữ tank đang dùng',copy:G.profile(s.player).name,detail:Math.ceil(s.player.hp)+' / '+s.player.maxHP+' HP',action:'Giữ xe hiện tại'}];
  }
  return{id,type,title,copy,options,wave:r.wave};
 }
 function next(s){
  const r=s.rogue;r.encounter=null;s.phase='wave';s.spawnTimer=.8;
  if(r.wave>=r.waveCount){r.bossReady=true;s.alert=0;return;}
  r.wave++;r.waveEnd=Math.ceil(r.wave*s.config.total/r.waveCount);s.alert=1.8;
 }
 function afterCombat(s){
  const r=s.rogue;if(!r||s.phase!=='wave'||r.bossReady||s.queueIndex<r.waveEnd||s.enemies.some(e=>!e.dead)||host.TankMeleeSystem.active(s))return;
  r.cleared=r.wave;s.bullets=[];stop(s);
  if(r.wave%2===0){s.phase='encounter';r.encounter=event(s);r.events++;}
  else{s.phase='wave_break';r.breakLeft=1.25;}
 }
 function wait(s,dt){stop(s);if(s.phase==='wave_break'){s.rogue.breakLeft-=dt;if(s.rogue.breakLeft<=0)next(s);}}
 function resolve(s,choiceID,eventID){
  const r=s.rogue,e=r&&r.encounter;if(s.phase!=='encounter'||!e||eventID&&eventID!==e.id)return false;
  const option=e.options.find(o=>o.id===choiceID);if(choiceID!=='skip'&&(!option||option.disabled))return false;let message='Tiếp tục hành trình';
  if(choiceID!=='skip'){
   if(e.type==='ammo'){r.ammoType=choiceID;message='Đã trang bị '+TYPES[choiceID].name.toLowerCase();}
   else if(e.type==='supply'){s.player.ammo[choiceID]=Math.min(choiceID==='mg'?999:99,s.player.ammo[choiceID]+SUPPLY[choiceID]);message='+'+SUPPLY[choiceID]+' đạn '+labels[choiceID];}
   else if(e.type==='medical'){if(choiceID==='heal'){const before=s.player.hp;s.player.hp=Math.min(s.player.maxHP,s.player.hp+90);message='+'+Math.round(s.player.hp-before)+' HP';}else{s.lootWallet.gears+=30;message='+30 bánh răng';}}
   else if(choiceID!=='keep'){const p=G.data.profiles[choiceID],ratio=s.player.hp/s.player.maxHP;s.player.skin=choiceID;s.player.maxHP=p.maxHP;s.player.hp=Math.max(1,Math.round(ratio*p.maxHP));message='Đã chuyển sang '+p.name.toLowerCase();}
  }
  r.decisions.push({wave:e.wave,type:e.type,choice:choiceID,ad:!!(option&&option.ad)});r.notice=message;r.noticeLeft=2.8;next(s);return true;
 }
 function splash(s,b,target,api){if(b.faction!=='player'||b.ammoType!=='explosive')return;const c=api.center(target);for(const other of s.enemies)if(other!==target&&!other.dead){const p=api.center(other);if(Math.hypot(p.x-c.x,p.y-c.y)<125)api.damageEnemy(s,other,b.damage*.28,'splash');}}
 const api={WEAPONS,labels,SUPPLY,TYPES,init,age,canFire,consume,damage,cooldown,speed,ammoType,afterCombat,wait,resolve,splash,readyForBoss:s=>!s.rogue||s.rogue.bossReady,event};host.TankRogueSystem=api;if(typeof module!=='undefined'&&module.exports)module.exports=api;
})(globalThis);
