using System;
using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.Cover
{
    /// <summary>
    /// 12_terrain_props §4/§5/§7 pure maths + validator over the ONE placement source (StageData / BattlefieldData).
    /// Widths come from a caller-supplied function so the same check runs on data widths (doc tables) and on the
    /// runtime preset widths (v8 gameplay-size formula).
    /// </summary>
    public static class TerrainLayout
    {
        /// <summary>Non-failing notes from the last Validate (declared proposal exceptions).</summary>
        public static readonly List<string> LastNotes = new List<string>();
        public struct Placed
        {
            public string coverId; public int row; public Team team; public Vector2 pos; public float width; public float depth;
            public float XMin => pos.x - width * 0.5f; public float XMax => pos.x + width * 0.5f;
            public float YMin => pos.y - depth * 0.5f; public float YMax => pos.y + depth * 0.5f;
        }

        /// <summary>
        /// §4.3 safe centre half-interval: |dx| <= coverHalfWidth - (actorRadius + incomingRadius) guarantees a straight
        /// shot is blocked. Negative = no guaranteed slot (the cover still "hides" the actor visually / x-range rule).
        /// </summary>
        public static float SafeHalfWidth(float coverWidth, float actorRadius, float incomingRadius)
            => coverWidth * 0.5f - (actorRadius + incomingRadius);

        /// <summary>Behind-cover x-range rule (pose / crouch category): actor centre inside the cover footprint.</summary>
        public static bool InCoverXRange(float coverX, float coverWidth, float actorX) => Mathf.Abs(actorX - coverX) <= coverWidth * 0.5f;

        public static bool GuaranteedSafe(float coverX, float coverWidth, float actorX, float actorRadius, float incomingRadius)
        {
            float h = SafeHalfWidth(coverWidth, actorRadius, incomingRadius);
            return h >= 0f && Mathf.Abs(actorX - coverX) <= h + 1e-5f;
        }

        public static List<Placed> Place(BattlefieldData field, CoverPlacement[] placements, Func<string, CoverData> lookup, Func<CoverData, float> width, bool pvpRotational)
        {
            var list = new List<Placed>();
            if (field == null || placements == null) return list;
            int ourRows = field.OurRowCount, enemyRows = field.EnemyRowCount;
            foreach (var p in placements)
            {
                var d = lookup(p.coverId);
                float w = d != null ? width(d) : 0f, dep = d != null ? d.depth : 0.5f;
                float y = CoverFieldSpawner.CoverY(field, p.rowIndex, out Team team, out _);
                var a = new Placed { coverId = p.coverId, row = p.rowIndex, team = team, pos = new Vector2(p.xPosition, y), width = w, depth = dep };
                list.Add(a);
                if (pvpRotational && team == Team.Player && enemyRows > 0)
                {
                    int er = Mathf.Clamp(enemyRows - 1 - p.rowIndex, 0, enemyRows - 1);
                    list.Add(new Placed { coverId = p.coverId, row = ourRows + er, team = Team.Enemy, pos = new Vector2(field.size.x - a.pos.x, field.size.y - a.pos.y), width = w, depth = dep });
                }
            }
            return list;
        }

        /// <summary>Total projected cover width / (rows x 24). Hazards are not covers.</summary>
        public static float Coverage(List<Placed> placed, Team team, int rows, float fieldWidth, out float sum)
        {
            sum = 0f;
            foreach (var p in placed) if (p.team == team) sum += p.width;
            return rows > 0 ? sum / (rows * fieldWidth) : 0f;
        }

        /// <summary>Validator (12 §7). Returns human-readable issues; empty list = pass.</summary>
        public static List<string> Validate(string key, BattlefieldData field, CoverPlacement[] placements, HazardPlacement[] hazards,
            Func<string, CoverData> lookup, Func<CoverData, float> width, bool pvpRotational, int expectedBarrels = -1, int[] expectedRowCounts = null)
        {
            var issues = new List<string>();
            LastNotes.Clear();
            var r = TerrainRules.Data;
            if (r.intactMin <= r.crackedMin || r.crackedMin <= 0f || r.intactMin > 1f) issues.Add($"{key}: thresholds not descending ({r.intactMin}/{r.crackedMin}/0)");
            if (placements == null || placements.Length == 0) { issues.Add($"{key}: no placements"); return issues; }

            var seen = new HashSet<string>();
            foreach (var p in placements)
            {
                var d = lookup(p.coverId);
                if (d == null) { issues.Add($"{key}: unknown cover id {p.coverId}"); continue; }
                if (d.maxHp <= 0 || d.width <= 0f || d.depth <= 0f) issues.Add($"{key}: {d.id} non-positive HP/width/depth");
                if (!seen.Add($"{p.rowIndex}:{p.xPosition:0.##}")) issues.Add($"{key}: duplicate placement row {p.rowIndex} x {p.xPosition}");
            }
            if (expectedRowCounts != null)
                for (int row = 0; row < expectedRowCounts.Length; row++)
                {
                    int n = 0; foreach (var p in placements) if (p.rowIndex == row) n++;
                    if (n != expectedRowCounts[row]) issues.Add($"{key}: row {row} has {n} covers, expected {expectedRowCounts[row]}");
                }

            var placed = Place(field, placements, lookup, width, pvpRotational);
            foreach (var a in placed)
            {
                if (a.YMax > r.noCoverMinY + 1e-4f && a.YMin < r.noCoverMaxY - 1e-4f)
                    issues.Add($"{key}: {a.coverId} {a.team} collider y {a.YMin:F2}..{a.YMax:F2} enters centre {r.noCoverMinY}..{r.noCoverMaxY}");
                if (a.XMin < -1e-4f || a.XMax > field.size.x + 1e-4f) issues.Add($"{key}: {a.coverId} x {a.XMin:F2}..{a.XMax:F2} outside field");
            }
            // overlap / minimum visual gap per row
            var byRow = new Dictionary<int, List<Placed>>();
            foreach (var a in placed) { if (!byRow.TryGetValue(a.row, out var l)) byRow[a.row] = l = new List<Placed>(); l.Add(a); }
            foreach (var kv in byRow)
            {
                kv.Value.Sort((u, v) => u.pos.x.CompareTo(v.pos.x));
                for (int i = 1; i < kv.Value.Count; i++)
                {
                    float gap = kv.Value[i].XMin - kv.Value[i - 1].XMax;
                    if (gap <= 0f) issues.Add($"{key}: row {kv.Key} overlap {kv.Value[i - 1].coverId}@{kv.Value[i - 1].pos.x} / {kv.Value[i].coverId}@{kv.Value[i].pos.x}");
                    else if (gap + 1e-4f < r.minimumVisualGap)
                    {
                        bool paired = r.pairedPropIds != null && System.Array.IndexOf(r.pairedPropIds, kv.Value[i].coverId) >= 0 && kv.Value[i].coverId == kv.Value[i - 1].coverId;
                        if (paired && gap + 1e-4f >= r.pairedPropMinGap)
                            LastNotes.Add($"{key}: row {kv.Key} PAIRED-PROP EXCEPTION {kv.Value[i].coverId} x {kv.Value[i - 1].pos.x}/{kv.Value[i].pos.x} gap {gap:F2} < minimumVisualGap {r.minimumVisualGap} (allowed >= {r.pairedPropMinGap})");
                        else issues.Add($"{key}: row {kv.Key} gap {gap:F2} < {r.minimumVisualGap} between x {kv.Value[i - 1].pos.x} and {kv.Value[i].pos.x}");
                    }
                }
            }
            if (pvpRotational)
            {
                foreach (var a in placed)
                {
                    if (a.team != Team.Player) continue;
                    var want = new Vector2(field.size.x - a.pos.x, field.size.y - a.pos.y);
                    bool ok = placed.Exists(b => b.team == Team.Enemy && b.coverId == a.coverId && Vector2.Distance(b.pos, want) < 1e-3f && Mathf.Abs(b.width - a.width) < 1e-4f);
                    if (!ok) issues.Add($"{key}: no rotational pair for {a.coverId}@({a.pos.x},{a.pos.y})");
                }
            }

            // hazards (barrels): not covers, not in the start area, spaced, counted
            var barrels = new List<Vector2>();
            if (hazards != null) foreach (var h in hazards) if (h != null && h.type == HazardType.ExplosiveBarrel) barrels.Add(h.PositionOn(field));
            if (expectedBarrels >= 0 && barrels.Count != expectedBarrels) issues.Add($"{key}: {barrels.Count} barrels, expected {expectedBarrels}");
            float startY = field.GetOurRowY(Mathf.Clamp(field.startRowIndex, 0, field.OurRowCount - 1));
            foreach (var b in barrels)
            {
                if (b.y > r.noCoverMinY && b.y < r.noCoverMaxY) issues.Add($"{key}: barrel ({b.x},{b.y}) in centre band");
                if (field.startX != null)
                    foreach (var sx in field.startX)
                        for (int row = 0; row < field.OurRowCount; row++)
                        {
                            float d = Vector2.Distance(b, new Vector2(sx, field.GetOurRowY(row)));
                            if (d <= r.barrelRadius + 0.5f) issues.Add($"{key}: barrel ({b.x},{b.y}) blast r{r.barrelRadius} reaches start/row point ({sx},{field.GetOurRowY(row)})");
                        }
            }
            for (int i = 0; i < barrels.Count; i++)
                for (int j = i + 1; j < barrels.Count; j++)
                    if (Vector2.Distance(barrels[i], barrels[j]) <= r.minBarrelSpacing) issues.Add($"{key}: barrels {barrels[i]} / {barrels[j]} closer than {r.minBarrelSpacing}");
            return issues;
        }
    }
}
