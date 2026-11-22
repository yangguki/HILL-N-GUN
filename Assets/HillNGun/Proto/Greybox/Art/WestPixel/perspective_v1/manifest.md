# HILL N GUN v4 — skin_west_pixel / perspective_v1 (2026-11-21)

Rear 1-point perspective (camera behind/above, ~25–30° down). Same pixel style as the parent folder:
**1 art px = 3x3 screen px, NEAREST only**. Unity: Point filter, no compression, **PPU 100** (ortho size 5.4 → 1:1 at 1080p).
Screen 1920x1080, positions = top-left corner in screen px (y from top). Unity pivot noted as normalized (origin bottom-left).

## Layout values (planner)
- Vanishing point: x 960 (50%), y 356 (33%). The street end in the art sits at about y 350–360.
- Enemy feet rows: 40% (432) / 44% (475) / 48% (518) / 52% (562).
- Fence: y 583–637 (54–59%).
- Player feet: y 670–1026 (62–95%), body height ~205 (19%). Preview uses feet y 950.

## Depth-scale formula (enemies and crates)
```
yf    = feet_y / 1080, clamped to [0.40, 0.52]
h(yf) = 1080 * (0.06 + (yf - 0.40) / 0.12 * 0.05)     # enemy body height in px: 64.8 at 40%, 118.8 at 52%
grunt scale = h(yf) / 120      # grunt_front_*.png body is ~120px (11%) at scale 1
crate height ≈ 0.62 * h(yf)
```
Non-integer runtime scaling breaks the 3px grid slightly. That is acceptable for the greybox. For pixel-perfect output, snap the scale to the art grid (pre-baked crates below show this).

## Draw order (back → front)
bg_sky_far → ground → bld_left / bld_right → grunts and crates, sorted by feet y (far first) → fence → bullets/FX → player → HUD

## Files
| file | size px | place at (x,y top-left) | pivot | notes |
|---|---|---|---|---|
| bg_sky_far.png | 1920x381 | (0,0) | top-left | Opaque. Sky (the top ~110px is an extended plain gradient), clouds, mesas. Area behind the buildings is filled with flat haze. |
| ground.png | 1920x732 | (0,348) | top-left | Opaque sand road, perspective trapezoid from the VP, grass tufts. Area under the buildings is filled with sand noise (hidden by the building layers). |
| bld_left.png | 960x330 | (0,270) | top-left | Transparent. Left building row converging to the VP (nearest building at x 0–~11%, base ~y 560–600). Blue/orange/blue/red/orange awnings, doors, cacti, barrel. |
| bld_right.png | 960x318 | (960,273) | top-left | Transparent. Right row (red/blue/blue/red awnings), cacti, barrels. |
| fence.png | 132x54 | top y 583, tile left→right from x=0 | (0,0) bottom-left = (0,1) top | Near-size repeat piece (post on left + 2 rails). Preview leaves a center gap at x 860–1060. |
| fence_post.png | 15x54 | top y 583 | (0.5,0) | End cap post. |
| crate_d40.png | 42x39 | bottom-center on feet y 432 | (0.5,0) | Crate at 40% depth. |
| crate_d46.png | 60x57 | bottom-center on feet y 497 | (0.5,0) | Crate at 46% depth. |
| crate_d52.png | 78x75 | bottom-center on feet y 562 | (0.5,0) | Crate at 52% depth. Player-zone crate = ../crate_near.png (159x153). |
| preview.png | 1920x1080 | – | – | Full composite: 4 enemy rows (12 grunts, scaled by h(yf)), crates, fence, bullets, player_back_shoot_1 at feet 88%, HUD from parent folder. |
| preview_guides.png | 1920x1080 | – | – | Same with guide lines at 33/40/52/54/59/62/95% and a VP marker. |
| compare_target.png | 2580x770 | – | – | Side by side: preview (left) vs target_look.png (right). |
| _src/ | – | – | – | persp_plate.jpg (generated source) + do_persp.py, do_preview.py (run from the parent folder). |

Sprites reused unchanged from the parent folder: player_back_*, grunt_front_*, bullet_*, muzzle/hit FX, crosshair, HUD.

## Known limits
- The building rows come from one generated plate, split by color/geometry. Their bottom edge (boardwalk/shadow) is slightly ragged in places. Far-end buildings near the VP are tiny and blurry-ish.
- The plate was vertically squashed ~10% to put the street end at 33%. The buildings are a little flatter than the source.
- The nearest buildings' bases reach ~y 560–600 at the screen edges, so they overlap the 52% enemy row and the fence line at the far left/right. In the target, the near buildings end higher.
- The convergence is milder than a true 1-point street. The rows bend toward the VP but do not follow exact straight lines to (960,356).
- Sizes follow the planner's numbers. Compared with target_look.png, the target's player (~25% height), near crate and enemy bullets are noticeably bigger. If the target's feel is wanted, the enemy bullets in particular should be scaled ~1.5–2x.
- The sky top (~110px) is a flat gradient with no clouds.
- Not tested in engine.
