# v4 그레이박스 — 작업 13: west pixel 스킨 (플레이 가능)

- 브랜치 `proto/v4-greybox-stage`. 작업 10–12 변경은 워킹 트리에 있음. 그 위에 스킨 레이어만 추가.
- 에셋은 이미 `Assets/HillNGun/Proto/Greybox/Art/WestPixel/` 에 복사됨 (`_src` 없음). 레이아웃·피벗·한계는 그 폴더의 `manifest.md` 와 `preview.png` 를 따른다.
- Unity 임포트: Filter **Point**, Compression **None**, Mip **off**, **PPU 100**, alpha is transparency. 1 art pixel = 3x3 screen px at 1080p.
- 사용자 요청: 이 그레이박스 프로토만 웨스턴 픽셀 아트로 입히고 플레이 가능하게. 이 에이전트 런에서는 Unity 실행·APK·commit·push 금지(호출측에서 테스트·캡처·빌드).

## 하드 룰
1. 변경은 `Assets/HillNGun/Proto/Greybox/` 와 `docs/handoff/` 만. 본편·ProjectSettings·Packages·asmdef 금지.
2. **게임플레이, 히트박스, 튜닝, HUD 버튼 클러스터 위치/히트 영역을 바꾸지 말 것.** 비주얼만 교체.
3. 새 `.meta` guid 는 32자리 16진수. 12세, 피 없음. 기존 EditMode 테스트를 약화하지 말 것.
4. 같은 에러 두 번이면 멈추고 보고.

## 스킨 레이어 설계
- 기본값은 **스킨 ON**. DBG 패널에 토글(예: "West pixel skin")로 그레이박스 절차적 비주얼 ↔ 스킨을 즉시 전환. 전환해도 발 위치, 이동/조준/데미지, 히트 형상은 그대로.
- 런타임에서 스프라이트를 안정적으로 로드해야 APK에도 들어간다.
  권장: Editor `AssetPostprocessor`(경로에 `Art/WestPixel` 포함, preview/target_look/manifest 제외)로 임포트 설정+매니페스트 피벗을 적용하고, `Proto_GreyboxWestPixelCatalog` ScriptableObject에 Sprite 참조를 채운다. `[InitializeOnLoad]` / `OnPostprocessAllAssets` / 빌드 직전(`Proto_GreyboxBuild.BuildAndroid` 시작 시)에 `RebuildCatalog()` 를 호출해 카탈로그가 비어 있지 않게 한다. 런타임은 카탈로그만 읽는다. preview.png·target_look.png·sheet 전체는 카탈로그/빌드에 넣지 말 것(용량).
- 피벗(매니페스트, Unity normalized, 원점 좌하단):
  - fence.png (0,0); fence_post (0.5,0); crate_* (0.5,0)
  - player_back_* (0.5, 0.028); grunt_front_* (0.5, 0.041)
  - bullet_* (0.75, 0.5); muzzle/hit_spark/crosshair/버튼/조이스틱 (0.5, 0.5)
  - hp_frame, hp_fill (0, 0.5) left-center
  - 배경은 쿼드 배치로 맞춰도 됨(피벗 top-left 권장).

## 배경 / 울타리 / 상자
매니페스트(1080p, 위가 0) 기준:
- `bg_far` : y0, 높이 420px (0–39%). 하늘+메사.
- `ground` : y360 (33%)부터. 와이드(20:9)에서는 `ground_tile.png` 를 가로로 타일하여 양쪽이 비지 않게. 16:9는 ground.png 또는 타일 모두 가능.
- `bg_town` : y120 (11%), 높이 300px, 지붕 위 알파, ground 위에 그림.
- 와이드에서 1920 배경이 부족하면 가운데 정렬 후 가장자리는 타일/메사·모래색 확장. 왜곡 스트레치로 픽셀을 늘리지 말 것(Point 필터 유지).
- 기존 Greybox_Sky / Greybox_Ground 절차적 쿼드는 스킨 ON일 때 끄거나 교체.
- 울타리: `fence.png` 를 화면 y **41–46%** 밴드에 가로 반복(왼쪽 포스트+레일). 오른쪽 끝은 `fence_post.png` 로 캡. 지금처럼 레일 사이가 보이게. 히트/커버 로직은 그대로.
- 상자: 기존 위치 유지 (`CrateFar` 52–56.5 / 30–36, `CrateMid` 12–21 / 52–63, `CrateNear` 58–69 / 62–82, 화면%). 발 피벗, Y 정렬 유지. far는 `crate_far`, mid/near는 `crate_near`. 화면 비율(크기)은 기존 월드 박스에 맞출 것.

## 플레이어 / 그런트
- 플레이어: `player_back` 플립북. 기존 상태로 구동.
  - 정지: idle_0/1 루프
  - 이동 중(구르기 아님): run_0..3
  - 발사 직후/조준 발사 순간: shoot_0/1 (팔이 분리되어 있지 않음. 발사 때 shoot만 고르면 됨)
  - 구르기: roll_0..3
  - 피격: hit_0 + 기존 히트 플래시 유지
- shoot 프레임은 화면 **오른쪽**을 본다. 조준 방향이 왼쪽이면 **X 플립**. 오른쪽/위는 플립 없음.
- 발 위치·깊이 스케일 유지. 보이는 **몸** 높이가 현재와 같이 화면의 **19%**(그런트 **11%**)가 되게 스케일. 스프라이트 여백을 감안할 것(플레이어 몸 ≈ 204/213 높이, 그런트 몸 ≈ 120/147). `ScaleForWorldHeightUniform` 을 스프라이트 전체 높이에 그대로 쓰면 몸이 약간 작아진다.
- 그런트: `grunt_front` idle_0/1, shoot_0/1, hit_0, down_0..2 후 **알파 페이드아웃**. 히트/사망 로직은 그대로. 깊이 스케일 유지.

