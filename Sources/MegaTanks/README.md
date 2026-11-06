# MegaTanks

## Canvas UI and front-end flow

Design reference: [MegaTank UI slides](https://docs.google.com/presentation/d/1wYHK3rKs4jGDTON6XZ8SG-Dip3ueG96ekwUVsSqcGwk/edit). The three reference slides are saved in `Reference/UI`; `Reference/UIDesign.url` pins the source. Their wireframes are adapted to the existing landscape 1920 x 1080 game.

The main scene contains a **MegaTanks UI Canvas** and **UI EventSystem**. `CanvasGameUI` builds native uGUI Images, Text, Buttons, Sliders, Toggle, ScrollRects and masks beneath that Canvas when the scene starts. All immediate-mode GUI rendering has been removed from gameplay scripts. The Canvas scales inside the device safe area; an isolated camera renders the existing tank artwork into the equipment avatar, without adding combat entities to the battlefield.

- **Home:** avatar/menu button, stage badge, Previous/Next buttons and swipe selection, Play, and Crew/Equipment/Tech buttons. Crew and Tech are explicitly future placeholders. Only Stage 1 currently exists, so the stage arrows stay disabled at its boundaries. Additional stage definitions can be added to the front-end catalog.
- **Equipment popup:** mortar plus Accessory 2 and Accessory 3 on the left; MG, cannon and armor on the right; a tank avatar and connector arrows; a scrollable six-column inventory; All/Cannon/MG/Mortar/Armor/Accessories filters; ordering by name, type or damage, ascending/descending; and item inspection/equip controls. MG/cannon/mortar choices are saved locally and configure the next run without changing definition assets. Every occupied slot shows its equipped item icon. Green check icons mark all currently equipped items in the inventory. The remaining accessory slots retain their existing saved slot-2/slot-3 identities; the former slot-1 accessory is no longer equipped.
- **Settings:** independently saved sound/music sliders and a vibration switch, with a dark dismissible backdrop and close button. `GameAudioSettings` on an AudioSource applies its saved Sound or Music channel level; no sound/music assets have been invented. Android player-hit vibration respects the switch and is rate limited.
- **Battle:** Canvas ammo/capacity bars, scraps, score, encounter/wave, hull health, floored floating damage, red selected Aggressive button and Defensive control. Pause offers Resume, Settings and Return Home; leaving an active run asks before discarding run progress.
- **Progression:** reward cards, card shop, supply/repair services, reveal, inter-wave, route-map icons/connections, defeat and victory all use Canvas controls. Reward selection still leads to the next-node map. Victory/defeat offer Play Again and Home.

`Data/UI/Frontend.asset` lists the stages and available equipment. `EquipmentDefinition` is the shared item base; `WeaponDefinition` derives from it while retaining existing weapon profiles. `ArmorDefinition` and `AccessoryDefinition` supply item types only: no armor/accessory item instances, stats or effects are created. Their inventory filters show a clear empty state. New definitions are available through **Create > MegaTanks > Equipment**.

**MegaTanks > Configure Canvas UI** creates missing setup/catalog references and preserves existing catalog lists. `CanvasUIAudit.Run` exercises real EventSystem raycasts through Home/settings/equipment/filter/equip/Play, pause/resume, rewards/map/node selection and Home, restoring settings after the check. `.Capture` additionally produces internal layout checks when explicitly needed.

### Layer Lab UI artwork

The Canvas UI uses 28 selected sprites from the supplied **GUI Pro-SuperCasual / Layer Lab** archive: [original ZIP on Drive](https://drive.google.com/file/d/1dj89xXo3nJAiK5Ygg3X5FyMNytJU0Zta/view). Only the selected button, popup, item-frame, slider/switch, bar and control-icon artwork is included in `Assets/MegaTanks/Art/UI/LayerLab`; `SourceAssets.txt` records their original archive paths. The original sprite slice borders are retained for scaling, with uncompressed bilinear imports and no mipmaps. Demo scenes, demo scripts, fonts and unrelated item artwork are excluded; the skin works with the existing uGUI controls and font.

`Data/UI/LayerLabSkin.asset` holds the sprite references and is assigned to the main scene's Canvas controller. Gold marks Play/purchases, blue/sky marks primary equipment/actions, dark frames support navigation, mint marks selected Defensive and red marks selected Aggressive. Navy popup panels, item frames, equipped checks, pause/close/menu/arrows, independent audio sliders and the vibration switch use the pack artwork. Slot item icons, tank artwork, card art and white/blue/purple/orange card rarity borders are unchanged.

**MegaTanks > Import Layer Lab UI Skin** configures these imports, rebuilds the skin references and assigns the scene. `CanvasUIAudit` also checks actual skin sprite bindings, sliced inventory frames, preserved borders, equipped-marker sprites and settings artwork while exercising real Canvas raycasts. Screen layouts remain built by the Canvas controller; this changes their artwork rather than adding new game systems.

**Pinned design source:** [Card effects and shop prices — Google Sheets, Cards tab](https://docs.google.com/spreadsheets/d/1JoIspgth7KeGzcHgBdwluRhmUyCh7FG83wH2ZbsP9iw/edit?gid=0#gid=0). A Windows shortcut is also saved at `Reference/CardEffects.url` in the repository. The game uses the checked-in snapshot and needs no network access.

## Ammo, rewards and shop

The current scene starts each run with **150 MG bullets**, **50 cannon rounds**, and **20 mortar shells**. Every salvo shot consumes one round; reload delays never refill reserves. Empty weapons remain silent until a resupply card restores ammo. Reserve capacities are editable in the separate weapon definition assets.

After clearing a battle node, combat stops for a choice of **one of three free cards**, followed by the route map. Hard battles give this choice after all three waves. Supply and repair nodes provide free/paid service cards; the previous automatic every-fifth-wave shop is replaced by node progression. Unspent scraps, ammunition, hull and upgrades carry across nodes. Restart resets the run. Boss victory ends the stage on a victory screen.

Each offer rolls **Legendary 5% / Epic 15% / Rare 30% / Normal 50%**, then draws uniformly from that rarity's unused cards. All three reward choices have distinct card IDs; different rarities of the same effect remain different cards. Once a rarity runs out of unused cards, its odds are redistributed proportionally among the remaining rarities for that offer set. Cards can appear again in later waves. The catalog contains all 18 sheet rows with their exact percentages and prices: Normal 100, Rare 200, Epic 500, Legendary 1000 scraps. Legendary currently has damage/max-HP cards only; no extra resupply/repair entries are invented.

- Damage upgrades add a percentage of the selected gun's starting damage, stacking additively for this run: two +10% cards give +20% total.
- Max-HP upgrades add a percentage of starting maximum HP (100), stacking additively. The added capacity also increases current HP, preserving existing missing HP. Restart resets damage and HP upgrades.
- Repairs add the card's percentage of maximum HP, rounded up and capped.
- Resupply adds the card's percentage of each full reserve, rounded up and capped: with the current capacities, Normal restores +50 MG/+17 cannon/+7 mortar; Rare +99/+33/+14; Epic fills all reserves.
- Flyer scraps start at **20 on wave one**, then +2 per subsequent wave. Ground scraps start at **30**, then +3 per subsequent wave. Both values are in the enemy data manager. Rewards use each enemy's own wave number.

`Assets/MegaTanks/Data/Cards/CardCatalog.asset` manages card definitions and images. `SheetSnapshot.json` records the read-only sheet snapshot dated 2026-11-03. To incorporate future sheet edits, update the snapshot and run **MegaTanks > Import Card Catalog**. The importer validates names and all rarity pools. It updates the catalog and scene reference without changing other battle settings.

`Art/Cards/CardAtlas.png` is a generated 18-card atlas sliced into individual sprites. Icons represent cannon damage, MG damage, tank HP, ammo supply and repair. Frames are white/blue/purple/orange by rarity, with foil detailing and a subtle animated sheen on Legendary cards. UI text is rendered by the game so values remain accurate after data changes. `ProgressionAudit.Run` verifies ammo, effects, scraps, rewards, shops and reset behavior without rendering previews.

Open this folder with Unity **2022.3.62f3 LTS**. Open `Assets/MegaTanks/Scenes/MainBattle.unity`, choose a **1920 x 1080 landscape** Game view, enter Play mode, then select **Play** on the Home screen.

The Android game is locked to landscape. HUD and safe-area scaling use a 1920 x 1080 design reference and adapt to the device's actual resolution. The tank uses the current **1.2x** uniform scale, with its treads anchored to the ground. Adjust **Scale** in `Data/Tanks/PlayerTank.asset` to tune it further.

The 2D tank uses the supplied cutaway art. MG/AA is on the roof, the main cannon is in the upper cabin, and the mortar is mounted on the left. Enemies approach from the right. Defensive and Aggressive buttons replace the ultimate placeholders; subroom and floating gun labels have been removed.

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

Car wheels rotate while advancing, including while firing, and stop at the chosen destination. The plane plays the supplied twelve-frame side-view propeller animation at 40 ms/frame; its landing wheels stay still in flight. Ground vehicles are aligned to the ground, health bars sit above the models, and box hitboxes fit their visible bounds. **Artwork Width** in each enemy definition sets the base width (car 2.2 units, plane 2.4 units). **Scale**, set to **1.25** for both enemies, multiplies that width, giving a 2.75-unit car and a 3-unit plane. Hitboxes, muzzle positions, grounding and wave spacing follow the scaled artwork. Change Scale in `Assets/MegaTanks/Data/EnemyDataManager.asset` to tune each enemy type before spawning it.

The supplied body images include fixed guns. Shots originate at their illustrated muzzle sockets, while ballistic aiming and the existing MG/cannon stats control the projectile direction. The body art is not rotated to aim. **MegaTanks > Import Enemy Vehicle Art** rebuilds the generated prefabs and updates only artwork references/widths in the existing enemy definitions, preserving combat and reward tuning.

Enemies begin firing as soon as they enter weapon range, then continue horizontally for a random **1-5 units** before stopping. Each enemy picks its distance once when spawned, keeping its destination stable and spreading the firing line. Tune **Advance Inside Range** (X = minimum, Y = maximum) per enemy definition. Destinations are clamped to the tank's X position for very short weapon ranges.

Select `Assets/MegaTanks/Data/EnemyDataManager.asset` to manage enemy definitions in one place. The battle scene references this asset. Each entry has an ID, enemy kind, health and movement scaling per wave, score reward, color, and its own weapon stats.

| ID | Weapon | Damage | Range | Salvo | Shot delay | Reload | Speed | Gravity |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| FlyingEnemy1 | MG | 2 | 30 | 3 | 0.1 s | 1.8 s | 22 | 0.35 |
| GroundEnemy1 | Cannon | 8 | 40 | 1 | 0.1 s | 4 s | 18 | 3 |

These are starting balance values. Enemies fire upon entering their weapon range, advance another 1-5 units, then hold position and aim at the hull. They no longer ram or deal contact damage. Their movement prediction reports zero velocity while stopped. Enemy fire uses the same ballistic flight, ground destruction and range limits as player fire. Enemy projectiles hit only the tank hull; player projectiles hit only enemies. Cannon-type enemies share the cannon projectile sprite.

The existing wave spawner uses the first definition matching each enemy kind. Additional named entries can be spawned explicitly through `SpawnEnemy(definitionId, position)`. **MegaTanks > Set Up Enemy Data** links the manager to the scene and creates a default asset only if one is missing, preserving an existing asset's tuning.

## Draggable battlefield camera

The battle uses one full-width orthographic camera, showing about 30.9 world units horizontally at 1920 x 1080. The second enemy camera, split divider and nearest-enemy view label have been removed. Drag left/right across the battlefield with a finger or mouse to grab and pan the world. Vertical movement has no effect; camera height, depth and zoom stay fixed. There is no automatic tank/enemy tracking or inertia.

Camera-center X is clamped between the original player spawn and the current wave's front enemy spawn. The current scene's **Enemy Spawn Distance** is 50 units (preserved from the existing tuning); it remains editable on the battle object. Formation members spawn behind that front point. Moving enemies and the advancing tank do not move the camera bounds during a wave. A new wave updates the right bound using the tank's current position plus the configured distance, while the left bound stays at the original player spawn. Wave changes retain the player's chosen camera position; stage start and restart frame the tank near the left edge, at 18% of the viewport width. **Tank Start Viewport X** on the battle object controls this placement. This changes only initial framing; the spawn limits and manually chosen position during play remain unchanged.

A transparent Canvas battlefield surface handles touch and mouse dragging underneath the HUD. HUD controls, popups, pause, rewards, route/service screens, defeat and Home block panning. Only the first active drag pointer is used. Backgrounds tile and parallax across the single viewport. Floating damage appears only when in view, and the tank hull bar hides when the tank is off-screen rather than sticking over the enemy formation.

`CameraPanAudit.Run` verifies the full-width camera, mouse/touch input, pixel-to-world drag distance, spawn clamps, fixed horizon, UI blocking, manual position retention during movement/wave changes, restart and Home. `.Capture` additionally writes two internal layout checks at the spawn limits. `EnemyCameraAudit.Run` and the historical smoke-test entry point now route to this current camera audit.
## Tank art

`Assets/MegaTanks/Prefabs/TankCutaway.prefab` is an editable assembly of all 39 supplied PNG parts. It preserves the original positions, custom pivots, group hierarchy and layer order at 180 pixels per unit. The original archives remain outside the Unity project in the repository's `Art` folder. PSD/ORA files and preview GIFs are not included in player builds.

- `Art/Tank/parts`: individually imported sprites.
- `Art/Tank/metadata`: source assembly information.
- `Art/VFX/sheets`: three sheets sliced into eight sprites each, with the supplied per-frame pivots.
- `TankArtwork`: subtle hull vibration and a fixed pool of three overlapping exhaust animations. Road wheels rotate while advancing and stop while stationary.
- `GunArtwork`: aiming, barrel recoil and muzzle flash replay for every shot. Muzzle sockets follow the moving barrels; projectile spawn positions use those sockets.
- `SpriteClipPlayer`: frame playback using gameplay time, so effects pause with combat. MG flash restarts for every salvo round, then finishes its smoke tail after the final shot.

The main cannon uses the supplied seven-degree art alignment correction. Its mantlet is fixed to the tank body; only the barrel rotates around its base and recoils, carrying its muzzle socket and effect. Gun aiming remains unrestricted; extreme rotations may still reveal limitations in the side-view art. The target hit effects and projectile shapes remain prototype visuals; the supplied cannon effect is used only at the muzzle.

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

Tune `Data/Weapons/Mortar.asset`: damage 60, reload 6 seconds, range 80 units, launch angle 70 degrees, gravity 12, 20 reserve shells, deviation 1-3 units, full-damage radius 2.5 and half-damage radius 5. These are initial editable balance values. Mortar ammunition has its own top HUD counter and is restored by the existing resupply cards along with MG/cannon reserves. Existing cannon/MG damage cards continue to upgrade their named weapons; the mortar has independent damage settings.

The mortar prefers the largest ground formation within range, falling back to flyers, and leads movement before sampling a left/right horizontal offset of 1-3 units once per shot. Launch speed is solved for the configured steep angle (clamped to 46-85 degrees); the ordinary low-arc gun solver is not used. Shells can travel above the visible camera area. Range gates target selection; the curved shell path is allowed to complete its descent. Shells explode on enemy or ground contact, never on the tank. Flying targets must be struck by the shell or be inside a blast; misses do not detonate in midair.

AoE uses 2D distance from the impact to enemy centers: distance <=2.5 takes full mortar damage; 2.5<distance<=5 takes half; beyond 5 takes none. Inner targets receive full damage once, without an additional half-damage hit. Explosions affect all living enemy types, preserve kill/scrap rewards, and generate ordinary floating damage labels. Two translucent pulses indicate the blast zones. Flight, recoil and reload pause with combat; empty reserves require resupply, and restart resets the mortar. `MortarAudit.Run` covers high arcs, scatter, AoE boundaries/multi-kills, physical impacts, ammo, resupply, pause and reset without generating previews.
## Tank stances

The tank retains its starting world X = -7.3. The single camera frames this spawn at 18% of the viewport width at stage start; its horizontal position is then controlled by battlefield dragging.

The supplied `Art/Anim/chain track` rig now runs through the official **Spine 4.2** runtime using `SkeletonAnimation`, its path constraint, 38 bones and 36 chain links. The original 3.8.99 binary cannot be read directly by 4.2, so `TrackSpine42Converter` converts this specific rig to compatible JSON, preserving its bones, slots, region transforms, closed path, spacing and idle/running timelines. Original art files remain untouched. The converted JSON, original packed atlas/texture, material and SkeletonDataAsset are in `Assets/MegaTanks/Art/Tracks/Spine42`. `Data/Tracks/TankTracks.asset` references the Spine asset and retains sampled geometry only for import metadata and travel-length measurement; it no longer drives individual link transforms.

Spine playback follows actual tank travel and artwork scale, alongside the road wheels. The track attachment is fitted to 103% width and 89% height about its belt center: the ends meet the grey backing while the accepted top/bottom wheel fit stays unchanged; `TrackAnimationImporter.TrackFitScale` retains this fit during reimports. Playback compensates for the attachment scale. Defensive stance, automatic stops, pauses and noncombat screens hold the current pose; restarting resets the loop. **MegaTanks > Import Track Animation** rebuilds the compatible JSON, import settings and tank prefab; full tank artwork imports retain the Spine attachment. `TrackAnimationAudit.Run` verifies the 4.2 runtime, bone count, placement, movement timing, idle/pause behavior and restart without producing previews.

The official runtimes are embedded under `Packages/com.esotericsoftware.spine.spine-csharp` (**4.2.37**) and `Packages/com.esotericsoftware.spine.spine-unity` (**4.2.121**) so Git carries the installation. Source: [Esoteric Software spine-runtimes, branch 4.2](https://github.com/EsotericSoftware/spine-runtimes/tree/5cb78e77d3711ddcd80c7bd043bb11cf70fcc88c), pinned snapshot `5cb78e77d3711ddcd80c7bd043bb11cf70fcc88c`. Both packages retain the upstream Spine Runtimes license. Built-in Unity UI, animation and physics modules are dependencies of the official plugin; gameplay continues to use 2D physics.

The tank starts in **Defensive**, which holds its world position while weapons continue firing automatically. **Aggressive** advances right at **0.5 world units/second**, stopping as soon as the MG, cannon and mortar each have a living enemy in their configured range. Each weapon can have a different target; range checks use the same mount/muzzle positions as their targeting logic. This is a range check, independent of ammo/reload or whether a ballistic shot is currently reachable. Aggressive stays selected and resumes advancing if a later target/wave is out of range. Empty battlefields, pause, reward/shop screens and defeat halt movement. If no enemy is vertically reachable by one of the weapons, advancing cannot fix that, so the tank holds position.

Set **Advance Speed** in `Data/Tanks/PlayerTank.asset` to tune movement. Movement carries the entire tank, weapon sockets and hull hitbox; the hull bar follows it when visible, while the camera retains the player's chosen pan position. Background layers scroll and repeat as the camera pans. Enemy shots lead the tank's movement. New waves spawn relative to the tank's current position. Restart restores the starting position and Defensive stance. Road wheels roll only while moving. `TankStanceAudit.Run` checks speed, stance switching, all three weapon ranges, pause/rewards, cameras, hitbox attachment, wheel motion and restart without previews.
## Stage routes

**Reference:** https://drive.google.com/drive/u/0/folders/17Wgdfne4Ht7xl1mzbyKYfOtbUulWCoTz (also pinned in `Reference/StageMap.url`). The landscape map progresses left to right. `Assets/MegaTanks/Data/Stages/Stage1.asset` configures the stage name, number of rounds (10), node-type weights, hard-wave break duration, and optional fixed map seed. The battle scene references this asset; additional stage definitions can be created through the asset menu.

Round 1 is one battle node and starts automatically. Every intermediate round randomly contains 2-4 nodes; the last round contains one boss. Each node connects to at least one nearest next-round node, extra connections are allowed when they do not cross, and every node has an incoming path. Every penultimate node connects to the boss. Only next-round nodes connected to the completed current node can be selected. The small green tank marks the current node, cyan paths indicate the next available choices, and visited paths turn green. Wide stages scroll horizontally. Node symbols are drawn with scalable native UI shapes: crossed swords, marked hard-battle swords, an ammo crate, a repair cross, a question mark, and a crowned skull.

- **Battle:** one enemy wave, one three-card reward choice, then the map.
- **Hard battle:** three consecutive waves with no intermediate rewards or route choices, then one three-card reward choice and the map. Ammo, hull and reload state carry between waves.
- **Supply:** one free Normal Resupply order and one distinct paid Resupply order, each available once. Purchases use the catalog scrap price. Leaving returns to the map.
- **Repair:** the same service flow using Repair order cards.
- **Unknown:** remains a question mark on the map until entered, then randomly reveals Battle, Hard battle, Supply or Repair with equal odds. A reveal panel confirms the encounter before it begins. Boss is excluded because it is exclusive to the last round.
- **Boss:** one placeholder enemy wave, followed directly by the victory screen and Play Again.

Paid service rarities are rolled with the existing 50/30/15/5 weights among available effect-specific cards, excluding the free card ID. The current catalog has no Legendary resupply/repair card and only one Normal variant per effect, so current paid offers are Rare or Epic with normalized 2:1 weights. No new card effects, prices or Legendary definitions are invented. Free and paid offers remain distinct, preventing duplicate offers within a service node.

Stage rounds and combat waves are separate counters. Enemy health, numbers and scrap scaling continue using the total combat-wave counter, including hard-battle waves; service visits do not increment it. Movement and combat stop on the map, reward, service, reveal, inter-wave and victory screens. `StageProgressionAudit.Run` verifies 1000 maps plus all node flows and purchases; `ProgressionAudit.Run` covers card/ammo behavior in the new route flow. `StageScreensAudit.Run` is the explicit internal visual-QA entry point for the map, service, reveal and victory screens.
## Battle backgrounds

`Art/Asset/BG/industrial_bg_6_layers.zip` and `parallax_bg_7_layers.zip` supply the Industrial (6 layers) and RuinedCity (7 layers) environments. Runtime PNG layers and the original parallax configurations are in `Assets/MegaTanks/Art/Backgrounds`. The prototype grid/ground line are replaced by these environments; gameplay ground height and collisions remain at the existing plane.

Each new battle node (normal, hard, revealed combat, or boss) independently chooses one environment with equal probability. Consecutive battles may select the same background. A hard battle retains its choice for all three waves. Supply/repair visits and reward/map screens do not reroll the environment; restarting the run selects again for round one.

`Assets/MegaTanks/Data/Backgrounds/BattleBackgrounds.asset` stores the sprites and depth factors. One set of environment layers follows the main battle camera. Sky stays screen-fixed; farther layers scroll more slowly and the road follows world movement. Horizontally repeated layers alternate mirrored tiles as recommended by the supplied metadata, avoiding gaps and mismatched border edges. The full 1920x1080 sprite canvases and aspect ratios are preserved; the camera crops to its viewport, and static sky expands uniformly to cover extra-wide screens. All artwork remains behind gameplay entities. Text shadows keep battlefield labels readable over bright art.

**MegaTanks > Import Battle Backgrounds** rebuilds import settings/catalog references and assigns two free camera-layer names without replacing scene tuning. `BattleBackgroundAudit.Run` verifies canvas preservation, single-camera layers, parallax, mirrored tiling, wide-screen coverage, random encounter selection and hard-wave retention. `.Capture` additionally renders both environments for explicit internal visual QA.
## Separate tank and weapon profiles

All existing player tuning has been migrated out of `BattleGame` into independent definition assets, preserving the current values. No armor, regeneration, critical-hit or other new stat mechanics are added in this change.

- `Assets/MegaTanks/Data/Tanks/PlayerTank.asset`: base maximum hull 100, advance speed 0.5, scale 1.2. This asset contains no weapon stats or weapon references.
- `Assets/MegaTanks/Data/Weapons/MachineGun.asset`: target priority, damage, range, salvo size, shot delay, reload time, projectile speed/gravity, reserves, projectile kind and optional sprite/width. Current tuning remains 6 damage, 35 range, three-round salvos, 0.1-second shot delay, 1.25-second reload, 18 projectile speed, 0.35 gravity and 150 rounds.
- `Assets/MegaTanks/Data/Weapons/Cannon.asset`: the same gun settings, independently configured. Current tuning remains 45 damage, 50 range, one-round salvos, 0.1-second shot delay, 4-second reload, 18 projectile speed, 3 gravity, 50 rounds and the supplied shell sprite at 1.5 units wide.
- `Assets/MegaTanks/Data/Weapons/Mortar.asset`: current mortar damage/timing/range, steep launch angle, gravity, reserves, deviation, both AoE radii, shell size and projectile sprite. Current values remain 60 damage, 6-second reload, 80 range, 70-degree launch angle, 12 gravity, 20 shells, 1-3 deviation, 2.5/5 radii and 0.45-unit shell width.
- `Assets/MegaTanks/Data/Weapons/StartingLoadout.asset`: references the three starting weapon definitions separately from the tank definition. The battle object references the tank profile and starting loadout; it retains encounter, camera, stage, background and artwork references.

`WeaponDefinition` is the shared weapon identity base; `GunDefinition` and `MortarDefinition` provide type-appropriate settings. Weapons copy their definition values into independent runtime state, so ammo usage, upgrades, and temporary adjustments cannot modify saved tuning or another weapon. HP and damage cards remain additive against the selected profiles' base stats. Restart loads fresh snapshots of the tank and starting loadout, restoring their configured capacities and base values. A weapon can be configured from another compatible definition without changing the tank or starting loadout; equipment selection UI and replacement-art logic remain future work.

Create additional definitions/loadouts through **Create > MegaTanks > Tanks/Weapons**. **MegaTanks > Migrate Player Stats To Profiles** creates missing assets from legacy scene tuning and preserves existing profiles on subsequent runs. The original pre-migration scene tuning is retained in `Reference/BattleBeforeStatsSeparation.unity.txt`. `StatsProfileAudit.Run` verifies runtime/profile isolation, unchanged starting values, additive local upgrades, compatible alternative definitions, and restart behavior without generating previews.
