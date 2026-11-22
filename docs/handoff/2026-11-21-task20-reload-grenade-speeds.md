# Task 20 — reload queue, grenade gauge, bullet speeds, fx_v2_1

Playtest follow-up (no commit/push/build in this task).

## 1) Bullet speed DBG sliders
- `Proto_GreyboxTuning`: `AllyBulletSpeed` and `EnemyBulletSpeed` both **4.2** (enemy was 2.2).
- Runtime DBG: ally/enemy sliders **2–12**, labels unchanged except ranges; `Proto_GreyboxRuntimeTuning` defaults mirror constants.
- Spawn paths already read `_tuning.AllyBulletSpeed` / `EnemyBulletSpeed` each simulate.
- Tests: `Tuning_DefaultBulletSpeedsEqual`, `Tuning_RuntimeBulletSpeedsIndependent`.

## 2) Grenade recharge gauge (6s)
- `GrenadeCooldown` default **6s**; DBG **"Grenade recharge s"** 2–15.
- `Proto_GreyboxGrenade`: `RechargeProgress01`, `JustBecameReady`, cooldown after detonation, **ReadyFlash** (full_0/1 @ 10fps × 3 cycles) before `CanThrow` again.
- `Proto_GreyboxGrenadeGaugeHud`: bg + radial fill over `Greybox_Hud_Grenade` (~132px ring), baked steps fallback; charge tap/drag → subtle shake, no throw while flying/cooldown/flash.
- Art: `Art/WestPixel/fx_v2_1/` (manifest.md); importer distinguishes `fx_v2_1` (enemy bullet pivot **0.65,0.5**, gauge center **0.5,0.5**).

## 3) Reload — one tap = one round (queue)
- `Proto_GreyboxGun`: each `StartReload` requests one round; 0.3s insert; queue capped by empty chambers; fire/roll `CancelReload` + clear queue; **casings only first tap** after shots (`ReloadCasingCount` > 0 once per spending burst).
- Player reload no longer blocked while `IsReloading` (queues taps).
- Cylinder HUD refill clips unchanged (per inserted round via `Proto_GreyboxCylinderHud`).

## 4) Enemy bullets fx_v2_1
- Catalog: `bulletEnemyFlipbook` 0..2 @ **12fps**, rotated to velocity; prefer over `fx_v2/bullet_enemy.png`.
- Fallback: ally sprite tinted red/orange + procedural glow.
- Hit radius / gameplay diameter unchanged.

## 5) Screenshot
- `Proto_GreyboxScreenshot.CaptureTask20Hud()` → `Builds/v4_greybox/task20_hud.png` (1920×1080, west + perspective ON).
- Logs gauge progress, enemy bullet count, ammo.

## 6) Tests touched
- Updated: `Gun_ReloadMagazineAndManual`, `Gun_RollCancelsReload`, `Gun_IncrementalReloadFromThreeFifths`, `Gun_StartReloadCasingCountMatchesSpent`.
- Added: `Gun_OneReloadTapThenInterval_DoesNotAutoContinue`, `Gun_FiveQueuedTapsFillMag_SixthIgnored`, `Gun_MidQueueFireClearsReload_KeepsAmmo`, `Grenade_RechargeProgressAndFlash`, `Tuning_RuntimeBulletSpeedsIndependent`.

## Files (main)
- `Scripts/Proto_GreyboxGun.cs`, `Proto_GreyboxGrenade.cs`, `Proto_GreyboxGrenadeGaugeHud.cs`
- `Scripts/Proto_GreyboxFxV2Catalog.cs`, `Proto_GreyboxWestPixelSkin.cs`, `Proto_GreyboxPlayerController.cs`
- `Scripts/Proto_GreyboxTuning.cs`, `Proto_GreyboxRuntimeDebugUi.cs`
- `Editor/Proto_GreyboxWestPixelImport.cs`, `Editor/Proto_GreyboxScreenshot.cs`
- `Tests/EditMode/Proto_GreyboxCombatTests.cs`