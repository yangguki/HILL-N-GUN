using UnityEngine;
using HillNGun.Core;
using HillNGun.Combat;
using HillNGun.PvP;
using HillNGun.Characters;

namespace HillNGun.DebugTools
{
    /// <summary>
    /// v14 diagnostic (-logic-ab): fixed-step (Time.captureDeltaTime = 1/60) identical-seed PvP logic recorder.
    /// Hashes, per simulated tick, every character's logical position / hit radius / HP / state, every active projectile,
    /// and the gameplay RNG state; plus an event stream of logical projectile spawns (tick, origin, velocity, owner) and
    /// grenade throws. Rig on/off (-diag-norig) runs are compared offline from the logged trajectory.
    /// </summary>
    public class LogicABRecorder : MonoBehaviour
    {
        private const int MaxTicks = 5400; // 90 s simulated
        private int _tick, _spawns, _grenades, _uRandDelta;
        private ulong _stateHash = 1469598103934665603UL, _eventHash = 1469598103934665603UL;
        private CombatSystem _combat;
        private PvPMatchManager _pvp;
        private readonly System.Text.StringBuilder _firstSpawns = new System.Text.StringBuilder();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-logic-ab")) return;
            Time.captureDeltaTime = 1f / 60f; // frame-rate independent: every frame advances exactly one 1/60 s step
            var go = new GameObject("LogicABRecorder"); DontDestroyOnLoad(go); go.AddComponent<LogicABRecorder>();
            Debug.Log($"[LogicAB] armed seed-state={GameplayRng.State} rig={(System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == "-diag-norig") ? "off" : "on")} captureDeltaTime={Time.captureDeltaTime}");
        }

        private static void Mix(ref ulong h, ulong v) { for (int i = 0; i < 8; i++) { h ^= (v >> (i * 8)) & 0xFF; h *= 1099511628211UL; } }
        private static ulong F(float f) => (ulong)System.BitConverter.SingleToInt32Bits(f) & 0xFFFFFFFFUL;

        private void OnEnable() { ProjectilePool.DiagSpawned += OnSpawn; }
        private void OnDisable() { ProjectilePool.DiagSpawned -= OnSpawn; }

        private void OnSpawn(Projectile p)
        {
            if (_pvp == null) return;
            _spawns++;
            Mix(ref _eventHash, (ulong)_tick); Mix(ref _eventHash, F(p.Position.x)); Mix(ref _eventHash, F(p.Position.y));
            Mix(ref _eventHash, F(p.Direction.x * p.Speed)); Mix(ref _eventHash, F(p.Direction.y * p.Speed)); Mix(ref _eventHash, (ulong)p.OwnerTeam);
            Mix(ref _eventHash, (ulong)(p.Owner != null ? p.Owner.name.GetHashCode() : 0));
            if (_spawns <= 6) _firstSpawns.Append($"#{_spawns}@t{_tick} {p.Owner?.name} o=({p.Position.x:F4},{p.Position.y:F4}) v=({p.Direction.x * p.Speed:F4},{p.Direction.y * p.Speed:F4}); ");
        }

        private void OnGrenade(GrenadeProjectile g, Vector2 target)
        {
            _grenades++;
            Mix(ref _eventHash, 0xABCDUL); Mix(ref _eventHash, (ulong)_tick); Mix(ref _eventHash, F(g.StartPosition.x)); Mix(ref _eventHash, F(g.StartPosition.y));
            Mix(ref _eventHash, F(target.x)); Mix(ref _eventHash, F(target.y));
        }

        private void LateUpdate()
        {
            if (_pvp == null)
            {
                _pvp = FindFirstObjectByType<PvPMatchManager>();
                if (_pvp == null) return;
                _combat = FindFirstObjectByType<CombatSystem>();
                if (_combat != null) _combat.OnGrenadeThrown += OnGrenade;
            }
            _tick++;
            Hash(_pvp.PlayerTeam); Hash(_pvp.EnemyTeam);
            var list = _combat?.ProjectilePool?.ActiveProjectiles;
            if (list != null) foreach (var p in list) { if (!p.IsActive) continue; Mix(ref _stateHash, F(p.Position.x)); Mix(ref _stateHash, F(p.Position.y)); Mix(ref _stateHash, F(p.Speed)); }
            Mix(ref _stateHash, GameplayRng.State); Mix(ref _stateHash, (ulong)_pvp.CurrentPhase); Mix(ref _stateHash, F(_pvp.RoundTimer));
            if (_tick % 60 == 0)
                Debug.Log($"[LogicAB] tick={_tick} state={_stateHash:X16} events={_eventHash:X16} spawns={_spawns} grenades={_grenades} rngCalls={GameplayRng.Calls} round={_pvp.CurrentRound} phase={_pvp.CurrentPhase} hp={Hp(_pvp.PlayerTeam)}|{Hp(_pvp.EnemyTeam)}");
            if (_tick >= MaxTicks)
            {
                Debug.Log($"[LogicAB] FINAL ticks={_tick} state={_stateHash:X16} events={_eventHash:X16} spawns={_spawns} grenades={_grenades} rngCalls={GameplayRng.Calls} first[{_firstSpawns}]");
                Application.Quit();
                enabled = false;
            }
        }

        private void OnApplicationQuit()
        {
            if (enabled && _pvp != null) Debug.Log($"[LogicAB] FINAL(match-ended) ticks={_tick} state={_stateHash:X16} events={_eventHash:X16} spawns={_spawns} grenades={_grenades} rngCalls={GameplayRng.Calls} first[{_firstSpawns}]");
        }

        private void Hash(System.Collections.Generic.IReadOnlyList<CharacterStateMachine> t)
        {
            if (t == null) return;
            foreach (var c in t)
            {
                if (c == null) { Mix(ref _stateHash, 0); continue; }
                Mix(ref _stateHash, F(c.Position.x)); Mix(ref _stateHash, F(c.Position.y)); Mix(ref _stateHash, F(c.HitRadius));
                Mix(ref _stateHash, (ulong)c.CurrentHp); Mix(ref _stateHash, (ulong)c.OverallState);
            }
        }

        private static string Hp(System.Collections.Generic.IReadOnlyList<CharacterStateMachine> t)
        {
            var sb = new System.Text.StringBuilder();
            if (t != null) foreach (var c in t) sb.Append(c == null ? -1 : c.CurrentHp).Append(',');
            return sb.ToString();
        }
    }
}
