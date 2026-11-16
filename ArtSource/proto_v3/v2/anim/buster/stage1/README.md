# 버스터 1단계 프레임 애니 (B 파츠 방식) — idle·run·shoot·hit × 아래/위/옆
- 원본: designs/pixel_v3/buster_pixel_sheet_v2_ref.jpg 정면/뒷면/측면에서 새로 잘라 128 배율(0.2244)로 조립
- 프레임: 128x128 RGBA(알파 0/255), NEAREST, 발 기준점 (64,124) = 하단 중앙. 옆은 오른쪽 보기 → 왼쪽은 엔진에서 flip_x
- 동작: idle 4f 6fps loop / run 8f 12fps loop / shoot 3f 12fps once(0~1프레임 노란 총구화염, 총·손 반동) / hit 2f 10fps once(0프레임 흰 플래시, 뒤로 쏠림, 피 없음)
## 구조
- sheets/buster_{dir}_{anim}.png  가로 스트립 (프레임 i = x 128*i)
- frames/{dir}/{anim}/buster_{dir}_{anim}_NN.png
- gif/buster_{dir}_{anim}.gif  (2배, 미리보기용 배경색 있음)
- buster_stage1_overview.gif / .jpg  3방향×4동작 개요
- buster_stage1_meta.json  프레임 수·fps·loop·기준점·shoot 총구 좌표(muzzle_px)·hit 플래시 프레임
- rig/buster_{dir}_rig128.json  기존 18파츠 리그의 뼈 계층·관절 이름·draw order를 128 공간으로 옮긴 것(+ 1단계 레이어 병합 매핑)
- _work/  재현 스크립트(build.py 파츠분리·포즈, render.py 포즈표·출력, overview.py)
## 한계
- 옆 방향 허벅지·정강이는 시트의 측면 다리가 홀스터·앞다리에 가려 깨끗이 잘리지 않아, 시트 청바지 팔레트로 단순 픽셀 도형을 새로 그림(부츠는 시트에서 잘라 씀). 원본보다 단순한 음영.
- 아래/위 run의 무릎 굽힘은 정강이 세로 압축 근사(v1.1: 압축을 줄이고 든 발을 1~2px 더 올림, 이전판은 _prev/)
- 팔은 총과 한 덩어리(gun_arms)라 팔꿈치 굽힘 없음. 반동 시 총 아래 몸통은 옆 픽셀로 메운 근사.
- hit 흰 플래시는 외곽선(아주 어두운 픽셀)만 남기고 85% 흰색.
