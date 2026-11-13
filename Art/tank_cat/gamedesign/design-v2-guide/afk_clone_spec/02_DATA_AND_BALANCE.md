# Phụ lục 02 — Data và số liệu thêm vào `src/data.js`

**Không** copy lại toàn bộ `TURRETS` / `HANDHELD` / `PACKS` / `MONSTERS` / `WAVES` / `SKILLS` — chúng đã có trong `src/data.js`. Chỉ **thêm** export. Không đổi số đã khóa bởi `scripts/check.mjs`.

---

## `BALANCE` (thêm)

```js
export const BALANCE = {
  startingWallet: { coins: 2000, spiritDust: 800, soulEssence: 0, forgeStone: 10, gem: 300, summonScroll: 5, relicStone: 0, skillCore: 0, catXp: 200, arenaToken: 0 },
  gemsPerScroll: 100,
  currencyCap: 999_999_999,
  idle: {
    capSeconds: 43200, minDisplaySeconds: 60,
    goldBasePerMin: 7, goldGrowth: 1.065,
    dustBasePerMin: 2.6, dustGrowth: 1.06,
    catXpBasePerMin: 5, catXpGrowth: 1.055,
    forgeBasePerHour: 0.4, forgePerStageHour: 0.05,
    scrollPer12hBase: 1, scrollStageDiv: 5,
    unclearedMultiplier: 0.5,
    chowPerMin: 0.1, beastCagePer12hBase: 0, beastCageStageDiv: 4
  },
  levelUp: { goldBase: 80, goldExp: 1.45, xpBase: 25, xpExp: 1.22, maxLevel: 240 },
  cats: { specialtyBonus: 0.15 },
  account: { xpBase: 15, xpPerLevel: 4, maxLevel: 300, floorsPerLevels: 20, maxFloors: 5, pvpWinXp: 25 },
  fortress: { hpBase: 12000, hpGrowth: 1.08, hpPerFloor: 800, upgradeGoldBase: 400, upgradeGoldExp: 1.4, defaultTurretId: 'turret-0' },
  turret: {
    goldBase: 60, goldExp: 1.40, dustBase: 30, dustExp: 1.30,
    starBonusPerStar: 0.14, levelCapBase: 40, levelCapPerStar: 8,
    emptyOperatorMult: 0.70, operatorAtkRef: 50, operatorAtkCoeff: 0.50,
    operatorFlatAtk: 0.15, operatorHasteCoeff: 0.25, hasteCap: 0.30, minFireInterval: 0.15
  },
  gear: { maxStars: 5, starBonusPerStar: 0.12, factionMult: 1.10,
    rarityMult: { COMMON: 1, RARE: 1.25, ELITE: 1.55, LEGENDARY: 1.90, MYTHIC: 2.30 } },
  gacha: { pityElite: 10, pityLegendary: 40 },
  pvp: { ratingStart: 1000, ratingMin: 0, eloK: 32, eloDiv: 400, winCoins: 200, winDust: 80, winTokens: 8, loseTokens: 2, dailyTokenCap: 40, skillAiInterval: 0.5 },
  nexus: { startingSlots: 3, maxSlots: 8, slotUnlocks: [/* clear → slots */] },
  classic: { maxHp: 48000, energyMax: 600, energyRegen: 9, energyOnKill: 6 },
  loadoutEnergy: { max: 600, startFrac: 1/3, regen: 16, onKill: 12 },
  militia: { maxHp: 240, damageMult: 0.75 },
  combat: { healUnitPct: 0.40, defendRadius: 140 },
  skills: {
    maxLevel: 8, costReducePerLevel: 0.06, minCostFrac: 0.55,
    effectPerLevel: 0.10, cdReduce: 0.25, minCd: 5,
    coreCostIsLevel: true,
    bossCoreFirst: { 5: 2, 10: 3, 15: 4, 20: 6 },
    bossCoreRepeat: { 5: 1, 10: 1, 15: 2, 20: 2 }
  }
};
```

`ascension.levelCaps` / `tierMultipliers` / `requirements`: dùng bảng đã chốt ở bản meta (RECRUIT 20/1.00 …). Copy đủ vào `BALANCE.ascension`.

