# Phụ lục 02 — Dữ liệu mẫu (JSON, copy vào `res://data/`)

Mọi file dưới đây là **bản đầy đủ**, không phải lược đồ. Codex tạo đúng path, đúng id. Được sửa số khi cân bằng ải 1 quá khó/dễ, **không đổi schema / id**.

---

## `res://data/balance.json`

```json
{
  "starting_wallet": {
    "gold": 2000,
    "spirit_dust": 800,
    "soul_essence": 0,
    "forge_stone": 10,
    "gem": 300,
    "summon_scroll": 5,
    "relic_stone": 0
  },
  "starting_roster": ["kael", "serin", "nila"],
  "currencies": { "cap": 999999999, "gems_per_scroll": 100 },
  "idle": {
    "cap_seconds": 43200,
    "min_display_seconds": 60,
    "gold_base_per_min": 20,
    "gold_growth": 1.08,
    "dust_base_per_min": 8,
    "dust_growth": 1.07,
    "forge_base_per_hour": 0.4,
    "forge_per_stage_hour": 0.05,
    "scroll_per_12h_base": 1,
    "scroll_stage_div": 5,
    "gem_per_12h": 0,
    "essence_per_12h": 0,
    "uncleared_multiplier": 0.5
  },
  "level_up": {
    "gold_base": 80,
    "gold_exp": 1.45,
    "dust_base": 40,
    "dust_exp": 1.35,
    "max_level": 240
  },
  "ascension": {
    "level_caps": {
      "RECRUIT": 20, "VETERAN": 40, "ELITE": 60, "ELITE_PLUS": 80,
      "LEGENDARY": 100, "LEGENDARY_PLUS": 120, "MYTHIC": 140, "MYTHIC_PLUS": 160,
      "TRANSCENDENT": 180, "TRANSCENDENT_1": 200, "TRANSCENDENT_2": 220, "TRANSCENDENT_3": 240
    },
    "tier_multipliers": {
      "RECRUIT": 1.00, "VETERAN": 1.10, "ELITE": 1.22, "ELITE_PLUS": 1.35,
      "LEGENDARY": 1.50, "LEGENDARY_PLUS": 1.66, "MYTHIC": 1.85, "MYTHIC_PLUS": 2.05,
      "TRANSCENDENT": 2.30, "TRANSCENDENT_1": 2.50, "TRANSCENDENT_2": 2.70, "TRANSCENDENT_3": 2.95
    },
    "starting_tier_by_rarity": {
      "COMMON": "RECRUIT", "RARE": "RECRUIT", "ELITE": "VETERAN",
      "LEGENDARY": "ELITE", "MYTHIC": "ELITE_PLUS"
    },
    "requirements": {
      "RECRUIT": { "need_self": 0, "fodder": [{ "rarity": "COMMON", "faction_rule": "any", "count": 2 }] },
      "VETERAN": { "need_self": 0, "fodder": [
        { "rarity": "COMMON", "faction_rule": "any", "count": 4 },
        { "rarity": "RARE", "faction_rule": "same", "count": 1 }
      ]},
      "ELITE": { "need_self": 0, "fodder": [{ "rarity": "RARE", "faction_rule": "same", "count": 2 }] },
      "ELITE_PLUS": { "need_self": 1, "fodder": [{ "rarity": "RARE", "faction_rule": "same", "count": 2 }] },
      "LEGENDARY": { "need_self": 1, "fodder": [{ "rarity": "ELITE", "faction_rule": "same", "count": 1 }] },
      "LEGENDARY_PLUS": { "need_self": 2, "fodder": [{ "rarity": "ELITE", "faction_rule": "same", "count": 2 }] },
      "MYTHIC": { "need_self": 1, "fodder": [] },
      "MYTHIC_PLUS": { "need_self": 2, "fodder": [] },
      "TRANSCENDENT": { "need_self": 1, "fodder": [] },
      "TRANSCENDENT_1": { "need_self": 1, "fodder": [] },
      "TRANSCENDENT_2": { "need_self": 1, "fodder": [] }
    }
  },
  "recycle": {
    "by_rarity": { "COMMON": 5, "RARE": 15, "ELITE": 40, "LEGENDARY": 120, "MYTHIC": 250 },
    "by_tier": {
      "RECRUIT": 0, "VETERAN": 5, "ELITE": 15, "ELITE_PLUS": 25, "LEGENDARY": 40,
      "LEGENDARY_PLUS": 55, "MYTHIC": 80, "MYTHIC_PLUS": 100, "TRANSCENDENT": 140,
      "TRANSCENDENT_1": 160, "TRANSCENDENT_2": 180, "TRANSCENDENT_3": 200
    },
    "per_copy": 20,
    "per_level": 0.25
  },
  "nexus": {
    "starting_slots": 3,
    "max_slots": 10,
    "min_anchors_for_level": 5,
    "slot_unlocks": [
      { "slot": 4, "clear": 4 },
      { "slot": 5, "clear": 6 },
      { "slot": 6, "clear": 8 },
      { "slot": 7, "clear": 10 },
      { "slot": 8, "clear": 13 },
      { "slot": 9, "clear": 16 },
      { "slot": 10, "clear": 20 }
    ]
  },
  "gear": {
    "max_stars": 5,
    "star_bonus_per_star": 0.12,
    "faction_gear_mult": 1.10,
    "rarity_mult": { "COMMON": 1.00, "RARE": 1.25, "ELITE": 1.55, "LEGENDARY": 1.90, "MYTHIC": 2.30 },
    "forge_base": 5,
    "forge_per_star": 5,
    "forge_rarity_bonus": { "COMMON": 0, "RARE": 2, "ELITE": 5, "LEGENDARY": 10, "MYTHIC": 18 },
    "gold_base": 200
  },
  "relic": {
    "max_level": 10,
    "stones_for_level": [0, 10, 10, 20, 30, 40, 50, 60, 70, 80, 90],
    "ult_damage_mult_at": { "0": 1.0, "1": 1.0, "5": 1.15, "10": 1.30 }
  },
  "faction": {
    "advantage_damage_dealt": 1.15,
    "disadvantage_damage_dealt": 0.85
  },
  "gacha": {
    "pity_elite": 10,
    "pity_legendary": 40
  },
  "campaign": {
    "enemy_level_per_stage": 3,
    "enemy_growth": 1.09,
    "growth_k": 0.08,
    "enemy_hp_bias": 1.15,
    "enemy_atk_bias": 1.00,
    "first_clear_gold_base": 500,
    "first_clear_gold_growth": 1.12,
    "repeat_gold_ratio": 0.24,
    "boss_hp_bias": 1.8,
    "boss_atk_bias": 1.2
  },
  "combat": {
    "tick_rate": 10,
    "max_duration_seconds": 90,
    "atb_threshold": 1000,
    "speed_to_atb": 10,
    "energy_max": 1000,
    "basic_energy_default": 50,
    "ult_energy_cost": 1000,
    "front_damage_taken_mult": 1.15,
    "back_damage_taken_mult": 0.90,
    "melee_vs_back_penalty": 0.85,
    "heal_power_from_atk": 0.80,
    "defend_factor": 80,
    "min_damage": 1,
    "timeout_is_defeat": true
  },
  "power": {
    "hp": 0.15, "atk": 1.6, "def": 1.2, "speed": 2.0,
    "crit_rate": 400, "crit_damage_offset": 200, "relic_level": 25
  }
}
```

