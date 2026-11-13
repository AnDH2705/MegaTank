# Editing the MegaTanks UI

Open `Assets/MegaTanks/Scenes/MainBattle.unity`. You do not need to enter Play mode.

1. Select **MegaTanks UI Canvas** in the Hierarchy.
2. In its **UICanvasLayout** inspector, choose **Preview screen**, then click **Show in Editor**.
3. Select the desired button, label, image, panel, or bar beneath that screen. Use the Rect tool to move/resize it; edit its RectTransform, Image, Text, or Button in the Inspector.
4. Save the scene to keep scene overrides. Use Unity's **Overrides > Apply** when the change should belong to the prefab itself.

Previewing a screen only changes what is visible in the editor. The game always begins on Home. Use a 1920 × 1080 landscape Game view as the reference. The Canvas safe-area container automatically fits the device; edit the screen contents beneath it.

## Prefab locations

Everything is under `Assets/MegaTanks/Prefabs/UI`.

- **MegaTanksCanvas.prefab**: complete Canvas, safe-area container, screen instances, shared templates, and runtime layout settings.
- **Home**, **BattleHUD**, **Equipment**, **Settings**, **Crew**, **Tech**, **LeaveRun**, **Pause**, **Defeat**, **Victory**, **Rewards**, **Shop**, **Service**, **Reveal**, **WaveBreak**, **Route**: independently editable screen prefabs. Double-click to open in Prefab Mode.
- **RewardCard**, **ShopCard**, **ServiceCard**: reusable cards nested into each screen's fixed slots. Move the slot instances to rearrange an offer; edit the card prefab to adjust its internal layout.
- **InventoryItem**: inventory cell, icon, name, selection outline, and equipped badge. Inventory spacing and cell size are controlled by **Equipment > Inventory > Content > Grid Layout Group**. The Content Size Fitter handles scrolling height. The existing sample items show the layout outside Play mode; runtime items come from the inventory catalog.
- **RouteNode**, **RouteConnection**, **RouteTank**: procedural map templates. Edit node icons and labels inside RouteNode. Round spacing, padding, and tank marker offset are exposed on UICanvasLayout. Sample nodes/connections in the Route prefab provide an editor preview; the current run replaces them with its generated map.
- **DamageNumber**: a moving anchor with an editable Label child. Font, size, alignment, and label offset can be changed on that child.
- **EnemyHealthBar**: a world-space Canvas attached above each enemy. Edit its Display children to change the bar and offset. Its component exposes automatic width fitting and enemy-color options; the prefab reference is on the Enemy Data Manager.
- **UIEventSystem**: input system prefab included in the scene.

## Live values and moving displays

Gameplay updates ammunition, health, scraps, stats, card data, item icons, selected states, and route availability. Those values will change during a run; positions, sizes, fonts, and other authored layout settings are retained.

Ammo/health bars use Image **Fill Amount**, preserving their authored dimensions. **BattleHUD > Hull Anchor** follows the tank; move or resize its Hull/Empty/Bar children to change the health display. **UICanvasLayout > World UI** exposes its world offset and health/damage colors. The Settings vibration switch has editable **On Position** and **Off Position** anchors.

UIView stores references with stable binding IDs. You can rename or reparent existing controls without breaking their bindings. Keep bound objects/components when changing their appearance; deleting a required control requires updating its binding and behavior. Adding decorative Images or labels needs no code.

Existing artwork skin assets remain the source of state-dependent sprites, such as selected equipment slots and route-node states. Their references are exposed on CanvasGameUI. Import/setup commands do not regenerate the authored layouts.
