# 힐앤건 (HILL N GUN) - 서부 카우보이 3v3 슈팅

서부 카우보이 3대3 슈팅 게임입니다.

## 요구 사항

- **Unity 6000.0.85f1 LTS** (Unity 6)
- 표준 Unity 패키지만 사용 (Input System 포함)
- Spine은 아직 설치되지 않음 (라이선스/버전 확정 대기)

## 프로젝트 설정

### 1. Unity Hub에서 프로젝트 열기

1. Unity Hub를 실행합니다
2. `Add` → `Add project from disk` 선택
3. 이 저장소의 루트 폴더 선택
4. Unity 6000.0.85f1로 프로젝트 열기

### 2. 전체 게임 설정

프로젝트를 처음 열면 다음 메뉴를 실행하세요:

**메뉴: `HillNGun` → `Setup All Scenes and Data`**

이 명령은 다음을 수행합니다:
- 모든 ScriptableObject 데이터 에셋 생성 (캐릭터, 적, 스테이지, 보스 등)
- 모든 게임 씬 생성 (Title, ModeSelect, CharacterSelect, PvE_Match, PvP_Match, M1_Sandbox, Result)
- Build Settings 업데이트

### 3. 게임 실행

1. `Assets/HillNGun/Scenes/Title.unity` 씬 열기
2. Play 버튼 클릭

## 게임 모드

### PvE 스토리 모드
- **먼지 마을** 3개 스테이지 + 보스전
- 적 게이지를 채워 스테이지 클리어
- 적 타입: 권총 졸개, 다이너마이트 강도, 말 탄 약탈자
- 보스: 방울뱀 렉스 (3페이즈)

### PvP 대전 모드
- 봇과 3v3 탈락전
- 3판 2선승 (라운드당 80초)
- 봇 AI가 플레이어와 같은 입력 커맨드 사용

### 연습장 (M1 Sandbox)
- 조작 연습 및 테스트
- 과녁과 엄폐물

## 조작법 (에디터 - 마우스/키보드)

| 입력 | 동작 |
|------|------|
| WASD / 방향키 | 이동 (좌우) 및 줄 이동 입력 (상하) |
| 마우스 좌클릭 드래그 | 조준 및 사격 |
| 마우스 우클릭 | 수류탄 조준/투척 |
| Space | 구르기 (이동 방향으로) |
| E | 스킬 사용 |
| 1 / 2 / 3 | 캐릭터 전환 (버스터/레이븐/볼트) |
| R | 과녁, 엄폐물, 더미 아군 리셋 |
| Tab | 설정 패널 열기/닫기 |
| Esc | 일시정지 메뉴 / 설정 패널 닫기 |

### 디버그 단축키

| 키 | 기능 |
|----|------|
| F1 | 판정 원 표시 토글 |
| F2 | 엄폐물 충돌체 표시 토글 |
| F3 | 사거리 표시 토글 |
| F4 | 투사체 표시 토글 |
| F5 | 상태 정보 표시 토글 |
| F6 | 그리드 표시 토글 |
| F7 | 무적 모드 토글 |
| F8 | 슬로모션 토글 (0.25x ↔ 1x) |

## 빌드

### 메뉴에서 빌드

- `HillNGun` → `Build` → `Build All Platforms`: 모든 플랫폼 빌드
- `HillNGun` → `Build` → `Build Windows`: Windows 빌드
- `HillNGun` → `Build` → `Build macOS`: macOS 빌드
- `HillNGun` → `Build` → `Build Android APK`: Android APK 빌드

### 커맨드라인 빌드

```bash
Unity -batchmode -quit -projectPath . -executeMethod HillNGun.Editor.BuildHelper.BuildFromCommandLine -platform windows
```

빌드 결과물은 `Builds/` 폴더에 저장됩니다.

## 캐릭터

### 버스터 (돌격)
- 체력: 3,200
- 무기: 더블배럴 산탄총 (5알 × 150, 사거리 5타일)
- 스킬: 돌파 사격 (퍼짐각 증가, 엄폐물 피해 2배)
- 패시브: 구르기 후 첫 발 +30%

### 레이븐 (저격)
- 체력: 2,200
- 무기: 충전식 사냥총 (900~1,440, 사거리 12타일)
- 스킬: 관통탄 (엄폐물 1개 관통)
- 패시브: 2초 이상 조준 시 약점 피해 +25%

### 볼트 (지원)
- 체력: 2,600
- 무기: 대구경 권총 (520, 사거리 7타일)
- 스킬: 긴급 보수 (엄폐물 40% 수리)
- 패시브: 소생 시간 2초

## 적 타입 (PvE)

### 권총 졸개
- 체력: 600, 공격력: 120, 게이지: +1

### 다이너마이트 강도
- 체력: 900, 폭발 공격력: 450, 게이지: +2

### 말 탄 약탈자
- 체력: 1400, 공격력: 100×3, 게이지: +3

### 보스: 방울뱀 렉스
- 체력: 24,000
- Phase 1: 부채꼴 5발 + 꼬리 휘두르기
- Phase 2: 부채꼴 7발 + 졸개 소환
- Phase 3: 부채꼴 9발 + 광폭화

## 핵심 시스템

