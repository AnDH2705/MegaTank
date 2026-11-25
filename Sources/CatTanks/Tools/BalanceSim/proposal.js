// Candidate rebalance "v1": data patch for catalog.json + code-rule flags understood by battle.js.
// Override numbers with env PROPOSAL='{"hpBase":0.3,...}'.
"use strict";
const P = Object.assign({ hpBase: .8, atkBase: 1.6, growth: 1.075, bossHpBase: .05, bossAtk: 1, speed: 2.5, revive: 0, fortressHits: true }, JSON.parse(process.env.PROPOSAL && process.env.PROPOSAL !== "1" ? process.env.PROPOSAL : "{}"));
const RANGE = { // player weapons: long / standard / short
  "turret-0": 1000, "turret-1": 1000, "turret-2": 1150, "turret-3": 1000, "turret-4": 1150, "turret-5": 720, "turret-6": 1150, "turret-7": 1200, "turret-8": 1000, "turret-9": 1050,
  "gun-0": 900, "gun-1": 900, "gun-2": 1000, "gun-3": 1100, "gun-4": 700, "gun-5": 950, "gun-6": 650, "gun-7": 1050, "gun-8": 1000, "gun-9": 950,
};
const ENEMY_RANGE = { "bug-2": 520, "bug-6": 500, "beast-3": 560, "bug-7": 520, "bug-8": 480, "beast-7": 560, "beast-9": 620, "bug-9": 540, "beast-8": 480 };
const WEAPON = {
  "turret-0": { splash: 110 }, "turret-4": { damage: 100 }, "turret-5": { damage: 40 }, "turret-7": { damage: 200, rate: 3.0, pierce: 2 },
  "turret-9": { damage: 85, rate: 2.6, count: 3 },
  "gun-1": { damage: 13 }, "gun-2": { rate: 2.6, splash: 130 }, "gun-4": { damage: 34 }, "gun-6": { damage: 28 }, "gun-8": { damage: 70 }, "gun-9": { damage: 70, rate: 1.3 },
};
const FLAGS = {
  captureHold: true, captureDaze: 4, revive: P.revive, fortressTakesHits: P.fortressHits, dotScale: .35, moduleScale: true, specialty: .3, healPct: .3,
  antiAir: { rapid: 1.5, double: 1.3, arrow: 1.3, drone: 1.5, rocket: 1.2, mortar: .4, bomb: .4, flame: .6, shotgun: .6, claw: .6, shell: .7 },
};
function patch(D) {
  for (const w of D.weapons) {
    if (RANGE[w.id]) w.range = RANGE[w.id]; else if (w.id.startsWith("pack-") && w.range >= 1450) w.range = 1000;
    Object.assign(w, WEAPON[w.id] || {}); if (D.W[w.id]) Object.assign(D.W[w.id], w);
  }
  for (const m of D.monsters) { if (ENEMY_RANGE[m.asset]) m.range = ENEMY_RANGE[m.asset]; m.speed *= P.speed; if (D.M[m.asset]) Object.assign(D.M[m.asset], m); }
  D.stages.forEach((s, i) => s.waves.forEach(w => { const g = Math.pow(P.growth, i); w.enemyHpMult = P.hpBase * g; w.enemyAtkMult = P.atkBase * g; w.bossHpMult = P.bossHpBase * g; }));
  const sk = D.S; Object.assign(sk.barrage, { rockets: 10, rocketDamage: 240 });
  return D;
}
module.exports = { patch, FLAGS, P, START_ENERGY: 120 };
