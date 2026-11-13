import {BALANCE,HANDHELD} from '../data.js';
import {GEAR_DEFS,gearStats} from '../meta/stats.js';
import {byId,check,uid} from '../meta/util.js';
export function gearDef(id){return byId(GEAR_DEFS,id)??(byId(HANDHELD,id)?{id,name:byId(HANDHELD,id).name,slot:'weapon',asset:id,baseStats:{atk:20}}:null);}
export function grantGear(state,id,rarity='COMMON'){check(gearDef(id),'Đồ không tồn tại');const item={uid:uid('gear'),gearId:id,rarity,stars:0,equippedBy:''};state.inventory.gear.push(item);return item;}
export function equip(cat,item,state){const def=gearDef(item.gearId);check(def,'Đồ chưa được hỗ trợ');for(const c of state.roster)for(const [slot,id]of Object.entries(c.gear))if(id===item.uid)delete c.gear[slot];const old=state.inventory.gear.find(g=>g.uid===cat.gear[def.slot]);if(old)old.equippedBy='';cat.gear[def.slot]=item.uid;item.equippedBy=cat.uid;}
export function unequip(cat,slot,state){const old=state.inventory.gear.find(g=>g.uid===cat.gear[slot]);if(old)old.equippedBy='';delete cat.gear[slot];}
export function enhanceCost(item){const b=BALANCE.gear;return {coins:Math.floor(b.enhance.goldBase*(item.stars+1)*b.rarityMult[item.rarity]),forgeStone:b.enhance.forgeBase+b.enhance.forgeStep*item.stars};}
export function enhance(item,wallet){check(item.stars<BALANCE.gear.maxStars,'Đã tối đa 5 sao');check(wallet.trySpend(enhanceCost(item)),'Thiếu xu/đá rèn');item.stars++;}
export function upgradeRelic(cat,wallet){check(cat.relicLevel<10,'Bảo vật đã tối đa');check(wallet.trySpend({relicStone:Math.max(10,10*cat.relicLevel)}),'Thiếu đá bảo vật');cat.relicLevel++;}
export function equipArtifact(cat,id,state){const old=state.inventory.artifacts.find(a=>a.uid===cat.artifactUid);if(old)old.equippedBy='';if(id){const art=state.inventory.artifacts.find(a=>a.uid===id);check(art,'Không có cổ vật');for(const c of state.roster)if(c.artifactUid===id)c.artifactUid='';art.equippedBy=cat.uid;}cat.artifactUid=id;}
export function quickEquip(cat,state){for(const def of GEAR_DEFS){const available=state.inventory.gear.filter(g=>!g.equippedBy&&gearDef(g.gearId)?.slot===def.slot);available.sort((a,b)=>Object.values(gearStats(b,gearDef(b.gearId))).reduce((s,n)=>s+n,0)-Object.values(gearStats(a,gearDef(a.gearId))).reduce((s,n)=>s+n,0));if(available[0])equip(cat,available[0],state);}}
