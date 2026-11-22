# v4 그레이박스 — 작업 10: 실기기 입력 버그 수정

- 브랜치 `proto/v4-greybox-stage` (시작 HEAD `bf2e327`).
- 사용자가 Android 실기기에서 `HillNGun_v4_greybox_1120.apk` 를 돌려 보고 보고한 문제:
  1. 방향조작이 제대로 안 됨 (이동 조이스틱).
  2. 조준이 반대 방향으로 조준됨 (FIRE 드래그 에임).
  3. 기타 문제가 있는지도 확인.

작업은 맥북 체크아웃에서 진행. Windows PC는 오프라인.

## 하드 룰
1. 변경은 `Assets/HillNGun/Proto/Greybox/` 와 `docs/handoff/` 만. 본편 게임 파일·ProjectSettings·Packages·asmdef 금지.
2. APK 빌드·git commit·push 금지. Unity 에디터/배치 테스트는 이 에이전트 런에서 실행하지 말 것(호출측에서 따로 돌림).
3. 같은 에러가 두 번이면 멈추고 보고. 12세 등급, 피 없음.
4. 새 `.meta` guid 는 32자리 16진수. 최소 수정.

## 조사로 확인한 원인

### A. 조준이 반대 방향 — `ScreenDeltaToWorldDrag` 의 Y 부호 반전
파일: `Proto_GreyboxPlayerController.cs`

FIRE 스틱은 `DragWorld = ScreenDeltaToWorldDrag(ev.Pos - _fireStickStartScreen)` 로 **현재 − 시작** 델타를 넘긴다. 이 차감 자체는 맞다.

문제는 변환이다:

```
world = (deltaScreen.x * worldPerPx, -deltaScreen.y * worldPerPy)
```

Unity 터치/마우스 스크린 Y 는 아래가 0, 위로 증가한다. 월드 Y 도 위로 증가한다. 델타의 Y 에 마이너스를 붙이면 **위로 드래그하면 조준이 아래(플레이어 쪽)로 간다.** 적 줄은 화면 위쪽이므로, 실기에서 "반대 방향으로 조준된다"와 정확히 맞다. 좌우는 부호가 맞아, 체감은 상하가 뒤집히거나 대각선이 어긋난 형태로 나타난다.

원인 추정: `PointerPercent` / `ScreenToGroundWorld` 가 쓰는 "화면 위가 0%인 from-top" 좌표계를 델타에도 잘못 적용한 것. 절대 좌표를 월드로 바꿀 때는 `(Screen.height - pos.y)` 가 맞지만, **델타의 부호는 뒤집으면 안 된다.**

수정: Y 마이너스를 제거한다. `drag up → world.y > 0 → AimDirection.y > 0` (적 쪽).

마우스 홀드 조준(`PollMouseAim` / `ScreenToGroundWorld` / `pointerWorld - pressOriginWorld`)은 절대 좌표를 같은 from-top 공간으로 변환하므로 부호가 맞다. 터치 FIRE 스틱만 이 버그의 대상.

### B. 이동 조이스틱이 제대로 안 됨 — 센터 Y 와 손가락 Y 의 좌표계가 다름
파일: `PollTouchJoystick()`

조이스틱 비주얼 중심은 화면 왼쪽 11.5%, **위에서 78%**.

```
cx = RefWidth * 0.115f                         // 왼쪽부터. 맞음
cy = RefHeight * (1f - 0.78f)                  // 아래부터 22% = 158.4 (bottom-up)
sx = pos.x / Screen.width * RefWidth           // 왼쪽부터. 맞음
sy = (Screen.height - pos.y) / Screen.height * RefHeight  // 위에서부터 (from-top)
delta = (sx - cx, sy - cy)
```

`sy` 는 위에서 아래, `cy` 는 아래에서 위라서 공간이 섞여 있다. 링의 보이는 중심을 눌러도 `sy≈561.6`, `cy≈158.4`, `delta.y≈+403` 이 나와 `maxR`(RefWidth×7.5%)를 크게 넘긴다. 히트 박스 전체(y 66–91%)에서 **analog.y 가 거의 항상 +1** 이다. 스틱을 잡는 순간 플레이어가 항상 위(울타리 쪽)로 밀리고, 위/아래 조작이 사실상 불가능하다. "방향조작이 제대로 안 됨"과 맞다.

수정: 손가락과 센터를 같은 Unity bottom-up 공간에서 계산한다.

```
cx = RefWidth * (JoystickCenterXPercent / 100)
cy = RefHeight * (1 - JoystickCenterYPercentFromTop / 100)
sx = pos.x / screenW * RefWidth
sy = pos.y / screenH * RefHeight   // from-top 변환 하지 않음
```

가운데를 누르면 analog≈0, 오른쪽 드래그 → +X, 위 드래그 → +Y. 베이스는 지금처럼 고정 링 중심을 유지한다(터치 다운 원점 방식으로 바꾸지 않음). 11.5 / 78 매직 넘버는 HUD와 공유하는 상수로 올린다.