---

## `res://data/heroes/heroes.json`

```json
{
  "heroes": [
    {
      "hero_id": "kael",
      "name": "Kael",
      "faction": "DAWNKIN",
      "role": "TANK",
      "rarity": "ELITE",
      "is_fodder_only": false,
      "placeholder_color": "#4A6FA5",
      "base_stats": { "max_hp": 320, "atk": 28, "def": 22, "speed": 85 },
      "growth": { "max_hp": 28, "atk": 2.0, "def": 1.6, "speed": 0.4 },
      "ratio_overrides": { "crit_rate": 0.05, "crit_damage": 1.50, "energy_regen": 50 },
      "skill_profile": "tank",
      "blurb": "A Dawnkin vanguard who holds the front line."
    },
    {
      "hero_id": "serin",
      "name": "Serin",
      "faction": "DAWNKIN",
      "role": "MAGE",
      "rarity": "RARE",
      "is_fodder_only": false,
      "placeholder_color": "#6EC1E4",
      "base_stats": { "max_hp": 200, "atk": 42, "def": 10, "speed": 95 },
      "growth": { "max_hp": 16, "atk": 3.2, "def": 0.7, "speed": 0.5 },
      "ratio_overrides": { "crit_rate": 0.06, "crit_damage": 1.55, "energy_regen": 50 },
      "skill_profile": "mage",
      "blurb": "A scholar of pale fire, striking entire rows."
    },
    {
      "hero_id": "nila",
      "name": "Nila",
      "faction": "SYLVAN",
      "role": "HEALER",
      "rarity": "ELITE",
      "is_fodder_only": false,
      "placeholder_color": "#3CB371",
      "base_stats": { "max_hp": 230, "atk": 30, "def": 12, "speed": 92 },
      "growth": { "max_hp": 20, "atk": 2.2, "def": 0.9, "speed": 0.4 },
      "ratio_overrides": { "crit_rate": 0.05, "crit_damage": 1.50, "energy_regen": 55 },
      "skill_profile": "healer",
      "blurb": "A grove tender who knits wounds mid-battle."
    },
    {
      "hero_id": "ruga",
      "name": "Ruga",
      "faction": "IRONFANG",
      "role": "WARRIOR",
      "rarity": "ELITE",
      "is_fodder_only": false,
      "placeholder_color": "#C45C26",
      "base_stats": { "max_hp": 280, "atk": 38, "def": 16, "speed": 90 },
      "growth": { "max_hp": 22, "atk": 3.0, "def": 1.1, "speed": 0.45 },
      "ratio_overrides": { "crit_rate": 0.08, "crit_damage": 1.60, "energy_regen": 50 },
      "skill_profile": "warrior",
      "blurb": "An Ironfang raider who lives for the clash."
    },
    {
      "hero_id": "thorn",
      "name": "Thorn",
      "faction": "SYLVAN",
      "role": "RANGER",
      "rarity": "RARE",
      "is_fodder_only": false,
      "placeholder_color": "#7A9A3A",
      "base_stats": { "max_hp": 210, "atk": 40, "def": 11, "speed": 100 },
      "growth": { "max_hp": 17, "atk": 3.1, "def": 0.7, "speed": 0.55 },
      "ratio_overrides": { "crit_rate": 0.10, "crit_damage": 1.60, "energy_regen": 50 },
      "skill_profile": "ranger",
      "blurb": "A quiet bowhunter of the deep green."
    },
    {
      "hero_id": "morwen",
      "name": "Morwen",
      "faction": "UMBRAL",
      "role": "MAGE",
      "rarity": "ELITE",
      "is_fodder_only": false,
      "placeholder_color": "#6B3FA0",
      "base_stats": { "max_hp": 195, "atk": 44, "def": 9, "speed": 96 },
      "growth": { "max_hp": 15, "atk": 3.4, "def": 0.6, "speed": 0.5 },
      "ratio_overrides": { "crit_rate": 0.07, "crit_damage": 1.65, "energy_regen": 50 },
      "skill_profile": "mage",
      "blurb": "She writes ruin in violet script."
    },
    {
      "hero_id": "solenne",
      "name": "Solenne",
      "faction": "ASTRAL",
      "role": "HEALER",
      "rarity": "LEGENDARY",
      "is_fodder_only": false,
      "placeholder_color": "#F2D675",
      "base_stats": { "max_hp": 250, "atk": 34, "def": 13, "speed": 94 },
      "growth": { "max_hp": 22, "atk": 2.6, "def": 1.0, "speed": 0.45 },
      "ratio_overrides": { "crit_rate": 0.06, "crit_damage": 1.50, "energy_regen": 60 },
      "skill_profile": "healer",
      "blurb": "A starlit warden whose light mends the fallen."
    },
    {
      "hero_id": "drax",
      "name": "Drax",
      "faction": "ABYSSAL",
      "role": "ASSASSIN",
      "rarity": "LEGENDARY",
      "is_fodder_only": false,
      "placeholder_color": "#8B1E3F",
      "base_stats": { "max_hp": 200, "atk": 46, "def": 10, "speed": 110 },
      "growth": { "max_hp": 16, "atk": 3.6, "def": 0.6, "speed": 0.6 },
      "ratio_overrides": { "crit_rate": 0.12, "crit_damage": 1.80, "energy_regen": 55, "lifesteal": 0.05 },
      "skill_profile": "assassin",
      "blurb": "He finds the weakest pulse and ends it."
    },
    {
      "hero_id": "squire",
      "name": "Squire",
      "faction": "DAWNKIN",
      "role": "WARRIOR",
      "rarity": "COMMON",
      "is_fodder_only": true,
      "placeholder_color": "#9AA0A6",
      "base_stats": { "max_hp": 160, "atk": 22, "def": 10, "speed": 80 },
      "growth": { "max_hp": 12, "atk": 1.5, "def": 0.6, "speed": 0.3 },
      "skill_profile": "warrior",
      "blurb": "A drill-yard recruit, often used as fodder."
    },
    {
      "hero_id": "pup",
      "name": "Pup",
      "faction": "IRONFANG",
      "role": "WARRIOR",
      "rarity": "COMMON",
      "is_fodder_only": true,
      "placeholder_color": "#B08A6A",
      "base_stats": { "max_hp": 150, "atk": 24, "def": 8, "speed": 82 },
      "growth": { "max_hp": 11, "atk": 1.6, "def": 0.5, "speed": 0.3 },
      "skill_profile": "warrior",
      "blurb": "Young Ironfang, all bite and little discipline."
    },
    {
      "hero_id": "sprout",
      "name": "Sprout",
      "faction": "SYLVAN",
      "role": "RANGER",
      "rarity": "COMMON",
      "is_fodder_only": true,
      "placeholder_color": "#8FBF6A",
      "base_stats": { "max_hp": 140, "atk": 23, "def": 7, "speed": 88 },
      "growth": { "max_hp": 10, "atk": 1.6, "def": 0.4, "speed": 0.35 },
      "skill_profile": "ranger",
      "blurb": "A sapling scout of the grove."
    },
    {
      "hero_id": "wisp",
      "name": "Wisp",
      "faction": "UMBRAL",
      "role": "MAGE",
      "rarity": "COMMON",
      "is_fodder_only": true,
      "placeholder_color": "#7A6A8A",
      "base_stats": { "max_hp": 135, "atk": 25, "def": 6, "speed": 86 },
      "growth": { "max_hp": 10, "atk": 1.7, "def": 0.4, "speed": 0.3 },
      "skill_profile": "mage",
      "blurb": "A flicker of leftover dusk."
    }
  ]
}
```

