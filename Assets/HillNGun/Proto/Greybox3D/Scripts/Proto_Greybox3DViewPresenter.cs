using System.Collections.Generic;
using HillNGun.Proto.Greybox;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    [DisallowMultipleComponent]
    public sealed class Proto_Greybox3DViewPresenter : MonoBehaviour
    {
        const float HeroMinHeight = 1.85f;
        const float GruntMinHeight = 2.35f;

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

        public Camera WorldCamera => _cam;

        public void EnsureCreated()
        {
            if (_built) return;
            _built = true;

            var camGo = new GameObject("Greybox3D_Camera");
            camGo.transform.SetParent(transform, false);
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = false;
            _cam.fieldOfView = 50f;
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

            _stageRoot = Proto_Greybox3DSceneBuilder.BuildRoot(transform);
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
            if (rolling)
                _hero.rotation = _heroBaseRot * Quaternion.Euler(_animPhase * 720f, 0f, 0f);
            else
            {
                float yaw = Mathf.Clamp(move.x, -1f, 1f) * 18f;
                _hero.rotation = _heroBaseRot * Quaternion.Euler(0f, yaw, 0f);
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
                bool down = host.Hearts != null && host.Hearts.Hearts <= 0;
                driver.Tick(_animPhase, dt, moving, firing, hit, down, rolling);
            }

            _aimMarker.position = Proto_Greybox3DFootMap.GroundPointToWorld(aim) + Vector3.up * 0.35f;
            SyncGrunts(host);
            SyncBullets(host);
            SyncGrenade(host);
            UpdateCamera(heroPos);
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
            var desired = new Vector3(heroPos.x * 0.06f, 2.65f, heroPos.z - 5.5f);
            var lookAt = heroPos + new Vector3(0f, 1.25f, 8.8f);
            var lookRot = Quaternion.LookRotation(lookAt - desired, Vector3.up);
            if (!Application.isPlaying)
            {
                _cam.transform.position = desired;
                _cam.transform.rotation = lookRot;
                return;
            }

            desired.x = Mathf.Lerp(_cam.transform.position.x, desired.x, 0.12f);
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, desired, 0.16f);
            _cam.transform.rotation = Quaternion.Slerp(_cam.transform.rotation, lookRot, 0.2f);
        }

        void SyncGrunts(Proto_GreyboxPlayerController host)
        {
            var grunts = host.WaveDirector.Grunts;
            while (_gruntViews.Count < grunts.Count)
            {
                var g = Proto_Greybox3DCharacterMesh.BuildGrunt(_stageRoot);
                g.gameObject.SetActive(false);
                _gruntViews.Add(g);
            }

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
                view.gameObject.SetActive(alive && !grunt.IsHidden);
                if (!alive || grunt.IsHidden) continue;

                var foot = grunt.Target.FootPosition;
                float h = Mathf.Max(Proto_Greybox3DFootMap.CharacterHeightAtFoot(foot, false), GruntMinHeight);
                var pos = Proto_Greybox3DFootMap.FootToWorld(foot);
                view.position = pos;
                view.localScale = Vector3.one * h;
                view.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

                var driver = view.GetComponentInChildren<Proto_Greybox3DAnimDriver>();
                if (driver != null)
                {
                    bool down = !alive;
                    driver.Tick(_animPhase + i * 0.17f, Application.isPlaying ? Time.deltaTime : 0.02f,
                        false, false, false, down, false);
                }
            }
        }

        void SyncBullets(Proto_GreyboxPlayerController host)
        {
            SyncBulletList(host.AllyBullets, _allyBulletViews, Proto_Greybox3DMaterials.SharedAllyBullet, 0.11f, 0.28f);
            SyncBulletList(host.EnemyBullets, _enemyBulletViews, Proto_Greybox3DMaterials.SharedEnemyBullet, 0.38f, 0.85f);
        }

        void SyncBulletList<T>(IReadOnlyList<T> sims, List<Transform> views, Material mat, float diam, float length)
            where T : class
        {
            while (views.Count < sims.Count)
                views.Add(SpawnBullet(mat, diam, length));

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
                var world = Proto_Greybox3DFootMap.GroundPointToWorld(pos) + Vector3.up * 0.55f;
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

        Transform SpawnBullet(Material mat, float diam, float length)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Greybox3D_Bullet";
            go.transform.SetParent(_stageRoot, false);
            go.transform.localScale = new Vector3(diam, diam, length);
            Proto_Greybox3DUtil.DestroySafe(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            // soft glow shell
            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "Glow";
            shell.transform.SetParent(go.transform, false);
            shell.transform.localScale = Vector3.one * 1.55f;
            Proto_Greybox3DUtil.DestroySafe(shell.GetComponent<Collider>());
            shell.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedEnemyGlow;
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
                _grenadeRing.gameObject.SetActive(show);
                _grenadeRing.position = b + Vector3.up * 0.03f;
                float r = g != null ? Mathf.Max(0.8f, g.ExplosionRadius * 0.85f) : 1.2f;
                _grenadeRing.localScale = new Vector3(r * 2f, 0.025f, r * 2f);
            }
        }
    }
}
