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

        /// <summary>Stable per-layout id (stage/PvP key + team + row + x), for logs and duplicate-spawn validation.</summary>
        public string InstanceId { get; set; }
        public int LayoutVersion { get; set; }
        /// <summary>Pose / occlusion class only (12 §2.2); never changes collision or damage.</summary>
        public CoverHeightClass HeightClass => Data != null ? TerrainRules.Data.ClassOf(Data.id) : CoverHeightClass.Half;
        /// <summary>Times OnDestroyed fired (must stay 0 or 1 per life).</summary>
        public int DestroyedEvents { get; private set; }

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

        public void TakeDamage(int damage, float multiplier = 1f) => TakeDamage(damage, multiplier, null);

        /// <param name="source">damage context for logs (bullet / grenade / barrel / skill)</param>
        public void TakeDamage(int damage, float multiplier, string source)
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
                Debug.Log($"[Terrain] {(Stage == CoverStage.Destroyed ? "cover_destroyed" : "cover_damaged")} id={Data.id} instance={InstanceId} state={Stage} hp={CurrentHp}/{Data.maxHp} row={RowIndex} team={Team} source={source ?? "unknown"} layoutVersion={LayoutVersion}");

                if (Stage == CoverStage.Destroyed)
                {
                    DestroyedEvents++;
                    OnDestroyed?.Invoke(this); // once: further hits return early (IsDestroyed)
                }
            }
        }

        public void Repair(float repairPercent)
        {
            if (!Data.repairable || Data.indestructible || IsDestroyed) return; // living, repairable covers only

            int healAmount = Mathf.RoundToInt(Data.maxHp * repairPercent);
            CurrentHp = Mathf.Min(Data.maxHp, CurrentHp + healAmount);

            Stage = Data.GetStage(CurrentHp);
            if (Stage != _previousStage)
            {
                OnStageChanged?.Invoke(this, Stage);
                _previousStage = Stage;
            }
        }

        /// <summary>Stage entry / PvP round start restore (no regeneration between waves / mid-round).</summary>
        public void Reset()
        {
            CurrentHp = Data.maxHp;
            Stage = CoverStage.Intact;
            _previousStage = Stage;
            DestroyedEvents = 0;
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
