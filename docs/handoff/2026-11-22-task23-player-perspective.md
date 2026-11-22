# Task 23 — Player perspective scale

Date: 2026-11-22 (Asia/Seoul). Branch: `proto/v4-greybox-stage`. No commit/push. No iOS.

## Player height

- Screen height % is driven by **foot Y** in the player move band (`PerspectiveMoveFootMinPercent` / `Max` = **62–95%** from top when world perspective is on; uses `MoveFootMinPercent` / `Max` when flat).
- **Defaults:** far/top **14%**, near/bottom **20%** (DBG sliders `Player h top %` / `Player h bottom %`).
- **Interpolation:** linear `t = clamp01((foot% − min) / (max − min))`, `height% = lerp(top, bottom, t)` — same linear-in-band style as enemy row height in `Proto_GreyboxPerspectiveDepth` (enemy uses fixed 40–52% band with linear px lerp).
- **Clamp:** `t` clamped 0–1 so feet slightly outside 62–95% do not extrapolate.
- **Smoothness:** per-frame `PlayerBodyHeightWorld(foot.y)` on visuals; no LOD steps.
- **Toggle OFF (`Player perspective`):** constant **bottom/near** height % at all foot Y (default 20%).

Helpers live on `Proto_GreyboxPerspectiveDepth` (`PlayerHeightScreenPercentAtFootPercent`, `PlayerBodyHeightWorld`, `PlayerVisualScaleAtFootWorldY`, `PlayerDepthTAtFootPercent`).

## What else scales

| Element | Behavior |
|--------|----------|
| West pixel body / greybox capsule | `PlayerBodyHeightWorld` each tick |
| Muzzle / reload casing Y | Body height × 0.55 |
| Roll / dust FX | `PlayerVisualScaleAtFootWorldY` on puff scale |
| Dizzy stars (procedural) | Orbit height + star scale |
| Ally bullet **visual** size | `AllyBulletVisualScaleAtFootWorldY` (follows player perspective; else legacy `Proto_DepthScale`) |
| Ally bullet **speed** | Unchanged world-unit `AllyBulletSpeed` |
| Enemy bullet hurt footprint | `PlayerGroundFootprint` width/depth × visual scale |
| Draw order | Still `WorldSpriteSortingOrder(foot.y)` |

Shadow is baked in west idle sprites; body uniform scale handles it.

## Vertical move factor

- DBG toggle **`Player depth move`**, default **ON** (child of player perspective UX).
- When player perspective **and** depth move are on: vertical input × `lerp(0.9, 1.0, t)` (top of band → 0.9, bottom → 1.0). Horizontal unchanged.
- When player perspective OFF: factor **1.0** (no slowdown).

## DBG (F1)

On `Proto_GreyboxRuntimeDebugUi` + `Proto_GreyboxRuntimeTuning.ApplyPlayerPerspective()`:

- Toggle **Player perspective** (default ON)
- Slider **Player h top %** 8–24, default 14
- Slider **Player h bottom %** 12–30, default 20 (clamped > top)
- Toggle **Player depth move** (default ON)

Not added to player-facing 설정 panel.

## Tests

EditMode `HillNGun.Proto.Greybox.Tests`: **91/91 passed** (84 + 7 in `Proto_GreyboxPlayerPerspectiveTests`).

- End heights 14% / 20%
- Mid 78.5% ≈ 17%
- Outside band clamped
- Footprint wider at bottom
- Toggle off → flat height/footprint
- Vertical factors 0.9 / 1.0 and disabled paths

## Screenshot

`Proto_GreyboxScreenshot.CaptureTask23Depth()` → Mac `Builds/v4_greybox/task23_depth.png` and box `/workspace/smoke_scale/v4_greybox/task23_depth.png` (1920×1080).

- West skin ON, perspective ON, player perspective defaults.
- `EnemyFireInterval=9999`, waves 0.
- **Side-by-side:** left = foot **62%** (top of move area, height 14%), right = **95%** (bottom, height 20%); two half-width renders stitched.
- Verified log: `topFoot%=62.0 h%=14.00 bottomFoot%=95.0 h%=20.00`. Size difference is clearly visible; draw order vs fence/crate looks correct. Each half is rendered at 960×1080 (taller aspect) so HUD is duplicated in both panels.

## APK

- Path (box): `/workspace/hillandgun_handoff/v4_greybox/HillNGun_v4_greybox_1122_task23.apk`
- Also on Mac: `Builds/v4_greybox/HillNGun_v4_greybox_1122_task23.apk`
- Size: **23,070,033 bytes** (22.00 MiB). Zip test OK (manifest, dex, arm64 il2cpp). Same package/settings as prior greybox APKs; `ProjectSettings.asset` restored after Unity test/capture/build runs. No commit. No iOS.

## Risks

- Bottom default **20%** vs legacy constant **19%**: ~5% taller at bottom when perspective on; toggle off uses 20% as “near” reference (not 19).
- Ally bullets use player visual scale along path when perspective on (not spawn-only); speed unchanged.
- Procedural `StageBuilder` capsule still uses 19% for editor greybox without skin.