using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>Persp2D combat VFX (fx_v2); visual only.</summary>
    sealed class Proto_GreyboxPersp2DCombatFx
    {
        struct SparkFx
        {
            public SpriteRenderer Sr;
            public float T;
            public int Frame;
            public Sprite[] Seq;
            public float SeqFps;
        }

        readonly Proto_GreyboxPlayerController _host;
        readonly System.Func<Transform> _layerRoot;
        Proto_GreyboxFxV2Catalog _fxV2;
        readonly List<SpriteRenderer> _sparkPool = new List<SpriteRenderer>();
        readonly List<SparkFx> _sparks = new List<SparkFx>();
        SpriteRenderer _rollTrailSr;
        float _rollTrailT;
        readonly List<SpriteRenderer> _muzzlePool = new List<SpriteRenderer>();

        float MinTracerWorldLength => Proto_GreyboxLayout.WorldWidth * 0.04f;
        float MaxTracerWorldLength => Proto_GreyboxLayout.WorldWidth * 0.07f;

        Proto_GreyboxPersp2DCatalog PerspCat => Proto_GreyboxPersp2DCatalog.LoadRuntime();

        static float TracerDepth01(float footPct) => Mathf.InverseLerp(62f, 47f, footPct);

        float TracerLengthWorld(float footPct) =>
            Mathf.Lerp(MinTracerWorldLength * 0.55f, MaxTracerWorldLength, TracerDepth01(footPct));

        float TracerThicknessWorld(float footPct) =>
            Proto_GreyboxLayout.WorldHeight * Mathf.Lerp(0.004f, 0.008f, TracerDepth01(footPct));

        public Proto_GreyboxPersp2DCombatFx(Proto_GreyboxPlayerController host, System.Func<Transform> layerRoot)
        {
            _host = host;
            _layerRoot = layerRoot;
        }

        public void Clear()
        {
            foreach (var s in _sparks) s.Sr?.gameObject.SetActive(false);
            _sparks.Clear();
            if (_rollTrailSr != null) _rollTrailSr.gameObject.SetActive(false);
        }

        public void Tick(float dt)
        {
            TickSparks(dt);
            TickRollTrail(dt);
        }

        public void OnRollStarted(Proto_GreyboxRollFacing facing)
        {
            _fxV2 ??= Proto_GreyboxFxV2Catalog.LoadRuntime();
            var foot = _host.Motor.FootPosition;
            SpawnRollDust(foot, true);
            var trail = _fxV2?.rollLeftAfterimage;
            if (trail == null) return;
            EnsureRollTrail();
            _rollTrailSr.sprite = trail;
            _rollTrailSr.gameObject.SetActive(true);
            _rollTrailT = Proto_GreyboxTuning.RollDuration + 0.05f;
            float fxScale = Proto_GreyboxPersp2DStageLayout.CharacterUniformScaleAtFootWorld(foot.y);
            _rollTrailSr.transform.position = new Vector3(foot.x, foot.y, 0f);
            _rollTrailSr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(trail, 0.35f * fxScale);
            _rollTrailSr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(foot.y);
        }

        public void OnRollEnded()
        {
            SpawnRollDust(_host.Motor.FootPosition, false);
            if (_rollTrailSr != null) _rollTrailSr.gameObject.SetActive(false);
        }

        public void SpawnBulletDust(Vector2 groundPos) => SpawnDustPuff(groundPos, false);

        public void OnGruntMuzzle(Vector2 foot, Vector2 playerFoot)
        {
            _fxV2 ??= Proto_GreyboxFxV2Catalog.LoadRuntime();
            var muzzle = PerspCat?.fxMuzzleFlash ?? _fxV2?.muzzleFlash;
            if (muzzle == null || muzzle.Length == 0) return;
            var sr = RentMuzzle();
            sr.sprite = muzzle[0];
            sr.color = Color.white;
            var dir = (playerFoot - foot).normalized;
            if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;
            float visScale = Proto_GreyboxPersp2DStageLayout.CharacterUniformScaleAtFootWorld(foot.y);
            float bodyH = Proto_GreyboxPerspectiveDepth.PlayerBodyHeightWorld(foot.y) * 0.85f;
            var pos = foot + dir * (0.38f * visScale) + new Vector2(0f, bodyH * 0.45f);
            sr.transform.position = new Vector3(pos.x, pos.y, 0f);
            float diam = Proto_GreyboxLayout.ScreenPercentToWorldHeight(Proto_GreyboxTuning.PlayerMuzzleFlashHeightPercent) *
                         visScale * 1.15f;
            sr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(muzzle[0], diam);
            sr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(foot.y) + 2;
            sr.gameObject.SetActive(true);
            _sparks.Add(new SparkFx { Sr = sr, T = Proto_GreyboxTuning.PlayerMuzzleFlashDuration, Frame = 0, Seq = muzzle, SeqFps = 24f });
        }

        public void SpawnHitSpark(Vector2 groundPos)
        {
            _fxV2 ??= Proto_GreyboxFxV2Catalog.LoadRuntime();
            var frames = _fxV2?.hitSparks ?? _fxV2?.dustGround;
            if (frames == null || frames.Length == 0)
            {
                SpawnDustPuff(groundPos, false);
                return;
            }

            var sr = RentSpark();
            sr.sprite = frames[0];
            sr.color = Color.white;
            float fxScale = Proto_GreyboxPersp2DStageLayout.CharacterUniformScaleAtFootWorld(groundPos.y);
            sr.transform.position = new Vector3(groundPos.x, groundPos.y, 0f);
            sr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(frames[0], 0.24f * fxScale);
            sr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(groundPos.y) + 2;
            sr.gameObject.SetActive(true);
            _sparks.Add(new SparkFx { Sr = sr, T = frames.Length / 18f, Frame = 0, Seq = frames, SeqFps = 18f });
        }

        public bool SyncAllyBullets(IReadOnlyList<Proto_GreyboxAllyBulletSim> bullets, float diameter,
            List<SpriteRenderer> cores, List<SpriteRenderer> tracers, List<SpriteRenderer> hotspots, Transform parent)
        {
            _fxV2 ??= Proto_GreyboxFxV2Catalog.LoadRuntime();
            var allyTracer = PerspCat?.fxTracerPlayerTrail ?? _fxV2?.bulletAllyTracer;
            var allyCore = PerspCat?.fxTracerPlayerHead ?? _fxV2?.bulletAlly;
            if (allyTracer == null && allyCore == null) return false;
            EnsureAllyBulletViews(bullets.Count, parent, cores, tracers, hotspots);
            for (int i = 0; i < cores.Count; i++)
            {
                var sr = cores[i];
                var head = i < tracers.Count ? tracers[i] : null;
                var hot = i < hotspots.Count ? hotspots[i] : null;
                if (i < bullets.Count && bullets[i].Alive)
                {
                    var b = bullets[i];
                    var vel = b.GroundVelocity;
                    var dir = vel.sqrMagnitude > 1e-6f ? vel.normalized : Vector2.right;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    var pos = b.VisualPositionAtGround();
                    float scale = b.VisualScale;
                    float diam = Mathf.Max(Proto_GreyboxTuning.AllyBulletVisualDiameter * scale,
                        Proto_GreyboxLayout.MinAllyBulletWorldDiameter);
                    int order = Proto_GreyboxLayout.CombatDrawSortingOrder(b.GroundPosition.y);
                    sr.gameObject.SetActive(true);
                    var tracerSprite = allyTracer ?? allyCore;
                    sr.sprite = tracerSprite;
                    sr.color = new Color(1f, 0.95f, 0.35f, 0.96f);
                    float footPct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(pos.y);
                    float trailLen = TracerLengthWorld(footPct);
                    float thick = TracerThicknessWorld(footPct);
                    var center = pos - dir * trailLen * 0.45f;
                    sr.transform.position = new Vector3(center.x, center.y, 0f);
                    sr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldSize(tracerSprite, trailLen, thick);
                    sr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    sr.sortingOrder = order;
                    if (hot != null) hot.gameObject.SetActive(false);
                    if (head != null && allyCore != null)
                    {
                        head.gameObject.SetActive(true);
                        head.sprite = allyCore;
                        head.color = Color.white;
                        var headPos = pos + dir * (diam * 0.22f);
                        head.transform.position = new Vector3(headPos.x, headPos.y, 0f);
                        head.transform.localScale =
                            Proto_GreyboxSpriteUtil.ScaleForWorldSize(allyCore, diam * 0.4f, diam * 0.34f);
                        head.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                        head.sortingOrder = order + 1;
                    }
                    else if (head != null)
                        head.gameObject.SetActive(false);
                }
                else
                {
                    sr.gameObject.SetActive(false);
                    if (head != null) head.gameObject.SetActive(false);
                    if (hot != null) hot.gameObject.SetActive(false);
                }
            }

            return true;
        }

        public bool SyncEnemyBullets(IReadOnlyList<Proto_GreyboxBulletSim> bullets, float diameter,
            List<SpriteRenderer> cores, List<SpriteRenderer> tracers, Transform parent, float simTime)
        {
            _fxV2 ??= Proto_GreyboxFxV2Catalog.LoadRuntime();
            var frames = _fxV2?.PreferEnemyBulletFrames();
            var enemyHead = PerspCat?.fxTracerEnemyHead
                ?? (frames != null && frames.Length > 0
                    ? frames[Mathf.FloorToInt(simTime * 12f) % frames.Length]
                    : _fxV2?.bulletEnemy);
            var enemyTracer = PerspCat?.fxTracerEnemyTrail ?? enemyHead;
            if (enemyHead == null && enemyTracer == null) return false;
            EnsureEnemyBulletViews(bullets.Count, parent, cores, tracers);
            for (int i = 0; i < cores.Count; i++)
            {
                var head = cores[i];
                var trail = i < tracers.Count ? tracers[i] : null;
                if (i < bullets.Count && bullets[i].Alive)
                {
                    var b = bullets[i];
                    var vel = b.GroundVelocity;
                    var dir = vel.sqrMagnitude > 1e-6f ? vel.normalized : Vector2.left;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    head.gameObject.SetActive(true);
                    head.sprite = enemyHead ?? enemyTracer;
                    head.color = new Color(1f, 0.95f, 0.4f, 1f);
                    var pos = b.GroundPosition;
                    head.transform.position = new Vector3(pos.x, pos.y, 0f);
                    float diam = Mathf.Max(b.VisualDiameter(diameter) * Proto_GreyboxTuning.EnemyBulletVisualDiameterMul,
                        Proto_GreyboxLayout.MinAllyBulletWorldDiameter * 0.85f);
                    head.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldSize(head.sprite, diam, diam * 0.75f);
                    head.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    head.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(b.GroundPosition.y);
                    if (trail != null)
                    {
                        trail.gameObject.SetActive(true);
                        trail.sprite = enemyTracer ?? head.sprite;
                        trail.color = new Color(1f, 0.95f, 0.35f, 0.94f);
                        float footPct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(pos.y);
                        float tailLen = ClampTracerWorldLength(footPct, Vector2.Distance(pos, _host.Motor.FootPosition));
                        float thick = TracerThicknessWorld(footPct) * 1.1f;
                        var headPos = (Vector2)pos;
                        var tailPos = headPos - dir * tailLen;
                        var center = (headPos + tailPos) * 0.5f;
                        trail.transform.position = new Vector3(center.x, center.y, 0f);
                        trail.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldSize(trail.sprite, tailLen,
                            thick);
                        trail.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                        head.transform.position = new Vector3(headPos.x, headPos.y, 0f);
                        trail.sortingOrder = head.sortingOrder - 1;
                    }
                }
                else
                {
                    head.gameObject.SetActive(false);
                    if (trail != null) trail.gameObject.SetActive(false);
                }
            }

            return true;
        }

        static void EnsureAllyBulletViews(int bulletCount, Transform parent, List<SpriteRenderer> cores,
            List<SpriteRenderer> tracers, List<SpriteRenderer> heads)
        {
            while (cores.Count < bulletCount)
            {
                var tail = new GameObject("P2D_AllyTracer");
                tail.transform.SetParent(parent, false);
                tracers.Add(tail.AddComponent<SpriteRenderer>());

                var go = new GameObject("P2D_AllyBulletHead");
                go.transform.SetParent(parent, false);
                cores.Add(go.AddComponent<SpriteRenderer>());

                var hot = new GameObject("P2D_AllyBulletHot");
                hot.transform.SetParent(parent, false);
                heads.Add(hot.AddComponent<SpriteRenderer>());
            }
        }

        static void EnsureEnemyBulletViews(int bulletCount, Transform parent, List<SpriteRenderer> cores,
            List<SpriteRenderer> tracers)
        {
            while (cores.Count < bulletCount)
            {
                var trail = new GameObject("P2D_EnemyTracer");
                trail.transform.SetParent(parent, false);
                tracers.Add(trail.AddComponent<SpriteRenderer>());

                var go = new GameObject("P2D_EnemyBullet");
                go.transform.SetParent(parent, false);
                cores.Add(go.AddComponent<SpriteRenderer>());
            }
        }

        void SpawnRollDust(Vector2 groundPos, bool takeoff)
        {
            _fxV2 ??= Proto_GreyboxFxV2Catalog.LoadRuntime();
            float fxScale = Proto_GreyboxPersp2DStageLayout.CharacterUniformScaleAtFootWorld(groundPos.y);
            var frames = takeoff ? _fxV2?.rollDustTakeoff : _fxV2?.rollDustLand;
            if (frames != null && frames.Length > 0)
            {
                var sr = RentSpark();
                sr.sprite = frames[0];
                sr.color = Color.white;
                sr.transform.position = new Vector3(groundPos.x, groundPos.y, 0f);
                sr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(frames[0], 0.22f * fxScale);
                sr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(groundPos.y) + 1;
                sr.gameObject.SetActive(true);
                _sparks.Add(new SparkFx { Sr = sr, T = frames.Length / 16f, Frame = 0, Seq = frames, SeqFps = 16f });
                return;
            }

            SpawnDustPuff(groundPos, takeoff);
        }

        void SpawnDustPuff(Vector2 groundPos, bool start)
        {
            float fxScale = Proto_GreyboxPersp2DStageLayout.CharacterUniformScaleAtFootWorld(groundPos.y);
            var sr = RentSpark();
            var groundFx = PerspCat?.fxGroundImpact ?? _fxV2?.dustGround;
            if (groundFx != null && groundFx.Length > 0)
            {
                sr.sprite = groundFx[0];
                sr.color = Color.white;
                sr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(groundFx[0], 0.26f * fxScale);
                _sparks.Add(new SparkFx { Sr = sr, T = groundFx.Length / 16f, Frame = 0, Seq = groundFx, SeqFps = 16f });
            }
            else
            {
                sr.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                sr.color = new Color(0.72f, 0.62f, 0.48f, start ? 0.55f : 0.42f);
                sr.transform.localScale = Vector3.one * ((start ? 0.22f : 0.18f) * fxScale);
                _sparks.Add(new SparkFx { Sr = sr, T = 0.35f, Frame = 0 });
            }

            sr.transform.position = new Vector3(groundPos.x, groundPos.y, 0f);
            sr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(groundPos.y) + 1;
            sr.gameObject.SetActive(true);
        }

        void EnsureRollTrail()
        {
            if (_rollTrailSr != null) return;
            var root = _layerRoot?.Invoke();
            var go = new GameObject("P2D_RollTrail");
            go.transform.SetParent(root != null ? root : _host.transform, false);
            _rollTrailSr = go.AddComponent<SpriteRenderer>();
        }

        void TickRollTrail(float dt)
        {
            if (_rollTrailSr == null || !_rollTrailSr.gameObject.activeSelf) return;
            _rollTrailT -= dt;
            if (_host.Roll.IsRolling)
            {
                var foot = _host.Motor.FootPosition;
                _rollTrailSr.transform.position = new Vector3(foot.x, foot.y, 0f);
            }

            if (_rollTrailT <= 0f) _rollTrailSr.gameObject.SetActive(false);
        }

        void TickSparks(float dt)
        {
            for (int i = _sparks.Count - 1; i >= 0; i--)
            {
                var s = _sparks[i];
                if (s.Sr == null || !s.Sr.gameObject.activeSelf)
                {
                    _sparks.RemoveAt(i);
                    continue;
                }

                s.T -= dt;
                if (s.Seq != null && s.Seq.Length > 0)
                {
                    float dur = Mathf.Max(0.001f, s.Seq.Length / s.SeqFps);
                    int frame = Mathf.Clamp(
                        Mathf.FloorToInt((1f - s.T / dur) * s.Seq.Length), 0, s.Seq.Length - 1);
                    s.Sr.sprite = s.Seq[frame];
                }

                if (s.T <= 0f)
                {
                    s.Sr.gameObject.SetActive(false);
                    _sparks.RemoveAt(i);
                }
                else
                    _sparks[i] = s;
            }
        }

        SpriteRenderer RentSpark()
        {
            foreach (var sr in _sparkPool)
            {
                if (sr != null && !sr.gameObject.activeSelf)
                    return sr;
            }

            var root = _layerRoot?.Invoke();
            var go = new GameObject("P2D_FxSpark");
            go.transform.SetParent(root != null ? root : _host.transform, false);
            var created = go.AddComponent<SpriteRenderer>();
            _sparkPool.Add(created);
            return created;
        }

        SpriteRenderer RentMuzzle()
        {
            foreach (var sr in _muzzlePool)
            {
                if (sr != null && !sr.gameObject.activeSelf)
                    return sr;
            }

            var root = _layerRoot?.Invoke();
            var go = new GameObject("P2D_GruntMuzzle");
            go.transform.SetParent(root != null ? root : _host.transform, false);
            var created = go.AddComponent<SpriteRenderer>();
            _muzzlePool.Add(created);
            return created;
        }

        readonly List<SpriteRenderer> _captureTracers = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _captureTracerHeads = new List<SpriteRenderer>();
        readonly List<CaptureTracerLine> _captureTracerLines = new List<CaptureTracerLine>();
        const float Fx4MuzzleScale = 1.55f;
        const float Fx4DustScale = 1.65f;

        struct CaptureTracerLine
        {
            public Vector2 Muzzle;
            public Vector2 Head;
            public Vector2 Tail;
        }

        float TracerWorldLengthScreenPx(float footPct, float px)
        {
            float pct = px / 1080f * 100f;
            return Proto_GreyboxLayout.ScreenPercentToWorldHeight(pct);
        }

        float ClampTracerWorldLength(float footPct, float headDistFromMuzzle)
        {
            float minW = TracerWorldLengthScreenPx(footPct, 40f);
            float maxW = TracerWorldLengthScreenPx(footPct, 110f);
            float len = Mathf.Lerp(minW, maxW, 0.55f);
            return Mathf.Clamp(len, minW, Mathf.Min(maxW, headDistFromMuzzle - 0.04f));
        }

        public void SpawnCaptureCombatVolley(System.Collections.Generic.IReadOnlyList<SpriteRenderer> bandits,
            Vector2 playerFoot)
        {
            _captureTracerLines.Clear();
            if (bandits == null || bandits.Count == 0) return;
            var enemyTrail = PerspCat?.fxTracerEnemyTrail ?? PerspCat?.fxTracerEnemyHead;
            var enemyHead = PerspCat?.fxTracerEnemyHead ?? enemyTrail;
            if (enemyTrail == null) return;
            var heroAim = playerFoot + new Vector2(0f, 0.42f);
            int tracersSpawned = 0;
            const int minTracers = 6;
            const int maxTracers = 10;
            int perBandit = Mathf.Clamp(maxTracers / Mathf.Max(1, bandits.Count), 2, 3);

            foreach (var banditSr in bandits)
            {
                if (banditSr == null || !banditSr.gameObject.activeSelf) continue;
                var foot = (Vector2)banditSr.transform.position;
                string logDir = Proto_GreyboxPersp2DDirection.EnemyFacePlayer(foot, playerFoot);
                Proto_GreyboxPersp2DDirection.Resolve("bandit", logDir, out _, out var flipX);
                float visScale = Proto_GreyboxPersp2DStageLayout.CharacterUniformScaleAtFootWorld(foot.y);
                var muzzle = Proto_GreyboxPersp2DAnimJson.MuzzleWorldFromFoot(foot, visScale, "bandit", logDir, flipX, 0);
                var jitter = new Vector2(Random.Range(-0.12f, 0.12f), Random.Range(-0.08f, 0.14f));
                var aim = heroAim + jitter;
                SpawnGruntMuzzleFx(muzzle, Fx4MuzzleScale);
                var toward = (aim - muzzle);
                if (toward.sqrMagnitude < 1e-6f) toward = Vector2.down;
                toward.Normalize();
                float footPct = Proto_GreyboxLayout.WorldYToScreenPercentFromTop(foot.y);
                for (int pass = 0; pass < perBandit && tracersSpawned < maxTracers; pass++)
                {
                    float along01 = Mathf.Lerp(0.08f, 0.82f, (pass + 0.5f) / perBandit);
                    SpawnEnemyTracerSegment(muzzle, toward, footPct, along01, enemyTrail, enemyHead, aim);
                    tracersSpawned++;
                }
            }

            while (tracersSpawned < minTracers && bandits.Count > 0)
            {
                var banditSr = bandits[tracersSpawned % bandits.Count];
                if (banditSr == null) break;
                var foot = (Vector2)banditSr.transform.position;
                string logDir2 = Proto_GreyboxPersp2DDirection.EnemyFacePlayer(foot, playerFoot);
                Proto_GreyboxPersp2DDirection.Resolve("bandit", logDir2, out _, out var flipX2);
                float vis2 = Proto_GreyboxPersp2DStageLayout.CharacterUniformScaleAtFootWorld(foot.y);
                var muzzle = Proto_GreyboxPersp2DAnimJson.MuzzleWorldFromFoot(foot, vis2, "bandit", logDir2, flipX2, 0);
                var toward = (heroAim - muzzle).normalized;
                SpawnEnemyTracerSegment(muzzle, toward,
                    Proto_GreyboxLayout.WorldYToScreenPercentFromTop(foot.y),
                    Random.Range(0.15f, 0.7f), enemyTrail, enemyHead, heroAim);
                tracersSpawned++;
            }

            SpawnImpactDustScaled(playerFoot + new Vector2(-0.62f + Random.Range(-0.08f, 0.05f), 0.01f + Random.Range(0f, 0.05f)),
                Fx4DustScale);
            SpawnImpactDustScaled(playerFoot + new Vector2(0.28f + Random.Range(-0.04f, 0.12f), 0.02f + Random.Range(-0.02f, 0.04f)),
                Fx4DustScale * 0.95f);
            SpawnImpactDustScaled(playerFoot + new Vector2(-0.12f + Random.Range(-0.15f, 0.1f), 0.06f + Random.Range(0f, 0.03f)),
                Fx4DustScale * 0.88f);
        }

        void SpawnImpactDustScaled(Vector2 groundPos, float scaleMul)
        {
            float fxScale = Proto_GreyboxPersp2DStageLayout.CharacterUniformScaleAtFootWorld(groundPos.y) * scaleMul;
            var sr = RentSpark();
            _fxV2 ??= Proto_GreyboxFxV2Catalog.LoadRuntime();
            var groundFx = PerspCat?.fxGroundImpact ?? _fxV2?.dustGround;
            if (groundFx != null && groundFx.Length > 0)
            {
                sr.sprite = groundFx[0];
                sr.color = Color.white;
                sr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(groundFx[0], 0.38f * fxScale);
                _sparks.Add(new SparkFx { Sr = sr, T = groundFx.Length / 14f, Frame = 0, Seq = groundFx, SeqFps = 14f });
            }
            else
            {
                sr.sprite = Proto_GreyboxSpriteUtil.GetCircleSprite(false);
                sr.color = new Color(0.72f, 0.62f, 0.48f, 0.55f);
                sr.transform.localScale = Vector3.one * (0.28f * fxScale);
                _sparks.Add(new SparkFx { Sr = sr, T = 0.45f, Frame = 0 });
            }

            sr.transform.position = new Vector3(groundPos.x, groundPos.y, 0f);
            sr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(groundPos.y) + 1;
            sr.gameObject.SetActive(true);
        }

        void SpawnEnemyTracerSegment(Vector2 muzzle, Vector2 toward, float footPct, float along01, Sprite trail,
            Sprite head, Vector2 aim)
        {
            float shotDist = Vector2.Distance(muzzle, aim);
            float headDist = Mathf.Max(0.15f, shotDist * along01);
            var headPos = muzzle + toward * headDist;
            float trailLen = ClampTracerWorldLength(footPct, headDist);
            var tailPos = headPos - toward * trailLen;
            if (Vector2.Dot(tailPos - muzzle, toward) < -0.01f)
            {
                tailPos = muzzle + toward * 0.02f;
                trailLen = Vector2.Distance(headPos, tailPos);
            }

            _captureTracerLines.Add(new CaptureTracerLine { Muzzle = muzzle, Head = headPos, Tail = tailPos });
            float thick = TracerThicknessWorld(footPct) * 1.1f;
            float angle = Mathf.Atan2(toward.y, toward.x) * Mathf.Rad2Deg;
            var center = (headPos + tailPos) * 0.5f;
            var sr = RentCaptureTracer();
            sr.sprite = trail;
            sr.color = new Color(1f, Mathf.Lerp(0.72f, 0.88f, along01), 0.22f, Mathf.Lerp(0.75f, 0.95f, along01));
            sr.transform.position = new Vector3(center.x, center.y, 0f);
            sr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldSize(trail, trailLen, thick);
            sr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            sr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(muzzle.y) + 3;
            sr.gameObject.SetActive(true);
            if (head == null) return;
            var tip = RentCaptureTracerHead();
            tip.sprite = head;
            tip.color = new Color(1f, 0.98f, 0.45f, 1f);
            tip.transform.position = new Vector3(headPos.x, headPos.y, 0f);
            tip.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldSize(head, thick * 2.4f, thick * 2.1f);
            tip.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            tip.sortingOrder = sr.sortingOrder + 1;
            tip.gameObject.SetActive(true);
        }

        void SpawnGruntMuzzleFx(Vector2 worldPos, float scaleMul)
        {
            _fxV2 ??= Proto_GreyboxFxV2Catalog.LoadRuntime();
            var muzzle = PerspCat?.fxMuzzleFlash ?? _fxV2?.muzzleFlash;
            if (muzzle == null || muzzle.Length == 0) return;
            var sr = RentMuzzle();
            sr.sprite = muzzle[0];
            sr.color = Color.white;
            float visScale = Proto_GreyboxPersp2DStageLayout.CharacterUniformScaleAtFootWorld(worldPos.y);
            sr.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
            float diam = Proto_GreyboxLayout.ScreenPercentToWorldHeight(Proto_GreyboxTuning.PlayerMuzzleFlashHeightPercent) *
                         visScale * scaleMul;
            sr.transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(muzzle[0], diam);
            sr.sortingOrder = Proto_GreyboxLayout.CombatDrawSortingOrder(worldPos.y) + 2;
            sr.gameObject.SetActive(true);
            _sparks.Add(new SparkFx { Sr = sr, T = Proto_GreyboxTuning.PlayerMuzzleFlashDuration * 1.4f, Frame = 0, Seq = muzzle, SeqFps = 24f });
        }

        public void WriteFxCaptureCheck(Camera cam, Vector2 heroFoot, StringBuilder sb)
        {
            if (cam == null) return;
            sb.AppendLine("fx_check_coords=top_left_y_down_1920x1080");
            var heroScreen = WorldToScreenPx(cam, heroFoot + Vector2.up * 0.35f);
            int tracerIdx = 0;
            foreach (var line in _captureTracerLines)
            {
                var head = WorldToScreenPx(cam, line.Head);
                var tail = WorldToScreenPx(cam, line.Tail);
                var muz = WorldToScreenPx(cam, line.Muzzle);
                float shotAngle = Mathf.Atan2(head.y - muz.y, head.x - muz.x) * Mathf.Rad2Deg;
                float tracerAngle = Mathf.Atan2(head.y - tail.y, head.x - tail.x) * Mathf.Rad2Deg;
                float err = Mathf.Abs(Mathf.DeltaAngle(tracerAngle, shotAngle));
                bool tailBetween = Vector2.Dot(line.Tail - line.Muzzle, line.Head - line.Muzzle) > 0f
                    && Vector2.Distance(line.Muzzle, line.Tail) < Vector2.Distance(line.Muzzle, line.Head) + 0.01f;
                sb.AppendLine(
                    $"tracer_{tracerIdx}: muzzle=({muz.x:F0},{muz.y:F0}) head=({head.x:F0},{head.y:F0}) tail=({tail.x:F0},{tail.y:F0}) angle_err_deg={err:F1} tail_between_muzzle_and_head={tailBetween}");
                tracerIdx++;
            }

            sb.AppendLine($"tracer_count={tracerIdx}");
            int muzzleN = 0;
            foreach (var sp in _sparks)
            {
                if (sp.Sr == null || !sp.Sr.gameObject.activeSelf || sp.Sr.gameObject.name != "P2D_GruntMuzzle") continue;
                var px = SpriteHeightPx(cam, sp.Sr);
                sb.AppendLine($"muzzle_{muzzleN}: center_px=({px.cx:F0},{px.cy:F0}) height_px={px.h:F1}");
                muzzleN++;
            }

            sb.AppendLine($"muzzle_count={muzzleN}");
            int dustN = 0;
            foreach (var sp in _sparks)
            {
                if (sp.Sr == null || !sp.Sr.gameObject.activeSelf || sp.Sr.gameObject.name != "P2D_FxSpark") continue;
                var px = SpriteHeightPx(cam, sp.Sr);
                sb.AppendLine($"dust_{dustN}: center_px=({px.cx:F0},{px.cy:F0}) height_px={px.h:F1}");
                dustN++;
            }

            sb.AppendLine($"dust_count={dustN}");
        }

        static Vector2 WorldToScreenPx(Camera cam, Vector2 world)
        {
            var sp = cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
            float x = 1920f * (sp.x / Mathf.Max(Screen.width, 1f));
            float yTop = 1080f * (1f - sp.y / Mathf.Max(Screen.height, 1f));
            return new Vector2(x, yTop);
        }

        static (float cx, float cy, float h) SpriteHeightPx(Camera cam, SpriteRenderer sr)
        {
            var b = sr.bounds;
            var top = WorldToScreenPx(cam, new Vector2(b.center.x, b.max.y));
            var bot = WorldToScreenPx(cam, new Vector2(b.center.x, b.min.y));
            var c = WorldToScreenPx(cam, b.center);
            return (c.x, c.y, Mathf.Abs(top.y - bot.y));
        }

        SpriteRenderer RentCaptureTracer()
        {
            foreach (var sr in _captureTracers)
            {
                if (sr != null && !sr.gameObject.activeSelf)
                    return sr;
            }

            var root = _layerRoot?.Invoke();
            var go = new GameObject("P2D_CaptureTracer");
            go.transform.SetParent(root != null ? root : _host.transform, false);
            var created = go.AddComponent<SpriteRenderer>();
            _captureTracers.Add(created);
            return created;
        }

        SpriteRenderer RentCaptureTracerHead()
        {
            foreach (var sr in _captureTracerHeads)
            {
                if (sr != null && !sr.gameObject.activeSelf)
                    return sr;
            }

            var root = _layerRoot?.Invoke();
            var go = new GameObject("P2D_CaptureTracerHead");
            go.transform.SetParent(root != null ? root : _host.transform, false);
            var created = go.AddComponent<SpriteRenderer>();
            _captureTracerHeads.Add(created);
            return created;
        }
    }
}