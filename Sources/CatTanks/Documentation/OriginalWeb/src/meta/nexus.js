import {BALANCE,CATS} from '../data.js';
import {byId,check} from '../meta/util.js';
export function anchors(state){return [...state.roster].filter(c=>byId(CATS,c.catId)).sort((a,b)=>b.level-a.level||Object.keys(BALANCE.ascension.levelCaps).indexOf(b.tier)-Object.keys(BALANCE.ascension.levelCaps).indexOf(a.tier)||a.uid.localeCompare(b.uid)).slice(0,5);}
export function nexusLevel(state){const a=anchors(state);return a.length===5?Math.min(...a.map(c=>c.level)):0;}
export function getEffectiveLevel(cat,state){if(!state)return cat.level;const n=nexusLevel(state);return state.nexus.slots.includes(cat.uid)&&n>0?Math.min(n,BALANCE.ascension.levelCaps[cat.tier]):cat.level;}
export function nexusSlots(state){return Math.max(BALANCE.nexus.startingSlots,...BALANCE.nexus.slotUnlocks.filter(u=>state.campaign.highestCleared>=u.clear).map(u=>u.slots));}
export function refreshNexus(state){const active=new Set(anchors(state).map(c=>c.uid));state.nexus.slots=Array.from({length:BALANCE.nexus.maxSlots},(_,i)=>{const id=state.nexus.slots[i]??'';return active.has(id)||!state.roster.some(c=>c.uid===id)?'':id;});}
export function placeNexus(state,id,index){check(Number.isInteger(index)&&index>=0&&index<nexusSlots(state),'Ô chưa mở');if(id){check(state.roster.some(c=>c.uid===id),'Không tìm thấy mèo');check(!anchors(state).some(c=>c.uid===id),'Mèo neo không thể vào ô');check(!state.nexus.slots.includes(id),'Mèo đã ở ô khác');}state.nexus.slots[index]=id;}
