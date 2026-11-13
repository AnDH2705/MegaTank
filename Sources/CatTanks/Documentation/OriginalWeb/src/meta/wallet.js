import {BALANCE} from '../data.js';
import {check,integer} from '../meta/util.js';
export const CURRENCY_KEYS=['coins','spiritDust','soulEssence','forgeStone','gem','summonScroll','relicStone','skillCore','catXp','arenaToken'];
export const WALLET_CAP=BALANCE.currencyCap;
export class Wallet{
 constructor(amounts={}){this.amounts=Object.fromEntries(CURRENCY_KEYS.map(k=>[k,0]));this.fromJSON(amounts);}
 get(id){return this.amounts[id]??0;}
 add(id,amount){check(CURRENCY_KEYS.includes(id)&&integer(amount),'Tài nguyên không hợp lệ');this.amounts[id]=Math.min(WALLET_CAP,this.get(id)+amount);}
 canAfford(cost={}){return Object.entries(cost).every(([key,n])=>CURRENCY_KEYS.includes(key)&&integer(n)&&this.get(key)>=n);}
 trySpend(cost={}){if(!this.canAfford(cost))return false;for(const [key,n]of Object.entries(cost))this.amounts[key]-=n;return true;}
 toJSON(){return {...this.amounts};}
 fromJSON(data={}){for(const key of CURRENCY_KEYS)if(data[key]!=null){check(integer(data[key],0,WALLET_CAP),`Số dư ${key} bị lỗi`);this.amounts[key]=data[key];}}
}
