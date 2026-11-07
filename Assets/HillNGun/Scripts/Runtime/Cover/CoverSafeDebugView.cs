using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Combat;
using HillNGun.Characters;
using HillNGun.Battlefield;

namespace HillNGun.Cover
{
    /// <summary>
    /// 12 §4.3 debug: collider footprint (white), guaranteed-safe centre interval (green bar) or "no safe slot" (red),
    /// height class label, hit circles; each actor is tagged "숨음" (inside the cover x-range = crouch / hide category)
    /// vs "보호" (inside the safe interval: a straight shot of the incoming radius is guaranteed blocked).
    /// Toggle: F4, pause menu "엄폐 디버그", or -cover-debug. View only.
    /// </summary>
    public class CoverSafeDebugView : MonoBehaviour
    {
        private static CoverSafeDebugView _instance;
        private static Material _mat;
        private CombatSystem _combat;
        private GUIStyle _style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (a == "-cover-debug") TerrainRules.CoverDebug = true;
            if (_instance != null) return;
            var go = new GameObject("[CoverSafeDebug]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CoverSafeDebugView>();
        }

        /// <summary>Largest bullet radius the given team can be hit by (opposing weapons, PvE enemy / boss bullets).</summary>
        public static float IncomingRadiusFor(Team team, CombatSystem combat)
        {
            float r = 0f;
            if (combat != null)
                foreach (var c in combat.Characters)
                    if (c != null && c.Team != team && c.Data?.weapon != null) r = Mathf.Max(r, c.Data.weapon.projectileRadius);
            var pve = team == Team.Player ? FindFirstObjectByType<HillNGun.PvE.PvEMatchManager>() : null;
            var st = pve != null ? pve.CurrentStage : null;
            if (st != null)
            {
                // only the bullets this stage can actually fire at us
                if (st.waves != null) foreach (var w in st.waves)
                        if (w?.enemies != null) foreach (var e in w.enemies) if (e?.enemyData != null) r = Mathf.Max(r, e.enemyData.bulletRadius);
                if (st.isBossStage && st.bossData?.phases != null)
                    foreach (var p in st.bossData.phases) if (p != null) r = Mathf.Max(r, p.fanShotBulletRadius);
            }
            return r > 0f ? r : 0.15f;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f4Key.wasPressedThisFrame) TerrainRules.CoverDebug = !TerrainRules.CoverDebug;
            _segs.Clear();
            if (TerrainRules.CoverDebug && SceneManager.GetActiveScene().name.EndsWith("_Match"))
            {
                if (_combat == null) _combat = ServiceLocator.Get<CombatSystem>();
                if (_combat != null) DrawWorld();
            }
        }

        private void DrawWorld()
        {
            float actorR = 0.5f * ScalePresets.CharacterMul;
            foreach (var c in _combat.Covers)
            {
                if (c == null || c.IsDestroyed) continue;
                var b = c.GetBounds();
                Color col = c.HeightClass == CoverHeightClass.Full ? new Color(0.7f, 0.5f, 1f) : c.HeightClass == CoverHeightClass.Low ? new Color(1f, 0.85f, 0.3f) : Color.white;
                Rect(b, col, 0.05f);
                float inc = IncomingRadiusFor(c.Team, _combat);
                float h = TerrainLayout.SafeHalfWidth(c.Width, actorR, inc);
                float y = c.Team == Team.Player ? b.yMin - 0.25f : b.yMax + 0.25f; // on the actor side
                if (h >= 0f) Line(new Vector2(c.Position.x - h, y), new Vector2(c.Position.x + h, y), new Color(0.2f, 1f, 0.3f), 0.14f);
                else { Line(new Vector2(c.Position.x - 0.25f, y - 0.2f), new Vector2(c.Position.x + 0.25f, y + 0.2f), new Color(1f, 0.2f, 0.2f), 0.08f);
                       Line(new Vector2(c.Position.x - 0.25f, y + 0.2f), new Vector2(c.Position.x + 0.25f, y - 0.2f), new Color(1f, 0.2f, 0.2f), 0.08f); }
            }
            foreach (var ch in _combat.Characters)
            {
                if (ch == null || ch.OverallState == CharacterOverallState.Eliminated) continue;
                Circle(ch.Position, ch.HitRadius, ch.Team == Team.Player ? new Color(0.3f, 0.9f, 1f) : new Color(1f, 0.5f, 0.4f), 0.04f);
            }
            foreach (var t in _combat.CombatTargets)
            {
                if (t == null || !t.CanBeHit) continue;
                if (t is HazardExplosiveBarrel hb) Circle(hb.Position, TerrainRules.Data.barrelRadius, hb.Armed ? Color.red : new Color(1f, 0.6f, 0.1f, 0.8f), 0.04f);
                else Circle(t.Position, t.HitRadius, new Color(1f, 0.5f, 0.4f), 0.04f);
            }
        }

        /// <summary>Status of an actor against its own team's covers on its row (for labels and tests / logs).</summary>
        public static string ActorCoverStatus(CharacterStateMachine ch, CombatSystem combat, out CoverInstance cover)
        {
            cover = null;
            if (ch == null || combat == null) return "";
            float inc = IncomingRadiusFor(ch.Team, combat);
            foreach (var c in combat.Covers)
            {
                if (c == null || c.IsDestroyed || c.Team != ch.Team) continue;
                float dy = ch.Team == Team.Player ? c.Position.y - ch.Position.y : ch.Position.y - c.Position.y;
                if (dy < 0f || dy > 1.0f) continue; // its own row's cover, between it and the enemy
                if (!TerrainLayout.InCoverXRange(c.Position.x, c.Width, ch.Position.x)) continue;
                cover = c;
                return TerrainLayout.GuaranteedSafe(c.Position.x, c.Width, ch.Position.x, ch.HitRadius, inc) ? "보호" : "숨음";
            }
            return "";
        }

