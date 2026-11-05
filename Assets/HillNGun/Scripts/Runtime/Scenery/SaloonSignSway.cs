using UnityEngine;
using HillNGun.Battlefield;

namespace HillNGun.Scenery
{
    /// <summary>Task I2: saloon sign sway. Visual occlusion only — no hitbox change.</summary>
    public class SaloonSignSway : MonoBehaviour
    {
        public float swayDeg = 8f;
        public float period = 2.4f;
        private float _t;
        private Vector3 _base;

        public void Place(Vector2 logicPos)
        {
            FieldView.Place(transform, logicPos, 1.1f);
            _base = transform.localEulerAngles;
            var sr = gameObject.GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            if (sr.sprite == null) sr.sprite = MakeSignSprite();
            sr.sortingOrder = FieldView.DepthOrder(logicPos.y) - 2;
            sr.color = new Color(0.45f, 0.28f, 0.16f, 0.95f);
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float a = Mathf.Sin(_t * (Mathf.PI * 2f / period)) * swayDeg;
            transform.localEulerAngles = _base + new Vector3(0f, 0f, a);
        }

        private static Sprite _spr;
        private static Sprite MakeSignSprite()
        {
            if (_spr != null) return _spr;
            int w = 48, h = 28;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool edge = x < 2 || y < 2 || x >= w - 2 || y >= h - 2;
                tex.SetPixel(x, y, edge ? new Color(0.2f, 0.12f, 0.08f, 1f) : new Color(0.55f, 0.35f, 0.2f, 1f));
            }
            tex.Apply();
            _spr = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), 32f);
            return _spr;
        }
    }
}
