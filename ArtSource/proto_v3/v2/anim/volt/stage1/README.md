# Volt stage1 — idle 4 / run 8 / shoot 3 / hit 2 × down·up·side
- 기준 시트: designs/pixel_v3/volt_pixel_sheet_v1_1.jpg (파츠 방식 B, NEAREST, 키 112px)
- 규격: 128x128 RGBA, 기준점 (64,124) 발 하단 중앙, side는 오른쪽 향함(왼쪽은 flip_x)
- FPS: idle 6 / run 12 / shoot 12 / hit 10 (idle·run 루프)
- 총구 화염: 아군 규칙 노랑. muzzle_px는 meta의 down|up|side/shoot에 있음(프레임별, 화염 없는 프레임은 null)
- 렌치: 몸통 레이어에 붙어 있어 모든 프레임에서 오른쪽 어깨(아래=화면 왼쪽, 위=화면 오른쪽, 옆=등 뒤)
- up(뒷모습): 시트 뒷모습엔 총이 안 보여서, 앞모습 총 윗부분을 화면 왼쪽 어깨 뒤로 살짝 보이게 붙임(화염 위치 확보용)
- 폴더: sheets/ frames/ gif/ volt_stage1_meta.json volt_stage1_overview.gif/.jpg, _work/(빌드 스크립트)
