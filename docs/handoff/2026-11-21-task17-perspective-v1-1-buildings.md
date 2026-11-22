# Task 17 — perspective_v1_1 building crop / ground fill (2026-11-21)

Designer drop: `skin_west_pixel/perspective_v1_1/` (same sizes, positions, pivots, and depth formula as v1).

## Art swap
Replaced only these three files in `Assets/HillNGun/Proto/Greybox/Art/WestPixel/perspective_v1/` (`.meta` left untouched: Point filter, PPU 100, center pivot, no mips):

- `bld_left.png` (960×330 at (0,270)): bottom trimmed; all pixels at screen y ≥ 540 transparent; sidewalk lip (~132px) on the cut. Lowest opaque screen y = 539 (v1: 599).
- `bld_right.png` (960×318 at (960,273)): same, lip ~123px. Lowest opaque screen y = 539 (v1: 590).
- `ground.png` (1920×732 at (0,348)): sand texture fills the newly exposed strip under the trimmed buildings (y ~537–605 at the edges).

No layout/placement code changes.

## Overlap with the 52% enemy row and fence
**Gone.** Buildings now end at screen y 539, which is above the 52% foot line (y 562) and the fence top (y 583). The engine capture shows a sidewalk lip, then sand, then the fence — the old boardwalk/shadow no longer crosses the 52% row or the fence at the left/right edges. (This wave-1 shot does not place a grunt exactly at the far left/right of the 52% row, but the building pixels themselves no longer reach those bands.)

## Tests / captures
- Greybox EditMode: **46/46 passed**.
- `ProjectSettings/ProjectSettings.asset` restored after Unity.
- Screenshot: `/workspace/smoke_scale/v4_greybox/task17_depth.png` (1920×1080, west skin + perspective ON).
- No APK / iOS builds. No commit/push.
