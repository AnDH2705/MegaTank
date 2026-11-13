import {trackAction} from '../meta/economy.js';
import {ensureFortress} from '../meta/fortress.js';
import {refreshNexus} from '../meta/nexus.js';
import {IdleSystem} from '../meta/idle.js';
import {ensureStarters} from '../meta/cats.js';
import {dispatch} from '../meta/actions.js';
import {BALANCE} from '../data.js';
import {Wallet} from '../meta/wallet.js';
import {createSave,emptyMonsterState,emptySkillState} from '../meta/save.js';
import {clone,uid,nowUnix,check} from '../meta/util.js';
export function newProfile(now=nowUnix()){return {schemaVersion:4,revision:0,profile:{name:'Chỉ huy mèo',createdAtUnix:now},wallet:clone(BALANCE.startingWallet),roster:[],inventory:{gear:[],artifacts:[]},nexus:{slots:[]},account:{level:1,xp:0},fortress:{hpLevel:1,floors:[],turrets:[]},squadSlots:['','','','',''],skills:emptySkillState(),presets:[null,null,null],campaign:{highestCleared:0,selected:1,clears:{}},idle:{lastAccrualUnix:now},gacha:{elite:0,legendary:0},pvp:{rating:BALANCE.pvp.ratingStart,tokensToday:0,tokenDay:'',defense:null},classicCleared:false,monsters:{...emptyMonsterState(),bag:clone(BALANCE.monsters.startingBag)},dailyQuests:{date:'',done:{},claimed:{},claimedBonus:false},shop:{date:'',bought:{}},flags:{tutorials:{},collectionRewards:{}},rngState:crypto.getRandomValues(new Uint32Array(1))[0]||1,activeBattle:null};}
export class GameState{
 constructor(storage=globalThis.localStorage,options={}){this.save=createSave(storage);this.now=options.now??nowUnix;this.locks=options.locks??globalThis.navigator?.locks;this.state={...newProfile(this.now()),...(this.save.load()??{})};ensureStarters(this.state);ensureFortress(this.state);refreshNexus(this.state);this.state.wallet=new Wallet(this.state.wallet).toJSON();this.save.save(this.state);this.listeners=new Set();this.queue=Promise.resolve();this.timer=null;}
 peekIdle(){const idle=new IdleSystem(BALANCE,()=>this.state.campaign.highestCleared);idle.lastAccrualUnix=this.state.idle.lastAccrualUnix;return idle.peek(this.now());}
 subscribe(fn){this.listeners.add(fn);return ()=>this.listeners.delete(fn);}
 notify(){for(const fn of this.listeners)try{fn(this.state);}catch(error){console.error(error);}}
 markDirty(){clearTimeout(this.timer);this.timer=setTimeout(()=>this.flush(),400);}
 flush(){clearTimeout(this.timer);const latest=this.save.load();if(latest&&(latest.revision??0)>(this.state.revision??0)){this.state=latest;this.notify();return;}this.save.save(this.state);}
 checkpointBattle(id,progress,abandoned=false){const latest=this.save.load();if(!latest?.activeBattle||latest.activeBattle.id!==id)return;latest.activeBattle.progress=clone(progress);latest.activeBattle.spent=clone(progress.captureUsed??{});latest.activeBattle.abandoned=abandoned;latest.revision=(latest.revision??0)+1;this.save.save(latest);this.state=latest;}
 sync(){const state=this.save.load();if(state&&state.revision!==this.state.revision){this.state=state;this.notify();}}
 async act(name,args={}){const execute=()=>{const old=this.save.load()??this.state;this.state=clone(old);const wallet=new Wallet(this.state.wallet);try{let result;switch(name){case 'claim':{const idle=new IdleSystem(BALANCE,()=>this.state.campaign.highestCleared);idle.lastAccrualUnix=this.state.idle.lastAccrualUnix;result=idle.claim(wallet,this.state.monsters.bag,this.now());this.state.idle.lastAccrualUnix=idle.lastAccrualUnix;break;}case 'rename':check(typeof args.name==='string'&&args.name.trim().length>0&&args.name.length<=24,'Tên cần 1–24 ký tự');this.state.profile.name=args.name.trim();break;case 'new':this.state=newProfile(this.now());ensureStarters(this.state);ensureFortress(this.state);break;default:result=dispatch(name,args,this.state,wallet,this.now());}trackAction(name,args,result,this.state,this.now());this.state.wallet=wallet.toJSON();if(name==='new')this.state.wallet=clone(BALANCE.startingWallet);refreshNexus(this.state);this.state.revision=(old.revision??0)+1;this.save.save(this.state);this.notify();return result;}catch(error){this.state=old;throw error;}};if(this.locks)return this.locks.request('meow-bastion-v37',execute);const task=this.queue.then(execute);this.queue=task.catch(()=>{});return task;}
}