---

## `CATS` (10 id khớp sprite)

| id | name (CAT_NAMES) | specialty | gợi ý role | rarity gốc |
|---|---|---|---|---|
| cat-0 | Mướp | operator | TANK | ELITE |
| cat-1 | Khói | infantry | MAGE | RARE |
| cat-2 | Bông | infantry | HEALER | ELITE |
| cat-3 | Tam Thể | operator | WARRIOR | RARE |
| cat-4 | Mun | infantry | ASSASSIN | ELITE |
| cat-5 | Xiêm | infantry | RANGER | RARE |
| cat-6 | Vằn | operator | WARRIOR | COMMON |
| cat-7 | Tux | operator | TANK | COMMON |
| cat-8 | Mochi | operator | HEALER | RARE |
| cat-9 | Mây | infantry | RANGER | COMMON |

`baseStats`/`growth`: đặt số gần cảm giác idle (Mướp tank HP cao, Mun atk/speed cao). **Ghi số cụ thể trong file data khi implement**, rồi chốt 1 fixture test (ví dụ Mướp lv1 + turret-0 lv1 0★) trong `check-meta.mjs`. Không lấy số từ tướng AFK (Kael…).

Starting roster: `['cat-0','cat-1','cat-2']` (đúng specialty: Mướp operator, Khói+Bông infantry). Gán đúng vị trí ×1.15; sai vị trí ×1.0, không phạt. `CATS[i].specialty` bắt buộc.

---

## `STAGES`

- 20 ải Hành quân, **mỗi ải đúng 1 đợt** (~1–2 phút, TUNABLE). Schema: `{ id, index, name, recommendedPower, boss: boolean, waves: [ /* length 1 */ { name, types, count, bursts: [{ after, count, spacing }], enemyHpMult, enemyAtkMult } ], firstClearRewards, repeatRewards, fortressHpMult }`.
- Ải thường (`boss: false`, gồm ải 1): không type 19. `bursts` dày dần về cuối (vd. ải 1: 6 con spacing 0.9s, rồi 8 × 0.55s, rồi 10 × 0.28s — số TUNABLE, starter phải thắng).
- Ải trùm **5 / 10 / 15 / 20**: `boss: true`, types gồm 19 + thêm quái, vẫn 1 đợt, `enemyHpMult`/`enemyAtkMult` scale (ải 5 trùm << 90.000 máu).
- `accountXp` first `80+25*index`, repeat `40+8*index` (ải 1: 105 / 48). Coins first `100+30*index`, repeat `40+12*index`. `catXp` first `20+5*index`, repeat `8+2*index`. `spiritDust` first `30+8*index`, repeat `12+4*index`. Repeat giữ nhỏ để idle ~70–80% coins/catXp. Pacing tầng TUNABLE (mục 11.1: late ~5 ngày/tầng). Classic 6 đợt (`WAVES`) không nằm `STAGES` v1.

Gear/module drop: first-clear ải 2–7 cho `gun`/`pack` cụ thể (id có sẵn).

---

## `MOCK_OPPONENTS`

6–8 object: `id`, `name` (tên Việt khác 10 mèo), `accountLevel`, `rating`, `turretIds[]`, `squadCatIds[]`. `LocalMockPvpClient` nặn snapshot bằng StatCalculator giả lập level ≈ accountLevel.

---

## Idle / level-up — số test đã chốt

Idle S=10, 21600s: coins **4441**, spiritDust **1581**, catXp **2914**, forgeStone **5**, summonScroll **1**, monsterChow **36**, beastCage **1**.

Account: `xpToNext = 15 + 4*level` — tổng 1→20/40/60/80 = **1045 / 3705 / 7965 / 13825**. `floorsPerLevels: 20`. Ải ~1–2 phút nên thời gian thực nhanh; **pacing TUNABLE (bàn sau)**, giữ bảng cho thống nhất.

Fortress starter HP: `floor(12000 + 800*1) = 12800`.

Level-up mèo (`coins` / `catXp`): 1→80/25; 2→218/58; 5→825/178; 10→2254/414; 20→6160/966; 40→16829/2251; 60→30297/3692; 80→45980/5244. Tổng catXp 1→20 = 8219; 60→80 ≈ 88418.