Các field `ratio_overrides` không bắt buộc; nếu thiếu, StatCalculator dùng mặc định mục 10.2.

---

## `res://data/heroes/skills.json`

```json
{
  "profiles": {
    "tank":     { "basic": "Single-target front-first attack.", "ult": "Heavy single-target smash, coeff 2.2." },
    "warrior":  { "basic": "Single-target front-first attack.", "ult": "Heavy single-target smash, coeff 2.2." },
    "ranger":   { "basic": "Single-target front-first attack.", "ult": "Main target coeff 2.0 plus a 0.6 hit on the lowest-HP backliner." },
    "mage":     { "basic": "Single-target front-first attack.", "ult": "Hits the whole current row, coeff 1.4 each." },
    "healer":   { "basic": "Heal the lowest %HP ally, or attack if all are full.", "ult": "Heal the whole living team." },
    "assassin": { "basic": "Hits the lowest %HP enemy, ignores front-first.", "ult": "Same target rule, coeff 2.5." },
    "boss":     { "basic": "Single-target front-first attack.", "ult": "Hits the whole current row like a mage." },
    "basic":    { "basic": "Single-target front-first attack.", "ult": "Single-target, coeff 2.2." }
  }
}
```

Text chỉ để UI. Combat đọc `skill_profile` string, không parse mô tả.

