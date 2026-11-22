using System.Collections.Generic;
using HillNGun.Proto.Greybox;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    [DisallowMultipleComponent]
    public sealed class Proto_Greybox3DViewPresenter : MonoBehaviour
    {
        Camera _cam;
        Transform _stageRoot;
        Transform _hero;
        Transform _aimMarker;
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
            _cam.fieldOfView = 52f;
            _cam.nearClipPlane = 0.12f;
            _cam.farClipPlane = 140f;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.62f, 0.82f, 0.95f);
            _cam.depth = 1f;
            _cam.enabled = false;

            _stageRoot = Proto_Greybox3DSceneBuilder.BuildRoot(transform);
            _hero = Proto_Greybox3DCharacterMesh.BuildHero(_stageRoot);
            _heroBaseRot = _hero.rotation;

            _aimMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            _aimMarker.name = "Greybox3D_Aim";
            _aimMarker.SetParent(_stageRoot, false);
            _aimMarker.localScale = Vector3.one * 0.22f;
            var aimCol = _aimMarker.GetComponent<Collider>();
            if (aimCol != null) Proto_Greybox3DUtil.DestroySafe(aimCol);
            _aimMarker.GetComponent<MeshRenderer>().sharedMaterial =
                Proto_Greybox3DMaterials.Cel(new Color(0.95f, 0.85f, 0.35f));

            gameObject.SetActive(false);
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
            float bodyH = Proto_Greybox3DFootMap.CharacterHeightAtFoot(foot, true);
            var heroPos = Proto_Greybox3DFootMap.FootToWorld(foot);
            float bob = host.Roll.IsRolling ? 0f : Mathf.Sin(_animPhase * 9f) * 0.04f;
            float lean = host.Roll.IsRolling ? 0f : Mathf.Clamp(host.Motor.LastMoveDirection.x, -1f, 1f) * 6f;
            _hero.position = heroPos + new Vector3(0f, bodyH * 0.45f + bob, 0f);
            _hero.localScale = Vector3.one * (bodyH / 1.2f);

            if (host.Roll.IsRolling)
                _hero.rotation = _heroBaseRot * Quaternion.Euler(0f, _animPhase * 720f, 0f);
            else
                _hero.rotation = _heroBaseRot * Quaternion.Euler(0f, lean, 0f);

            _aimMarker.position = Proto_Greybox3DFootMap.GroundPointToWorld(aim) + Vector3.up * 0.25f;
            SyncGrunts(host);
            SyncBullets(host);
            UpdateCamera(heroPos);
        }

        void UpdateCamera(Vector3 heroPos)
        {
            var desired = new Vector3(heroPos.x * 0.35f, heroPos.y + 3.9f, heroPos.z - 7.4f);
            var lookAt = heroPos + new Vector3(0f, 1.15f, 10.5f);
            var lookRot = Quaternion.LookRotation(lookAt - desired, Vector3.up);
            if (!Application.isPlaying)
            {
                _cam.transform.position = desired;
                _cam.transform.rotation = lookRot;
                return;
            }

            desired.x = Mathf.Lerp(_cam.transform.position.x, desired.x, 0.12f);
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, desired, 0.18f);
            _cam.transform.rotation = Quaternion.Slerp(_cam.transform.rotation, lookRot, 0.22f);
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
                float h = Proto_Greybox3DFootMap.CharacterHeightAtFoot(foot, false);
                var pos = Proto_Greybox3DFootMap.FootToWorld(foot);
                view.position = pos + new Vector3(0f, h * 0.42f, 0f);
                view.localScale = Vector3.one * (h / 1.05f);
                view.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            }
        }

        void SyncBullets(Proto_GreyboxPlayerController host)
        {
            SyncBulletList(host.AllyBullets, _allyBulletViews, Proto_Greybox3DMaterials.SharedAllyBullet, 0.12f, 0.32f);
            SyncBulletList(host.EnemyBullets, _enemyBulletViews, Proto_Greybox3DMaterials.SharedEnemyBullet, 0.22f, 0.55f);
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
                var world = Proto_Greybox3DFootMap.GroundPointToWorld(pos) + Vector3.up * 0.35f;
                view.position = world;
                if (vel.sqrMagnitude > 0.01f)
                {
                    var dir = new Vector3(vel.x, 0f,
                        Proto_Greybox3DFootMap.FootWorldYToDepthZ(pos.y + vel.y * 0.02f) -
                        Proto_Greybox3DFootMap.FootWorldYToDepthZ(pos.y));
                    if (dir.sqrMagnitude > 0.001f)
                        view.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
            }
        }

        Transform SpawnBullet(Material mat, float diam, float length)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Greybox3D_Bullet";
            go.transform.SetParent(_stageRoot, false);
            go.transform.localScale = new Vector3(diam, length * 0.5f, diam);
            var col = go.GetComponent<Collider>();
            if (col != null) Proto_Greybox3DUtil.DestroySafe(col);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.SetActive(false);
            return go.transform;
        }
    }
}