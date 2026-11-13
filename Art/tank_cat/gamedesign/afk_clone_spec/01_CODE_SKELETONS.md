# Phụ lục 01 — Skeleton GDScript (copy được)

Toàn bộ identifier / comment: **tiếng Anh**. Codex copy vào đường dẫn ghi ở đầu mỗi khối, rồi **lấp thân hàm** đúng `AFK_CLONE_SPEC.md`. Những chỗ đã viết công thức sẵn thì **giữ nguyên**, đừng viết lại khác.

Không compile được nếu thiếu class khác — làm theo milestone, có thể tạm stub. `class_name` giữ đúng.

---

## A. Enums và tiện ích

### `res://core/enums/GameEnums.gd`

```gdscript
class_name GameEnums
extends RefCounted

enum Faction { DAWNKIN, IRONFANG, SYLVAN, UMBRAL, ASTRAL, ABYSSAL }
enum HeroRarity { COMMON, RARE, ELITE, LEGENDARY, MYTHIC }
enum AscensionTier {
    RECRUIT, VETERAN, ELITE, ELITE_PLUS, LEGENDARY, LEGENDARY_PLUS,
    MYTHIC, MYTHIC_PLUS, TRANSCENDENT, TRANSCENDENT_1, TRANSCENDENT_2, TRANSCENDENT_3
}
enum HeroRole { TANK, WARRIOR, RANGER, MAGE, HEALER, ASSASSIN }
enum StatId {
    MAX_HP, ATK, DEF, SPEED, CRIT_RATE, CRIT_DAMAGE, HASTE,
    ENERGY_REGEN, LIFESTEAL, ACCURACY, RESIST
}

const STAT_KEYS := [
    "max_hp", "atk", "def", "speed", "crit_rate", "crit_damage",
    "haste", "energy_regen", "lifesteal", "accuracy", "resist"
]

const GEAR_SLOTS := ["weapon", "helm", "chest", "boots"]

const FACTION_KEYS := ["DAWNKIN", "IRONFANG", "SYLVAN", "UMBRAL", "ASTRAL", "ABYSSAL"]
const RARITY_KEYS := ["COMMON", "RARE", "ELITE", "LEGENDARY", "MYTHIC"]
const TIER_KEYS := [
    "RECRUIT", "VETERAN", "ELITE", "ELITE_PLUS", "LEGENDARY", "LEGENDARY_PLUS",
    "MYTHIC", "MYTHIC_PLUS", "TRANSCENDENT", "TRANSCENDENT_1", "TRANSCENDENT_2", "TRANSCENDENT_3"
]
const ROLE_KEYS := ["TANK", "WARRIOR", "RANGER", "MAGE", "HEALER", "ASSASSIN"]

static func stat_key(id: StatId) -> String:
    return STAT_KEYS[int(id)]

static func faction_to_string(v: Faction) -> String:
    return FACTION_KEYS[int(v)]

static func faction_from_string(s: String) -> Faction:
    var i := FACTION_KEYS.find(s)
    assert(i >= 0, "Unknown faction: %s" % s)
    return i as Faction

static func rarity_to_string(v: HeroRarity) -> String:
    return RARITY_KEYS[int(v)]

static func rarity_from_string(s: String) -> HeroRarity:
    var i := RARITY_KEYS.find(s)
    assert(i >= 0, "Unknown rarity: %s" % s)
    return i as HeroRarity

static func tier_to_string(v: AscensionTier) -> String:
    return TIER_KEYS[int(v)]

static func tier_from_string(s: String) -> AscensionTier:
    var i := TIER_KEYS.find(s)
    assert(i >= 0, "Unknown tier: %s" % s)
    return i as AscensionTier

static func role_to_string(v: HeroRole) -> String:
    return ROLE_KEYS[int(v)]

static func role_from_string(s: String) -> HeroRole:
    var i := ROLE_KEYS.find(s)
    assert(i >= 0, "Unknown role: %s" % s)
    return i as HeroRole

static func is_front_slot(slot: int) -> bool:
    return slot == 0 or slot == 1

static func starting_tier_for_rarity(rarity: HeroRarity) -> AscensionTier:
    # TUNABLE mapping is also in balance.json; keep this in sync or prefer balance if present.
    match rarity:
        HeroRarity.COMMON, HeroRarity.RARE:
            return AscensionTier.RECRUIT
        HeroRarity.ELITE:
            return AscensionTier.VETERAN
        HeroRarity.LEGENDARY:
            return AscensionTier.ELITE
        HeroRarity.MYTHIC:
            return AscensionTier.ELITE_PLUS
        _:
            return AscensionTier.RECRUIT

static func role_to_skill_profile(role: HeroRole) -> String:
    match role:
        HeroRole.TANK: return "tank"
        HeroRole.WARRIOR: return "warrior"
        HeroRole.RANGER: return "ranger"
        HeroRole.MAGE: return "mage"
        HeroRole.HEALER: return "healer"
        HeroRole.ASSASSIN: return "assassin"
        _:
            return "basic"
```

### `res://core/util/TimeUtil.gd`

```gdscript
class_name TimeUtil
extends RefCounted

static func now_unix() -> int:
    return int(Time.get_unix_time_from_system())

static func format_hms(seconds: int) -> String:
    seconds = max(0, seconds)
    var h := seconds / 3600
    var m := (seconds % 3600) / 60
    var s := seconds % 60
    return "%02d:%02d:%02d" % [h, m, s]

# Display a unix timestamp in the device local timezone.
static func format_local(unix: int) -> String:
    return Time.get_datetime_string_from_unix_time(unix, true)
```

### `res://core/util/MathUtil.gd`

```gdscript
class_name MathUtil
extends RefCounted

static func safe_div(n: float, d: float, fallback: float = 0.0) -> float:
    if d == 0.0:
        return fallback
    return n / d

static func clamp01(v: float) -> float:
    return clampf(v, 0.0, 1.0)

static func floor_int(v: float) -> int:
    return int(floor(v))

static func format_compact(n: int) -> String:
    if n < 10_000:
        return str(n)
    if n < 1_000_000:
        return "%.1fK" % (n / 1000.0)
    return "%.1fM" % (n / 1_000_000.0)
```

### `res://core/util/IdUtil.gd`

