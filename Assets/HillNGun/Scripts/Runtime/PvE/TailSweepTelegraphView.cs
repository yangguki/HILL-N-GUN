using UnityEngine;
using HillNGun.Battlefield;

namespace HillNGun.PvE
{
    /// <summary>
    /// Floor warning for the boss tail sweep: a pulsing orange zone over the locked hit area
    /// during the warning, then a short sand-coloured swipe flash when it resolves. No blood.
    /// </summary>
    public class TailSweepTelegraphView : MonoBehaviour
    {
        private BossInstance _boss;
        private MeshFilter _filter;
        private MeshRenderer _renderer;
        private Mesh _mesh;
        private Material _mat;
        private float _flashTimer;
        private const float FlashDuration = 0.35f;
        private bool _wasWarning;

        public bool IsShowing => _renderer != null && _renderer.enabled;

        public void Initialize(BossInstance boss)
        {
            _boss = boss;
            _filter = gameObject.AddComponent<MeshFilter>();
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _mat = new Material(Shader.Find("Sprites/Default"));
            _renderer.sharedMaterial = _mat;
            _renderer.sortingOrder = FieldView.BackgroundOrder + 50; // above field, below actors
            _mesh = new Mesh { name = "TailSweepZone" };
            _filter.sharedMesh = _mesh;
            _renderer.enabled = false;
            transform.position = Vector3.zero;
            _boss.OnTailSweep += HandleSweep;
        }

        private void HandleSweep(BossInstance boss, float width)
        {
            Build(boss.TailSweepArea);
            _flashTimer = FlashDuration;
        }

        private void Update()
        {
            if (_boss == null) return;
            bool warning = _boss.CurrentWarning == BossInstance.WarningType.TailSweep;

            if (warning)
            {
                if (!_wasWarning) Build(_boss.TailSweepArea);
                float pulse = 0.35f + 0.25f * Mathf.Sin(Time.time * 18f);
                SetColor(new Color(1f, 0.45f, 0.1f, pulse));
                _renderer.enabled = true;
            }
            else if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                float a = Mathf.Clamp01(_flashTimer / FlashDuration) * 0.8f;
                SetColor(new Color(0.95f, 0.85f, 0.6f, a));
                _renderer.enabled = true;
            }
            else
            {
                _renderer.enabled = false;
            }
            _wasWarning = warning;
        }

        private void Build(Rect area)
        {
            Vector3 bl = FieldView.ToWorld(new Vector2(area.xMin, area.yMin));
            Vector3 br = FieldView.ToWorld(new Vector2(area.xMax, area.yMin));
            Vector3 tr = FieldView.ToWorld(new Vector2(area.xMax, area.yMax));
            Vector3 tl = FieldView.ToWorld(new Vector2(area.xMin, area.yMax));
            _mesh.Clear();
            _mesh.vertices = new[] { bl, br, tr, tl };
            _mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            _mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _mesh.RecalculateBounds();
        }

        private void SetColor(Color c)
        {
            if (_mesh == null || _mesh.vertexCount != 4) return;
            _mesh.colors = new[] { c, c, c, c };
        }

        private void OnDestroy()
        {
            if (_boss != null) _boss.OnTailSweep -= HandleSweep;
            if (_mat != null) Destroy(_mat);
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
