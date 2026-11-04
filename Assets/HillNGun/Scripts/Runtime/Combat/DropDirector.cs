using System;
using System.Collections.Generic;
using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Characters;
using HillNGun.Cover;
using HillNGun.Battlefield;
using HillNGun.Feel;

namespace HillNGun.Combat
{
    /// <summary>
    /// Match glue for field drops (t48u): listens for destroyed covers (CombatSystem) and defeated enemies (PvE manager
    /// calls <see cref="HandleEnemyDefeated"/>), rolls DropTableData, picks a landing spot on a reachable row, ticks
    /// pickups and plays the destruction / pickup feedback. Logic lives in <see cref="DropSystem"/>; DropView draws.
    /// PvE: everything is tossed to the player team. PvP: a destroyed cover drops for the team that broke it.
    /// </summary>
    public class DropDirector : MonoBehaviour
    {
        public static DropDirector Instance { get; private set; }

        private DropSystem _system;
        private CombatSystem _combat;
        private GameMode _mode;
        private Func<IReadOnlyList<CharacterStateMachine>> _pickers;
        private Func<CharacterStateMachine> _controlled;
        private Feel.DropView _view;

        public DropSystem System => _system;
        public int CoverDrops { get; private set; }
        public int EnemyDrops { get; private set; }

        public static DropDirector Begin(GameMode mode, Func<IReadOnlyList<CharacterStateMachine>> pickers, Func<CharacterStateMachine> controlled)
        {
            if (Instance == null) Instance = new GameObject("[DropDirector]").AddComponent<DropDirector>();
            Instance.Setup(mode, pickers, controlled);
            return Instance;
        }

        private void Setup(GameMode mode, Func<IReadOnlyList<CharacterStateMachine>> pickers, Func<CharacterStateMachine> controlled)
        {
            var table = ServiceLocator.Get<DropTableData>() ?? RuntimeAssetManager.Instance?.DropTable;
            if (table == null) table = ScriptableObject.CreateInstance<DropTableData>();
            _mode = mode;
            _pickers = pickers;
            _controlled = controlled;
            if (_system == null || _system.Table != table)
            {
                _system = new DropSystem(table);
                _system.OnPicked += HandlePicked;
            }
            else _system.Clear();
            Hook();
            if (_view == null) _view = Feel.DropView.Create(transform);
            _view.Bind(_system);
        }

        private void Hook()
        {
            var c = ServiceLocator.Get<CombatSystem>();
            if (c == _combat) return;
            if (_combat != null) _combat.OnCoverDestroyed -= HandleCoverDestroyed;
            _combat = c;
            if (_combat != null) _combat.OnCoverDestroyed += HandleCoverDestroyed;
        }

        private void OnDestroy()
        {
            if (_combat != null) _combat.OnCoverDestroyed -= HandleCoverDestroyed;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_system == null) return;
            if (_combat == null) Hook();
            _system.Tick(Time.deltaTime, _pickers?.Invoke());
        }

        /// <summary>Stage / round change: clear what is on the ground.</summary>
        public void ClearField() => _system?.Clear();

        // ------------------------------------------------------------------ sources
        private void HandleCoverDestroyed(CoverInstance cover)
        {
            if (cover == null) return;
            var bounds = cover.GetBounds();
            EffectsManager.Instance?.SpawnCoverDebris(cover.Position, bounds.width, true);
            if (_system == null) return;
            var bf = ServiceLocator.Get<BattlefieldSystem>();
            Team to = _mode == GameMode.PvP ? (cover.Team == Team.Player ? Team.Enemy : Team.Player) : Team.Player;
            float y;
            if (bf == null) y = cover.Position.y;
            else if (to == cover.Team) y = bf.GetRowY(bf.GetNearestRowIndex(cover.Position.y, to), to);
            else y = FrontRowY(bf, to);
            var it = _system.TryDrop(_system.Table.coverDropChance, cover.Position, Land(bf, cover.Position.x, y), to);
            if (it != null) CoverDrops++;
        }

        /// <summary>PvE: a defeated enemy may toss loot to the player's row.</summary>
        public void HandleEnemyDefeated(Vector2 position, int gaugeValue)
        {
            if (_system == null) return;
            var t = _system.Table;
            float chance = gaugeValue >= t.eliteGaugeValue ? t.eliteDropChance : t.enemyDropChance;
            var bf = ServiceLocator.Get<BattlefieldSystem>();
            var pc = _controlled?.Invoke();
            float y = pc != null && bf != null ? bf.GetRowY(bf.GetNearestRowIndex(pc.Position.y, Team.Player), Team.Player)
                    : bf != null ? FrontRowY(bf, Team.Player) : 4.5f;
            var it = _system.TryDrop(chance, position, Land(bf, position.x, y), Team.Player);
            if (it != null) EnemyDrops++;
        }

        private static float FrontRowY(BattlefieldSystem bf, Team team)
        {
            var d = bf.Data;
            if (d == null) return team == Team.Player ? 7f : 11f;
            return team == Team.Player ? d.GetOurRowY(d.OurRowCount - 1) : d.GetEnemyRowY(0);
        }

        private Vector2 Land(BattlefieldSystem bf, float x, float y)
        {
            float s = _system.Table.landScatter;
            float lx = x + UnityEngine.Random.Range(-s, s);
            float min = bf?.Data != null ? bf.Data.boundaryMinX + 0.6f : 1f;
            float max = bf?.Data != null ? bf.Data.boundaryMaxX - 0.6f : 23f;
            return new Vector2(Mathf.Clamp(lx, min, max), y);
        }

        // ------------------------------------------------------------------ feedback
        private void HandlePicked(DropItem it, CharacterStateMachine c)
        {
            string label; Color col = it.Entry.color;
            switch (it.Type)
            {
                case DropType.Heal: label = $"+{Mathf.RoundToInt(it.Entry.value * 100f)}% HP"; break;
                case DropType.Ammo: label = "AMMO"; break;
                case DropType.Grenade: label = "+1 TNT"; break;
                case DropType.WeaponBoost: label = $"x{it.Entry.value:0.##} DMG {it.Entry.duration:0}s"; break;
                default: label = $"+${it.Entry.amount}"; break;
            }
            EffectsManager.Instance?.SpawnFloatingText(c.Position + new Vector2(0f, 1.2f), label, col);
            if (it.Type == DropType.Coins)
            {
                var s = GameFlowManager.Instance?.CurrentSession;
                if (s != null) s.bonusBounty += Mathf.Max(0, it.Entry.amount);
            }
            Debug.Log($"[Drops] {c.name} picked {it.Type} at {it.Position} ({_system.Summary})");
        }
    }
}
