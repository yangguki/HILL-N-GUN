# Task 19 — fx_v2 art wired (magazine, dizzy, roll/bullet/grenade/aim)

## Art source

- Designer frames live in `Assets/HillNGun/Proto/Greybox/Art/WestPixel/fx_v2/` (263 PNG + `manifest.md`).
- Import: Point filter, PPU 100, uncompressed, no mips (`Proto_GreyboxWestPixelImport`); `fx_v2` custom pivots per manifest (feet `0.028` roll/dizzy body, `0.021` dizzy FX overlay, reload casings `0.909`, etc.).
- Catalog: `Proto_GreyboxFxV2CatalogRebuild` fills `Resources/Proto_GreyboxFxV2Catalog.asset` on import / editor delay (individual frames, not `*_sheet.png`).

## Magazine / reload (5-round cylinder)

- `MagazineSize = 5`, `ReloadRoundInterval = 0.3s` per round (`Proto_GreyboxGun`).
- Fire with ammo > 0 cancels incremental reload; casings VFX only on `StartReload` (`reload_casings_{k}_` 12f @20fps, k = rounds fired).
- HUD: `Proto_GreyboxCylinderHud` uses `btn_fire_cyl_{0..5}`, `btn_fire_empty_blink` @4fps, `btn_fire_refill_{n}to{n+1}` @20fps (~0.25s). Procedural chamber fallback if art missing.

## Roll / dizzy

- Chain window **1.2s** after prior roll **end** (`Proto_GreyboxRollDizzy.ChainWindowSec`).
- 1–2 rolls: roll 00→10 (skip frame 10 if entering dizzy) → idle.
- 3–4: roll 00→09 → `dizzy_light_in_fx` (2f @12fps) → `dizzy_light_body` + `dizzy_light_fx` synced @8fps for **~1.5s** (`LightDurationSec`). **Does not block** move/fire/roll; no extra engine wobble when fx body is active.
- 5+: roll 00→09 → spin in (2f @12fps) → spin body (12f @12fps = **1.0s**, input locked) → spin out (3f @12fps) → idle (`StunDurationSec` = in + body + out).
- Roll visual: 11f @22fps, no 0.6 squash when fx roll present; takeoff/land dust @16fps; afterimage hook optional.
- FX overlay parented at player feet (same pivot family as body).

## Combat VFX

- Ally: `bullet_ally_tracer` (rotate to travel) + optional `bullet_ally` tip; enemy `bullet_enemy` @ speed 2.2.
- Muzzle `muzzle_flash_` 3f @30fps; hit `hit_spark_` 3f @24fps (fx path even if legacy catalog empty).
- Ground dust `dust_ground_` / crate `dust_crate_` @16fps.

## Grenade + aim

- Flight: `dynamite_spin_` loop @16fps, `smoke_puff_*`, `bomb_shadow` scale/alpha vs fall t.
- Explosion `explosion_00..04` @16fps, depth scale ≈ grunt `h(yf)/120 × 1.2`.
- Gun aim: `aim_band` (pivot bottom, rotate aim, **scale Y** for range), `aim_tip` at end.
- Grenade preview: `aim_bomb_dot` along arc, `aim_bomb_land` ellipse (perspective depth).

## Screenshots (parent run)

| Method | Output |
|--------|--------|
| `CaptureTask19Fire()` | `Builds/v4_greybox/task19_fire.png` |
| `CaptureTask19Bomb()` | `Builds/v4_greybox/task19_bomb.png` |
| `CaptureTask19Roll()` | `Builds/v4_greybox/task19_roll.png` |
| `CaptureTask19Dizzy()` | `Builds/v4_greybox/task19_dizzy.png` |

1920×1080, west skin + perspective. Parent copies to `/workspace/smoke_scale/v4_greybox/`.

## Tests

- `Proto_GreyboxRollDizzyTests`: 1.2s gap reset, light **1.5s** non-blocking, 5-chain stun through spin body window. Tick no longer wipes the first roll's chain (sentinel last-end time). EditMode **65/65** passed.

## Out of scope

- No commit, push, or mobile builds in this task.