using UnityEngine;
using HillNGun.Data;

namespace HillNGun.Scenery
{
    /// <summary>Pure logic(tile) -> view-world mapping (same formula as BattlefieldSystem) usable without a scene.</summary>
    public struct FieldMap
    {
        public float sizeX, sizeY, persBottom, persTop;
        public float[] ourRows, enemyRows;

        public static FieldMap From(BattlefieldData d)
        {
            if (d == null) return Default;
            return new FieldMap
            {
                sizeX = d.size.x, sizeY = d.size.y, persBottom = d.perspectiveScaleBottom, persTop = d.perspectiveScaleTop,
                ourRows = d.ourRowsY, enemyRows = d.enemyRowsY
            };
        }

        public static FieldMap Default => new FieldMap
        {
            sizeX = 24f, sizeY = 18f, persBottom = 1f, persTop = 0.8f,
            ourRows = new[] { 2f, 4.5f, 7f }, enemyRows = new[] { 11f, 13.5f, 16f }
        };

        public float S(float ly) => Mathf.Lerp(persBottom, persTop, Mathf.InverseLerp(0f, sizeY, ly));

        public Vector2 W(float lx, float ly)
        {
            float s = S(ly);
            return new Vector2((lx - sizeX * 0.5f) * s, ly - sizeY * 0.5f);
        }

        public Vector2 ToLogic(Vector2 w)
        {
            float ly = w.y + sizeY * 0.5f;
            float s = S(ly);
            return new Vector2(w.x / s + sizeX * 0.5f, ly);
        }

        public float Horizon => sizeY * 0.5f;
    }
}
