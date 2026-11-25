// Line-by-line port of the campaign path of Assets/CatTanks/Scripts/Battle.cs (no classic/arena, no visuals).
// Extras for testing: per-source damage accounting, overkill tracking, god mode, custom spawn scenarios, simple skill/dodge AI.
"use strict";
const fs = require("fs"), path = require("path");
const DEFAULT_CATALOG = path.join(__dirname, "../../Assets/CatTanks/Resources/Data/catalog.json");
function loadCatalog(file) {
  const D = JSON.parse(fs.readFileSync(file || DEFAULT_CATALOG, "utf8"));
  D.M = Object.fromEntries(D.monsters.map(m => [m.asset, Object.assign({ armor: 0, range: 0, flying: false, boss: false }, m)]));
  D.W = Object.fromEntries(D.weapons.map(w => [w.id, Object.assign({ rate: 1, damage: 0, range: 1500, speed: 780, splash: 0, slow: 0, burn: 0, poison: 0, stun: 0, count: 1, pierce: 1, chain: 3 }, w)]));
  D.C = Object.fromEntries(D.cats.map(c => [c.id, c]));
  D.S = Object.fromEntries(D.skills.map(s => [s.id, s]));
  return D;
}
const V = (x, y) => ({ x, y }), dist = (a, b) => Math.hypot(a.x - b.x, a.y - b.y);
const clamp = (v, a, b) => Math.max(a, Math.min(b, v)), lerp = (a, b, t) => a + (b - a) * t;
function deltaAngle(a, b) { let d = (b - a) % 360; if (d > 180) d -= 360; if (d < -180) d += 360; return d; }

function catStats(D, cat, role, F = {}) {
  const d = D.C[cat.id], t = D.balance.ascension.tierMultipliers[cat.tier || "RECRUIT"], s = {};
  for (const k in d.baseStats) s[k] = (d.baseStats[k] + d.growth[k] * ((cat.level || 1) - 1)) * t;
  if (d.specialty === role) { s.atk *= 1 + (F.specialty ?? .15); s.maxHp *= 1 + (F.specialty ?? .15); }
  for (const k in s) s[k] = Math.floor(s[k]);
  return s;
}

