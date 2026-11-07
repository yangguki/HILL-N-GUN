using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Scenery;

namespace HillNGun.Battlefield
{
    /// <summary>
    /// Field background (view only). Resolves the stage theme (PvE stage -> StageData.themeId, PvP -> library pvp
    /// theme), bakes the procedural Western backdrop for that theme ONCE into a texture (BackdropBaker) and shows it
    /// on a single quad at FieldView.BackgroundOrder = 1 draw call. Re-bakes only when the theme or the screen aspect
    /// changes. Ambient life (rolling tumbleweeds, drifting dust) is a separate batched layer (AmbientFx).
    /// Without a theme library it falls back to the previous flat vertex-colour mesh (BuildFlat).
    /// </summary>
    public class BattlefieldRenderer : MonoBehaviour
    {
        private BattlefieldSystem _battlefield;
        private BattlefieldData _data;
        private Material _mat;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private readonly List<Vector3> _verts = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<int> _tris = new List<int>();

        private Color _skyTop = new Color(0.4f, 0.6f, 0.85f);
        private Color _skyBottom = new Color(0.9f, 0.75f, 0.5f);
        private Color _hillColor = new Color(0.45f, 0.35f, 0.25f);
        private Color _outerGround = new Color(0.42f, 0.32f, 0.22f);
        private Color _groundColor = new Color(0.7f, 0.55f, 0.35f);
        private Color _groundDark = new Color(0.55f, 0.4f, 0.25f);
        private Color _playerLane = new Color(0.5f, 0.65f, 0.45f, 0.25f);
        private Color _enemyLane = new Color(0.65f, 0.5f, 0.45f, 0.25f);
        private Color _laneLineColor = new Color(0.4f, 0.3f, 0.2f, 0.5f);
        private Color _fenceColor = new Color(0.5f, 0.35f, 0.2f);

        // themed backdrop
        private StageThemeData _theme;
        private Texture2D _bakedTex;
        private Material _bakedMat;
        private Mesh _quadMesh;
        private float _bakedAspect;
        private int _bakedPxH;
        private AmbientFx _ambient;
        private ParallaxBackdrop _parallax;
        private HillNGun.PvE.PvEMatchManager _pve;
        private bool _pveSearched;
        private string _appliedVisualStageId;

        /// <summary>Last bake time in ms (perf log).</summary>
        public static float LastBakeMs { get; private set; }

        private void Start()
        {
            _battlefield = ServiceLocator.Get<BattlefieldSystem>();
            _data = _battlefield?.Data;
            var theme = ResolveTheme();
            if (theme != null) ApplyTheme(theme);
            else Build();
        }

        private StageThemeData ResolveTheme()
        {
            var lib = ThemeContext.Library;
            if (lib == null) return null;
            if (!_pveSearched) { _pve = FindAnyObjectByType<HillNGun.PvE.PvEMatchManager>(); _pveSearched = true; }
            if (_pve != null && _pve.CurrentStage != null) return lib.Get(_pve.CurrentStage.themeId);
            if (_pve == null && FindAnyObjectByType<HillNGun.PvP.PvPMatchManager>() != null) return lib.Get(lib.pvpThemeId);
            return _pve != null ? lib.Get(lib.defaultThemeId) : lib.Get(lib.pvpThemeId);
        }

        private void LateUpdate()
        {
            if (!_pveSearched) { _pve = FindAnyObjectByType<HillNGun.PvE.PvEMatchManager>(); _pveSearched = true; }
            // Theme may stay "town_street" across 1-1..Rex — still must (re)apply StageVisual when stage starts/changes.
            // Same key ResolveAndApplyVisual stores (PvP = "pvp_town_street"). Perf pass fix: the old null key never matched in
            // PvP, so the stage visual (parallax, TOD grade, ambient reconfigure + a log line) was re-applied EVERY frame.
            string stageId = StageVisualKey();
            var want = (_pve != null && _pve.CurrentStage != null) ? ThemeContext.Library?.Get(_pve.CurrentStage.themeId) : _theme;
            if (want != null && want != _theme)
            {
                ApplyTheme(want);
                return;
            }
            if (stageId != _appliedVisualStageId)
            {
                if (_theme == null && want != null) ApplyTheme(want);
                else ResolveAndApplyVisual();
                return;
            }
            if (_theme == null) return;
            var cam = Camera.main;
            float aspect = cam != null ? cam.aspect : _bakedAspect;
            if (Mathf.Abs(aspect - _bakedAspect) > 0.02f || Mathf.Abs(Screen.height - _bakedPxH) > 160)
            {
                Bake();
                // Keep concept BG on top after rebake
                ResolveAndApplyVisual();
            }
        }

