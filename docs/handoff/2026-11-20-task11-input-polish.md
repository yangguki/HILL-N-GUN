# v4 그레이박스 — 작업 11: 남은 입력 이슈 + 일시정지/재시작 + 와이드 화면

- 브랜치 `proto/v4-greybox-stage`. 작업 10(조준 Y 부호, 조이스틱 좌표계)은 이미 워킹 트리에 있음. 그 위에 이어서 수정.
- 사용자 승인(Asia/Seoul 2026-11-20 23:06): 열려 있던 작은 이슈를 고친 뒤 APK 빌드. 이 에이전트 런에서는 빌드하지 말 것.

## 하드 룰
1. 변경은 `Assets/HillNGun/Proto/Greybox/` 와 `docs/handoff/` 만. 본편·ProjectSettings·Packages·asmdef 금지.
2. APK 빌드·git commit·push·Unity 에디터/배치 테스트 실행 금지(호출측에서 테스트·빌드).
3. 같은 에러가 두 번이면 멈추고 보고. 12세 등급, 피 없음.
4. 새 `.meta` guid 는 32자리 16진수. 최소 수정.
5. 작업 10 테스트를 약화하지 말 것. `Build_MatchesLayoutRatios` 도 약화하지 말 것.

## 고칠 것

### 1. RELOAD / ROLL 히트 영역이 겹치지 않게
현재:
- ROLL  `73–83, 72–84` (from-top %)
- RELOAD `76–86.5, 62–73` — 중심 81.5, 67
- FIRE   `88–98, 66–79` — 중심 93, 72.5
겹침: x 76–83, y 72–73.

약간 줄이거나 옮겨 최소 1% 이상 간격을 둔다. **RELOAD는 계속 FIRE의 왼쪽/왼쪽 위.** 비주얼(버튼 원, 라벨, 장전! 위치)도 히트와 맞출 것. ROLL과도 FIRE와도 겹치면 안 된다. 상수를 테스트에서 바로 검증할 수 있게 유지.

### 2. 조이스틱은 존 안에서 Began 한 손가락만
`PollTouchJoystick`가 `_joyFingerId == -2 && (p.Began || p.Held)` 로 캡처해서, 다른 곳에서 미끄러져 들어온 손가락이 이동을 훔친다. **Began 이면서 조이스틱 히트 안에 있을 때만** 새 손가락을 잡는다. FIRE/ROLL/RELOAD/GRENADE/DBG 등 버튼에서 시작한 손가락은 플레이어를 움직이면 안 된다(Began-only 로 해결). 이미 잡은 손가락의 Moved/Stationary 추적은 유지.

가능하면 "이 스크린 좌표에서 Began 하면 조이스틱을 잡는지"를 순수 함수로 두어 테스트한다.

### 3. 결과 화면은 '다시하기' 버튼(또는 PC에서 R/Enter)만 재시작
지금은 `PollScreenTapForRestart`가 DBG가 아닌 아무 탭이나 재시작한다. 결과 오버레이에 보이는 **「다시하기」** 버튼을 두고, 그 히트(와 `KeyCode.R` / `KeyCode.Return` / `KeyCode.KeypadEnter`)만 `RestartPressed`로 친다. 힌트 문구 "탭하여 다시 시작"은 버튼이 있으면 그에 맞게 바꾼다. 한글 폰트는 기존 `GetUiFont`를 쓴다.

버튼 히트 상수(퍼센트)를 테스트할 수 있게 공개한다.

### 4. Pause 버튼이 실제로 동작
HUD의 Pause 원(`Greybox_Hud_Pause`, 오른쪽 위)을 토글한다.
- `Time.timeScale` 0 ↔ 1
- 간단한 딤 오버레이: 제목 **「일시정지」**, 버튼 **「계속」**(재개), **「다시하기」**(런 재시작 + timeScale=1)
- 일시정지 중에는 이동/조준/발사/구르기/장전 등 전투 입력을 무시. Pause/계속/다시하기만 받는다.
- 재개 또는 재시작 시 `timeScale=1`을 반드시 복구. 결과 화면으로 넘어갈 때도 1로 복구.
- Update는 timeScale=0에도 돌므로, `Simulate` 앞에서 일시정지 분기를 명확히 할 것.

순수하게 테스트하기 쉽게: 일시정지 플래그 + "일시정지 중이면 발사/이동 입력을 적용하지 않음"을 EditMode에서 `Time.timeScale`에 의존하지 않고 검증할 수 있게 한다(컨트롤러에 `IsPaused` / `SetPaused` 또는 입력 필터 함수).

### 5. 와이드 폰(20:9, 19.5:9)에서 조준 각도·HUD가 왜곡되지 않게
지금 `ScreenDeltaToWorldDrag`와 레이아웃 월드 폭이 `TargetAspect`(16:9) 고정이라, 2400×1080에서는 픽셀당 X 월드가 너무 작아 같은 픽셀 대각선 드래그의 각도가 더 가팔라진다. `Proto_GreyboxCameraSetup`도 `cam.aspect = TargetAspect`로 강제한다.

요구: 조준/드래그 수학과 HUD 배치가 넓은 화면에서 맞을 것. 조준 각도는 **카메라의 실제 aspect / 월드 변환**을 써서 왜곡이 없어야 한다.

