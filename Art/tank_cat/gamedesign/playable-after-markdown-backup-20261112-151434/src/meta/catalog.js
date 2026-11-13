import {requireRule} from './economy.js';
const files={balance:'balance.json',heroes:'heroes/heroes.json',gear:'items/gear.json',relics:'items/relics.json',artifacts:'items/artifacts.json',pool:'gacha/summon_pools.json',stages:'stages/campaign_stages.json',bridge:'tank_bridge.json'};
export async function loadCatalog(fetcher=fetch){
 const data={};await Promise.all(Object.entries(files).map(async([key,file])=>{const response=await fetcher(`/data/${file}`);if(!response.ok)throw new Error(`Không tải được data/${file}`);data[key]=await response.json();}));
 return prepareCatalog(data);
}
export function prepareCatalog(raw){
 const data={...raw,heroes:raw.heroes.heroes??raw.heroes,gear:raw.gear.gear??raw.gear,relics:raw.relics.relics??raw.relics,artifacts:raw.artifacts.artifacts??raw.artifacts,stages:raw.stages.stages??raw.stages};
 data.heroById=Object.fromEntries(data.heroes.map(h=>[h.hero_id,h]));data.gearById=Object.fromEntries(data.gear.map(g=>[g.gear_id,g]));data.artifactById=Object.fromEntries(data.artifacts.map(a=>[a.artifact_id,a]));data.relicByHero=Object.fromEntries(data.relics.map(r=>[r.hero_id,r]));
 requireRule(data.heroes.length===12&&Object.keys(data.heroById).length===12,'Cần đúng 12 định nghĩa tướng');
 requireRule(data.stages.length===20&&data.stages.every((s,i)=>s.index===i+1),'Dữ liệu 20 ải không đúng thứ tự');
 for(const id of data.balance.starting_roster)requireRule(data.heroById[id],`Thiếu tướng ${id}`);
 for(const [rarity,ids]of Object.entries(data.pool.hero_ids_by_rarity)){requireRule(ids.length>0,`Pool rỗng ${rarity}`);for(const id of ids)requireRule(data.heroById[id],`Pool tham chiếu tướng lạ ${id}`);}
 for(const stage of data.stages){if(stage.gear_reward)requireRule(data.gearById[stage.gear_reward.gear_id],'Đồ rơi không hợp lệ');if(stage.artifact_reward)requireRule(data.artifactById[stage.artifact_reward],'Cổ vật không hợp lệ');}
 for(const h of data.heroes)for(const key of ['max_hp','atk','def','speed'])requireRule(Number.isFinite(h.base_stats[key])&&h.base_stats[key]>0&&Number.isFinite(h.growth[key]),`Chỉ số ${h.hero_id} không hợp lệ`);
 return data;
}
