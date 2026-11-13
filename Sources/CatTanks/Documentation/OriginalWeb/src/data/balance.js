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

BALANCE.monsters = {
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
};
BALANCE.monsters.capture = {
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
};

BALANCE.startingRoster = ['cat-0','cat-1','cat-2'];
BALANCE.ascension = {levelCaps:{RECRUIT:20,VETERAN:40,ELITE:60,ELITE_PLUS:80,LEGENDARY:100,LEGENDARY_PLUS:120,MYTHIC:140,MYTHIC_PLUS:160,TRANSCENDENT:180,TRANSCENDENT_1:200,TRANSCENDENT_2:220,TRANSCENDENT_3:240},tierMultipliers:{RECRUIT:1,VETERAN:1.1,ELITE:1.22,ELITE_PLUS:1.35,LEGENDARY:1.5,LEGENDARY_PLUS:1.66,MYTHIC:1.85,MYTHIC_PLUS:2.05,TRANSCENDENT:2.3,TRANSCENDENT_1:2.5,TRANSCENDENT_2:2.7,TRANSCENDENT_3:2.95},needSelf:[0,0,0,1,1,2,1,2,1,1,1],essenceBase:10,essenceStep:15};
BALANCE.nexus.slotUnlocks=[{clear:4,slots:4},{clear:6,slots:5},{clear:8,slots:6},{clear:10,slots:7},{clear:13,slots:8}];
BALANCE.recycle={COMMON:5,RARE:15,ELITE:40,LEGENDARY:120,MYTHIC:250,perCopy:20};
BALANCE.gacha.weights={COMMON:5000,RARE:3500,ELITE:1200,LEGENDARY:270,MYTHIC:30};
BALANCE.monsters.startingBag={beastCage:3,moonCage:0,monsterChow:15};
BALANCE.monsters.knockback={distance:130,stun:.6,flyingMult:.7,infantryMult:.6};
BALANCE.monsters.loot={cageBonusChance:.15,moonChance:.08,chowBase:8,chowKillDiv:5};
BALANCE.gear.enhance={goldBase:200,forgeBase:5,forgeStep:5};
BALANCE.unlocks={monsters:1,skills:2,town:3,shop:3,summon:4,arena:5};
BALANCE.daily={bonus:{coins:800,spiritDust:200,gem:20,summonScroll:1,monsterChow:10,accountXp:150},individual:{coins:40,catXp:10},monsterGift:{beastCage:1,monsterChow:5}};
BALANCE.campaign={hpBase:.13,atkBase:.17,growth:1.12,bossHpBase:.018,firstGoldBase:100,firstGoldStep:30,repeatGoldBase:40,repeatGoldStep:12};
