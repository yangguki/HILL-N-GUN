# Proto_TopDown: 탑다운 픽셀 vs 3D 디오라마 비교 프로토타입

## Goal
같은 게임 로직 하나로 그림만 픽셀판 / 3D 디오라마판으로 플레이 중 즉시 바꿔 가며 손맛·가독성 비교. 스타일 전환 결정 아님(테스트용).

## Context / decisions already made
- Unity 6000.0.85f1, 브랜치 cursor/m1-control-sandbox-901f, HEAD d5c2f03 + 미커밋 v17 변경(106개). 커밋·푸시 금지, 기존 미커밋 파일 되돌리기 금지.
- 기존 로직은 FreeMove(360° 이동, XY 논리 평면), 고정 틱, GameplayRng, 로직/뷰 분리. 논리 좌표는 그대로 쓰고 뷰만 매핑.
  - 픽셀판: 논리 XY → 화면 XY(45° 느낌은 y 압축/그림자로 표현 가능).
  - 3D판: 논리 (x, y) → 월드 (x, 0, y), 약 45° 내려다보는 고정 카메라.
- 피 연출 없음(12+). 풀숲 숨기는 이번 범위 제외.

## Relevant files (재사용, 수정 금지)
- Assets/HillNGun/Scripts/Runtime/Battlefield/FreeMoveWorld.cs, FreeMoveCollision.cs
- Assets/HillNGun/Scripts/Runtime/Combat/CombatSystem.cs, Projectile.cs, ProjectilePool.cs, GrenadeSystem.cs, GrenadeProjectile.cs
- Assets/HillNGun/Scripts/Runtime/Core/GameplayRng.cs, Core/Data/*.cs (GrenadeData 등)
- Assets/HillNGun/Scripts/Runtime/PvE/* (기본 적 AI)
- 참고: Scripts/Runtime/Debug/FreeMoveSmokeRunner.cs, Editor/FreeMoveSetup.cs, Tests/EditMode/FreeMoveTests.cs
- 기존 씬(Assets/HillNGun/Scenes/*.unity), 배경, 리그, ScriptableObject 데이터는 수정 금지. 필요하면 데이터는 복제본을 Proto 폴더에.

## Next steps
1. 새 폴더 Assets/HillNGun/Proto/TopDown/ (Scripts, Art, Data) 와 새 씬 Assets/HillNGun/Scenes/Proto_TopDown.unity. 빌드 세팅 씬 목록에는 추가하지 않음.
2. 테스트 전용 어댑터(ProtoTopDownBootstrap 등)로 기존 FreeMoveWorld/CombatSystem/GrenadeSystem/적 AI를 구동. 기존 코드 수정이 꼭 필요하면 멈추고 이유 보고.
3. 아레나: 대칭 1개. 벽돌 L벽 2, 모래주머니 3, 상자·통, 가장자리 건물. 엄폐물 = 이동 충돌 + 탄 막음(기존 충돌/엄폐 로직 사용). 카메라 고정, 아레나 전체가 화면에 보이게.
4. 플레이: 버스터 1명(플레이어), 기존 기본 적 3~4명. 적은 위쪽 가장자리 문(울타리/건물 문)에서 걸어 들어옴. 360° 조이스틱 이동+조준, FIRE/BOMB/굴리기, 오토에임 없음, 투사체 회피 가능. 1판 60~120초(웨이브 수로 맞춤, 수치 변경 없이).
5. 수류탄: 포물선 + 바닥 그림자 + 착탄 원 표시(두 스킨 공통 로직 위치 사용).
6. 캐릭터 화면 높이 약 13%. 히트박스는 기존 데이터 값 유지(보이는 크기만 맞춤).
7. 스킨 전환: 화면 구석 버튼 + 디버그 키(F1)로 플레이 중 즉시 픽셀 ↔ 3D. 두 뷰 루트를 같은 논리 상태에서 갱신하고 활성만 토글. 로직·충돌·수치 공유.
8. 임시 아트(정식 아트 제작 금지)
   - 픽셀판: /workspace/hillandgun_handoff/v2/designs/buster_pixel_1112/buster_pixel_sheet_v2.jpg(아래·위·옆 3방향)를 잘라 스프라이트로(파일은 봇이 Mac으로 복사해 둠: ArtSource/proto_topdown/). 좌우는 반전. 애니는 방향 전환 + 간단한 흔들림/반동. 적은 버스터 색상 변경판. 배경은 단순 타일/스프라이트, Point 필터, 압축 없음. 참고 시안: ArtSource/proto_topdown/v3_pixel_small.jpg.
   - 3D판: 기본 큐브/실린더 각진 블록 캐릭터(정육면체 머리 + 판 모자), 평면 셀 셰이딩 + 파스텔 단색 + 부드러운 그림자, 외곽선 없음.
     - 색·스타일 참고: ArtSource/proto_topdown/diorama_ref_user.png (사용자가 고른 기준: 각진 블록, 파스텔, 외곽선 없음)
     - 배치·색 참고: ArtSource/proto_topdown/diorama_arena_mock.jpg (같은 아레나를 디오라마로 그린 시안)
9. 측정: 각 스킨에서 드로우콜·프레임 타임 한 번씩(참고용, 에디터/Mac).

## Done criteria and tests to run
- 에디터에서 두 스킨 모두 1판 끝까지 플레이 가능, 플레이 중 전환 시 로직 끊김 없음(전환 전후 위치/HP/탄 수 동일).
- EditMode 테스트: 기존 FreeMove/Projectile/Grenade 관련 테스트 통과 + 스킨 전환 시 논리 상태 불변 테스트 1개 추가.
- 결과물: 각 스킨 스크린샷 1장 + 짧은 플레이 영상 1개씩 → Builds/proto_topdown/ (봇이 /workspace/smoke_scale/proto_topdown/ 로 옮김).

## Constraints
- One task only, no unrelated refactoring
- Run only related tests/compile checks
- No APK/iOS build, no commit/push
- 기존 씬·배경·리그·데이터·전투 코드 수정 금지
- If the same error repeats twice, stop and report cause and options

## Report back
변경 사항(새 파일 목록), 테스트 결과, 측정값, 남은 문제(짧게)
