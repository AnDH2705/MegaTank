export const CURRENCIES=['gold','spirit_dust','soul_essence','forge_stone','gem','summon_scroll','relic_stone'];
export const copy=value=>structuredClone(value);
export const nowUnix=()=>Math.floor(Date.now()/1000);
export const makeId=prefix=>`${prefix}_${crypto.randomUUID()}`;
export function requireRule(condition,message){if(!condition)throw new Error(message);}
export function integer(value,min=0,max=Number.MAX_SAFE_INTEGER){return Number.isSafeInteger(value)&&value>=min&&value<=max;}

export class CurrencyWallet{
 constructor(values,balance){this.values=values;this.cap=balance.currencies.cap;}
 get_amount(id){requireRule(CURRENCIES.includes(id),'Tài nguyên không hợp lệ');return this.values[id]??0;}
 can_afford(cost){return Object.entries(cost).every(([key,value])=>CURRENCIES.includes(key)&&integer(value)&&this.get_amount(key)>=value);}
 try_spend(cost){if(!this.can_afford(cost))return false;for(const [key,value]of Object.entries(cost))this.values[key]-=value;return true;}
 add(id,amount){requireRule(CURRENCIES.includes(id)&&integer(amount),'Số tài nguyên không hợp lệ');this.values[id]=Math.min(this.cap,this.get_amount(id)+amount);}
 grant(rewards){for(const key of CURRENCIES)if(rewards[key])this.add(key,rewards[key]);}
}

export class IdleRewardSystem{
 constructor(state,balance){this.state=state;this.config=balance.idle;}
 peek_rewards(now=nowUnix()){
  const c=this.config,s=this.state.campaign.highest_cleared;
  const elapsed=Math.min(c.cap_seconds,Math.max(0,now-this.state.idle.last_accrual_unix));
  const mult=s===0?c.uncleared_multiplier:1;
  const final=value=>Math.floor(Math.floor(value)*mult);
  return {gold:final(c.gold_base_per_min*c.gold_growth**(Math.max(s,1)-1)*elapsed/60),spirit_dust:final(c.dust_base_per_min*c.dust_growth**(Math.max(s,1)-1)*elapsed/60),forge_stone:final((c.forge_base_per_hour+c.forge_per_stage_hour*s)*elapsed/3600),summon_scroll:final((c.scroll_per_12h_base+Math.floor(s/c.scroll_stage_div))*elapsed/c.cap_seconds),elapsed_seconds:elapsed,capped:elapsed===c.cap_seconds,stage_index:s};
 }
 claim(wallet,now=nowUnix()){
  const rewards=this.peek_rewards(now);requireRule(rewards.elapsed_seconds>=this.config.min_display_seconds,'Cần tích ít nhất 1 phút');
  wallet.grant(rewards);this.state.idle.last_accrual_unix=Math.max(now,this.state.idle.last_accrual_unix);return rewards;
 }
}

export const SAVE_KEY='meow-bastion-profile-v1';
export const BACKUP_KEY=`${SAVE_KEY}-backup`;
function checksum(text){let hash=2166136261;for(let i=0;i<text.length;i++){hash^=text.charCodeAt(i);hash=Math.imul(hash,16777619);}return (hash>>>0).toString(16);}
export function encodeSave(state){const payload=JSON.stringify(state);return JSON.stringify({payload,checksum:checksum(payload)});}
export function decodeSave(text){const envelope=JSON.parse(text);requireRule(typeof envelope.payload==='string'&&checksum(envelope.payload)===envelope.checksum,'Dữ liệu lưu không toàn vẹn');return JSON.parse(envelope.payload);}

export class SaveManager{
 constructor(storage,validate){this.storage=storage;this.validate=validate;this.recovered=false;}
 load_game(){
  const main=this.storage.getItem(SAVE_KEY),backup=this.storage.getItem(BACKUP_KEY);
  if(main===null&&backup===null)return null;
  let primaryError;
  if(main!==null)try{const state=decodeSave(main);this.validate(state);return state;}catch(error){primaryError=error;if(error.message.includes('mới hơn'))throw error;}
  if(backup!==null)try{const state=decodeSave(backup);this.validate(state);this.recovered=true;return state;}catch{}
  throw new Error(`Không đọc được bản lưu. ${primaryError?.message??'Bản sao lưu bị hỏng'}. Hãy xuất dữ liệu lỗi hoặc tạo hồ sơ mới.`);
 }
 save_game(state){
  this.validate(state);const encoded=encodeSave(state),previous=this.storage.getItem(SAVE_KEY);
  // localStorage commits one entire string atomically. Never acknowledge a failed write.
  this.storage.setItem(SAVE_KEY,encoded);
  if(previous!==null)try{const old=decodeSave(previous);this.validate(old);this.storage.setItem(BACKUP_KEY,previous);}catch{}
 }
}
