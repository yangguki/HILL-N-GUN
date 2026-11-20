using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public enum Proto_GreyboxWaveDirectorState
    {
        Idle,
        Spawning,
        Fighting,
        WaveDelay,
        Cleared
    }

    public class Proto_GreyboxWaveDirector
    {
        public Proto_GreyboxWaveDirectorState State { get; private set; } = Proto_GreyboxWaveDirectorState.Idle;
        public int CurrentWave { get; private set; }
        public int TotalWaves => 3;

        readonly List<Proto_GreyboxGrunt> _grunts = new List<Proto_GreyboxGrunt>();
        readonly List<Vector2> _pendingSpawns = new List<Vector2>();
        readonly List<Vector2> _slotPositions;
        readonly int _rngSeed;
        System.Random _rng;

        int _spawnedThisWave;
        int _targetThisWave;
        float _spawnTimer;
        float _waveDelayTimer;
        public IReadOnlyList<Proto_GreyboxGrunt> Grunts => _grunts;

        public Proto_GreyboxWaveDirector(int rngSeed = 42)
        {
            _rngSeed = rngSeed;
            _rng = new System.Random(rngSeed);
            _slotPositions = BuildDefaultSlots();
        }

        public void ResetRun()
        {
            _grunts.Clear();
            _pendingSpawns.Clear();
            State = Proto_GreyboxWaveDirectorState.Idle;
            CurrentWave = 0;
            _spawnedThisWave = 0;
            _targetThisWave = 0;
            _spawnTimer = 0f;
            _waveDelayTimer = 0f;
            _rng = new System.Random(_rngSeed);
        }

        public void StartRun(Proto_GreyboxRuntimeTuning tuning)
        {
            ResetRun();
            BeginWave(1, tuning);
        }

        public void BeginWave(int wave, Proto_GreyboxRuntimeTuning tuning)
        {
            CurrentWave = wave;
            _targetThisWave = tuning.WaveEnemyCount(wave);
            _spawnedThisWave = 0;
            _pendingSpawns.Clear();
            _spawnTimer = 0f;
            State = Proto_GreyboxWaveDirectorState.Spawning;
            for (int i = 0; i < _targetThisWave; i++)
            {
                var spawn = PickSpawnDoor(i);
                _pendingSpawns.Add(spawn);
            }
        }

        public void Tick(float dt, Proto_GreyboxRuntimeTuning tuning)
        {
            if (State == Proto_GreyboxWaveDirectorState.Spawning)
            {
                _spawnTimer -= dt;
                while (_spawnTimer <= 0f && _spawnedThisWave < _targetThisWave)
                {
                    SpawnNextGrunt(tuning);
                    _spawnTimer += Proto_GreyboxTuning.GruntSpawnInterval;
                }

                if (_spawnedThisWave >= _targetThisWave)
                    State = Proto_GreyboxWaveDirectorState.Fighting;
            }
            else if (State == Proto_GreyboxWaveDirectorState.WaveDelay)
            {
                _waveDelayTimer -= dt;
                if (_waveDelayTimer <= 0f)
                {
                    if (CurrentWave >= TotalWaves)
                        State = Proto_GreyboxWaveDirectorState.Cleared;
                    else
                        BeginWave(CurrentWave + 1, tuning);
                }
            }
            else if (State == Proto_GreyboxWaveDirectorState.Fighting && AllGruntsDead())
            {
                State = Proto_GreyboxWaveDirectorState.WaveDelay;
                _waveDelayTimer = Proto_GreyboxTuning.WaveClearDelay;
            }
        }

        void SpawnNextGrunt(Proto_GreyboxRuntimeTuning tuning)
        {
            var spawn = _pendingSpawns[_spawnedThisWave];
            var slot = PickFreeSlot();
            var behavior = (_rng.NextDouble() < 0.5)
                ? Proto_GreyboxGruntBehavior.Strafe
                : Proto_GreyboxGruntBehavior.Popup;
            if (Mathf.Abs(slot.x - Proto_GreyboxLayout.ScreenXPercentToWorldX(54f)) > 4f)
                behavior = Proto_GreyboxGruntBehavior.Strafe;
            float firePhase = (float)_rng.NextDouble() * tuning.EnemyFireInterval;
            float strafePhase = (float)_rng.NextDouble() * Mathf.PI * 2f;
            _grunts.Add(new Proto_GreyboxGrunt(spawn, slot, behavior, tuning.EnemyHp, firePhase, strafePhase));
            _spawnedThisWave++;
        }

        public bool AllGruntsDead()
        {
            for (int i = 0; i < _grunts.Count; i++)
            {
                if (_grunts[i].Target.IsAlive) return false;
            }

            return _grunts.Count > 0;
        }

        Vector2 PickSpawnDoor(int index)
        {
            float footY = Proto_GreyboxLayout.ScreenYPercentToWorldY(37f);
            if (index % 3 == 0)
                return new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(0f), Proto_GreyboxLayout.ScreenYPercentToWorldY(37f));
            if (index % 3 == 1)
                return new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(100f), Proto_GreyboxLayout.ScreenYPercentToWorldY(37f));
            int block = (index / 3) % 5;
            float cx = Proto_GreyboxLayout.ScreenXPercentToWorldX(10f + block * 20f);
            return new Vector2(cx, footY);
        }

        Vector2 PickFreeSlot()
        {
            float footY = Proto_GreyboxLayout.ScreenYPercentToWorldY(37f);
            for (int attempt = 0; attempt < 32; attempt++)
            {
                float t = (float)_rng.NextDouble();
                float xPct = Mathf.Lerp(21f, 79f, t);
                float cx = Proto_GreyboxLayout.ScreenXPercentToWorldX(xPct);
                var candidate = new Vector2(cx, footY);
                bool busy = false;
                foreach (var g in _grunts)
                {
                    if (!g.Target.IsAlive) continue;
                    if ((g.Target.FootPosition - candidate).sqrMagnitude < 0.12f)
                    {
                        busy = true;
                        break;
                    }
                }

                if (!busy) return candidate;
            }

            return new Vector2(_slotPositions[_grunts.Count % _slotPositions.Count].x, footY);
        }

        static List<Vector2> BuildDefaultSlots()
        {
            var list = new List<Vector2>();
            float footY = Proto_GreyboxLayout.ScreenYPercentToWorldY(37f);
            for (int i = 0; i < 8; i++)
            {
                float t = i / 7f;
                float xPct = Mathf.Lerp(21f, 79f, t);
                list.Add(new Vector2(Proto_GreyboxLayout.ScreenXPercentToWorldX(xPct), footY));
            }

            return list;
        }

        public void KillAllGruntsForTest()
        {
            foreach (var g in _grunts)
                g.Target.ApplyDamage(999);
        }
    }
}