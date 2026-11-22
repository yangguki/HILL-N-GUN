# Task 16 — Death fade, perspective depth, perspective_v1 art

## Dead grunts loop (fix)

**Root cause:** `TryPlayGruntDeath` removed `GruntVisualState` when `DeathT` hit 0. The next `SyncGruntViews` created a fresh state, re-enabled the view, and restarted the down animation / scoring.

**Fix:** `Finished` flag on `GruntVisualState`; state stays in the dictionary. Timeline after death:

1. Down frames at 0 / 0.2 / 0.45s (unchanged indices)
2. Hold last down frame at alpha 1 for **0.4s**
3. Fade out over **0.3s**
4. Disable view, `Finished = true`

`TryPlayGruntDeath` still returns `true` when finished (blocks unskinned `MarkDefeated`). `SyncGruntLiving` skips `Dying` or `Finished`. Helper: `Proto_GreyboxGruntDeathVisual` (EditMode tests).

## Enemy bullet visual 1.75×

- `Proto_GreyboxTuning.EnemyBulletVisualDiameterMul` → **1.75**
- `EnemyBulletVisualGlowMul` → **1.85** (glow still reads around larger core)
- Gameplay hit radius unchanged

**Import verification (unchanged, still correct):** `Art/WestPixel` postprocessor — `FilterMode.Point`, PPU 100, bullet pivot `(0.75, 0.5)`, travel-aligned rotation in sync code, `CombatDrawSortingOrder`, `ScaleForWorldSize` preserves aspect.

## Perspective depth

- **Toggle:** Runtime debug UI “Perspective depth” (default **ON**); sets `Proto_GreyboxPerspectiveDepth.Enabled`, `MoveZone.ResetToDefaults()`, rebuilds stage + west skin via `SetPerspectiveDepth`.
- **Formula** (yf = foot% / 100, clamped [0.40, 0.52]):  
  `h(yf) = 1080 × (0.06 + (yf − 0.40) / 0.12 × 0.05)` → 6% screen height at 40%, 11% at 52%  
  Grunt uniform scale = `h / 120`. Crate art ≈ `0.62 × h` (baked in perspective_v1 sprites).
- **OFF:** Task-15 flat layout — enemy foot **37%**, height **11%**, fence **41–46%**, move **60–90%**.
- **ON:** Enemy rows **40 / 46 / 52%** (`WaveDirector`), fence **54–59%**, move **62–95%**.
- **Aim default:** **46%** (ON) / **38%** (OFF).
- **Hitboxes:** `GroundFootprint` / `BodyCenterGround` scale with perspective body height.
- **Sorting:** `SortingGround` (-480) in front of sky, behind `SortingBuildingBase` (-450). Far-field enemies/crates Y-sort between **buildings+1** and **fence−1**; player zone in front of fence; bullets use `CombatDrawSortingOrder`.

### Known limit

Nearest building bases reach ~**52–56%** screen Y at the left/right edges and visually overlap the 52% grunt row and fence. Buildings stay **behind** enemies and fence so combat reads correctly on top.

## perspective_v1 integration

Sprites under `Art/WestPixel/perspective_v1/` (parent flat art unchanged). Catalog fields: `bgSkyFar`, `bldLeft`, `bldRight`, `perspGround`, `perspFence`, `perspFencePost`, `crateD40/D46/D52`. Rebuild loads via `LoadPerspectiveSprite`.

When perspective **ON** and catalog ready:

- bg_sky_far, ground, buildings, designer fence tiling (gap 860–1060, post, uniform scale)
- Depth crates at preview feet: (0.51,0.40), (0.40,0.46), (0.72,0.52), (0.24,0.52)
- Procedural far/mid crates hidden; **crate_near** on player-zone box (~62–82% / 58–69%)
- **OFF:** flat west-skin path (bg_far, bg_town, parent ground/fence)

## Tests

- `Proto_GreyboxPerspectiveTests` — height curve, clamp, wave rows, footprint scale, sorting, death hold/fade, bullet mul
- Updated stage/skin tests for perspective-default fence percents
- **No new APK/iOS** this task

## Screenshots

- `CaptureTask16Depth()` → `Builds/v4_greybox/task16_depth.png` (west ON, perspective ON, multi-row grunts, bullets, move boundary)
- `task16_compare.png` — composed outside Unity by parent agent

## Builds (added after screenshots)

- Android APK: `/workspace/hillandgun_handoff/v4_greybox/HillNGun_v4_greybox_1121_persp.apk` (22,326,061 bytes).
- Screenshots: `/workspace/smoke_scale/v4_greybox/task16_depth.png` (1920×1080), `/workspace/smoke_scale/v4_greybox/task16_compare.png` (engine | compare_target).
- iOS Xcode export: `Builds/v4_greybox/ios_1121_persp`. `xcodebuild` Debug + automatic signing (team `9KSCW68JQW`) **BUILD SUCCEEDED** (codesign did not hang). Installed on the connected iPhone 16 as `com.yangguki.hillngun.greybox`. Launch via devicectl failed because the phone was locked — unlock and open **HillNGun Greybox**.
- `ProjectSettings/ProjectSettings.asset` restored after Unity. No commit/push.