        private void ApplyTheme(StageThemeData theme)
        {
            _theme = theme;
            ThemeContext.Set(theme);
            Bake();
            ResolveAndApplyVisual();
            if (ThemeContext.NoAmbient) return;
            if (_ambient == null)
            {
                var go = new GameObject("AmbientFx");
                go.transform.SetParent(transform, false);
                _ambient = go.AddComponent<AmbientFx>();
            }
            _ambient.Configure(theme, FieldMap.From(_data));
        }

        private void Bake()
        {
            var cam = Camera.main;
            if (cam == null || _theme == null) return;
            float t0 = Time.realtimeSinceStartup;
            var b = FieldView.CameraBounds(cam);
            var view = new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
            const float margin = 0.9f;
            var lib = ThemeContext.Library;
            int maxSize = lib != null ? lib.maxBakeSize : 2048;
            var tex = BackdropBaker.BakeField(_theme, FieldMap.From(_data), view, margin, maxSize, Mathf.Clamp(Screen.height, 360, 1440));
            if (tex == null) { if (_bakedTex == null) Build(); return; }
            if (_bakedTex != null) Destroy(_bakedTex);
            _bakedTex = tex;
            _bakedAspect = cam.aspect;
            _bakedPxH = Screen.height;

            _meshFilter = gameObject.GetComponent<MeshFilter>();
            if (_meshFilter == null) _meshFilter = gameObject.AddComponent<MeshFilter>();
            _meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (_meshRenderer == null) _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            if (_bakedMat == null) _bakedMat = new Material(Shader.Find("Sprites/Default")) { name = "BackdropBaked" };
            _bakedMat.mainTexture = tex;
            _meshRenderer.sharedMaterial = _bakedMat;
            _meshRenderer.sortingOrder = FieldView.BackgroundOrder;
            _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            float x0 = view.xMin - margin, x1 = view.xMax + margin, y0 = view.yMin - margin, y1 = view.yMax + margin;
            if (_quadMesh == null) _quadMesh = new Mesh { name = "BackdropQuad" };
            _quadMesh.Clear();
            _quadMesh.SetVertices(new List<Vector3> { new Vector3(x0, y0, 1f), new Vector3(x1, y0, 1f), new Vector3(x1, y1, 1f), new Vector3(x0, y1, 1f) });
            _quadMesh.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            _quadMesh.SetColors(new List<Color> { Color.white, Color.white, Color.white, Color.white });
            _quadMesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            _quadMesh.RecalculateBounds();
            _meshFilter.sharedMesh = _quadMesh;
            LastBakeMs = (Time.realtimeSinceStartup - t0) * 1000f;
            Debug.Log($"[Backdrop] theme={_theme.id} baked {tex.width}x{tex.height} in {LastBakeMs:F0} ms");
        }

