using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Battlefield;

namespace HillNGun.Combat
{
    /// <summary>
    /// Renders active projectiles from CombatSystem's pool as pooled SpriteRenderers.
    /// View only: reads logic state each LateUpdate.
    /// </summary>
    public class ProjectileView : MonoBehaviour
    {
        private CombatSystem _combat;
        private readonly List<SpriteRenderer> _pool = new List<SpriteRenderer>();
        private Sprite _bullet;
        private Sprite _bulletHeavy;

        public static ProjectileView Ensure()
        {
            var existing = FindFirstObjectByType<ProjectileView>();
            if (existing != null) return existing;
            return new GameObject("[ProjectileView]").AddComponent<ProjectileView>();
        }

        private void Start()
        {
            var lib = ServiceLocator.Get<SpriteLibrary>();
            _bullet = lib != null && lib.bullet != null ? lib.bullet : CreateDot();
            _bulletHeavy = lib != null && lib.bulletHeavy != null ? lib.bulletHeavy : _bullet;
        }

        private void LateUpdate()
        {
            if (_combat == null) _combat = ServiceLocator.Get<CombatSystem>();
            var list = _combat?.ProjectilePool?.ActiveProjectiles;
            int used = 0;

            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var p = list[i];
                    if (!p.IsActive) continue;

                    var sr = Get(used++);
                    sr.sprite = p.Damage >= 500 ? _bulletHeavy : _bullet;
                    sr.color = p.OwnerTeam == Team.Player ? new Color(1f, 0.95f, 0.6f) : new Color(1f, 0.5f, 0.4f);
                    sr.transform.position = FieldView.ToWorld(p.Position);
                    float angle = Mathf.Atan2(p.Direction.y, p.Direction.x) * Mathf.Rad2Deg;
                    sr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    float s = Mathf.Max(0.6f, p.Radius * 3f) * FieldView.PerspectiveScale(p.Position.y);
                    sr.transform.localScale = new Vector3(s, s, 1f);
                    sr.sortingOrder = FieldView.ProjectileOrder;
                    if (!sr.gameObject.activeSelf) sr.gameObject.SetActive(true);
                }
            }

            for (int i = used; i < _pool.Count; i++)
            {
                if (_pool[i].gameObject.activeSelf) _pool[i].gameObject.SetActive(false);
            }
        }

        private SpriteRenderer Get(int index)
        {
            while (_pool.Count <= index)
            {
                var go = new GameObject("Bullet");
                go.transform.SetParent(transform, false);
                _pool.Add(go.AddComponent<SpriteRenderer>());
            }
            return _pool[index];
        }

        private static Sprite CreateDot()
        {
            const int n = 8;
            var tex = new Texture2D(n, n) { filterMode = FilterMode.Point };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x - n / 2f + 0.5f, dy = y - n / 2f + 0.5f;
                    px[y * n + x] = dx * dx + dy * dy <= (n / 2f) * (n / 2f) ? Color.white : Color.clear;
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