class Battle {
  // loadout: {floors:[{turret, turretLevel, stars, cat:{id,level,tier}|null, modules:[]}], squad:[cat|null x5], skills:[ids], skillLevel, hpLevel, squadCount}
  constructor(D, loadout, opt = {}) {
    this.D = D; this.opt = opt; const F = this.F = opt.flags || {}; let seed = opt.seed || 1;
    this.rand = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
    this.time = 0; this.energy = opt.energy ?? 200; this.energyRegen = 16; this.playerX = 290; this.move = 0;
    this.enemies = []; this.units = []; this.bullets = []; this.buffs = {}; this.cooldowns = {}; this.nextId = 1; this.targetId = -1;
    this.state = "Playing"; this.kills = 0; this.spawned = 0; this.wave = 0; this.spawnTimer = .8; this.waveGap = 0;
    this.burstCounts = null; this.burstTimeOffset = 0; this.burstClearTimer = 1; this.decoy = null;
    this.stats = { dmgBySrc: {}, overkill: {}, fired: {}, taken: { fortress: 0, units: 0 }, unitDeaths: 0, skillUses: {}, minX: {}, dodged: 0, hitsTaken: 0 };
    if (opt.scenario) { this.waves = [{ count: Infinity, types: [], enemyHpMult: opt.scenario.hpMult ?? 1, enemyAtkMult: opt.scenario.atkMult ?? 1, bossHpMult: opt.scenario.bossHpMult ?? opt.scenario.hpMult ?? 1, bursts: [] }]; }
    else { this.stage = D.stages[opt.stage - 1]; this.waves = this.stage.waves; }
    const L = loadout;
    this.maxHp = Math.floor(12000 * Math.pow(1.08, (L.hpLevel || 1) - 1) + 800 * L.floors.length); this.hp = this.maxHp;
    L.floors.forEach((f, i) => {
      const w = Object.assign({}, D.W[f.turret]); const cat = f.cat;
      const dmg = w.damage * (1 + .08 * ((f.turretLevel || 1) - 1)) * (1 + .14 * (f.stars || 0));
      let u;
      if (!cat) { w.damage = dmg * .7; u = this.addUnit("tower", i, w, 240, f.turret); }
      else {
        const st = catStats(D, cat, "", F);
        w.damage = (dmg * (1 + st.atk / 50 * .5) + st.atk * .15) * (D.C[cat.id].specialty === "operator" ? 1 + (F.specialty ?? .15) : 1);
        w.rate = Math.max(.15, w.rate / (1 + Math.min(.3, st.speed / 100 * .25)));
        u = this.addUnit("tower", i, w, catStats(D, cat, "operator", F).maxHp, f.turret + ":" + cat.id); u.defense = st.def;
      }
      for (const m of f.modules || []) if (m) { const mw = Object.assign({}, D.W[m]); if (F.moduleScale) mw.damage *= (1 + .08 * ((f.turretLevel || 1) - 1)) * (1 + .14 * (f.stars || 0)); this.addUnit("aux", this.units.filter(a => a.role === "aux").length, mw, 300, m); }
    });
    for (let i = 0; i < 5; i++) {
      const c = i < (L.squadCount ?? 5) ? L.squad[i] : null; if (L.noMilitia && !c) continue;
      const st = c ? catStats(D, c, "infantry", F) : null;
      const gun = c ? (c.gun || c.id.replace("cat-", "gun-")) : "gun-0";
      const w = Object.assign({}, D.W[gun]); w.damage *= st ? 1 + st.atk / 50 : .75;
      if (st) w.rate = Math.max(.15, w.rate / (1 + st.speed / 500));
      const u = this.addUnit("infantry", i, w, st ? st.maxHp : 240, c ? gun + ":" + c.id : "militia"); u.defense = st ? st.def : 0;
    }
    this.skills = [];
    for (const id of L.skills || []) {
      const s = Object.assign({}, D.S[id]); const lv = L.skillLevel || 1;
      s.cost = Math.floor(s.baseCost * Math.max(.55, 1 - .06 * (lv - 1))); s.cooldown = Math.max(5, s.baseCd - .25 * (lv - 1));
      s.damage = (s.damage || 0) * (1 + .1 * (lv - 1)); s.healVehicle = (s.healVehicle || 0) * (1 + .1 * (lv - 1)); s.rocketDamage = (s.rocketDamage || 0) * (1 + .1 * (lv - 1));
      this.skills.push(s);
    }
  }
  addUnit(role, index, w, hp, src) {
    const u = { id: this.nextId++, role, index, weapon: w, hp, maxHp: hp, defense: 0, stun: 0, dead: false, visible: true, src, timer: .15 + index * .24, position: V(110 + index * 35, 196) };
    this.units.push(u); return u;
  }
  buff(id) { return this.buffs[id] || 0; }
  spawn(species, x = -1) {
    const d = this.D.M[species], w = this.waves[this.wave];
    const health = d.hp * (d.boss ? w.bossHpMult : w.enemyHpMult);
    const e = { id: this.nextId++, asset: species, def: d, position: V(x < 0 ? 1590 + this.rand() * 100 : x, 175 + d.size * .43 + (d.flying ? 200 : 0)), hp: health, maxHp: health, phase: this.rand() * 6.28, attack: 1.6 + this.rand() * 2, summon: 9, age: 0, slow: 0, burn: 0, poison: 0, stun: 0, flash: 0, dead: false, burnSrc: null, poisonSrc: null };
    this.enemies.push(e); this.spawned++; return e;
  }
  target(u) {
    let best = null, bd = Infinity, bp = -Infinity; const sniper = u.weapon.kind === "sniper";
    for (const e of this.enemies) {
      if (e.dead || e.position.x <= -100 || e.position.x >= 1720) continue;
      const d = dist(e.position, u.position); if (d >= u.weapon.range) continue;
      if (e.id === this.targetId) { if (this.F.captureHold && e.hp / e.maxHp <= (e.def.boss ? .1 : .25)) continue; return e; }
      const p = sniper ? (e.def.boss ? 900 : 0) + (e.def.armor > 0 ? 200 : 0) : 0;
      if (!best || (sniper ? (p > bp || p === bp && e.hp < best.hp) : d < bd)) { best = e; bd = d; bp = p; }
    }
    return best;
  }
  step(dt) {
    if (this.state !== "Playing") return; dt = Math.min(dt, .05); this.time += dt;
    this.energy = Math.min(600, this.energy + dt * this.energyRegen);
    for (const k in this.cooldowns) this.cooldowns[k] = Math.max(0, this.cooldowns[k] - dt);
    for (const k in this.buffs) this.buffs[k] = Math.max(0, this.buffs[k] - dt);
    if (this.buff("poison") > 0) this.hurtPlayer(dt * 52);
    if (this.opt.ai) this.opt.ai(this, dt);
    this.playerX = clamp(this.playerX + this.move * 110 * dt * (this.buff("slow") > 0 ? .42 : 1), 175, 550);
    this.updateWave(dt);
    for (const u of this.units) {
      if (u.dead) { if (this.F.revive && (u.down = (u.down || 0) + dt) >= this.F.revive) { u.dead = false; u.down = 0; u.hp = u.maxHp * .5; } else { u.visible = false; continue; } }
      u.stun = Math.max(0, u.stun - dt);
      if (u.role === "tower") u.position = V(this.playerX + 35, 258 + u.index * 80);
      else if (u.role === "aux") u.position = V(this.playerX - 88 + u.index % 2 * 172, 232 + Math.floor(u.index / 2) * 214);
      else u.position = V(lerp(u.position.x, this.playerX - 180 + u.index % 5 * 88, Math.min(1, dt * 2.8)), 194 + Math.abs(Math.sin(this.time * 9 + u.index * 2)) * 5);
      if (u.stun > 0) continue;
      u.timer -= dt * (this.buff("rally") > 0 ? 1.8 : 1) * (this.buff("frost") > 0 ? .65 : 1);
      const t = this.target(u);
      if (u.timer > 0) continue;
      const w = u.weapon;
      if (w.kind === "heal") { const before = this.hp; this.hp = Math.min(this.maxHp, this.hp + w.damage); this.credit(u.src, this.hp - before, 0, "heal"); this.energy = Math.min(600, this.energy + 15); u.timer = w.rate; continue; }
      if (w.kind === "shield") { this.buffs.shield = Math.max(this.buff("shield"), 2); u.timer = w.rate; continue; }
      if (t) { this.fire(u, t); u.timer = w.rate; }
    }
    if (this.buff("drone") > 0 && (this.droneTimer = (this.droneTimer || 0) - dt) <= 0) {
      const u = { position: V(this.playerX + 190, 650), weapon: this.D.W["gun-9"], role: "drone", src: "skill:drone" }; const t = this.target(u); if (t) this.fire(u, t); this.droneTimer = .3;
    }
    for (const e of this.enemies) this.updateEnemy(e, dt);
    for (const b of this.bullets) this.updateBullet(b, dt);
    if (this.decoy && (this.decoy.hp <= 0 || this.time > this.decoy.age)) this.decoy = null;
    this.enemies = this.enemies.filter(e => !(e.dead && e.age - e.deathTime > .6)); this.bullets = this.bullets.filter(b => !b.dead);
    if (this.hp <= 0) this.state = "Lost";
  }
  credit(src, dealt, wasted, kind) { const s = this.stats; s.dmgBySrc[src] = (s.dmgBySrc[src] || 0) + dealt; s.overkill[src] = (s.overkill[src] || 0) + wasted; }
  updateWave(dt) {
    if (this.opt.scenario) return this.opt.scenario.update(this, dt);
    const w = this.waves[this.wave]; this.spawnTimer -= dt;
    if (!this.burstCounts) this.burstCounts = w.bursts.map(() => 0);
    for (let i = 0; i < w.bursts.length; i++) {
      const b = w.bursts[i]; if (this.burstCounts[i] >= b.count) continue;
      if (this.time + this.burstTimeOffset < b.after) {
        if (this.spawned === 0 || this.enemies.some(e => !e.dead)) this.burstClearTimer = 1; else this.burstClearTimer -= dt;
        if (this.burstClearTimer > 0) break;
        this.burstTimeOffset = b.after - this.time; this.spawnTimer = 0;
      }
      if (this.spawnTimer <= 0) {
        let sp = w.types[this.spawned % w.types.length];
        if (w.boss && this.spawned === w.count - 1) sp = "beast-8"; else if (sp === "beast-8") sp = w.types.find(t => t !== "beast-8");
        this.spawn(sp); this.burstCounts[i]++; this.spawnTimer = b.spacing; this.burstClearTimer = 1;
      }
      break;
    }
    if (this.spawned < w.count || this.enemies.some(e => !e.dead)) return;
    if (this.waveGap <= 0) { this.waveGap = 1; this.energy = Math.min(600, this.energy + 90); }
    this.waveGap -= dt; if (this.waveGap > 0) return;
    if (this.wave === this.waves.length - 1) { this.state = "Won"; return; }
    this.wave++; this.spawned = 0; this.spawnTimer = 0; this.burstCounts = null; this.waveGap = 0; this.burstTimeOffset = 0; this.burstClearTimer = 1;
  }
  updateEnemy(e, dt) {
    e.age += dt; if (e.dead) return;
    e.attack -= dt; e.summon -= dt;
    e.slow = Math.max(0, e.slow - dt); e.burn = Math.max(0, e.burn - dt); e.poison = Math.max(0, e.poison - dt); e.stun = Math.max(0, e.stun - dt);
    if (e.burn > 0) this.damage(e, (e.burnDps || 24) * dt, "flame", e.burnSrc + "~burn"); if (e.poison > 0) this.damage(e, (e.poisonDps || 30) * dt, "poison", e.poisonSrc + "~poison"); if (e.dead) return;
    const d = e.def, ratio = e.hp / e.maxHp;
    e.burrowed = d.behavior === "burrow" && e.age % 7 < 2.4;
    e.charging = (d.behavior === "charge" || d.behavior === "rhino") && e.age % 7 > 5.3;
    const leap = d.behavior === "leap" ? Math.max(0, Math.sin(e.age * 2.3)) * 95 : 0;
    let y = 175 + d.size * .43 + (d.flying ? 190 + Math.sin(e.age * 2 + e.phase) * 58 : leap);
    if (d.behavior === "bee") y = 265 + Math.sin(e.age * 1.4 + e.phase) * 140;
    let speed = d.speed * (e.charging ? 5 : 1) * (this.waves[this.wave].enemySpeedMult || 1);
    if (d.behavior === "tiger" && e.age % 5 > 3.8) speed *= 3.5;
    if (d.behavior === "wolf") for (const o of this.enemies) if (o !== e && !o.dead && dist(o.position, e.position) < 180) { speed *= 1.45; break; }
    if (d.behavior === "enrage" || d.behavior === "boss") speed *= 1 + (1 - ratio) * 1.5;
    if (e.slow > 0 || this.buff("freeze") > 0) speed *= .35; if (e.stun > 0) speed = 0;
    const stop = this.playerX + (d.range > 0 ? d.range : 150) + d.size * .25;
    e.position = V(e.position.x - (e.position.x > stop ? speed * dt : 0), y);
    this.stats.minX[e.asset] = Math.min(this.stats.minX[e.asset] ?? 9999, e.position.x - this.playerX);
    if (e.attack <= 0 && e.position.x < stop + 25 && e.stun <= 0) {
      e.attack = d.flying ? 2.4 : 1.8;
      const dmg = d.damage * this.waves[this.wave].enemyAtkMult;
      if (d.range > 0 || d.flying) {
        const count = d.behavior === "moth" || d.behavior === "griffin" || d.boss ? 3 : 1;
        for (let i = 0; i < count; i++) {
          const a = Math.atan2(300 + this.rand() * 280 - e.position.y, this.playerX - e.position.x) + (i - (count - 1) / 2) * .14;
          const kind = d.behavior === "frost" ? "ice" : ["venom", "queen", "moth"].includes(d.behavior) ? "poison" : d.behavior === "web" ? "web" : d.boss ? "flame" : "enemy";
          this.makeBullet(e.position, a, { kind, damage: dmg, speed: 280, range: 1900, splash: 0, pierce: 1 }, null, true, e.asset);
        }
      } else this.enemyHit(e.position, dmg * (e.charging ? 2 : 1) * (d.behavior === "enrage" ? 2 - ratio : 1), e.asset);
      if (d.behavior === "lava" && e.position.x < this.playerX + 350) this.hurtPlayer(120 * this.waves[this.wave].enemyAtkMult, e.asset);
    }
    if (d.behavior === "healer" && e.summon <= 0) { for (const o of this.enemies) if (!o.dead && dist(o.position, e.position) < 460) o.hp = Math.min(o.maxHp, o.hp + 180); e.summon = 3; }
    if ((d.behavior === "queen" || d.boss) && e.summon <= 0) {
      this.spawn("bug-0", e.position.x - 100); this.spawn(d.boss && ratio < .35 ? "bug-4" : "bug-0", e.position.x + 30); this.spawned -= 2;
      e.summon = d.boss && ratio < .35 ? 6 : 11;
    }
  }
  fire(u, target) {
    const w = u.weapon; this.stats.fired[u.src] = (this.stats.fired[u.src] || 0) + 1;
    if (w.kind === "laser" || w.kind === "sniper") {
      const dir = norm(sub(target.position, u.position));
      const line = this.enemies.filter(e => !e.dead && dot(sub(e.position, u.position), dir) > 0 && Math.abs((e.position.x - u.position.x) * dir.y - (e.position.y - u.position.y) * dir.x) < e.def.size * .38)
        .sort((a, b) => dist(a.position, u.position) - dist(b.position, u.position)).slice(0, w.pierce);
      for (const e of line) this.damage(e, w.damage, w.kind, u.src); return;
    }
    if (w.kind === "tesla") {
      let cur = target, origin = u.position; const hit = new Set();
      for (let i = 0; i < w.chain && cur; i++) {
        this.damage(cur, w.damage * (1 - i * .14), "tesla", u.src); cur.stun = w.stun > 0 ? w.stun : .4; hit.add(cur.id); origin = cur.position;
        cur = this.enemies.filter(e => !e.dead && !hit.has(e.id) && dist(e.position, origin) < 350).sort((a, b) => dist(a.position, origin) - dist(b.position, origin))[0];
      }
      return;
    }
    const count = w.kind === "double" ? 2 : Math.max(1, w.count);
    for (let i = 0; i < count; i++) {
      let a = Math.atan2(target.position.y - u.position.y, target.position.x - u.position.x);
      if (["shotgun", "crystal", "claw"].includes(w.kind)) a += (i - (count - 1) / 2) * .09;
      else if (w.kind === "rocket") a += (i - (count - 1) / 2) * .3;
      else a += (this.rand() - .5) * .035;
      this.makeBullet(add(u.position, V(Math.cos(a) * (u.role === "tower" ? 70 : 40), Math.sin(a) * (u.role === "tower" ? 70 : 40))), a, w, target, false, u.src);
    }
  }
  makeBullet(pos, angle, w, target, enemy, src) {
    this.bullets.push({ id: this.nextId++, position: pos, previous: pos, start: pos, end: target ? target.position : pos, angle, weapon: w, target, enemy, src, pierce: Math.max(1, w.pierce || 1), age: 0, dead: false, hit: new Set(), flight: target ? Math.max(.5, dist(pos, target.position) / w.speed) : 1 });
  }
  updateBullet(b, dt) {
    if (b.dead) return; b.age += dt; b.previous = b.position; const w = b.weapon;
    if ((w.kind === "rocket" || w.kind === "drone") && b.target && !b.target.dead) {
      const desired = Math.atan2(b.target.position.y - b.position.y, b.target.position.x - b.position.x);
      b.angle += clamp(deltaAngle(b.angle * 180 / Math.PI, desired * 180 / Math.PI) * Math.PI / 180, -dt * 3.5, dt * 3.5);
    }
    b.position = add(b.position, V(Math.cos(b.angle) * w.speed * dt, Math.sin(b.angle) * w.speed * dt));
    if (w.kind === "mortar" || w.kind === "bomb") {
      const t = Math.min(1, b.age / b.flight); b.position = V(lerp(b.start.x, b.end.x, t), lerp(b.start.y, b.end.y, t) + Math.sin(t * Math.PI) * 220);
      if (t >= 1) this.impact(b, b.target);
    }
    if (b.enemy) {
      if (b.position.x < this.playerX + 115 && b.position.x > this.playerX - 145 && b.position.y > 175 && b.position.y < 700) {
        this.enemyHit(b.position, w.damage, b.src); if (w.kind === "poison") this.buffs.poison = 4; if (w.kind === "web") this.buffs.slow = 4; if (w.kind === "ice") this.buffs.frost = 4;
        b.dead = true; this.stats.hitsTaken++;
      } else if (b.position.x < this.playerX - 145 && !b.counted) { b.counted = true; this.stats.dodged++; }
    } else if (!b.dead && w.kind !== "mortar" && w.kind !== "bomb") {
      for (const e of this.enemies) {
        if (e.dead || b.hit.has(e.id)) continue;
        const delta = sub(b.position, b.previous), q = clamp(dot(sub(e.position, b.previous), delta) / Math.max(.001, dot(delta, delta)), 0, 1);
        if (dist(e.position, add(b.previous, V(delta.x * q, delta.y * q))) < e.def.size * .36 + 7) { this.impact(b, e); if (b.dead) break; }
      }
    }
    if (b.age > 6 || dist(b.position, b.start) > w.range || b.position.x < -100 || b.position.x > 1850 || b.position.y < 120 || b.position.y > 1200) b.dead = true;
  }
  impact(b, e) {
    const w = b.weapon;
    if (e && !e.dead) {
      b.hit.add(e.id); this.damage(e, w.damage, w.kind, b.src);
      e.slow = Math.max(e.slow, w.slow || 0);
      if (w.burn > e.burn || !e.burnSrc) { if (w.burn) e.burnSrc = b.src; } e.burn = Math.max(e.burn, w.burn || 0);
      if (w.poison) e.poisonSrc = b.src; e.poison = Math.max(e.poison, w.poison || 0);
      if (this.F.dotScale) { if (w.burn) e.burnDps = Math.max(e.burnDps || 0, 24, w.damage * this.F.dotScale); if (w.poison) e.poisonDps = Math.max(e.poisonDps || 0, 30, w.damage * this.F.dotScale); }
    } else if (e && e.dead) this.credit(b.src, 0, w.damage, w.kind);
    if (w.splash > 0) {
      for (const o of this.enemies) {
        if (o.dead || o === e || dist(o.position, b.position) > w.splash + o.def.size * .2) continue;
        this.damage(o, w.damage * .6, w.kind, b.src); o.slow = Math.max(o.slow, w.slow || 0); if (w.poison) { o.poison = Math.max(o.poison, w.poison); o.poisonSrc = b.src; }
        if (w.kind === "gravity") o.position = V(lerp(o.position.x, b.position.x, .22), lerp(o.position.y, b.position.y, .22));
      }
      b.dead = true;
    }
    if (--b.pierce <= 0) b.dead = true;
  }
  damage(e, amount, kind, src) {
    if (e.dead) return; let def = e.def.armor || 0;
    if (this.F.antiAir && e.def.flying) amount *= this.F.antiAir[kind] ?? 1;
    if (["sniper", "drill", "laser", "poison", "arrow"].includes(kind)) def *= .2;
    if (e.def.behavior === "crystal" && ["laser", "plasma", "tesla", "ice", "gravity"].includes(kind)) def = .58;
    if (e.def.behavior === "frost" && kind === "ice") def = .8;
    if (e.burrowed) def = .9; if (e.def.behavior === "tiger" && e.age % 5 > 3.8) def = Math.max(def, .55);
    let dealt = amount * (1 - def);
    if (this.F.captureDaze && e.id === this.targetId && e.capStart != null && this.time - e.capStart < this.F.captureDaze) dealt = Math.min(dealt, Math.max(0, e.hp - e.maxHp * .01));
    const useful = Math.min(dealt, e.hp); e.hp -= dealt;
    this.credit(src || "?", useful, dealt - useful + (amount - dealt), kind);
    if (e.def.boss && e.capStart == null && e.hp > 0 && e.hp / e.maxHp < .1) e.capStart = this.time;
    if (!e.def.boss && e.capStart == null && e.hp > 0 && e.hp / e.maxHp <= .25) e.capStart = this.time;
    if (e.hp > 0) return;
    e.dead = true; e.deathTime = e.age; this.kills++; this.energy = Math.min(600, this.energy + 12);
    (this.stats.capWindows = this.stats.capWindows || []).push({ id: e.id, locked: !!(this.locked && this.locked.has(e.id)), asset: e.asset, boss: e.def.boss, window: e.capStart == null ? 0 : this.time - e.capStart });
    (this.stats.ttk = this.stats.ttk || {})[e.asset] = (this.stats.ttk[e.asset] || []).concat(e.age);
    if (this.targetId === e.id) this.targetId = -1;
  }
  hurtPlayer(amount, src) { if (this.state !== "Playing") return; const a = amount * (this.buff("shield") > 0 ? .06 : 1); this.stats.taken.fortress += a; (this.stats.takenBy = this.stats.takenBy || {})[src] = (this.stats.takenBy[src] || 0) + a; if (!this.opt.god) this.hp = Math.max(0, this.hp - a); }
  enemyHit(position, amount, src) {
    if (this.decoy && dist(position, this.decoy.position) < 420) { this.decoy.hp -= amount; return; }
    const p = V(Math.min(position.x, this.playerX + 80), Math.min(position.y, 650));
    const u = this.units.filter(a => !a.dead && a.visible && a.role !== "aux" && dist(a.position, p) < 140).sort((a, b) => ((a.role === "tower") - (b.role === "tower")) || dist(a.position, position) - dist(b.position, position))[0];
    if (u && this.F.fortressTakesHits) { this.hurtPlayer(amount * 80 / (80 + u.defense), src); if (amount > 100) u.stun = .6; return; }
    if (u) {
      const a = amount * 80 / (80 + u.defense) * (this.buff("shield") > 0 ? .06 : 1); this.stats.taken.units += a; (this.stats.takenBy = this.stats.takenBy || {})[src] = (this.stats.takenBy[src] || 0) + a;
      if (!this.opt.god) { u.hp = Math.max(0, u.hp - a); if (u.hp <= 0 && !u.dead) { u.dead = true; this.stats.unitDeaths++; } }
      if (amount > 100) { u.position = add(u.position, V(u.role === "infantry" ? -78 : -130, 0)); u.stun = .6; }
      return;
    }
    this.hurtPlayer(amount, src);
  }
  useSkill(id) {
    const s = this.skills.find(v => v.id === id); if (this.state !== "Playing" || !s || (this.cooldowns[id] || 0) > 0 || this.energy < s.cost) return false;
    this.energy -= s.cost; this.cooldowns[id] = s.cooldown; if (s.duration > 0) this.buffs[id] = s.duration; this.stats.skillUses[id] = (this.stats.skillUses[id] || 0) + 1;
    const live = this.enemies.filter(e => !e.dead);
    if (id === "heal") { const before = this.hp; this.hp = Math.min(this.maxHp, this.hp + (this.F.healPct ? this.maxHp * this.F.healPct * (1 + .1 * ((this.opt.skillLevel || 1) - 1)) : s.healVehicle)); this.credit("skill:heal", this.hp - before, 0); this.buffs.poison = 0; for (const u of this.units) if (!u.dead) u.hp = Math.min(u.maxHp, u.hp + u.maxHp * .4); }
    if (id === "freeze") for (const e of live) { e.slow = 6; this.damage(e, s.damage, "ice", "skill:freeze"); }
    if (id === "barrage") for (let i = 0; i < s.rockets && live.length; i++) { const w = Object.assign({}, this.D.W["turret-2"], { damage: s.rocketDamage, splash: 145, speed: 650 }); this.makeBullet(V(this.playerX + 80 + i * 40, 950 + i * 18), -Math.PI / 2, w, live[i % live.length], false, "skill:barrage"); }
    if (id === "orbital" || id === "kamikaze") {
      const t = live.find(e => e.id === this.targetId) || live.slice().sort((a, b) => b.hp - a.hp)[0];
      if (t) { if (id === "orbital") this.damage(t, s.damage * (t.def.boss ? s.bossMult : 1), "laser", "skill:orbital"); else for (const e of live) if (dist(e.position, t.position) < s.splash + e.def.size * .2) this.damage(e, s.damage, "bomb", "skill:kamikaze"); }
    }
    if (id === "decoy") this.decoy = { position: V(this.playerX + 150, 220), hp: 800, age: this.time + s.duration };
    return true;
  }
  run(maxTime = 240, dt = 1 / 30) { while (this.state === "Playing" && this.time < maxTime) this.step(dt); if (this.state === "Playing") this.state = "Timeout"; return this; }
}
function sub(a, b) { return V(a.x - b.x, a.y - b.y); } function add(a, b) { return V(a.x + b.x, a.y + b.y); }
function dot(a, b) { return a.x * b.x + a.y * b.y; } function norm(a) { const l = Math.hypot(a.x, a.y) || 1; return V(a.x / l, a.y / l); }

