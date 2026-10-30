using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Battlefield
{
    public class BattlefieldRenderer : MonoBehaviour
    {
        private BattlefieldSystem _battlefield;
        private BattlefieldData _data;
        private Material _mat;

        private Color _skyTop = new Color(0.4f, 0.6f, 0.85f);
        private Color _skyBottom = new Color(0.9f, 0.75f, 0.5f);
        private Color _hillColor = new Color(0.45f, 0.35f, 0.25f);
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
            CreateMaterial();
        }

        private void CreateMaterial()
        {
            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            
            _mat = new Material(shader);
            _mat.hideFlags = HideFlags.HideAndDontSave;
            _mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _mat.SetInt("_ZWrite", 0);
        }

        private void OnRenderObject()
        {
            if (_battlefield == null || _data == null) return;
            if (_mat == null) CreateMaterial();

            _mat.SetPass(0);
            GL.PushMatrix();

            DrawSky();
            DrawHills();
            DrawGround();
            DrawLanes();
            DrawGroundTexture();
            DrawFences();

            GL.PopMatrix();
        }

        private void DrawSky()
        {
            GL.Begin(GL.QUADS);

            float skyTop = 12f;
            float horizon = 4f;
            
            Vector3 bl = new Vector3(-15f, horizon, 1f);
            Vector3 br = new Vector3(15f, horizon, 1f);
            Vector3 tr = new Vector3(15f, skyTop, 1f);
            Vector3 tl = new Vector3(-15f, skyTop, 1f);

            GL.Color(_skyBottom);
            GL.Vertex(bl);
            GL.Vertex(br);
            GL.Color(_skyTop);
            GL.Vertex(tr);
            GL.Vertex(tl);

            GL.End();
        }

        private void DrawHills()
        {
            GL.Begin(GL.TRIANGLES);
            GL.Color(_hillColor);

            DrawHill(-10f, 3.5f, 6f, 2.5f);
            DrawHill(-4f, 3.5f, 5f, 2f);
            DrawHill(2f, 3.5f, 7f, 3f);
            DrawHill(9f, 3.5f, 5f, 2.2f);

            Color darkerHill = _hillColor * 0.8f;
            darkerHill.a = 1f;
            GL.Color(darkerHill);
            DrawHill(-7f, 3f, 4f, 1.5f);
            DrawHill(5f, 3f, 4.5f, 1.8f);
            DrawHill(12f, 3f, 4f, 1.5f);

            GL.End();
        }

        private void DrawHill(float x, float baseY, float width, float height)
        {
            int segments = 8;
            for (int i = 0; i < segments; i++)
            {
                float t1 = i / (float)segments;
                float t2 = (i + 1) / (float)segments;
                
                float x1 = x - width / 2f + t1 * width;
                float x2 = x - width / 2f + t2 * width;
                float y1 = baseY + Mathf.Sin(t1 * Mathf.PI) * height;
                float y2 = baseY + Mathf.Sin(t2 * Mathf.PI) * height;

                GL.Vertex(new Vector3(x1, baseY, 0.9f));
                GL.Vertex(new Vector3(x2, baseY, 0.9f));
                GL.Vertex(new Vector3(x2, y2, 0.9f));

                GL.Vertex(new Vector3(x1, baseY, 0.9f));
                GL.Vertex(new Vector3(x2, y2, 0.9f));
                GL.Vertex(new Vector3(x1, y1, 0.9f));
            }
        }

        private void DrawGround()
        {
            GL.Begin(GL.QUADS);

            Vector3 bl = _battlefield.LogicToWorldPosition(new Vector2(0, 0));
            Vector3 br = _battlefield.LogicToWorldPosition(new Vector2(_data.size.x, 0));
            Vector3 tr = _battlefield.LogicToWorldPosition(new Vector2(_data.size.x, _data.size.y));
            Vector3 tl = _battlefield.LogicToWorldPosition(new Vector2(0, _data.size.y));

            GL.Color(_groundColor);
            GL.Vertex(bl);
            GL.Vertex(br);
            GL.Color(_groundDark);
            GL.Vertex(tr);
            GL.Vertex(tl);

            GL.End();
        }

        private void DrawLanes()
        {
            float laneHeight = 1.2f;

            GL.Begin(GL.QUADS);

            foreach (float rowY in _data.ourRowsY)
            {
                GL.Color(_playerLane);
                DrawLaneQuad(rowY - laneHeight * 0.5f, rowY + laneHeight * 0.5f);
            }

            foreach (float rowY in _data.enemyRowsY)
            {
                GL.Color(_enemyLane);
                DrawLaneQuad(rowY - laneHeight * 0.5f, rowY + laneHeight * 0.5f);
            }

            GL.End();

            GL.Begin(GL.LINES);
            GL.Color(_laneLineColor);

            foreach (float rowY in _data.ourRowsY)
            {
                DrawDashedLine(rowY);
            }

            foreach (float rowY in _data.enemyRowsY)
            {
                DrawDashedLine(rowY);
            }

            GL.End();
        }

        private void DrawLaneQuad(float yMin, float yMax)
        {
            Vector3 bl = _battlefield.LogicToWorldPosition(new Vector2(0, yMin));
            Vector3 br = _battlefield.LogicToWorldPosition(new Vector2(_data.size.x, yMin));
            Vector3 tr = _battlefield.LogicToWorldPosition(new Vector2(_data.size.x, yMax));
            Vector3 tl = _battlefield.LogicToWorldPosition(new Vector2(0, yMax));

            GL.Vertex(bl);
            GL.Vertex(br);
            GL.Vertex(tr);
            GL.Vertex(tl);
        }

        private void DrawDashedLine(float y)
        {
            float dashLen = 1.5f;
            float gapLen = 0.8f;
            float x = 0;
            while (x < _data.size.x)
            {
                Vector3 start = _battlefield.LogicToWorldPosition(new Vector2(x, y));
                Vector3 end = _battlefield.LogicToWorldPosition(new Vector2(Mathf.Min(x + dashLen, _data.size.x), y));
                GL.Vertex(start);
                GL.Vertex(end);
                x += dashLen + gapLen;
            }
        }

        private void DrawGroundTexture()
        {
            GL.Begin(GL.LINES);
            GL.Color(new Color(0.5f, 0.4f, 0.3f, 0.15f));

            for (float x = 0; x < _data.size.x; x += 2f)
            {
                Vector3 bottom = _battlefield.LogicToWorldPosition(new Vector2(x, 0));
                Vector3 top = _battlefield.LogicToWorldPosition(new Vector2(x + 0.5f, _data.size.y));
                GL.Vertex(bottom);
                GL.Vertex(top);
            }

            GL.End();
        }

        private void DrawFences()
        {
            GL.Begin(GL.QUADS);
            GL.Color(_fenceColor);

            float postWidth = 0.15f;
            float postHeight = 0.8f;
            float railHeight = 0.08f;

            for (float x = _data.boundaryMinX; x <= _data.boundaryMaxX; x += 3f)
            {
                DrawFencePost(x, 0, postWidth, postHeight);
                DrawFencePost(x, _data.size.y, postWidth, postHeight);
            }

            Vector3 leftBottom = _battlefield.LogicToWorldPosition(new Vector2(_data.boundaryMinX, 0));
            Vector3 rightBottom = _battlefield.LogicToWorldPosition(new Vector2(_data.boundaryMaxX, 0));
            DrawFenceRail(leftBottom, rightBottom, railHeight, 0.3f);
            DrawFenceRail(leftBottom, rightBottom, railHeight, 0.6f);

            Vector3 leftTop = _battlefield.LogicToWorldPosition(new Vector2(_data.boundaryMinX, _data.size.y));
            Vector3 rightTop = _battlefield.LogicToWorldPosition(new Vector2(_data.boundaryMaxX, _data.size.y));
            DrawFenceRail(leftTop, rightTop, railHeight, 0.3f);
            DrawFenceRail(leftTop, rightTop, railHeight, 0.6f);

            GL.End();
        }

        private void DrawFencePost(float x, float y, float width, float height)
        {
            Vector3 pos = _battlefield.LogicToWorldPosition(new Vector2(x, y));
            float hw = width * 0.5f;
            
            GL.Vertex(pos + new Vector3(-hw, 0, 0));
            GL.Vertex(pos + new Vector3(hw, 0, 0));
            GL.Vertex(pos + new Vector3(hw, height, 0));
            GL.Vertex(pos + new Vector3(-hw, height, 0));
        }

        private void DrawFenceRail(Vector3 left, Vector3 right, float height, float yOffset)
        {
            float hh = height * 0.5f;
            Vector3 offset = new Vector3(0, yOffset, 0);

            GL.Vertex(left + offset + new Vector3(0, -hh, 0));
            GL.Vertex(right + offset + new Vector3(0, -hh, 0));
            GL.Vertex(right + offset + new Vector3(0, hh, 0));
            GL.Vertex(left + offset + new Vector3(0, hh, 0));
        }

        private void OnDestroy()
        {
            if (_mat != null) DestroyImmediate(_mat);
        }
    }
}
