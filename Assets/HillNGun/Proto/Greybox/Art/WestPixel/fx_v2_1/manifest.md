# fx_v2_1 — enemy bullet redraw + grenade gauge (2026-11-21)

Same style: 1 art px = 3x3 screen px, NEAREST only, transparent PNG. Unity import: **Sprite (2D), Filter Point, Compression None, Mip off, PPU 100** (ortho 5.4 → 1:1 at 1080p). 12+, no blood.

## Enemy bullet (replaces fx_v2/bullet_enemy.png)
| file | size px | frames | pivot | fps | mode | notes |
|---|---|---|---|---|---|---|
| bullet_enemy_0..2.png | 102x48 | 3 | (0.65, 0.5) bullet body center | 12 | loop | Points RIGHT (0°); rotate to the travel direction. Red thick head + copper casing + dark outline + red/orange glow + short flame tail; only the tail flickers between frames. |
| bullet_enemy_sheet.png | 306x48 | 3 | – | – | – | Strip of the 3 frames, same order. |
Collider suggestion: circle r≈20px at the pivot (head+body); the tail is visual only.

## Grenade button gauge (overlays btn_grenade.png, 120x120)
| file | size px | frames | pivot | notes |
|---|---|---|---|---|
| btn_grenade_gauge_bg.png | 120x120 | 1 | center | Dark translucent ring track (outer r 59px, inner r 48px), drawn over the button's metal rim. |
| btn_grenade_gauge_fill.png | 120x120 | 1 | center | Full gold ring for Unity **Image → Image Type: Filled, Fill Method: Radial 360, Fill Origin: Top, Clockwise: on**; set fillAmount 0–1. Same size/position as bg. |
| btn_grenade_gauge_000/025/050/075/100.png | 120x120 | 5 | center | Baked bg + fill steps for preview / fallback (top, clockwise). |
| btn_grenade_gauge_full_0..1.png (+ full_sheet 276x138) | 138x138 | 2 | center (same center as the 120 button, overhangs 9px each side) | Ready/full flash: bright ring + outer halo. Play 0→1 at 10fps 2–3 times when it reaches 100%, then hold _100 (or loop slowly at 4fps for "ready"). |

Draw order (UI): btn_grenade → gauge_bg → gauge_fill (Filled) → full flash (only at 100%).

preview.png: 1920x1080 on the perspective_v1_1 screen. New enemy bullets (3 flicker frames) next to fx_v2 yellow tracers; grenade gauge at 50% on the HUD; top-left inset shows 0/25/50/75/100% and the full flash at 1x.
Script: build.py (run from skin_west_pixel: `python3 fx_v2_1/build.py`).