Gear 20 atk × (1+0.12*5) × 1.10 = 35 (một lần `Math.floor` cuối).

Elo 1000 vs 1000: ±16.

Không nhân `MONSTER_BALANCE` lần hai.


---

## `MONSTER_BOOK` + lồng (thêm vào `data.js`)

Không sửa `MONSTERS[].hp/damage`. `speciesId` = `asset`. `flying` lấy từ `MONSTERS[].flying`.

```js
export const MONSTER_BOOK = [
  { speciesId: 'bug-0',   catchRarity: 'COMMON',    element: 'nature',  baseAtk: 18, baseHp: 80,  baseDef: 8,  baseSpeed: 90,  weapon: { name: 'Đàn kiến', kind: 'rapid', rate: 0.55, range: 700, color: '#ff9d65' }, passive: null },
  { speciesId: 'beast-0', catchRarity: 'RARE',      element: 'dark',    baseAtk: 26, baseHp: 110, baseDef: 10, baseSpeed: 95,  weapon: { name: 'Nanh đêm', kind: 'claw', rate: 1.1, range: 620, color: '#bca6d0', count: 3 }, passive: null },
  { speciesId: 'bug-4',   catchRarity: 'COMMON',    element: 'nature',  baseAtk: 20, baseHp: 70,  baseDef: 6,  baseSpeed: 110, weapon: { name: 'Kim chích', kind: 'arrow', rate: 0.9, range: 820, color: '#ffdc6b' }, passive: null, flying: true },
  { speciesId: 'bug-1',   catchRarity: 'RARE',      element: 'earth',   baseAtk: 24, baseHp: 140, baseDef: 18, baseSpeed: 70,  weapon: { name: 'Giáp va', kind: 'shell', rate: 1.6, range: 640, color: '#ff777d', splash: 50 }, passive: { fortressDefPct: 0.02 } },
  { speciesId: 'beast-1', catchRarity: 'RARE',      element: 'earth',   baseAtk: 28, baseHp: 130, baseDef: 12, baseSpeed: 80,  weapon: { name: 'Húc', kind: 'shell', rate: 1.5, range: 600, color: '#e8ae76' }, passive: null },
  { speciesId: 'bug-2',   catchRarity: 'COMMON',    element: 'dark',    baseAtk: 19, baseHp: 85,  baseDef: 8,  baseSpeed: 75,  weapon: { name: 'Tơ', kind: 'poison', rate: 1.3, range: 900, color: '#c79ae0', slow: 2 }, passive: null },
  { speciesId: 'bug-3',   catchRarity: 'RARE',      element: 'nature',  baseAtk: 27, baseHp: 100, baseDef: 9,  baseSpeed: 100, weapon: { name: 'Đao nhảy', kind: 'claw', rate: 1.0, range: 580, color: '#c5f47c' }, passive: null },
  { speciesId: 'beast-2', catchRarity: 'ELITE',     element: 'fire',    baseAtk: 36, baseHp: 180, baseDef: 14, baseSpeed: 70,  weapon: { name: 'Cú vồ', kind: 'shell', rate: 1.8, range: 620, color: '#f4a776' }, passive: null },
  { speciesId: 'bug-5',   catchRarity: 'COMMON',    element: 'earth',   baseAtk: 21, baseHp: 90,  baseDef: 10, baseSpeed: 80,  weapon: { name: 'Khoan đất', kind: 'drill', rate: 1.4, range: 640, color: '#ffe0a7', pierce: 2 }, passive: null },
  { speciesId: 'beast-4', catchRarity: 'ELITE',     element: 'fire',    baseAtk: 38, baseHp: 140, baseDef: 10, baseSpeed: 115, weapon: { name: 'Nhát kiếm', kind: 'sniper', rate: 1.7, range: 1000, color: '#ffca57', pierce: 2 }, passive: null },
  { speciesId: 'bug-6',   catchRarity: 'RARE',      element: 'poison',  baseAtk: 25, baseHp: 120, baseDef: 11, baseSpeed: 70,  weapon: { name: 'Nọc', kind: 'poison', rate: 1.5, range: 880, color: '#c298ed', poison: 4 }, passive: null },
  { speciesId: 'beast-3', catchRarity: 'ELITE',     element: 'ice',     baseAtk: 32, baseHp: 160, baseDef: 12, baseSpeed: 65,  weapon: { name: 'Băng ném', kind: 'ice', rate: 1.4, range: 920, color: '#8eeaff', slow: 3 }, passive: { energyRegen: 0.4 } },
  { speciesId: 'bug-7',   catchRarity: 'ELITE',     element: 'poison',  baseAtk: 30, baseHp: 110, baseDef: 8,  baseSpeed: 100, weapon: { name: 'Bụi ảo', kind: 'poison', rate: 1.3, range: 860, color: '#edb569', splash: 80, poison: 3 }, passive: null, flying: true },
  { speciesId: 'beast-5', catchRarity: 'LEGENDARY', element: 'fire',    baseAtk: 44, baseHp: 220, baseDef: 20, baseSpeed: 55,  weapon: { name: 'Dung nham', kind: 'flame', rate: 0.9, range: 700, color: '#ffaf47', burn: 4, splash: 70 }, passive: { maxHpPct: 0.02 } },
  { speciesId: 'beast-6', catchRarity: 'LEGENDARY', element: 'earth',   baseAtk: 48, baseHp: 240, baseDef: 22, baseSpeed: 60,  weapon: { name: 'Húc giáp', kind: 'shell', rate: 2.0, range: 640, color: '#c3a0ef' }, passive: { fortressDefPct: 0.03 } },
  { speciesId: 'bug-8',   catchRarity: 'ELITE',     element: 'crystal', baseAtk: 34, baseHp: 150, baseDef: 14, baseSpeed: 70,  weapon: { name: 'Mảnh pha lê', kind: 'crystal', rate: 1.6, range: 840, color: '#ffce69', count: 5, pierce: 1 }, passive: null },
  { speciesId: 'beast-7', catchRarity: 'LEGENDARY', element: 'wind',    baseAtk: 42, baseHp: 170, baseDef: 11, baseSpeed: 105, weapon: { name: 'Lông vũ', kind: 'arrow', rate: 1.2, range: 1100, color: '#ffd99b', count: 3 }, passive: null, flying: true },
  { speciesId: 'beast-9', catchRarity: 'LEGENDARY', element: 'nature',  baseAtk: 30, baseHp: 200, baseDef: 14, baseSpeed: 70,  weapon: { name: 'Hào quang', kind: 'plasma', rate: 1.6, range: 800, color: '#b5ed8e', splash: 60 }, passive: { maxHpPct: 0.03 } },
  { speciesId: 'bug-9',   catchRarity: 'LEGENDARY', element: 'poison',  baseAtk: 46, baseHp: 210, baseDef: 16, baseSpeed: 60,  weapon: { name: 'Đạn độc chúa', kind: 'poison', rate: 1.7, range: 900, color: '#ed8e9c', splash: 90, poison: 5 }, passive: null },
  { speciesId: 'beast-8', catchRarity: 'MYTHIC',    element: 'dark',    baseAtk: 70, baseHp: 300, baseDef: 18, baseSpeed: 80,  weapon: { name: 'Hơi rồng', kind: 'flame', rate: 1.5, range: 1000, color: '#ff767d', splash: 120, burn: 5 }, passive: { maxHpPct: 0.04 } }
];
```

