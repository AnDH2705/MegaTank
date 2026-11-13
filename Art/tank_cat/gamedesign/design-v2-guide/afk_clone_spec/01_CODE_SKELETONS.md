# Phụ lục 01 — Skeleton JavaScript (module ES, cho `tank_cat`)

Giữ identifier trong code tiếng Anh. Copy vào path đã ghi. **Không** tạo class Godot. `Battle` mặc định không đổi.

---

## `src/battle.js` — chỉ thêm options, giữ body cũ

```js
export class Battle {
  constructor(onEvent = () => {}, random = Math.random, options = {}) {
    this.emit = onEvent;
    this.random = random;
    this.options = options;
    this.classic = !options.loadout && options.classic !== false;
    // ... existing field init (time, state, hp 48000, energy 600, units 5+10+4, etc.)
    this.energyMax = 600;
    this.pendingCatches = [];
    this.capture = options.capture?.enabled ? { ...options.capture, throwsUsed: 0 } : null;
    if (this.classic) {
      this.assignWeapons(); // existing
    } else {
      this.applyLoadout(options.loadout);
    }
  }
  applyLoadout(loadout) { /* unitHp: set u.maxHp/u.hp/u.alive; energyMax; 1 wave; companions included */ }
  chooseDefendTarget(x, y) { /* loadout only: living companion/infantry/tower in radius, else vehicle */ }
  hurtUnit(u, amount) { /* if !alive return; hp-=; if hp<=0 alive=false, emit unit-down */ }
  assignWeapons() {
    /* if loadout && !loadout.rotateBanks: return
       tower / infantry / aux only — NEVER else→PACKS (companions must be skipped) */
  }
  tryCatch(enemyId, cageType = 'beastCage') {
    /* disabled if !this.capture; early fail does not consume; see spec 13.3 */
    return false;
  }
  companionAcquire() { /* own AI: nearest valid in range; independent of player targetId */ }
  applyKnockback(u, fromX, fromY, dist, stun) { /* clamp x to [40, player.x+80]; flying *0.7 */ }
  // start, spawn, chooseTarget, step, fire, makeBullet, impact, damage, hurtPlayer: classic unchanged
  // useSkill('heal'): classic +9000 vehicle only; if unitHp, also heal living units (no revive)
  // step: skip !u.alive; companion follow/leash + companionAcquire + fire; mark e.catchable
  // boss (and charge) hits: hurtUnit + applyKnockback on living companions / infantry (0.6)
  // enemy attacks: if unitHp then chooseDefendTarget else hurtPlayer
}
```

`step`: đã có `if(!u.visible||u.timer>0)continue` — tầng khóa đặt `visible=false`. Companion không đi nhánh infantry (xoay 18s).

---

## `src/meta/save.js`

```js
export const SAVE_KEY = 'meow-bastion-save-v1';
export const SCHEMA_VERSION = 4;

export function createSave(storage = globalThis.localStorage) {
  return {
    load() {
      const raw = storage?.getItem(SAVE_KEY);
      if (!raw) return null;
      try {
        const data = JSON.parse(raw);
        if (!data || typeof data !== 'object') throw new Error('bad');
        return migrate(data);
      } catch {
        try { storage.setItem(`${SAVE_KEY}-broken-${Math.floor(Date.now()/1000)}`, raw); } catch {}
        return null;
      }
    },
    save(data) {
      const payload = { ...data, schemaVersion: SCHEMA_VERSION, savedAtUnix: Math.floor(Date.now()/1000) };
      storage.setItem(SAVE_KEY, JSON.stringify(payload));
    },
    clear() { storage?.removeItem(SAVE_KEY); }
  };
}
export function emptyMonsterState() {
  return {
    bag: { beastCage: 0, moonCage: 0, monsterChow: 0 },
    instances: [], shards: {}, seen: [], caught: [],
    pity: { beastElite: 0, beastLegendary: 0, moonElite: 0, moonLegendary: 0, moonMythic: 0 },
    companionSlots: ["", "", ""],
    lastDailyUnix: 0
  };
}
export function emptySkillState() {
  return { levels: { heal: 1, barrage: 1 }, loadout: ['heal', 'barrage', '', ''] };
}
export function migrate(data) {
  if (!data.schemaVersion || data.schemaVersion < 2) {
    data.monsters = { ...emptyMonsterState(), ...(data.monsters || {}) };
    data.monsters.bag = { ...emptyMonsterState().bag, ...(data.monsters.bag || {}) };
    data.monsters.pity = { ...emptyMonsterState().pity, ...(data.monsters.pity || {}) };
    data.schemaVersion = 2;
  }
  if (data.schemaVersion < 3) {
    data.wallet = { skillCore: 0, ...(data.wallet || {}) };
    if (!data.wallet.skillCore) data.wallet.skillCore = 0;
    data.skills = { ...emptySkillState(), ...(data.skills || {}) };
    data.schemaVersion = 3;
  }
  if (data.schemaVersion < 4) {
    data.wallet = { catXp: 0, arenaToken: 0, ...(data.wallet || {}) };
    if (data.wallet.catXp == null) data.wallet.catXp = 0;
    if (data.wallet.arenaToken == null) data.wallet.arenaToken = 0;
    data.dailyQuests = data.dailyQuests || { date: '', done: {}, claimedBonus: false };
    data.pvp = { tokensToday: 0, tokenDay: '', ...(data.pvp || {}) };
    data.schemaVersion = 4;
  }
  return data;
}
```

