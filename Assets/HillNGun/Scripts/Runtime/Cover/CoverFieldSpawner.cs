using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Combat;

namespace HillNGun.Cover
{
    /// <summary>
    /// The ONE cover placement path (12_terrain_props §7): StageData.coverPlacements (PvE) or BattlefieldData.covers
    /// (PvP / fallback) -> CoverInstance + CoverView, registered with CombatSystem. No other manager spawns covers in
    /// match scenes. Positions: row y + coverCenterOffset (0.60), front rows use frontRowCoverOffset (0.35) so the
    /// collider never enters the centre no-cover band. PvP enemy side = rotational copy (24-x, 18-y).
    /// </summary>
    public static class CoverFieldSpawner
    {
        /// <summary>Last spawn (debug / validation): layout key and instance ids.</summary>
        public static string LastLayoutKey { get; private set; }
        public static int LastSpawnCount { get; private set; }

        /// <summary>
        /// Spawn for a stage id / PvP key. Placement authority = stage placements, else BattlefieldData.covers.
        /// ScalePresetData layouts only replace it when ScalePresetData.layoutsOverrideStageData is on (default off since P0).
        /// </summary>
        public static List<CoverInstance> SpawnWithPreset(BattlefieldData field, CoverData[] coverDatas, bool mirrorToEnemySide, string layoutKey, CoverPlacement[] stagePlacements = null)
        {
            var fallback = stagePlacements != null && stagePlacements.Length > 0 ? stagePlacements : field?.covers;
            bool presetOverride = ScalePresets.Data != null && ScalePresets.Data.layoutsOverrideStageData;
            var placements = presetOverride ? ScalePresets.LayoutFor(layoutKey, fallback) : fallback;
            Debug.Log($"[CoverFieldSpawner] scale option {ScalePresets.Current} layout '{layoutKey}' -> {(placements == fallback ? (stagePlacements != null && stagePlacements.Length > 0 ? "StageData" : "BattlefieldData") : "preset set (override on)")} ({placements?.Length ?? 0} placements) layoutVersion={TerrainRules.Data.layoutVersion}");
            return Spawn(field, coverDatas, mirrorToEnemySide, placements, layoutKey);
        }

        /// <summary>Logic-y of a cover for a placement row (12 §2.1 offsets, front-row exception).</summary>
        public static float CoverY(BattlefieldData field, int rowIndex, out Team team, out int enemyRow)
        {
            var r = TerrainRules.Data;
            int ourRows = field.OurRowCount;
            enemyRow = -1;
            if (rowIndex < ourRows)
            {
                team = Team.Player;
                bool front = rowIndex == ourRows - 1;
                return field.GetOurRowY(rowIndex) + (front ? r.frontRowCoverOffset : r.coverCenterOffset);
            }
            team = Team.Enemy;
            enemyRow = rowIndex - ourRows;
            bool efront = enemyRow == 0;
            return field.GetEnemyRowY(enemyRow) - (efront ? r.frontRowCoverOffset : r.coverCenterOffset);
        }

        public static List<CoverInstance> Spawn(BattlefieldData field, CoverData[] coverDatas, bool mirrorToEnemySide, CoverPlacement[] placementsOverride = null, string layoutKey = null)
        {
            var result = new List<CoverInstance>();
            var placements = placementsOverride != null && placementsOverride.Length > 0 ? placementsOverride : field?.covers;
            if (field == null || placements == null || coverDatas == null) return result;

            var combat = ServiceLocator.Get<CombatSystem>();
            int ourRows = field.OurRowCount;
            int enemyRows = field.EnemyRowCount;
            var ids = new HashSet<string>();

            foreach (var placement in placements)
            {
                CoverData data = Find(coverDatas, placement.coverId);
                if (data == null)
                {
                    Debug.LogWarning($"[CoverFieldSpawner] CoverData not found: {placement.coverId}");
                    continue;
                }

                float y = CoverY(field, placement.rowIndex, out Team team, out int er);
                var pos = new Vector2(placement.xPosition, y);
                var c = Create(data, pos, placement.rowIndex, team, combat, layoutKey, ids);
                if (c != null) result.Add(c);

                if (team == Team.Player && mirrorToEnemySide && enemyRows > 0)
                {
                    // PvP: rotational copy (24-x, 18-y). Our row i <-> enemy row (ourRows-1-i) so front<->front.
                    var mp = new Vector2(field.size.x - pos.x, field.size.y - pos.y);
                    int enemyRow = Mathf.Clamp(enemyRows - 1 - placement.rowIndex, 0, enemyRows - 1);
                    var m = Create(data, mp, ourRows + enemyRow, Team.Enemy, combat, layoutKey, ids);
                    if (m != null) result.Add(m);
                }
            }

            LastLayoutKey = layoutKey;
            LastSpawnCount = result.Count;
            Debug.Log($"[CoverFieldSpawner] Spawned {result.Count} covers (mirror={mirrorToEnemySide}, rotational) key={layoutKey}");
            return result;
        }

        private static CoverInstance Create(CoverData data, Vector2 pos, int row, Team team, CombatSystem combat, string layoutKey, HashSet<string> ids)
        {
            string id = $"{layoutKey ?? "field"}:{team}:r{row}:x{pos.x:0.##}";
            if (!ids.Add(id))
            {
                Debug.LogWarning($"[CoverFieldSpawner] duplicate placement {id} skipped (double spawn guard)");
                return null;
            }
            var cover = new CoverInstance();
            cover.Initialize(data, pos, row, team, ScalePresets.CoverSizeOf(data));
            cover.InstanceId = id;
            cover.LayoutVersion = TerrainRules.Data.layoutVersion;
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
