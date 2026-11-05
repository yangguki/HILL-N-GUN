using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Cover
{
    public class CoverInstance
    {
        public CoverData Data { get; private set; }
        public Vector2 Position { get; private set; }
        public int RowIndex { get; private set; }
        public Team Team { get; private set; }
        public int CurrentHp { get; private set; }
        public CoverStage Stage { get; private set; }
        public bool IsDestroyed => Stage == CoverStage.Destroyed;
        /// <summary>Collision width (tiles). = CoverData.width unless a scale preset set it at spawn.</summary>
        public float Width { get; private set; }
        /// <summary>Resolved preset size (view reads visualWidth / visualHeight); fromPreset = false when unscaled.</summary>
        public CoverSize Size { get; private set; }

        private CoverStage _previousStage;

        public event System.Action<CoverInstance, int> OnDamaged;
        public event System.Action<CoverInstance, CoverStage> OnStageChanged;
        public event System.Action<CoverInstance> OnDestroyed;

        public void Initialize(CoverData data, Vector2 position, int rowIndex, Team team)
            => Initialize(data, position, rowIndex, team, new CoverSize { logicWidth = data.width, visualWidth = data.width, visualHeight = -1f });

        public void Initialize(CoverData data, Vector2 position, int rowIndex, Team team, CoverSize size)
        {
            Data = data;
            Size = size;
            Width = size.logicWidth > 0f ? size.logicWidth : data.width;
            Position = position;
            RowIndex = rowIndex;
            Team = team;
            CurrentHp = data.maxHp;
            Stage = CoverStage.Intact;
            _previousStage = Stage;
        }

        public void TakeDamage(int damage, float multiplier = 1f)
        {
            if (IsDestroyed) return;
            if (Data != null && Data.indestructible) return;

            int actualDamage = Mathf.RoundToInt(damage * multiplier);
            CurrentHp = Mathf.Max(0, CurrentHp - actualDamage);

            OnDamaged?.Invoke(this, actualDamage);

            Stage = Data.GetStage(CurrentHp);

            if (Stage != _previousStage)
            {
                OnStageChanged?.Invoke(this, Stage);
                _previousStage = Stage;

                if (Stage == CoverStage.Destroyed)
                {
                    OnDestroyed?.Invoke(this);
                }
            }
        }

        public void Repair(float repairPercent)
        {
            if (!Data.repairable || IsDestroyed) return;

            int healAmount = Mathf.RoundToInt(Data.maxHp * repairPercent);
            CurrentHp = Mathf.Min(Data.maxHp, CurrentHp + healAmount);

            Stage = Data.GetStage(CurrentHp);
            if (Stage != _previousStage)
            {
                OnStageChanged?.Invoke(this, Stage);
                _previousStage = Stage;
            }
        }

        public void Reset()
        {
            CurrentHp = Data.maxHp;
            Stage = CoverStage.Intact;
            _previousStage = Stage;
        }

        public bool ContainsX(float x)
        {
            float halfWidth = Width / 2f;
            return x >= Position.x - halfWidth && x <= Position.x + halfWidth;
        }

        public Rect GetBounds()
        {
            return new Rect(
                Position.x - Width / 2f,
                Position.y - Data.depth / 2f,
                Width,
                Data.depth
            );
        }

        public float HpRatio => (float)CurrentHp / Data.maxHp;
    }
}