```gdscript
class_name IdUtil
extends RefCounted

static var _seq: int = 0

static func make(prefix: String) -> String:
    _seq += 1
    var rng := RandomNumberGenerator.new()
    rng.randomize()
    return "%s_%d_%d_%d" % [prefix, TimeUtil.now_unix(), _seq, rng.randi_range(100, 999)]
```

---

## B. Autoload stubs

### `res://autoload/EventBus.gd`

```gdscript
extends Node

signal currencies_changed(wallet_snapshot: Dictionary)
signal hero_added(hero_uid: String)
signal hero_removed(hero_uid: String)
signal hero_leveled_up(hero_uid: String, new_level: int)
signal hero_ascended(hero_uid: String, new_tier: int)
signal hero_stats_dirty(hero_uid: String)
signal gear_changed(hero_uid: String)
signal nexus_changed
signal idle_accrued(pending: Dictionary)
signal idle_claimed(rewards: Dictionary)
signal campaign_progressed(stage_id: String)
signal battle_started(request_id: String)
signal battle_tick(snapshot: Dictionary)
signal battle_finished(result)
signal rewards_granted(rewards: Dictionary)
signal toast_requested(message: String, kind: String)
signal save_completed
signal screen_changed(screen_id: String)
```

### `res://autoload/DataRegistry.gd`

```gdscript
extends Node

var _balance: Dictionary = {}
var _heroes: Dictionary = {}       # hero_id -> Dictionary
var _gear: Dictionary = {}
var _relics: Dictionary = {}
var _artifacts: Dictionary = {}
var _stages: Dictionary = {}       # stage_id -> Dictionary
var _stages_by_index: Dictionary = {}
var _enemies: Dictionary = {}
var _gacha_pool: Dictionary = {}
var boot_ok: bool = false
var boot_error: String = ""

func _ready() -> void:
    var err := _load_all()
    if err != "":
        boot_ok = false
        boot_error = err
        push_error("DataRegistry boot failed: %s" % err)
    else:
        boot_ok = true

func _load_all() -> String:
    # Load every JSON listed in the spec. On missing file or missing required key, return a clear message.
    # Implementation: helper _load_json(path) -> Dictionary / error string.
    return "not_implemented"

func get_balance() -> Dictionary:
    return _balance

func get_hero_data(hero_id: String) -> Dictionary:
    return _heroes.get(hero_id, {})

func get_all_hero_ids() -> Array:
    return _heroes.keys()

func get_gear_data(gear_id: String) -> Dictionary:
    return _gear.get(gear_id, {})

func get_relic_data(hero_id: String) -> Dictionary:
    return _relics.get(hero_id, {})

func get_artifact_data(artifact_id: String) -> Dictionary:
    return _artifacts.get(artifact_id, {})

func get_stage(stage_id: String) -> Dictionary:
    return _stages.get(stage_id, {})

func get_stage_by_index(index: int) -> Dictionary:
    return _stages_by_index.get(index, {})

func get_enemy_template(enemy_id: String) -> Dictionary:
    return _enemies.get(enemy_id, {})

func get_gacha_pool() -> Dictionary:
    return _gacha_pool

func get_balance_path(path_dot: String, default_value: Variant = null) -> Variant:
    # "idle.gold_base_per_min" -> nested lookup
    var node: Variant = _balance
    for part in path_dot.split("."):
        if typeof(node) != TYPE_DICTIONARY or not node.has(part):
            return default_value
        node = node[part]
    return node
```

### `res://autoload/AudioDirector.gd`

```gdscript
extends Node

func play_sfx(_id: String) -> void:
    # v1: no-op if no file. Later: map id -> AudioStream.
    pass

func play_bgm(_id: String) -> void:
    pass
```

### `res://autoload/SaveManager.gd`

```gdscript
extends Node

const SAVE_PATH := "user://save.json"
const TMP_PATH := "user://save.json.tmp"
const SCHEMA_VERSION := 1

func has_save() -> bool:
    return FileAccess.file_exists(SAVE_PATH)

func load_raw() -> Dictionary:
    # If missing: return {}. If parse error: copy to user://save.broken.<unix>.json and return {}.
    return {}

func save_raw(data: Dictionary) -> Error:
    # Write TMP_PATH then rename to SAVE_PATH. Set schema_version and saved_at_unix here or in GameState.
    return ERR_UNCONFIGURED

func delete_save() -> Error:
    return ERR_UNCONFIGURED

func migrate(data: Dictionary) -> Dictionary:
    # Fill missing keys with defaults. Never drop unknown keys silently if you can help it.
    if int(data.get("schema_version", 1)) > SCHEMA_VERSION:
        push_error("Save schema is newer than this build.")
    return data
```

### `res://autoload/GameState.gd` (khung)

```gdscript
extends Node

var wallet  # CurrencyWallet
var roster  # HeroRoster
var inventory  # Inventory
var nexus  # ResonanceNexus
var campaign  # CampaignSystem
var idle  # IdleRewardSystem
var gacha  # GachaSystem
var battle_service  # BattleService
var calculator  # StatCalculator
var profile: Dictionary = {}
var team_slots: Array[String] = ["", "", "", "", ""]

var _save_dirty: bool = false
var _save_timer: Timer

func _ready() -> void:
    # 1) Wait / assume DataRegistry already booted (autoload order).
    # 2) If !DataRegistry.boot_ok: do not start a profile; AppRoot shows BootError.
    # 3) Load or create profile.
    # 4) idle.peek_rewards → EventBus.idle_accrued
    # 5) Setup debounce timer 0.4s
    pass

func _notification(what: int) -> void:
    if what == NOTIFICATION_WM_CLOSE_REQUEST or what == NOTIFICATION_APPLICATION_PAUSED:
        _flush_save()

func _create_new_profile() -> void:
    # starting_wallet, starting_roster from balance.json, last_accrual_unix = now
    pass

func grant_rewards(rewards: Dictionary, source: String) -> void:
    pass

func can_afford(cost: Dictionary) -> bool:
    return wallet.can_afford(cost)

func try_spend(cost: Dictionary, reason: String) -> bool:
    var ok: bool = wallet.try_spend(cost, reason)
    if ok:
        mark_dirty_and_save()
    return ok

func get_hero_final_stats(hero_uid: String) -> FinalStats:
    return FinalStats.new()

func build_team_snapshots(slot_uids: Array[String]) -> Array:
    return []

func try_summon(count: int) -> Dictionary:
    return {"ok": false, "error": "not_implemented"}

func to_dict() -> Dictionary:
    return {}

func from_dict(data: Dictionary) -> void:
    pass

func mark_dirty_and_save() -> void:
    _save_dirty = true
    if _save_timer:
        _save_timer.start(0.4)

func _flush_save() -> void:
    if not _save_dirty:
        return
    _save_dirty = false
    # SaveManager.save_raw(to_dict())
    # EventBus.save_completed.emit()
```

