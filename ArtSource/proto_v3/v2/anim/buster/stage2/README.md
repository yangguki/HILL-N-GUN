# 버스터 2단계 roll·throw·down × 아래/위/옆 (1단계 파츠 합성 + 보간)
- 규격: 128x128 RGBA(알파 0/255), NEAREST, 기준점 (64,124). 옆은 오른쪽 보기 → 왼쪽은 flip_x
- roll 6f 12fps once: 웅크림 → 몸 전체 공 회전(50/140/230/320°) → 일어남, 먼지
- throw 5f 12fps once: 다이너마이트 오버핸드(와인드업 2 → 2프레임째 손에서 떠남 → 팔로스루 2). release_px는 meta에
- down 6f 10fps once: 튀어 뒤로 쓰러짐(공중 8px) → 착지 → 튕김 → 누움 + 눈 X(아래/옆) + 별. 모자는 날아가 배 위에, 총은 바닥에. 피 없음
- 구조: sheets/, frames/{dir}/{anim}/, gif/, buster_stage2_meta.json, buster_stage2_overview.gif/.jpg
- _keypose_ref/buster_keyposes_side_gen.jpg: GenerateImage 핵심 자세(동작 참고용만, 프레임에 직접 쓰지 않음 — 얼굴/옷이 기준 시트와 달라 1단계 파츠로 다시 조립)
- _work/: anim2.py(합성기), buster_stage2.py(포즈표), ov.py(개요), clean.py
## 한계
- 아래/위 roll은 화면 평면 회전이라 '카메라 쪽으로 앞구르기'보다는 옆으로 도는 느낌.
- 던지는 팔은 시트에 없는 자세라 시트 색으로 새로 그린 단순 팔(굵기 10/9px). 아래 방향은 원래 오른팔/받침손을 지우고 대체.
- 누운 프레임은 몸통 중심을 x=64에 맞춤(발 기준점이 아님). 128 폭 안에 넣으려고 무릎을 굽힘.
- 모자가 벗겨진 프레임의 머리카락은 단순 타원으로 메움.