---

## `src/meta/wallet.js`

```js
export const CURRENCY_KEYS = ['coins','spiritDust','soulEssence','forgeStone','gem','summonScroll','relicStone','skillCore','catXp','arenaToken'];
export const WALLET_CAP = 999_999_999;

export class Wallet {
  constructor(amounts = {}) {
    this.amounts = Object.fromEntries(CURRENCY_KEYS.map(k => [k, 0]));
    this.fromJSON(amounts);
  }
  get(id) { return this.amounts[id] ?? 0; }
  add(id, amount) {
    if (amount < 0) throw new Error('negative add');
    if (!Object.hasOwn(this.amounts, id)) return;
    this.amounts[id] = Math.min(WALLET_CAP, this.amounts[id] + amount);
  }
  canAfford(cost = {}) { return Object.entries(cost).every(([k,v]) => this.get(k) >= v); }
  trySpend(cost = {}) {
    if (!this.canAfford(cost)) return false;
    for (const [k,v] of Object.entries(cost)) this.amounts[k] -= v;
    return true;
  }
  toJSON() { return { ...this.amounts }; }
  fromJSON(data = {}) { for (const k of CURRENCY_KEYS) if (data[k] != null) this.amounts[k] = Math.max(0, Math.floor(data[k])); }
}
```

---

## `src/meta/idle.js`

```js
export class IdleSystem {
  constructor(balance, getStageIndex) {
    this.balance = balance.idle;
    this.getStageIndex = getStageIndex; // () => S
    this.lastAccrualUnix = 0;
  }
  now() { return Math.floor(Date.now() / 1000); }
  effectiveElapsed(now = this.now()) {
    if (now < this.lastAccrualUnix) return 0;
    return Math.min(now - this.lastAccrualUnix, this.balance.capSeconds);
  }
  peek(now = this.now()) {
    const idle = this.balance;
    const cap = idle.capSeconds;
    const elapsed = this.effectiveElapsed(now);
    const S = this.getStageIndex();
    const minutes = elapsed / 60, hours = elapsed / 3600, frac = cap ? elapsed / cap : 0;
    const stage = Math.max(S, 1);
    let coins = Math.floor(idle.goldBasePerMin * Math.pow(idle.goldGrowth, stage - 1) * minutes);
    let spiritDust = Math.floor(idle.dustBasePerMin * Math.pow(idle.dustGrowth, stage - 1) * minutes);
    let catXp = Math.floor((idle.catXpBasePerMin || 0) * Math.pow(idle.catXpGrowth || 1, stage - 1) * minutes);
    let forgeStone = Math.floor((idle.forgeBasePerHour + idle.forgePerStageHour * S) * hours);
    let summonScroll = Math.floor((idle.scrollPer12hBase + Math.floor(S / idle.scrollStageDiv)) * frac);
    let monsterChow = Math.floor((idle.chowPerMin || 0.1) * minutes);
    let beastCage = Math.floor((idle.beastCagePer12hBase + Math.floor(S / (idle.beastCageStageDiv || 4))) * frac);
    if (S === 0) {
      const m = idle.unclearedMultiplier;
      coins = Math.floor(coins * m); spiritDust = Math.floor(spiritDust * m); catXp = Math.floor(catXp * m);
      forgeStone = Math.floor(forgeStone * m); summonScroll = Math.floor(summonScroll * m);
      monsterChow = Math.floor(monsterChow * m); beastCage = Math.floor(beastCage * m);
    }
    return { coins, spiritDust, catXp, forgeStone, summonScroll, monsterChow, beastCage, elapsedSeconds: elapsed, capped: elapsed >= cap && cap > 0, stageIndex: S };
  }
  claim(wallet, bag, now = this.now()) {
    const rewards = this.peek(now);
    wallet.add('coins', rewards.coins);
    wallet.add('spiritDust', rewards.spiritDust);
    wallet.add('catXp', rewards.catXp || 0);
    wallet.add('forgeStone', rewards.forgeStone);
    wallet.add('summonScroll', rewards.summonScroll);
    if (bag) {
      bag.monsterChow = (bag.monsterChow || 0) + rewards.monsterChow;
      bag.beastCage = (bag.beastCage || 0) + rewards.beastCage;
    }
    this.lastAccrualUnix = now;
    return rewards;
  }
}
```