---

## C. Ví, thống kê, tướng

### `res://core/inventory/CurrencyWallet.gd`

```gdscript
class_name CurrencyWallet
extends RefCounted

const KNOWN := ["gold", "spirit_dust", "soul_essence", "forge_stone", "gem", "summon_scroll", "relic_stone"]
const CAP := 999_999_999  # TUNABLE via balance.json currencies.cap if present

var _amounts: Dictionary = {}

func _init() -> void:
    for k in KNOWN:
        _amounts[k] = 0

func get_amount(currency_id: String) -> int:
    return int(_amounts.get(currency_id, 0))

func add(currency_id: String, amount: int, source: String) -> void:
    if amount < 0:
        push_error("CurrencyWallet.add negative amount (%s, %s)" % [currency_id, source])
        return
    if amount == 0:
        return
    if not _amounts.has(currency_id):
        push_error("Unknown currency %s" % currency_id)
        return
    _amounts[currency_id] = min(CAP, int(_amounts[currency_id]) + amount)
    EventBus.currencies_changed.emit(to_dict())

func can_spend(currency_id: String, amount: int) -> bool:
    return amount >= 0 and get_amount(currency_id) >= amount

func can_afford(cost: Dictionary) -> bool:
    for k in cost.keys():
        if get_amount(str(k)) < int(cost[k]):
            return false
    return true

func spend(currency_id: String, amount: int, reason: String) -> bool:
    if amount < 0:
        push_error("CurrencyWallet.spend negative (%s)" % reason)
        return false
    if not can_spend(currency_id, amount):
        return false
    _amounts[currency_id] = int(_amounts[currency_id]) - amount
    EventBus.currencies_changed.emit(to_dict())
    return true

func try_spend(cost: Dictionary, reason: String) -> bool:
    if not can_afford(cost):
        return false
    for k in cost.keys():
        _amounts[str(k)] = get_amount(str(k)) - int(cost[k])
    EventBus.currencies_changed.emit(to_dict())
    return true

func to_dict() -> Dictionary:
    return _amounts.duplicate()

func from_dict(data: Dictionary) -> void:
    for k in KNOWN:
        _amounts[k] = int(data.get(k, 0))
```

Lưu ý: Autoload `EventBus` chỉ gọi được khi chạy trong cây scene. Unit test thuần có thể tạm thời không emit (bọc `Engine.is_editor_hint` không đủ). Cách sạch: thêm `var bus: Object = null` và `func _emit(): if bus: bus.currencies_changed.emit(...)` — hoặc trong test runner add EventBus. Ưu tiên: test wallet trong scene runner đã có Autoload.

### `res://core/stats/FinalStats.gd`

```gdscript
class_name FinalStats
extends RefCounted

var values: Dictionary = {}

func _init() -> void:
    for k in GameEnums.STAT_KEYS:
        values[k] = 0.0

func get_stat(stat: String) -> float:
    return float(values.get(stat, 0.0))

func set_stat(stat: String, v: float) -> void:
    values[stat] = v

func to_dict() -> Dictionary:
    return values.duplicate()

static func from_dict(d: Dictionary) -> FinalStats:
    var s := FinalStats.new()
    for k in GameEnums.STAT_KEYS:
        s.values[k] = float(d.get(k, 0.0))
    return s

func duplicate_stats() -> FinalStats:
    return FinalStats.from_dict(to_dict())
```

### `res://core/stats/StatModifier.gd`

```gdscript
class_name StatModifier
extends RefCounted

var stat: String = "atk"
var op: String = "add"  # "add" | "mult"  (mult is a fraction to add into pct_total, e.g. 0.1 = +10%)
var value: float = 0.0
var source: String = ""
```

### `res://core/stats/FactionMatrix.gd`

```gdscript
class_name FactionMatrix
extends RefCounted

# Wheel: DAWNKIN > IRONFANG > SYLVAN > UMBRAL > DAWNKIN
# ASTRAL <-> ABYSSAL advantage both ways. They are neutral vs the wheel.

const WHEEL := ["DAWNKIN", "IRONFANG", "SYLVAN", "UMBRAL"]

var advantage: float = 1.15
var disadvantage: float = 0.85

func configure(balance: Dictionary) -> void:
    var f: Dictionary = balance.get("faction", {})
    advantage = float(f.get("advantage_damage_dealt", 1.15))
    disadvantage = float(f.get("disadvantage_damage_dealt", 0.85))

func get_damage_multiplier(attacker: String, defender: String) -> float:
    if attacker == "ASTRAL" and defender == "ABYSSAL":
        return advantage
    if attacker == "ABYSSAL" and defender == "ASTRAL":
        return advantage
    var ai := WHEEL.find(attacker)
    var di := WHEEL.find(defender)
    if ai < 0 or di < 0:
        return 1.0
    if di == (ai + 1) % WHEEL.size():
        return advantage
    if ai == (di + 1) % WHEEL.size():
        return disadvantage
    return 1.0
```

### `res://core/stats/StatCalculator.gd`

