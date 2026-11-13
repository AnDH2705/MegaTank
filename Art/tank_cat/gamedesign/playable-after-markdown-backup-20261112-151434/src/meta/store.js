import {CURRENCIES,CurrencyWallet,IdleRewardSystem,SaveManager,copy,nowUnix,makeId,requireRule,integer,encodeSave,decodeSave} from './economy.js';
import {createHero,addFromSummon,levelUp,ascend,recycleCopies,recycleHero,placeNexus,refreshNexus,SLOTS,RARITIES} from './heroes.js';
import {equipGear,unequipGear,enhanceGear,upgradeRelic,equipArtifact} from './items.js';
import {summon} from './gacha.js';
import {createRequest,finishBattle} from './campaign.js';

export function newProfile(data,now=nowUnix()){
 const roster=data.balance.starting_roster.map(id=>createHero(id,data));
 return {schema_version:1,revision:0,saved_at_unix:now,profile:{player_name:'Chỉ huy mèo',created_at_unix:now},wallet:copy(data.balance.starting_wallet),roster,inventory:{gear:[],artifacts:[]},nexus:{slot_uids:Array(data.balance.nexus.max_slots).fill('')},idle:{last_accrual_unix:now},gacha:{pity_elite:0,pity_legendary:0,rng_state:crypto.getRandomValues(new Uint32Array(1))[0]||1},campaign:{highest_cleared:0,selected_index:1,team:[roster[0].uid,'',roster[1].uid,roster[2].uid,''],first_cleared_ids:{},clear_counts:{}},active_battle:null};
}
export function validateProfile(s,data){
 const validId=value=>typeof value==='string'&&/^[a-z]+_[a-zA-Z0-9_-]{1,90}$/.test(value);
 requireRule(s&&integer(s.schema_version,1),'Phiên bản lưu không hợp lệ');requireRule(s.schema_version<=1,'Bản lưu thuộc phiên bản mới hơn');
 // Only optional v1 additions may default; balances, clocks and RNG must survive intact.
 if(s.wallet&&s.wallet.relic_stone===undefined)s.wallet.relic_stone=0;
 if(s.inventory&&s.inventory.artifacts===undefined)s.inventory.artifacts=[];
 if(Array.isArray(s.roster))for(const h of s.roster){h.locked??=false;h.relic_level??=0;h.artifact_uid??='';h.equipped_gear??={};}
 requireRule(integer(s.revision)&&integer(s.saved_at_unix)&&typeof s.profile?.player_name==='string'&&s.profile.player_name.length<=40&&integer(s.profile.created_at_unix),'Hồ sơ không hợp lệ');
 for(const key of CURRENCIES)requireRule(integer(s.wallet?.[key],0,data.balance.currencies.cap),`Số dư ${key} không hợp lệ`);
 requireRule(Array.isArray(s.roster)&&s.roster.length<=data.heroes.length,'Danh sách tướng không hợp lệ');const ids=new Set(),heroIds=new Set();
 for(const h of s.roster){requireRule(h&&validId(h.uid)&&!ids.has(h.uid)&&data.heroById[h.hero_id]&&!heroIds.has(h.hero_id),'Tướng hoặc UID không hợp lệ');ids.add(h.uid);heroIds.add(h.hero_id);requireRule(integer(h.level,1,data.balance.ascension.level_caps[h.tier]??0)&&integer(h.stored_copies)&&integer(h.relic_level,0,data.balance.relic.max_level)&&typeof h.locked==='boolean'&&h.equipped_gear&&typeof h.artifact_uid==='string','Trạng thái tướng không hợp lệ');}
 requireRule(Array.isArray(s.campaign?.team)&&s.campaign.team.length===5&&s.campaign.team.every(id=>id===''||ids.has(id))&&new Set(s.campaign.team.filter(Boolean)).size===s.campaign.team.filter(Boolean).length&&integer(s.campaign.highest_cleared,0,20)&&integer(s.campaign.selected_index,1,20),'Đội hình/tiến trình không hợp lệ');
 requireRule(s.campaign.first_cleared_ids&&s.campaign.clear_counts,'Thiếu lịch sử chiến dịch');for(let i=1;i<=s.campaign.highest_cleared;i++){const id=data.stages[i-1].stage_id;requireRule(s.campaign.first_cleared_ids[id]===true&&integer(s.campaign.clear_counts[id],1),'Lịch sử ải bị thiếu');}
 requireRule(Array.isArray(s.nexus?.slot_uids)&&s.nexus.slot_uids.length===data.balance.nexus.max_slots&&s.nexus.slot_uids.every(id=>id===''||ids.has(id))&&new Set(s.nexus.slot_uids.filter(Boolean)).size===s.nexus.slot_uids.filter(Boolean).length,'Cộng hưởng không hợp lệ');
 requireRule(integer(s.idle?.last_accrual_unix)&&integer(s.gacha?.rng_state,1,4294967295)&&integer(s.gacha.pity_elite,0,data.balance.gacha.pity_elite-1)&&integer(s.gacha.pity_legendary,0,data.balance.gacha.pity_legendary-1),'Thời gian/bảo hiểm triệu hồi không hợp lệ');
 requireRule(Array.isArray(s.inventory?.gear)&&Array.isArray(s.inventory.artifacts),'Túi đồ không hợp lệ');const itemIds=new Set();
 for(const gear of s.inventory.gear){requireRule(validId(gear.uid)&&!itemIds.has(gear.uid)&&data.gearById[gear.gear_id]&&RARITIES.includes(gear.rarity)&&integer(gear.stars,0,data.balance.gear.max_stars)&&(gear.equipped_by===''||ids.has(gear.equipped_by)),'Trang bị không hợp lệ');itemIds.add(gear.uid);if(gear.equipped_by){const h=s.roster.find(h=>h.uid===gear.equipped_by);requireRule(h.equipped_gear[data.gearById[gear.gear_id].slot]===gear.uid,'Trang bị trỏ sai tướng');}}
 for(const art of s.inventory.artifacts){requireRule(validId(art.uid)&&!itemIds.has(art.uid)&&data.artifactById[art.artifact_id]&&(art.equipped_by===''||ids.has(art.equipped_by)),'Cổ vật không hợp lệ');itemIds.add(art.uid);if(art.equipped_by)requireRule(s.roster.find(h=>h.uid===art.equipped_by).artifact_uid===art.uid,'Cổ vật trỏ sai tướng');}
 for(const h of s.roster){for(const [slot,uid]of Object.entries(h.equipped_gear)){const gear=s.inventory.gear.find(g=>g.uid===uid);requireRule(SLOTS.includes(slot)&&gear&&gear.equipped_by===h.uid&&data.gearById[gear.gear_id].slot===slot,'Ô trang bị không hợp lệ');}if(h.artifact_uid)requireRule(s.inventory.artifacts.some(a=>a.uid===h.artifact_uid&&a.equipped_by===h.uid),'Ô cổ vật không hợp lệ');}
 if(s.active_battle)requireRule(typeof s.active_battle.id==='string'&&typeof s.active_battle.owner==='string'&&integer(s.active_battle.stage_index,1,s.campaign.highest_cleared+1)&&s.active_battle.status==='active','Trận đang chơi không hợp lệ');
 return true;
}