Đủ 20, khớp `MONSTERS`. `name` lấy từ `MONSTERS[].name` lúc load nếu muốn tránh trùng (Codex được merge theo `speciesId`).

```js
// BALANCE.monsters
{
  cages: {
    beastCage: { COMMON: 5500, RARE: 3000, ELITE: 1200, LEGENDARY: 300, MYTHIC: 0 },
    moonCage:  { COMMON: 2000, RARE: 4000, ELITE: 2700, LEGENDARY: 1200, MYTHIC: 100 }
  },
  pity: { elite: 10, legendary: 40, mythic: 80 },
  shardsByRarity: { COMMON: 10, RARE: 20, ELITE: 40, LEGENDARY: 80, MYTHIC: 150 },
  starShardCosts: [20, 40, 80, 160, 280],
  chowBase: 12, chowExp: 1.30, levelCapBase: 20, levelCapPerStar: 16,
  evo: [{ minStars: 3, minLevel: 40, shards: 50 }, { minStars: 5, minLevel: 80, shards: 100 }],
  /* không companionDamageMult — pet có HP và có thể chết */
  passiveMaxHpCap: 0.08, passiveRegenCap: 0.8,
  evoMultPerStage: 0.25, starBonusPerStar: 0.14, levelGrowth: 0.08, ivBonus: 0.20,
  slots: { secondAtCaught: 5, secondAtLevel: 10, thirdAtCaught: 10, thirdAtLevel: 25 },
  shop: { beastCageCoins: 1500, beastCageGems: 80, moonCageGems: 250, dailyBeastLimit: 5, dailyMoonLimit: 2, dailyRefresh: true },
  arenaShop: { daily: true, stock: [/* dust, essence, forgeStone, scroll, chow, skillCore đắt */] },
  rewards: {
    5:  { beastCage: 2, monsterChow: 20, coins: 200 },
    10: { moonCage: 1, monsterChow: 40, coins: 500 },
    15: { moonCage: 2, monsterChow: 80, gem: 20 },
    20: { moonCage: 5, monsterChow: 200, gem: 100, relicStone: 1 }
  }
}
```

