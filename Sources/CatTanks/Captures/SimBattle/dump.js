// Chạy bản port Battle.cs (Tools/BalanceSim/battle.js) tới giữa trận rồi xuất trạng thái để vẽ.
const ROOT = "F:/tank/MegaTank/Sources/CatTanks";
const { Battle, loadCatalog, skillAI } = require(ROOT + "/Tools/BalanceSim/battle.js");
const D = loadCatalog();
const stage = +(process.argv[2] || 4), seed = +(process.argv[3] || 7);
const cat = (id, lv = 12) => ({ id, level: lv, tier: "RECRUIT", gun: D.C[id].gun });
const L = {
  floors: [
    { turret: "turret-0", turretLevel: 10, cat: cat("cat-0"), modules: ["pack-0"] },
    { turret: "turret-1", turretLevel: 10, cat: cat("cat-1"), modules: ["pack-1"] },
    { turret: "turret-2", turretLevel: 10, cat: cat("cat-2"), modules: [] },
  ],
  squad: ["cat-3", "cat-4", "cat-5", "cat-6", "cat-7"].map(id => cat(id)), squadCount: 5,
  skills: ["heal", "barrage", "shield", "freeze"], hpLevel: 4,
};
const b = new Battle(D, L, { stage, seed, energy: 200 });
const ai = skillAI();
let best = null, bestScore = -1;
for (let f = 0; f < 30 * 70 && b.state === "Playing"; f++) {
  b.step(1 / 30);
  if (f % 30 === 0 && b.time > 8) ai(b, 1 / 30);
  const live = b.enemies.filter(e => !e.dead && e.position.x > 560 && e.position.x < 1480); const xs = live.map(e => e.position.x).sort((a, c) => a - c); let gaps = 0; for (let i = 1; i < xs.length; i++) gaps += Math.min(150, xs[i] - xs[i - 1]);
  const bullets = b.bullets.filter(x => !x.dead && x.position.x < 1600).length;
  const score = Math.min(live.length, 6) * 10 + Math.min(bullets, 12) + new Set(live.map(e => e.asset)).size * 6 + gaps / 20;
  if (score > bestScore && b.hp > b.maxHp * .5) { bestScore = score; best = snap(); }
}
function snap() {
  return {
    stage, stageName: D.stages[stage - 1].name, time: b.time, hp: b.hp, maxHp: b.maxHp, energy: b.energy,
    kills: b.kills, spawned: b.spawned, total: b.waves.reduce((s, w) => s + w.count, 0), playerX: b.playerX, targetId: b.targetId,
    skills: b.skills.map(s => ({ id: s.id, name: s.name, cost: s.cost, cd: b.cooldowns[s.id] || 0 })),
    units: b.units.filter(u => !u.dead).map(u => ({ role: u.role === "infantry" ? "inf" : u.role, index: u.index, x: u.position.x, y: u.position.y,
      angle: u.angle || 0, cat: (u.src.split(":")[1]) || null, weapon: u.weapon.asset, flip: !!u.weapon.flip, hp: u.hp, maxHp: u.maxHp, recoil: u.recoil || 0 })),
    enemies: b.enemies.filter(e => !e.dead).map(e => ({ id: e.id, asset: e.asset, size: e.def.size, boss: !!e.def.boss, flying: !!e.def.flying, x: e.position.x, y: e.position.y, hp: e.hp, maxHp: e.maxHp, age: e.age, phase: e.phase, slow: e.slow, burn: e.burn, poison: e.poison })),
    bullets: b.bullets.filter(x => !x.dead).map(x => ({ x: x.position.x, y: x.position.y, px: x.previous.x, py: x.previous.y, angle: x.angle, kind: x.weapon.kind, color: x.weapon.color || "#ffd36b", enemy: x.enemy })),
    buffs: Object.assign({}, b.buffs),
  };
}
console.log(JSON.stringify(best));
console.error("score", bestScore, "t", best.time.toFixed(1), "enemies", best.enemies.length, best.enemies.map(e => e.asset).join(","), "bullets", best.bullets.length);