        /// <summary>Fallback: flat vertex-colour field (no theme library).</summary>
        private void Build()
        {
            if (_battlefield == null || _data == null) return;

            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");
            _mat = new Material(shader);

            _meshFilter = gameObject.GetComponent<MeshFilter>();
            if (_meshFilter == null) _meshFilter = gameObject.AddComponent<MeshFilter>();
            _meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (_meshRenderer == null) _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.sharedMaterial = _mat;
            _meshRenderer.sortingOrder = FieldView.BackgroundOrder;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            _verts.Clear(); _colors.Clear(); _tris.Clear();

            Vector3 fieldTopLeft = W(0, _data.size.y);
            float horizon = fieldTopLeft.y;

            // Outer ground fills the whole screen width below the horizon
            Quad(new Vector3(-40f, -30f), new Vector3(40f, -30f), new Vector3(40f, horizon), new Vector3(-40f, horizon), _outerGround, _outerGround);
            // Sky above the horizon
            Quad(new Vector3(-40f, horizon), new Vector3(40f, horizon), new Vector3(40f, horizon + 20f), new Vector3(-40f, horizon + 20f), _skyBottom, _skyTop);
            // Low hills just above the horizon (only ~1 world unit of sky is on camera)
            for (float x = -20f; x <= 20f; x += 5.5f) Hill(x, horizon, 6f, 0.6f + 0.25f * Mathf.Abs(Mathf.Sin(x)), _hillColor);

            // Field ground
            Quad(W(0, 0), W(_data.size.x, 0), W(_data.size.x, _data.size.y), W(0, _data.size.y), _groundColor, _groundDark);

            // Lanes
            const float laneHeight = 1.2f;
            foreach (float rowY in _data.ourRowsY) LaneQuad(rowY - laneHeight * 0.5f, rowY + laneHeight * 0.5f, _playerLane);
            foreach (float rowY in _data.enemyRowsY) LaneQuad(rowY - laneHeight * 0.5f, rowY + laneHeight * 0.5f, _enemyLane);
            foreach (float rowY in _data.ourRowsY) DashedLine(rowY);
            foreach (float rowY in _data.enemyRowsY) DashedLine(rowY);

            // Fences
            for (float x = _data.boundaryMinX; x <= _data.boundaryMaxX; x += 3f)
            {
                FencePost(x, 0f);
                FencePost(x, _data.size.y);
            }
            FenceRails(0f);
            FenceRails(_data.size.y);

            var mesh = new Mesh { name = "BattlefieldMesh" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(_verts);
            mesh.SetColors(_colors);
            mesh.SetTriangles(_tris, 0);
            mesh.RecalculateBounds();
            _meshFilter.sharedMesh = mesh;
        }

        private Vector3 W(float x, float y) => _battlefield.LogicToWorldPosition(new Vector2(x, y));

        private void Quad(Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl, Color bottom, Color top)
        {
            int i = _verts.Count;
            _verts.Add(bl); _verts.Add(br); _verts.Add(tr); _verts.Add(tl);
            _colors.Add(bottom); _colors.Add(bottom); _colors.Add(top); _colors.Add(top);
            _tris.Add(i); _tris.Add(i + 2); _tris.Add(i + 1);
            _tris.Add(i); _tris.Add(i + 3); _tris.Add(i + 2);
        }

        private void Hill(float x, float baseY, float width, float height, Color c)
        {
            const int segments = 10;
            for (int s = 0; s < segments; s++)
            {
                float t1 = s / (float)segments, t2 = (s + 1) / (float)segments;
                float x1 = x - width / 2f + t1 * width, x2 = x - width / 2f + t2 * width;
                float y1 = baseY + Mathf.Sin(t1 * Mathf.PI) * height, y2 = baseY + Mathf.Sin(t2 * Mathf.PI) * height;
                Quad(new Vector3(x1, baseY), new Vector3(x2, baseY), new Vector3(x2, y2), new Vector3(x1, y1), c, c);
            }
        }

        private void LaneQuad(float yMin, float yMax, Color c)
        {
            Quad(W(0, yMin), W(_data.size.x, yMin), W(_data.size.x, yMax), W(0, yMax), c, c);
        }

        private void Line(Vector3 a, Vector3 b, float thickness, Color c)
        {
            Vector3 d = (b - a).normalized;
            Vector3 n = new Vector3(-d.y, d.x, 0f) * (thickness * 0.5f);
            Quad(a - n, b - n, b + n, a + n, c, c);
        }

        private void DashedLine(float y)
        {
            const float dashLen = 1.5f, gapLen = 0.8f;
            for (float x = 0; x < _data.size.x; x += dashLen + gapLen)
                Line(W(x, y), W(Mathf.Min(x + dashLen, _data.size.x), y), 0.06f, _laneLineColor);
        }

        private void FencePost(float x, float y)
        {
            Vector3 p = W(x, y);
            const float hw = 0.075f, h = 0.8f;
            Quad(p + new Vector3(-hw, 0), p + new Vector3(hw, 0), p + new Vector3(hw, h), p + new Vector3(-hw, h), _fenceColor, _fenceColor);
        }

        private void FenceRails(float y)
        {
            Vector3 l = W(_data.boundaryMinX, y), r = W(_data.boundaryMaxX, y);
            Line(l + new Vector3(0, 0.3f), r + new Vector3(0, 0.3f), 0.08f, _fenceColor);
            Line(l + new Vector3(0, 0.6f), r + new Vector3(0, 0.6f), 0.08f, _fenceColor);
        }

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
            if (_bakedMat != null) Destroy(_bakedMat);
            if (_bakedTex != null) Destroy(_bakedTex);
            if (_quadMesh != null) Destroy(_quadMesh);
        }
        public void RefreshStageVisual() => ResolveAndApplyVisual();

