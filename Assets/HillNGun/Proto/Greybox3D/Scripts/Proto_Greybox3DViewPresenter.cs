using System.Collections.Generic;
using HillNGun.Proto.Greybox;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    [DisallowMultipleComponent]
    public sealed class Proto_Greybox3DViewPresenter : MonoBehaviour
    {
        const float HeroMinHeight = 0.85f;
        const float GruntMinHeight = 0.65f;

        Camera _cam;
        Transform _stageRoot;
        Transform _hero;
        Transform _aimMarker;
        Transform _grenadeArcRoot;
        Transform _grenadeRing;
        readonly List<Transform> _grenadeArcDots = new List<Transform>();
        readonly List<Transform> _gruntViews = new List<Transform>();
        readonly List<Transform> _allyBulletViews = new List<Transform>();
        readonly List<Transform> _enemyBulletViews = new List<Transform>();
        float _animPhase;
        bool _built;
        bool _active;
        Quaternion _heroBaseRot;
        Proto_Greybox3DFx _fx;
        int _prevAllyAlive;
        int _prevEnemyAlive;
        bool _wasExploding;
        float _rollDustT;

        public Camera WorldCamera => _cam;

        public void EnsureCreated()
        {
            if (_built) return;
            _built = true;

            var camGo = new GameObject("Greybox3D_Camera");
            camGo.transform.SetParent(transform, false);
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = false;
            _cam.fieldOfView = 48f;
            _cam.nearClipPlane = 0.12f;
            _cam.farClipPlane = 180f;
            _cam.clearFlags = RenderSettings.skybox != null
                ? CameraClearFlags.Skybox
                : CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.45f, 0.72f, 0.95f);
            _cam.depth = 1f;
            _cam.enabled = false;
            _cam.allowMSAA = false;
            _cam.allowHDR = false;
            _cam.gameObject.AddComponent<Proto_Greybox3DVignette>();

            _stageRoot = Proto_Greybox3DSceneBuilder.BuildRoot(transform);
            _fx = Proto_Greybox3DFx.Ensure(_stageRoot);
            _hero = Proto_Greybox3DCharacterMesh.BuildHero(_stageRoot);
            _heroBaseRot = Quaternion.identity;

            _aimMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            _aimMarker.name = "Greybox3D_Aim";
            _aimMarker.SetParent(_stageRoot, false);
            _aimMarker.localScale = Vector3.one * 0.22f;
            Proto_Greybox3DUtil.DestroySafe(_aimMarker.GetComponent<Collider>());
            _aimMarker.GetComponent<MeshRenderer>().sharedMaterial =
                Proto_Greybox3DMaterials.Cel(new Color(0.95f, 0.18f, 0.18f), outline: 0.008f);

            BuildGrenadeViews();
            gameObject.SetActive(false);
        }

        void BuildGrenadeViews()
        {
            _grenadeArcRoot = new GameObject("Greybox3D_GrenadeArc").transform;
            _grenadeArcRoot.SetParent(_stageRoot, false);
            for (int i = 0; i < 8; i++)
            {
                var d = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                d.name = "ArcDot";
                d.SetParent(_grenadeArcRoot, false);
                d.localScale = Vector3.one * 0.16f;
                Proto_Greybox3DUtil.DestroySafe(d.GetComponent<Collider>());
                d.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedRing;
                d.gameObject.SetActive(false);
                _grenadeArcDots.Add(d);
            }

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            ring.name = "Greybox3D_GrenadeRing";
            ring.SetParent(_stageRoot, false);
            ring.localScale = new Vector3(1.6f, 0.02f, 1.6f);
            Proto_Greybox3DUtil.DestroySafe(ring.GetComponent<Collider>());
            ring.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedRing;
            ring.gameObject.SetActive(false);
            _grenadeRing = ring;
        }

        public void SetActive(bool active, Proto_GreyboxPlayerController host)
        {
            EnsureCreated();
            _active = active;
            gameObject.SetActive(active);
            _cam.enabled = active;
            if (active)
            {
                _cam.tag = "MainCamera";
                Sync(host);
            }
            else
                _cam.tag = "Untagged";
        }

        public void Sync(Proto_GreyboxPlayerController host)
        {
            if (!_active || host == null) return;
            float dt = Application.isPlaying ? Time.deltaTime : 0.02f;
            _animPhase += dt;

            var foot = host.Motor.FootPosition;
            var aim = host.Aim.AimGroundPosition();
            float bodyH = Mathf.Max(Proto_Greybox3DFootMap.CharacterHeightAtFoot(foot, true), HeroMinHeight);
            var heroPos = Proto_Greybox3DFootMap.FootToWorld(foot);
            _hero.position = heroPos;
            _hero.localScale = Vector3.one * bodyH;

            bool rolling = host.Roll.IsRolling;
            var move = host.Motor.LastMoveDirection;
            var heroFx = _hero.GetComponent<Proto_Greybox3DCombatFx>();
            bool down = host.Hearts != null && host.Hearts.Hearts <= 0;
            if (rolling)
                _hero.rotation = _heroBaseRot * Quaternion.Euler(_animPhase * 540f, move.x * 12f, 0f);
            else if (heroFx == null || !heroFx.IsDying)
            {
                float yaw = Mathf.Clamp(move.x, -1f, 1f) * 18f;
                _hero.rotation = _heroBaseRot * Quaternion.Euler(0f, yaw, 0f);
            }

            if (heroFx != null)
            {
                heroFx.Tick(dt, host.PlayerHitFlashActive, down, rolling, Vector3.back);
                _hero.position = heroPos + heroFx.KnockbackOffset;
            }

            if (rolling)
            {
                _rollDustT -= dt;
                if (_rollDustT <= 0f)
                {
                    _fx?.RollDust(heroPos + Vector3.up * 0.05f);
                    _rollDustT = 0.08f;
                }
            }

            var driver = _hero.GetComponentInChildren<Proto_Greybox3DAnimDriver>();
            if (driver != null)
            {
                bool moving = move.sqrMagnitude > 0.04f;
                bool firing = host.Gun != null && host.AllyBullets != null &&
                              CountAliveAllies(host) > 0;
                // fire pose: use a short window after fire via gun if available
                firing = host.Aim != null && (firing || WasRecentShot(host));
                bool hit = host.PlayerHitFlashActive;
                float dzdy = (Proto_Greybox3DFootMap.DepthZFar - Proto_Greybox3DFootMap.DepthZNear) /
                             Mathf.Max(0.001f, Proto_Greybox3DFootMap.FootWorldYEnemyFar - Proto_Greybox3DFootMap.FootWorldYPlayerNear);
                var vel = host.Motor.Velocity;
                float groundSpeed = new Vector2(vel.x, vel.y * dzdy).magnitude;
                float cycleDist = Proto_GreyboxTuning.HeroVisibleWidthAtMidBand() *
                                  Proto_GreyboxTuning.WalkCycleDistanceInBodyWidths;
                driver.Tick(_animPhase, dt, moving, firing, hit, down, rolling, groundSpeed, cycleDist);
            }

            _aimMarker.position = Proto_Greybox3DFootMap.GroundPointToWorld(aim) + Vector3.up * 0.35f;
            SyncGrunts(host);
            SyncBullets(host);
            SyncGrenade(host);
            _fx?.Tick(dt);
            ApplyDress(_hero);
            UpdateCamera(heroPos);
        }

        static void ApplyDress(Transform root)
        {
            if (root == null) return;
            var follows = root.GetComponentsInChildren<Proto_Greybox3DDressFollow>(true);
            for (int i = 0; i < follows.Length; i++)
                follows[i].Apply();
        }

        static int CountAliveAllies(Proto_GreyboxPlayerController host)
        {
            int n = 0;
            var list = host.AllyBullets;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && list[i].Alive) n++;
            return n;
        }

        static bool WasRecentShot(Proto_GreyboxPlayerController host)
        {
            // Ally bullets in flight is a good enough "shooting" cue for the proto.
            return CountAliveAllies(host) > 0;
        }

        void UpdateCamera(Vector3 heroPos)
        {
            // Raised, wider framing: player bottom-center, fence + enemy row readable, sky band on top.
            // Just above hat height, behind the hero, nearly level so the hat brim reads.
            // Smaller BS-scale hero still sits bottom-center; look over the hat at the enemy row.
            var desired = new Vector3(heroPos.x * 0.05f, 2.55f, heroPos.z - 4.85f);
            var lookAt = heroPos + new Vector3(0f, 0.72f, 9.2f);
            var lookRot = Quaternion.LookRotation(lookAt - desired, Vector3.up);
            if (!Application.isPlaying)
            {
                _cam.transform.position = desired;
                _cam.transform.rotation = lookRot;
                return;
            }

            desired.x = Mathf.Lerp(_cam.transform.position.x, desired.x, 0.12f);
            var shake = _fx != null ? _fx.ShakeOffset : Vector3.zero;
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, desired, 0.16f) + shake;
            _cam.transform.rotation = Quaternion.Slerp(_cam.transform.rotation, lookRot, 0.2f);
        }

        void SyncGrunts(Proto_GreyboxPlayerController host)
        {
            var grunts = host.WaveDirector.Grunts;
            while (_gruntViews.Count < grunts.Count)
            {
                int idx = _gruntViews.Count;
                var g = Proto_Greybox3DCharacterMesh.BuildGrunt(_stageRoot, idx % 3);
                g.gameObject.SetActive(false);
                _gruntViews.Add(g);
            }

            float dt = Application.isPlaying ? Time.deltaTime : 0.02f;
            for (int i = 0; i < _gruntViews.Count; i++)
            {
                var view = _gruntViews[i];
                if (i >= grunts.Count)
                {
                    view.gameObject.SetActive(false);
                    continue;
                }

                var grunt = grunts[i];
                bool alive = grunt.Target.IsAlive;
                var fx = view.GetComponent<Proto_Greybox3DCombatFx>();
                if (fx != null)
                    fx.Tick(dt, false, !alive, false, Vector3.forward);

                bool dying = fx != null && fx.IsDying;
                bool show = !grunt.IsHidden && (alive || dying);
                view.gameObject.SetActive(show);
                if (!show) continue;

                var foot = grunt.Target.FootPosition;
                float h = Mathf.Max(Proto_Greybox3DFootMap.CharacterHeightAtFoot(foot, false), GruntMinHeight);
                var pos = Proto_Greybox3DFootMap.FootToWorld(foot);
                if (!dying)
                {
                    view.position = pos;
                    view.localScale = Vector3.one * h;
                    view.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                }

                var driver = view.GetComponentInChildren<Proto_Greybox3DAnimDriver>();
                if (driver != null)
                    driver.Tick(_animPhase + i * 0.17f, dt, false, false, dying && !alive, !alive, false);
                ApplyDress(view);
            }
        }

        void SyncBullets(Proto_GreyboxPlayerController host)
        {
            float heroW = Proto_GreyboxTuning.HeroVisibleWidthAtMidBand();
            float allyD = Mathf.Max(0.06f, Proto_GreyboxTuning.AllyBulletWidthOfHero * heroW);
            float enemyD = Mathf.Max(0.08f, Proto_GreyboxTuning.EnemyBulletWidthOfHero * heroW);
            if (host.RuntimeTuning != null)
            {
                allyD = host.RuntimeTuning.AllyBulletDiameter;
                enemyD = host.RuntimeTuning.EnemyBulletDiameter;
            }
            SyncBulletList(host.AllyBullets, _allyBulletViews, Proto_Greybox3DMaterials.SharedAllyBullet, allyD, allyD * 2.2f, false);
            SyncBulletList(host.EnemyBullets, _enemyBulletViews, Proto_Greybox3DMaterials.SharedEnemyBullet, enemyD, enemyD * 2.4f, true);
            int allyNow = CountAliveAllies(host);
            int enemyNow = CountAliveEnemies(host);
            if (allyNow > _prevAllyAlive)
            {
                var muzzlePos = _hero.position + Vector3.up * (1.15f) + _hero.forward * 0.45f + _hero.right * 0.18f;
                _fx?.Muzzle(muzzlePos, _hero.forward);
                _fx?.Casing(muzzlePos + Vector3.up * 0.05f, _hero.right);
            }
            if (enemyNow > _prevEnemyAlive)
            {
                // flash at the new enemy bullet
                for (int i = 0; i < host.EnemyBullets.Count; i++)
                {
                    var b = host.EnemyBullets[i];
                    if (b == null || !b.Alive) continue;
                    var wp = Proto_Greybox3DFootMap.GroundPointToWorld(b.GroundPosition) + Vector3.up * 1.1f;
                    _fx?.Muzzle(wp, Vector3.back);
                    break;
                }
            }
            if (allyNow < _prevAllyAlive)
                TryCrateImpact(host);
            _prevAllyAlive = allyNow;
            _prevEnemyAlive = enemyNow;
        }

        static int CountAliveEnemies(Proto_GreyboxPlayerController host)
        {
            int n = 0;
            var list = host.EnemyBullets;
            if (list == null) return 0;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && list[i].Alive) n++;
            return n;
        }

        void TryCrateImpact(Proto_GreyboxPlayerController host)
        {
            // When an ally bullet dies, puff near the nearest crate prop.
            Transform nearest = null;
            if (_stageRoot != null)
            {
                float best = 2.4f;
                Vector3 hero = _hero != null ? _hero.position : Vector3.zero;
                foreach (var t in _stageRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.IndexOf("Crate", System.StringComparison.Ordinal) < 0) continue;
                    // use last known ally bullet? just puff at crate closest to aim
                    var aim = Proto_Greybox3DFootMap.GroundPointToWorld(host.Aim.AimGroundPosition());
                    float d = Vector3.Distance(t.position, aim);
                    if (d < best)
                    {
                        best = d;
                        nearest = t;
                    }
                }
                if (nearest != null)
                    _fx?.Impact(nearest.position + Vector3.up * 0.35f);
            }
        }

        void SyncBulletList<T>(IReadOnlyList<T> sims, List<Transform> views, Material mat, float diam, float length, bool isEnemy)
            where T : class
        {
            while (views.Count < sims.Count)
                views.Add(SpawnBullet(mat, diam, length, isEnemy));

            for (int i = 0; i < views.Count; i++)
            {
                var view = views[i];
                if (i >= sims.Count)
                {
                    view.gameObject.SetActive(false);
                    continue;
                }

                bool alive = false;
                Vector2 pos = default;
                Vector2 vel = default;
                if (sims[i] is Proto_GreyboxAllyBulletSim ally && ally.Alive)
                {
                    alive = true;
                    pos = ally.GroundPosition;
                    vel = ally.GroundVelocity;
                }
                else if (sims[i] is Proto_GreyboxBulletSim enemy && enemy.Alive)
                {
                    alive = true;
                    pos = enemy.GroundPosition;
                    vel = enemy.GroundVelocity;
                }

                view.gameObject.SetActive(alive);
                if (!alive) continue;
                var world = Proto_Greybox3DFootMap.GroundPointToWorld(pos) + Vector3.up * 0.70f;
                view.position = world;
                if (vel.sqrMagnitude > 0.01f)
                {
                    var dir = new Vector3(vel.x, 0f,
                        Proto_Greybox3DFootMap.FootWorldYToDepthZ(pos.y + vel.y * 0.02f) -
                        Proto_Greybox3DFootMap.FootWorldYToDepthZ(pos.y));
                    if (dir.sqrMagnitude > 0.001f)
                        view.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up) *
                                        Quaternion.Euler(90f, 0f, 0f);
                }
            }
        }

        Transform SpawnBullet(Material mat, float diam, float length, bool enemy)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = enemy ? "Greybox3D_EnemyBullet" : "Greybox3D_AllyBullet";
            go.transform.SetParent(_stageRoot, false);
            go.transform.localScale = new Vector3(diam, diam, length);
            Proto_Greybox3DUtil.DestroySafe(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "Glow";
            shell.transform.SetParent(go.transform, false);
            shell.transform.localScale = Vector3.one * (enemy ? 1.7f : 1.45f);
            Proto_Greybox3DUtil.DestroySafe(shell.GetComponent<Collider>());
            shell.GetComponent<MeshRenderer>().sharedMaterial = enemy
                ? Proto_Greybox3DMaterials.SharedEnemyGlow
                : mat;
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = enemy ? 0.28f : 0.14f;
            trail.startWidth = diam * (enemy ? 1.15f : 0.7f);
            trail.endWidth = diam * 0.08f;
            trail.minVertexDistance = 0.04f;
            trail.material = mat;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.numCornerVertices = 2;
            trail.numCapVertices = 2;
            go.SetActive(false);
            return go.transform;
        }

        void SyncGrenade(Proto_GreyboxPlayerController host)
        {
            var g = host.Grenade;
            bool preview = host.GrenadeAimPreviewActive;
            bool flying = g != null && g.IsFlying;
            bool show = preview || flying;
            Vector2 start = host.Motor.FootPosition;
            Vector2 end = preview ? host.GrenadePreviewLanding : (g != null ? g.AimGround : start);
            if (flying)
            {
                start = g.StartGround;
                end = g.AimGround;
            }

            var a = Proto_Greybox3DFootMap.GroundPointToWorld(start) + Vector3.up * 0.4f;
            var b = Proto_Greybox3DFootMap.GroundPointToWorld(end) + Vector3.up * 0.05f;
            for (int i = 0; i < _grenadeArcDots.Count; i++)
            {
                float t = (i + 1f) / (_grenadeArcDots.Count + 1f);
                var p = Vector3.Lerp(a, b, t);
                p.y += 2.4f * 4f * t * (1f - t);
                _grenadeArcDots[i].position = p;
                _grenadeArcDots[i].gameObject.SetActive(show);
            }

            if (_grenadeRing != null)
            {
                _grenadeRing.gameObject.SetActive(show && !host.Grenade.ExplosionVisible(host.SimTime));
                _grenadeRing.position = b + Vector3.up * 0.03f;
                float r = g != null ? Mathf.Max(0.8f, g.ExplosionRadius * 0.85f) : 1.2f;
                _grenadeRing.localScale = new Vector3(r * 2f, 0.025f, r * 2f);
            }

            bool exploding = g != null && g.ExplosionVisible(host.SimTime);
            if (exploding && !_wasExploding)
                _fx?.Explosion(b + Vector3.up * 0.2f, g.ExplosionRadius);
            _wasExploding = exploding;
        }
    }
}
