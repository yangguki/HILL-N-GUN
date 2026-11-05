using UnityEngine;
using HillNGun.Battlefield;

namespace HillNGun.PvE
{
    /// <summary>1-3 train stub (06): solid scrolling bar + spawn block. Damage 0.</summary>
    public class TrainPassView : MonoBehaviour
    {
        public static TrainPassView Instance { get; private set; }
        private float _t, _dur;
        private bool _active;
        private static Material _mat;

        public static TrainPassView Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("[TrainPassView]");
            return go.AddComponent<TrainPassView>();
        }

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Show(float duration)
        {
            _dur = Mathf.Max(0.5f, duration);
            _t = 0f;
            _active = true;
        }

        private void Update()
        {
            if (!_active) return;
            _t += Time.deltaTime;
            if (_t >= _dur) _active = false;
        }

        private void OnGUI()
        {
            if (!_active) return;
            float u = _t / _dur;
            // solid bar across the top of the playfield (screen space approximation)
            float y = Screen.height * (0.12f + 0.08f * Mathf.Sin(u * Mathf.PI));
            float h = Screen.height * 0.06f;
            float x = -Screen.width * 0.2f + u * Screen.width * 1.4f;
            var r = new Rect(x, y, Screen.width * 0.55f, h);
            Color c = new Color(0.25f, 0.22f, 0.2f, 0.85f);
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(0.9f, 0.75f, 0.35f, 0.9f);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 4f, r.width, 4f), Texture2D.whiteTexture); // dust line
            GUI.color = Color.white;
        }
    }
}
