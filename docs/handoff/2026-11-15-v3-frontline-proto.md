# v3 전선 모드 프로토 (중앙 분할선 이동 제한 + 봇 대전)

## Goal
v3 컨셉(메탈슬러그풍 픽셀 + 자유 이동·조준 + 엄폐 사격)의 PvP '전선' 모드를 프로토로 1개 만든다. 맵을 가운데에서 둘로 나누고 각 진영은 자기 쪽 안에서만 이동, 상대는 봇. 온라인 네트워크는 이번 범위 아님(로컬 봇 대전).

## Context / decisions already made
- 브랜치: feat/v3-pixel-topdown (cursor/m1-control-sandbox-901f 655259c 에서 분기, Proto_TopDown 미커밋 작업 포함).
- 그래픽 기준: Proto_TopDown 의 픽셀 스킨. 3D 디오라마·2D 치비는 보류(코드 삭제하지 말고 이번엔 쓰지 않음).
- 전투: 기존 자유 이동(FreeMoveWorld), 투사체, 엄폐(피격 감소·탄 막음), 구르기, 수류탄(엄폐 파괴) 그대로 재사용. 수치·데이터 변경 없음.
- PvP 2모드 중 이번은 ② 전선만. ① 난전(맵 전체 3v3)은 다음 작업.
- Proto_TopDown 은 적 도망(fleesOnDefeat) 등 PvE 동작 그대로 둠.

## Relevant files
- Assets/HillNGun/Proto/TopDown/** (ProtoTopDownMatch, ProtoControls, ProtoPresentation, ProtoSafeLayout, Editor/ProtoIOSBuild)
- Assets/HillNGun/Scripts/Runtime/Battlefield/FreeMoveWorld.cs, FreeMoveCollision.cs
- Assets/HillNGun/Scripts/Runtime/Combat/*, Cover/*, AI/*, PvE/AllyFreeSteer.cs
- 참고: Assets/HillNGun/Scripts/Runtime/PvP/PvPMatchManager.cs (기존 RowLane PvP 승패·라운드 규칙), Scenes/PvP_Match.unity (수정 금지)

## Next steps
1. 새 씬 Assets/HillNGun/Scenes/Proto_Frontline.unity + 코드 Assets/HillNGun/Proto/Frontline/ (Proto/TopDown 의 부트스트랩·뷰를 재사용, 복사보다 참조 우선).
2. 아레나: 좌우 분할(가로 화면). 플레이어 진영 왼쪽, 적 진영 오른쪽. 중앙 분할선 표시(픽셀 스킨). 분할선 양옆으로 엄폐물(L벽·모래주머니·상자)을 각 2열씩 대칭 배치. 카메라 고정·아레나 전체 보이게, 아이폰 가로·safe area 기준 유지.
3. 이동 제한: 각 팀 유닛은 자기 진영 영역 밖으로 못 나감(분할선에 보이지 않는 벽, 구르기·넉백 포함 클램프). 탄·수류탄은 선을 넘어감. 테스트 전용 어댑터/설정으로 처리하고 기존 FreeMove 코드 수정이 꼭 필요하면 멈추고 보고.
4. 팀: 1v1 기본(플레이어 버스터 vs 봇), 설정으로 3v3(아군 봇 2 + 적 봇 3) 전환. 봇은 기존 AI(자유 이동 조준·엄폐·수류탄)를 자기 진영 제한 안에서 사용.
5. 승패(기획 확정): 단판 라운드 90초. 상대 팀 전멸 시 즉시 승리. 시간 초과 시 ① 생존 인원이 많은 팀 승리 ② 같으면 팀별 남은 HP 비율 합(각 유닛 현재HP/최대HP의 합)이 높은 팀 승리 ③ 이것도 같으면 무승부. 절대 HP 합은 쓰지 않음. 종료 후 재시작 버튼.
6. 조작: Proto_TopDown 과 동일(왼쪽 이동 스틱, 오른쪽 조준, 사격/폭탄/구르기, 키보드 WASD/마우스/J/K/Space). HUD 상단 타이머·양 팀 HP만.

## Done criteria and tests to run
- 에디터에서 1v1·3v3 각각 1판 끝까지 진행(봇 vs 봇 자동 모드 옵션 있으면 좋음).
- EditMode 테스트: 진영 클램프(이동·구르기 모두 선을 못 넘음), 탄은 선을 통과, 승패 판정(전멸/시간초과 인원수/HP 비율 합/무승부, 절대 HP 합이 아닌 비율로 판정되는지) 각 1개 이상 + 기존 Proto/FreeMove/Projectile/Grenade 관련 테스트 통과.
- 기존 씬·데이터·전투 코드·ProjectSettings·EditorBuildSettings 변경 없음.

## Constraints
- One task only, no unrelated refactoring
- Run only related tests/compile checks
- No APK/iOS build, no commit/push
- If the same error repeats twice, stop and report cause and options

## Report back
변경 파일, 테스트 결과, 1v1/3v3 진행 결과, 남은 문제(짧게)
