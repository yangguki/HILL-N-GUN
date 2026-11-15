# Proto_TopDown 아이폰 테스트 빌드 준비 (터치 마무리·시작 장면·재시작·프로토 전용 iOS 내보내기)

## Goal
사용자가 아이폰(무료 Apple ID Personal Team, Xcode 직접 설치)에서 Proto_TopDown만 바로 플레이해 볼 수 있게 준비. 기존 게임 앱(com.hillngun.game)과 설정은 건드리지 않음.

## Context / decisions already made
- 프로토타입: Assets/HillNGun/Proto/TopDown/, Scenes/Proto_TopDown.unity (미커밋, HEAD 655259c). 검증 완료: 관련 EditMode 43/43, 두 스킨 1판 클리어, 전환 시 로직 불변.
- 이미 있음: ProtoControls.BuildUi (ScreenSpaceOverlay) 왼쪽 이동 스틱, 오른쪽 조준 스틱, 사격/폭탄/구르기, 우상단 스킨 F1 버튼. OnGUI HUD(HP/탄/수류탄/웨이브/시계/스킨). 자동 입력은 argv -proto-topdown-capture 일 때만(일반 플레이에선 꺼짐).
- 라운드 종료: ProtoTopDownMatch._ended + _result(패배/클리어)는 있으나 재시작 없음.
- iOS 내보내기: Assets/HillNGun/Scripts/Editor/LocalIOSBuild.cs 의 Export() 는 EditorBuildSettings 활성 씬(첫 장면 Title)을 쓰고, 번들 ID 덮어쓰기 기능 없음. 출력 Builds/iOS/Xcode.
- 적이 문 앞에서 죽으면 같은 문으로 도망쳐 나가는 현상(fleesOnDefeat)은 이번 범위 아님(기획 판단 대기).

## Relevant files
- Assets/HillNGun/Proto/TopDown/** (ProtoControls, ProtoTopDownMatch, ProtoPresentation)
- Assets/HillNGun/Scripts/Editor/LocalIOSBuild.cs (읽기만, 수정 금지)
- 참고만: Assets/HillNGun/Scripts/Runtime/UI/Touch/*, Input/MobileTouchControls (재사용 필요 없으면 쓰지 않음)

## Next steps
1. 터치 확인/마무리(프로토 코드 안에서만): 왼쪽 이동 스틱, 오른쪽 조준+발사(조준 스틱을 밀면 조준, FIRE 버튼으로 발사; 오토에임 없음), BOMB, 구르기, 구석 스킨 전환 버튼. 멀티터치(이동하면서 조준·발사) 동작 확인. 버튼 최소 크기 약 44pt 이상.
2. iPhone 화면 기준: 가로 고정(Landscape Left/Right), 19.5:9(예 2556x1179) 기준으로 아레나 전체가 보이게(16:9/4:3에서도 잘리지 않게 카메라 맞춤). 모든 UI는 Screen.safeArea 안에 배치(노치/다이내믹 아일랜드, 아래 홈 인디케이터 피하기).
3. HUD: 화면 상단 safe area 안에 타이머와 HP만 표시(OnGUI 디버그 표시는 기기 빌드에서 숨김, 에디터에선 디버그 토글로 유지 가능).
4. 종료 화면: 패배/클리어 표시 + 재시작 버튼(씬 다시 로드 또는 매치 리셋). 자동 입력 없음 유지.
5. 프로토 전용 iOS 내보내기: 새 에디터 스크립트 Assets/HillNGun/Proto/TopDown/Editor/ProtoIOSBuild.cs 에 Export() 추가.
   - 씬 목록: Proto_TopDown 하나만(EditorBuildSettings 는 바꾸지 않고 BuildPlayerOptions.scenes 로 지정).
   - 번들 ID: 실행 동안만 com.yanginkuk.hillngun.proto 로 바꾸고 끝나면(성공/실패 모두) 원래 값과 productName 을 원래대로 복원. 앱 이름은 "HNG Proto".
   - 출력: Builds/iOS/ProtoXcode (기존 Builds/iOS/Xcode 덮어쓰기 금지). Builds/iOS/.team 읽기 방식은 LocalIOSBuild 와 동일하게(값 출력·커밋 금지).
   - 끝난 뒤 ProjectSettings 에 변경이 남지 않아야 함(git diff ProjectSettings 비어 있음).
6. 관련 EditMode 테스트만: 기존 ProtoTopDownSkinTests + 재시작 후 상태 초기화 테스트 1개 + safe area 레이아웃 계산 테스트 1개(가능하면).

## Done criteria and tests to run
- 관련 EditMode 테스트 통과, 컴파일 경고 신규 없음.
- 에디터 Simulator(iPhone 15/16 계열)에서 UI가 safe area 안, 아레나 전체 보임, 종료 후 재시작 동작.
- ProtoIOSBuild.Export 로 Builds/iOS/ProtoXcode 생성, ProjectSettings 원상 복구 확인.
- Xcode 빌드·기기 설치는 하지 않음(봇이 이어서 함).

## Constraints
- One task only, no unrelated refactoring
- 기존 씬·데이터·전투 코드·LocalIOSBuild.cs·EditorBuildSettings·main bundle ID 수정 금지
- Run only related tests/compile checks
- No APK, no Xcode device install, no commit/push
- If the same error repeats twice, stop and report cause and options

## Report back
변경 파일, 테스트 결과, ProtoXcode 경로, 남은 문제(짧게)
