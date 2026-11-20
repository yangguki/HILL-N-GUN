# v4 그레이박스 — 작업 2: 이동·구르기·엄폐 (+ HUD 컨트롤 모양 수정)

- 브랜치 `proto/v4-greybox-stage`. 작업 1 완료(씬 `Assets/HillNGun/Proto/Greybox/Scenes/Proto_GreyboxStage.unity`, 빌더 `Proto_GreyboxStageBuilder`, 레이아웃 `Proto_GreyboxLayout`, 깊이 스케일 `Proto_DepthScale`, HUD `Proto_GreyboxHudBuilder`, 테스트 asmdef `HillNGun.Proto.Greybox.Tests.EditMode`).
- 범위 문서: `docs/handoff/2026-11-20-v4-greybox-scope.md` 3.1, 3.3(구르기), 3.4, 6절, 9절 작업 2. 화면 = 세로 10 × 가로 약 17.78 유닛 (가로 폭은 `Proto_GreyboxLayout.WorldWidth` 사용, 16 하드코딩 금지).
- 이번에는 **작업 2만**. 조준·발사·재장전·수류탄(작업 3), 적·웨이브·슬라이더 패널(작업 4)은 하지 않는다.

## 하드 룰
1. 변경은 `Assets/HillNGun/Proto/Greybox/` 아래만 (새 파일 + 작업 1에서 만든 파일 수정 OK). 그 밖의 기존 파일·`ProjectSettings/`·`Packages/`·기존 asmdef 수정 금지. Build Settings 추가 금지.
2. 새 `.meta` 의 guid 는 반드시 **32자리 16진수** (예: PowerShell `[guid]::NewGuid().ToString('N')`). 33자리 금지.
3. 리팩터링 금지, 새 아트 금지(도형만), 빌드·git commit/push 금지, Unity 실행 금지. 같은 에러 두 번이면 멈추고 보고.
4. 물리 엔진(Physics2D 시뮬레이션) 대신 **순수 C# 로직 클래스**로 판정한다(EditMode 테스트에서 dt 를 넣어 결정적으로 돌릴 수 있게). Physics2D 전역 설정을 바꾸지 않는다.
5. Unity API 에서 읽기 전용 프로퍼티(Camera.pixelWidth 등)에 대입하지 말 것.

## 0. HUD 컨트롤 모양 수정 (작은 수정)
작업 1 캡처에서 조이스틱 자리·버튼 자리가 갈색 사각형이라 상자처럼 보인다. uGUI 컨트롤을 **반투명 원**으로 바꾼다 (런타임 생성 원형 스프라이트, 새 이미지 파일 금지):
- 조이스틱: 반투명 흰 링(바깥 원, 중심 (11.5%, 78%), x 4–19%) + 작은 흰 노브(입력 방향으로 움직임).
- 구르기: 어두운 회색 반투명 원 (x 77–87%, y 65–80%).
- 발사: 노란 반투명 원 (x 88–98%, y 66–79%).
- 재장전·수류탄: 회색 반투명 작은 원 (각 x 77–86% / 88–98%, y 84–95%).
- 월드 오브젝트(상자 등)와 같은 갈색 계열 쓰지 않기. 라벨 텍스트(ROLL/FIRE 등) 작게 OK.

## 1. 이동 (3.1)
- `Proto_GreyboxMoveZone`(순수 로직): 발 기준 이동 가능 영역 = 화면 y **75–90%** (월드 y 1.0–2.5), x **15–85%**. 울타리(상단 41%/하단 46%, 월드 y ~5.4–5.9)와 그 위 적 쪽은 절대 진입 불가 — 어떤 입력·구르기로도 발 y 가 존 상한(2.5)을 넘지 않는다. `Clamp(Vector2 foot)`.
- `Proto_GreyboxPlayerMotor`(순수 로직, 상태: 위치, 속도): 360° 아날로그 입력(크기 0–1) × 이동 속도. 대각선 정규화. Tick(input, dt) 후 Clamp.
- 플레이어 오브젝트 스케일은 발 y 에 따라 `Proto_DepthScale` 로 갱신(근거리 존 안이라 변화는 작음), sortingOrder 도 발 y 기준 갱신.

## 2. 구르기 (3.3)
- 구르기 버튼/Space: 현재 이동 입력 방향(없으면 마지막 좌우 방향, 기본 오른쪽)으로 **거리 2.2 유닛을 0.28초** 동안 이동. 존 Clamp 적용(울타리 못 넘음).
- **무적 0.22초**(구르기 시작부터), 쿨다운 **1.2초(구르기 종료 후부터)**. 구르기 중 일반 이동 입력 무시.
- 구르기 중 시각: 캡슐을 납작하게(scaleY 0.6) + 살짝 밝게. 아트 금지.
- 로직: `Proto_GreyboxRoll` 순수 클래스 (`TryStart(dir)`, `Tick(dt)`, `IsRolling`, `IsInvulnerable`, `CooldownRemaining`).

