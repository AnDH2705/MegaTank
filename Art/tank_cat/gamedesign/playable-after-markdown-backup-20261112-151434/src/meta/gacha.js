import {requireRule} from './economy.js';
import {RARITIES,addFromSummon,refreshNexus} from './heroes.js';
export function nextRandom(state){let x=state.rng_state>>>0;x^=x<<13;x^=x>>>17;x^=x<<5;state.rng_state=x>>>0;return state.rng_state/4294967296;}
export function summonOne(gacha,data,floor=0){
 const elite=gacha.pity_elite+1>=data.balance.gacha.pity_elite,legendary=gacha.pity_legendary+1>=data.balance.gacha.pity_legendary;
 floor=Math.max(floor,legendary?3:elite?2:0);
 const rates=data.pool.rates.filter(r=>RARITIES.indexOf(r.rarity)>=floor);let roll=nextRandom(gacha)*rates.reduce((sum,r)=>sum+r.weight,0),rarity=rates.at(-1).rarity;
 for(const rate of rates){roll-=rate.weight;if(roll<0){rarity=rate.rarity;break;}}
 const ids=data.pool.hero_ids_by_rarity[rarity],hero_id=ids[Math.floor(nextRandom(gacha)*ids.length)],rank=RARITIES.indexOf(rarity);
 gacha.pity_elite=rank>=2?0:gacha.pity_elite+1;gacha.pity_legendary=rank>=3?0:gacha.pity_legendary+1;
 return {hero_id,rarity,pity_triggered:legendary?'legendary':elite?'elite':''};
}
export function summon(state,data,wallet,count){requireRule(count===1||count===10,'Chỉ triệu hồi 1 hoặc 10 lần');requireRule(wallet.try_spend({summon_scroll:count}),'Không đủ vé triệu hồi');const results=[];for(let i=0;i<count;i++)results.push(summonOne(state.gacha,data,count===10&&i===9&&!results.some(r=>RARITIES.indexOf(r.rarity)>=2)?2:0));for(const result of results)Object.assign(result,addFromSummon(state,result.hero_id,data));refreshNexus(state,data);return results;}
