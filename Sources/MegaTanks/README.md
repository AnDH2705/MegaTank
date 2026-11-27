# MegaTanks

## Canvas UI and front-end flow

### Decision clarity improvements

- Home labels Garage, Crew, Tech and Start Battle explicitly. Crew opens crew management; Tech displays Coming Soon; single-stage navigation arrows are hidden.
- Garage compares the equipped and selected weapon's effective damage, reload, range and reserve capacity, with signed changes. Accuracy, penetration, critical stats and damage type remain visible.
- Reward and service cards preview actual, capped ammo and hull gains. Full reserves/hull show a no-benefit warning; damage and maximum-hull cards show before/after values. Purchased cards are marked Applied.
- Battle counters display `! LOW AMMO` at 20% reserves and `! EMPTY` at zero, using text and color. Running dry briefly shows a weapon-specific resupply alert; alerts reset on a new run.
- After each completed battle, the tank drives to a field tent and continues automatically after its event.
- Defeat reports round reached, actual enemies defeated, score, reserves and weapon loadout. Garage ends the defeated run and opens equipment; retry starts with fresh run data.

These controls are authored in the existing UI prefabs. `MegaTanks > Apply UX Improvements` is a repeatable prefab migration that preserves existing binding IDs. `UXImprovementsAudit.Run` checks comparison tradeoffs, capped reward effects, warning transitions, tent progression, defeat navigation and restart, and writes rendered screenshots under `Checks/UXImprovements`.

The visual theme uses dark slate panels, warm gold primary actions, cyan resource values, and consistent bold headings with readable body text. `MegaTanks > Polish UI Visuals` applies the theme to existing authored screens and card templates; the UX migration also reapplies it. Decorative panels and rules never receive pointer input, and the visual pass does not change gameplay, bindings, or button actions.

