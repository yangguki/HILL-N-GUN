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
        public static List<CoverInstance> Spawn(BattlefieldData field, CoverData[] coverDatas, bool mirrorToEnemySide)
        {
            var result = new List<CoverInstance>();
            if (field == null || field.covers == null || coverDatas == null) return result;

            var combat = ServiceLocator.Get<CombatSystem>();
            float offset = (field.coverOffsetMin + field.coverOffsetMax) * 0.5f;
            int ourRows = field.OurRowCount;
            int enemyRows = field.EnemyRowCount;

            foreach (var placement in field.covers)
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
            cover.Initialize(data, pos, row, team);
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
            return null;
        }
    }
}
