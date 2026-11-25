// Combat lab: per-weapon, per-cat, per-monster, per-skill and per-stage tests on the battle.js port.
// Usage: node lab.js [weapons|cats|monsters|skills|stages|capture|all] [catalogPath]
"use strict";
const { Battle, loadCatalog, catStats, skillAI } = require("./battle.js");
const mode = process.argv[2] || "all"; let D = loadCatalog(process.argv[3]);
const PROP = process.env.PROPOSAL ? require("./proposal.js") : null; if (PROP) D = PROP.patch(D);
// LIVE=1: catalog.json already holds the v1 data; only switch on the code rules now in Battle.cs.
const LIVE = process.env.LIVE ? { FLAGS: Object.assign({}, require("./proposal.js").FLAGS, { fortressTakesHits: true, revive: 0, specialty: D.balance.cats.specialtyBonus, healPct: D.S.heal.healPct, dotScale: D.balance.combat.dotScale, antiAir: D.balance.combat.antiAir, captureDaze: D.balance.monsters.capture.dazeSeconds }), START_ENERGY: Math.round(600 * D.balance.loadoutEnergy.startFrac) } : null;
const mk = (L, o) => new Battle(D, L, Object.assign({ flags: (LIVE || PROP) ? (LIVE || PROP).FLAGS : {}, energy: (LIVE || PROP) ? (LIVE || PROP).START_ENERGY : 200, skillLevel: L.skillLevel }, o));
console.log(LIVE ? "== LIVE catalog.json + v1 code rules" : PROP ? "== PROPOSAL v1 " + JSON.stringify(PROP.P) : "== CURRENT DATA");
const pad = (s, n) => String(s).padEnd(n), lp = (s, n) => String(s).padStart(n), f0 = x => Math.round(x).toString();

