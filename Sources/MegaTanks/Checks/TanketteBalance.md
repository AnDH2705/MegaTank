# Balance check: tankette / light tank

Scope: combat, inferred from the requested light armor. No separate tankette GDD exists. Sources: GroundEnemy1Vehicle.prefab, TanketteVehicle.prefab, player MachineGun/Cannon/MissilePackAT definitions, EnemyDataManager.cs, CombatDamage.cs, and StageDefinition spawnableEnemies.

## Health summary: HEALTHY starting tuning; playtest enemy mix

| Stat | Car | Tankette |
| --- | ---: | ---: |
| Base HP / growth | 80 / +4 per difficulty | 80 / +4 per difficulty |
| Armor / growth | 0 / 0 | 3 / 0 |
| Movement speed | 0.7 | 0.6 |
| Width / scale | 2.2 / 1 | 2.2 / 1 |
| Weapon | Machine gun | Small cannon |
| Weapon damage, salvo, base reload | 10, 3, 2s | 14, 1, 3s |
| Scraps / score | 20 / 100 | 25 / 125 |

Both enemies retain the car's existing weapon growth values. At difficulty 0, full accuracy and zero player armor, sustained raw car DPS is 30 / (1 + 0.2) = 25 before misses. The tankette's small cannon fires a single 14-damage shell, then uses 3 seconds of base reload / (1 + 100/100) = 1.5 seconds of battle reload, for 9.33 raw DPS. It hits harder per shot but fires substantially more slowly. Thirty shells provide 420 raw damage over the full reserve, compared with the car's 300 from thirty MG bullets. At difficulty 10, base weapon damage is 19 versus the car's 15; raw DPS is 12.67 versus 37.5. Range, velocity, accuracy, penetration and gravity retain the original tuning. Heavy-projectile muzzle/impact effects and the existing shell art are reused.

HP matches the car so the new enemy is not a boss-like health sponge; it moves about 14% slower and pays 25% more scraps. Its light armor remains unchanged by the cannon conversion.

The game's armor is flat subtraction after penetration. Against base-level, zero-star player weapons before evasion/crit:

| Attack | Car damage per hit | Tankette damage per hit |
| --- | ---: | ---: |
| MG, 6 base damage | 6 | 3 |
| Cannon, 45 base damage | 45 | 42 |
| AT missile, 24 damage / 25% penetration | 24 | 21.75 |

At 80 HP, an MG needs 14 car hits or 27 tankette hits. A cannon still needs two hits for either enemy. With the existing +1 damage/level, a level-10 MG deals 15 versus the car and 12 versus the tankette; flat armor becomes less significant as weapons level up. Armor has zero growth, so difficulty scaling cannot turn this light tank into an invulnerable heavy tank. Standard MG rounds can always penetrate its starting armor.

The new profile is appended to stages that already include GroundEnemy1. The current uniform regular-ground selection gives car and tankette equal chances, while dedicated carrier handling and other enemy types stay in their existing pools. Playtest this encounter mix; no stages, bosses or existing enemy stats are retuned.

TanketteAudit verifies both enemies spawn naturally, exact world-width parity, armor damage, ordinary growth, grounding/facing, four moving road wheels, pause, cannon type and single-shot salvo, illustrated muzzle/ammo, actual projectile damage, full reload between shots, defeat and source immutability. TrojanScrapCatAudit recognizes authored regular-ground roster entries alongside the carrier.
