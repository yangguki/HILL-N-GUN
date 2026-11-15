using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.PvE
{
    /// <summary>
    /// FreeMove PvE spawn director (pure logic). Wave entries queue individual enemies; each is placed at an explicit
    /// StageSpawnPoint id or a point of its type chosen with the stage seed. Points within the safe radius of any
    /// active player, or still busy with a previous entrance / living sniper, are skipped. A wave alternates sides so it
    /// always uses at least two directions. When nothing is safe the spawn is deferred and retried deterministically;
    /// after RelaxAfter it may use any safe non-sniper point, and a sniper whose posts stay occupied for GiveUpAfter is
    /// skipped (logged) so a wave can never stall forever.
    /// </summary>
    public sealed class EnemySpawnDirector
    {
        public struct Pending
        {
            public EnemyData data;
            public string pointId;
            public SpawnPointType type;
            public float readyAt;
            public float firstTry;
            public int serial;
            public int wave;
        }

        public StreetLayoutData Layout { get; private set; }
        public float SafeRadius = 3f;
        public float RetryInterval = 0.25f;
        public float RelaxAfter = 2f;
        public float GiveUpAfter = 10f;
        public float EntranceBusyPad = 0.35f;

        public int PendingCount => _queue.Count;
        public int Deferrals { get; private set; }
        public int Relaxed { get; private set; }
        public int Skipped { get; private set; }
        public int Spawned { get; private set; }
        public IReadOnlyCollection<SpawnSide> WaveSides => _waveSides;
        public readonly Dictionary<SpawnPointType, int> SpawnsByType = new Dictionary<SpawnPointType, int>();

        private readonly List<Pending> _queue = new List<Pending>();
        private readonly HashSet<SpawnSide> _waveSides = new HashSet<SpawnSide>();
        private readonly Dictionary<SpawnSide, int> _sideUse = new Dictionary<SpawnSide, int>();
        private float[] _busyUntil;
        private bool[] _occupied;
        private System.Random _rng;
        private int _serial;
        private int _wave;
        private readonly List<int> _cand = new List<int>();

        public EnemySpawnDirector(StreetLayoutData layout, int seed)
        {
            Layout = layout;
            int n = layout?.spawnPoints?.Length ?? 0;
            _busyUntil = new float[n];
            _occupied = new bool[n];
            _rng = new System.Random(seed);
        }

        public void BeginWave(int waveIndex)
        {
            _wave = waveIndex;
            _waveSides.Clear();
            _sideUse.Clear();
        }

        public void Clear()
        {
            _queue.Clear();
            for (int i = 0; i < _busyUntil.Length; i++) { _busyUntil[i] = 0f; _occupied[i] = false; }
        }

        public static bool IsSniper(EnemyData d) => d != null && (d.stationary || d.enemyType == EnemyType.BalconySniper);

        public void Enqueue(EnemySpawnEntry entry, float now, float stagger)
        {
            if (entry?.enemyData == null) return;
            for (int i = 0; i < Mathf.Max(1, entry.count); i++)
            {
                var type = IsSniper(entry.enemyData) ? SpawnPointType.BalconySniper : entry.spawnType;
                _queue.Add(new Pending
                {
                    data = entry.enemyData, pointId = entry.spawnPointId, type = type,
                    readyAt = now + entry.spawnDelay + i * stagger, firstTry = -1f, serial = _serial++, wave = _wave
                });
            }
        }

        /// <summary>A living sniper keeps its window occupied until it is gone.</summary>
        public void SetOccupied(int pointIndex, bool occupied)
        {
            if (pointIndex >= 0 && pointIndex < _occupied.Length) _occupied[pointIndex] = occupied;
        }

        /// <summary>Spawns every due entry it can place; spawn(entry, pointIndex). Players = active player positions.</summary>
        public void Tick(float now, IList<Vector2> players, System.Action<Pending, int> spawn)
        {
            for (int qi = 0; qi < _queue.Count; qi++)
            {
                var p = _queue[qi];
                if (now < p.readyAt) continue;
                if (p.firstTry < 0f) p.firstTry = now;
                bool relaxed = now - p.firstTry >= RelaxAfter;
                int idx = Choose(p, players, now, relaxed);
                if (idx < 0)
                {
                    if (p.type == SpawnPointType.BalconySniper && now - p.firstTry >= GiveUpAfter)
                    {
                        Skipped++;
                        Debug.Log($"[Spawn] sniper skipped after {GiveUpAfter:0}s (windows occupied/unsafe) wave={p.wave}");
                        _queue.RemoveAt(qi); qi--; continue;
                    }
                    Deferrals++;
                    p.readyAt = now + RetryInterval;
                    _queue[qi] = p;
                    continue;
                }
                if (relaxed) Relaxed++;
                _queue.RemoveAt(qi); qi--;
                var sp = Layout.spawnPoints[idx];
                _busyUntil[idx] = now + sp.telegraphTime + sp.entranceTime + EntranceBusyPad;
                _waveSides.Add(sp.side);
                _sideUse[sp.side] = (_sideUse.TryGetValue(sp.side, out int u) ? u : 0) + 1;
                SpawnsByType[sp.type] = (SpawnsByType.TryGetValue(sp.type, out int c) ? c : 0) + 1;
                Spawned++;
                spawn?.Invoke(p, idx);
            }
        }

        public bool IsSafe(StageSpawnPoint sp, IList<Vector2> players)
        {
            if (players == null) return true;
            float r2 = SafeRadius * SafeRadius;
            for (int i = 0; i < players.Count; i++)
                if ((players[i] - sp.position).sqrMagnitude < r2 || (players[i] - sp.entranceStart).sqrMagnitude < r2) return false;
            return true;
        }

        private bool Usable(int i, IList<Vector2> players, float now)
        {
            var sp = Layout.spawnPoints[i];
            return sp != null && now >= _busyUntil[i] && !_occupied[i] && IsSafe(sp, players);
        }

        /// <summary>Point index for the pending spawn, or -1 to defer.</summary>
        public int Choose(Pending p, IList<Vector2> players, float now, bool relaxed)
        {
            if (Layout?.spawnPoints == null || Layout.spawnPoints.Length == 0) return -1;
            bool sniper = p.type == SpawnPointType.BalconySniper;
            _cand.Clear();
            int fixedIdx = string.IsNullOrEmpty(p.pointId) ? -1 : Layout.IndexOf(p.pointId);
            if (fixedIdx >= 0 && !relaxed)
            {
                if (Usable(fixedIdx, players, now)) return fixedIdx;
                // explicit point unsafe/busy: same type alternatives only
                var t = Layout.spawnPoints[fixedIdx].type;
                for (int i = 0; i < Layout.spawnPoints.Length; i++)
                    if (i != fixedIdx && Layout.spawnPoints[i].type == t && Usable(i, players, now)) _cand.Add(i);
                if (_cand.Count == 0) return -1;
                return PickBySide(_cand);
            }
            for (int i = 0; i < Layout.spawnPoints.Length; i++)
            {
                var sp = Layout.spawnPoints[i];
                if (sp == null || sp.IsSniperPost != sniper) continue;
                bool typeOk = sniper || relaxed || p.type == SpawnPointType.Any || sp.type == p.type;
                if (typeOk && Usable(i, players, now)) _cand.Add(i);
            }
            if (_cand.Count == 0) return -1;
            return PickBySide(_cand);
        }

        /// <summary>Prefer a side this wave has not used yet (≥ 2 directions), then the least-used side; seeded pick.</summary>
        private int PickBySide(List<int> cand)
        {
            int bestUse = int.MaxValue;
            foreach (int i in cand)
            {
                var side = Layout.spawnPoints[i].side;
                int use = _waveSides.Contains(side) ? (_sideUse.TryGetValue(side, out int u) ? u : 0) + 1 : 0;
                if (use < bestUse) bestUse = use;
            }
            int count = 0;
            foreach (int i in cand)
            {
                var side = Layout.spawnPoints[i].side;
                int use = _waveSides.Contains(side) ? (_sideUse.TryGetValue(side, out int u) ? u : 0) + 1 : 0;
                if (use == bestUse) count++;
            }
            int pick = _rng.Next(count);
            foreach (int i in cand)
            {
                var side = Layout.spawnPoints[i].side;
                int use = _waveSides.Contains(side) ? (_sideUse.TryGetValue(side, out int u) ? u : 0) + 1 : 0;
                if (use != bestUse) continue;
                if (pick-- == 0) return i;
            }
            return cand[0];
        }
    }
}
