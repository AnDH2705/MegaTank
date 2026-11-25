// Rough CatTanks battle sim (approximation of Battle.cs, not frame-exact).
const fs = require("fs");
const D = JSON.parse(fs.readFileSync(process.argv[2], "utf8"));
const M = Object.fromEntries(D.monsters.map(m => [m.asset, m]));
const W = Object.fromEntries(D.weapons.map(w => [w.id, w]));
const C = Object.fromEntries(D.cats.map(c => [c.id, c]));
const tierMul = D.balance.ascension.tierMultipliers;
const opts = JSON.parse(process.argv[3] || "{}");
const SK = Object.fromEntries(D.skills.map(s => [s.id, s]));

function catStats(id, level, tier, role) {
  const d = C[id], t = tierMul[tier], s = {};
  for (const k in d.baseStats) s[k] = (d.baseStats[k] + d.growth[k] * (level - 1)) * t;
  if (d.specialty === role) { s.atk *= 1.15; s.maxHp *= 1.15; }
  for (const k in s) s[k] = Math.floor(s[k]);
  return s;
}
function buildTeam(p) {
  const units = [];
  for (let i = 0; i < p.floors; i++) {
    const cat = p.operators[i]; const w = { ...W["turret-" + i] };
    let dmg = w.damage * (1 + .08 * (p.turretLevel - 1));
    if (!cat) { w.damage = dmg * .7; units.push({ role: "tower", w, hp: 240, maxHp: 240, def: 0 }); continue; }
    const st = catStats(cat.id, cat.level, cat.tier, "");
    w.damage = (dmg * (1 + st.atk / 50 * .5) + st.atk * .15) * (C[cat.id].specialty === "operator" ? 1.15 : 1);
    w.rate = Math.max(.15, w.rate / (1 + Math.min(.3, st.speed / 100 * .25)));
    const so = catStats(cat.id, cat.level, cat.tier, "operator");
    units.push({ role: "tower", w, hp: so.maxHp, maxHp: so.maxHp, def: st.def });
  }
  for (let i = 0; i < 5; i++) {
    const cat = p.squad[i]; if (i >= p.squadCount) { units.push({ role: "inf", w: { ...W["gun-0"], damage: W["gun-0"].damage * .75 }, hp: 240, maxHp: 240, def: 0 }); continue; }
    const st = catStats(cat.id, cat.level, cat.tier, "infantry"); const w = { ...W[cat.id.replace("cat-", "gun-")] };
    w.damage *= 1 + st.atk / 50; w.rate = Math.max(.15, w.rate / (1 + st.speed / 500));
    units.push({ role: "inf", w, hp: st.maxHp, maxHp: st.maxHp, def: st.def });
  }
  units.forEach((u, i) => u.timer = .15 + i * .24);
  return units;
}
function run(stageIndex, p, seed = 1) {
  let rnd = seed; const R = () => (rnd = (rnd * 16807) % 2147483647) / 2147483647;
  const stage = D.stages[stageIndex - 1], wv = stage.waves[0];
  const units = buildTeam(p); const maxHp = Math.floor(12000 * Math.pow(1.08, p.hpLevel - 1) + 800 * p.floors);
  let hp = maxHp, energy = 200, t = 0, spawned = 0, enemies = [], cd = { heal: 0, barrage: 0 }, burstIdx = 0, burstCount = 0, spawnTimer = 0, offset = 0, clearT = 1;
  const playerX = 290, dt = .05; let healUses = 0, barrageUses = 0, towerDeaths = 0, bossT = null, bossCapWindow = 0;
  const healAmt = SK.heal.healVehicle, rocketDmg = SK.barrage.rocketDamage, rockets = SK.barrage.rockets;
  const types = wv.types;
  function spawn(sp) { const d = M[sp]; const h = d.hp * (d.boss ? wv.bossHpMult : wv.enemyHpMult); enemies.push({ d, hp: h, maxHp: h, x: 1590 + R() * 100, atk: 1.6 + R() * 2, summon: 9, age: 0 }); }
  function damage(e, amt, kind) {
    if (e.hp <= 0) return; let def = e.d.armor || 0; if (["sniper", "drill", "laser", "poison", "arrow"].includes(kind)) def *= .2;
    const before = e.hp; e.hp -= amt * (1 - def);
    if (e.d.boss && before / e.maxHp >= .1 && e.hp > 0 && e.hp / e.maxHp < .1) e.capStart = t;
    if (e.d.boss && e.hp <= 0 && e.capStart != null) bossCapWindow = t - e.capStart;
    if (e.d.boss && e.hp <= 0) bossT = t;
    if (e.hp <= 0) energy = Math.min(600, energy + 12);
  }
  function hurtSide(amount) {
    const alive = units.filter(u => u.hp > 0 && u.role === "tower");
    if (alive.length) { const u = alive[Math.floor(R() * alive.length)]; u.hp -= amount * 80 / (80 + u.def); if (u.hp <= 0) towerDeaths++; return; }
    hp -= amount;
  }
  while (t < 300) {
    t += dt; energy = Math.min(600, energy + dt * 16); for (const k in cd) cd[k] = Math.max(0, cd[k] - dt);
    // spawning
    if (burstIdx < wv.bursts.length) {
      const b = wv.bursts[burstIdx]; let go = true;
      if (t + offset < b.after) { if (spawned === 0 || enemies.some(e => e.hp > 0)) clearT = 1; else clearT -= dt; if (clearT > 0) go = false; else { offset = b.after - t; spawnTimer = 0; } }
      if (go) { spawnTimer -= dt; if (spawnTimer <= 0) { let sp = types[spawned % types.length]; if (wv.boss && spawned === wv.count - 1) sp = "beast-8"; else if (sp === "beast-8") sp = types.find(x => x !== "beast-8"); spawn(sp); spawned++; burstCount++; spawnTimer = b.spacing; clearT = 1; if (burstCount >= b.count) { burstIdx++; burstCount = 0; } } }
    }
    const live = enemies.filter(e => e.hp > 0);
    // AI skills
    if (opts.skills !== false) {
      const towerLow = units.some(u => u.role === "tower" && u.hp > 0 && u.hp / u.maxHp < .4);
      if ((hp / maxHp < .5 || towerLow) && cd.heal <= 0 && energy >= SK.heal.baseCost) { energy -= SK.heal.baseCost; cd.heal = SK.heal.baseCd; hp = Math.min(maxHp, hp + healAmt); units.forEach(u => { if (u.hp > 0) u.hp = Math.min(u.maxHp, u.hp + u.maxHp * .4); }); healUses++; }
      if (live.length >= 4 && cd.barrage <= 0 && energy >= SK.barrage.baseCost) {
        energy -= SK.barrage.baseCost; cd.barrage = SK.barrage.baseCd; barrageUses++;
        for (let i = 0; i < rockets; i++) { const tg = live[i % live.length]; damage(tg, rocketDmg, "rocket"); for (const o of live) if (o !== tg && Math.abs(o.x - tg.x) < 145 + o.d.size * .2) damage(o, rocketDmg * .6, "rocket"); }
      }
    }
    // player fire
    for (const u of units) {
      if (u.hp <= 0) continue; u.timer -= dt; if (u.timer > 0) continue;
      const cand = enemies.filter(e => e.hp > 0 && e.x < 1720 && e.x - playerX < u.w.range); if (!cand.length) continue;
      cand.sort((a, b) => a.x - b.x); const tg = cand[0]; u.timer = u.w.rate; const w = u.w;
      const n = w.kind === "double" ? 2 : Math.max(1, w.count || 1);
      if (w.kind === "laser" || w.kind === "sniper") { cand.slice(0, w.pierce || 1).forEach(e => damage(e, w.damage, w.kind)); continue; }
      for (let i = 0; i < n; i++) { damage(tg, w.damage, w.kind); if (w.burn) tg.burnT = Math.max(tg.burnT || 0, w.burn); if (w.splash) for (const o of cand) if (o !== tg && Math.abs(o.x - tg.x) < w.splash + o.d.size * .2) damage(o, w.damage * .6, w.kind); }
    }
    // enemies
    for (const e of enemies) {
      if (e.hp <= 0) continue; e.age += dt; e.atk -= dt; e.summon -= dt;
      if (e.burnT > 0) { e.burnT -= dt; damage(e, 24 * dt, "flame"); }
      const d = e.d, ratio = e.hp / e.maxHp; let sp = d.speed; if (d.behavior === "enrage" || d.boss) sp *= 1 + (1 - ratio) * 1.5;
      const stop = playerX + (d.range > 0 ? d.range : 150) + d.size * .25; if (e.x > stop) e.x -= sp * dt;
      if (e.atk <= 0 && e.x < stop + 25) {
        e.atk = d.flying ? 2.4 : 1.8; const dmg = d.damage * wv.enemyAtkMult; const cnt = (d.range > 0 || d.flying) ? (d.behavior === "moth" || d.behavior === "griffin" || d.boss ? 3 : 1) : 1;
        hurtSide(dmg * cnt * (d.range > 0 || d.flying ? .85 : 1));
      }
      if ((d.behavior === "queen" || d.boss) && e.summon <= 0) { spawn("bug-0"); spawn(d.boss && ratio < .35 ? "bug-4" : "bug-0"); e.summon = d.boss && ratio < .35 ? 6 : 11; }
      if (d.behavior === "healer" && e.summon <= 0) { for (const o of enemies) if (o.hp > 0) o.hp = Math.min(o.maxHp, o.hp + 180); e.summon = 3; }
    }
    enemies = enemies.filter(e => e.hp > 0);
    if (hp <= 0) return { win: false, t, healUses, barrageUses, towerDeaths };
    if (spawned >= wv.count && enemies.length === 0) return { win: true, t, hpLeft: hp / maxHp, healUses, barrageUses, towerDeaths, bossT, bossCapWindow };
  }
  return { win: false, t, timeout: true, healUses, barrageUses, towerDeaths };
}
module.exports = { run, buildTeam, D };
if (require.main === module) {
  const L = opts.level || 1;
  const p = { floors: opts.floors || 1, turretLevel: opts.turret || L, hpLevel: opts.hp || 1, squadCount: opts.squadCount || 2,
    operators: [{ id: "cat-0", level: L, tier: "VETERAN" }], squad: [{ id: "cat-1", level: L, tier: "RECRUIT" }, { id: "cat-2", level: L, tier: "VETERAN" }] };
  for (let s = opts.from || 1; s <= (opts.to || 20); s++) {
    let wins = 0, agg = { t: 0, heal: 0, bar: 0, td: 0, hpl: 0, cap: 0 };
    for (let k = 1; k <= 10; k++) { const r = run(s, p, k * 7919); if (r.win) { wins++; agg.hpl += r.hpLeft; agg.cap += r.bossCapWindow || 0; } agg.t += r.t; agg.heal += r.healUses; agg.bar += r.barrageUses; agg.td += r.towerDeaths; }
    console.log(`stage ${s}: win ${wins}/10  avgTime ${(agg.t / 10).toFixed(0)}s  heal ${(agg.heal / 10).toFixed(1)}  barrage ${(agg.bar / 10).toFixed(1)}  towerDeaths ${(agg.td / 10).toFixed(1)}  hpLeft ${wins ? (agg.hpl / wins * 100).toFixed(0) : "-"}%` + (D.stages[s - 1].boss ? `  bossCaptureWindow ${wins ? (agg.cap / wins).toFixed(2) : "-"}s` : ""));
  }
}