Giữ đúng thứ tự floor. Test S=10, elapsed=21600 → 4441 / 1581 / 2914 / 5 / 1 / 36 / 1.

---

## `src/meta/account.js`

```js
export class Account {
  constructor(balance) {
    this.balance = balance.account;
    this.level = 1;
    this.xp = 0;
  }
  xpToNext() { return this.balance.xpBase + this.balance.xpPerLevel * this.level; } // 15 + 4*level
  unlockedFloorCount() {
    const { maxFloors, floorsPerLevels } = this.balance;
    return Math.min(maxFloors, 1 + Math.floor(this.level / floorsPerLevels));
  }
  addXp(amount) {
    let gained = 0;
    let left = amount;
    while (left > 0 && this.level < this.balance.maxLevel) {
      const need = this.xpToNext() - this.xp;
      if (left >= need) { left -= need; this.xp = 0; this.level++; gained++; }
      else { this.xp += left; left = 0; }
    }
    return { levelsGained: gained, newLevel: this.level };
  }
}
```

---

## `src/meta/stats.js` (rút gọn API)

```js
export class StatCalculator {
  constructor(data) { this.data = data; }
  compute(cat, ctx) {
    /* base+growth, tier, gear; if ctx.role && cat.specialty===ctx.role: * (1+specialtyBonus 0.15) on atk+maxHp */
    return { maxHp:1, atk:1, def:1, speed:1, critRate:0.05, critDamage:1.5 };
  }
  computeTurret(turret, operator, ctx) {
    // scale TURRETS; empty * 0.70; operator match specialty already baked in compute(cat, {role:'operator'})
  }
  toWeapon(baseWeapon, turretStats) {
    return { ...baseWeapon, damage: turretStats.atk, rate: turretStats.fireInterval, range: turretStats.range ?? baseWeapon.range };
  }
  power(stats) { return Math.floor(stats.maxHp*0.15 + stats.atk*1.6 + stats.def*1.2 + stats.speed*2); }
}
```

---

## `src/meta/gacha.js` / `pvp.js` / `game-state.js`

Gacha: `summonOne()`, `summonTen()`, pity fields, `Random` inject.

Pvp: `ratingDelta(...)`. `LocalMockPvpClient`. `pickPvpSkill(battle, loadoutSkills, dt)` — mỗi 0.5s, 1 skill nếu energy≥cost và CD sẵn, luật 10.4 (heal khi hp<50%, …). Người chơi thủ công; chỉnh sau. Token: thắng 8 / thua 2, trần 40/ngày.

GameState: starter; `markDirty()`; `grantRewards`; `trySummon`; `buildLoadout(stage)`; `onBattleEnd(result, source)` — luôn cộng loot đã nhặt + trừ lồng đã ném; **chỉ thắng** mới first-clear / accountXp / resolvePendingCatches; **thua** hủy pending.

`hub.js`: querySelector `#hub`, bind 3 chỗ (Hành quân / Doanh trại / Thành mèo), `hub.go(target)` cho daily Đi (xem 03). Không tính floor XP.