        private string StageVisualKey() => _pve != null && _pve.CurrentStage != null ? _pve.CurrentStage.id : (_pve == null ? "pvp_town_street" : null);

        /// <summary>Perf diagnostics: how many times the stage visual was (re)applied this session.</summary>
        public static int VisualApplyCount;

        public void ResolveAndApplyVisual()
        {
            if (!_pveSearched) { _pve = FindAnyObjectByType<HillNGun.PvE.PvEMatchManager>(); _pveSearched = true; }
            VisualApplyCount++;
            StageVisualData visual = null;
            if (_pve != null && _pve.CurrentStage != null) visual = _pve.CurrentStage.visual;
            // Temporary: PvP (and any no-stage town_street) uses the empty noon plate until dedicated plates arrive.
            if (visual == null)
            {
                var am = HillNGun.Core.RuntimeAssetManager.Instance;
                if (am != null && am.StageDatas != null)
                {
                    for (int i = 0; i < am.StageDatas.Length; i++)
                    {
                        var s = am.StageDatas[i];
                        if (s != null && s.id == "dusty_town_1_1" && s.visual != null) { visual = s.visual; break; }
                    }
                }
            }
            _appliedVisualStageId = StageVisualKey();

            if (_parallax == null)
            {
                var go = new GameObject("ParallaxBackdrop");
                go.transform.SetParent(transform, false);
                _parallax = go.AddComponent<ParallaxBackdrop>();
            }
            _parallax.Apply(visual);
            HillNGun.Scenery.TimeOfDayGrade.Set(visual);

            // When Task I concept layers are present, hide the procedural bake so tablets
            // cannot keep showing the old Western scenery underneath / instead.
            bool hasConcept = false;
            if (visual != null && visual.layers != null)
            {
                for (int i = 0; i < visual.layers.Length; i++)
                    if (visual.layers[i] != null && visual.layers[i].sprite != null) { hasConcept = true; break; }
            }
            if (_meshRenderer != null) _meshRenderer.enabled = !hasConcept;

            if (_ambient != null && visual != null)
            {
                if (_theme != null)
                {
                    _theme.dustMotes = visual.dustMotes;
                    _theme.tumbleweeds = visual.tumbleweeds;
                }
                bool low = System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-vfxlow" || a == "-lowfx");
                if (low && _theme != null) _theme.dustMotes = visual.dustMotesLowTier;
                _ambient.Configure(_theme, FieldMap.From(_data));
                _ambient.BanCombatLaneDecor = true;
            }
            _parallax.EnforceNoDecorOnL3();
            if (hasConcept)
                Debug.Log($"[Backdrop] StageVisual applied stage={_appliedVisualStageId} timeOfDay={visual.timeOfDay} (procedural bake hidden)");
        }

    }
}
