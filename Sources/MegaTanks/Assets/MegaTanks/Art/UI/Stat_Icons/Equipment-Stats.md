# Equipment stat icons

The equipment details panel uses labeled icon rows for both current stats and equipped-to-selected comparisons. The original detail strings and all gameplay calculations remain unchanged. Long lists scroll, including armor tank totals and accessory weapon comparisons.

Existing icons: atk, accuracy, hp, defense, move_speed, crit_rate.

Generated with built-in image_gen using the original icons as style references:
- penetration: shell piercing a cyan shield.
- crit_damage: magenta impact burst with yellow lightning and orange fist.
- range: cyan range arrow and distant yellow targeting reticle.
- ammo: three cartridges in a red ammunition holder.
- reload_time: cyan stopwatch with a red reload arrow.
- reload_speed: cartridge, circular cyan arrows and magenta lightning.
- evasion: lime paw shield dodging an orange projectile.

Damage and damage bonus share ATK; the damage-type row also uses the attack symbol. Reload base/battle time share the stopwatch, while reload-speed percentage has its own icon. Armor uses Defense, hull bonuses use HP, and speed uses Move Speed. Every stat retains a text label and units so icon recognition is optional.

Generated originals are transparent 1254 x 1254 RGBA PNGs, imported into Unity at a maximum of 512 pixels. Existing source icons are preserved. New PNGs are also saved in the supplied Art/Asset/UI/Stat_Icons folder. Exact prompts and generated source paths are recorded in generated-equipment-icons.json.

Equipment visibility follows the selected item level gains: weapons show damage; armor shows HP only when hpPerLevel is positive; accessories show their bonus and derived affected stats only when bonusPerLevel is positive. Separate PER LEVEL rows and the duplicate upgrade summary are hidden. Empty groups are removed, and items without level gains show a no-stats-change caption.