Idle thêm (floor cuối): `chowPerMin: 0.1` → 6 giờ ≈ 36 chow; `beastCagePer12h: floor(S/4)` (S=10 → 2 lồng / 12h, 6h → 1). Không idle `moonCage`.

Chow test: `floor(12 * level^1.30)` → **12, 97, 239, 589** (level 1/5/10/20).


## Bắt trên sân (`BALANCE.monsters.capture`)

```js
capture: {
  energyCost: 0,
  hpThreshold: 0.25,
  bossHpThreshold: 0.10,
  bossBaseChance: 0.02,
  maxThrows: 5,
  chanceCap: 0.90,
  hpSlope: 0.55,
  baseChance: {
    beastCage: { COMMON: 0.50, RARE: 0.28, ELITE: 0.14, LEGENDARY: 0.06, MYTHIC: 0 },
    moonCage:  { COMMON: 0.80, RARE: 0.55, ELITE: 0.32, LEGENDARY: 0.16, MYTHIC: 0 }
  }
}
```

Quái thường: `chance = min(0.90, base * (0.45 + 0.55 * (0.25-hpRatio)/0.25))` khi `hpRatio≤0.25`, else 0.

Trùm: chỉ `moonCage`, `hpRatio < 0.10`, `chance = 0.02 * (0.45 + 0.55 * (0.10-hpRatio)/0.10)` (vừa dưới 10% ≈ 0,9%; 5% = 1,45%; 0% = 2%). `hpRatio >= 0.10` → 0. Không pity sân. Lồng thú lên trùm = 0, không trừ. Mở Lồng trăng ở Chuồng vẫn 1% + pity 80.

Test: COMMON + `beastCage` + hp 0 → **0.50**; hp 0.25 → **0.225**. Trùm + `moonCage` + hp 0.10 → **0**; hp 0.05 → **0.0145**; hp 0 → **0.02**.

Starter bag (hồ sơ mới): `beastCage: 3`, `monsterChow: 15`.


## `SKILL_DEFS` (thêm vào `data.js`, **không** sửa object `SKILLS` 6 id classic)

