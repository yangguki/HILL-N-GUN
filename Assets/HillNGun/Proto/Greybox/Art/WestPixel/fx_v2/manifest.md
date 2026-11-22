# fx_v2 — HILL N GUN v4 west pixel FX + player roll/dizzy (dev frames, 2026-11-21)

Same style as skin_west_pixel: **1 art px = 3x3 screen px, NEAREST only**, transparent PNG. Unity: **Filter Point, Compression None, Mip off, PPU 100** (ortho 5.4 → 1:1 at 1080p). 12+, no blood.
Pivots are Unity-normalized (origin bottom-left). Directional sprites point RIGHT (0°) unless noted.
Animated sets: `<name>_NN.png` frames (all the same canvas) + `<name>_sheet.png` (horizontal strip, same order).

## Player roll (new) — player_back compatible
| set | frames | size px | pivot | fps | mode | notes |
|---|---|---|---|---|---|---|
| roll_left_00..10 (+ roll_left_sheet) | 11 | 306x213 | (0.5, 0.028) feet — identical to player_back_* | 22 | one-shot (~0.5s) | 00 stand, 01 crouch, 02 dive, 03 tuck, 04 roll90, 05 upside, 06 roll270, 07 land, 08 hat_up, 09 hat_down, 10 stand |
| roll_right_00..10 (+ roll_right_sheet) | 11 | 306x213 | (0.5, 0.028) feet — identical to player_back_* | 22 | one-shot (~0.5s) | 00 stand, 01 crouch, 02 dive, 03 tuck, 04 roll90, 05 upside, 06 roll270, 07 land, 08 hat_up, 09 hat_down, 10 stand |
| roll_forward_00..10 (+ roll_forward_sheet) | 11 | 306x213 | (0.5, 0.028) feet — identical to player_back_* | 22 | one-shot (~0.5s) | 00 stand, 01 crouch, 02 head_down, 03 ball, 04 boots_up, 05 ball2, 06 unfold, 07 rise, 08 hat_up, 09 hat_down, 10 stand |
| roll_back_00..10 (+ roll_back_sheet) | 11 | 306x213 | (0.5, 0.028) feet — identical to player_back_* | 22 | one-shot (~0.5s) | 00 stand, 01 sit, 02 legs_up, 03 ball, 04 ball2, 05 legs_over, 06 squat, 07 rise, 08 hat_up, 09 hat_down, 10 stand |
| roll_{left,right,forward,back}_afterimage.png | 1 | 306x213 | (0.5,0.028) | – | – | Purple 43%-alpha silhouette of tuck frame. Optional: spawn 2–3 copies at previous positions on frames 03–06, fade 0.15s. |
| roll_dust_takeoff_00..03 | 4 | 90x42 | (0.5,0.21) bottom-center at takeoff foot | 16 | one-shot | Spawn at roll frame 01 (crouch) at feet; offset opposite to roll dir by ~12px. |
| roll_dust_land_00..03 | 4 | 132x42 | (0.5,0.21) bottom-center at feet | 16 | one-shot | Spawn at roll frame 07 (land/squat/unfold). |

**Roll timing:** 00 stand, 01 crouch (takeoff dust), 02 dive, 03–06 tuck/rotate (good i-frame window), 07 land/unfold (landing dust), 08 hat up 1px, 09 hat down 1px, 10 stand. Frames are **in place**; the engine moves the body (roll distance over frames 02–07, ease-out). Frame 00 and 10 = player_back_idle_0, so swapping in/out is seamless.
Right = horizontal mirror of Left (revolver hand flips during the roll only). Forward = somersault into the screen (move up/away), Back = backward roll toward camera (move down). 8-direction: use Left/Right for diagonals (or mix with Forward/Back by dominant axis).

## Dizzy chain after a roll
Roll counter: count rolls whose start is within 1.2s of the previous roll end; reset otherwise.
- **1–2 rolls:** roll 00→10 → idle.
- **3–4 rolls (light):** roll 00→09, then `dizzy_light_in_fx` (2f, over the standing body), then loop `dizzy_light_body` + `dizzy_light_fx` together (same frame index) at 8fps for ~1.5s (3 loops) → idle. Gameplay idea: move −15%, slight aim wobble.
- **5+ rolls (spin):** roll 00→09 → `dizzy_spin_in_body`+`_fx` (2f) → `dizzy_spin_body`+`dizzy_spin_fx` (12f @12fps = 1.0s, input locked) → `dizzy_spin_out_body`+`_fx` (3f) → idle.
- Body sets use the player canvas (306x213, same feet pivot). FX overlays use a wider/taller canvas (378x285) with the **same feet point** (pivot (0.5,0.021)) — just parent the overlay to the player at the feet.

