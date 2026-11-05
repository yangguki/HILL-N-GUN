using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Combat;

namespace HillNGun.Cover
{
    /// <summary>
    /// Spawns covers for a match from BattlefieldData.covers (logic + view), registers them
    /// with CombatSystem. Optionally mirrors the layout onto the enemy side (PvP).
    /// </summary>
    public static class CoverFieldSpawner
    {
        /// <summary>
        /// Spawn with the current scale preset: the preset's layout for <paramref name="layoutKey"/> (StageData.id or the
        /// PvP key) replaces the stage / field layout when the preset has one; every cover gets its preset size.
        /// </summary>
        public static List<CoverInstance> SpawnWithPreset(BattlefieldData field, CoverData[] coverDatas, bool mirrorToEnemySide, string layoutKey, CoverPlacement[] stagePlacements = null)
        {
            var fallback = stagePlacements != null && stagePlacements.Length > 0 ? stagePlacements : field?.covers;
            var placements = ScalePresets.LayoutFor(layoutKey, fallback);
            Debug.Log($"[CoverFieldSpawner] scale option {ScalePresets.Current} layout '{layoutKey}' -> {(placements == fallback ? "stage default" : "preset set")} ({placements?.Length ?? 0} placements)");
            return Spawn(field, coverDatas, mirrorToEnemySide, placements);
        }

        public static List<CoverInstance> Spawn(BattlefieldData field, CoverData[] coverDatas, bool mirrorToEnemySide, CoverPlacement[] placementsOverride = null)
        {
            var result = new List<CoverInstance>();
            var placements = placementsOverride != null && placementsOverride.Length > 0 ? placementsOverride : field?.covers;
            if (field == null || placements == null || coverDatas == null) return result;

            var combat = ServiceLocator.Get<CombatSystem>();
            float offset = (field.coverOffsetMin + field.coverOffsetMax) * 0.5f;
            int ourRows = field.OurRowCount;
            int enemyRows = field.EnemyRowCount;

            foreach (var placement in placements)
            {
                CoverData data = Find(coverDatas, placement.coverId);
                if (data == null)
                {
                    Debug.LogWarning($"[CoverFieldSpawner] CoverData not found: {placement.coverId}");
                    continue;
                }

                if (placement.rowIndex < ourRows)
                {
                    // Our side: cover sits toward the enemy (larger Y)
                    float y = field.GetOurRowY(placement.rowIndex) + offset;
                    result.Add(Create(data, new Vector2(placement.xPosition, y), placement.rowIndex, Team.Player, combat));

                    if (mirrorToEnemySide && enemyRows > 0)
                    {
                        // Mirror: our back row (0) <-> enemy back row (last); cover toward us (smaller Y)
                        int enemyRow = Mathf.Clamp(enemyRows - 1 - placement.rowIndex, 0, enemyRows - 1);
                        float ey = field.GetEnemyRowY(enemyRow) - offset;
                        result.Add(Create(data, new Vector2(placement.xPosition, ey), ourRows + enemyRow, Team.Enemy, combat));
                    }
                }
                else
                {
                    int enemyRow = placement.rowIndex - ourRows;
                    float ey = field.GetEnemyRowY(enemyRow) - offset;
                    result.Add(Create(data, new Vector2(placement.xPosition, ey), placement.rowIndex, Team.Enemy, combat));
                }
            }

            Debug.Log($"[CoverFieldSpawner] Spawned {result.Count} covers (mirror={mirrorToEnemySide})");
            return result;
        }

        private static CoverInstance Create(CoverData data, Vector2 pos, int row, Team team, CombatSystem combat)
        {
            var cover = new CoverInstance();
            cover.Initialize(data, pos, row, team, ScalePresets.CoverSizeOf(data));
            combat?.RegisterCover(cover);

            var go = new GameObject($"Cover_{data.id}_{team}_{pos.x:F0}");
            var view = go.AddComponent<CoverView>();
            view.Initialize(cover);
            return cover;
        }

        private static CoverData Find(CoverData[] datas, string id)
        {
            foreach (var d in datas)
                if (d != null && d.id == id) return d;
            return ScalePresets.FindExtraCover(id); // preset-only covers (crate, hay) live in ScalePresetData
        }
    }
}