```js
export const SKILL_DEFS = {
  heal:      { id: 'heal',      name: 'Sửa chữa',     unlock: { start: true },           baseCost: 80,  baseCd: 8,  kind: 'heal',     healVehicle: 9000, healUnitPct: 0.40 },
  barrage:   { id: 'barrage',   name: 'Mưa tên lửa',  unlock: { start: true },           baseCost: 140, baseCd: 10, kind: 'barrage',  rockets: 18, rocketDamage: 190 },
  shield:    { id: 'shield',    name: 'Lá chắn',      unlock: { stage: 2 },              baseCost: 80,  baseCd: 8,  kind: 'shield',   duration: 7 },
  rally:     { id: 'rally',     name: 'Tiếp viện',    unlock: { stage: 4 },              baseCost: 60,  baseCd: 7,  kind: 'rally',    duration: 10, fireMult: 1.8 },
  freeze:    { id: 'freeze',    name: 'Bão băng',     unlock: { stage: 5 },              baseCost: 100, baseCd: 9,  kind: 'freeze',   slow: 6, damage: 120 },
  drone:     { id: 'drone',     name: 'Drone',        unlock: { stage: 10 },             baseCost: 70,  baseCd: 8,  kind: 'drone',    duration: 14 },
  orbital:   { id: 'orbital',   name: 'Không kích',   unlock: { stage: 15 },             baseCost: 150, baseCd: 10, kind: 'orbital',  damage: 4000, bossMult: 1.5, delay: 0.4 },
  decoy:     { id: 'decoy',     name: 'Mồi nhử',      unlock: { accountLevel: 20 },      baseCost: 70,  baseCd: 8,  kind: 'decoy',    duration: 5, hp: 800, radius: 420 },
  magnet:    { id: 'magnet',    name: 'Nam châm',     unlock: { accountLevel: 40 },      baseCost: 50,  baseCd: 6,  kind: 'magnet' },
  kamikaze:  { id: 'kamikaze',  name: 'Mèo cảm tử',   unlock: { accountLevel: 60 },      baseCost: 90,  baseCd: 9,  kind: 'kamikaze', damage: 2200, splash: 180 }
};
```

`SKILLS` (classic) giữ nguyên cost 80/120/150/100/200/120 và cd 16/20/24/20/28/22.

Test nâng: heal lv1 cost 80 cd 8; lv5 cost **60**, cd **7**, effectMult **1.40**.


---

## Kinh tế / daily / shop (xem TANK_CAT_SPEC 11.1)

Nguồn chính: idle 12h. Không gem / moonCage / skillCore / arenaToken / accountXp từ idle.

Daily quest (reset 00:00 local): claim idle; thắng 3 ải; nâng cấp 1 lần; mở lồng hoặc bắt 1; 1 trận PvP. Bonus hoàn thành early ≈ 800 coins, 200 dust, 20 gem, 1 scroll, 10 chow (tăng nhẹ mid/late).

Shop refresh mỗi ngày. Trần mua: beastCage coins 5, gem 5; moonCage gem 2. Arena shop tiêu `arenaToken` (8 thắng / 2 thua, trần 40/ngày).

Thu nhập ngày xấp xỉ (claim 12h + 5 repeat + daily + 3 PvP thắng): early coins ≈ 8400 (idle ~77%); mid ≈ 11500; late ≈ 20200. catXp late idle 9956 / ngày ≈ 11000. Hố: lên cấp mèo (lv60 = 30297 coins + 3692 catXp / lần; 60→80 một con ≈ 88418 catXp ≈ 8 ngày idle; cả đội lâu hơn). Flag lạm phát: đừng buff mạnh repeat coins.

Knockback pet (Battle loadout): 100–160px, stun 0.45–0.8s, clamp x ∈ [40, player.x+80], flying ×0.7.

PvP skill AI: mỗi 0.5s, 1 skill, luật ưu tiên heal/shield → crowd/boss (spec 10.4). Chỉnh sau.


---

## Sẵn sàng mở rộng (data)

Mỗi entry registry có `id` (hoặc `asset` / `speciesId` ổn định) và `classic: true` cho bộ gốc. Entry mới không ghi `classic` (mặc định false) hoặc `classic: false`.

`WAVES[].types` đổi sang speciesId: `['bug-0','beast-0','bug-4']`, …, `['beast-8']` — không `[19]`.

Gacha / lồng: `weight[rarity]` trên bảng rarity, rồi chia đều trong loài cùng rarity (tự normalize). Thêm loài/mèo không sửa tổng tay.

`CATS`, `MONSTER_BOOK`, `SKILL_DEFS`, `STAGES`, gear: thêm dòng là đủ. Test dummy không cần sprite (fallback). Không nhân `MONSTER_BALANCE` lần hai trên entry mới nếu đã ghi số final — hoặc cùng pipeline map() như hiện tại, chọn một và ghi rõ.
