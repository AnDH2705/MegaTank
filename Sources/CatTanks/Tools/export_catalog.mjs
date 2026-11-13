import * as d from '../Documentation/OriginalWeb/src/data.js';
import {writeFileSync} from 'node:fs';

const catalog = {
  weapons: d.WEAPONS, monsters: d.MONSTERS, cats: d.CATS, stages: d.STAGES,
  skills: Object.values(d.SKILL_DEFS),
  classicSkills: Object.entries(d.SKILLS).map(([id, skill]) => ({id, ...skill})),
  waves: d.WAVES, balance: d.BALANCE, monsterBook: d.MONSTER_BOOK,
};
writeFileSync(new URL('../Assets/CatTanks/Resources/Data/catalog.json', import.meta.url), JSON.stringify(catalog, null, 2));
console.log(`Exported ${d.WEAPONS.length} weapons, ${d.MONSTERS.length} monsters, ${d.CATS.length} cats, ${d.STAGES.length} stages.`);