구현 지침:
- 런타임 카메라는 기기 화면 aspect를 쓴다(16:9로 강제하지 않음). 16:9 화면에서는 그대로 16:9.
- `ScreenDeltaToWorldDrag(delta, screenW, screenH)`의 월드 폭은 `2 * orthoSize * (screenW/screenH)`, 높이는 `2 * orthoSize`(기존 10). 정사각 픽셀에서 (100,100) 드래그는 2400×1080과 1920×1080 모두 각도 ≈ 45도가 되어야 한다.
- HUD는 이미 화면 % 앵커라 기본적으로 맞다. 조이스틱/버튼 % 가 화면 모서리 기준으로 유지되는지 확인.
- `Proto_GreyboxLayout.WorldWidth`(16:9 상수)를 쓰는 기존 16:9 레이아웃 테스트는 깨지거나 약화하지 말 것. 런타임/에임 변환만 실제 aspect를 쓰거나, aspect를 인자로 받는 오버로드를 추가.
- `SceneOpens_WithOrthographicCamera16x9`가 aspect를 16:9로 단언하면, "16:9 화면이면 16:9"를 유지하고 와이드는 실제 aspect를 쓰도록 테스트를 갱신(삭제·약화 금지).

테스트: 2400×1080에서 (100,100)px 드래그 각도가 약 45°(허용 오차 작게), 1920×1080과 각도가 같고, 구식 16:9 WorldWidth 고정 변환과는 각도가 달라야 한다(회귀 방지).

### 6. 빌드 출력 이름만 준비
`Proto_GreyboxBuild.cs`의 출력 경로를 `Builds/v4_greybox/HillNGun_v4_greybox_1120b.apk` 로 바꾼다. 패키지/IL2CPP ARM64/디버그 키스토어/랜드스케이프/제품명 등은 그대로. 이 런에서 빌드는 실행하지 말 것.

## 추가 테스트 (EditMode, `Proto_GreyboxInputTests` 또는 적절한 기존 파일)
- RELOAD∩ROLL = 공집합, RELOAD∩FIRE = 공집합. RELOAD 중심이 FIRE 중심의 왼쪽(+ 위이거나 같거나 위).
- 조이스틱 존 안 Began → 캡처, 존 밖 Began 후 존 안으로 이동 → 캡처 안 함.
- 일시정지 중 FireStickReleased / FireQuickAuto 가 발사로 이어지지 않음.
- 2400×1080 각도 테스트(위 5).
- 결과 다시하기 히트 박스가 있고, 그 밖 탭은 재시작이 아님을 순수 히트 함수로 검증하면 좋음.

끝나면 이 문서에 수정 파일, 새 히트 상수, 테스트 이름, 남은 이슈를 적을 것.

---

## 작업 11 완료 기록 (에이전트 2026-11-20)

### 수정·추가 파일
- `Scripts/Proto_GreyboxHudBuilder.cs` — RELOAD/ROLL 히트·센터·장전! 라벨, 조이스틱 히트, `PauseButtonName`
- `Scripts/Proto_GreyboxPlayerController.cs` — 조이스틱 Began-only, 일시정지/결과 재시작 폴링, `IsPaused`/`SetPaused`, `FilterCombatInputWhenPaused`
- `Scripts/Proto_GreyboxPointerMath.cs` — 실제 aspect 기준 `WorldWidthForScreen`, `ScreenDeltaToWorldDrag`, 레거시 16:9 고정 오버로드
- `Scripts/Proto_GreyboxCameraSetup.cs` — 플레이 중 `Screen` aspect
- `Scripts/Proto_GreyboxResultUi.cs` — 「다시하기」 버튼·히트
- `Scripts/Proto_GreyboxPauseUi.cs` (+ `.meta`) — 일시정지 오버레이·히트
- `Scripts/Proto_GreyboxCombatInputGate.cs` (+ `.meta`) — 순수 히트/일시정지 입력 게이트
- `Editor/Proto_GreyboxBuild.cs` — `HillNGun_v4_greybox_1120b.apk`
- `Tests/EditMode/Proto_GreyboxInputTests.cs` — 신규·갱신 테스트

### 히트 상수 (퍼센트, from-top)
| 영역 | X0–X1 | Y0–Y1 |
|------|-------|-------|
| RELOAD | 64–72 | 58–65 |
| ROLL | 73–83 | 73–84 |
| FIRE | 88–98 | 66–79 (변경 없음) |
| Joystick | 4–19 | 66–91 |
| Pause (HUD) | 92.5–98.5 | 3.2–9.2 |
| Pause Resume | 38–62 | 52–58 |
| Pause Restart | 38–62 | 61–67 |
| Result Restart | 38–62 | 73–79 |

RELOAD 중심 (68, 61.5), FIRE 중심 (93, 72.5).

### 테스트 이름 (추가·갱신)
- `HitRects_Reload_DisjointFromRollAndFire_LeftOfFire`
- `Joystick_OnlyBeganInsideZone_Captures_NotSlipIn`
- `Pause_StripsFireStickAndQuickAuto`
- `Drag_100PxDiagonal_Approx45Degrees_On1920And2400`
- `Drag_100PxDiagonal_WideScreenDiffersFromLegacy16x9LayoutWidth`
- `Drag_SameScreenPercent_MatchesAngleAcrossWideScreens` (기존 동일 월드델타 검증 → 각도 일치로 갱신)
- `Result_RestartHit_OnlyInsideButtonRect`

### 남은 이슈
- EditMode/PlayMode 테스트는 호출측에서 실행 필요 (본 런 미실행).
- 와이드 기기에서 월드 X 레이아웃(`Proto_GreyboxLayout.WorldWidth` 16:9 상수)과 카메라 실제 폭 차이로 스테이지 좌우 여백이 16:9와 다를 수 있음 — HUD %·조준 각도만 보정됨.
- APK 빌드 경로만 `1120b`로 준비; 빌드·커밋은 미수행.
