using UnityEngine;
using HillNGun.Combat;
using HillNGun.Battlefield;
using HillNGun.Feel;

namespace HillNGun.Scenery
{
    /// <summary>Task I2: cracked blink → explode VFX (stars/dust/wood only, no blood).</summary>
    public class ExplosiveBarrelView : MonoBehaviour
    {
        private HazardExplosiveBarrel _barrel;
        private SpriteRenderer _sr;
        private float _blink;
        private static Sprite _spr;

        public void Bind(HazardExplosiveBarrel barrel)
        {
            _barrel = barrel;
            FieldView.Place(transform, barrel.Position, 1f);
            _sr = gameObject.GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = BarrelSprite();
            _sr.sortingOrder = FieldView.DepthOrder(barrel.Position.y) + 2;
            _barrel.OnExploded += HandleExploded;
        }

        private void OnDestroy()
        {
            if (_barrel != null) _barrel.OnExploded -= HandleExploded;
        }

        private void Update()
        {
            if (_barrel == null || _barrel.Exploded) return;
            if (_barrel.Cracked)
            {
                _blink += Time.deltaTime;
                float pulse = 0.5f + 0.5f * Mathf.Sin(_blink / 0.4f * Mathf.PI * 2f);
                _sr.color = Color.Lerp(new Color(0.55f, 0.35f, 0.15f), new Color(1f, 0.25f, 0.1f), pulse);
            }
            else
            {
                float t = _barrel.Accumulated / (float)HazardExplosiveBarrel.TriggerHp;
                _sr.color = Color.Lerp(new Color(0.5f, 0.32f, 0.14f), new Color(0.7f, 0.4f, 0.15f), t);
            }
        }

        private void HandleExploded(HazardExplosiveBarrel b)
        {
            EffectsManager.Instance?.SpawnExplosion(b.Position, HazardExplosiveBarrel.Radius);
            EffectsManager.Instance?.SpawnCoverDebris(b.Position, 1.4f, true);
            EffectsManager.Instance?.SpawnImpact(b.Position, false);
            if (_sr != null) _sr.enabled = false;
            Destroy(gameObject, 0.05f);
        }

        private static Sprite BarrelSprite()
        {
            if (_spr != null) return _spr;
            int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float nx = (x - 15.5f) / 10f, ny = (y - 15.5f) / 14f;
                bool inside = nx * nx + ny * ny < 1f;
                tex.SetPixel(x, y, inside ? new Color(0.5f, 0.32f, 0.14f, 1f) : Color.clear);
            }
            tex.Apply();
            _spr = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
            return _spr;
        }
    }
}