function keepAlive(list, n, spacing = .35, hpMult = .36, extra = {}) {
  let t = 0, i = 0;
  return Object.assign({ hpMult, atkMult: .47, bossHpMult: extra.bossHpMult ?? 2, update(b, dt) { t -= dt; if (t <= 0 && b.enemies.filter(e => !e.dead).length < n) { b.spawn(list[i++ % list.length]); t = spacing; } } }, extra);
}
const SCEN = {
  swarm: () => keepAlive(["bug-0", "bug-0", "bug-4", "beast-0"], 9, .3),
  armor: () => keepAlive(["bug-1", "beast-6", "beast-5"], 3, .8),
  ranged: () => keepAlive(["bug-2", "bug-6", "bug-7", "beast-7"], 4, .8),
  flyers: () => keepAlive(["bug-4", "bug-7", "beast-7"], 5, .5),
  crystal: () => keepAlive(["bug-8"], 3, .8),
  boss: () => keepAlive(["beast-8"], 1, 1),
  mixed: () => keepAlive(["bug-0", "beast-0", "bug-4", "bug-1", "beast-1", "bug-2", "bug-3", "beast-2", "bug-5", "beast-4", "bug-6", "beast-3"], 7, .5),
};
const REF_CAT = { id: "cat-0", level: 20, tier: "VETERAN" };
function soloLoadout(kind, id) {
  if (kind === "turret") return { floors: [{ turret: id, turretLevel: 20, cat: REF_CAT }], squad: [], squadCount: 0, noMilitia: true };
  if (kind === "gun") return { floors: [], squad: [{ id: "cat-9", level: 20, tier: "RECRUIT", gun: id }], squadCount: 1, noMilitia: true };
  return { floors: [{ turret: "turret-0", cat: null, modules: [id] }], squad: [], squadCount: 0, noMilitia: true, dropTowers: true };
}
function solo(kind, id, scen, seed = 3) {
  const L = soloLoadout(kind, id); const b = mk(L, { scenario: SCEN[scen](), god: true, seed });
  if (L.dropTowers) b.units = b.units.filter(u => u.role !== "tower");
  b.run(60); const src = b.units[0].src; const dealt = b.stats.dmgBySrc[src] || 0; let burn = 0;
  for (const k in b.stats.dmgBySrc) if (k.startsWith(src + "~")) burn += b.stats.dmgBySrc[k];
  const waste = b.stats.overkill[src] || 0;
  return { dps: (dealt + burn) / 60, dot: burn / 60, waste: waste / Math.max(1, dealt + waste), kills: b.kills };
}
function weapons() {
  const scen = ["swarm", "armor", "ranged", "flyers", "crystal", "boss", "mixed"];
  for (const [kind, prefix] of [["turret", "turret-"], ["gun", "gun-"], ["module", "pack-"]]) {
    console.log(`\n### ${kind.toUpperCase()} — useful DPS over 60s, god mode, stage-10 enemy HP (${kind === "turret" ? "operator Mướp lv20 VETERAN, turret lv20" : kind === "gun" ? "infantry Mây lv20" : "module raw, no scaling"})`);
    console.log(pad("id", 9) + pad("kind", 9) + scen.map(s => lp(s, 8)).join("") + lp("waste%", 8) + lp("dot%", 6));
    for (const w of D.weapons.filter(w => w.id.startsWith(prefix))) {
      if (w.kind === "heal" || w.kind === "shield") { console.log(pad(w.id, 9) + pad(w.kind, 9) + "  (support: " + (w.kind === "heal" ? `+${w.damage} HP / ${w.rate}s ≈ ${(w.damage / w.rate).toFixed(0)} HP/s, +15 energy` : `shield 2s every ${w.rate}s = ${(200 / w.rate).toFixed(0)}% uptime, −94% damage`) + ")"); continue; }
      const r = scen.map(s => solo(kind, w.id, s)); const m = r[6];
      console.log(pad(w.id, 9) + pad(w.kind, 9) + r.map(x => lp(f0(x.dps), 8)).join("") + lp((m.waste * 100).toFixed(0), 8) + lp((m.dot / Math.max(1, m.dps) * 100).toFixed(0), 6));
    }
  }
}
function cats() {
  console.log("\n### CATS at lv20, RECRUIT tier — as operator (turret-0) and as infantry (own gun-N), mixed scenario DPS; EHP = maxHp*(80+def)/80");
  console.log(pad("cat", 8) + pad("name", 10) + pad("spec", 10) + pad("rar", 7) + lp("opDPS", 7) + lp("opEHP", 7) + lp("infDPS", 8) + lp("infEHP", 8) + "  gun");
  for (const c of D.cats) {
    const cat = { id: c.id, level: 20, tier: "RECRUIT" };
    const op = mk({ floors: [{ turret: "turret-0", turretLevel: 20, cat }], squad: [], squadCount: 0, noMilitia: true }, { scenario: SCEN.mixed(), god: true, seed: 5 }).run(60);
    const inf = mk({ floors: [], squad: [cat], squadCount: 1, noMilitia: true }, { scenario: SCEN.mixed(), god: true, seed: 5 }).run(60);
    const so = catStats(D, cat, "operator"), sd = catStats(D, cat, ""), si = catStats(D, cat, "infantry");
    const sum = b => Object.entries(b.stats.dmgBySrc).reduce((a, [k, v]) => a + v, 0) / 60;
    console.log(pad(c.id, 8) + pad(c.name, 10) + pad(c.specialty, 10) + pad(c.rarity, 7) + lp(f0(sum(op)), 7) + lp(f0(so.maxHp * (80 + sd.def) / 80), 7) + lp(f0(sum(inf)), 8) + lp(f0(si.maxHp * (80 + si.def) / 80), 8) + "  " + c.id.replace("cat-", "gun-") + " " + D.W[c.id.replace("cat-", "gun-")].kind);
  }
}
function team(L, floors, squadN) {
  const ops = [{ id: "cat-0", level: L, tier: "VETERAN" }, { id: "cat-3", level: L, tier: "RECRUIT" }, { id: "cat-8", level: L, tier: "RECRUIT" }, { id: "cat-6", level: L, tier: "RECRUIT" }, { id: "cat-7", level: L, tier: "RECRUIT" }];
  const turrets = ["turret-0", "turret-6", "turret-4", "turret-2", "turret-7"];
  const sq = [{ id: "cat-1", level: L, tier: "RECRUIT" }, { id: "cat-2", level: L, tier: "VETERAN" }, { id: "cat-4", level: L, tier: "VETERAN" }, { id: "cat-5", level: L, tier: "RECRUIT" }, { id: "cat-9", level: L, tier: "RECRUIT" }];
  return { floors: Array.from({ length: floors }, (_, i) => ({ turret: turrets[i], turretLevel: L, cat: ops[i], modules: [] })), squad: sq, squadCount: squadN, skills: ["heal", "barrage"], hpLevel: Math.max(1, Math.round(L / 3)) };
}
function monsters() {
  console.log("\n### MONSTERS — 8 of one species at stage-10 multipliers vs expected stage-10 team (lv15, 2 floors, 5 squad), god mode");
  console.log(pad("id", 9) + pad("name", 20) + pad("behav", 9) + lp("HP", 7) + lp("TTK s", 7) + lp("closest", 8) + lp("dmgOut", 8) + lp("dmg/HP", 8));
  const rows = [];
  for (const m of D.monsters) {
    let cnt = 0; const scen = { hpMult: .36, atkMult: .47, bossHpMult: .05, update(b) { if (cnt < (m.boss ? 1 : 8) && b.time > cnt * .9) { b.spawn(m.asset); cnt++; } } };
    const b = mk(team(15, 2, 5), { scenario: scen, god: true, seed: 9 }); b.skills = [];
    while (b.time < 120 && (cnt < (m.boss ? 1 : 8) || b.enemies.some(e => !e.dead && e.asset === m.asset))) b.step(1 / 30);
    const ttk = (b.stats.ttk || {})[m.asset] || [0]; const avg = ttk.reduce((a, x) => a + x, 0) / ttk.length;
    const out = (b.stats.takenBy || {})[m.asset] || 0; const hp = m.hp * (m.boss ? .05 : .36);
    rows.push([m, hp, avg, b.stats.minX[m.asset], out / (m.boss ? 1 : 8)]);
  }
  for (const [m, hp, avg, mx, out] of rows) console.log(pad(m.asset, 9) + pad(m.name, 20) + pad(m.behavior, 9) + lp(f0(hp), 7) + lp(avg.toFixed(1), 7) + lp(f0(mx), 8) + lp(f0(out), 8) + lp((out / hp).toFixed(2), 8));
}
function expected(s, scale = 1) {
  const L = Math.max(1, Math.round((1 + 1.5 * (s - 1)) * scale));
  const floors = s < 8 ? 1 : s < 14 ? 2 : 3, squadN = s < 4 ? 2 : s < 6 ? 3 : s < 9 ? 4 : 5;
  const t = team(L, floors, squadN); t.skillLevel = Math.max(1, Math.round(s / 3 * scale));
  t.skills = ["heal", "barrage"].concat(s >= 2 ? ["shield"] : []).concat(s >= 5 ? ["freeze"] : []).slice(0, 4); return t;
}
function runStage(s, L, opt = {}) {
  const base = opt.noSkills ? null : skillAI(opt);
  const ai = (b, dt) => { if (base) base(b, dt); if (opt.lock && !b.enemies.some(e => e.id === b.targetId && !e.dead)) { const c = b.enemies.filter(e => !e.dead && e.position.x < 1720); const t = c.find(e => e.def.boss) || c[0]; if (t) { b.targetId = t.id; (b.locked = b.locked || new Set()).add(t.id); } } };
  const b = mk(L, Object.assign({ stage: s, ai }, opt)); return b.run(240);
}
function stages() {
  console.log("\n### STAGE CURVE — 10 seeds each; exp = expected progression, low = 60% of it; skill AI on");
  for (const dodge of [false, true]) {
    console.log(dodge ? "\n-- with dodge AI (move fortress when enemy shots incoming)" : "-- no movement");
    for (let s = 1; s <= 20; s++) {
      const cells = [];
      for (const [lab, sc] of [["exp", 1], ["low", .6]]) {
        let w = 0, hp = 0, t = 0, deaths = 0, heal = 0, dodged = 0, hit = 0; const L = expected(s, sc);
        for (let k = 1; k <= 10; k++) { const b = runStage(s, L, { seed: k * 7919, dodge }); if (b.state === "Won") { w++; hp += b.hp / b.maxHp; } t += b.time; deaths += b.stats.unitDeaths; heal += b.stats.skillUses.heal || 0; dodged += b.stats.dodged; hit += b.stats.hitsTaken; }
        cells.push(`${lab} L${lp(L.floors[0].cat.level, 2)} win ${lp(w, 2)}/10 hp ${w ? lp(f0(hp / w * 100), 3) : "  -"}% ${lp(f0(t / 10), 3)}s deaths ${(deaths / 10).toFixed(1)} heal ${(heal / 10).toFixed(1)}` + (dodge && lab === "exp" ? ` dodge ${f0(dodged / Math.max(1, dodged + hit) * 100)}%` : ""));
      }
      console.log(`st${lp(s, 2)}${D.stages[s - 1].boss ? "B" : " "} ` + cells.join(" | "));
    }
  }
}
function skills() {
  console.log("\n### SKILLS — stage 12, expected team, only one skill equipped (skill lv4); 10 seeds");
  const base = expected(12);
  for (const id of ["none", "heal", "barrage", "shield", "rally", "freeze", "drone", "orbital", "decoy", "kamikaze"]) {
    let w = 0, hp = 0, t = 0, uses = 0, dealt = 0;
    for (let k = 1; k <= 10; k++) {
      const L = Object.assign({}, base, { skills: id === "none" ? [] : [id], skillLevel: 4 });
      const b = mk(L, { stage: 12, seed: k * 31, ai: (bb) => { const live = bb.enemies.filter(e => !e.dead); if (id === "heal") { if (bb.hp / bb.maxHp < .6 || bb.units.some(u => !u.dead && u.role === "tower" && u.hp / u.maxHp < .45)) bb.useSkill(id); } else if (id === "decoy") { if (live.some(e => e.position.x - bb.playerX < 500)) bb.useSkill(id); } else if (live.length >= 3) bb.useSkill(id); } }).run(240);
      if (b.state === "Won") { w++; hp += b.hp / b.maxHp; } t += b.time; uses += b.stats.skillUses[id] || 0; dealt += b.stats.dmgBySrc["skill:" + id] || 0;
    }
    const s = D.S[id]; console.log(pad(id, 10) + `win ${lp(w, 2)}/10  hpLeft ${w ? lp(f0(hp / w * 100), 3) : "  -"}%  time ${lp(f0(t / 10), 3)}s  uses ${(uses / 10).toFixed(1)}  skillDmg/heal ${lp(f0(dealt / 10), 7)}` + (s ? `  cost ${s.baseCost} cd ${s.baseCd}` : ""));
  }
}
function capture() {
  console.log("\n### CAPTURE WINDOWS — time the LOCKED target spends at capturable HP (≤25%, boss <10%) before dying; expected team, 5 seeds");
  for (const s of [1, 3, 5, 8, 10, 15, 20]) {
    const all = [];
    for (let k = 1; k <= 5; k++) all.push(...(runStage(s, expected(s), { seed: k * 13, lock: true }).stats.capWindows || []));
    const n = all.filter(x => !x.boss && x.locked), b = all.filter(x => x.boss);
    const zero = n.filter(x => x.window < .05).length / Math.max(1, n.length), avg = n.reduce((a, x) => a + x.window, 0) / Math.max(1, n.length);
    const p1 = n.filter(x => x.window >= 1).length / Math.max(1, n.length);
    console.log(`st${lp(s, 2)}  normal: skipped window ${lp(f0(zero * 100), 3)}%  avg ${avg.toFixed(2)}s  ≥1s ${lp(f0(p1 * 100), 3)}%` + (b.length ? `   boss avg window ${(b.reduce((a, x) => a + x.window, 0) / b.length).toFixed(2)}s` : ""));
  }
}
const all = { weapons, cats, monsters, skills, stages, capture };
if (mode === "all") Object.values(all).forEach(f => f()); else all[mode]();
