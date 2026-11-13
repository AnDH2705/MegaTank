# Combat effects

The combat renderer uses selected effects from the newly imported packs. Gameplay damage, cooldowns and rewards are unchanged.

| Event / library key | Pack and source prefab |
| --- | --- |
| Muzzle / `muzzle` | 118 sprite effects bundle, MuzzleFlashes/ef_1_red |
| Bullet hit / `impact` | Epic Toon FX, Prefabs 2D/Explosions/MetalHit2D |
| Splash, orbital, kamikaze / `blast` | FX Quest Mobile 2D, Explosions/M_2D_AirExplode_01 |
| Enemy defeated / `death` | FX Quest Mobile 2D, Misc/M_2D_SmokePuff_01 |
| Flame hit / `fire` | FX Quest Mobile 2D, Magics/M_2D_FireHit_01 |
| Ice hit and freeze on each enemy / `ice` | Epic Toon FX, Combat/Explosions/FrostExplosion/FrostExplosionBlue |
| Poison hit / `poison` | Epic Toon FX, Prefabs 2D/Blood/Green/GreenBloodExplosion2D |
| Lightning endpoint / `electric` | FX Quest Mobile 2D, Magics/M_2D_ThunderHit_01 |
| Heal / `heal` | FX Quest Mobile 2D, Abilities/M_2D_Recovery_01 |
| Shield / `shield` | FX Quest Mobile 2D, Abilities/M_2D_BoostDefense_01 |
| Other skill activation / `buff` | FX Quest Mobile 2D, Abilities/M_2D_Buff_01 |
| Successful capture / `capture` | FX Quest Mobile 2D, Misc/M_2D_PickItem_02 |

Full source paths and adjustable duration, size and instance limits are in `Assets/CatTanks/Resources/CombatVfxLibrary.asset`. Editable effect prefabs are in `Assets/CatTanks/Resources/CombatVFX/`. They keep vendor material/texture references and remove demo scripts, collisions, lights and audio. Vendor assets are untouched. The Particle System Unity module is now included in Packages.

Instances are pooled with a global limit of 48 active events and individual limits for each effect. Simulation follows battle effect time, including pause. Sorting order 46 keeps particles above units and below health indicators and overlay UI. Beam geometry, projectiles and persistent shield indicators remain part of BattleView.

`CatTanks > Combat > Build Selected Combat Effects` regenerates the prefabs and library, overwriting local adjustments to those generated assets. Adjust builder defaults if changes should survive regeneration.

In Play Mode, run `CatTanks > Combat > Test Effects and Capture Preview` to check emission, pause, advancement, pooling and limits. Reports go to `Logs/combat-vfx-*.json`; previews go to `Captures/CombatVFX/`. Gallery order is the table order, left to right, four effects per row. The combat preview is a controlled visual sample with effects placed at representative hit positions.

The unused AAA Projectiles LWRP shaders require their original render pipeline. No render pipeline change is needed for the selected combat prefabs. CartoonVFX9X fireworks were excluded because their presentation does not fit routine combat hits.
