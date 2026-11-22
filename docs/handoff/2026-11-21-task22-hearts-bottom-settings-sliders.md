# Task 22 — Hearts bottom HUD + settings slider fix

Date: 2026-11-21 (Asia/Seoul). Branch: `proto/v4-greybox-stage`. No commit/push.

## Hearts at bottom

- `Proto_GreyboxHeartsHud` root is **bottom-center** at **31–69% x**, **92.2–97.2% y** (screen % from top via `ApplyRect`). Constants on `Proto_GreyboxHudBuilder` (`HeartsRootX0` … `HeartsRootY1`).
- Sits in the gap between the joystick hit box (4–19 × 66–91) and the right-thumb cluster (FIRE/RELOAD/GRENADE/ROLL centers ~84–93 × 57–73). Player foot layout ~88% y; hearts are HUD chrome below that band.
- `EnsureBound` **re-applies** `ApplyHeartsRootRect` when an existing `Greybox_Hud_Hearts` is found (stale task-21 top bar 22–38 × 2–8.5 is migrated on next bind).
- Behaviors unchanged: 3 hearts, 10s bottom-up regen, i-frames, pop/shake/flash, legacy HP hidden, placeholder art, canvas sort 1000.

### Aspect overlap (EditMode)

`Proto_GreyboxHudLayoutMath.HeartsClearHudChromeAt(w,h)` checks heart rect vs joystick AABB and vs each action circle (hit radius scaled to screen height). Passed at **1920×1080**, **2048×1536**, **1024×768**, **2532×1170** (19.5:9 landscape). Canvas match=0.5 does not change percent anchors; circle test uses screen-height–scaled hit padding like gameplay.

## Settings sliders — root cause & fix

### Root cause (specific)

The Greybox stage **never creates a Unity `EventSystem`**. HUD is built at runtime (`Proto_GreyboxHudBuilder.Build`) with `GraphicRaycaster` on the canvas, but **no `EventSystem` and no UI input module** exist anywhere under `Assets/HillNGun/Proto/Greybox` (confirmed grep across the project).

- `ProjectSettings` **activeInputHandler = 2** (Input System package only). Even a legacy `StandaloneInputModule` would not receive touches.
- uGUI `Slider` drag/tap depends on `EventSystem` + **`InputSystemUIInputModule`** to turn touches into pointer events. Without it, the 설정 panel **renders** but drags never update `Slider.value`.
- **Why DBG sliders seemed fine:** `Proto_GreyboxRuntimeDebugUi` sliders are built as uGUI on the same canvas but dragging them from the device was not the reported path; DBG panel interaction in editor often uses mouse in play mode. The 설정 panel is the first player-facing uGUI that **must** work on tablet touch while paused.
- **Why editor was easy to miss:** In-editor play with mouse can appear to work only if a scene EventSystem existed; Greybox scene has none. Combat buttons use **custom** `GatherPointers` hit tests, not uGUI, so pause/설정/닫기 worked while sliders did not.

### Fix

- `Proto_GreyboxUiEventSystem.Ensure()` — creates `Greybox_EventSystem` with `EventSystem` + `InputSystemUIInputModule` (fallback `StandaloneInputModule` when Input System define off).
- Called from `Proto_GreyboxHudBuilder.Build` and `Proto_GreyboxSettingsUi.Ensure` / `SetVisible`.
- `HillNGun.Proto.Greybox.asmdef` references `Unity.InputSystem`.
- Slider rows rebuilt with standard **Fill Area / Handle Slide Area**, `raycastTarget` on track + handle, `interactable = true`.
- **− / +** buttons per row (`BulletSpeedNudge = 0.5`, clamp 2–12) via `Button` + `NudgeAlly` / `NudgeEnemy`; live `RuntimeTuning` + `Proto_GreyboxBulletSpeedPrefs` unchanged.
- `timeScale = 0` while paused: Input System UI module still delivers pointer events (no change to pause polling except close on custom tap for 닫기).

Pointer-injection drag tests are **not** in EditMode (batch EventSystem + paused timeScale brittle); value/nudge/prefs paths are covered.

## Tests

EditMode `HillNGun.Proto.Greybox.Tests`: **84/84 passed** (77 + 7 in `Proto_GreyboxHudHeartsSettingsTests`).

- Bottom band + not legacy top bar
- No overlap joystick/cluster at 16:9, 4:3, 19.5:9 landscape
- HUD build creates EventSystem + InputSystemUIInputModule
- Settings nudge clamp + prefs round-trip
- `SetAllyBulletSpeed` → tuning

## Screenshots

- `Proto_GreyboxScreenshot.CaptureTask22Hud()` → Mac `Builds/v4_greybox/task22_hud.png` and box `/workspace/smoke_scale/v4_greybox/task22_hud.png` (1920×1080, west ON, perspective ON).
  Verified log: `heartBand=92.2-97.2% hearts=1 regen01=0.66 rollLock=4.84`. Hearts sit bottom-center between the joystick and the right cluster; one heart is mid-regen; energy bar is gone from the top.
- `Proto_GreyboxScreenshot.CaptureTask22Settings()` → box `/workspace/smoke_scale/v4_greybox/task22_settings.png`. Panel visible with title **설정** at the top of the card, **내 총알 속도** then **적 총알 속도**, values 4.2, −/+ on each row, **닫기**. (Title had been placed at 72–80% y under 닫기; moved to 26–34% y at the top of the card before the final APK.)

## APK

- Path (box): `/workspace/hillandgun_handoff/v4_greybox/HillNGun_v4_greybox_1121_task22.apk`
- Also on Mac: `Builds/v4_greybox/HillNGun_v4_greybox_1121_task22.apk`
- Size: **23,063,577 bytes** (22.00 MiB). Zip test OK (268 entries, `AndroidManifest.xml`, `classes.dex`, `lib/arm64-v8a/libil2cpp.so`). Same package/settings as prior greybox APKs; `ProjectSettings.asset` restored after Unity test/capture/build runs.

## Leftovers / risks

- Other scenes outside Greybox still have no shared EventSystem; only Greybox HUD ensures one.
- DBG uGUI sliders still lack EventSystem until HUD is built; opening DBG after stage load is OK.
- Close button uses both uGUI raycast (`raycastTarget` plate) and legacy percent tap on `PointerDown` began only — sliders use full drag via EventSystem.
- No commit/push.