        private void OnGUI()
        {
            if (!TerrainRules.CoverDebug || _combat == null || !SceneManager.GetActiveScene().name.EndsWith("_Match")) return;
            var cam = Camera.main; if (cam == null) return;
            float k = Screen.height / 1080f;
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _style.fontSize = Mathf.Max(10, Mathf.RoundToInt(15 * k));
            float actorR = 0.5f * ScalePresets.CharacterMul;
            foreach (var c in _combat.Covers)
            {
                if (c == null || c.IsDestroyed) continue;
                float inc = IncomingRadiusFor(c.Team, _combat);
                float h = TerrainLayout.SafeHalfWidth(c.Width, actorR, inc);
                var b = c.GetBounds();
                float y = c.Team == Team.Player ? b.yMin - 0.55f : b.yMax + 0.55f;
                string txt = $"{c.HeightClass} w{c.Width:0.00} " + (h >= 0f ? $"안전±{h:0.00}" : "안전슬롯 없음");
                Label(cam, new Vector2(c.Position.x, y), txt, h >= 0f ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.55f, 0.5f), 230f * k);
            }
            foreach (var ch in _combat.Characters)
            {
                if (ch == null || ch.OverallState == CharacterOverallState.Eliminated) continue;
                string st = ActorCoverStatus(ch, _combat, out _);
                if (st.Length == 0) continue;
                Label(cam, ch.Position + new Vector2(0f, -0.75f), st == "보호" ? "보호 보장" : "숨음(보장X)", st == "보호" ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.9f, 0.3f), 140f * k);
            }
            var r = new Rect(12f * k, Screen.height - 64f * k, 1100f * k, 30f * k);
            GUI.color = new Color(0, 0, 0, 0.55f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white;
            _style.alignment = TextAnchor.MiddleLeft;
            GUI.Label(r, $"  엄폐 디버그(F4) · 흰=판정 폭 · 초록=안전 중심(폭/2-(액터 {actorR:0.00}+탄 반경)) · 빨강X=보장 슬롯 없음 · layout v{TerrainRules.Data.layoutVersion} (제안 A)", _style);
            _style.alignment = TextAnchor.MiddleCenter;
        }

        private void Label(Camera cam, Vector2 logic, string text, Color col, float w)
        {
            Vector3 sp = cam.WorldToScreenPoint(FieldView.ToWorld(logic));
            float h = _style.fontSize + 8f;
            var rr = new Rect(sp.x - w * 0.5f, Screen.height - sp.y - h * 0.5f, w, h);
            GUI.color = new Color(0, 0, 0, 0.55f); GUI.DrawTexture(rr, Texture2D.whiteTexture);
            GUI.color = col; GUI.Label(rr, text, _style); GUI.color = Color.white;
        }

        // ---------------------------------------------------------------- segments (perf pass: ONE GL quad batch, was 1 LineRenderer = 1 draw each)
        private struct Seg { public Vector3 a, b; public Color c; public float w; }
        private readonly List<Seg> _segs = new List<Seg>(256);

        private void Add(Vector3 a, Vector3 b, Color c, float w) => _segs.Add(new Seg { a = a, b = b, c = c, w = w });

        private void OnRenderObject()
        {
            if (_segs.Count == 0 || Camera.current == null || Camera.current != Camera.main) return;
            if (_mat == null) _mat = new Material(Shader.Find("Sprites/Default")) { name = "CoverDebug_shared" };
            _mat.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.QUADS);
            foreach (var s in _segs)
            {
                Vector3 d = s.b - s.a; d.z = 0f;
                Vector3 n = d.sqrMagnitude > 1e-8f ? new Vector3(-d.y, d.x, 0f).normalized * (s.w * 0.5f) : Vector3.zero;
                GL.Color(s.c);
                GL.Vertex(s.a - n); GL.Vertex(s.a + n); GL.Vertex(s.b + n); GL.Vertex(s.b - n);
            }
            GL.End();
            GL.PopMatrix();
        }

        private void Line(Vector2 a, Vector2 b, Color c, float w) => Add(FieldView.ToWorld(a), FieldView.ToWorld(b), c, w);

        private void Rect(Rect r, Color c, float w)
        {
            Vector3 p0 = FieldView.ToWorld(new Vector2(r.xMin, r.yMin)), p1 = FieldView.ToWorld(new Vector2(r.xMax, r.yMin));
            Vector3 p2 = FieldView.ToWorld(new Vector2(r.xMax, r.yMax)), p3 = FieldView.ToWorld(new Vector2(r.xMin, r.yMax));
            Add(p0, p1, c, w); Add(p1, p2, c, w); Add(p2, p3, c, w); Add(p3, p0, c, w);
        }

        private void Circle(Vector2 center, float radius, Color c, float w)
        {
            const int n = 24;
            Vector3 prev = FieldView.ToWorld(center + new Vector2(radius, 0f));
            for (int i = 1; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                Vector3 cur = FieldView.ToWorld(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                Add(prev, cur, c, w); prev = cur;
            }
        }
    }
}