### Dizzy
| file / set | frames | size px | pivot | fps | mode | notes |
|---|---|---|---|---|---|---|
| dizzy_star.png | 1 | 15x15 | center | – | – |  |
| dizzy_bird.png | 1 | 18x15 | center | – | – | Flip X when moving left on orbit back half. |
| dizzy_light_body_00..03 (+sheet) | 4 | 306x213 | (0.5,0.028) feet — same as player_back | 8 | loop | 3-roll light dizzy body sway ±3°. Play ~1.5s (3 loops) then idle. |
| dizzy_light_fx_00..03 (+sheet) | 4 | 378x285 | (0.5,0.021) feet (overlay canvas 378x285, feet 6px above bottom; same feet point as body) | 8 | loop | 2 stars orbit. Sync frame-by-frame with dizzy_light_body. |
| dizzy_light_in_fx_00..01 (+sheet) | 2 | 378x285 | (0.5,0.021) feet (overlay canvas 378x285, feet 6px above bottom; same feet point as body) | 12 | one-shot | Stars pop in (1 then 2) over the roll's last frame (hat_down/stand). |
| dizzy_spin_in_body_00..01 (+sheet) | 2 | 306x213 | (0.5,0.028) feet — same as player_back | 12 | one-shot | Stagger into spin. |
| dizzy_spin_in_fx_00..01 (+sheet) | 2 | 378x285 | (0.5,0.021) feet (overlay canvas 378x285, feet 6px above bottom; same feet point as body) | 12 | one-shot | Orbit builds up. |
| dizzy_spin_body_00..11 (+sheet) | 12 | 306x213 | (0.5,0.028) feet — same as player_back | 12 | one-shot (1.0s, = 2×6-frame cycle) | 5+ rolls: 1.0s wobble/spin ±9°, mirrored frames sell the spin. Input locked. |
| dizzy_spin_fx_00..11 (+sheet) | 12 | 378x285 | (0.5,0.021) feet (overlay canvas 378x285, feet 6px above bottom; same feet point as body) | 12 | one-shot (1.0s) | Stars + birds orbit + swirl-eye bubble. |
| dizzy_spin_out_body_00..02 (+sheet) | 3 | 306x213 | (0.5,0.028) feet — same as player_back | 12 | one-shot | Recover → idle. |
| dizzy_spin_out_fx_00..02 (+sheet) | 3 | 378x285 | (0.5,0.021) feet (overlay canvas 378x285, feet 6px above bottom; same feet point as body) | 12 | one-shot | Orbit fades out. |

### Bullets & impacts
| file / set | frames | size px | pivot | fps | mode | notes |
|---|---|---|---|---|---|---|
| bullet_ally_tracer.png | 1 | 144x21 | (0.92,0.5) head center | – | – | Points RIGHT (0°). Rotate to travel dir. Tail fades left. |
| bullet_ally.png | 1 | 27x21 | center | – | – | Head only (no tail), 27x21. |
| muzzle_flash_00..02 (+sheet) | 3 | 51x51 | center | 30 | one-shot | ~0.1s. Spawn at gun tip. |
| bullet_enemy.png | 1 | 138x60 | (0.75,0.5) head center | – | – | Points RIGHT. Big, slow, red/orange + tail. |
| hit_spark_00..02 (+sheet) | 3 | 57x57 | center | 24 | one-shot | On enemy hit. No blood. |
| dust_ground_00..02 (+sheet) | 3 | 78x60 | center | 16 | one-shot | Bullet hits ground. |
| dust_crate_00..02 (+sheet) | 3 | 78x60 | center | 16 | one-shot | Bullet hits crate: dust + splinters. |

### Reload casings
| file / set | frames | size px | pivot | fps | mode | notes |
|---|---|---|---|---|---|---|
| casing.png | 1 | 12x6 | center | – | – | Single empty casing (6x4 art). Rotate 90° steps for spin. |
| reload_casings_1_00..11 (+sheet) | 12 | 108x132 | (0.5,0.909) = cylinder/gun point (18,4 art from top) | 20 | one-shot | 1 empty casing(s) fall ~114px to floor, bounce, fade. Play at reload start with k = rounds fired. |
| reload_casings_2_00..11 (+sheet) | 12 | 108x132 | (0.5,0.909) = cylinder/gun point (18,4 art from top) | 20 | one-shot | 2 empty casing(s) fall ~114px to floor, bounce, fade. Play at reload start with k = rounds fired. |
| reload_casings_3_00..11 (+sheet) | 12 | 108x132 | (0.5,0.909) = cylinder/gun point (18,4 art from top) | 20 | one-shot | 3 empty casing(s) fall ~114px to floor, bounce, fade. Play at reload start with k = rounds fired. |
| reload_casings_4_00..11 (+sheet) | 12 | 108x132 | (0.5,0.909) = cylinder/gun point (18,4 art from top) | 20 | one-shot | 4 empty casing(s) fall ~114px to floor, bounce, fade. Play at reload start with k = rounds fired. |
| reload_casings_5_00..11 (+sheet) | 12 | 108x132 | (0.5,0.909) = cylinder/gun point (18,4 art from top) | 20 | one-shot | 5 empty casing(s) fall ~114px to floor, bounce, fade. Play at reload start with k = rounds fired. |

