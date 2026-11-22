# v4 그레이박스 — 작업 12: 오른쪽 엄지 클러스터 + 브롤식 정밀 조준

- 브랜치 `proto/v4-greybox-stage`. 작업 10·11 변경은 워킹 트리에 유지. 작업 12 완료 (미커밋).

## 최종 HUD 클러스터 (% 좌상단 기준, Ref 1280×720)

| 컨트롤 | 중심 X | 중심 Y | PlaceCircle 반지름 % |
|--------|--------|--------|----------------------|
| FIRE | 93.0 | 72.5 | 6.8 |
| RELOAD | 87.2 | 62.2 | 4.2 |
| GRENADE | 84.8 | 72.5 | 4.2 |
| ROLL | 93.0 | 57.9 | 4.2 |

- 가장자리 간격: 화면 너비 **2%** (`ActionClusterEdgeGapWidthPercent`). 비주얼 반지름 = `radius% × RefHeight`.
- 히트: 원형 + **9px** 패딩 (`ActionClusterHitPaddingPx`, `Proto_GreyboxHudBuilder.CircleHitTest` / `Proto_GreyboxCombatInputGate.*CircleHit`).
- 장전 방사형 링·「장전!」 펄스는 RELOAD 중심을 따름.

## 조준 스틱 파라미터

| 상수 | 값 |
|------|-----|
| `AimStickRadiusScreenHeightPercent` | 13 (% 높이) |
| `AimStickDeadZonePortion` | 0.15 |
| `AimStickDragThresholdPortion` | 0.15 |
| `AimStickSmoothTime` | 0.06 s |

- FIRE 드래그: 화면 픽셀 델타 → 각도만 → `WorldDirectionFromScreenDrag` + `SmoothAimDirection` (1−exp(−dt/τ)).
- 릴리즈 판정: `AimStickReleaseIntent` (스틱 반지름 대비 비율). 마우스 홀드 조준은 변경 없음.
- DBG: `Proto_GreyboxRuntimeTuning` + 패널 슬라이더 `Aim stick radius %H`, `Aim stick smooth s`.

## EditMode 테스트 (추가·갱신)

- `HitCircles_Cluster_Disjoint_AndPositionsRelativeToFire` (구 `HitRects_Reload_…` 대체)
- `Aim_TapNearZeroDrag_AutoAimNearestLiving` — 스크린 스틱 경로
- `Aim_DragOutThenReturnToCenter_Cancel` — 스크린 스틱 경로
- `AimStick_AngleMatchesDrag_IndependentOfPullDistance`
- `AimStick_SmoothingConvergesTowardTargetAngle`
- 유지: 부호/조이스틱 Began-only/일시정지/와이드 45° (`Drag_100PxDiagonal_Approx45Degrees_On1920And2400` 등)

## 스크린샷

- `Proto_GreyboxScreenshot.CaptureTask12Hud()` → `Builds/v4_greybox/task12_hud.png` (1280×720). 이 런에서 Unity 미실행.

## 남은 이슈

- 실기/APK에서 클러스터 간격·조준 스무딩 체감 확인 필요 (DBG 슬라이더로 12–14% / 0.05–0.08s 튜닝).
- 수류탄은 탭 투척만 (드래그 에임 없음) — 의도대로 유지.
- `PoseAimStickDrag`·CombatTests는 레거시 월드 델타 경로 유지 (에디터 포즈용).

## 하드 룰 (참고)

1. 변경은 `Assets/HillNGun/Proto/Greybox/` 와 `docs/handoff/` 만.
2. 12세 등급, 피 없음. APK/commit/push/Unity 실행 없음.