---

## `res://data/items/gear.json`

`base_stats` là chỉ số ở rarity COMMON, 0 sao.

```json
{
  "gear": [
    { "gear_id": "iron_sword", "name": "Iron Sword", "slot": "weapon", "faction": null, "base_stats": { "atk": 20 } },
    { "gear_id": "dawn_blade", "name": "Dawn Blade", "slot": "weapon", "faction": "DAWNKIN", "base_stats": { "atk": 20 } },
    { "gear_id": "fang_axe", "name": "Fang Axe", "slot": "weapon", "faction": "IRONFANG", "base_stats": { "atk": 20 } },
    { "gear_id": "grove_bow", "name": "Grove Bow", "slot": "weapon", "faction": "SYLVAN", "base_stats": { "atk": 20 } },
    { "gear_id": "dusk_wand", "name": "Dusk Wand", "slot": "weapon", "faction": "UMBRAL", "base_stats": { "atk": 20 } },
    { "gear_id": "leather_helm", "name": "Leather Helm", "slot": "helm", "faction": null, "base_stats": { "max_hp": 80 } },
    { "gear_id": "dawn_helm", "name": "Dawn Helm", "slot": "helm", "faction": "DAWNKIN", "base_stats": { "max_hp": 80, "def": 2 } },
    { "gear_id": "hide_chest", "name": "Hide Chest", "slot": "chest", "faction": null, "base_stats": { "def": 12 } },
    { "gear_id": "ironfang_mail", "name": "Ironfang Mail", "slot": "chest", "faction": "IRONFANG", "base_stats": { "def": 12, "max_hp": 20 } },
    { "gear_id": "travel_boots", "name": "Travel Boots", "slot": "boots", "faction": null, "base_stats": { "speed": 8 } },
    { "gear_id": "sylvan_treads", "name": "Sylvan Treads", "slot": "boots", "faction": "SYLVAN", "base_stats": { "speed": 8, "atk": 3 } },
    { "gear_id": "umbral_greaves", "name": "Umbral Greaves", "slot": "boots", "faction": "UMBRAL", "base_stats": { "speed": 8 } }
  ]
}
```

Khi rơi đồ từ campaign (v1 đơn giản): RewardService có thể cho `gear_id` ngẫu nhiên trong list, rarity COMMON hoặc RARE (80/20) ở ải 1–8; ELITE bắt đầu xuất hiện từ ải 9 (10%). Hoặc ải first-clear lẻ cho 1 món cố định. **Cách tối thiểu để nghiệm thu M9–M13:** debug grant + first-clear ải 2 cho `iron_sword` COMMON 0★, ải 3 cho `leather_helm`, ải 4 `hide_chest`, ải 5 `travel_boots`. Ghi rule này trong `RewardService` (hoặc field `gear_reward` trên stage — nếu thêm field, được phép). Khuyến nghị thêm `"gear_reward": "iron_sword"` vào march_02, v.v. trong file stages nếu bạn muốn tường minh.

Bổ sung gợi ý (Codex hãy chèn vào đúng ải trong `campaign_stages.json`):

- march_02: `"gear_reward": { "gear_id": "iron_sword", "rarity": "COMMON", "stars": 0 }`
- march_03: `leather_helm` COMMON
- march_04: `hide_chest` COMMON
- march_06: `travel_boots` COMMON
- march_07: `dawn_blade` RARE

---

## `res://data/items/relics.json`

Một relic cho mỗi tướng chơi được (không cần fodder). `per_level_stats` là **cộng thêm mỗi cấp** (level 3 = 3×).

```json
{
  "relics": [
    { "hero_id": "kael",    "name": "Bulwark Sigil",   "per_level_stats": { "max_hp": 18, "def": 1.2 }, "ult_heal_pct_at": {} },
    { "hero_id": "serin",   "name": "Pale Quartz",     "per_level_stats": { "atk": 2.4 }, "ult_heal_pct_at": {} },
    { "hero_id": "nila",    "name": "Grove Heart",     "per_level_stats": { "atk": 1.4, "max_hp": 10 }, "ult_heal_pct_at": { "10": 0.04 } },
    { "hero_id": "ruga",    "name": "Blood Tusk",      "per_level_stats": { "atk": 2.2, "lifesteal": 0.004 }, "ult_heal_pct_at": {} },
    { "hero_id": "thorn",   "name": "Quiet Thorn",     "per_level_stats": { "atk": 2.0, "speed": 0.4 }, "ult_heal_pct_at": {} },
    { "hero_id": "morwen",  "name": "Violet Codex",    "per_level_stats": { "atk": 2.5 }, "ult_heal_pct_at": {} },
    { "hero_id": "solenne", "name": "Star Chalice",    "per_level_stats": { "atk": 1.8, "max_hp": 12 }, "ult_heal_pct_at": { "5": 0.03, "10": 0.06 } },
    { "hero_id": "drax",    "name": "Red Eclipse",     "per_level_stats": { "atk": 2.8, "crit_rate": 0.004 }, "ult_heal_pct_at": {} }
  ]
}
```

