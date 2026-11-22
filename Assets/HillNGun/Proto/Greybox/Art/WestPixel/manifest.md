# HILL N GUN v4 greybox — skin_west_pixel (1차, 2026-11-21)

Style ref: `target_look.png`. Screen basis 1920x1080. 12+, no blood.
**Pixel grid: every asset = 1 art pixel → 3x3 screen px** (art grid 640x360, upscaled with NEAREST only).
Unity import: Filter **Point**, Compression **None**, Mip off, **PPU 100** (ortho size 5.4 → 1080px = 10.8 units, so files show 1:1 at 1080p). Pixel-perfect camera option: ref res 640x360, Assets PPU 33.33 (=100/3).

## Layout used in preview.png (screen px, top-left origin)
| layer | file | position |
|---|---|---|
| sky/mesa | bg_far.png | x0 y0 (opaque) |
| ground | ground.png | x0 y360 (opaque) |
| town | bg_town.png | x0 y120 (alpha above rooftops; drawn OVER ground) |
| enemy row | grunt feet | y 400 (37%) |
| fence | fence.png tiled | top y 443 (41%) → bottom ~500 (46%) |
| player | feet | y 950 (88%) |
Building base line: center buildings ~y 330–350 (30–32%); end buildings curve lower to ~y 400 (37%) at far left/right (U-shaped street from the source art).

## Files
| file | size px | frames | pivot (Unity normalized, origin bottom-left) | notes |
|---|---|---|---|---|
| bg_far.png | 1920x420 | 1 | top-left | RGB. Sky+clouds+mesas. Below mesas filled with flat haze color (hidden by town). |
| bg_town.png | 1920x300 | 1 | top-left | RGBA. Buildings (blue/orange/red/teal awnings, doorways), cacti, sand strip at bottom. |
| ground.png | 1920x720 | 1 | top-left | RGB. Exact match to bg_town colors. Not tileable. |
| ground_tile.png | 1728x720 | 1 | top-left | RGB. Horizontally tileable version (dithered seam blend). |
| fence.png | 141x57 | 1 | (0,0) | Repeat tile: 1 post on LEFT + 2 rails. Tile left→right; cap the right end with fence_post. |
| fence_post.png | 15x57 | 1 | (0.5,0) | Single post (same scale). |
| crate_near.png | 159x153 | 1 | (0.5,0) bottom-center | |
| crate_far.png | 72x75 | 1 | (0.5,0) bottom-center | |
| player_back_{idle_0,idle_1,shoot_0,shoot_1,run_0..3,roll_0..3,hit_0}.png | 306x213 each | 13 | (0.5, 0.028) feet (6px above bottom) | Body ~204px tall (19%). Also player_back_sheet.png (13 frames in a row, same order: idle0,idle1,shoot0,shoot1,run0-3,roll0-3,hit0). |
| grunt_front_{idle_0,idle_1,shoot_0,shoot_1,hit_0,down_0,down_1,down_2}.png | 168x147 each | 8 | (0.5, 0.041) feet | Body ~120px tall (11%). Also grunt_front_sheet.png. |
| bullet_enemy.png | 102x48 | 1 | (0.75,0.5) head center | Points RIGHT (0°); rotate to travel dir. Big red/orange + trail. |
| bullet_ally.png | 30x12 | 1 | (0.75,0.5) | Points RIGHT. Small yellow. |
| muzzle_flash_0/1.png | 42x42 | 2 | center | |
| hit_spark_0..2.png | 42x42 | 3 | center | |
| crosshair.png | 51x51 | 1 | center | Red pixel 4-diamond cross. |
| btn_fire.png | 180x180 | 1 | center | Big circle, yellow core. |
| btn_reload.png / btn_grenade.png / btn_roll.png | 120x120 | 1 | center | |
| btn_pause.png | 78x78 | 1 | center | |
| joystick_base.png | 192x192 | 1 | center | Semi-transparent. |
| joystick_knob.png | 90x90 | 1 | center | |
| hp_frame.png | 336x36 | 1 | left-center | Inner fill area starts at x9,y9. |
| hp_fill.png | 318x18 | 1 | left-center | Scale X or crop for HP %. Place at frame +9,+9. |
| preview.png | 1920x1080 | – | – | Full composite. |

## Known limits (honest)
- **Player shoot_0/1**: the arm points to screen-RIGHT (sideways), not straight forward into the screen. Works as a placeholder; no separate arm/gun part — **aim rotation part not made** (generated sheet could not be split cleanly).
- **Player roll**: 4 frames are rough (crouch → bend → upside-down tumble → stand); not a clean rolling ball loop. Roll_3 is basically a standing pose.
- **Player hit_0**: synthesized = idle_0 white-flashed (not hand-drawn).
- **Player frames** come from one generated sheet: small detail jitter between frames (hat/hands shift a few px).
- **Grunt down**: down_0 stumble, down_1 sitting dizzy w/ stars, down_2 lying flat, hat off, stars. No blood. Fade out in engine (alpha) after down_2. Hit_0 has dizzy-star FX baked in.
- **Grunt idle** holds gun at side; shoot_1 has baked yellow muzzle flash (enemy *bullets* are still red per rule).
- **bg_town**: building row is U-shaped (outer buildings sit lower, to ~37%). Center gap between buildings is small. Small mesa sliver remains at far top-left corner of bg_town.
- **Ground**: top band near building bases was patched with copied sand (some repeat visible if looked for).
- HUD/FX/crosshair are hand-coded pixel art (simple), not generated; no SCORE/STAGE text (planner said optional).
- Nothing tested in engine.

Source/scripts: `_src/` (generated source sheets + build_sprites.py, do_chars.py, do_bg.py, do_props_fx_hud.py, do_preview.py). Re-run in that order from this folder.
