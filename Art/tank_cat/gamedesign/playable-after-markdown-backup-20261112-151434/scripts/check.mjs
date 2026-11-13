import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { Battle } from '../src/battle.js';
import { WEAPONS, MONSTERS, SKILLS, TURRETS } from '../src/data.js';
import {WOLF_FRAME_NAMES} from '../src/enemy-animation.js';
for (const file of ['src/boot.js','src/data.js','src/battle.js','src/visuals.js','src/audio.js','src/game.js','src/enemy-animation.js','scripts/server.mjs']) {
 const result=spawnSync(process.execPath,['--check',file],{encoding:'utf8'});assert.equal(result.status,0,result.stderr);
}
assert.equal(WEAPONS.length,40);assert.equal(MONSTERS.length,20);assert.equal(new Set(WEAPONS.map(w=>w.id)).size,40);assert.equal(new Set(MONSTERS.map(m=>m.size)).size,20,'Every monster has a different size');
for(const [index,size,hp,damage]of [[0,154,780,104],[1,264,1800,208],[19,660,90000,680]]){
 const def=MONSTERS[index];assert.deepEqual([def.size,def.hp,def.damage],[size,hp,damage],'Monster size 2x, previous HP 1.5x, damage 0.8x');
 const battle=new Battle();const enemy=battle.spawn(index);assert.deepEqual([enemy.size,enemy.hp,enemy.maxHp,enemy.y-enemy.size*.43],[size,hp,hp,175]);
}
function damageFixture(){const battle=new Battle(()=>{},()=>.5);battle.start();battle.spawnTimer=999;battle.units.forEach(u=>u.timer=999);return battle;}
const melee=damageFixture();melee.spawn(0,400,{attack:0});melee.step(.05);assert.equal(melee.maxHp-melee.hp,104);
const venom=damageFixture();venom.buffs.poison=1;venom.step(.05);assert(Math.abs(venom.maxHp-venom.hp-2.6)<1e-8,'Poison also deals 80% damage');
const lava=damageFixture();lava.spawn(13,400,{attack:0});lava.step(.05);assert.equal(lava.maxHp-lava.hp,480,'Lava melee and fire pulse both deal 80% damage');
const ranged=damageFixture();ranged.spawn(5,1000,{attack:0});ranged.step(.05);assert.equal(ranged.bullets[0].weapon.damage,128);
const manifest=JSON.parse(readFileSync('public/assets/manifest.json','utf8'));for(const name of Object.keys(manifest))assert(existsSync(`public/assets/${name}.png`),name);
for(const name of WOLF_FRAME_NAMES)assert(existsSync(`public/assets/${name}.png`),name);
let seed=42;const random=()=>{seed=(seed*16807)%2147483647;return seed/2147483647;};
const events=[];const b=new Battle(e=>events.push(e),random);b.start();
for(let i=0;i<60*700&&b.state==='playing';i++){
 if(i%60===0){const boss=b.enemies.find(e=>e.def.boss&&!e.dead);if(boss)b.targetId=boss.id;if(b.hp<39000)b.useSkill('heal');if(b.hp<30000)b.useSkill('shield');if(b.wave===5&&b.energy>400)b.useSkill('barrage');}
 b.step(1/60);
 assert(Number.isFinite(b.hp)&&b.hp>=0&&b.hp<=48000);assert(b.energy>=0&&b.energy<=600);
}
console.log(JSON.stringify({outcome:b.state,seconds:Math.round(b.time),kills:b.kills,seen:b.seen.size,weapons:b.fired.size,hp:Math.round(b.hp),missing:WEAPONS.filter(w=>!b.fired.has(w.id)).map(w=>w.id)},null,2));
assert.equal(b.state,'won','The full level must be completable');assert.equal(b.seen.size,20,'All monsters must spawn');assert.equal(b.fired.size,40,'All weapons must be used');
assert.equal(b.units.filter(u=>u.role==='tower').length,5);assert.equal(b.units.filter(u=>u.role==='infantry').length,10);
const skillBattle=new Battle(()=>{},random);skillBattle.start();skillBattle.hp=20000;
assert(skillBattle.useSkill('heal'));assert.equal(skillBattle.hp,29000);assert.equal(skillBattle.energy,600-SKILLS.heal.cost);assert.equal(skillBattle.useSkill('heal'),false);
skillBattle.buffs.shield=5;const hp=skillBattle.hp;skillBattle.hurtPlayer(1000);assert.equal(hp-skillBattle.hp,60);
skillBattle.spawn(0,800);const target=skillBattle.enemies[0];skillBattle.targetId=target.id;const unit=skillBattle.units[0];assert.equal(skillBattle.chooseTarget(unit).id,target.id);skillBattle.damage(target,999999,'laser');assert.equal(skillBattle.targetId,null);
const a=new Battle(()=>{},random);a.start();const slowEnemy=a.spawn(1,1000);a.units.forEach(u=>u.timer=999);slowEnemy.slow=3;const prev=slowEnemy.x;a.step(.05);assert(prev-slowEnemy.x<MONSTERS[1].speed*.05);
assert(events.some(e=>e.type==='beam'));assert(events.some(e=>e.type==='lightning'));assert(events.some(e=>e.type==='bullet'));
console.log('PASS: syntax, 130 sprites, full six-wave battle, 20 monster types, all 40 weapons, crew, targeting, skills, shield, slow, beams and lightning.');
