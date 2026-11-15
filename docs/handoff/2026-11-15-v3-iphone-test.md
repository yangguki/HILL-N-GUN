# v3 아이폰 테스트 준비: 전선 마무리 + 탄 색 규칙 + 모드 선택 첫 화면 + 프로토 iOS 내보내기 2씬

## Goal
HNG Proto(com.yanginkuk.hillngun.proto) 앱에서 첫 화면에 두 모드(자유 이동 PvE = Proto_TopDown, 전선 PvP = Proto_Frontline)를 골라 아이폰으로 테스트할 수 있게 한다. 픽셀 스킨 기본.

## Context / decisions already made
- 브랜치 feat/v3-pixel-topdown (655259c 기반, 전부 미커밋). Proto_Frontline 은 docs/handoff/2026-11-15-v3-frontline-proto.md 기준으로 21:11 KST 까지 Cursor 가 작업함(Proto/Frontline/Scripts: FrontlineMatch, FrontlineBot, FrontlineRules, Editor/FrontlineEditor, Tests/EditMode/FrontlineTests.cs). 미완 항목이 있으면 그 문서 기준으로 먼저 마무리.
- 전선 확정 규칙: 좌우 분할(내 진영 왼쪽), 분할선 양옆 엄폐물 2열씩, 단판 90초, 전멸 시 승리, 시간 초과 시 생존 인원 → HP 비율 합(현재/최대) → 무승부. 1v1 기본, 3v3 봇 옵션. 탄·수류탄은 선을 넘음.
- v3 색 규칙(확정): 적 탄은 빨강·주황으로 크게, 아군 탄은 노랑으로 작게. 속도·피해·히트박스 수치는 변경 금지(보이는 크기·색만). 적 졸병은 빨간 두건 복장으로 통일(픽셀 시트 색 변형). 발밑 팀 링은 넣지 않음.
- iOS: Assets/HillNGun/Proto/TopDown/Editor/ProtoIOSBuild.cs 가 Proto_TopDown 한 씬만 내보냄, 번들 ID/앱 이름은 내보내는 동안만 바꾸고 복원, 출력 Builds/iOS/ProtoXcode.

## Next steps
1. 전선 프로토 완료 확인(위 문서 Done criteria). 빠진 것만 채움.
2. 탄 색·크기 규칙을 두 프로토(Proto_TopDown, Proto_Frontline)의 픽셀 스킨 뷰에 반영. 뷰 전용 설정으로, 로직 수치 불변. 3D 디오라마 스킨은 기본 꺼짐(전환 버튼은 디버그용으로 남겨도 됨).
3. 새 첫 화면 씬 Assets/HillNGun/Scenes/Proto_Menu.unity: 두 버튼(자유 이동 PvE / 전선 PvP), 전선은 1v1·3v3 선택. 각 모드 종료 화면에 재시작 + 메뉴로 버튼. 가로 고정·safe area(ProtoSafeLayout) 사용.
4. ProtoIOSBuild.Export 씬 목록을 [Proto_Menu, Proto_TopDown, Proto_Frontline] 로(첫 장면 Proto_Menu). 번들 ID 복원·EditorBuildSettings 불변 유지.
5. 관련 EditMode 테스트: FrontlineTests, ProtoTopDown*Tests, 탄 색/크기 매핑이 로직 수치를 바꾸지 않는지 1개, 메뉴→씬 이름 매핑 1개.

## Done criteria and tests to run
- 관련 EditMode 테스트 통과, 신규 경고 없음.
- ProtoIOSBuild.Export 로 Builds/iOS/ProtoXcode 재생성, git diff ProjectSettings 비어 있음.
- Xcode 빌드·기기 설치는 하지 않음(봇이 이어서 함).

## Constraints
- One task only, no unrelated refactoring
- 기존 게임 씬·데이터·전투 코드·LocalIOSBuild.cs·EditorBuildSettings·main bundle ID 수정 금지
- No APK, no commit/push
- If the same error repeats twice, stop and report cause and options

## Report back
변경 파일, 테스트 결과, ProtoXcode 생성 시각, 남은 문제(짧게)