`game.js`: CTA Hành quân → `new Battle(..., { loadout, unitHp: true, capture })` + HUD `#skillbar-loadout` 4 ô (phím 1–4) và `#catch`. Classic: `new Battle(onEvent)`, HUD 6 skill cũ, không capture/unitHp. PvP không capture.

---

## `scripts/check-meta.mjs`

```js
import assert from 'node:assert/strict';
import { Wallet } from '../src/meta/wallet.js';
import { IdleSystem } from '../src/meta/idle.js';
import { Account } from '../src/meta/account.js';
import { BALANCE } from '../src/data.js';
import { Battle } from '../src/battle.js';
import { ratingDelta } from '../src/meta/pvp.js';

// wallet atomic, idle 4441/1581/2914/5/1/36/1, account floors, elo ±16,
// specialty +15%, lose discards pendingCatches, PvP skill AI heal-when-low,
// new Battle() still 5 tower + 10 infantry (sanity),
// new Battle(fn, rnd, { loadout: mini }) steps and can kill one weak spawn.
console.log('PASS meta');
```

Cũng gọi `node --check` các file `src/meta/*.js` + `hub.js`.


---

## `src/meta/monsters.js`

```js
import { MONSTERS, MONSTER_BOOK, BALANCE } from '../data.js';

export function speciesById(id) {
  return MONSTERS.find(m => m.asset === id);
}

export function rollIvs(rng) {
  const stat = () => Math.floor(rng() * 16);
  return { hp: stat(), atk: stat(), def: stat(), speed: stat() };
}

/** @param {'beastCage'|'moonCage'} cageType */
export function openCage(cageType, rng, state, balance = BALANCE.monsters) {
  // 1) increment pity  2) floor rarity from pity  3) roll weights  4) pick species
  // 5) if already in state.caught: add shards, else new instance (ivs once)
  // NEVER use Math.random; only rng()
  return { speciesId: '', rarity: 'COMMON', isNew: false, shardsGranted: 0, instance: null, pityTriggered: '' };
}

export function chowCost(level, balance = BALANCE.monsters) {
  return Math.floor(balance.chowBase * Math.pow(level, balance.chowExp)); // 12, 1.30
}

export function levelCap(stars) { return 20 + 16 * stars; }

export function tryLevelUp(instance, bag, steps, balance) { return { ok: false, levelsGained: 0 }; }
export function tryStarUp(instance, shards, balance) { return { ok: false }; }
export function tryEvolve(instance, shards, balance) { return { ok: false }; }

export function companionStats(instance, bookEntry, balance) {
  // starMult = 1+0.14*stars; evoMult = 1+0.25*evo; iv = 1+iv/15*0.20
  // atk = floor(baseAtk * (1+0.08*(level-1)) * starMult * evoMult * ivAtk)
  return { atk: 1, maxHp: 1, def: 1, speed: 1, rate: 1, range: 1 };
}

export function toCompanionSnapshot(instance, bookEntry, stats) {
  return {
    id: `companion-${instance.uid}`,
    role: 'companion',
    name: bookEntry.name,
    asset: instance.speciesId,
    flying: !!bookEntry.flying,
    weapon: { ...bookEntry.weapon, damage: stats.atk, rate: stats.rate, range: stats.range },
    maxHp: stats.maxHp,
    offset: { x: 0, y: 0 }
  };
}

export function unlockedCompanionSlots(caughtCount, accountLevel, balance = BALANCE.monsters.slots) {
  let n = 1;
  if (caughtCount >= 5 || accountLevel >= 10) n = 2;
  if (caughtCount >= 10 || accountLevel >= 25) n = 3;
  return n;
}

export function collectionRewardTier(caughtCount, flags) {
  // return which of 5/10/15/20 not yet claimed
  return null;
}

/** pending from Battle.pendingCatches; roll IVs here, not in Battle */
export function resolvePendingCatches(pending, state, rng, balance) {
  return { results: [] }; // each: { speciesId, isNew, shardsGranted, instance, ivs }
}

export function catchChance(hpRatio, rarity, cageType, cap, isBoss = false) {
  if (isBoss) {
    if (cageType !== 'moonCage' || hpRatio >= cap.bossHpThreshold) return 0;
    const bonus = (cap.bossHpThreshold - hpRatio) / cap.bossHpThreshold;
    return Math.min(0.05, cap.bossBaseChance * ((1 - cap.hpSlope) + cap.hpSlope * bonus));
  }
  const t = cap.hpThreshold, slope = cap.hpSlope;
  if (hpRatio > t) return 0;
  const base = cap.baseChance[cageType][rarity] || 0;
  const hpBonus = (t - hpRatio) / t;
  return Math.min(cap.chanceCap, base * ((1 - slope) + slope * hpBonus));
}

export function buildCaptureOptions(bag, book, balance) {
  // only for loadout; classic/PvP → enabled false. charges capped by maxThrows; boss: moonCage, hp<10%, no field pity
  return { enabled: false };
}
```

