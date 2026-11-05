using UnityEngine;
using HillNGun.Data;
using HillNGun.Battlefield;

namespace HillNGun.Scenery
{
    /// <summary>
    /// Task I: data-driven L0–L4 parallax using StageVisualData sprites (concept PNGs as temp BG).
    /// Works with a single filled layer. L3 combatLane never receives decor children.
    /// </summary>
    public class ParallaxBackdrop : MonoBehaviour
    {
        private StageVisualData _visual;
        private readonly Transform[] _layerRoots = new Transform[5];
        private readonly SpriteRenderer[] _layerSr = new SpriteRenderer[5];
        private readonly Vector3[] _basePos = new Vector3[5];
        private SpriteRenderer _gradeOverlay;
        private Transform _lanternRoot;
        private Camera _cam;
        private Vector3 _camOrigin;

        public StageVisualData Visual => _visual;

        public void Apply(StageVisualData visual)
        {
            _visual = visual;
            StageVisualData.EnsureFiveLayers(visual);
            EnsureRoots();
            if (visual == null)
            {
                for (int i = 0; i < 5; i++) if (_layerSr[i] != null) _layerSr[i].enabled = false;
                if (_gradeOverlay != null) _gradeOverlay.enabled = false;
                ClearLanterns();
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                var layer = visual.layers[i];
                var sr = _layerSr[i];
                if (sr == null) continue;
                if (layer == null || layer.sprite == null)
                {
                    sr.enabled = false;
                    continue;
                }
                sr.enabled = true;
                sr.sprite = layer.sprite;
                sr.color = layer.tint;
                sr.sortingOrder = layer.sortingOrder;
                // Fit sprite to camera orthographic height roughly
                FitToCamera(sr);
                // Dim concept-art cover clutter slightly on town/far layers (not combat lane)
                if (!layer.combatLane && visual.conceptCoverDim > 0.01f && i <= 2)
                {
                    var c = sr.color;
                    c.a = Mathf.Clamp01(c.a * (1f - visual.conceptCoverDim * 0.25f));
                    sr.color = c;
                }
                _basePos[i] = _layerRoots[i].localPosition;
            }

            if (_gradeOverlay != null)
            {
                bool grade = visual.gradeTint != Color.white || Mathf.Abs(visual.gradeExposure - 1f) > 0.01f;
                _gradeOverlay.enabled = grade;
                if (grade)
                {
                    var c = visual.gradeTint;
                    c.a = Mathf.Clamp01(1f - visual.gradeExposure * 0.5f) * 0.35f;
                    // exposure via multiply: use overlay only for dusk/night darkening
                    if (visual.timeOfDay == TimeOfDay.Sunset)
                        _gradeOverlay.color = new Color(1f, 0.45f, 0.2f, 0.18f);
                    else if (visual.timeOfDay == TimeOfDay.Night)
                        _gradeOverlay.color = new Color(0.05f, 0.08f, 0.22f, 0.42f);
                    else if (visual.timeOfDay == TimeOfDay.Afternoon)
                        _gradeOverlay.color = new Color(1f, 0.85f, 0.55f, 0.10f);
                    else
                        _gradeOverlay.color = new Color(c.r, c.g, c.b, 0.05f);
                    _gradeOverlay.sortingOrder = 50;
                }
            }

            BuildLanterns(visual);
            // Hard guard: strip any decor mistakenly parented under L3
            EnforceNoDecorOnL3();
        }

        public static void EnforceNoDecorOnLayer(Transform layerRoot, int layerIndex)
        {
            if (layerRoot == null || StageVisualData.AllowsDecorOnLayer(layerIndex)) return;
            for (int i = layerRoot.childCount - 1; i >= 0; i--)
            {
                var ch = layerRoot.GetChild(i);
                if (ch != null && ch.name.StartsWith("Decor_"))
                    Object.Destroy(ch.gameObject);
            }
        }

        public void EnforceNoDecorOnL3()
        {
            if (_layerRoots[3] != null) EnforceNoDecorOnLayer(_layerRoots[3], 3);
        }