```gdscript
class_name StatCalculator
extends RefCounted

func compute(hero: HeroInstance, ctx: Dictionary) -> FinalStats:
    var data_reg = ctx["data"]
    var nexus = ctx["nexus"]
    var inv = ctx["inventory"]
    var hero_data: Dictionary = data_reg.get_hero_data(hero.hero_id)
    var level: int = nexus.get_effective_level(hero)
    var acc: Dictionary = _base_with_level_tier(hero_data, level, hero.tier)
    # acc[stat] = {"add": float, "pct": float} or split two dicts — pick one and keep it.
    _apply_gear(hero, inv, acc)
    _apply_relic(hero, data_reg, acc)
    _apply_artifact(hero, inv, acc)
    _apply_furniture(hero, acc)
    _apply_engraving(hero, acc)
    return _finalize(acc)

func compute_power(stats: FinalStats, hero: HeroInstance) -> int:
    var p: float = 0.0
    p += stats.get_stat("max_hp") * 0.15
    p += stats.get_stat("atk") * 1.6
    p += stats.get_stat("def") * 1.2
    p += stats.get_stat("speed") * 2.0
    p += stats.get_stat("crit_rate") * 400.0
    p += (stats.get_stat("crit_damage") - 1.5) * 200.0
    p += hero.relic_level * 25
    return int(floor(p))

func _base_with_level_tier(data: Dictionary, level: int, tier: int) -> Dictionary:
    # flat = base + growth * (level - 1); after_tier = flat * tier_mult
    # Also fill default ratio stats (crit_rate 0.05, crit_damage 1.50, energy_regen 50, ...)
    # then allow hero JSON to override those ratios (they do not get tier-mult unless data says so).
    return {}

func _apply_gear(hero: HeroInstance, inv, acc: Dictionary) -> void:
    pass

func _apply_relic(hero: HeroInstance, data, acc: Dictionary) -> void:
    pass

func _apply_artifact(hero: HeroInstance, inv, acc: Dictionary) -> void:
    pass

func _apply_furniture(_hero: HeroInstance, _acc: Dictionary) -> void:
    # Phase 2 no-op
    pass

func _apply_engraving(_hero: HeroInstance, _acc: Dictionary) -> void:
    # Phase 2 no-op
    pass

func _finalize(acc: Dictionary) -> FinalStats:
    # raw = (add) * (1 + pct); clamp; int for max_hp/atk/def/speed
    return FinalStats.new()

func _tier_multiplier(tier: int, balance: Dictionary) -> float:
    var key := GameEnums.tier_to_string(tier as GameEnums.AscensionTier)
    var table: Dictionary = balance.get("ascension", {}).get("tier_multipliers", {})
    return float(table.get(key, 1.0))
```

### `res://core/hero/HeroInstance.gd`

```gdscript
class_name HeroInstance
extends RefCounted

var uid: String = ""
var hero_id: String = ""
var level: int = 1
var xp: int = 0
var tier: int = GameEnums.AscensionTier.RECRUIT
var stored_copies: int = 0
var equipped_gear: Dictionary = {}  # slot -> gear_uid
var relic_level: int = 0
var artifact_uid: String = ""
var locked: bool = false
var in_nexus_slot: int = -1
var is_nexus_anchor: bool = false

var _level_caps: Dictionary = {}  # injected from balance

func configure_caps(caps: Dictionary) -> void:
    _level_caps = caps

func get_level_cap() -> int:
    var key := GameEnums.tier_to_string(tier as GameEnums.AscensionTier)
    return int(_level_caps.get(key, 20))

func is_at_cap() -> bool:
    return level >= get_level_cap()

func to_dict() -> Dictionary:
    return {
        "uid": uid,
        "hero_id": hero_id,
        "level": level,
        "xp": xp,
        "tier": GameEnums.tier_to_string(tier as GameEnums.AscensionTier),
        "stored_copies": stored_copies,
        "equipped_gear": equipped_gear.duplicate(),
        "relic_level": relic_level,
        "artifact_uid": artifact_uid,
        "locked": locked,
        "in_nexus_slot": in_nexus_slot,
    }

static func from_dict(data: Dictionary) -> HeroInstance:
    var h := HeroInstance.new()
    h.uid = str(data.get("uid", ""))
    h.hero_id = str(data.get("hero_id", ""))
    h.level = int(data.get("level", 1))
    h.xp = int(data.get("xp", 0))
    h.tier = GameEnums.tier_from_string(str(data.get("tier", "RECRUIT")))
    h.stored_copies = int(data.get("stored_copies", 0))
    h.equipped_gear = data.get("equipped_gear", {}).duplicate()
    h.relic_level = int(data.get("relic_level", 0))
    h.artifact_uid = str(data.get("artifact_uid", ""))
    h.locked = bool(data.get("locked", false))
    h.in_nexus_slot = int(data.get("in_nexus_slot", -1))
    return h
```

### `res://core/hero/HeroRoster.gd`

```gdscript
class_name HeroRoster
extends RefCounted

var heroes: Dictionary = {}  # uid -> HeroInstance

func get_hero(uid: String) -> HeroInstance:
    return heroes.get(uid, null)

func all_heroes() -> Array:
    return heroes.values()

func find_primary(hero_id: String) -> HeroInstance:
    # First instance with this hero_id (stable: oldest uid / insertion).
    return null

func add_from_summon(hero_id: String, rarity: int, data_reg) -> Dictionary:
    # If primary exists: stored_copies += 1, return { "uid": ..., "new": false }
    # Else: create instance at starting tier for rarity, return { "uid": ..., "new": true }
    # Emit EventBus.hero_added on new instance.
    return {}

func remove_hero(uid: String) -> bool:
    return false

func to_dict() -> Dictionary:
    var arr: Array = []
    for h in heroes.values():
        arr.append(h.to_dict())
    return {"heroes": arr}

func from_dict(data: Dictionary, caps: Dictionary) -> void:
    heroes.clear()
    for d in data.get("heroes", data if data is Array else []):
        # accept either {heroes:[...]} or a raw array from the top-level save "roster" key
        pass
```

Top-level save dùng `"roster": [ ... ]` (mảng). `from_dict` phải nhận mảng hoặc `{heroes:[]}`.

### `res://core/hero/LevelUpService.gd`

```gdscript
class_name LevelUpService
extends RefCounted

var gold_base: float = 80
var gold_exp: float = 1.45
var dust_base: float = 40
var dust_exp: float = 1.35

func configure(balance: Dictionary) -> void:
    var b: Dictionary = balance.get("level_up", {})
    gold_base = float(b.get("gold_base", 80))
    gold_exp = float(b.get("gold_exp", 1.45))
    dust_base = float(b.get("dust_base", 40))
    dust_exp = float(b.get("dust_exp", 1.35))

func cost_for(level: int) -> Dictionary:
    # Cost to go from `level` to `level + 1`
    var gold := int(floor(gold_base * pow(float(level), gold_exp)))
    var dust := int(floor(dust_base * pow(float(level), dust_exp)))
    return {"gold": gold, "spirit_dust": dust}

func preview_multi(hero: HeroInstance, steps: int, wallet: CurrencyWallet, nexus: ResonanceNexus) -> Dictionary:
    return {}

func try_level_up(hero: HeroInstance, steps: int, wallet: CurrencyWallet, nexus: ResonanceNexus) -> Dictionary:
    return {"ok": false, "levels_gained": 0, "spent": {}, "error": "not_implemented"}
```

