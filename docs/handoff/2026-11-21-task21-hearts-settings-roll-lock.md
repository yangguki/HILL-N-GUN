# Task 21 — Hearts, settings, roll chain lock

Date: 2026-11-21 (Asia/Seoul). Branch: `proto/v4-greybox-stage`. No commit/push. No iOS build.

## Player bullet speed settings (설정)

- Pause menu **설정** button + top HUD **gear** (dark circle left of pause). Gear pauses and opens settings directly.
- Sliders: **내 총알 속도** / **적 총알 속도** (2–12, step 0.1, live value label).
- `Proto_GreyboxSettingsUi` applies to `RuntimeTuning`; DBG sliders stay in sync via `Proto_GreyboxBulletSpeedPrefs` keys:
  - `HillNGun.Greybox.AllyBulletSpeed`
  - `HillNGun.Greybox.EnemyBulletSpeed`
- Loaded on player `EnsureLogic()`; DBG or 설정 changes write prefs.
- EditMode: `EnterTestScope()` uses an in-memory store so tests do not clobber user PlayerPrefs.

## 3 hearts + regen

- `PlayerMaxHp = 3`; enemy hits still `EnemyBulletDamage = 1` → one heart per bullet. `CheckRunEnd` unchanged (`_hp` synced).
- `Proto_GreyboxPlayerHearts` + `Proto_GreyboxHeartsHud` on the top bar, left of the stage title (approx. 22–38% x, 2–8.5% y). **Placeholder** procedural heart sprites (`Proto_GreyboxSpriteUtil.GetHeartSprite`) — swap art later.
- Regen: the next empty heart (right-depleted bar) fills bottom-up over **10s** (`HeartRegenSec`, DBG **Heart regen s**). Any hit zeros partial fill. No regen after death.
- **~0.5s i-frames** after a hit (`PlayerHitIFrameSec`); included in `IsInvulnerable` with roll invuln; player sprite blinks.
- Pop/shake on heart loss; white flash on restore. Bottom energy/HP bar, west HP fill, and `HP n/n` are hidden.

## Roll chain lock

- Defaults: **Roll chain max = 3**, **Roll cooldown s = 5** (`RollChainLockSec`) on `RuntimeTuning`; DBG sliders added.
- After 3 chained rolls (1.2s window unchanged), a **5s roll lock** starts when the 3rd roll **ends**. Light dizzy on the 3rd roll is kept (~1.5s, move/fire still work).
- Stun at 5+ chains remains in code but is unreachable at the default cap unless DBG raises chain max (stun tests set max ≥ 8).
- `Proto_GreyboxRollGaugeHud` on the ROLL button: radial dim fill (remaining/duration) + seconds (`0.0`); nudge when roll is pressed while locked.

## Tests

EditMode `HillNGun.Proto.Greybox.Tests`: **77/77 passed** (was 70; added hearts, prefs round-trip, and roll-cap cases).

- `Proto_GreyboxPlayerHeartsTests` — 3 hits / i-frames (tests expire i-frames between hits), regen, mid-regen hit, two-step regen.
- `BulletSpeedPrefs_SaveLoadRoundTrip` — isolated via `EnterTestScope()`.
- `Proto_GreyboxRollDizzyTests` — 3-chain 5s lock, cap=3 cannot reach stun, 5-chain stun with raised cap.
- Cover tests still use `PlayerMaxHp` as starting int (now 3).

## Screenshot

- Capture: `Proto_GreyboxScreenshot.CaptureTask21Hud()` → Mac `Builds/v4_greybox/task21_hud.png` and box `/workspace/smoke_scale/v4_greybox/task21_hud.png` (1920×1080, west ON, perspective ON).
- Capture mutes enemy fire (`EnemyFireInterval = 9999`) so the 3-heart player is not downed during warmup.
- Verified log/pose: `hearts=1 regen01=0.66 rollLock=4.84`. Middle heart shows a partial bottom-up fill; ROLL has a radial dim and seconds. Energy bar is gone. Gear is the dark circle left of pause (⚙ glyph may be missing in the UI font).

## APK

- Path (box): `/workspace/hillandgun_handoff/v4_greybox/HillNGun_v4_greybox_1121_task21.apk`
- Also on Mac: `Builds/v4_greybox/HillNGun_v4_greybox_1121_task21.apk`
- Size: **23,058,713 bytes** (21.99 MiB). Zip test OK (268 entries, `AndroidManifest.xml`, `classes.dex`, `lib/arm64-v8a/libil2cpp.so`). Same package/settings as prior greybox APKs; `ProjectSettings.asset` restored after Unity test/capture/build runs.

## Leftovers

- Heart sprites are placeholder procedural art; replace when the designer delivers hearts.
- HUD gear ⚙ may render as an empty dark circle if the UI font lacks the glyph; pause still opens 설정.
- Optional roll afterimage (fx_v2 frames 03–06) is still not hooked (task 19 leftover).
- No iOS build in this task. No git commit/push.
