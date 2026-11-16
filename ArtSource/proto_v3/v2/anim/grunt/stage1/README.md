# 적 졸병(빨간 두건) idle·run·shoot·hit·down × 아래/위/옆
- 원본: designs/pixel_v3/grunt_pixel_sheet_v1.jpg 정면/뒷면/측면, 높이 112px(버스터와 같은 키)
- 규격: 128x128 RGBA(알파 0/255), NEAREST, 기준점 (64,124), 옆=오른쪽 보기(왼쪽 flip_x)
- idle 4f 6fps loop / run 8f 12fps loop / shoot 3f 12fps once(빨강·주황 총구 화염 = 적 탄 규칙) / hit 2f 10fps once(흰 플래시) / down 6f 10fps once(흰 플래시→튀어 넘어짐→X눈+별, 총 떨어뜨림, 피 없음)
- 구조: sheets/, frames/{dir}/{anim}/, gif/, grunt_stage1_meta.json(shoot muzzle_px 포함), grunt_stage1_overview.gif/.jpg
- _work/: build.py(버스터 1단계 리그를 졸병 좌표·올리브 바지 팔레트로 이식), render.py, grunt_down.py
## 한계
- 옆 다리 허벅지·정강이는 버스터처럼 시트 바지색으로 단순 도형을 새로 그림(부츠만 시트).
- 재킷과 바지가 같은 올리브색이라 아래/위 다리 분리선은 y=96 고정.
- 누운 프레임에서 총이 빠진 자리는 옆 픽셀로 메운 근사.