### `res://core/hero/AscensionService.gd` / `RecycleService.gd`

```gdscript
class_name AscensionService
extends RefCounted

func configure(balance: Dictionary) -> void:
    pass

func next_tier(tier: int) -> int:
    if tier >= GameEnums.AscensionTier.TRANSCENDENT_3:
        return -1
    return tier + 1

func requirement_for(from_tier: int) -> Dictionary:
    # Read balance.ascension.requirements[TIER_KEY]
    return {}

func get_level_cap(tier: int, balance: Dictionary) -> int:
    var key := GameEnums.tier_to_string(tier as GameEnums.AscensionTier)
    return int(balance.get("ascension", {}).get("level_caps", {}).get(key, 20))

func can_ascend(hero: HeroInstance, offered_uids: Array[String], roster: HeroRoster) -> Dictionary:
    return {"ok": false, "missing": [], "will_consume": [], "error": "not_implemented"}

func try_ascend(hero: HeroInstance, offered_uids: Array[String], roster: HeroRoster) -> Dictionary:
    return {"ok": false}
```

```gdscript
class_name RecycleService
extends RefCounted

func configure(balance: Dictionary) -> void:
    pass

func preview(hero: HeroInstance, include_copies: bool) -> Dictionary:
    return {"soul_essence": 0}

func try_recycle_copies(hero: HeroInstance, count: int, wallet: CurrencyWallet) -> Dictionary:
    return {"ok": false}

func try_recycle_hero(hero: HeroInstance, roster: HeroRoster, inventory, wallet: CurrencyWallet, nexus) -> Dictionary:
    return {"ok": false}
```

---


## D. Nexus, đồ, gacha, idle, campaign

### `res://core/nexus/ResonanceNexus.gd`

```gdscript
class_name ResonanceNexus
extends RefCounted

var unlocked_slots: int = 3
var slot_uids: Array[String] = ["", "", ""]
var _anchors: Array = []  # HeroInstance, size up to 5
var _nexus_level: int = 0

func refresh_anchors(roster: HeroRoster) -> void:
    # Sort roster by level desc, then tier, rarity (need data_reg or store rarity on instance), then uid.
    # Take 5. nexus_level = min of their levels if 5 exist, else 0.
    # If a slot uid became an anchor, clear that slot.
    # EventBus.nexus_changed.emit()
    pass

func get_anchors() -> Array:
    return _anchors.duplicate()

func get_nexus_level() -> int:
    return _nexus_level

func is_anchor(hero_uid: String) -> bool:
    for a in _anchors:
        if a.uid == hero_uid:
            return true
    return false

func is_follower(hero_uid: String) -> bool:
    return hero_uid in slot_uids and hero_uid != ""

func get_effective_level(hero: HeroInstance) -> int:
    if is_follower(hero.uid) and _nexus_level > 0:
        return min(_nexus_level, hero.get_level_cap())
    return hero.level

func can_place(hero: HeroInstance, slot_index: int) -> Dictionary:
    return {"ok": false, "error": "not_implemented"}

func try_place(hero: HeroInstance, slot_index: int) -> Dictionary:
    return {"ok": false}

func try_remove(slot_index: int) -> Dictionary:
    return {"ok": false}

func update_unlocks(highest_cleared_stage: int) -> void:
    # Table from balance.nexus.slot_unlocks: [{"slot": 4, "clear": 4}, ...]
    # Grow slot_uids with "" when unlocking.
    pass

func to_dict() -> Dictionary:
    return {"unlocked_slots": unlocked_slots, "slot_uids": slot_uids.duplicate()}

func from_dict(data: Dictionary) -> void:
    unlocked_slots = int(data.get("unlocked_slots", 3))
    slot_uids.clear()
    for u in data.get("slot_uids", []):
        slot_uids.append(str(u))
    while slot_uids.size() < unlocked_slots:
        slot_uids.append("")
```

### Gear / Inventory (rút gọn API, implement đủ field)

```gdscript
# res://core/inventory/GearInstance.gd
class_name GearInstance
extends RefCounted

var uid: String
var gear_id: String
var rarity: int = GameEnums.HeroRarity.COMMON
var stars: int = 0
var equipped_by: String = ""

func to_dict() -> Dictionary:
    return {
        "uid": uid, "gear_id": gear_id,
        "rarity": GameEnums.rarity_to_string(rarity as GameEnums.HeroRarity),
        "stars": stars, "equipped_by": equipped_by
    }

static func from_dict(data: Dictionary) -> GearInstance:
    var g := GearInstance.new()
    g.uid = str(data.get("uid", ""))
    g.gear_id = str(data.get("gear_id", ""))
    g.rarity = GameEnums.rarity_from_string(str(data.get("rarity", "COMMON")))
    g.stars = int(data.get("stars", 0))
    g.equipped_by = str(data.get("equipped_by", ""))
    return g


# res://core/inventory/Inventory.gd
class_name Inventory
extends RefCounted

var gear: Dictionary = {}         # uid -> GearInstance
var artifacts: Dictionary = {}    # uid -> Dictionary {uid, artifact_id, equipped_by}

func add_gear(inst: GearInstance) -> void:
    gear[inst.uid] = inst

func get_gear(uid: String) -> GearInstance:
    return gear.get(uid, null)

func list_gear_for_slot(slot: String) -> Array:
    return []

func to_dict() -> Dictionary:
    return {"gear": [], "artifacts": []}

func from_dict(data: Dictionary) -> void:
    pass


# res://core/inventory/GearEnhanceService.gd
class_name GearEnhanceService
extends RefCounted

func configure(balance: Dictionary) -> void:
    pass

func cost_for(gear: GearInstance) -> Dictionary:
    return {"forge_stone": 0, "gold": 0}

func try_enhance(gear: GearInstance, wallet: CurrencyWallet) -> Dictionary:
    return {"ok": false}

func compute_gear_stats(gear: GearInstance, hero_faction: String, data_reg) -> Dictionary:
    # stat = floor(base * rarity_mult * star_mult * faction_mult)
    # star_mult = 1.0 + 0.12 * stars
    return {}

func try_equip(hero: HeroInstance, gear: GearInstance, inventory: Inventory) -> Dictionary:
    return {"ok": false}

func try_unequip(hero: HeroInstance, slot: String, inventory: Inventory) -> Dictionary:
    return {"ok": false}
```

