# 2D chibi skin (slice J, 2026-11-23)

Flat 2D chibi style for the third view option (`View2DChibi`): thick dark outlines, flat bright colours,
a western street with SALOON / STORE / HOTEL / SHERIFF lining it, a chibi gunslinger (seen from behind),
and chibi bandit enemies (front-facing). It's a skin swap only. The 2D gameplay, hit tests, and the
3-heart logic are unchanged.

## License

**Self-made, CC0 1.0 (public domain dedication).** Every PNG here was drawn from code with Pillow.
See `Source~/` (Unity ignores folders that end in `~`). No third-party art was traced or cut.
The GPT/Grok concept image was used only as a look reference.
The only third-party piece is the **Lilita One** font, which is baked into the button and plate
labels and used for the SCORE text. It is SIL OFL 1.1, already in `Greybox3D/Art/Fonts/OFL.txt`
and listed in `Greybox3D/ThirdParty/LICENSES.md`.

Regenerate: `cd Source~ && python3 build_all.py` (it writes to `chibi_out/Chibi/`; copy the PNGs here).

## Canvas sizes and pivots

These match the WestPixel set so the shared `Proto_GreyboxWestPixelSkin` layout math places both styles the same way:

| Sprite | Size | Pivot |
|---|---|---|
| `player_back_*` (idle 2, shoot 2, run 4, roll 11, hit 1) | 306×213, body 204 px | (0.5, 0.028) |
| `grunt_front_*` (idle 2, shoot 2, hit 1, down 3) | 168×147, body 120 px | (0.5, 0.041) |
| `bg_full_scene` | 1920×1080, top-left at screen top | center |
| `fence` / `fence_post` | 132×54 / 15×54 | (0,0) / (0.5,0) |
| `crate_d40/46/52`, `crate_near/far` | same as pixel | (0.5, 0) |
| `btn_fire` 180, `btn_reload/grenade/roll` 120, `btn_pause` 78, `joystick_base` 192, `joystick_knob` 90 | square | center |
| `score_plate` 360×80, `stage_plate` 400×80, `hp_plate` 560×80, `heart_full/empty` 64×58 | | center |

`Proto_GreyboxChibiImport` imports this folder as bilinear sprites at PPU 100. The four background
plates are CompressedHQ. It also rebuilds `Resources/Proto_GreyboxChibiCatalog.asset`
(same `Proto_GreyboxWestPixelCatalog` type, `isChibi = true`).
