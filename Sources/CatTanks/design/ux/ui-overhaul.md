# CatTanks UI/UX — LayerLab Studio redesign

Status: Implemented, 2026-11-13. Scope: the existing CatTanks Unity game, keyboard/mouse, landscape screens. This work uses Codex Game Studios `studio-router` and the Unity UI specialist / UX designer references; implementation and checks are performed directly in this chat.

## Player journey

Choose a stage → review power and rewards → prepare the current team → deploy → use skills/capture enemies → review the result → upgrade or continue. Daily tasks and AFK rewards provide clear return goals. Existing progression, combat rules and profile serialization remain authoritative.

## Visual system

LayerLab GUI Pro–SuperCasual supplies nine-slice rounded panels, blue selected tabs, dark secondary buttons, yellow primary actions, item icons, input backgrounds and progress bars. `StudioUITheme.asset` references original sprites; art is not duplicated. The editor menu **CatTanks → UI → Rebuild LayerLab Theme** regenerates those references.

- Background `#0d152a`, main surface `#17243e`, secondary surface `#24324e`.
- Main text `#e4eafa`, supporting text `#aabbd6`, headings `#fff0bc`.
- Body 20, supporting 17, section 23, heading 34 logical pixels. Arial/Segoe UI supports Vietnamese diacritics.
- A 1600 × 900 logical layout fits inside `Screen.safeArea`; extra aspect-ratio space receives the background. Portrait-specific layouts are not part of this Windows game redesign.

## Screen inventory

| Area | UI behavior |
| --- | --- |
| Hub | Persistent commander identity, resource counters, icon + label navigation, combat power. |
| March | 20 stage nodes with written locked/ready/cleared states, selected stage details, recommendation and first-clear rewards. |
| Barracks | Cat, fortress, squad, skills, pets, equipment, presets and resonance tabs; consistent cards and selected states. |
| Selection | Explicit picker for squad cats, turrets, modules, skills, companions and equipment recipient. |
| Town | Summon showcase with pity count and affordability; scrollable supply cards with daily limits; arena opponent selection. |
| AFK | Chest illustration, elapsed time, rewards, progress to 12-hour cap and claim action. |
| Daily tasks | Five progress rows, claimed/in-progress/claimable labels, daily chest progress. |
| Battle | Fortress/enemy HP, time and wave status, energy, skill costs/cooldowns, capture counts, pause and sound controls. |
| Overlays | Deployment confirmation, settings, encyclopedia, selection, pause and result. |

## Interaction patterns

All screens use native UGUI controls authored in prefabs. `Assets/CatTanks/Resources/UI/CatTanksUI.prefab` is installed in Main, with Screens, Popups and Notifications layers. Fifteen screen prefabs and six separate popup prefabs are nested beneath it. Runtime code binds data and actions to stable widget IDs; prefab-authored RectTransform positions and typography survive updates. Reusable component prefabs provide additional data-driven list entries. Decorative images and labels do not consume pointer events. A full-screen scrim and disabled underlying controls isolate modal input. Mouse actions on UI do not select battlefield targets.

### Editing the Canvas

Open Main, expand **CatTanksUI → Safe Area → Screens / Popups**, then open the desired nested prefab. Each of `Popup_settings`, `Popup_book`, `Popup_prebattle`, `Popup_choice`, `Popup_pause` and `Popup_result` is an independent prefab under `Assets/CatTanks/Resources/UI/Popups`. Edit geometry and text styling in Prefab Mode; static bindings retain these edits while runtime values update. Buttons use the normal/selected/primary theme styles for gameplay state.

The border repair retained each sprite's authored proportions, reserved a nonzero stretch center, and used Full Rect meshes for 371 stretchable LayerLab component sprites. Image.Type.Sliced and sprite-specific pixel density keep corners fixed when resized. Icons and character art remain Simple with preserved aspect ratio. The prefab validation checks every referenced slice and compares generated Unity meshes at two widths to detect stretched corners.

The **Build Canvas and Popup Prefabs** command is a regeneration tool: it rebuilds the prefab layouts from the layout code and replaces manual layout edits. Ordinary Play Mode uses the saved Canvas/prefabs and does not regenerate them.

Tab / Shift+Tab moves among enabled buttons; Enter submits using Unity's event system. Escape closes an overlay, pauses a running battle, or returns to March. Input fields suppress gameplay shortcuts while editing. Selected tabs use a blue state; stage selection uses yellow and written progress labels. Disabled purchases show price/limit/unlock requirements. Empty inventory and companion screens retain actionable guidance.

UI feedback uses the existing notification system. A reduced-motion preference disables button transition fades. This is keyboard/mouse support, not a claim of complete controller or screen-reader accessibility.

## Verification

**CatTanks → UI → Run UI Smoke Checks (Play Mode)** traverses management screens, encyclopedia tabs, native button commands, the squad picker, modal input isolation, deploy confirmation, battle HUD, pause/resume and retreat; it also checks theme references and stable widget pooling. It uses an isolated temporary profile and restores the player's original profile. Output: `Logs/ui-smoke-validation.json`.

**CatTanks → UI → Capture UI Gallery (Play Mode)** captures representative screens under `Captures/StudioUI` with an isolated profile. Manual visual checks supplement functional checks. Existing **CatTanks → Validate Port** remains the gameplay regression check.

Verified in Unity 2022.3.62f2. The current UI smoke report is `Logs/ui-smoke-validation.json`; authored Canvas/9-slice results are `Logs/ui-prefab-validation.json`. Earlier gameplay regression passed 124 checks. Safe-area containment was checked at 1366 × 768, 904 × 565 (16:10), and 1920 × 1080; original Game View size was restored. Twenty screen captures are available, with `gallery-overview.jpg` as the visual index. No standalone or mobile build was produced for this UI task.