### `res://core/gacha/GachaSystem.gd`

```gdscript
class_name GachaSystem
extends RefCounted

var pity_elite: int = 0
var pity_legendary: int = 0
var rng: RandomNumberGenerator = RandomNumberGenerator.new()
var _pool: Dictionary = {}
var _pity_elite_threshold: int = 10
var _pity_legendary_threshold: int = 40

func configure(pool: Dictionary, balance: Dictionary, p_rng: RandomNumberGenerator) -> void:
    _pool = pool
    rng = p_rng
    var g: Dictionary = balance.get("gacha", {})
    _pity_elite_threshold = int(g.get("pity_elite", 10))
    _pity_legendary_threshold = int(g.get("pity_legendary", 40))

func summon_one() -> Dictionary:
    return {"hero_id": "", "rarity": "COMMON", "pity_triggered": ""}

func summon_ten() -> Array:
    var results: Array = []
    for i in 10:
        results.append(summon_one())
    # If none >= ELITE, replace the 10th with a forced ELITE+ roll and fix pity counters.
    return results

func preview_rates() -> Dictionary:
    return {}

func to_dict() -> Dictionary:
    return {"pity_elite": pity_elite, "pity_legendary": pity_legendary, "rng_state": rng.state}

func from_dict(data: Dictionary) -> void:
    pity_elite = int(data.get("pity_elite", 0))
    pity_legendary = int(data.get("pity_legendary", 0))
    if data.has("rng_state"):
        rng.state = int(data["rng_state"])
```

Thuật toán `summon_one` (bắt buộc đúng thứ tự pity ở mục 11.3 của spec).

### `res://core/idle/IdleRewardSystem.gd`

```gdscript
class_name IdleRewardSystem
extends RefCounted

var last_accrual_unix: int = 0
var _balance: Dictionary = {}
var _campaign  # CampaignSystem

func configure(balance: Dictionary, campaign) -> void:
    _balance = balance
    _campaign = campaign

func get_effective_elapsed(now_unix: int) -> int:
    var cap := int(_balance.get("idle", {}).get("cap_seconds", 43200))
    if now_unix < last_accrual_unix:
        return 0
    return min(now_unix - last_accrual_unix, cap)

func peek_rewards(now_unix: int) -> Dictionary:
    var idle: Dictionary = _balance.get("idle", {})
    var cap := int(idle.get("cap_seconds", 43200))
    var elapsed := get_effective_elapsed(now_unix)
    var S: int = 0
    if _campaign:
        S = int(_campaign.highest_cleared)
    var minutes := elapsed / 60.0
    var hours := elapsed / 3600.0
    var frac := (float(elapsed) / float(cap)) if cap > 0 else 0.0
    var stage_for_rate: int = max(S, 1)
    var gold_rate: float = float(idle.get("gold_base_per_min", 20)) * pow(float(idle.get("gold_growth", 1.08)), stage_for_rate - 1)
    var dust_rate: float = float(idle.get("dust_base_per_min", 8)) * pow(float(idle.get("dust_growth", 1.07)), stage_for_rate - 1)
    var forge_rate: float = float(idle.get("forge_base_per_hour", 0.4)) + float(idle.get("forge_per_stage_hour", 0.05)) * S
    var scroll_12h: float = float(idle.get("scroll_per_12h_base", 1)) + floor(float(S) / float(idle.get("scroll_stage_div", 5)))
    var gold := int(floor(gold_rate * minutes))
    var dust := int(floor(dust_rate * minutes))
    var forge := int(floor(forge_rate * hours))
    var scrolls := int(floor(scroll_12h * frac))
    if S == 0:
        var m: float = float(idle.get("uncleared_multiplier", 0.5))
        gold = int(floor(gold * m))
        dust = int(floor(dust * m))
        forge = int(floor(forge * m))
        scrolls = int(floor(scrolls * m))
    return {
        "gold": gold,
        "spirit_dust": dust,
        "forge_stone": forge,
        "summon_scroll": scrolls,
        "elapsed_seconds": elapsed,
        "capped": elapsed >= cap and cap > 0,
        "stage_index": S,
    }

func claim(now_unix: int, wallet: CurrencyWallet) -> Dictionary:
    var rewards := peek_rewards(now_unix)
    # Grant currency keys only; then last_accrual_unix = now_unix
    # EventBus.idle_claimed.emit(rewards)
    return rewards

func to_dict() -> Dictionary:
    return {"last_accrual_unix": last_accrual_unix}

func from_dict(data: Dictionary) -> void:
    last_accrual_unix = int(data.get("last_accrual_unix", 0))
```

Công thức `peek_rewards` ở trên là **bản chuẩn** — giữ nguyên khi copy.

### Campaign / Rewards / Enemy factory

```gdscript
# res://core/campaign/CampaignSystem.gd
class_name CampaignSystem
extends RefCounted

var highest_cleared: int = 0
var selected_index: int = 1
var first_cleared_ids: Dictionary = {}
var clear_counts: Dictionary = {}

func is_unlocked(index: int) -> bool:
    if index <= 1:
        return true
    return highest_cleared >= index - 1

func get_stage(index: int) -> Dictionary:
    return DataRegistry.get_stage_by_index(index)  # prefer injected data_reg in tests

func on_battle_result(stage: Dictionary, result: CombatResult) -> Dictionary:
    return {"first_clear": false, "rewards": {}}

func to_dict() -> Dictionary:
    return {
        "highest_cleared": highest_cleared,
        "selected_index": selected_index,
        "first_cleared_ids": first_cleared_ids.duplicate(),
        "clear_counts": clear_counts.duplicate(),
    }

func from_dict(data: Dictionary) -> void:
    highest_cleared = int(data.get("highest_cleared", 0))
    selected_index = int(data.get("selected_index", 1))
    first_cleared_ids = data.get("first_cleared_ids", {}).duplicate()
    clear_counts = data.get("clear_counts", {}).duplicate()


# res://core/campaign/RewardService.gd
class_name RewardService
extends RefCounted

func rewards_for_campaign(stage: Dictionary, first_clear: bool, victory: bool) -> Dictionary:
    if not victory:
        return {}
    if first_clear:
        var r: Dictionary = stage.get("first_clear_rewards", {}).duplicate()
        if stage.has("artifact_reward"):
            r["artifact_id"] = stage["artifact_reward"]
        return r
    return stage.get("repeat_rewards", {}).duplicate()


# res://core/campaign/EnemyWaveFactory.gd
class_name EnemyWaveFactory
extends RefCounted

func build_wave(stage: Dictionary, balance: Dictionary, data_reg) -> Array:
    # For each enemy_id / slot: scale stats per spec 12.2, wrap CombatantSnapshot.
    return []
```

