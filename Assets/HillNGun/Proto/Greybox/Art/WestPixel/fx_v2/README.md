# West Pixel fx_v2 (designer drop-in)

Optional sprites for Task 19 hooks. **Missing files are OK** — runtime uses procedural placeholders.

Folder: `Assets/HillNGun/Proto/Greybox/Art/WestPixel/fx_v2/`

Wire via `Proto_GreyboxFxV2Catalog` (`Resources/Proto_GreyboxFxV2Catalog.asset`).

## Expected filenames (suggested)

| Group | Files |
|-------|--------|
| Cylinder / reload | `cylinder_full.png`, `cylinder_empty.png`, `cylinder_blink.png`, `casing_01.png`… |
| Dizzy | `dizzy_star_lite_01.png`…, `dizzy_star_spin_01.png`…, `dizzy_bird_01.png`… |
| Roll 8f | `roll_left_01.png`…`08`, `roll_right_01`…, `roll_front_01`…, `roll_back_01`… |
| Roll dust | `roll_dust_start_01.png`…, `roll_dust_land_01.png`… |
| Bullets | `muzzle_01.png`…, `tracer_ally.png`, `spark_hit_01.png`…, `dust_ground_01.png`…, `dust_crate_01.png`… |
| Grenade | `dynamite_spin_01.png`…`04`, `explosion_01.png`…`05`, `smoke_trail_01.png`… |
| Aim | `aim_band_cap.png`, `grenade_aim_dot.png`, `grenade_land_ellipse.png` |

Import: PPU 100, point filter (match `Proto_GreyboxWestPixelCatalog.ImportPpu`).