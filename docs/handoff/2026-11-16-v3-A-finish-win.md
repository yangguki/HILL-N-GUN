# v3 Step A finish (Windows PC) — Frontline BG v2.1, walk bounds, arch, fence, covers, bot pacing

Date: 2026-11-16 KST. Branch `feat/v3-pixel-topdown` (base `cf157e5`). Read `2026-11-16-v3-A-vsplit.md` first: its Step A work is already implemented (top/bottom split, `FrontlineMatch.SplitAxis`, fence H fields, hay/crate stages, 3v3 distinct slots). This pass finishes A against the new background.

## Hard rules
- Step A only. Do NOT touch `ArtSource/proto_v3/v2/anim/` or character animation (Step B is next).
- No commit/push. No builds (APK/iOS/other). Do not modify `LocalIOSBuild.cs`, `ProjectSettings/`, `EditorBuildSettings`, `FrontlineRules.Judge`, `FrontlineRules.RoundSeconds` (90), HP, damage, range, fire interval, or collision sizes of covers.
- Do NOT launch Unity (no tests, no smoke, no compile run). The bot will run EditMode tests and the windowed smoke afterwards. Keep code compiling by inspection.
- Windows machine; Unity 6000.0.85f1 at `C:\Program Files\Unity\Hub\Editor\6000.0.85f1\Editor\Unity.exe`.

## 1) Frontline background v2.1
- New file: `ArtSource/proto_v3/v2/bg_frontline_v2_1_1920.png` (1920x1080). Copy to `Assets/HillNGun/Proto/Art/v2/` with the same importer settings as `bg_frontline_v2_1920.png` (Bilinear, no compression, Sprite). Commit-ready `.meta` (let Unity generate on import is fine; or write a meta mirroring the existing BG meta with a fresh GUID).
- Make it the `background` of `ProtoPixelArt_Frontline.asset` (via `ProtoPixelArtSetup` so the asset is data-driven). Keep v2 file in repo, unused.
- Layout of v2.1: shops on the LEFT and RIGHT edges, dirt is the center field, top-center entrance arch, bottom fence with a center gap. Reference composite: `ArtSource/proto_v3/v2/v2_1_frontline_preview.jpg` (1024x576 = 1920x1080 scaled by 0.5333; multiply preview px by 1.875).
- Top/bottom split stays default: player team bottom, enemy top (exactly like the preview).

## 2) Walk / spawn / clamp bounds
- Movement area = dirt rect **x 358–1563, y 128–916 px** on the 1920x1080 plate (y from top). Convert to world via the existing BG fit math; store as data on the art SO (e.g. reuse `dirtUvMin/dirtUvMax` as UV of that rect: uMin=358/1920, uMax=1563/1920, vMin=1-916/1080, vMax=1-128/1080). Spawns and clamps for both sides must be inside it (player half = below midline, enemy half = above).
- Top entrance arch occupies y 60–262; its two posts are at approx x 790–830 and x 1085–1125 px (preview x ~421–443 and ~579–601), y ~60–262. Either add two small static post obstacles (prefer: same obstacle mechanism covers use, non-destructible, display nothing extra since the BG draws them) or clamp the enemy side so units stay below y=262 in the post columns. Pick one, document it.
- The crates/barrels in front of the shops on the far left/right are BG decoration: no colliders (they are outside the dirt rect anyway).
- The bottom fence/gap (y > 916) is outside bounds; no exit.

## 3) Center horizontal fence (prop, display only)
- Preview rail: spans approx **x 506–1425 px**, centered y ~**512** px, rail visual height ~**41 px** (posts incl.) at 1920x1080. Center it on the split line, tile `prop_fence_h_tile_v2` with `prop_fence_h_cap_v2` caps at both ends.
- Current horizontal scale 0.10. Planner asked 1.3x the previous value (=0.13). Measure: the visible rail content height inside the 1107x438 tile at scale 0.13 vs the 41 px target; if 0.13 matches the preview within ~20%, use 0.13; otherwise use the preview-matching value. Also make the fence's total length roughly match x 506–1425 (fewer tiles or a length param), not full dirt width. Report the final scale and length.
- No collision, no movement-limit change from the fence.

## 4) Cover layout (top/bottom mode, follow the preview)
Preview positions converted to 1920x1080 px (center-bottom foot roughly at the lower edge):
- Enemy half (top): brick wall `prop_wall_h_v2` ~ x 420–620 / y 155–240; hay ~ x 1335–1425 / y 170–250; sandbags ~ x 450–590 / y 350–420; crate ~ x 1385–1455 / y 355–430.
- Player half (bottom): sandbags ~ x 450–590 / y 580–650; barrel ~ x 1385–1455 / y 560–650; hay ~ x 555–650 / y 795–875; crate ~ x 1305–1375 / y 800–875.
Reposition existing cover instances to these spots (front row = nearest the fence: sandbags/crate/barrel; back row = wall/hay/crate). Keep each cover's collision width/depth/body unchanged; only position and display sprite/scale. Keep the 2-rows-per-side structure in the data/tests; if the preview gives fewer than the current count, keep extra covers mirrored in the remaining lanes rather than deleting slots the 3v3 slot assignment needs (document what you did). Left/right SplitAxis mode must still work (may keep its old layout).

## 5) Bot pacing (FrontlineBot only)
Target bot-vs-bot rounds **45–75 s** (1v1 and 3v3). `_v3` smoke had all rounds hit 90 s. Without combat numbers: shorten hide/hold time, lengthen peek+shoot windows, reduce reaction delay, advance to a nearer cover after 3–4 s without trading, and make the last unit(s) of a team push. Keep peek/roll/advance behaviour and distinct 3v3 slots. Make the tunables serialized fields with defaults so the bot can retune quickly.

## 6) Tests and smoke helper
- Update/add EditMode tests in `Assets/HillNGun/Tests/EditMode/FrontlineTests.cs` (and Proto tests if relevant): dirt rect from the v2.1 data contains all spawns and clamps; player spawns bottom/enemy top; arch posts block (or clamp keeps units below y 262 in the post columns); fence is display-only (no collider); camera fit at 19.5:9 still contains the dirt; existing Judge/tiebreak tests unchanged and passing.
- `ProtoV3Smoke` must write `menu_v4.png`, `topdown_v4.png`, `frontline_1v1_v4.png`, `frontline_3v3_v4.png` plus results with per-round result/reason/time and averages (1v1 x3, 3v3 x3). Make sure `results.txt` clearly lists each round's seconds.

## Report (print at end)
Files changed; bounds data; arch choice; fence scale/length used; cover placement; bot tunables changed (old -> new); tests added/changed; risks. If you stop early append an **Exact state (Windows)** section here.

- 모델 규칙(2026-11-16 14:56 KST, 사용자): cursor-agent는 Cursor 자체 모델(Composer 계열)만 사용. 매 실행에 --model composer-2.5 를 명시(auto 금지).

