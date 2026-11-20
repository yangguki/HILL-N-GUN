using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public class Proto_GreyboxTarget
    {
        const float FootprintDepthHalf = 0.25f;

        public Vector2 FootPosition { get; set; }
        public int Hp { get; private set; }
        public bool IsAlive => Hp > 0;

        public Proto_GreyboxTarget(Vector2 foot, int hp = -1)
        {
            FootPosition = foot;
            Hp = hp < 0 ? Proto_GreyboxTuning.TargetHp : hp;
        }

        public static Rect GroundFootprint(Vector2 foot)
        {
            float halfW = Proto_GreyboxLayout.WorldWidth * (Proto_GreyboxTuning.TargetWidthPercent / 100f) * 0.5f;
            return new Rect(foot.x - halfW, foot.y - FootprintDepthHalf, halfW * 2f, FootprintDepthHalf * 2f);
        }

        public Vector2 BodyCenterGround =>
            FootPosition + new Vector2(0f,
                Proto_GreyboxLayout.ScreenPercentToWorldHeight(Proto_GreyboxTuning.TargetHeightPercent) * 0.35f *
                Proto_DepthScale.ScaleAt(FootPosition.y));

        public bool ApplyDamage(int amount)
        {
            if (!IsAlive) return false;
            Hp = Mathf.Max(0, Hp - amount);
            return Hp == 0;
        }

        public static float GroundDistance(Vector2 from, Proto_GreyboxTarget t) =>
            (t.FootPosition - from).magnitude;

        public static Proto_GreyboxTarget SelectNearest(Vector2 from, IList<Proto_GreyboxTarget> targets)
        {
            Proto_GreyboxTarget best = null;
            float bestDist = float.MaxValue;
            float bestDx = float.MaxValue;
            foreach (var t in targets)
            {
                if (t == null || !t.IsAlive) continue;
                float dist = GroundDistance(from, t);
                float dx = Mathf.Abs(t.FootPosition.x - from.x);
                if (dist < bestDist - 0.0001f || (Mathf.Abs(dist - bestDist) < 0.0001f && dx < bestDx))
                {
                    bestDist = dist;
                    bestDx = dx;
                    best = t;
                }
            }

            return best;
        }
    }

    public class Proto_GreyboxTargetView : MonoBehaviour
    {
        SpriteRenderer _sprite;
        Color _baseColor;
        float _flashTimer;
        Proto_GreyboxTarget _logic;

        public Proto_GreyboxTarget Logic => _logic;

        public void Init(Proto_GreyboxTarget logic)
        {
            _logic = logic;
            _sprite = GetComponent<SpriteRenderer>();
            _baseColor = _sprite != null ? _sprite.color : new Color(1f, 0.15f, 0.15f, 0.45f);
            SyncTransform();
        }

        public bool TakeDamage(int amount)
        {
            if (_logic == null || !_logic.IsAlive) return false;
            bool killed = _logic.ApplyDamage(amount);
            _flashTimer = 0.08f;
            if (killed)
                gameObject.SetActive(false);
            return killed;
        }

        public void FlashHit()
        {
            if (_logic == null || !_logic.IsAlive) return;
            _flashTimer = 0.08f;
        }

        public void SetHiddenVisual(bool hidden)
        {
            if (_sprite == null) return;
            _sprite.color = hidden
                ? new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0.2f)
                : _baseColor;
        }

        public void MarkDefeated()
        {
            if (_sprite != null)
                _sprite.color = new Color(0.55f, 0.55f, 0.55f, 0.85f);
        }

        void LateUpdate()
        {
            if (_logic == null || !_logic.IsAlive) return;
            SyncTransform();
            if (_sprite == null) return;
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                _sprite.color = Color.white;
            }
            else
                _sprite.color = _baseColor;
        }

        public void SyncFromLogic()
        {
            if (_logic == null || !_logic.IsAlive) return;
            SyncTransform();
        }

        void SyncTransform()
        {
            if (_logic == null) return;
            var foot = _logic.FootPosition;
            transform.position = new Vector3(foot.x, foot.y, 0f);
            float scale = Proto_DepthScale.ScaleAt(foot.y);
            float targetH = Proto_GreyboxLayout.ScreenPercentToWorldHeight(Proto_GreyboxTuning.TargetHeightPercent);
            var sprite = _sprite != null ? _sprite.sprite : null;
            if (sprite != null)
                transform.localScale = Proto_GreyboxSpriteUtil.ScaleForWorldHeightUniform(sprite, targetH);
            else
            {
                float targetW = Proto_GreyboxLayout.WorldWidth * (Proto_GreyboxTuning.TargetWidthPercent / 100f);
                float baseH = targetH / scale;
                float baseW = targetW / scale;
                transform.localScale = new Vector3(baseW * scale, baseH * scale, 1f);
            }
            if (_sprite != null)
            {
                int depthOrder = Proto_GreyboxLayout.SortingOrderAtFootY(foot.y);
                _sprite.sortingOrder = Mathf.Max(depthOrder, Proto_GreyboxLayout.SortingEnemyMarker);
            }
        }
    }
}