`ult_damage_mult` lấy từ `balance.relic.ult_damage_mult_at` theo mốc đã đạt (level >= 5 → 1.15, >= 10 → 1.30). BattleService nén vào `skill_mods`.

---

## `res://data/items/artifacts.json`

```json
{
  "artifacts": [
    {
      "artifact_id": "art_dawn_charm",
      "name": "Dawn Charm",
      "rarity": "RARE",
      "stat_mods": [{ "stat": "max_hp", "op": "add", "value": 80 }, { "stat": "def", "op": "add", "value": 6 }],
      "combat_mods": { "start_energy": 100 }
    },
    {
      "artifact_id": "art_iron_heart",
      "name": "Iron Heart",
      "rarity": "ELITE",
      "stat_mods": [{ "stat": "max_hp", "op": "add", "value": 150 }, { "stat": "atk", "op": "add", "value": 8 }],
      "combat_mods": { "damage_taken_mult": 0.95 }
    },
    {
      "artifact_id": "art_sylvan_leaf",
      "name": "Sylvan Leaf",
      "rarity": "ELITE",
      "stat_mods": [{ "stat": "atk", "op": "add", "value": 12 }, { "stat": "speed", "op": "add", "value": 6 }],
      "combat_mods": { "start_energy": 200 }
    },
    {
      "artifact_id": "art_void_lens",
      "name": "Void Lens",
      "rarity": "LEGENDARY",
      "stat_mods": [{ "stat": "atk", "op": "add", "value": 20 }, { "stat": "crit_rate", "op": "add", "value": 0.05 }],
      "combat_mods": { "start_energy": 300, "ult_damage_mult": 1.10 }
    }
  ]
}
```

Nguồn: first-clear ải 5 / 10 / 15 / 20 (field `artifact_reward` trên stage).

---

## `res://data/gacha/summon_pools.json`

```json
{
  "pool_id": "standard",
  "rates": [
    { "rarity": "COMMON", "weight": 5000 },
    { "rarity": "RARE", "weight": 3500 },
    { "rarity": "ELITE", "weight": 1200 },
    { "rarity": "LEGENDARY", "weight": 270 },
    { "rarity": "MYTHIC", "weight": 30 }
  ],
  "hero_ids_by_rarity": {
    "COMMON": ["squire", "pup", "sprout", "wisp"],
    "RARE": ["serin", "thorn"],
    "ELITE": ["kael", "nila", "ruga", "morwen"],
    "LEGENDARY": ["solenne", "drax"],
    "MYTHIC": ["solenne", "drax"]
  }
}
```

**Lưu ý MYTHIC:** v1 chưa có tướng rarity gốc MYTHIC. Khi roll trúng weight MYTHIC, lấy đều trong `hero_ids_by_rarity.MYTHIC` và **vẫn gắn rarity kết quả là MYTHIC** (bản sao cực quý — `add_from_summon` cộng copy cho Solenne/Drax). Không bịa tướng mới. Panel tỷ lệ vẫn hiện MYTHIC 0.30%.

Hoặc (được chấp nhận, chọn 1 và ghi trong README project): khi roll MYTHIC, drop xuống LEGENDARY hero nhưng flag `rarity: "MYTHIC"` chỉ để pity reset như legendary+ (tức MYTHIC reset cả hai pity). **Chọn phương án 1** (vẫn `rarity: "MYTHIC"` trên kết quả, hero_id là solenne/drax). `roster.add_from_summon` dùng **rarity gốc trong HeroData** để starting tier khi tạo mới, không dùng rarity roll nếu tướng đã tồn tại. Khi tạo mới Solenne từ roll MYTHIC, starting tier vẫn theo HeroData = ELITE (Legendary hero), không nhảy bậc.

---

## `res://data/stages/enemies.json`

`base_stats` là template cấp 1, trước khi `EnemyWaveFactory` scale.