        /// <summary>Spawn a decor prop; returns null if layer is combat lane (L3).</summary>
        public GameObject TrySpawnDecor(int layerIndex, string name, Vector3 localPos)
        {
            if (!StageVisualData.AllowsDecorOnLayer(layerIndex)) return null;
            if (_layerRoots[layerIndex] == null) return null;
            var go = new GameObject("Decor_" + name);
            go.transform.SetParent(_layerRoots[layerIndex], false);
            go.transform.localPosition = localPos;
            return go;
        }

        private void EnsureRoots()
        {
            if (_layerRoots[0] != null) return;
            for (int i = 0; i < 5; i++)
            {
                var go = new GameObject("Parallax_L" + i);
                go.transform.SetParent(transform, false);
                _layerRoots[i] = go.transform;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.drawMode = SpriteDrawMode.Simple;
                sr.enabled = false;
                _layerSr[i] = sr;
            }
            var grade = new GameObject("GradeOverlay");
            grade.transform.SetParent(transform, false);
            _gradeOverlay = grade.AddComponent<SpriteRenderer>();
            _gradeOverlay.sprite = WhiteSprite();
            _gradeOverlay.enabled = false;
            _lanternRoot = new GameObject("Lanterns").transform;
            _lanternRoot.SetParent(transform, false);
        }

        private void FitToCamera(SpriteRenderer sr)
        {
            _cam = Camera.main;
            if (_cam == null || sr.sprite == null) return;
            float worldH = _cam.orthographicSize * 2f;
            float worldW = worldH * _cam.aspect;
            var size = sr.sprite.bounds.size;
            if (size.y < 0.01f) return;
            float scale = Mathf.Max(worldW / size.x, worldH / size.y) * 1.02f;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            sr.transform.localPosition = Vector3.zero;
        }

        private void BuildLanterns(StageVisualData visual)
        {
            ClearLanterns();
            if (visual?.lanternPositions == null || visual.lanternPositions.Length == 0) return;
            if (visual.timeOfDay != TimeOfDay.Sunset && visual.timeOfDay != TimeOfDay.Night) return;
            foreach (var lp in visual.lanternPositions)
            {
                var go = new GameObject("Lantern");
                go.transform.SetParent(_lanternRoot, false);
                var w = FieldView.ToWorld(lp);
                go.transform.position = new Vector3(w.x, w.y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SoftGlowSprite();
                sr.color = visual.lanternColor;
                sr.sortingOrder = 5;
                go.transform.localScale = Vector3.one * (visual.timeOfDay == TimeOfDay.Night ? 2.2f : 1.4f);
            }
        }

        private void ClearLanterns()
        {
            if (_lanternRoot == null) return;
            for (int i = _lanternRoot.childCount - 1; i >= 0; i--)
                Destroy(_lanternRoot.GetChild(i).gameObject);
        }

        private void LateUpdate()
        {
            if (_visual == null) return;
            _cam = Camera.main;
            if (_cam == null) return;
            if (_camOrigin == Vector3.zero) _camOrigin = _cam.transform.position;
            Vector3 delta = _cam.transform.position - _camOrigin;
            for (int i = 0; i < 5; i++)
            {
                if (_layerRoots[i] == null || _layerSr[i] == null || !_layerSr[i].enabled) continue;
                float p = _visual.layers[i] != null ? _visual.layers[i].parallax : 0f;
                // combat lane locked
                if (_visual.layers[i] != null && _visual.layers[i].combatLane) p = 0f;
                _layerRoots[i].localPosition = _basePos[i] + new Vector3(delta.x * p, delta.y * p * 0.35f, 0f);
            }
            if (_gradeOverlay != null && _gradeOverlay.enabled)
            {
                FitToCamera(_gradeOverlay);
                _gradeOverlay.transform.position = new Vector3(_cam.transform.position.x, _cam.transform.position.y, 0f);
            }
        }

        private static Sprite _white;
        private static Sprite WhiteSprite()
        {
            if (_white != null) return _white;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            t.Apply();
            _white = Sprite.Create(t, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
            return _white;
        }

        private static Sprite _glow;
        private static Sprite SoftGlowSprite()
        {
            if (_glow != null) return _glow;
            int s = 64;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var cx = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx)) / cx;
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            t.Apply();
            _glow = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _glow;
        }
    }
}