### FIRE cylinder button
| file / set | frames | size px | pivot | fps | mode | notes |
|---|---|---|---|---|---|---|
| btn_fire_cyl_0.png | 1 | 192x192 | center | – | – | 0/5 rounds. Convention: chamber k at angle -90+72k-72n; next empty chamber at top. |
| btn_fire_cyl_1.png | 1 | 192x192 | center | – | – | 1/5 rounds. Convention: chamber k at angle -90+72k-72n; next empty chamber at top. |
| btn_fire_cyl_2.png | 1 | 192x192 | center | – | – | 2/5 rounds. Convention: chamber k at angle -90+72k-72n; next empty chamber at top. |
| btn_fire_cyl_3.png | 1 | 192x192 | center | – | – | 3/5 rounds. Convention: chamber k at angle -90+72k-72n; next empty chamber at top. |
| btn_fire_cyl_4.png | 1 | 192x192 | center | – | – | 4/5 rounds. Convention: chamber k at angle -90+72k-72n; next empty chamber at top. |
| btn_fire_cyl_5.png | 1 | 192x192 | center | – | – | 5/5 rounds. Convention: chamber k at angle -90+72k-72n; next empty chamber at top. |
| btn_fire_refill_0to1_00..04 (+sheet) | 5 | 192x192 | center | 20 | one-shot | One round: slide in, seat, cylinder turns 72°. ~0.25s. Chain n→n+1 for full reload. |
| btn_fire_refill_1to2_00..04 (+sheet) | 5 | 192x192 | center | 20 | one-shot | One round: slide in, seat, cylinder turns 72°. ~0.25s. Chain n→n+1 for full reload. |
| btn_fire_refill_2to3_00..04 (+sheet) | 5 | 192x192 | center | 20 | one-shot | One round: slide in, seat, cylinder turns 72°. ~0.25s. Chain n→n+1 for full reload. |
| btn_fire_refill_3to4_00..04 (+sheet) | 5 | 192x192 | center | 20 | one-shot | One round: slide in, seat, cylinder turns 72°. ~0.25s. Chain n→n+1 for full reload. |
| btn_fire_refill_4to5_00..04 (+sheet) | 5 | 192x192 | center | 20 | one-shot | One round: slide in, seat, cylinder turns 72°. ~0.25s. Chain n→n+1 for full reload. |
| btn_fire_empty_blink_00..01 (+sheet) | 2 | 192x192 | center | 4 | loop | While 0/5. Tap = reload. |

### Bomb
| file / set | frames | size px | pivot | fps | mode | notes |
|---|---|---|---|---|---|---|
| dynamite_spin_00..03 (+sheet) | 4 | 60x60 | center | 16 | loop | Fuse spark flickers every frame. |
| smoke_puff_0.png | 1 | 21x21 | center | – | – | Trail puff, spawn every ~0.05s, fade alpha 1→0 over 0.4s, grow 1.0→1.4. |
| smoke_puff_1.png | 1 | 27x27 | center | – | – | Trail puff, spawn every ~0.05s, fade alpha 1→0 over 0.4s, grow 1.0→1.4. |
| smoke_puff_2.png | 1 | 33x33 | center | – | – | Trail puff, spawn every ~0.05s, fade alpha 1→0 over 0.4s, grow 1.0→1.4. |
| bomb_shadow.png | 1 | 42x15 | center | – | – | Ground shadow under bomb; scale 0.6→1.0 and alpha 0.5→1 as it falls. |
| explosion_00..04 (+sheet) | 5 | 168x144 | (0.5,0.42) blast center | 16 | one-shot | Single size 168x144. Engine scales by depth (use h(yf)/120 like grunts, ×~1.2). |

### Aim guides
| file / set | frames | size px | pivot | fps | mode | notes |
|---|---|---|---|---|---|---|
| aim_band.png | 1 | 60x300 | (0.5,0) bottom-center (at player gun) | – | – | Points UP. Rotate to aim dir; scale Y only for range (taper keeps). 60x300 px = 100 art px long. |
| aim_tip.png | 1 | 27x27 | center | – | – | Place at band end (max range). |
| aim_bomb_dot.png | 1 | 12x12 | center | – | – | Place along parabola every ~1/16 of flight; y_screen = lerp - h*4t(1-t). |
| aim_bomb_land.png | 1 | 90x39 | center | – | – | Landing/blast radius. Flattened 0.43 for perspective. Scale by depth. |

Notes: tracer is 1.5x the concept (head 27x21px, total 144x21). Reload casings replace per-shot casings: at reload start play `reload_casings_k` with k = rounds fired (1–5) at the revolver; floor is ~114px below the pivot (≈ hand→feet at 1x). Cylinder: full reload = chain refill n→n+1 from current count to 5 (~0.25s each). Explosion is one size; scale in engine by depth (grunt formula h(yf)/120, ×~1.2).

## Previews
- preview.png — 1920x1080 on perspective_v1_1 battle screen (composite of many moments).
- roll_strip_2x.png — all 4 roll directions, 2x zoom.
- roll_concept_sheet.png — roll phases, dust/afterimage placement, dizzy chain.
- _src/ — roll source image (generated) + build scripts (run from skin_west_pixel: build_roll → build_fx → build_dizzy → build_preview → build_rollsheets).
