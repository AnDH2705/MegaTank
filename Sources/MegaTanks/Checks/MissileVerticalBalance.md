# Vertical-launch missile balance check

Combat scope: AT and AA packs. Sources: Data/Weapons/MissilePackAT.asset, MissilePackAA.asset, Scripts/GunDefinition.cs, AutoTurret.cs, Projectile2D.cs.

Both weapons now leave the original mortar mount vertically and then use the same bounded pursuit/retargeting logic. Speed remains 12 units/sec; lifetime remains 5 seconds. Steering changes from 80 to 240 degrees/sec. The minimum turning radius changes from approximately 8.59 to 2.86 units; a 90-degree turn takes 0.375 seconds rather than 1.125 seconds. The old radius caused upward-launched missiles to circle past ground targets within the AT pack's 12-unit acquisition range.

Damage, range, armor penetration, ammo, salvo, reload, progression and economy values are unchanged. Raw steady DPS remains 14.55 for AT and 9.70 for AA at level 1/zero stars, before defenses, misses and flight time. Neither variant gains a new damage multiplier. The variants retain different target classes; neither replaces the other.

Close or late replacement targets can still be missed because steering is finite, and missiles expire without remote damage. The end-to-end missile audit checks actual upward launch velocities, bounded curvature followed by an AT/AA hit, original mortar mount alignment, and a late retarget miss. Exact combat hit rate remains dependent on enemy movement and spacing.
