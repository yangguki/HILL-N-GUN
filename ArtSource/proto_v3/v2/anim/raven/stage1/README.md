# Raven stage1 — idle 4 / run 8 / shoot 3 / hit 2 × down·up·side
- 기준 시트: designs/pixel_v3/raven_pixel_sheet_v1_1.jpg (파츠 방식 B, NEAREST, 키 112px)
- 규격: 128x128 RGBA, 기준점 (64,124) 발 하단 중앙, side는 오른쪽 향함(왼쪽은 flip_x)
- FPS: idle 6 / run 12 / shoot 12 / hit 10 (idle·run 루프)
- 총구 화염: 아군 규칙 노랑. muzzle_px는 meta의 down|up|side/shoot에 있음(프레임별)
- 망토: 별도 레이어(보라 망토 추출), 몸통과 따로 흔들림(idle ±1, run 뒤로 날림/좌우 교차 ±2~4, hit 반동)
- up(뒷모습)은 망토가 다리 앞, down/side는 다리 뒤
- 폴더: sheets/ frames/ gif/ raven_stage1_meta.json raven_stage1_overview.gif/.jpg, _work/(빌드 스크립트)