### 총알 게이지
- 사격할 때마다 1칸 소모
- **사격 버튼에서 손을 떼야만 충전**
- 게이지가 0이면 발사 불가 + "장전 중" 표시

### 탭 = 발사 안 함
- 사격 버튼을 누른 후 데드존(버튼 반경의 20%) 안에서 떼면 발사하지 않음
- 자동 조준 없음

### 줄 이동
- 위/아래 방향으로 0.15초 유지하면 줄 이동 시작
- 3줄 (뒤 y=2, 가운데 y=4.5, 앞 y=7)
- 구르기로도 줄 이동 가능 (더 빠름)

### 구르기
- 이동 방향으로 2.5타일 구르기
- 무적 시간: 0.35초 (시작부터)
- 구르기 중 사격 불가

## 폴더 구조

```
Assets/HillNGun/
├── Art/                    # 아트 에셋 (플레이스홀더)
├── Audio/                  # 사운드 에셋
├── Data/                   # ScriptableObject 데이터
│   ├── Characters/         # 캐릭터 데이터
│   ├── Weapons/            # 무기 데이터
│   ├── Grenades/           # 수류탄 데이터
│   ├── Rolls/              # 구르기 데이터
│   ├── Skills/             # 스킬 데이터
│   ├── Passives/           # 패시브 데이터
│   ├── Cover/              # 엄폐물 데이터
│   ├── Battlefield/        # 전장 데이터
│   ├── Input/              # 입력 설정
│   ├── Downed/             # 기절/소생 설정
│   ├── Feel/               # 타격감 설정
│   ├── Targets/            # 과녁 데이터
│   ├── Enemies/            # 적 데이터 (PvE)
│   ├── Stages/             # 스테이지 데이터 (PvE)
│   ├── Bosses/             # 보스 데이터 (PvE)
│   └── AI/                 # 봇 AI 설정 (PvP)
├── Localization/           # 로컬라이제이션 (strings_ko.csv)
├── Prefabs/                # 프리팹
├── Scenes/                 # 씬
│   ├── Title.unity         # 타이틀 화면
│   ├── ModeSelect.unity    # 모드 선택
│   ├── CharacterSelect.unity # 캐릭터 선택
│   ├── PvE_Match.unity     # PvE 스토리
│   ├── PvP_Match.unity     # PvP 대전
│   ├── M1_Sandbox.unity    # 연습장
│   └── Result.unity        # 결과 화면
├── Scripts/
│   ├── Runtime/            # 런타임 코드
│   │   ├── Core/           # 핵심 시스템 및 데이터 클래스
│   │   ├── Input/          # 입력 처리
│   │   ├── Battlefield/    # 좌표계 및 전장
│   │   ├── Combat/         # 전투 시스템
│   │   ├── Characters/     # 캐릭터 상태 머신
│   │   ├── Cover/          # 엄폐물 시스템
│   │   ├── Feel/           # 타격감 (히트스톱, 흔들림)
│   │   ├── Logging/        # CSV 로깅
│   │   ├── UI/             # UI 시스템
│   │   ├── DebugTools/     # 디버그 도구
│   │   ├── PvE/            # PvE 모드
│   │   ├── PvP/            # PvP 모드
│   │   └── AI/             # 봇 AI
│   └── Editor/             # 에디터 스크립트
└── Tests/                  # 테스트
    ├── EditMode/           # EditMode 테스트
    └── PlayMode/           # PlayMode 테스트
```

## 설정 변경

모든 게임플레이 수치는 ScriptableObject 에셋에 저장됩니다.
Inspector에서 직접 변경하면 플레이 중에도 즉시 반영됩니다.

상세한 질문 목록: [docs/QUESTIONS_FOR_DESIGN.md](docs/QUESTIONS_FOR_DESIGN.md)

## 로그

게임 로그는 CSV 형식으로 저장됩니다:
- 위치: `Application.persistentDataPath/logs/`
- 형식: `<session_id>.csv`

## 테스트

EditMode 테스트 실행:
1. Window → General → Test Runner
2. EditMode 탭 선택
3. Run All 클릭

## 제한사항

- 오프라인 전용
- Spine 미설치 (플레이스홀더 그래픽 사용)
- 플레이스홀더 사운드 (절차적 생성)

## 추가 시스템

### 플레이스홀더 사운드
모든 사운드 효과는 절차적으로 생성됩니다 (외부 에셋 불필요):
- 사격, 피격, 장전
- 구르기, 수류탄
- 엄폐물 피격/파괴
- 기절, 소생
- 스킬, UI 효과

### 더미 아군 (소생 테스트)
- 맵 우측 아래에 기절 상태로 배치
- 가까이 다가가면 자동으로 소생 시작
- 소생 진행 중 시각적 피드백 (깜박임)
- 10초 내 소생하지 않으면 탈락
- R 키로 리셋

### 설정 패널 (Tab)
- 버튼 크기: 작게/보통/크게 (0.85x, 1.0x, 1.15x)
- 화면 흔들림 토글
- 진동 토글
- 사운드 토글 및 볼륨

## 라이선스

이 프로젝트는 비공개입니다. Spine 런타임 사용 시 별도 라이선스가 필요합니다.

---

힐앤건 © 2026