## 구현 지침
1. 스크린↔월드/아날로그 변환을 **순수 정적 함수**로 추출해 EditMode에서 `Screen.width` 없이 해상도를 인자로 넣게 한다. 예: `Proto_GreyboxPointerMath.cs`. 컨트롤러의 `ScreenDeltaToWorldDrag` / 조이스틱 아날로그 / (필요하면) `PointerPercent` 는 이 헬퍼를 호출만 한다.
2. 위 A, B 를 고친다.
3. EditMode 테스트 추가/갱신 (`Assets/HillNGun/Proto/Greybox/Tests/EditMode/`, 새 파일 권장):
   - 오른쪽 드래그 → 조준/이동 +X
   - 위 드래그 → 조준 +Y (적 방향), 이동 +Y
   - 스틱 중심을 누르면 이동 analog ≈ 0 (더 이상 강제 +Y 가 아님)
   - 아래 드래그 → 이동 −Y
   - 탭(드래그 거의 0) → `AutoAim`, 가장 가까운 생존 적
   - 충분히 당겼다가 중심으로 복귀 → `Cancel`
   - 해상도 독립: 같은 화면 비율 드래그를 2400×1080 과 1920×1080 에 넣었을 때 조준 방향(및 퍼센트 기준 월드 델타)이 같다. 구버전의 Y 반전/좌표 섞임이 이 해상도에서 재현되지 않아야 한다.
   - 기존 `Aim_TapAutoAim_SelectsNearestLiving` 등 Combat 테스트를 약화하지 말 것.
4. 입력 관련으로 **분명하고 작은** 버그만 추가로 고친다. 나머지는 이 문서 "기타 이슈"에 연다/고침으로 나눈다.

## 구현 완료 (2026-11-20)

### 수정 파일
- `Assets/HillNGun/Proto/Greybox/Scripts/Proto_GreyboxPointerMath.cs` (+ `.meta`) — 스크린 델타→월드, 조이스틱 아날로그, `PointerPercentFromScreen`, `ScreenToGroundWorld`
- `Assets/HillNGun/Proto/Greybox/Scripts/Proto_GreyboxPlayerController.cs` — A/B 버그 수정, 헬퍼 위임
- `Assets/HillNGun/Proto/Greybox/Scripts/Proto_GreyboxHudBuilder.cs` — `JoystickCenterXPercent` / `JoystickCenterYPercentFromTop` 상수 공유
- `Assets/HillNGun/Proto/Greybox/Tests/EditMode/Proto_GreyboxInputTests.cs` (+ `.meta`)

### 추가 EditMode 테스트
- `Drag_RightDrag_WorldDeltaPositiveX`
- `Drag_UpDrag_WorldDeltaPositiveY_AimTowardEnemies`
- `Joystick_CenterTouch_AnalogNearZero`
- `Joystick_RightDrag_PositiveX_UpDrag_PositiveY_DownDrag_NegativeY`
- `Aim_TapNearZeroDrag_AutoAimNearestLiving`
- `Aim_DragOutThenReturnToCenter_Cancel`
- `Drag_SameScreenPercent_ResolutionIndependentWorldDelta`

### 기타 이슈 — 이번에 고친 것
- (없음) 입력 관련 소규모 추가 버그는 핸드오프 목록(A/B)만 수정.

## 기타 이슈 (구현 후, 여전히 열림)

고치지 않고 열어 둔 것(명확한 소규모 입력이 아니면 손대지 말 것):
- RELOAD 히트(76–86.5, 62–73)와 ROLL 히트(73–83, 72–84)가 x 76–83, y 72–73 에서 얇게 겹침.
- 조이스틱이 `Began` 뿐 아니라 `Held` 로도 캡처되어, 히트 박스 안으로 미끄러진 손가락이 이동을 훔칠 수 있음.
- 결과 화면은 DBG 를 제외한 아무 탭이나 재시작.
- Pause 버튼은 장식만 있고 핸들러가 없음.
- 변환이 `Screen.width/height` 전체와 16:9 월드 폭을 그대로 쓴다. 20:9 실기기에서 카메라가 레터박스면 각도만 살짝 왜곡될 수 있으나, 부호 버그와는 별개. 이번엔 카메라 `ScreenToWorldPoint` 로 바꾸지 않음.
- 조준선 클램프(`ClipAimEnd` / AimVisual top)와 구르기 중 장전 취소는 이번 보고와 무관하게 기존 의도대로 보임.
- 기존 시각 갭(그런트가 문 안에서 작아 보임, 플레이어가 각짐, 버튼 라벨이 엷음 등)은 범위 밖.

## 완료 시 이 문서에 적을 것
- 수정 파일 목록
- 테스트 파일/케이스 이름
- 고친 기타 이슈 vs 열린 이슈

### 테스트 실행 (맥북, Asia/Seoul 2026-11-20 22:48)
- Unity 6000.0.85f1 batchmode, `-assemblyNames HillNGun.Proto.Greybox.Tests.EditMode`, `-quit` 없이 실행.
- 결과: **24/24 통과** (기존 17 + 신규 입력 7).
- `Joystick_RightDrag_...` 가 한 번 실패했다가, 모터 시작점을 이동 존 바닥(`Vector2.zero` → 클램프 Y=1)이 아니라 중간으로 옮겨 재실행 후 통과. 프로덕션 수학은 이미 맞았고 테스트 픽스처 문제였다.
- Unity가 `ProjectSettings.asset`에 빈 iPhone 아이콘 블록을 넣어 더럽혔고, 매번 `git checkout -- ProjectSettings/ProjectSettings.asset` 으로 되돌렸다. 커밋에 넣지 말 것.
- APK 빌드·커밋·푸시는 하지 않음.
