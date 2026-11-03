# MegaTanks

Open this folder with Unity **2022.3.62f3 LTS**. Open `Assets/MegaTanks/Scenes/MainBattle.unity`, choose a **1080 x 1920 portrait** Game view, press Play, then select **Start Battle**.

The Android game is locked to portrait. HUD and safe-area scaling use a 1080 x 1920 design reference and adapt to the device's actual resolution. The tank is enlarged uniformly by **1.25x**, with its treads anchored to the ground. Adjust **Tank Scale** on the scene's battle object to tune it further.

The stationary 2D tank uses the supplied cutaway art. MG/AA is on the roof; the main cannon is in the upper cabin. Enemies approach from the right. Subroom labels and five ultimate slots remain placeholders.

## Gun settings

Select **MegaTanks 2D Battle** in the scene to tune the two weapons independently.

| Gun | Damage | Salvo size | Shot delay | Reload after final shot | Max range |
| --- | ---: | ---: | ---: | ---: | ---: |
| MG/AA | 6 | 3 | 0.1 s | 1.25 s | 50 units |
| Main Gun | 45 | 1 | 0.1 s | 1.4 s | 80 units |

Range is measured from each weapon's mount to the enemy's center. Shells expire at a radius matching the gun range around their launch point; the extra length of a curved path does not shorten engagement range. Range, spawn distance and enemy-camera speed are editable on the battle object.

Bullets use downward gravity: **MG Projectile Gravity = 0.35** and **Main Gun Projectile Gravity = 3**, in world units/second squared. Tune these separately on the battle object; zero restores straight flight. Both guns compensate for gravity and lead moving targets using the lower reachable arc. Small collision-sweep steps follow the curve accurately, and fading tracers make its direction visible. Increasing gravity too far can make distant targets physically unreachable at the current 18-unit/second launch speed; a gun waits until it can solve a shot instead of wasting a salvo.

Cannon rounds use the supplied transparent `Art/Projectiles/CannonShell.png` sprite at **0.65 world units wide** (approximately 38 pixels at the reference camera zoom), with its original proportions and colors. Its nose follows the current flight direction as gravity bends the trajectory. Adjust **Cannon Projectile Width** on the battle object to tune the visual size. MG visuals and existing collision radii are retained.

Bullets and their tracers are destroyed as soon as the collision circle's lower edge touches the ground. Ground contact is swept along each ballistic step, so fast rounds cannot skip through the ground.

## Split battlefield

The portrait battlefield is divided into equal left/right views, each showing 9.2 world units horizontally at the reference resolution. The left camera stays on the tank. The right camera initially centers horizontally on the nearest enemy, then follows the nearest living enemy horizontally at **3 world units/second**, including transitions after a kill. Its height stays locked to the tank camera, keeping the ground aligned across both views. Distance is measured from the tank anchor; tracking motion pauses with combat. Both views show the same world, so shells can travel through the unseen space between them.

When the tracked enemy belongs to a different wave, the enemy camera catches up at **10 units/second**, then returns to **3 units/second** after reaching the new wave. Both speeds are configurable on the battle object. The previous wave is remembered through the gap between waves; restarting a battle resets this tracking state.

The front of each wave starts **90 horizontal units** to the right of the tank anchor, with the remaining formation spaced behind it. Enemies advance at their existing speeds; weapons wait until enemies enter their respective ranges. The right view displays the nearest enemy's distance from the tank.

MG/AA prioritizes flyers; Main Gun prioritizes ground enemies. Each falls back to the other type if needed. Pause uses the HUD button or Escape. Returning from the background pauses the battle. Hull destruction offers a restart.

## Tank art

`Assets/MegaTanks/Prefabs/TankCutaway.prefab` is an editable assembly of all 39 supplied PNG parts. It preserves the original positions, custom pivots, group hierarchy and layer order at 180 pixels per unit. The original archives remain outside the Unity project in the repository's `Art` folder. PSD/ORA files and preview GIFs are not included in player builds.

- `Art/Tank/parts`: individually imported sprites.
- `Art/Tank/metadata`: source assembly information.
- `Art/VFX/sheets`: three sheets sliced into eight sprites each, with the supplied per-frame pivots.
- `TankArtwork`: subtle hull vibration and a fixed pool of three overlapping exhaust animations. Wheels stay still while the tank is stationary.
- `GunArtwork`: aiming, barrel recoil and muzzle flash replay for every shot. Muzzle sockets follow the moving barrels; projectile spawn positions use those sockets.
- `SpriteClipPlayer`: frame playback using gameplay time, so effects pause with combat. MG flash restarts for every salvo round, then finishes its smoke tail after the final shot.

The main cannon uses the supplied seven-degree art alignment correction. Its mantlet is fixed to the tank body; only the barrel rotates around its base and recoils, carrying its muzzle socket and effect. Gun aiming remains unrestricted; extreme rotations may still reveal limitations in the side-view art. The target hit effects and projectile shapes remain prototype visuals; the supplied cannon effect is used only at the muzzle.

**MegaTanks > Import Tank Artwork** regenerates the prefab from the included metadata and links it to the scene while retaining gun settings. It replaces edits made directly to the generated prefab; use a prefab variant for custom changes. **Create Main Battle Scene** replaces the scene with a new bootstrap scene and resets its settings.

## Validation

Generate previews only when requested or when necessary to verify a change; routine gameplay checks run without rendering previews.

Close this project in Unity before running a separate batch instance:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' -batchmode -nographics -projectPath 'D:\SeagullGame\MegaTanks\MegaTank\Sources\MegaTanks' -executeMethod MegaTanks.Editor.MegaTanksSetup.SmokeTest -logFile 'D:\SeagullGame\MegaTanks\MegaTank\Sources\MegaTanks\smoke.log'
```

Success logs contain `MEGATANKS_SMOKE_OK`. The check covers imported art, VFX frames, gun sockets, actual projectile hits, salvo timing, target priorities, waves, pause, defeat and restart. `MegaTanks.Editor.MegaTanksSetup.CapturePreview` produces `Preview/MainBattle.png` using a graphics-enabled Unity instance.

Portrait mobile settings are configured. **MegaTanks > Apply Portrait Settings** reapplies the orientation and reference resolution without replacing the scene. Install the appropriate Unity platform modules before Android/iOS builds. Real-device performance and touch behavior still require hardware validation.