```json
{
  "enemies": [
    { "enemy_id": "e_bandit_tank", "name": "Gate Thug", "faction": "DAWNKIN", "role": "TANK", "skill_profile": "tank",
      "base_stats": { "max_hp": 260, "atk": 22, "def": 16, "speed": 80, "crit_rate": 0.05, "crit_damage": 1.5, "energy_regen": 50 },
      "placeholder_color": "#5A5A6A" },
    { "enemy_id": "e_bandit_melee", "name": "Road Blade", "faction": "DAWNKIN", "role": "WARRIOR", "skill_profile": "warrior",
      "base_stats": { "max_hp": 200, "atk": 28, "def": 10, "speed": 88, "crit_rate": 0.05, "crit_damage": 1.5, "energy_regen": 50 },
      "placeholder_color": "#6A5A4A" },
    { "enemy_id": "e_bandit_ranged", "name": "Sling Hand", "faction": "DAWNKIN", "role": "RANGER", "skill_profile": "ranger",
      "base_stats": { "max_hp": 160, "atk": 30, "def": 8, "speed": 96, "crit_rate": 0.08, "crit_damage": 1.5, "energy_regen": 50 },
      "placeholder_color": "#4A6A5A" },
    { "enemy_id": "e_bandit_mage", "name": "Field Hexer", "faction": "UMBRAL", "role": "MAGE", "skill_profile": "mage",
      "base_stats": { "max_hp": 150, "atk": 32, "def": 7, "speed": 94, "crit_rate": 0.06, "crit_damage": 1.5, "energy_regen": 50 },
      "placeholder_color": "#4A3A6A" },
    { "enemy_id": "e_bandit_healer", "name": "Camp Medic", "faction": "SYLVAN", "role": "HEALER", "skill_profile": "healer",
      "base_stats": { "max_hp": 180, "atk": 24, "def": 9, "speed": 90, "crit_rate": 0.05, "crit_damage": 1.5, "energy_regen": 55 },
      "placeholder_color": "#3A5A4A" },
    { "enemy_id": "e_beast_tank", "name": "Bristlehide", "faction": "IRONFANG", "role": "TANK", "skill_profile": "tank",
      "base_stats": { "max_hp": 300, "atk": 24, "def": 18, "speed": 78, "crit_rate": 0.05, "crit_damage": 1.5, "energy_regen": 50 },
      "placeholder_color": "#7A4A2A" },
    { "enemy_id": "e_beast_warrior", "name": "Pack Raider", "faction": "IRONFANG", "role": "WARRIOR", "skill_profile": "warrior",
      "base_stats": { "max_hp": 220, "atk": 32, "def": 12, "speed": 90, "crit_rate": 0.07, "crit_damage": 1.6, "energy_regen": 50 },
      "placeholder_color": "#8A3A1A" },
    { "enemy_id": "e_beast_ranger", "name": "Howl Archer", "faction": "IRONFANG", "role": "RANGER", "skill_profile": "ranger",
      "base_stats": { "max_hp": 170, "atk": 33, "def": 9, "speed": 100, "crit_rate": 0.09, "crit_damage": 1.6, "energy_regen": 50 },
      "placeholder_color": "#6A3A2A" },
    { "enemy_id": "e_shade_mage", "name": "Veil Scribe", "faction": "UMBRAL", "role": "MAGE", "skill_profile": "mage",
      "base_stats": { "max_hp": 155, "atk": 36, "def": 7, "speed": 97, "crit_rate": 0.07, "crit_damage": 1.65, "energy_regen": 50 },
      "placeholder_color": "#3A2A5A" },
    { "enemy_id": "e_shade_assassin", "name": "Quiet Fang", "faction": "ABYSSAL", "role": "ASSASSIN", "skill_profile": "assassin",
      "base_stats": { "max_hp": 150, "atk": 36, "def": 8, "speed": 108, "crit_rate": 0.12, "crit_damage": 1.7, "energy_regen": 55 },
      "placeholder_color": "#4A1020" },
    { "enemy_id": "e_warden_boss", "name": "Warden", "faction": "ASTRAL", "role": "TANK", "skill_profile": "boss",
      "base_stats": { "max_hp": 400, "atk": 34, "def": 20, "speed": 84, "crit_rate": 0.08, "crit_damage": 1.6, "energy_regen": 60 },
      "placeholder_color": "#C0A040", "is_boss": true }
  ]
}
```

Ải 10 và 20 dùng `e_warden_boss` — factory nhân thêm `campaign.boss_hp_bias` / `boss_atk_bias` khi `is_boss` hoặc `enemy_id` bắt đầu bằng `e_warden`.

---

## `res://data/stages/campaign_stages.json`