`CampaignSystem.get_stage` trong production nên nhận `data_reg` inject, **không** gọi Autoload nếu đang viết test. Sửa signature nếu cần: `func configure(data_reg) -> void`.

---

## E. Combat (cô lập) + BattleService

Đặt `BattleService` ở `res://core/BattleService.gd` (ngoài folder `combat/auto_battle`).

### DTO

```gdscript
# res://core/combat/CombatantSnapshot.gd
class_name CombatantSnapshot
extends RefCounted

var uid: String = ""
var display_name: String = ""
var faction: String = "DAWNKIN"
var role: String = "WARRIOR"
var slot: int = 0
var is_enemy: bool = false
var stats: FinalStats = FinalStats.new()
var skill_mods: Dictionary = {}
var skill_profile: String = "basic"
var placeholder_color: Color = Color.WHITE

func to_dict() -> Dictionary:
    return {
        "uid": uid, "display_name": display_name, "faction": faction, "role": role,
        "slot": slot, "is_enemy": is_enemy, "stats": stats.to_dict(),
        "skill_mods": skill_mods.duplicate(), "skill_profile": skill_profile,
        "placeholder_color": placeholder_color.to_html()
    }

static func from_dict(d: Dictionary) -> CombatantSnapshot:
    var c := CombatantSnapshot.new()
    c.uid = str(d.get("uid", ""))
    c.display_name = str(d.get("display_name", ""))
    c.faction = str(d.get("faction", "DAWNKIN"))
    c.role = str(d.get("role", "WARRIOR"))
    c.slot = int(d.get("slot", 0))
    c.is_enemy = bool(d.get("is_enemy", false))
    c.stats = FinalStats.from_dict(d.get("stats", {}))
    c.skill_mods = d.get("skill_mods", {}).duplicate()
    c.skill_profile = str(d.get("skill_profile", "basic"))
    return c


# res://core/combat/CombatRequest.gd
class_name CombatRequest
extends RefCounted

var request_id: String = ""
var allies: Array = []
var enemies: Array = []
var seed: int = 0
var max_duration_seconds: float = 90.0
var tick_rate: float = 10.0
var modifiers: Dictionary = {}
var record_log: bool = true

func validate() -> Dictionary:
    if allies.is_empty() or enemies.is_empty():
        return {"ok": false, "error": "empty_side"}
    if max_duration_seconds <= 0.0 or tick_rate <= 0.0:
        return {"ok": false, "error": "bad_timing"}
    var seen: Dictionary = {}
    for u in allies + enemies:
        if seen.has(u.slot) and u.is_enemy == seen[u.slot]:
            # slots must be unique per side — implement a clearer check (two dicts).
            pass
        if u.stats == null:
            return {"ok": false, "error": "missing_stats"}
    return {"ok": true, "error": ""}


# res://core/combat/CombatResult.gd
class_name CombatResult
extends RefCounted

var request_id: String = ""
var victory: bool = false
var reason: String = ""
var duration_seconds: float = 0.0
var ticks_simulated: int = 0
var allies_survived: int = 0
var enemies_survived: int = 0
var stats_by_uid: Dictionary = {}
var events: Array = []
var error: String = ""
var rewards: Dictionary = {}  # AutoBattle MUST leave this empty

func to_dict() -> Dictionary:
    return {
        "request_id": request_id, "victory": victory, "reason": reason,
        "duration_seconds": duration_seconds, "ticks_simulated": ticks_simulated,
        "allies_survived": allies_survived, "enemies_survived": enemies_survived,
        "stats_by_uid": stats_by_uid.duplicate(true), "events": events.duplicate(true),
        "error": error, "rewards": rewards.duplicate(true)
    }


# res://core/combat/CombatInterface.gd
class_name CombatInterface
extends RefCounted

func run_battle(request: CombatRequest) -> CombatResult:
    push_error("CombatInterface.run_battle is abstract")
    var r := CombatResult.new()
    r.victory = false
    r.reason = "error"
    r.error = "abstract_interface"
    return r

func start(_request: CombatRequest) -> void:
    pass

func step() -> Dictionary:
    return {"done": true, "events": [], "hp_by_uid": {}}

func get_result() -> CombatResult:
    return CombatResult.new()
```

### AutoBattle