## 탄 / FX / 크로스헤어
- `bullet_enemy` / `bullet_ally` 는 진행 방향으로 회전(아트는 오른쪽 0°, 피벗 머리 쪽 0.75,0.5). 게임플레이 크기(적 0.30, 아군 0.09 월드)는 유지. 스프라이트가 히트보다 조금 커 보여도 됨. 스킨 ON이면 기존 원형 코어+글로우/트레이서는 숨기거나 스프라이트로 대체.
- 플레이어 발사 시 `muzzle_flash_0/1` 짧은 재생(가능하면 총구 근처). 적/아군 탄 명중 시 `hit_spark_0..2`.
- 크로스헤어는 `crosshair.png`.

## HUD
- 기존 클러스터 좌표/히트 **그대로**. 스프라이트만 교체: FIRE `btn_fire`, RELOAD `btn_reload`, GRENADE `btn_grenade`, ROLL `btn_roll`, Pause `btn_pause`, 조이스틱 `joystick_base` / `joystick_knob`.
- 기존 원/아이콘이 스킨 버튼 위에 겹치지 않게(아이콘 숨김 또는 버튼 스프라이트만 보이게).
- 장전 방사형 링과 「장전!」 펄스는 동작 유지, RELOAD 위에.
- HP: `hp_frame` + `hp_fill`(프레임 안쪽 오프셋 아트 기준 x9,y9). 기존 HP 바 자리를 사용.
- SCORE / STAGE 텍스트는 유지. 픽셀풍 볼드 + 아웃라인 정도면 됨(전용 폰트 에셋 필수 아님).

## 테스트 / 스크린샷 헬퍼 / 빌드 이름
- 기존 EditMode를 깨지 말 것. 가능하면 "스킨 토글이 이동 존/플레이어 높이 %를 바꾸지 않음" 정도의 가벼운 테스트만 추가.
- `Proto_GreyboxScreenshot.CaptureTask13Skin()`: 스킨 ON, 전투 HUD가 보이게 1280×720으로 `Builds/v4_greybox/task13_skin.png` 저장. 이 런에서 실행하지 말 것. `EditorApplication.Exit` 는 기존 CaptureTask12Hud 와 같이.
- `Proto_GreyboxBuild` 출력 경로만 `Builds/v4_greybox/HillNGun_v4_greybox_1121_west.apk` 로 변경. 빌드는 실행하지 말 것.

끝나면 이 문서에 적용 파일, 토글 방법, preview 대비 남은 시각 갭, 테스트 이름을 적을 것.

---

## 적용 완료 (2026-11-21 에이전트)

### 변경/추가 파일 (Greybox + handoff만)
- `Scripts/Proto_GreyboxWestPixelCatalog.cs` — `Resources/Proto_GreyboxWestPixelCatalog.asset` 런타임 로드
- `Scripts/Proto_GreyboxWestPixelSkinState.cs` — 기본 ON
- `Scripts/Proto_GreyboxWestPixelSkin.cs` — 배경·울타리·상자·캐릭터·탄/FX·HUD 스킨 레이어
- `Editor/Proto_GreyboxWestPixelImport.cs` — WestPixel 임포트(PPU 100, Point, None, pivot) + 카탈로그 자동 Rebuild
- `Resources/Proto_GreyboxWestPixelCatalog.asset` — 스프라이트 참조(에디터 RebuildCatalog로 채움)
- `Art/WestPixel/*` — 아트(기존) + `Art.meta` / `WestPixel.meta`
- 수정: `Proto_GreyboxPlayerController.cs`, `Proto_GreyboxTarget.cs`, `Proto_GreyboxRuntimeDebugUi.cs`, `Proto_GreyboxRuntimeTuning.cs`, `Proto_GreyboxBuild.cs`, `Proto_GreyboxScreenshot.cs`
- 테스트: `Tests/EditMode/Proto_GreyboxSkinTests.cs`

### 스킨 토글
- **기본:** 스킨 ON (`Proto_GreyboxWestPixelSkinState`, `Proto_GreyboxRuntimeTuning.WestPixelSkin`)
- **런타임:** DBG 칩 → **West pixel skin** 토글 → 즉시 그레이박스 ↔ WestPixel 전환(이동 존·히트·HUD 좌표 동일)
- EditMode `Build()` 는 기존 절차적 Sky/Ground/Fence 유지(StageTests 호환). 스킨은 플레이/시뮬에서 바인딩.

### 배치·캡처·빌드 (실행 안 함)
- 스크린샷: `Proto_GreyboxScreenshot.CaptureTask13Skin()` → `Builds/v4_greybox/task13_skin.png` (1280×720)
- APK 경로: `Builds/v4_greybox/HillNGun_v4_greybox_1121_west.apk` (`Proto_GreyboxBuild.BuildAndroid` 시작 시 `RebuildCatalog()`)

### preview.png 대비 알려진 시각 갭 (manifest와 동일)
- 플레이어 `shoot_*` 가 화면 오른쪽(side-on); 구르기 4프레임 거침; `hit_0` 합성 플래시
- 그런트 down 시퀀스 후 엔진 페이드(피 없음)
- 장전 방사 링·「장전!」·탄알 pips는 기존 UI 유지(버튼만 픽셀 스프라이트)
- 20:9 ground_tile 가로 타일; 배경 가로는 uniform 스케일(픽셀 늘리기 없음)

### 테스트
- 기존 EditMode 유지
- 추가: `WestPixelSkinToggle_DoesNotChangeMoveZoneOrLayoutPercents`, `Build_StillUsesProceduralSkyForEditModeLayoutTest`