// Simple player AI: heal when hurt, barrage/freeze/orbital when useful, shield vs boss volleys, optional dodge.
function skillAI(opts = {}) {
  return (b, dt) => {
    const live = b.enemies.filter(e => !e.dead); const boss = live.find(e => e.def.boss);
    const towersLow = b.units.some(u => u.role === "tower" && !u.dead && u.hp / u.maxHp < .45);
    if (b.hp / b.maxHp < .55 || towersLow) b.useSkill("heal");
    if (live.length >= 5 || (boss && live.length >= 1)) b.useSkill("barrage");
    if (live.length >= 6) b.useSkill("freeze");
    if (boss || live.some(e => e.def.range > 0 && e.position.x - b.playerX < 1000)) b.useSkill("shield");
    if (live.length >= 3) b.useSkill("rally");
    if (live.length >= 4) b.useSkill("drone");
    if (boss || live.some(e => e.maxHp > 3000)) b.useSkill("orbital");
    if (opts.dodge) {
      const inc = b.bullets.filter(x => x.enemy && !x.dead && x.position.x - b.playerX < 420 && x.position.x - b.playerX > 0);
      b.move = inc.length ? (b.playerX > 400 ? -1 : 1) : (b.playerX > 300 ? -1 : 0);
    }
  };
}
module.exports = { Battle, loadCatalog, catStats, skillAI };
