import {readFile,mkdir,writeFile} from 'node:fs/promises';
import path from 'node:path';
const source=await readFile('../afk_clone_spec/02_EXAMPLE_DATA.md','utf8');
let count=0;
for(const match of source.matchAll(/## `res:\/\/data\/([^`]+)`[^\n]*\n[\s\S]*?```json\s*\n([\s\S]*?)\n```/g)){
 const target=path.join('public/data',match[1]);
 const data=JSON.parse(match[2]);
 await mkdir(path.dirname(target),{recursive:true});
 await writeFile(target,JSON.stringify(data,null,2)+'\n');count++;
}
if(count!==10)throw new Error(`Expected 10 data files, got ${count}`);
const file='public/data/stages/campaign_stages.json';
const stages=JSON.parse(await readFile(file,'utf8'));
for(const [index,gear_id,rarity]of [[2,'iron_sword','COMMON'],[3,'leather_helm','COMMON'],[4,'hide_chest','COMMON'],[6,'travel_boots','COMMON'],[7,'dawn_blade','RARE']])stages.stages[index-1].gear_reward={gear_id,rarity,stars:0};
await writeFile(file,JSON.stringify(stages,null,2)+'\n');
console.log(`Imported ${count} JSON catalogs; added the five gear rewards recommended in the spec.`);
