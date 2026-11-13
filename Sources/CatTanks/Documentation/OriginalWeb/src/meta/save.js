import {clone,check,nowUnix} from '../meta/util.js';
export const SAVE_KEY='meow-bastion-save-v1';
export const SCHEMA_VERSION=4;
export function emptyMonsterState(){return {bag:{beastCage:0,moonCage:0,monsterChow:0},instances:[],shards:{},seen:[],caught:[],pity:{beastElite:0,beastLegendary:0,moonElite:0,moonLegendary:0,moonMythic:0},companionSlots:['','',''],lastDailyUnix:0};}
export function emptySkillState(){return {levels:{heal:1,barrage:1},loadout:['heal','barrage','','']};}
export function migrate(input){const s=clone(input);check(s&&typeof s==='object'&&!Array.isArray(s),'Hồ sơ không hợp lệ');check((s.schemaVersion??1)<=SCHEMA_VERSION,'Hồ sơ thuộc phiên bản mới hơn');const defaults=emptyMonsterState();s.monsters={...defaults,...s.monsters,bag:{...defaults.bag,...s.monsters?.bag},pity:{...defaults.pity,...s.monsters?.pity}};s.skills={...emptySkillState(),...s.skills};s.wallet={skillCore:0,catXp:0,arenaToken:0,...s.wallet};s.dailyQuests??={date:'',done:{},claimed:{},claimedBonus:false};s.pvp={rating:1000,tokensToday:0,tokenDay:'',...s.pvp};s.flags={tutorials:{},collectionRewards:{},...s.flags};s.flags.tutorials??={};s.flags.collectionRewards??={};s.schemaVersion=SCHEMA_VERSION;return s;}
export function createSave(storage=globalThis.localStorage){return {recovered:false,broken:false,
 load(){const raw=storage.getItem(SAVE_KEY);if(!raw)return null;try{return migrate(JSON.parse(raw));}catch(error){if(error.message.includes('mới hơn'))throw error;this.broken=true;try{storage.setItem(`${SAVE_KEY}-broken-${nowUnix()}`,raw);}catch{}const backup=storage.getItem(`${SAVE_KEY}-backup`);if(backup)try{this.recovered=true;return migrate(JSON.parse(backup));}catch{}return null;}},
 save(state){const payload=JSON.stringify({...state,schemaVersion:SCHEMA_VERSION,savedAtUnix:nowUnix()}),previous=storage.getItem(SAVE_KEY);storage.setItem(SAVE_KEY,payload);if(previous)try{JSON.parse(previous);storage.setItem(`${SAVE_KEY}-backup`,previous);}catch{}},
 clear(){storage.removeItem(SAVE_KEY);storage.removeItem(`${SAVE_KEY}-backup`);}
};}
