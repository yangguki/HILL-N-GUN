# v4 그레이박스 — 작업 1: 씬·카메라·구도 (Task 1 only)

- 브랜치: `proto/v4-greybox-stage` (main 기준). 범위 전체: `docs/handoff/2026-11-20-v4-greybox-scope.md` (2.1 비율표, 2.2 Unity 세팅, 9절 작업 1). 구도 레퍼런스: `docs/handoff/v4_ref_composition.png` (구도만 참고, 에셋으로 쓰지 않음).
- 이번에는 **작업 1만** 한다. 이동·사격·적 AI·웨이브(작업 2~4)는 하지 않는다.

## 하드 룰 (반드시)
1. **새 파일만 추가.** 기존 스크립트·씬·프리팹·asmdef·`ProjectSettings/`·`Packages/` 를 수정하지 않는다. EditorBuildSettings 에 씬을 추가하지 않는다 (ProjectSettings 변경 금지).
2. 기존 asmdef 에 참조를 추가하지 말고 **새 asmdef** 를 만든다.
3. 리팩터링 금지, 새 아트(이미지) 금지 — 도형(런타임 생성 흰색 텍스처 스프라이트 + 색)만.
4. 빌드 금지, git commit/push 금지.
5. Unity 를 직접 실행하지 않아도 된다(테스트는 상위 에이전트가 batchmode 로 돌림). 같은 에러가 두 번 반복되면 멈추고 보고.

## 만들 것
폴더: `Assets/HillNGun/Proto/Greybox/`
- `Scripts/HillNGun.Proto.Greybox.asmdef` (runtime, 참조: `Unity.ugui` 정도만. HillNGun.Runtime 참조 불필요)
- `Scripts/*.cs` — 네임스페이스 `HillNGun.Proto.Greybox`, 클래스 이름은 `Proto_` 접두 (예: `Proto_GreyboxLayout`, `Proto_GreyboxStageBuilder`, `Proto_DepthScale`).
- `Scenes/Proto_GreyboxStage.unity` — 카메라 + 빌더 오브젝트 하나(빌더가 Awake 에서 도형을 생성하는 방식 권장. 씬 YAML 을 손으로 쓸 경우 유효한 Unity 씬 형식이어야 하고 스크립트 GUID 는 해당 .cs.meta 의 guid 와 일치해야 함. .meta 파일도 직접 만들 것: 새 GUID).
- `Tests/EditMode/HillNGun.Proto.Greybox.Tests.EditMode.asmdef` (Editor 전용, `UNITY_INCLUDE_TESTS`, references: HillNGun.Proto.Greybox, UnityEngine.TestRunner, UnityEditor.TestRunner; `overrideReferences` true + `nunit.framework.dll`).
- 필요하면 `Editor/` 에 `HillNGun.Proto.Greybox.Editor.asmdef` + 스크린샷 유틸 `Proto_GreyboxScreenshot.Capture()` (static, `-executeMethod` 로 호출: 씬 열기 → 빌더 Build → 카메라를 1280×720 RenderTexture 로 렌더 → `Builds/v4_greybox_task1.png` 저장 → 종료 코드 0). HUD 가 Screen Space - Overlay 면 RT 에 안 찍히므로 캡처 시엔 Screen Space - Camera 로 두거나 기본을 Screen Space - Camera 로 한다.

## 카메라·월드 매핑 (2.2)
- 2D Orthographic, size 5, 고정. 화면 = 세로 10 유닛 × 가로 16 유닛(16:9). 카메라 위치 (0, 5, -10) → 월드 y 0 = 화면 하단, y 10 = 화면 상단, x -8..8.
- 변환: 화면 비율 p(위에서부터 %) → 월드 y = 10 × (1 − p). 가로 q(왼쪽부터 %) → 월드 x = 16 × q − 8.
- 비율 값은 한 곳(`Proto_GreyboxLayout` 상수/ScriptableObject 없이 static 클래스 OK)에 모아 둔다.
- 깊이 스케일: `Proto_DepthScale.ScaleAt(footY) = Lerp(farScale=1.0, nearScale=2.5, t)`, t = InverseLerp(적 발 y 6.3, 플레이어 발 y 1.2, footY). (이후 작업의 탄·캐릭터에 재사용)

## 배치 (2.1 비율, 위=0%)
| 오브젝트 | 형태·색 | 위치/크기 (화면 %) |
|---|---|---|
| 하늘 | 파란 띠 | y 0–16%, 가로 전체 |
| 건물(상점) 블록 | 갈색 블록 여러 개 | y 16–32% |
| 땅(거리 + 근거리) | 주황 면 | y 32–100% |
| 울타리 | 가로 갈색 막대 | 상단 y 41%, 하단 y 46%, 가로 전체 |
| 적 자리 마커(작업 4 용, 반투명 빨강 캡슐 8개) | 높이 11%, 폭 ~3.5% | 머리 y 26%, 발 y 37%, x 21–79% 균등 |
| 원거리 상자 | 갈색 사각 | x 52–56.5%, y 30–36% |
| 중단 상자(왼쪽) | 갈색 사각 | x 12–21%, y 52–63% |
| 근거리 상자(오른쪽) | 갈색 사각 | x 71–82%, y 62–82% |
| 플레이어 | 보라 캡슐(타원+사각 조합 가능) | 머리 y 61%, 발 y 88%, 가로 중심 49%, 폭 ~8% |
| 조준점 placeholder | 작은 링/십자 | 중심 (50%, 38%), 3% × 5% |
| HUD (uGUI Canvas, 텍스트·사각형만) | | SCORE 좌상단 y 2–8%; "STAGE 1" 상단 중앙 y 2–8%; 일시정지 버튼 자리 x 93–98% y 2–10%; HP 바 x 35–64% y 92–96% ("0/25"); 조이스틱·구르기·발사·재장전·수류탄 자리 원/사각도 2.1 표대로 (반투명) |

정렬: 뒤(y 큰 쪽)가 아래 sortingOrder. 플레이어·상자는 발 y 기준으로 sortingOrder = −(footY×100) 같은 식.
위치는 발(하단) 기준 pivot 으로 놓고, 높이는 표의 최종 화면 높이가 나오게 한다(상자도 `Proto_DepthScale` 를 쓰되 기본 크기를 조정해 최종 높이가 표와 같게).

## 테스트 (EditMode 만, 이 새 asmdef 안에서)
1. `EditorSceneManager.OpenScene("Assets/HillNGun/Proto/Greybox/Scenes/Proto_GreyboxStage.unity")` 가 성공하고 Camera 가 orthographic, size 5, aspect 대상 16:9 임. `LogAssert.NoUnexpectedReceived()`.
2. 빌더 Build() 후(에디트 모드에서 호출 가능하게) 각 오브젝트의 Renderer.bounds 를 1280×720 기준 화면 비율로 변환해서 검증, 허용 오차 ±2%:
   - 플레이어 머리 61%, 발 88%, 높이 27%
   - 울타리 상단 41%, 하단 46%
   - 적 자리 발 37%, 높이 11%
   - 상자 3개 높이 6% / 11% / 20% 및 위치
   - `Proto_DepthScale.ScaleAt(6.3)=1`, `ScaleAt(1.2)=2.5`, 단조 증가
3. 테스트가 끝나면 새 씬으로 돌려놓아 다른 테스트에 영향이 없게.
PlayMode 테스트는 이번엔 만들지 않는다(씬을 Build Settings 에 추가해야 하므로).

## 끝나면
- 추가한 파일 목록, 각 asmdef 이름, 테스트 어셈블리 이름, 스크린샷 메서드 이름을 짧게 출력.
