# Task 15 — visible bullets, wider move zone, boundary line, grunt “self deaths”

## Root cause: enemies “dying on their own”

- **No extra damage path found.** `Proto_GreyboxAllyBulletSim` and `Proto_GreyboxGrenade` are the only sources that call `ApplyDamage` on grunts. `Proto_GreyboxBulletSim` only tests the player footprint (documented on the class).
- Grunts spawn with `tuning.EnemyHp` (default 3); no HP-0 spawn path.
- **Perceived self-deaths** came from **invisible player shots**: west `SyncAllyBullets` drew tiny ally cores (~0.09 gameplay diam × 1.4), **disabled tracers**, and `SyncEnemyBullets` **disabled glow**. Auto `FireQuickAuto` / tap-fire still fires 3-damage one-shot kills; west death clip is the stumble/lie-down animation, which looked like falling over with no visible hit.
- Guard: enemy bullets remain player-only (`Proto_GreyboxBulletSim` summary + EditMode test through two grunts on the same row).

## Bullet visuals (gameplay hit radii unchanged)

- Sim diameters stay **0.09 ally** / **0.30 enemy**; `HitRadius` formulas unchanged.
- New visual-only constants on `Proto_GreyboxTuning`: `AllyBulletVisualDiameter` 0.32, `AllyBulletVisualTracerLength` 0.80, `EnemyBulletVisualDiameterMul` 1.2, `EnemyBulletVisualGlowMul` 1.55.
- West skin + procedural fallback: larger bright yellow ally cores/tracers; enemy cores + red/orange glow enabled; muzzle flash ~5.5% height / 0.14s; optional short grunt muzzle on fire.

## Move zone

- Default **move top** (`FootYPercentMin`) **75 → 60** (feet can advance toward mid-field; body still stops below fence via `MaxFootWorldY = min(percent, FenceBottomWorldY − 0.08)`).
- DBG slider **“Move top %”** 50–74 on `Proto_GreyboxMoveZone.SetFootYPercentMin`; `ResetToDefaults()` for EditMode. Side bounds 22–78 unchanged.

## Move boundary (west skin ON)

- Procedural dashed sandbag/rope line at **top** of move area, faint **left/right** stakes from bottom to top of zone.
- Subtle highlight when player foot is within ~0.4 world units of an edge; rebuilds when move-top % changes.

## Tests / screenshot / build

- `Proto_GreyboxCombatTests`: enemy bullet through grunts, survival sim without ally/grenade damage, spawn HP.
- `Proto_GreyboxMoveTests` / skin test: `ResetToDefaults()` on move zone.
- Screenshot: `CaptureTask15Combat()` → `Builds/v4_greybox/task15_combat.png` (west ON, move up, intermittent fire, boundary visible).
- APK path: `Builds/v4_greybox/HillNGun_v4_greybox_1121_west3.apk`.