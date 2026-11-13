import {BALANCE,SKILL_DEFS} from '../data.js';
import {check} from '../meta/util.js';
export function isUnlocked(id,{highestCleared=0,accountLevel=1}={},defs=SKILL_DEFS){const u=defs[id]?.unlock;if(!u)return false;if(u.start)return true;if(u.stage!=null)return highestCleared>=u.stage;if(u.accountLevel!=null)return accountLevel>=u.accountLevel;return false;}
export const context=state=>({highestCleared:state.campaign.highestCleared,accountLevel:state.account.level});
export const effectMult=level=>1+BALANCE.skills.effectPerLevel*(level-1);
export const upgradedCost=(base,level)=>Math.max(Math.ceil(base*BALANCE.skills.minCostFrac),Math.floor(base*(1-BALANCE.skills.costReducePerLevel*(level-1))));
export const upgradedCd=(base,level)=>Math.max(BALANCE.skills.minCd,base-BALANCE.skills.cdReduce*(level-1));
export const coreCost=level=>level;
export function upgradeSkill(id,state,wallet,defs=SKILL_DEFS){check(isUnlocked(id,context(state),defs),'Kỹ năng chưa mở');const level=state.skills.levels[id]??1;check(level<BALANCE.skills.maxLevel,'Đã tối đa cấp 8');check(wallet.trySpend({skillCore:coreCost(level)}),'Thiếu lõi kỹ năng');state.skills.levels[id]=level+1;return {level:level+1};}
export function toSkillSnapshot(ids,levels,defs=SKILL_DEFS){return ids.filter(id=>id&&defs[id]).slice(0,4).map(id=>{const level=levels[id]??1,definition=defs[id],m=effectMult(level),out={...definition,level,cost:upgradedCost(definition.baseCost,level),cooldown:upgradedCd(definition.baseCd,level),effectMult:m};for(const key of ['healVehicle','healUnitPct','rocketDamage','damage','slow','duration','hp'])if(Number.isFinite(out[key]))out[key]*=m;return out;});}
export function setLoadout(ids,state,defs=SKILL_DEFS){check(Array.isArray(ids)&&ids.length<=4,'Tối đa 4 kỹ năng');const selected=ids.filter(Boolean);check(selected.length>0&&new Set(selected).size===selected.length,'Cần ít nhất 1 kỹ năng và không trùng');check(selected.every(id=>isUnlocked(id,context(state),defs)),'Kỹ năng chưa mở');state.skills.loadout=Array.from({length:4},(_,i)=>ids[i]??'');for(const id of selected)state.skills.levels[id]??=1;return state.skills.loadout;}