```gdscript
# res://core/combat/auto_battle/AutoBattleConfig.gd
class_name AutoBattleConfig
extends RefCounted

var tick_rate: float = 10.0
var max_duration_seconds: float = 90.0
var atb_threshold: float = 1000.0
var speed_to_atb: float = 10.0
var energy_max: float = 1000.0
var basic_energy_default: float = 50.0
var ult_energy_cost: float = 1000.0
var front_damage_taken_mult: float = 1.15
var back_damage_taken_mult: float = 0.90
var melee_vs_back_penalty: float = 0.85
var heal_power_from_atk: float = 0.80
var defend_factor: float = 80.0
var min_damage: int = 1
var timeout_is_defeat: bool = true

static func from_dict(d: Dictionary) -> AutoBattleConfig:
    var c := AutoBattleConfig.new()
    # assign each field if present
    if d.is_empty():
        return c
    c.tick_rate = float(d.get("tick_rate", c.tick_rate))
    c.max_duration_seconds = float(d.get("max_duration_seconds", c.max_duration_seconds))
    c.atb_threshold = float(d.get("atb_threshold", c.atb_threshold))
    c.speed_to_atb = float(d.get("speed_to_atb", c.speed_to_atb))
    c.energy_max = float(d.get("energy_max", c.energy_max))
    c.front_damage_taken_mult = float(d.get("front_damage_taken_mult", c.front_damage_taken_mult))
    c.back_damage_taken_mult = float(d.get("back_damage_taken_mult", c.back_damage_taken_mult))
    c.melee_vs_back_penalty = float(d.get("melee_vs_back_penalty", c.melee_vs_back_penalty))
    c.heal_power_from_atk = float(d.get("heal_power_from_atk", c.heal_power_from_atk))
    c.defend_factor = float(d.get("defend_factor", c.defend_factor))
    c.min_damage = int(d.get("min_damage", c.min_damage))
    c.timeout_is_defeat = bool(d.get("timeout_is_defeat", true))
    return c


# res://core/combat/auto_battle/AutoBattle.gd
class_name AutoBattle
extends CombatInterface

var config: AutoBattleConfig = AutoBattleConfig.new()
var sim: AutoBattleSim

func run_battle(request: CombatRequest) -> CombatResult:
    sim = AutoBattleSim.new()
    sim.config = config
    return sim.run_to_end(request)

func start(request: CombatRequest) -> void:
    sim = AutoBattleSim.new()
    sim.config = config
    sim.setup(request)

func step() -> Dictionary:
    if sim == null:
        return {"done": true, "events": [], "hp_by_uid": {}}
    return sim.step_one_tick()

func get_result() -> CombatResult:
    if sim == null:
        return CombatResult.new()
    return sim.build_result_if_done()


# res://core/combat/auto_battle/AutoBattleSim.gd
class_name AutoBattleSim
extends RefCounted

var config: AutoBattleConfig
# Internal sim state only. Do NOT reference GameState / Inventory / DataRegistry Autoload.

func setup(request: CombatRequest) -> void:
    pass

func run_to_end(request: CombatRequest) -> CombatResult:
    setup(request)
    while not is_done():
        step_one_tick()
    return build_result_if_done()

func step_one_tick() -> Dictionary:
    return {"done": true, "events": [], "hp_by_uid": {}}

func is_done() -> bool:
    return true

func build_result_if_done() -> CombatResult:
    return CombatResult.new()

# Implement per spec 13.6:
# _advance_atb, _perform_action, _choose_target, _apply_damage, _apply_heal, FactionMatrix local instance
```

**Cấm** trong 3 file `auto_battle/*` và DTO/Interface: `GameState`, `Inventory`, `user://`, `DataRegistry`, `HeroInstance`. Config và `FactionMatrix` (pure) thì được. `FactionMatrix.configure` phải được BattleService gọi rồi truyền vào sim (`sim.faction_matrix = ...`), hoặc sim nhận multipliers qua `request.modifiers`. Cách sạch nhất: `sim.faction_matrix = FactionMatrix.new(); sim.faction_matrix.configure(request.modifiers.get("faction_balance", {}))`.

### `res://core/BattleService.gd`

```gdscript
class_name BattleService
extends RefCounted

var combat: CombatInterface
var calculator: StatCalculator
var _reward_applied_for: String = ""

func set_combat_impl(impl: CombatInterface) -> void:
    # To swap combat later, call this with a new CombatInterface implementation.
    combat = impl

func configure(_deps: Dictionary) -> void:
    pass

func prepare_campaign_request(team_slots: Array[String], stage: Dictionary) -> CombatRequest:
    return CombatRequest.new()

func run_campaign_battle_batch(team_slots: Array[String], stage: Dictionary) -> CombatResult:
    return CombatResult.new()

func begin_campaign_battle(team_slots: Array[String], stage: Dictionary) -> CombatRequest:
    return CombatRequest.new()

func step_combat() -> Dictionary:
    return {"done": true, "events": [], "hp_by_uid": {}}

func finish_if_done() -> CombatResult:
    # Attach rewards ONCE per request_id, then grant via GameState (caller or here).
    return CombatResult.new()
```

---

## F. Test runner tối giản

### `res://tests/test_runner.gd` (gắn vào `test_runner.tscn`, F6)

```gdscript
extends Node

func _ready() -> void:
    var results: Array = []
    # Call each test file's run(results)
    TestIdle.new().run(results)
    TestStatCalculator.new().run(results)
    TestLevelUp.new().run(results)
    # ... add the rest as you implement milestones
    var failed := 0
    for line in results:
        print(line)
        if str(line).begins_with("FAIL"):
            failed += 1
    print("--- %d tests, %d failed ---" % [results.size(), failed])
    get_tree().quit(1 if failed > 0 else 0)
```

Quy ước helper:

```gdscript
func _assert_eq(results: Array, name: String, got: Variant, expected: Variant) -> void:
    if got == expected:
        results.append("PASS %s" % name)
    else:
        results.append("FAIL %s: got %s expected %s" % [name, str(got), str(expected)])

func _assert_approx(results: Array, name: String, got: int, expected: int, slack: int = 1) -> void:
    if abs(got - expected) <= slack:
        results.append("PASS %s" % name)
    else:
        results.append("FAIL %s: got %s expected %s (±%d)" % [name, got, expected, slack])
```

Test bắt buộc tối thiểu (expected lấy từ spec mục 16):

- Idle: S=10, elapsed=21600 → gold **14392**, dust **5294**, forge **5**, scroll **1**.
- Kael lv10 VETERAN no gear: HP **629**, ATK **50**, DEF **40**, SPD **97**.
- Level cost: lv1→80/40; lv5→825/351; lv10→2254/895; lv20→6160/2282; lv40→16829/5818; lv60→30297/10059.
- Faction: DAWNKIN vs IRONFANG = 1.15; IRONFANG vs DAWNKIN = 0.85; ASTRAL vs ABYSSAL = 1.15; DAWNKIN vs SYLVAN = 1.0.
- Gacha pity và 10x guarantee.
- AutoBattle deterministic cùng seed; không thưởng trong `AutoBattle`.
- Gear: weapon base 20, 5 star + faction → 35.

---

## G. Checklist copy file

Tạo đúng path ở mục 3.1 của spec. Mỗi class một file, `class_name` trùng tên file. Sau khi copy skeleton, làm đầy theo milestone, đừng để `not_implemented` tồn tại ở milestone đã nhận là xong.