```json
{
  "stages": [
  {
    "stage_id": "march_01",
    "index": 1,
    "name": "Pearl Gate",
    "recommended_power": 400,
    "enemy_ids": [
      "e_bandit_tank",
      "e_bandit_melee",
      "e_bandit_ranged",
      "e_bandit_ranged",
      "e_bandit_mage"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 500,
      "spirit_dust": 200,
      "summon_scroll": 1,
      "gem": 20
    },
    "repeat_rewards": {
      "gold": 120,
      "spirit_dust": 48
    },
    "difficulty_bias": 0.85
  },
  {
    "stage_id": "march_02",
    "index": 2,
    "name": "Lowland Road",
    "recommended_power": 472,
    "enemy_ids": [
      "e_bandit_tank",
      "e_bandit_melee",
      "e_bandit_ranged",
      "e_bandit_ranged",
      "e_bandit_mage"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 560,
      "spirit_dust": 220
    },
    "repeat_rewards": {
      "gold": 134,
      "spirit_dust": 52
    },
    "difficulty_bias": 0.92
  },
  {
    "stage_id": "march_03",
    "index": 3,
    "name": "Ash Farm",
    "recommended_power": 556,
    "enemy_ids": [
      "e_bandit_tank",
      "e_bandit_melee",
      "e_bandit_ranged",
      "e_bandit_ranged",
      "e_bandit_mage"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 627,
      "spirit_dust": 242,
      "forge_stone": 4
    },
    "repeat_rewards": {
      "gold": 150,
      "spirit_dust": 58
    },
    "difficulty_bias": 1.0
  },
  {
    "stage_id": "march_04",
    "index": 4,
    "name": "River Ford",
    "recommended_power": 657,
    "enemy_ids": [
      "e_bandit_tank",
      "e_bandit_melee",
      "e_bandit_ranged",
      "e_bandit_ranged",
      "e_bandit_mage"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 702,
      "spirit_dust": 266
    },
    "repeat_rewards": {
      "gold": 168,
      "spirit_dust": 63
    },
    "difficulty_bias": 1.0
  },
  {
    "stage_id": "march_05",
    "index": 5,
    "name": "Pine Watch",
    "recommended_power": 775,
    "enemy_ids": [
      "e_beast_tank",
      "e_beast_warrior",
      "e_bandit_ranged",
      "e_beast_ranger",
      "e_bandit_mage"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 786,
      "spirit_dust": 292,
      "summon_scroll": 1,
      "gem": 30
    },
    "repeat_rewards": {
      "gold": 188,
      "spirit_dust": 70,
      "forge_stone": 1
    },
    "difficulty_bias": 1.0,
    "artifact_reward": "art_dawn_charm"
  },
  {
    "stage_id": "march_06",
    "index": 6,
    "name": "Broken Bridge",
    "recommended_power": 915,
    "enemy_ids": [
      "e_beast_tank",
      "e_beast_warrior",
      "e_bandit_ranged",
      "e_beast_ranger",
      "e_bandit_mage"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 881,
      "spirit_dust": 322,
      "forge_stone": 5
    },
    "repeat_rewards": {
      "gold": 211,
      "spirit_dust": 77
    },
    "difficulty_bias": 1.0
  },
  {
    "stage_id": "march_07",
    "index": 7,
    "name": "Cinder Hamlet",
    "recommended_power": 1079,
    "enemy_ids": [
      "e_beast_tank",
      "e_beast_warrior",
      "e_bandit_ranged",
      "e_beast_ranger",
      "e_bandit_mage"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 986,
      "spirit_dust": 354
    },
    "repeat_rewards": {
      "gold": 236,
      "spirit_dust": 84
    },
    "difficulty_bias": 1.0
  },
  {
    "stage_id": "march_08",
    "index": 8,
    "name": "Thorn Pass",
    "recommended_power": 1274,
    "enemy_ids": [
      "e_beast_tank",
      "e_beast_warrior",
      "e_bandit_ranged",
      "e_beast_ranger",
      "e_bandit_mage"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 1105,
      "spirit_dust": 389,
      "relic_stone": 10
    },
    "repeat_rewards": {
      "gold": 265,
      "spirit_dust": 93
    },
    "difficulty_bias": 1.0
  },
  {
    "stage_id": "march_09",
    "index": 9,
    "name": "Echo Quarry",
    "recommended_power": 1503,
    "enemy_ids": [
      "e_beast_tank",
      "e_beast_warrior",
      "e_bandit_ranged",
      "e_beast_ranger",
      "e_bandit_mage"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 1237,
      "spirit_dust": 428,
      "forge_stone": 6
    },
    "repeat_rewards": {
      "gold": 296,
      "spirit_dust": 102
    },
    "difficulty_bias": 1.0
  },
  {
    "stage_id": "march_10",
    "index": 10,
    "name": "The First Warden",
    "recommended_power": 1774,
    "enemy_ids": [
      "e_warden_boss",
      "e_beast_warrior",
      "e_shade_assassin",
      "e_shade_mage",
      "e_bandit_healer"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 1386,
      "spirit_dust": 471,
      "summon_scroll": 1,
      "gem": 50
    },
    "repeat_rewards": {
      "gold": 332,
      "spirit_dust": 113,
      "forge_stone": 1
    },
    "difficulty_bias": 1.0,
    "artifact_reward": "art_iron_heart"
  },
  {
    "stage_id": "march_11",
    "index": 11,
    "name": "Night Orchard",
    "recommended_power": 2093,
    "enemy_ids": [
      "e_beast_tank",
      "e_shade_assassin",
      "e_shade_mage",
      "e_beast_ranger",
      "e_bandit_healer"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 1552,
      "spirit_dust": 518
    },
    "repeat_rewards": {
      "gold": 372,
      "spirit_dust": 124
    },
    "difficulty_bias": 1.0
  },
  {
    "stage_id": "march_12",
    "index": 12,
    "name": "Salt Flats",
    "recommended_power": 2470,
    "enemy_ids": [
      "e_beast_tank",
      "e_shade_assassin",
      "e_shade_mage",
      "e_beast_ranger",
      "e_bandit_healer"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 1739,
      "spirit_dust": 570,
      "forge_stone": 7,
      "relic_stone": 10
    },
    "repeat_rewards": {
      "gold": 417,
      "spirit_dust": 136
    },
    "difficulty_bias": 1.0
  },
  {
    "stage_id": "march_13",
    "index": 13,
    "name": "Red Gully",
    "recommended_power": 2915,
    "enemy_ids": [
      "e_beast_tank",
      "e_shade_assassin",
      "e_shade_mage",
      "e_beast_ranger",
      "e_bandit_healer"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 1947,
      "spirit_dust": 627
    },
    "repeat_rewards": {
      "gold": 467,
      "spirit_dust": 150
    },
    "difficulty_bias": 1.0
  },
  {
    "stage_id": "march_14",
    "index": 14,
    "name": "Hollow Keep",
    "recommended_power": 3439,
    "enemy_ids": [
      "e_beast_tank",
      "e_shade_assassin",
      "e_shade_mage",
      "e_beast_ranger",
      "e_bandit_healer"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 2181,
      "spirit_dust": 690
    },
    "repeat_rewards": {
      "gold": 523,
      "spirit_dust": 165
    },
    "difficulty_bias": 1.0
  },
  {
    "stage_id": "march_15",
    "index": 15,
    "name": "Moonwell Stair",
    "recommended_power": 4058,
    "enemy_ids": [
      "e_beast_tank",
      "e_beast_warrior",
      "e_shade_assassin",
      "e_shade_mage",
      "e_bandit_healer"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 2443,
      "spirit_dust": 759,
      "summon_scroll": 1,
      "gem": 50,
      "forge_stone": 8
    },
    "repeat_rewards": {
      "gold": 586,
      "spirit_dust": 182,
      "forge_stone": 1
    },
    "difficulty_bias": 1.0,
    "artifact_reward": "art_sylvan_leaf"
  },
  {
    "stage_id": "march_16",
    "index": 16,
    "name": "Iron Causeway",
    "recommended_power": 4789,
    "enemy_ids": [
      "e_beast_tank",
      "e_beast_warrior",
      "e_shade_assassin",
      "e_shade_mage",
      "e_bandit_healer"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 2736,
      "spirit_dust": 835,
      "relic_stone": 10
    },
    "repeat_rewards": {
      "gold": 656,
      "spirit_dust": 200
    },
    "difficulty_bias": 1.0
  },
  {
    "stage_id": "march_17",
    "index": 17,
    "name": "Dusk Harbor",
    "recommended_power": 5651,
    "enemy_ids": [
      "e_beast_tank",
      "e_beast_warrior",
      "e_shade_assassin",
      "e_shade_mage",
      "e_bandit_healer"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 3065,
      "spirit_dust": 918
    },
    "repeat_rewards": {
      "gold": 735,
      "spirit_dust": 220
    },
    "difficulty_bias": 1.0
  },
  {
    "stage_id": "march_18",
    "index": 18,
    "name": "Veil of Crows",
    "recommended_power": 6668,
    "enemy_ids": [
      "e_beast_tank",
      "e_beast_warrior",
      "e_shade_assassin",
      "e_shade_mage",
      "e_bandit_healer"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 3433,
      "spirit_dust": 1010,
      "forge_stone": 9
    },
    "repeat_rewards": {
      "gold": 823,
      "spirit_dust": 242
    },
    "difficulty_bias": 1.0
  },
  {
    "stage_id": "march_19",
    "index": 19,
    "name": "Aether Breach",
    "recommended_power": 7869,
    "enemy_ids": [
      "e_beast_tank",
      "e_beast_warrior",
      "e_shade_assassin",
      "e_shade_mage",
      "e_bandit_healer"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 3844,
      "spirit_dust": 1111
    },
    "repeat_rewards": {
      "gold": 922,
      "spirit_dust": 266
    },
    "difficulty_bias": 1.0
  },
  {
    "stage_id": "march_20",
    "index": 20,
    "name": "The Last Warden",
    "recommended_power": 9285,
    "enemy_ids": [
      "e_warden_boss",
      "e_beast_warrior",
      "e_shade_assassin",
      "e_shade_mage",
      "e_bandit_healer"
    ],
    "enemy_slots": [
      0,
      1,
      2,
      3,
      4
    ],
    "first_clear_rewards": {
      "gold": 4306,
      "spirit_dust": 1223,
      "summon_scroll": 2,
      "gem": 80
    },
    "repeat_rewards": {
      "gold": 1033,
      "spirit_dust": 293,
      "forge_stone": 1
    },
    "difficulty_bias": 1.0,
    "artifact_reward": "art_void_lens"
  }
]
}
```

(Mảng 20 ải ở trên đã có `artifact_reward` cho ải 5/10/15/20, `difficulty_bias` 0.85/0.92 cho ải 1–2 để starter thắng được.)

---

## `res://data/factions.json` (tùy chọn, phục vụ UI)

```json
{
  "factions": [
    { "id": "DAWNKIN", "name_vi": "Dawnkin", "color": "#4A6FA5", "beats": "IRONFANG" },
    { "id": "IRONFANG", "name_vi": "Ironfang", "color": "#C45C26", "beats": "SYLVAN" },
    { "id": "SYLVAN", "name_vi": "Sylvan", "color": "#3CB371", "beats": "UMBRAL" },
    { "id": "UMBRAL", "name_vi": "Umbral", "color": "#6B3FA0", "beats": "DAWNKIN" },
    { "id": "ASTRAL", "name_vi": "Astral", "color": "#F2D675", "beats": "ABYSSAL" },
    { "id": "ABYSSAL", "name_vi": "Abyssal", "color": "#8B1E3F", "beats": "ASTRAL" }
  ]
}
```

---

Hết data mẫu. `DataRegistry` phải load tất cả file trên lúc boot.