Mọi hàm thuần (không DOM, không `Battle`). `game.js` / `loadout.js` gọi `toCompanionSnapshot` khi dựng loadout. `visuals.js` chỉ đọc `u.role==='companion'` và `u.asset`.

---

## Bổ sung `scripts/check-meta.mjs`

Thêm assert: chow 12/97/239/589; `openCage` seeded; classic không đổi; pet chết `alive===false`; ải 1 đợt / trùm 5; floors 19/20/80; XP 1045/13825; catchChance 13.9; skill 4 ô / heal lv5; energy 200/16; tryCatch không trừ energy; specialty Mướp operator ×1.15, infantry ×1.0; thua hủy pending, ví giữ loot; idle S=10 6h 4441/1581/2914/5/1; cat level 1 cost 80 coins + 25 catXp; PvP AI heal khi hp thấp; **M20 dummy** (cat/monster/weapon) lookup + spawn + save orphan + sprite fallback, `check.mjs` vẫn PASS.


---

## `src/meta/skills.js`

```js
import { SKILL_DEFS, BALANCE } from '../data.js';

export function isUnlocked(id, { highestCleared = 0, accountLevel = 1 } = {}, defs = SKILL_DEFS) {
  const u = defs[id]?.unlock; if (!u) return false;
  if (u.start) return true;
  if (u.stage != null) return highestCleared >= u.stage;
  if (u.accountLevel != null) return accountLevel >= u.accountLevel;
  return false;
}

export function effectMult(level) { return 1 + 0.10 * (level - 1); }
export function upgradedCost(baseCost, level) {
  return Math.max(Math.ceil(baseCost * 0.55), Math.floor(baseCost * (1 - 0.06 * (level - 1))));
}
export function upgradedCd(baseCd, level) { return Math.max(5, baseCd - 0.25 * (level - 1)); }
export function coreCost(level) { return level; } // spend `level` to go level → level+1

export function upgradeSkill(id, state, wallet, defs = SKILL_DEFS) {
  // fail if locked, max level, or cannot afford; else spend skillCore, level++
  return { ok: false };
}

export function toSkillSnapshot(loadoutIds, levels, defs = SKILL_DEFS) {
  // 1–4 baked defs for Battle (cost, cooldown, effects * effectMult)
  return [];
}

export function setLoadout(ids, ctx) { /* max 4, must be unlocked, unique, pad '' */ return ['heal','barrage','','']; }
```

Classic không gọi module này. `loadout.js` dùng `toSkillSnapshot`.


---

## Registry / mở rộng (mục 16)

```js
export function byId(list, id) { return list.find(x => x.id === id || x.asset === id || x.speciesId === id) || null; }
export function indexById(list) {
  const m = new Map();
  for (const x of list) { if (x.id) m.set(x.id, x); if (x.asset) m.set(x.asset, x); if (x.speciesId) m.set(x.speciesId, x); }
  return m;
}
export function classicOnly(list) { return list.filter(x => x.classic !== false); }
export function resolveSprite(name, textures, fallback = 'cat-0') {
  if (textures?.has?.(name) || textures?.get?.(name)) return name;
  return fallback; // never throw
}
```

`Battle.spawn(speciesId)` lookup `byId(MONSTERS, speciesId)`. `WAVES[].types` / ải: id chuỗi. Save chỉ id; id lạ → `orphans`, không throw.

Test dummy: bản sao `CATS`/`MONSTERS`/`TURRETS` + 1 entry `*-dummy` — byId, length, gacha/cage normalize, spawn, save ghost, `resolveSprite` fallback. Không sửa file production trong test này.
