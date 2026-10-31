using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Battlefield
{
    /// <summary>
    /// Draws the field background as a static vertex-coloured mesh rendered in the sprite
    /// sorting pipeline (sortingOrder = FieldView.BackgroundOrder), so every actor sprite
    /// draws on top of it. (Previously GL in OnRenderObject painted over all sprites.)
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

        private void Start()
        {
            _battlefield = ServiceLocator.Get<BattlefieldSystem>();
            _data = _battlefield?.Data;
            Build();
        }

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
        }
    }
}