Design reference: [MegaTank UI slides](https://docs.google.com/presentation/d/1wYHK3rKs4jGDTON6XZ8SG-Dip3ueG96ekwUVsSqcGwk/edit). The three reference slides are saved in `Reference/UI`; `Reference/UIDesign.url` pins the source. Their wireframes are adapted to the existing landscape 1920 x 1080 game.

The main scene contains a **MegaTanks UI Canvas** and **UI EventSystem**. All UI layouts are authored as native uGUI prefabs under `Assets/MegaTanks/Prefabs/UI` and are present in the scene outside Play mode. `CanvasGameUI` binds actions and updates live data without rebuilding static screens or resetting their layout. Select the Canvas and use **UICanvasLayout > Preview screen > Show in Editor** to inspect any screen. See [Editing the UI](UIEditing.md). All immediate-mode GUI rendering has been removed from gameplay scripts. The Canvas scales inside the device safe area; an isolated camera renders the existing tank artwork into the equipment avatar, without adding combat entities to the battlefield.

- **Home:** live tank and battlefield, stage-name plate at the top left, menu at the top right, Garage/Crew/Tech down the right side, stage/round selector with arrows at the bottom left, and Start at the bottom right. Layout follows the [new home reference](https://drive.google.com/file/d/1JBPI8HVlMUl04ukEAtPeZajUKhXeVRg1/view), saved at `Reference/UI/Home-20261110.png`. Home controls use the seven supplied sprites from `Art/Asset/UI`, following `ui copy.png`: yellow stage/Play panels, the menu button, combined stage arrows, dark button bases, and Garage/Crew/Tech icons. Crew opens crew management; Tech remains a placeholder; Garage opens equipment. Home lists Stages 1–10. The arrows stop at the ends of the list. Start hides Home and starts the first wave synchronously in the existing scene, retaining the camera and background without a loading screen. Enemies spawn ahead of the tank on the right and advance left toward it. Returning Home resets the tank and framing.
- **Equipment screen:** the supplied garage background, Back/header, mortar plus Accessory 2 and Accessory 3 on the left, and MG/cannon/armor on the right; a centered tank preview; a scrollable six-column inventory; All/Cannon/MG/Mortar/Armor/Accessories filters; ordering by name, type or damage, ascending/descending; and item inspection/equip controls. MG/cannon/mortar choices are saved locally and configure the next run without changing definition assets. Every occupied slot shows its equipped item icon. Green check icons mark all currently equipped items in the inventory. The remaining accessory slots retain their existing saved slot-2/slot-3 identities; the former slot-1 accessory is no longer equipped.
- **Settings:** independently saved sound/music sliders and a vibration switch, with a dark dismissible backdrop and close button. `GameAudioSettings` on an AudioSource applies its saved Sound or Music channel level; no sound/music assets have been invented. Android player-hit vibration respects the switch and is rate limited.
- **Battle:** Canvas ammo/capacity bars, scraps, score, encounter/wave, hull health, floored floating damage, and automatic Aggressive behavior. The stance buttons and bottom panel are removed to leave that space available for battle and dragging. Pause offers Resume, Settings and Return Home; leaving an active run asks before discarding run progress.
- **Progression:** tent rewards, card shop, defeat, and victory use Canvas controls. Victory and defeat offer Play Again and Home.

`Data/UI/Frontend.asset` lists the stages and available equipment. `EquipmentDefinition` is the shared item base; `WeaponDefinition` derives from it while retaining existing weapon profiles. `ArmorDefinition` supplies additive tank HP, armor, speed, and evasion, with Basic Armor and Light Armor in the inventory. `AccessoryDefinition` remains an item type without configured instances; its filter shows an empty state. New definitions are available through **Create > MegaTanks > Equipment**.

**MegaTanks > Configure Canvas UI** attaches the authored Canvas/EventSystem prefabs if missing, creates missing catalog references, and preserves existing layouts and catalog lists. `CanvasUIAudit.Run` exercises real EventSystem raycasts through Home/settings/equipment/filter/equip/Play, pause/resume, tent rewards, travel and Home, restoring settings after the check. `.Capture` additionally produces internal layout checks when explicitly needed.

### Layer Lab UI artwork

The Canvas UI uses 28 selected sprites from the supplied **GUI Pro-SuperCasual / Layer Lab** archive: [original ZIP on Drive](https://drive.google.com/file/d/1dj89xXo3nJAiK5Ygg3X5FyMNytJU0Zta/view). Only the selected button, popup, item-frame, slider/switch, bar and control-icon artwork is included in `Assets/MegaTanks/Art/UI/LayerLab`; `SourceAssets.txt` records their original archive paths. The original sprite slice borders are retained for scaling, with uncompressed bilinear imports and no mipmaps. Demo scenes, demo scripts, fonts and unrelated item artwork are excluded; the skin works with the existing uGUI controls and font.

`Data/UI/LayerLabSkin.asset` holds the sprite references and is assigned to the main scene's Canvas controller. Gold marks retry/purchases, blue/sky marks primary equipment/actions, dark frames support navigation. Home has its own supplied artwork in `Assets/MegaTanks/Art/UI/Home`; the `home*` fields on the same skin asset hold these sprites. `MegaTanks > Import Home UI And Mortar Shell` configures the supplied sprites and updates their references without changing weapon stats or tank placement. Navy popup panels, item frames, equipped checks, pause/close/menu/arrows, independent audio sliders and the vibration switch use the pack artwork. Slot item icons, tank artwork, card art and white/blue/purple/orange card rarity borders are unchanged.

**MegaTanks > Import Layer Lab UI Skin** configures these imports, rebuilds the skin references and assigns the scene. `CanvasUIAudit` also checks actual skin sprite bindings, sliced inventory frames, preserved borders, equipped-marker sprites and settings artwork while exercising real Canvas raycasts. Screen layouts remain built by the Canvas controller; this changes their artwork rather than adding new game systems.

**Pinned design source:** [Card effects and shop prices — Google Sheets, Cards tab](https://docs.google.com/spreadsheets/d/1JoIspgth7KeGzcHgBdwluRhmUyCh7FG83wH2ZbsP9iw/edit?gid=0#gid=0). A Windows shortcut is also saved at `Reference/CardEffects.url` in the repository. The game uses the checked-in snapshot and needs no network access.

## Gatling gun

The supplied [Gatling artwork](https://drive.google.com/file/d/1X8qJpfAATtQGrxvI7X3fb18w-cauDCkD/view) is preserved at `Art/Weapons/Gatling.png` in the repository and imported under `Assets/MegaTanks/Art/Weapons/Gatling`. In **Garage > MG**, select **Gatling Gun** and Equip. The original MG remains available. The live roof weapon, tank portrait, slot icon, equipped badge and saved selection update together; Start uses the selected weapon.

`Data/Weapons/GatlingGun.asset` starts with an independent copy of the current MG stats: 6 damage, 35 range, 3-shot salvo, 0.1-second shot delay, 1.25-second base reload time, 18 projectile speed, 0.35 gravity and 150 reserve bullets. Its optional artwork prefab sets a scaled 2D gun on the roof mount with its own forward muzzle and the existing MG flash. Re-equipping the original MG restores its artwork. `MegaTanks > Import Gatling Gun` rebuilds the art references and adds the item to the catalog without overwriting an existing Gatling stat configuration. No barrel-spin animation is added to the single supplied image.

## Ammo, rewards and shop

The current scene starts each run with **150 MG bullets**, **50 cannon rounds**, and **20 mortar shells**. Every salvo shot consumes one round; reload delays never refill reserves. Empty weapons remain silent until a resupply card restores ammo. Reserve capacities are editable in the separate weapon definition assets.

After a regular or hard battle, resolve a field tent event before continuing. Unspent scraps, ammunition, hull, and upgrades carry into the next battle. Restart resets the run.

Each offer rolls **Legendary 5% / Epic 15% / Rare 30% / Normal 50%**, then draws uniformly from that rarity's unused cards. All three reward choices have distinct card IDs; different rarities of the same effect remain different cards. Once a rarity runs out of unused cards, its odds are redistributed proportionally among the remaining rarities for that offer set. Cards can appear again in later waves. The catalog contains all 18 sheet rows with their exact percentages and prices: Normal 100, Rare 200, Epic 500, Legendary 1000 scraps. Legendary currently has damage/max-HP cards only; no extra resupply/repair entries are invented.

- Damage upgrades add a percentage of the selected gun's starting damage, stacking additively for this run: two +10% cards give +20% total.
- Max-HP upgrades add a percentage of starting maximum HP (100), stacking additively. The added capacity also increases current HP, preserving existing missing HP. Restart resets damage and HP upgrades.
- Repairs add the card's percentage of maximum HP, rounded up and capped.
- Resupply adds the card's percentage of each full reserve, rounded up and capped: with the current capacities, Normal restores +50 MG/+17 cannon/+7 mortar; Rare +99/+33/+14; Epic fills all reserves.
- Flyer/ground base scraps remain **20/30**. Scraps now follow the same difficulty-growth formula as other enemy stats; Scraps Grow defaults to 0.

`Assets/MegaTanks/Data/Cards/CardCatalog.asset` manages card definitions and images. `SheetSnapshot.json` records the read-only sheet snapshot dated 2026-11-03. To incorporate future sheet edits, update the snapshot and run **MegaTanks > Import Card Catalog**. The importer validates names and all rarity pools. It updates the catalog and scene reference without changing other battle settings.

`Art/Cards/CardAtlas.png` is a generated 18-card atlas sliced into individual sprites. Icons represent cannon damage, MG damage, tank HP, ammo supply and repair. Frames are white/blue/purple/orange by rarity, with foil detailing and a subtle animated sheen on Legendary cards. UI text is rendered by the game so values remain accurate after data changes. `ProgressionAudit.Run` verifies ammo, effects, scraps, rewards, shops and reset behavior without rendering previews.

Open this folder with Unity **2022.3.62f3 LTS**. Open `Assets/MegaTanks/Scenes/MainBattle.unity`, choose a **1920 x 1080 landscape** Game view, enter Play mode, then select **Play** on the Home screen.

The Android game is locked to landscape. HUD and safe-area scaling use a 1920 x 1080 design reference and adapt to the device's actual resolution. The tank defaults to **0.75** uniform scale and spawn anchor **(-7.3, -4.483333, 0)**, preserving the approved 2-unit downward move from the previous position. **Tank Spawn Position** on the battle object controls placement independently of **Scale** in `Data/Tanks/PlayerTank.asset`. Start, restart and Home retain these defaults; the hull bar follows the tank’s adjusted bottom edge.

The 2D tank uses the supplied cutaway art. MG/AA is on the roof, the main cannon is in the upper cabin, and the mortar is mounted on the left. Enemies approach from the right. Aggressive behavior is automatic; ultimate, stance, subroom and floating gun controls/labels are hidden.

## Gun settings

Edit `Data/Weapons/MachineGun.asset` and `Data/Weapons/Cannon.asset` to tune the two guns independently. `Data/Weapons/Mortar.asset` contains mortar tuning, and `Data/Weapons/StartingLoadout.asset` selects the starting weapon definitions.

| Gun | Damage | Salvo size | Shot delay | Reload after final shot | Max range |
| --- | ---: | ---: | ---: | ---: | ---: |
| MG/AA | 6 | 3 | 0.1 s | 1.25 s | 35 units |
| Main Gun | 45 | 1 | 0.1 s | 4 s | 50 units |

Range is measured from each weapon's mount to the enemy's center. Shells expire at a radius matching the gun range around their launch point; the extra length of a curved path does not shorten engagement range. Weapon ranges are editable in weapon definitions; spawn distance remains on the battle object.

Bullets use downward gravity: **MG Projectile Gravity = 0.35** and **Main Gun Projectile Gravity = 3**, in world units/second squared. Tune Gravity separately in each weapon definition; zero restores straight flight. Both guns compensate for gravity and lead moving targets using the lower reachable arc. Small collision-sweep steps follow the curve accurately, and fading tracers make its direction visible. Increasing gravity too far can make distant targets physically unreachable at the current 18-unit/second launch speed; a gun waits until it can solve a shot instead of wasting a salvo.

Cannon rounds use the supplied transparent `Art/Projectiles/CannonShell.png` sprite at the current **1.5 world units wide**, with its original proportions and colors. Its nose follows the current flight direction as gravity bends the trajectory. Adjust **Projectile Width** in the cannon definition to tune the visual size. Each weapon definition owns its projectile artwork and size. MG visuals and existing collision radii are retained.

Bullets and their tracers are destroyed as soon as the collision circle's lower edge touches the ground. Ground contact is swept along each ballistic step, so fast rounds cannot skip through the ground.

## Enemy data and weapons

The project's source **art folder** is `D:\SeagullGame\MegaTanks\MegaTank\Art`. `Enemies_vehicles.zip` supplies the layered vehicle artwork. `GroundEnemy1` uses the car and `FlyingEnemy1` uses the plane, mirrored to face the tank. Runtime PNGs and assembly manifests are imported into `Assets/MegaTanks/Art/EnemyVehicles`; layered prefabs are under `Prefabs/Enemies`.

Car wheels rotate while advancing, including while firing, and stop at the chosen destination. The plane plays the supplied twelve-frame side-view propeller animation at 40 ms/frame; its landing wheels stay still in flight. Flying wave lanes now start at **2.1 world units** with **0.45** lane spacing so planes and health bars remain below the top HUD. Tune **Flying Enemy Height** and **Flying Enemy Lane Spacing** on the battle object. Ground vehicles spawn **2 units lower**, with wheels aligned to the gameplay ground at **Y = -5.05**. Projectile ground impacts and mortar ground targeting use that same lowered plane; the scenery keeps its existing placement. Health bars sit above the models, and box hitboxes fit their visible bounds. **Artwork Width** in each enemy definition sets the base width (car 2.2 units, plane 2.4 units). **Scale**, set to **1.25** for both enemies, multiplies that width, giving a 2.75-unit car and a 3-unit plane. Hitboxes, muzzle positions, grounding and wave spacing follow the scaled artwork. Change Scale in `Assets/MegaTanks/Data/EnemyDataManager.asset` to tune each enemy type before spawning it.

The supplied body images include fixed guns. Shots originate at their illustrated muzzle sockets, while ballistic aiming and the existing MG/cannon stats control the projectile direction. The body art is not rotated to aim. **MegaTanks > Import Enemy Vehicle Art** rebuilds the generated prefabs and updates only artwork references/widths in the existing enemy definitions, preserving combat and reward tuning.

Enemies begin firing as soon as they enter weapon range, then continue horizontally for a random **1-5 units** before stopping. Each enemy picks its distance once when spawned, keeping its destination stable and spreading the firing line. Tune **Advance Inside Range** (X = minimum, Y = maximum) per enemy definition. Destinations are clamped to the tank's X position for very short weapon ranges.

Select `Assets/MegaTanks/Data/EnemyDataManager.asset` for the enemy-prefab registry. Open `Prefabs/Enemies/FlyingEnemy1Vehicle.prefab` or `GroundEnemy1Vehicle.prefab` and select the root **Enemy Stats Profile** to edit paired Base / Grow columns for unit and weapon stats. Existing base values are preserved; all new Grow values start at zero. Artwork, identity, and categorical types do not grow.

Each stage asset has **Stage Number** and independently editable **Stage Difficulty**. **Create > MegaTanks > Stage Definition** assigns the next stage number and defaults Stage Difficulty to `(stageNumber - 1) * 10`. **Use Default Stage Difficulty** restores that formula when desired; editing a stage never silently overwrites its custom difficulty.

At the start of each battle, `Difficulty = stageDifficulty + currentRoundNumber`, with rounds starting at 1. Every numeric combat property is calculated as `floor(Base + Difficulty * Grow)`, including HP, defenses, advance-distance endpoints, score, scraps, and all numeric weapon stats. Movement speed uses `Base Speed + Difficulty * Speed Grow` without rounding. Percentages remain uncapped until the final combat roll. Negative Grow values are allowed. The old HP/speed/scraps-per-wave additions are removed. All three waves of a hard battle reuse the first wave's snapshot, and the next battle recalculates using its own round. Runtime copies leave the prefab values unchanged.

Movement speed retains fractions: with zero Grow, base speed 0.75 stays 0.75. Timings still round down: shot delay 0.1 becomes 0, and reload 1.8 becomes 1. Tune Base / Grow explicitly to change these results. Existing firing-loop minimum timing safeguards still apply. Visual dimensions and scale keep their authored values.

| ID | Weapon | Damage | Range | Salvo | Shot delay | Base Reload Time | Projectile Speed | Gravity |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| FlyingEnemy1 | MG | 2 | 30 | 3 | 0.1 s | 1.8 s | 22 | 0.35 |
| GroundEnemy1 | Cannon | 8 | 40 | 1 | 0.1 s | 4 s | 18 | 3 |

These are starting balance values. Enemies fire upon entering their weapon range, advance another 1-5 units, then hold position and aim at the hull. They no longer ram or deal contact damage. Their movement prediction reports zero velocity while stopped. Enemy fire uses the same ballistic flight, ground destruction and range limits as player fire. Enemy projectiles hit only the tank hull; player projectiles hit only enemies. Cannon-type enemies share the cannon projectile sprite.

Use **Spawnable Enemies** in the stage Inspector to choose which prefab profiles can appear in regular waves. Each profile has a Kind of Ground or Flyer. With `step = floor(wave / 3)`, regular waves use `min(3 + step, 5)` ground slots and `min(2 + step, 5)` flyers. The wave number counts across the full run. If the roster has no enemies of a kind, that formation is skipped. Stage 1 uses the standard ground and flying vehicles. Stages 2–10 also include the Trojan Scrap Cat, which takes an interior ground slot when other ground vehicles are present. The boss uses its own stage settings. **MegaTanks > Set Up Enemy Data** links the manager to the scene and creates a default asset only if one is missing, preserving an existing asset's tuning.

The current enemy art comes from `Art/Asset/Enemies/Enemies_vehicles_Contrast_v2`: cobalt-blue ground vehicles and wine-red aircraft, with matching wheels and all 12 updated propeller frames. The 19 runtime PNGs keep their existing sprite GUIDs. Updated manifests account for the added transparent padding and local pivots while preserving canvas attachment points. **Import Enemy Vehicle Art** retains existing artwork widths and all enemy stats when updating existing prefabs. Original PSD/ORA files and comparison previews stay in the art folder.

## Draggable battlefield camera

The main camera reaches the physical top edge of the display; UI controls stay inside the safe area. The top HUD backdrop is transparent, with shaded text and individual counter panels over the battlefield. Exposed space between top controls accepts camera dragging. The battle uses one full-width orthographic camera, showing about 20.6 world units horizontally at 1920 x 1080, with the HUD overlaid on the same full-height view used by Home. The second enemy camera, split divider and nearest-enemy view label have been removed. Drag left/right across the battlefield with a finger or mouse to grab and pan the world. Vertical movement has no effect; camera height, depth and zoom stay fixed. When the tank advances, the camera moves by the same horizontal distance, keeping the tank at its current screen position and preserving any manual pan offset. There is no drag inertia.

Camera-center X is clamped between the player anchor and the enemy-side pan limit. The current scene's **Enemy Spawn Distance** is 50 units, editable on the battle object. Formation members spawn farther right behind the front point. The camera and both pan limits move with the advancing tank; a new wave updates the right bound from the tank's current position plus the configured distance. Wave changes retain the manually chosen camera position. Stage start, restart and Home frame the tank near the left edge at 18% of viewport width; **Tank Start Viewport X** controls this placement. The initial frame stays within the spawn bounds. Aggressive advances right toward the enemy formation.

A transparent Canvas battlefield surface handles touch and mouse dragging underneath the HUD. HUD controls, popups, pause, rewards, route/service screens, defeat and Home block panning. Only the first active drag pointer is used. Backgrounds tile and parallax across the single viewport. Floating damage appears only when in view, and the tank hull bar hides when the tank is off-screen rather than sticking over the enemy formation.

`CameraPanAudit.Run` verifies the full-width camera, mouse/touch input, pixel-to-world drag distance, spawn clamps, fixed horizon, UI blocking, stable tank screen position and manual pan offset during movement, camera position retention across wave changes, restart and Home. `CameraPanAudit.FollowRun` provides a focused check of stable tank screen position before and after manual panning, pause, defensive stance, restart and Home. `.Capture` additionally writes two internal layout checks at the spawn limits. `EnemyCameraAudit.Run` and the historical smoke-test entry point now route to this current camera audit.
## Tank art

`Assets/MegaTanks/Prefabs/TankCutaway.prefab` is an editable assembly of all 39 supplied PNG parts. It preserves the original positions, custom pivots, group hierarchy and layer order at 180 pixels per unit. The original archives remain outside the Unity project in the repository's `Art` folder. PSD/ORA files and preview GIFs are not included in player builds.

- `Art/Tank/parts`: individually imported sprites.
- `Art/Tank/metadata`: source assembly information.
- `Art/VFX/sheets`: three sheets sliced into eight sprites each, with the supplied per-frame pivots.
- `TankArtwork`: subtle hull vibration and a fixed pool of three overlapping exhaust animations. Road wheels rotate while advancing and stop while stationary.
- `GunArtwork`: aiming, barrel recoil and muzzle flash replay for every shot. Muzzle sockets follow the moving barrels; projectile spawn positions use those sockets.
- `SpriteClipPlayer`: frame playback using gameplay time, so effects pause with combat. MG flash restarts for every salvo round, then finishes its smoke tail after the final shot.

The main cannon uses the supplied seven-degree art alignment correction. Its mantlet is fixed to the tank body; only the barrel rotates around its base and recoils, carrying its muzzle socket and effect. The MG and cannon return to their default barrel position, clear recoil and stop muzzle flashes whenever no living target is in their own range, including after a wave ends. This visual reset preserves ammunition and reload/salvo progress. Gun aiming remains unrestricted; extreme rotations may still reveal limitations in the side-view art. The target hit effects and projectile shapes remain prototype visuals; the supplied cannon effect is used only at the muzzle.

**MegaTanks > Import Tank Artwork** regenerates the prefab from the included metadata and links it to the scene while retaining gun settings. It replaces edits made directly to the generated prefab; use a prefab variant for custom changes. **Create Main Battle Scene** replaces the scene with a new bootstrap scene and resets its settings.

## Validation

Generate previews only when requested or when necessary to verify a change; routine gameplay checks run without rendering previews.

Close this project in Unity before running a separate batch instance:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' -batchmode -projectPath 'D:\SeagullGame\MegaTanks\MegaTank\Sources\MegaTanks' -executeMethod MegaTanks.Editor.MegaTanksSetup.SmokeTest -logFile 'D:\SeagullGame\MegaTanks\MegaTank\Sources\MegaTanks\smoke.log'
```

Success logs contain `MEGATANKS_CAMERA_PAN_OK`. This entry point now checks the single draggable camera and its input/boundary behavior without writing previews. Use the dedicated weapon, mortar, stats, stage and Canvas audits for those systems. `MegaTanks.Editor.CameraPanAudit.Capture` additionally creates spawn-limit layout images in `Checks/CameraPan` when needed.

Landscape mobile settings are configured. **MegaTanks > Apply Landscape Settings** reapplies the orientation and reference resolution without replacing the scene. Install the appropriate Unity platform modules before Android/iOS builds. Real-device performance and touch behavior still require hardware validation.

Damage numbers display actual HP lost rounded down to whole numbers (12.5 displays as 12; HP calculations keep their precision), rise and fade over 1.1 seconds, freeze while paused, and clear between waves or on restart. Enemy damage is yellow and tank damage is red; labels are clipped to the single battlefield viewport. Zero damage and hits on defeated entities do not create labels.

The cannon aiming pivot sits 0.45 local units (81 source pixels) left of the barrel's original pivot, behind the fixed mantlet. The barrel and muzzle retain their original rest positions; the mantlet renders over the barrel. Tank artwork import and Fix Cannon Mount preserve this arrangement.

## Mortar

`Art/Tanks/Tank_Mortar_Frame_v4.zip` provides the mortar barrel and fixed front support. Only those two layers are added to the original tank, preserving its layered interior, cannon pivot and MG. The v4 canvas's 120-pixel tank shift is compensated when attaching the mortar. Its tube stays upright in the cradle and recoils downward; the muzzle uses the supplied metadata socket. `MegaTanks > Import Mortar Artwork` updates this attachment, and the tank importer restores it during a full artwork rebuild.

Tune `Data/Weapons/Mortar.asset`: damage 60, base reload time 6 seconds, range 80 units, launch angle 70 degrees, gravity 12, 20 reserve shells, deviation 1-3 units, full-damage radius 2.5 and half-damage radius 5. These are initial editable balance values. Mortar ammunition has its own top HUD counter and is restored by the existing resupply cards along with MG/cannon reserves. Existing cannon/MG damage cards continue to upgrade their named weapons; the mortar has independent damage settings.

Mortar shells use the supplied `Art/Asset/Tanks/Changeable_parts/ammo/mortar_ammo.png`, imported under `Art/Projectiles`, at the existing **0.45-world-unit** width. The sprite preserves its proportions and its nose follows the shell velocity through the arc. The separate `mortar_ammo_fire` image is not used.

The mortar prefers the largest ground formation within range, falling back to flyers, and leads movement before sampling a left/right horizontal offset of 1-3 units once per shot. Launch speed is solved for the configured steep angle (clamped to 46-85 degrees); the ordinary low-arc gun solver is not used. Shells can travel above the visible camera area. Range gates target selection; the curved shell path is allowed to complete its descent. Shells explode on enemy or ground contact, never on the tank. Flying targets must be struck by the shell or be inside a blast; misses do not detonate in midair.

Mortar shells pass through enemies and tank hulls during flight and detonate only when their lower edge touches the ground. Ground contact is swept so fast shells cannot tunnel through it. Units receive no direct shell-contact damage. AoE uses 2D distance from the ground impact to enemy centers: the configured inner radius deals full damage, the outer radius deals half damage, and targets beyond it receive none. Each target receives at most one damage application per explosion. Flying enemies are affected only if within the ground blast radius. Enemy and boss mortar shells follow the same ground-only rule and retain their faction-specific area damage. Mortar explosions do not leave lingering fire. Flight, recoil and reload pause with combat; empty reserves require resupply, and restart resets the mortar. `MortarGroundAudit.Run` verifies ascending/descending pass-through, no contact damage, fast ground impacts, full/half/outside blast damage, enemy hull pass-through and ordinary cannon collisions. `MortarAudit.Run` also covers high arcs, scatter, ammunition and resupply.
## Tank stances

The tank retains its starting world X = -7.3. The single camera frames this spawn at 18% of the viewport width at stage start; its horizontal position is then controlled by battlefield dragging.

The supplied `Art/Anim/chain track` rig now runs through the official **Spine 4.2** runtime using `SkeletonAnimation`, its path constraint, 38 bones and 36 chain links. The original 3.8.99 binary cannot be read directly by 4.2, so `TrackSpine42Converter` converts this specific rig to compatible JSON, preserving its bones, slots, region transforms, closed path, spacing and idle/running timelines. Original art files remain untouched. The converted JSON, original packed atlas/texture, material and SkeletonDataAsset are in `Assets/MegaTanks/Art/Tracks/Spine42`. `Data/Tracks/TankTracks.asset` references the Spine asset and retains sampled geometry only for import metadata and travel-length measurement; it no longer drives individual link transforms.

Spine playback follows actual tank travel and artwork scale, alongside the road wheels. The track attachment is fitted to 103% width and 89% height about its belt center: the ends meet the grey backing while the accepted top/bottom wheel fit stays unchanged; `TrackAnimationImporter.TrackFitScale` retains this fit during reimports. Playback compensates for the attachment scale. Defensive stance, automatic stops, pauses and noncombat screens hold the current pose; restarting resets the loop. **MegaTanks > Import Track Animation** rebuilds the compatible JSON, import settings and tank prefab; full tank artwork imports retain the Spine attachment. `TrackAnimationAudit.Run` verifies the 4.2 runtime, bone count, placement, movement timing, idle/pause behavior and restart without producing previews.

The official runtimes are embedded under `Packages/com.esotericsoftware.spine.spine-csharp` (**4.2.37**) and `Packages/com.esotericsoftware.spine.spine-unity` (**4.2.121**) so Git carries the installation. Source: [Esoteric Software spine-runtimes, branch 4.2](https://github.com/EsotericSoftware/spine-runtimes/tree/5cb78e77d3711ddcd80c7bd043bb11cf70fcc88c), pinned snapshot `5cb78e77d3711ddcd80c7bd043bb11cf70fcc88c`. Both packages retain the upstream Spine Runtimes license. Built-in Unity UI, animation and physics modules are dependencies of the official plugin; gameplay continues to use 2D physics.

The tank starts in **Aggressive**, which advances right at **0.5 world units/second**, stopping as soon as the MG, cannon and mortar each have a living enemy in their configured range. Each weapon can have a different target; range checks use the same mount/muzzle positions as their targeting logic. This is a range check, independent of ammo/reload or whether a ballistic shot is currently reachable. Aggressive stays selected and resumes advancing if a later target/wave is out of range. Empty battlefields, pause, reward/shop screens and defeat halt movement. If no enemy is vertically reachable by one of the weapons, advancing cannot fix that, so the tank holds position.

Set **Advance Speed** in `Data/Tanks/PlayerTank.asset` to tune movement. Movement carries the entire tank, weapon sockets and hull hitbox; the hull bar follows it when visible, while the camera retains the player's chosen pan position. Background layers scroll and repeat as the camera pans. Enemy shots lead the tank's movement. New waves spawn relative to the tank's current position. Restart restores the starting position and Aggressive stance. Road wheels roll only while moving. `TankStanceAudit.Run` checks speed, stance switching, all three weapon ranges, pause/rewards, cameras, hitbox attachment, wheel motion and restart without previews.
## Stage progression

Rounds count battles, including the opening battle and the final boss. A regular battle has one wave and a hard battle has three waves. Ammo, hull, and reload state carry across waves. The battle and hard-battle weights and stage seed control the sequence.

After a battle, the tank travels at twice its normal speed to a tent marked with a roadside sign. The camera retains its battle framing. Every fifth battle and the stop before the boss use a Vendor tent. If those schedules coincide, there is one vendor visit. Other stops roll equally among Upgrade, Repair, and Resupply.

- **Upgrade:** choose one of three distinct upgrades using the existing rarity weights.
- **Repair:** choose the common Repair card or Rare Scavenge battlefield.
- **Resupply:** choose the common Resupply card or Rare Scavenge battlefield.
- **Vendor:** buy from eight distinct cards, including common Repair and Resupply. You can leave without buying anything.

Scavenge battlefield adds 75–110% of the scraps dropped in the completed battle, including all three waves of a hard battle. Resolving the event or leaving the shop starts one second of travel into the next battle. Defeating the final boss opens Victory and grants stage completion rewards.

StageProgressionAudit.Run checks the tents, camera continuity, travel, vendor timing, hard waves, and the transition to Victory. StageScreensAudit.Run also captures the tent and Victory screens.

## Equipment UI artwork

`Art/Asset/UI/Equipment/equipment demo.png` is the layout reference. Thirteen supplied PNGs are imported under `Assets/MegaTanks/Art/UI/Equipment`, with bindings in `Data/UI/EquipmentSkin.asset`. The full-screen garage uses the Back/title artwork, six normal/yellow selected-slot frames, plus icons for empty slots, an orange item-card grid, both equipped ticks, the bottom inventory/detail panel, filter/sort fields and arrows, and the yellow Equip button. The original item selection, slot compatibility, saved loadout, filters, sort order/direction, and equipment changes are retained. Armor and accessory slots remain empty until actual items exist.

The tank portrait is framed from its visible bounds and refreshed after equipment changes. This changes only the preview; battle scale and position are preserved. Item details show the selected weapon’s base damage, percentage bonuses, accuracy, penetration, critical values, range, maximum ammo, damage type, Base Reload Time, total Reload Speed and calculated battle reload time, including uncapped percentage values. **MegaTanks > Import Equipment UI** configures the artwork and binds the equipment skin without replacing item data. `CanvasUIAudit.CaptureEquipment` checks real filtering/sorting/slot/equip/Back interactions and writes only equipment layout checks when visual QA is needed.

## Tent sign artwork

Tent signs use the existing repair cross, ammunition, upgrade arrow, and shop icon from the radar route skin.

## Battle backgrounds

`Art/Asset/BG/industrial_bg_6_layers.zip` and `parallax_bg_7_layers.zip` supply the Industrial (6 layers) and RuinedCity (7 layers) environments. Runtime PNG layers and the original parallax configurations are in `Assets/MegaTanks/Art/Backgrounds`. The prototype grid/ground line are replaced by these environments; gameplay ground height and collisions use Y = -5.05, independently of the scenery alignment reference at Y = -3.05.

Set Background Id on a stage asset to choose its environment. The defaults alternate between Industrial on odd stages and Ruined City on even stages. Stage selection updates the background on Home, and that background stays in use throughout the stage.

`Assets/MegaTanks/Data/Backgrounds/BattleBackgrounds.asset` stores the sprites, depth factors, and **Scenery Scale**, currently **0.5**. Scenery layers shrink uniformly around the gameplay ground line; the full-screen sky continues to cover the view, and a reflected textured road section fills below the scaled road canvas. `05_foreground_debris` stays flush with the camera viewport bottom and `06_overhead_cables` stays flush with its top, including when the camera moves or changes size. Their horizontal parallax and half-size scale remain unchanged. One set of environment layers follows the main battle camera. Sky stays screen-fixed; farther layers scroll more slowly and the road follows world movement. Horizontally repeated layers alternate mirrored tiles as recommended by the supplied metadata, avoiding gaps and mismatched border edges. The full 1920x1080 sprite canvases and aspect ratios are preserved; the camera crops to its viewport, and static sky expands uniformly to cover extra-wide screens. All artwork remains behind gameplay entities. Text shadows keep battlefield labels readable over bright art.

**MegaTanks > Import Battle Backgrounds** rebuilds import settings/catalog references and assigns two free camera-layer names without replacing scene tuning. `BattleBackgroundAudit.Run` verifies canvas preservation, single-camera layers, parallax, mirrored tiling, wide-screen coverage, stage selection and background retention. `.Capture` additionally renders both environments for explicit internal visual QA.
## Combat stat properties

Tank and enemy profiles now include Armor, Evasion, Critical Chance Reduction, and Critical Damage Reduction. Their existing HP and Speed values remain in the original serialized fields: tank `maxHull` / `advanceSpeed`, enemy `baseHealth` / `baseSpeed`, including the existing enemy wave scaling. Inspector labels show HP and Speed. Unit death/defeat at zero HP keeps its existing behavior.

All gun, mortar and enemy-weapon stats include Damage Bonus, Accuracy, Armor Penetration, Critical Chance, Critical Damage, and Damage Type. Existing `damage`, `reserveCapacity`, and `range` fields retain their values and now appear as Base Damage, Max Ammo, and Range. Enemy weapons additionally have a new Max Ammo field. Every newly added numeric field starts at **0**. `DamageType.Normal = 0` and `DamageType.Fire = 1`; the firing-style enum for MG/cannon remains separate. Percentages use percentage points: 25 means 25%, rather than 0.25. All raw percentage properties can exceed 100%; their values remain uncapped through modifier subtraction. Only final hit and critical roll probabilities are clamped to 0–100%. For example, 170 Accuracy − 130 Evasion gives a 40% hit chance.

Combat formulas are active for both teams and mortar AoE. Hit chance is clamp(Accuracy − Evasion, 0, 100), rolled once per target. Weapon damage is Base Damage × (1 + Damage Bonus / 100). Critical chance is clamp(Critical Chance − Critical Chance Reduction, 0, 100). On a critical, the damage multiplier is 1 + max(0, Critical Damage − Critical Damage Reduction) / 100; both reductions are treated as percentage-point subtractions. Armor after penetration is max(0, Armor) × (1 − Armor Penetration / 100). HP lost is max(0, weapon damage × critical multiplier × AoE multiplier − armor after penetration). Mortar uses AoE multipliers 1 and 0.5, then applies armor independently for each target. Evasion is not rolled a second time. Armor Penetration also retains values above 100%; the stated formula can therefore produce negative effective armor. HP loss still has a zero minimum.

**Combat bonus/chance properties default to zero; Reload Speed defaults to 100%:** 0 Accuracy always misses, and enemy Max Ammo of 0 means no enemy shots until ammunition is configured. The current enemy weapon profiles are configured at 100 Accuracy: FlyingEnemy1 has range 7.5 and 30 bullets; GroundEnemy1 has range 10 and 10 rounds. Misses and armor-blocked attacks cannot heal or damage a target. Fired rounds consume ammo even if they miss, and reload does not refill it. Enemy ammo is finite and supports explicit resupply. Existing damage cards add percentage points to Damage Bonus instead of compounding. Projectiles snapshot weapon modifiers and damage type at launch, while target defenses are read on impact. Normal and Fire are tags only; no burn effect is added. Scripted direct-HP helpers remain available separately from weapon attack resolution.

`CombatFormulaAudit.Run` verifies probability boundaries, zero accuracy, armor/penetration, critical reductions, physical projectiles for both teams, independent mortar targets, additive cards, enemy ammo/resupply and stat isolation. Enemy and mortar combat audits use explicit hit-capable runtime fixtures and leave saved profiles unchanged. `EnemyWeaponLiveAudit.Run` additionally tests the actual saved weapon values, both flyer lanes, the ground cannon, real approach and damage, ammunition consumption, and a moving tank without overriding weapon accuracy, range, or reserves.

`UnitDefenseStats` and `WeaponCombatStats` share the added fields across the existing stat classes. Gun, mortar and tank runtime copies retain all new values independently of saved assets. **MegaTanks > Persist Combat Stat Properties** serializes missing fields using their defaults without overwriting existing tuning; `CombatPropertiesAudit.Validate` checks the schema, defaults, damage tags and copying.

## Separate tank and weapon profiles

All existing player tuning has been migrated out of `BattleGame` into independent definition assets, preserving the current values. Defense and weapon-modifier properties are available as described above; their calculations are active through the shared combat resolver.

- `Assets/MegaTanks/Data/Tanks/PlayerTank.asset`: base maximum hull 100, advance speed 0.5, scale 0.75. This asset contains no weapon stats or weapon references.
- `Assets/MegaTanks/Data/Weapons/MachineGun.asset`: target priority, damage, range, salvo size, shot delay, reload time, projectile speed/gravity, reserves, projectile kind and optional sprite/width. Current tuning remains 6 damage, 35 range, three-round salvos, 0.1-second shot delay, 1.25-second base reload time, 18 projectile speed, 0.35 gravity and 150 rounds.
- `Assets/MegaTanks/Data/Weapons/Cannon.asset`: the same gun settings, independently configured. Current tuning remains 45 damage, 50 range, one-round salvos, 0.1-second shot delay, 4-second base reload time, 18 projectile speed, 3 gravity, 50 rounds and the supplied shell sprite at 1.5 units wide.
- `Assets/MegaTanks/Data/Weapons/Mortar.asset`: current mortar damage/timing/range, steep launch angle, gravity, reserves, deviation, both AoE radii, shell size and projectile sprite. Current values remain 60 damage, 6-second base reload time, 80 range, 70-degree launch angle, 12 gravity, 20 shells, 1-3 deviation, 2.5/5 radii and 0.45-unit shell width.
- `Assets/MegaTanks/Data/Weapons/StartingLoadout.asset`: references the three starting weapon definitions separately from the tank definition. The battle object references the tank profile and starting loadout; it retains encounter, camera, stage, background and artwork references.

`WeaponDefinition` is the shared weapon identity base; `GunDefinition` and `MortarDefinition` provide type-appropriate settings. Weapons copy their definition values into independent runtime state, so ammo usage, upgrades, and temporary adjustments cannot modify saved tuning or another weapon. HP and damage cards remain additive against the selected profiles' base stats. Restart loads fresh snapshots of the tank and starting loadout, restoring their configured capacities and base values. A weapon can be configured from another compatible definition without changing the tank or starting loadout; equipment selection UI and replacement-art logic remain future work.

Create additional definitions/loadouts through **Create > MegaTanks > Tanks/Weapons**. **MegaTanks > Migrate Player Stats To Profiles** creates missing assets from legacy scene tuning and preserves existing profiles on subsequent runs. The original pre-migration scene tuning is retained in `Reference/BattleBeforeStatsSeparation.unity.txt`. `StatsProfileAudit.Run` verifies runtime/profile isolation, unchanged starting values, additive local upgrades, compatible alternative definitions, and restart behavior without generating previews.

## Crew management

Crew and Garage share the supplied Crew_demo artwork style: tank/loadout on the left, selected-item details and upgrades in a slanted middle panel, and a three-column roster on the right. Their separate CrewRosterItem and EquipmentRosterItem prefabs preserve each type of content. The existing bindings, assignment/equip actions, role/category filters, sorting, currency costs and saves are retained. Garage comparisons scroll; crew stat/skill icons open live information. The crew portrait opens detailed level gains. Change Tank in Crew opens Garage, where the existing tank selector remains available. See `Assets/MegaTanks/Art/UI/Crew/INTEGRATION.md` for asset mappings and `MegaTanks > UI > Rebuild Crew and Equipment Artwork` for authoring. No new skill economy or combat bonuses are introduced.

The Home **Crew** button opens the editable `Prefabs/UI/Crew.prefab`, styled like equipment management. The sample roster contains five cat characters: **Milo** (gunner), **Pip** (driver), **Ember** (sub gunner), **Mochi** (weapon specialist), and **Olive** (commando). Generated portraits and the exact built-in image-generation prompts are saved in `Art/Crew`; their definitions and catalog are in `Data/Crew`. `Data/UI/Frontend.asset > Crew` links the catalog.

Select a roster card to highlight its compatible station in gold, both in the station list and on the central tank preview. Click either highlighted station to assign the selected member immediately. Selecting a station also filters the roster to matching-role crew; clicking the filter button clears that station filter and restores All Crew. Without a station filter, the filter button cycles All / Available / Assigned. Sorting and availability filters respect the selected role. The **Assign** button remains available as an alternative. Portraits update on both views, and green ticks identify assigned members. Clicking a nonmatching station changes the role filter without moving the wrong-role member. Assigning another member of the same role replaces the existing occupant. **Unequip** removes the selected cat. A cat cannot occupy two stations, and deployed loadouts cannot be edited. Role restrictions also apply to saved-loadout recovery.

Assignments save locally using stable crew IDs and remain equipped across battle, Home, and restarting the app. Unknown or duplicate saved IDs are ignored; malformed saves fall back to an empty crew. Crew assignment is separate from weapons and run upgrades. Crew members can only occupy their matching role station. Legacy saved assignments migrate by member role. Each stat has a Value and a Gain; its leveled value is Value + Gain × (Level − 1), followed by the existing +10% per star multiplier.

All screen elements are native Canvas prefab objects, editable outside Play through **UICanvasLayout > Preview screen > Crew > Show in Editor**. `Tank Preview/Tank Slot 0…4` are editable portrait anchors over the supplied crew tank illustration. Garage retains its live equipment portrait. Both rosters use their dedicated card prefabs and three-column Grid Layout Groups. **MegaTanks > Configure Sample Crew** imports missing sample definitions and links the catalog without rebuilding an already-authored crew layout or replacing edited crew definitions. `CrewAudit.Run` checks persistence, role restrictions, direct assignment through both slot views, role/availability filters, real pointer raycasts, portraits, prefab geometry, and battle/Home retention while restoring the user's saved crew. `CrewAudit.RunInteractions` runs those checks against the existing authored UI without reinstalling sample assets or saving a new editor portrait.

## Progression currencies

Parts, Pet Food, and Gems are persistent player balances, separate from run-only scraps. Home, Garage, and Crew show all three balances and update after a transaction. New wallets start at zero; starting/retrying a battle or returning Home does not reset them.

- **Parts:** level up weapons and armor in the Garage.
- **Pet Food:** level up crew members.
- **Gems:** premium currency, with its gameplay and purchase uses deferred.

`BattleGame.Wallet` exposes `GetBalance`, `CanAfford`, `TryAdd`, and `TrySpend` using `CurrencyType`. Successful transactions save locally and fire `Changed`; invalid amounts, insufficient funds, and integer overflow are rejected. Balances use the `MegaTanks.Wallet` PlayerPrefs key. Existing equipment and crew assignments are preserved. This local prototype wallet is not a purchase verification system.

Stage completion now awards currencies; other drop sources and premium features are not configured yet. `MegaTanks > Configure Currencies` adds missing authored balance labels without rebuilding existing screens. `CurrencyAudit.Run` checks persistence, independent balances, transaction guards, malformed saves, and repeatable prefab migration using a disposable save key.

## Weapon and crew progression

Weapons and crew start at level 1 and zero stars. A level purchase costs `ceil(100 * 1.1^(currentLevel - 1))`: 100, 110, 121, 134, ... . Weapons and armor spend Parts; crew spend Pet Food. No level cap is imposed, but purchases whose cost exceeds the integer wallet limit are disabled.

Star costs depend on the current stars: 20 at 0–4, 40 at 5–9, 70 at 10–14, and 100 at 15–19. Maximum stars is 20. Weapons consume their own Blueprints; crew consume their own Pet Tags. Materials cannot transfer between definitions. Extra materials remain at the cap.

Weapon permanent damage is `(definitionBaseDamage + level - 1) * (1 + 0.1 * stars)`. Crew no longer use the implicit +1 damage-per-level formula or a shared damage pool. Gunner accuracy, damage bonus, reload speed, critical chance and critical damage apply only to the cannon. Sub gunner applies those bonuses only to the machine gun or gatling; weapon specialist applies them only to the mortar. Driver HP, evasion and speed add only to the main hull. Assigned bonuses refresh immediately at Home and are copied into battle stats. Definition assets are never modified by player upgrades.

Garage and Crew have authored upgrade strips with levels, stars, owned/required materials, next-level and next-star damage previews, and disabled controls when funds are insufficient or the cap is reached. Inventory cards show levels. Stars appear below inventory, equipped-slot, and detail icons (including crew tank markers): 1–5 yellow, 6–10 purple, 11–15 red, and 16–20 rainbow. Each tier shows 1–5 stars; zero stars shows none. Rainbow stars use vertex colors rather than font symbols. The controls and star anchors remain editable in the prefabs.

### Adding content and granting materials

Create a Gun Definition, Mortar Definition, or Crew Member through Unity's Create menu, configure its base stats/art, and add it to the existing Frontend inventory or crew catalog. The permanent `id` is generated automatically. Preserve it across renames and reordering. When duplicating an existing definition, clear its copied ID once to generate a new one. Duplicate or empty IDs are rejected rather than sharing saved progression accidentally. Legacy weapon selections saved by asset name migrate to permanent IDs on load.

`BattleGame.Progression` exposes `Get(definition)`, `TryGrantFragments(definition, amount)`, `CanLevelUp`, `TryLevelUp`, `CanStarUp`, `TryStarUp`, and `Damage`. Grant fragments using a weapon definition for Blueprints or a crew definition for Pet Tags. Grant Parts/Pet Food through the existing `BattleGame.Wallet.TryAdd` API. No new currency enum, per-item save field, or combat branch is needed when adding a weapon or crew member. Stage completion awards currencies; equipment and crew material drop sources remain to be configured. The temporary play-test allowance below supplies testing resources.

Progression records use the `MegaTanks.Progression` PlayerPrefs key with a schema version and item IDs. Material counts and stars save together. Level purchases stage both the progression change and wallet deduction before one PlayerPrefs save, then notify the UI. Unknown saved item records are retained for temporarily unavailable content; invalid values are sanitized and malformed saves fall back safely.

`MegaTanks > Configure Progression` installs missing authored controls and star bindings. `ItemProgressionAudit.Run` checks cost rounding, all star bands, cap/leftovers, independent materials, reloads, added content, renaming, duplicate IDs, malformed saves, legacy loadouts, real Garage/Crew button raycasts, battle damage, and retry behavior. It restores player saves after testing and writes screenshots under the validation project's `Checks/ItemProgression` folder.


### Upgrade play-test resources

The current play-test allowance tops up the saved player to at least 100,000 Parts, Pet Food, and Gems, plus 1,000 Blueprints per catalog weapon and 1,000 Pet Tags per crew member. Existing levels, stars, and higher balances are preserved. It is applied once before the UI opens, with the MegaTanks.PlaytestResources.UpgradeTesting.v1 marker preventing automatic refills on subsequent launches. Use MegaTanks > Playtesting > Top Up Upgrade Resources to refill deliberately; this also refreshes the live UI when running in Play mode. Remove the temporary PlaytestResources.ApplyOnce call when moving to the actual reward economy.


## Armor

Garage > Armor lists Basic Armor and Light Armor. Neither is equipped automatically. Select one to inspect its bonuses and the resulting tank totals, then Equip. Replacing armor removes the prior contribution. Selection saves by permanent armor ID; level/star/material records use the independent armor ID namespace.

| Armor | Base HP | HP per level | Armor | Speed | Evasion |
|---|---:|---:|---:|---:|---:|
| Basic Armor | 100 | 10 | 10 | 0 | 0% |
| Light Armor | 50 | 5 | 5 | 0.5 | 5% |

At level 1 and zero stars, armor adds its listed base stats. HP is `(baseHP + HPPerLevel * (level - 1)) * (1 + 0.1 * stars)`. Armor, speed, and evasion remain fixed. Evasion adds percentage points to the tank's existing evasion. For example, level 2 / one star Basic Armor gives 121 HP; Light Armor gives 60.5 HP. Armor uses Parts for levels, its own Blueprints for stars, the existing costs and 20-star cap, and the same colored stars below icons.

All four properties add to a fresh copy of the tank definition. Equipping or upgrading at Home refreshes tank HP/defenses immediately. Each run recalculates the equipped contribution once and starts at full HP, so switching armor or retrying cannot stack it. The resulting armor and evasion enter the existing incoming-damage calculation, and speed enters tank movement. No armor visuals are attached to the tank itself; the generated icons are used in inventory, slot, and item details.

`ArmorDefinition` contains the four stats, HP per level, and a stable ID; new armor requires only a definition and a Frontend inventory entry. `MegaTanks > Configure Sample Armor` creates missing sample assets and references without replacing edited stats. `ArmorSetup.InstallAndGrant` also applies the initial 1,000-Blueprint allowance for each sample armor. A separate one-time armor allowance marker lets existing play-test saves receive the new materials without replenishing spent currencies; the manual resource top-up also covers armor.

The two transparent icons are in `Assets/MegaTanks/Art/Armor`; `SourceAssets.txt` records the built-in generation mode and exact prompts. `ArmorAudit.Run` checks stat values, both HP progressions, Parts/Blueprint purchases, additive tank stats, damage mitigation/evasion, persistence, real Garage filtering/equip/upgrade clicks, replacement and retry behavior. It restores player saves and captures the Basic and upgraded Light Armor screens under the validation project's `Checks/ItemProgression` folder.

## Boss tank balance

The final Boss route node spawns a single tank using the player's TankCutaway artwork, mirrored horizontally. It advances from the right, animates its own tracks, and fires its own MG, cannon, and mortar at the player. Killing it completes the stage. Boss spawning is restricted to active combat at the special Boss map node, including direct API calls; other encounters cannot spawn it, repeat calls cannot create duplicates, and completed encounters cannot respawn it.

Open **MegaTanks > Boss > Select Boss Template** or select `Assets/MegaTanks/Data/Boss/BossTank.asset`. Drag the same weapon, armor, accessory and crew definition assets used in the player inventory directly into its slots. Equipment is now an inline group of MG, Cannon and Mortar slots, rather than a separate boss loadout asset. The hull determines whether the Inspector shows one armor/two accessories or two armor/one accessory, matching the player. Five fixed crew slots use Gunner, Driver, Sub gunner, Weapon specialist and Commando roles. Templates contain no levels, stars or difficulty growth. Duplicate a template or use **Create > MegaTanks > Boss Tank Template** for another boss.

Select a stage asset under `Assets/MegaTanks/Data/Stages` and expand **Boss**. Choose its **Template**, then set separate levels/stars for **Machine Gun**, **Cannon**, **Mortar**, **Armor**, and each crew station. Crew progression matches the template's station names; unlisted stations default to level 1, zero stars. The stage Inspector previews total HP, armor, speed, crew damage and all three weapon damages. Two stages can share one template with different progression settings. New stages require an explicit boss template selection. Changes apply to the next boss spawn.

The heavy template is `Assets/MegaTanks/Data/Boss/Heavy/HeavyBossTank.asset`, available through **MegaTanks > Boss > Select Heavy Boss Template**. Both existing bosses now reference shared player weapons and crew. The heavy boss uses Basic Armor and Light Armor in its two slots; drag different player items into these slots to change its loadout. Stage Boss settings hold all item levels/stars, including both accessory slots and second armor. Accessory bonuses use levels, matching player behavior; stars do not add an accessory bonus. Reload speed and accuracy affect boss weapons, and evasion affects its hull. Shared assets are copied into runtime combat stats and never changed by boss progression or ammunition. Editing a shared item definition intentionally affects every player/boss loadout referencing it. **MegaTanks > Boss > Migrate Shared Equipment Slots** migrates old boss-only item references once, preserving later drag/drop choices and stage progression. Old boss-only assets remain on disk for reference. `SharedBossSlotsAudit.Run` validates shared inventory links, stage progression, accessory bonuses and runtime stat isolation.

Boss advance follows the player's all-weapons range rule: it advances until its MG, cannon, and mortar can all reach the player, then holds position. Each weapon uses its own muzzle position and runtime range, including vertical separation. The boss recalculates its destination as the player moves and resumes advancing if any weapon leaves range. It takes only the movement step needed to reach firing range, with no extra travel inside range. Ordinary enemies retain their personal stopping destinations. `BossTank.AllWeaponsInRange` and `AdvanceDistanceNeeded` expose the current range decision.

Starter tuning at stage-configured level 1 / zero stars: hull 300 HP plus Basic Armor's 100 HP and 10 armor; speed/scale match the player template. MG/cannon/mortar base damage is 3/12/20. Boss crew values and gains are zero; role-specific bonuses apply only to their associated weapon or hull. Boss power is independent of stage difficulty, stage number and round count. Equipment damage is `(baseDamage + level - 1) * (1 + 0.1 * stars)` with matching crew percentage bonuses applied afterward. Armor HP uses its existing HP-per-level formula. Weapon ammo, salvo timing, reload, range, ballistics, accuracy, penetration, and critical stats live on the corresponding Boss weapon assets. Boss stats and ammo never depend on player saves or selected equipment.

Runtime access: `StageDefinition.boss` / `BattleGame.BossSettings` expose the stage's template and progression; `BattleGame.BossProfile` exposes the selected template. `EnemyEntity.Boss` exposes its copied `Settings`, `Profile`, `Artwork`, copied weapon stats, ammo and `ShotsFired`. `BossEncounterSettings.CalculateTankStats()`, `CrewDamage`, and `WeaponDamage(baseDamage, progression)` provide balance calculations without spawning. `MegaTanks > Boss > Install Boss Tank` creates missing assets and assigns the existing template to stages without a selection; it preserves configured stages and template stats. The old scene-wide boss reference and difficulty growth are removed. `BossTankAudit.Run` checks alternate stage templates, per-slot progression, difficulty/round independence, runtime isolation, retry, final-node spawning, range stopping/resume, weapons/ammo, team damage, pause and victory. It saves `Checks/boss-tank.png` in the validation project.


### Currency icons

Parts, Pet Food, and Gems use generated transparent sprites under `Assets/MegaTanks/Art/Currencies`. Home, Garage, and Crew show each icon beside its currency name and live balance, on a dark backing for readability. Parts also appears on the Garage level-up button (including armor); Pet Food appears on the Crew level-up button. Gems has no purchase action yet.

The counters and cost icons are authored in their existing screen prefabs. `MegaTanks > Configure Currencies` imports the sprites and adds missing controls/bindings without duplicating badges on repeated runs. The old aggregate currency text binding remains available but does not render. `CanvasCurrenciesUI` updates each numeric counter through the existing wallet change event. Decorative images never receive pointer input and preserve sprite proportions. Sprite imports use alpha transparency, bilinear filtering, no mipmaps, uncompressed textures, and a 256-pixel size limit.

`SourceAssets.txt` records the built-in image-generation mode and exact prompts. `CurrencyAudit.RunWithUI` checks wallet behavior, exact sprite references, repeatable installation, live values on all three screens after purchases, and real upgrade-button pointer interactions. It restores the player's saved balances/progression and writes Home/Garage/Crew captures under the validation project's `Checks/ItemProgression` folder.


## Stage completion rewards

First clears also award account XP according to `100 + 10 × (stage number − 1)`. An account starts at Level 1, and advancing from its current level costs `100 + 25 × (current level − 1)` XP. Excess XP carries into the next level. Victory shows the first-clear XP and account progress. Home shows the account level. Existing first-clear records retain their eligibility status, and XP from new first clears saves alongside the clear history even when currency payment is deferred.

Stages now award persistent currencies automatically when the final boss is defeated. Every successful clear gives `100 + (stageNumber - 1) * 10` Parts and the same amount of Pet Food. The first clear additionally gives `500 + (stageNumber - 1) * 50` Parts, the same amount of Pet Food, and 100 Gems. The first clear receives both reward types.

| Stage | Every clear: Parts / Food | First bonus: Parts / Food / Gems | First-clear total: Parts / Food / Gems |
|---|---|---|---|
| 1 | 100 / 100 | 500 / 500 / 100 | 600 / 600 / 100 |
| 2 | 110 / 110 | 550 / 550 / 100 | 660 / 660 / 100 |
| 10 | 190 / 190 | 950 / 950 / 100 | 1,140 / 1,140 / 100 |

`StageDefinition` exposes the first-clear Parts base, Pet Food base, resource increment per stage, and Gems in the Inspector. The defaults are 500, 500, 50, and 100. Its Inspector also previews the calculated payouts. The repeat formula uses the stage number, independently of round count and combat difficulty. Keep stage numbers unique and stable: completion history uses this number, so renaming or reordering catalog entries preserves eligibility, while renumbering changes the stage identity.

The Victory prefab shows the every-completion reward, first-completion bonus, total earned, and payout status using the existing currency sprites. On replays, the first bonus is marked already collected and contributes zero. Rewards are paid by the boss-clear gameplay event, not by drawing the result screen or clicking Home/Play Again. Intermediate encounters still offer the existing run cards and scraps; defeat, pause, abort, and returning Home mid-run do not award permanent stage currencies. Repeated clear callbacks within a run reuse the same receipt.

`BattleGame.StageRewards` owns the local reward ledger at `MegaTanks.StageCompletionRewards`. It saves first-clear flags and earned entitlements before payment, then stages all three wallet credits and the removal of the pending entitlement before one PlayerPrefs save. If any currency would exceed the wallet's integer limit, the complete payout remains saved and is retried after spending or loading; it never partially credits or silently discards the first bonus. A fresh first-completion history starts when this feature is introduced; earlier runs without saved completion records are not inferred.

`MegaTanks > Configure Stage Rewards` adds missing authored Victory bindings and saves reward defaults into existing stage assets without changing their difficulty or round settings. `StageRewardsAudit.Run` checks formulas at stages 1, 2, and 10, per-stage overrides, first-clear persistence, duplicate callbacks, aborted runs, saved deferred payments, real final-boss clears, replay/Home raycasts, failure behavior, and victory screenshots. It restores player balances and first-clear eligibility afterward. Existing boss/route/victory audits also preserve these saves. Captures are in the validation project's `Checks/ItemProgression/StageRewards-First.png` and `StageRewards-Repeat.png`.

## Combat VFX

The six clips from Art/Asset/vfx/Tank_VFX_Unity.zip are integrated using all 48 original transparent 320 x 320 frames. MG bullets use animated tracers; cannon and mortar shells retain their projectile artwork and carry animated smoke. Player, enemy and boss weapons play muzzle flashes at their moving muzzle sockets. Light hits and ground contacts play sparks, cannon hits play explosions, and mortar impacts play explosions without lingering fire; destroyed ground vehicles can still leave short-lived fire. The ground-fire clip remains available for future weapons. Airborne destruction does not create fire on the road. The fire is decorative and does not add damage or change blast radii.

Assets/MegaTanks/Data/Resources/BattleVfx.asset configures effect sizes, fire height/duration and the active-effect limit. The battle loads this catalog automatically; an explicit catalog may also be assigned on BattleGame. Original frame order, pivots, frame rates and loop flags are preserved from Art/VFX/vfx_manifest.json. The earlier muzzle/exhaust artwork and its documentation are retained. MegaTanks > Import Tank VFX rebuilds sprite settings and clip references without resetting size/lifetime tuning.

Animations pause with combat pause. Final-hit explosions finish while rewards, victory or defeat are displayed. Stage transitions, restart and Home clear effects; attached effects disappear with their muzzle/projectile. Effect creation is capped, and bullets fall back to their original rendering if the cap is reached. BattleVfxAudit.Run imports and checks all six clips, real weapon firing and projectile ground contacts, pause, idle gun resets, final-enemy destruction and cleanup. Mortar and projectile-art audits cover the existing physical projectile behavior.

## Medium and Heavy tanks

The original player tank is named Medium Tank (100 base HP, 0.5 base speed). Heavy Tank uses the 11 supplied Tank_3_HP artwork layers, with 200 base HP and 0.25 base speed. Both use the current MG, cannon, mortar, crew, weapon upgrades, ammunition, aiming, recoil, combat VFX, stance and camera-follow systems. Heavy's four supplied road wheels rotate with terrain travel; its supplied track chassis stays fixed. Barrel pivots and all muzzle sockets follow the native artwork metadata.

In the Garage, use CHANGE TANK beside the title to cycle Medium/Heavy. The preview, live Home tank, base-stat readout and equipment layout update immediately. Selection is saved by stable tank ID and applies to the next battle; changing tanks during a deployed run is blocked. Medium has one armor and two accessory slots. Heavy has two armor and one accessory slot: Armor 2 replaces the last accessory slot. Weapons and primary armor are shared; the second armor and the displaced Medium accessory remain saved while inactive. Both Heavy armor plates contribute their upgraded HP, armor, speed and evasion; an item moved between armor slots swaps instead of being counted twice. Armor comparisons and equipped badges respect the active tank's slots.

Edit Assets/MegaTanks/Data/Tanks/HeavyTank.asset to tune Heavy HP, speed, scale and slot count; the Medium profile remains at Assets/MegaTanks/Data/Tanks/PlayerTank.asset to preserve existing references. Frontend.asset lists the selectable tank profiles. MegaTanks > Add Heavy Tank reimports the layered prefab and garage controls while retaining tuned stats. HeavyTankAudit.Run verifies selection, preview, save persistence, armor stacking, slot restoration, real three-weapon fire, idle poses, wheel animation, movement and camera follow, with captures under Checks/HeavyTank in the validation project.

## Auto Loader accessory

Auto Loader is the first functional accessory, available under Accessories in the Garage with its own generated transparent icon. Level 1 gives +10% reload speed to MG, cannon and mortar. Each further level adds 0.5 percentage points: level 2 gives 10.5%, level 3 gives 11%. Effective reload time is Base Reload Time / (1 + total Reload Speed / 100). Every weapon defaults to 100% Reload Speed; Auto Loader adds its bonus to that value. A 4-second base cannon reload is 2 seconds at 100%, or about 1.90 seconds with level-1 Auto Loader at 110%. Burst shot delay, ammo capacity and enemy/boss weapon stats are unaffected.

Level up Auto Loader using the existing Parts cost curve. Its level is saved by a permanent accessory ID. This first accessory has level upgrades only; stars do not modify its bonus. The Garage shows the current/next-level bonus and before/after reload times for all three weapons. A single accessory moves or swaps between slots instead of counting twice; only the active tank's available accessory slots contribute. An accessory in Medium's second slot remains saved but inactive on Heavy until moved into Heavy's available slot. Equipping and level upgrades refresh Home previews immediately, and run reload stats are copied from definitions at deployment.

Tune the base bonus and per-level increment in Assets/MegaTanks/Data/Accessories/AutoLoader.asset. The generated source PNG is retained in Assets/MegaTanks/Art/Accessories/AutoLoader.png. MegaTanks > Configure Accessories registers the item and icon while preserving tuned values. AccessoryAudit.Run checks real garage equipping and level-up, Parts spending, both tank layouts, permanent saves, all three reload times, unchanged burst/ammo/enemy stats, no duplicate/refresh stacking and unmodified source definitions.

## Targeting Computer and Camo Paint

Targeting Computer adds +10 percentage points of accuracy to all three player weapons at level 1. Camo Paint adds +10 percentage points of tank evasion at level 1, stacking with evasion from armor. Each gains +0.5 percentage points per additional level: 10.5 at level 2, 11 at level 3. Both use the existing Parts level-up costs and level-only accessory progression. Each has its own generated transparent garage icon and saved permanent item ID.

Accuracy and evasion use the existing combat formula: final hit chance = clamp(Accuracy - Evasion, 0, 100). Accuracy is retained above 100 to offset enemy evasion; the targeting bonus is copied into MG, cannon and mortar projectile attacks. Camo Paint affects incoming hit rolls through the tank's defense snapshot. Neither item changes reload speed. Bonuses apply only from active accessory slots, support swapping without duplicates, and refresh immediately at Home and on deployment. Garage details show the relevant current and replacement stats. Auto Loader keeps its existing saved ID, tuned values and reload behavior.

Tune the bonus type, base bonus and per-level increment in Assets/MegaTanks/Data/Accessories/TargetingComputer.asset or CamoPaint.asset. Configure Accessories imports all three icons and registers missing items without overwriting tuned definitions. CombatAccessoriesAudit.Run verifies generated icons, both level upgrades, saved selections/levels, actual projectile accuracy snapshots and hit/evasion calculations, Medium/Heavy slot limits, mixed accessory loadouts and definition immutability. AccessoryAudit.Run continues to cover Auto Loader.

## Weapon reload stats

Every player, regular-enemy and boss weapon exposes Base Reload Time and Reload Speed. Existing reload durations are retained as base values, and Reload Speed defaults to 100%. Battle reload time = Base Reload Time / (1 + Reload Speed / 100). A base time of 4 seconds yields 4 seconds at 0%, 2 seconds at 100%, and 1.6 seconds at 150%. Auto Loader adds its percentage-point bonus to the weapon's Reload Speed before this formula is evaluated; it does not rewrite the base duration.

The formula applies to MG/cannon post-salvo cooldowns and mortar cooldowns, including all three boss weapons. Burst Shot Delay remains a separate setting. Enemy Reload Speed has its own Grow value and follows the existing difficulty snapshot rules. The Garage displays Base Reload Time, total Reload Speed including accessories, and Battle Reload. Existing serialized base-time values retain their stable storage key for compatibility. MegaTanks > Configure Weapon Reload Stats saves newly defaulted speed fields while preserving tuned base times and speeds. ReloadSpeedAudit.Run checks default/custom speeds, enemy growth, actual player/enemy/boss cooldowns, Auto Loader stacking, labels and immutable source data.


### Crew roles and commando

Crew assets expose their role, associated slot, and relevant stat Value/Gain pairs in the Inspector. The Crew screen shows current stats and gains in a scrollable detail panel; incompatible Assign controls are disabled. Crew star bonuses still apply after level gains. The commando’s integrated base reload time uses only its level gain, so star upgrades do not slow firing.

The commando is an independent allied unit with HP, armor, speed, evasion, critical defenses and an integrated gun (damage, damage bonus, accuracy, penetration, critical stats, reload speed, base reload time, range and projectile speed). It deploys when a living enemy enters Activation Range measured from the main tank. It moves into weapon range, fires its own projectiles, can be targeted and damaged by enemies, and takes enemy mortar splash damage independently. It returns to the tank when no enemies remain within Activation Range, retaining HP. Defeat lasts until the next battle. Pause freezes combat; returning Home clears the unit. Zero HP or Activation Range prevents deployment. All commando numeric values and gains are zero, like the rest of the crew.

`CrewRoleAudit.Run` checks zeroed production assets, role guards, saved assignments and migration, per-stat gains and stars, isolated cannon/sub gun/gatling/mortar/hull effects, UI restrictions, and live commando activation, shooting, HP, retreat, death and new-run recovery. Temporary test values and player saves are restored afterward. Boss crew also use matching stations and per-stat gains for their equipment/hull.


### Warrior commando artwork

The deployed commando uses the original Spine 4.2.43 character from `Art/Anim/warrior_melee_export`. Its source texture, atlas and binary skeleton are copied unchanged into `Assets/MegaTanks/Art/Commando/Warrior`. `Prefabs/WarriorCommando.prefab` provides idle, walk, shovel_attack and death animations; its feet align to the battlefield ground, facing follows its target, and animation pauses with combat. The crew portrait remains the inventory image. Commando crew assets expose a Commando Prefab reference so future characters can be assigned separately. The asset changes appearance and animation; configured weapon damage/range/reload behavior is retained.

`CommandoArtImporter.ValidateAndCapture` imports and renders all four animation states and verifies that authored crew stats are unchanged. `CrewRoleAudit.RunWithAuthoredTuning` exercises deployment, walk/attack/death transitions, facing, outgoing and incoming projectiles, retreat and next-run recovery with temporary test values, restoring crew tuning and player saves afterward. No stat reset is performed by the artwork importer.


### Enemy boss commando

Boss crew Commando slots use the dog character from `Art/Anim/enemy_melee_export`, imported unchanged into `Assets/MegaTanks/Art/Commando/Enemy`. Both standard and heavy boss crew reference `Prefabs/EnemyBossCommando.prefab`; the player keeps its separate warrior prefab. Native Spine idle, walk, melee_attack and death animations drive the deployed character. The warrior importer excludes boss crew, so reimporting player artwork cannot replace enemy visuals.

The boss reads its matching Commando station and stage-configured crew level/stars into an independent hull and integrated weapon snapshot. A player tank or active player commando inside the boss activation range triggers deployment beyond the boss hull. The summon moves, attacks the player faction, and can be targeted by player guns, mortar and commando projectiles. It stows outside activation range without healing or losing cooldown, cannot respawn after defeat in the same encounter, and is removed when its boss dies. It grants no scraps or score, and boss cleanup occurs before encounter completion to prevent a stranded summon from blocking the route. Retry creates a fresh summon. Authored HP and Activation Range must be positive for deployment; importing artwork does not change stat values or gains.

`BossCommandoArtImporter.ValidateAndCapture` renders the original enemy animations and checks that crew tuning is preserved. `BossCommandoAudit.Run` uses temporary cloned crew and stage data to validate activation, stage crew growth, targeting and projectile teams, independent HP/damage, animation/facing, pause, stow/reactivation, defeat, boss cleanup, encounter completion and retry. Source assets and player tuning remain unchanged.


### Commando melee mode

Both player and boss commandos use melee when their effective Weapon Range is zero. They choose a reachable opponent, walk to its collider edge, stop just outside the body, and strike with the warrior shovel or enemy melee animation. Melee does not require projectile speed or ammunition and never creates projectiles. Positive Weapon Range retains the existing ranged weapon behavior. The Crew stat panel displays MELEE or RANGED, and the Inspector explains the range switch.

A melee swing snapshots the usual damage, damage bonus, accuracy, armor penetration, critical chance and critical damage. The hit resolves once at the authored animation hit event (or animation completion if an asset lacks a hit event), only while attacker and target are alive, deployed and still touching. Targets can evade a swing by moving away before impact; unreachable high flyers are ignored. Base Reload Time and Reload Speed determine melee cooldown, while an active swing must finish before another starts, including when Base Reload Time is zero. Pause freezes movement, animation and impacts. Death and stowing cancel pending strikes. Enemy melee can hit either the main player hull or its nearby deployed commando; friendly units are excluded.

`CommandoMeleeAudit.Run` validates both factions with temporary test tuning: approach and body-edge stopping, delayed single impacts, ordinary damage and critical calculations, moving-target misses, pause/resume, commando duels, tank melee, zero projectile speed, zero reload time, unreachable flyers, no melee projectiles, and positive-range projectile behavior. Authored crew values/gains and player saves are restored afterward.


### Commando HP bars

Deployed commandos on both factions show a compact HP bar above the character: cyan for the player and red for the boss commando. Fill tracks current HP divided by maximum HP, including damage from melee, projectiles and splash. Retreat and death hide the bar; redeployment preserves remaining HP, while a fresh run starts at full HP. Bars follow their unit without inheriting the Spine character flip and render above its artwork.

Both use the existing editable `Prefabs/UI/EnemyHealthBar.prefab`. The `EnemyHealthBarUI` Inspector exposes Commando Width and Commando Sorting Order separately from ordinary-enemy sizing. `CommandoMeleeAudit.Run` checks spawn visibility, faction colors, full/damaged fill, rendering order, stow/death visibility, reactivation and reset, and saves `Checks/ItemProgression/Commando-HP-Bars.png` in the validation project.

## Missile packs

AT Missile Pack and AA Missile Pack are available under **Garage > Mortar**, on both Medium and Heavy Tanks. They occupy the original mortar mount, stand upright, and fire straight upwards while retaining the main cannon. After launch, missiles bend toward their locked target using bounded steering. AT locks ground enemies only; AA locks flyers only. Each missile keeps its living target and reacquires the nearest active enemy of the same class within range after losing that target. Steering remains limited after retargeting, so a late change can miss. With no target it maintains its heading until reacquisition, ground impact or flight expiry. Wrong-class enemies cannot intercept the missile.

The editable definitions are `Assets/MegaTanks/Data/Weapons/MissilePackAT.asset` and `MissilePackAA.asset`. Tuning: speed 12, steering 240 degrees/sec, flight lifetime 5 seconds, two-shot salvo with 0.3-second spacing, 6-second Base Reload Time, 100% Reload Speed (3-second battle reload), and 60 ammo. The vertical-launch arc uses a tighter turn limit than the former direct-fire setup so missiles can reach ground targets within their range. AT has 24 base damage, 25% penetration and range 12; AA has 16 damage, zero penetration and range 14. Both use normal progression, Weapon Specialist crew stats, accessories, combat damage calculations, saves and ammo resupply. Switching back restores the standard mortar.

The original launcher icons, mount artwork and missile sprite are under `Art/Missiles/`; `SourceAssets.txt` records their sources. Existing muzzle, looping smoke and explosion effects are reused, keeping the missile body visible and avoiding persistent fire on ground impact. Main's garage stat rows include target class, turn rate and flight lifetime, and its battle HUD identifies mortar-slot missile ammo.

**MegaTanks > Configure Missile Packs** restores missing definitions, bindings, prefabs and Garage entries without resetting existing tuning. `MegaTanks.Editor.MissilePackAudit.Run` validates both tank mounts, garage equip/persistence, retained main cannon, mortar restoration, launcher ammo/reload, same-class collisions/locks, retargeting, turn limits, a late miss, burnout, pause and source-asset immutability. Timestamped captures are under the validation project's `Checks/MissilePacks/`. Main's native combat, camp flow, garage presentation and vehicle travel animation remain in place.
