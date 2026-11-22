# Task 18 — ally bullets, aim stick, grenade drag-aim

## Invisible ally bullets (root cause + fix)

- **Cause:** With west pixel skin ON, `SyncAllyBullets` / `SyncEnemyBullets` returned `true` after syncing **existing** view lists. On a fresh run `_allyBulletViews` was empty, the west loop ran zero times, and `SyncAllyBulletViews` returned early — **no SpriteRenderers were ever created** while sim bullets still flew.
- **Fix:** West skin now **grows** core / tracer / hotspot (ally) or core / glow (enemy) lists under the same bullet roots before sync. Controller procedural path unchanged as fallback when skin is off.

## Visual / sorting

- Ally bullet: bright yellow core (`bullet_ally` tint), white hotspot child, **0.60 wu** tracer, muzzle-forward offset, `MinAllyBulletWorldDiameter` raised (~2.4% screen H).
- `CombatDrawSortingOrder` floor raised to `SortingFence + 50` so combat draws above ground, buildings, and fence.

## Aim stick (Brawl-like)

| Constant | Value |
|----------|-------|
| `AimStickRadiusScreenHeightPercent` | **18** |
| `AimStickSmoothTime` | **0.12 s** |
| `AimAngularSpeedCapDegPerSec` | **240** |
| `AimFineAimExponent` | **1.5** |

- Implemented in `SmoothAimDirection`; threaded through `Proto_GreyboxAim.ConfigureScreenStick` and runtime tuning.
- DBG sliders: radius %H 12–26, smooth 0.04–0.25, angular cap 90–480, exponent 1–2.5.

## Grenade drag-aim

- Touch: **hold GRENADE → drag direction + distance → release** to throw at clipped landing (`Proto_GreyboxGrenadeAim`, max range 25 wu).
- Short tap (below aim-stick drag threshold): auto-aim **nearest living enemy** (keyboard **G** same).
- Drag past threshold then back to dead zone: **cancel**, no throw.
- `PollTouchGrenadeStick()` (separate finger id); no `PollTouchButton` grenade tap on drag start.
- Input: `GrenadeStickHeld`, `GrenadeStickReleased`, `GrenadeStickDragWorld`; stripped when paused.
- Preview: arc line + landing circle, combat sorting, visible with west skin.

## Tests added / updated

- **Skin:** `WestSkin_AllyBullets_CreateVisibleViewsWithTracer`, `CombatDrawSortingOrder_AboveGroundBuildingsAndFence`.
- **Input:** angular cap, fine-aim exponent, large-flick convergence; grenade auto-aim, drag range, cancel; pause strips grenade stick.
- Existing grenade cooldown test unchanged.

## Screenshots (parent run)

`Proto_GreyboxScreenshot.CaptureTask18BulletFlight()` →

- `/workspace/smoke_scale/v4_greybox/task18_bullet_f1.png`
- `/workspace/smoke_scale/v4_greybox/task18_bullet_f2.png`
- `/workspace/smoke_scale/v4_greybox/task18_bullet_f3.png`
- `/workspace/smoke_scale/v4_greybox/task18_bullet_f4.png`

West ON, perspective ON, 1920×1080, logged ally bullet positions each frame.

## APK (parent)

`/workspace/hillandgun_handoff/v4_greybox/HillNGun_v4_greybox_1121_persp3.apk`

- No iOS build in this task.
- No commit in this task.

## Verification

- EditMode: **55/55 passed**. `ProjectSettings/ProjectSettings.asset` restored after Unity.
- Screenshots: `/workspace/smoke_scale/v4_greybox/task18_bullet_f1.png` … `f4.png` (1920×1080). Yellow core + tracer clearly travels toward the enemy row.
- Android APK: `/workspace/hillandgun_handoff/v4_greybox/HillNGun_v4_greybox_1121_persp3.apk` (22,315,725 bytes). No iOS. No commit/push.