## 3. 엄폐 (3.4)
- `Proto_GreyboxCover`(순수 로직): 상자 사각형(월드 rect) 목록 + `bool SegmentBlocked(Vector2 from, Vector2 to, out Vector2 hitPoint)` (선분-사각형 교차). 적 쪽(위)에서 플레이어 쪽(아래)으로 오는 탄이 상자 rect 를 지나면 상자에 먼저 막힌다. 옆으로 새는 탄은 맞는다.
- 테스트용 간단 탄: `Proto_GreyboxTestBullet` 또는 로직 `Proto_GreyboxBulletSim`(위치, 속도, 반지름) — 매 Tick 이동 선분이 상자에 막히면 소멸, 플레이어 히트박스(발~머리 rect)에 닿으면 플레이어 피해(무적 중이면 무시). 엄폐 피해 감소 100%. (실제 적 발사는 작업 4 — 이번엔 로직 + 테스트용만. 원하면 에디터 디버그용으로 `T` 키를 누르면 화면 위 중앙에서 플레이어를 향해 빨간 큰 탄 하나 발사, 속도 3.5, 지름 0.55.)
- 플레이어 HP 25 (HUD "HP/25" 표시 갱신 OK).

## 4. 입력 (에디터 테스트용)
- activeInputHandler = Both 이므로 **레거시 `UnityEngine.Input`** 사용(새 asmdef 참조 추가 불필요). 키보드 WASD/방향키 이동, Space 구르기. 마우스/터치: 조이스틱 원 영역 안을 누르고 드래그하면 노브가 따라가고 아날로그 입력(반지름 기준 0–1). 구르기 원 클릭/터치 = 구르기.
- 입력은 `IProto_GreyboxInput` 같은 작은 인터페이스/구조체로 모터에 전달해서 테스트가 입력을 직접 넣을 수 있게.
- MonoBehaviour `Proto_GreyboxPlayerController` 가 입력 → 모터/구르기 → Transform 반영. 빌더가 플레이어에 붙인다.

## 5. 튜닝 값 (6절 초기값, `[SerializeField]` + 로직 생성자 인자로 전달)
이동 속도 5.0 유닛/초, 구르기 거리 2.2, 구르기 시간 0.28초, 무적 0.22초, 쿨다운 1.2초, 플레이어 HP 25, 적탄 속도 3.5, 적탄 지름 0.55, 엄폐 피해 감소 100%. 기본값 상수는 한 곳(`Proto_GreyboxTuning` static 또는 [Serializable] 클래스)에.

## 6. 테스트 (EditMode, 기존 테스트 asmdef 안에 새 파일 `Proto_GreyboxMoveTests.cs`) — 아래 4가지만
1. 이동 클램프: 큰 입력으로 여러 Tick 후 발 위치가 x 15–85%, y 75–90% 영역 안.
2. 울타리 통과 금지: 위쪽 입력 + 구르기 위쪽으로 계속 → 발 y 가 존 상한 이하, 울타리 하단 월드 y 보다 항상 아래.
3. 구르기 무적: 시작 후 0.21초엔 무적, 0.23초 이후엔 아님; 구르기 끝 + 1.2초 전엔 재시작 불가, 이후 가능; 이동 거리 ≈ 2.2(±0.05, 클램프 안 걸리는 위치에서).
4. 상자가 정면 탄 차단: 근거리 상자(작업 1 위치) 바로 뒤(아래)에 플레이어, 적 쪽 위에서 플레이어를 향한 탄 → 상자에 막혀 HP 25 유지. 대조: 상자 옆으로 빗겨 있으면 맞아서 HP 감소.
기존 작업 1 테스트 2개도 계속 통과해야 함.

## 7. 스크린샷 유틸 확장
`Proto_GreyboxScreenshot` 에 메서드 추가 (`-executeMethod` 용, 1280×720, `Builds/` 저장, 끝나면 종료 코드 0):
- `Capture()` 유지 → `Builds/v4_greybox_task2.png` 로 저장(파일명 인자 없으면 task2).
- `CaptureMoveSequence()` → 씬 열고 빌드한 뒤 플레이어 로직을 직접 구동(입력 주입, dt 스텝)해서 4장: `Builds/v4_task2_seq_0_start.png`(시작 위치), `_1_left.png`(왼쪽으로 이동), `_2_right_forward.png`(오른쪽·근거리 상자 뒤 쪽으로 이동), `_3_midroll.png`(구르기 중간, 납작한 캡슐).

## 끝나면
추가/수정 파일 목록, 테스트 이름, 스크린샷 메서드 이름을 짧게 출력.
