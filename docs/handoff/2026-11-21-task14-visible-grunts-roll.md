# Task 14 — visible west-pixel grunts, roll chain, visual fixes

## Sort bug (invisible enemies)

- `WP_BgTown` sorts at `SortingBuildingBase + 20` (-430). Grunt `TargetView` used `Max(SortingOrderAtFootY, SortingEnemyMarker)` → about **-435** at the enemy row foot Y (~6.3), **behind** the town layer. Opaque town + sand covers the entire grunt row.
- **Fix:** `Proto_GreyboxLayout.WorldSpriteSortingOrder(footY)` maps far-field feet (≥ fence foot ~46%) into **ground+1 … fence-1** with Y sort; near field uses `Max(SortingOrderAtFootY, SortingFence+1)`. `SortingEnemyMarker` raised to **SortingGround+1** (-399). Applied on targets, crates, player, and west-pixel grunt/player sync.
- With west skin ON, `TargetView.LateUpdate` only syncs **position + sort** so skin sprite/scale/color are not overwritten.

## Roll chaining

- `RollCooldown` default **0** (DBG slider still works if set > 0). Duration (0.28s) and i-frames (0.22s) unchanged.
- `Simulate` ticks roll first, then allows **held** `RollPressed` to `TryStart` (still blocked while `_active`). Reload cancel on roll start unchanged.

## Visual fixes

1. **Ground:** single `ground.png` via `PlaceTopLeftSprite` (no overlapping `ground_tile` tiles).
2. **HP bar:** thinner frame lower on screen (~95–97.8%), below boots.
3. **Crosshair:** idle visual at `DefaultAimGround` (50%/38%), not on left-edge first spawn.
4. **HUD:** west skin hides FIRE/ROLL/GRENADE/RELOAD text labels; `장전!` still shows when ammo empty.

## Tests / screenshot

- MoveTests: zero-cooldown restart + chain test.
- SkinTests: grunt sort band vs town/ground/fence (+ live grunt renderer when catalog loads).
- Screenshot: `Proto_GreyboxScreenshot.CaptureTask14Combat()` → `Builds/v4_greybox/task14_combat.png`.