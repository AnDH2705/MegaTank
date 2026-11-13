export const clone=value=>structuredClone(value);
export const nowUnix=()=>Math.floor(Date.now()/1000);
export const uid=prefix=>`${prefix}_${crypto.randomUUID()}`;
export function check(ok,message){if(!ok)throw Error(message);}
export const integer=(n,min=0,max=Number.MAX_SAFE_INTEGER)=>Number.isSafeInteger(n)&&n>=min&&n<=max;
export function byId(list,id,key='id'){return list.find(item=>item[key]===id);}
export function indexById(list,key='id'){return new Map(list.map(item=>[item[key],item]));}
export function nextRandom(state){let x=state.rngState>>>0;x^=x<<13;x^=x>>>17;x^=x<<5;state.rngState=x>>>0;return state.rngState/4294967296;}
export function dateKey(now=nowUnix()){const d=new Date(now*1000);return `${d.getFullYear()}-${d.getMonth()+1}-${d.getDate()}`;}
export function weighted(items,rng){const total=items.reduce((sum,item)=>sum+item.weight,0);check(total>0,'Bảng tỷ lệ rỗng');let roll=rng()*total;for(const item of items){roll-=item.weight;if(roll<0)return item;}return items.at(-1);}