export class GameStore{
 constructor(data,storage,{now=nowUnix,locks=globalThis.navigator?.locks}={}){this.data=data;this.now=now;this.owner=makeId('tab');this.locks=locks;this.saves=new SaveManager(storage,s=>validateProfile(s,data));this.state=null;this.listeners=new Set();this.queue=Promise.resolve();}
 async init(){return this.exclusive(()=>{this.state=this.saves.load_game();if(!this.state){this.state=newProfile(this.data,this.now());this.saves.save_game(this.state);}refreshNexus(this.state,this.data);return this;});}
 exclusive(callback){if(this.locks)return this.locks.request('meow-bastion-economy',callback);const task=this.queue.then(callback);this.queue=task.catch(()=>{});return task;}
 subscribe(callback){this.listeners.add(callback);return ()=>this.listeners.delete(callback);}
 notify(){for(const callback of this.listeners)try{callback(this.state);}catch(error){console.error(error);}}
 sync(){const loaded=this.saves.load_game();if(loaded&&loaded.revision!==this.state.revision){this.state=loaded;this.notify();}}
 peekIdle(){return new IdleRewardSystem(this.state,this.data.balance).peek_rewards(this.now());}
 async command(name,args={}){
  return this.exclusive(()=>{
   const latest=this.saves.load_game();requireRule(latest,'Không tìm thấy hồ sơ');this.state=copy(latest);const s=this.state,data=this.data,wallet=new CurrencyWallet(s.wallet,data.balance);
   const hero=()=>{const h=s.roster.find(h=>h.uid===args.uid);requireRule(h,'Không tìm thấy tướng');return h;};
   let result;
   try{
    switch(name){
     case 'claim':result=new IdleRewardSystem(s,data.balance).claim(wallet,this.now());break;
     case 'level':result=levelUp(hero(),s,data,wallet,args.steps??1);break;
     case 'ascend':result=ascend(hero(),s,data,args.offered??[]);break;
     case 'lock':hero().locked=!!args.locked;break;
     case 'recycle_copies':result=recycleCopies(hero(),args.count,wallet,data);break;
     case 'recycle_hero':result=recycleHero(hero(),s,data,wallet);break;
     case 'nexus':placeNexus(s,data,args.uid??'',args.index);break;
     case 'team':requireRule(integer(args.slot,0,4),'Vị trí không hợp lệ');if(args.uid){hero();requireRule(!data.heroById[hero().hero_id].is_fodder_only,'Tướng nguyên liệu không thể vào đội hỗ trợ');s.campaign.team=s.campaign.team.map(uid=>uid===args.uid?'':uid);}s.campaign.team[args.slot]=args.uid??'';break;
     case 'equip':{const gear=s.inventory.gear.find(g=>g.uid===args.gear_uid);requireRule(gear,'Không tìm thấy trang bị');equipGear(hero(),gear,s,data);break;}
     case 'unequip':requireRule(SLOTS.includes(args.slot),'Ô trang bị không hợp lệ');unequipGear(hero(),args.slot,s);break;
     case 'enhance':{const gear=s.inventory.gear.find(g=>g.uid===args.gear_uid);requireRule(gear,'Không tìm thấy trang bị');result=enhanceGear(gear,wallet,data);break;}
     case 'relic':upgradeRelic(hero(),wallet,data);break;
     case 'artifact':equipArtifact(hero(),args.artifact_uid??'',s);break;
     case 'summon':result=summon(s,data,wallet,args.count);break;
     case 'buy_scroll':requireRule(integer(args.count,1,10),'Số vé không hợp lệ');requireRule(wallet.try_spend({gem:data.balance.currencies.gems_per_scroll*args.count}),'Không đủ ngọc');wallet.add('summon_scroll',args.count);break;
     case 'select_stage':requireRule(integer(args.index,1,data.stages.length),'Ải không hợp lệ');s.campaign.selected_index=args.index;break;
     case 'begin_battle':result=createRequest(s,data,args.index,this.owner);break;
     case 'finish_battle':result=finishBattle(s,data,wallet,args.id,args.result);break;
     case 'rename':{const name=String(args.name).trim();requireRule(name.length>=1&&name.length<=24,'Tên cần từ 1 đến 24 ký tự');s.profile.player_name=name;break;}
     case 'new_game':this.state=newProfile(data,this.now());break;
     case 'import':{const imported=decodeSave(args.text);validateProfile(imported,data);imported.active_battle=null;this.state=copy(imported);break;}
     default:throw new Error('Thao tác không hợp lệ');
    }
    refreshNexus(this.state,data);this.state.revision=latest.revision+1;this.state.saved_at_unix=this.now();this.saves.save_game(this.state);
   }catch(error){this.state=latest;throw error;}
   this.notify();return result;
  });
 }
 export(){return encodeSave(this.state);}
}
