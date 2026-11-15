using System.Collections.Generic;
using System.Text;
using UnityEngine;
using HillNGun.Characters;
using HillNGun.PvE;

namespace HillNGun.Proto.TopDown
{
    /// <summary>Logic fields a skin swap must not touch: position, HP, ammo, grenades, and every enemy.</summary>
    public struct ProtoLogicStamp
    {
        public Vector2 Position;
        public int Hp;
        public int AmmoCells;
        public int Grenades;
        public int EnemyCount;
        public Vector2[] EnemyPositions;
        public int[] EnemyHp;

        public static ProtoLogicStamp Capture(CharacterStateMachine player, IReadOnlyList<EnemyInstance> enemies)
        {
            int n = enemies != null ? enemies.Count : 0;
            var stamp = new ProtoLogicStamp
            {
                Position = player != null ? player.Position : Vector2.zero,
                Hp = player != null ? player.CurrentHp : 0,
                AmmoCells = player != null && player.AmmoGauge != null ? player.AmmoGauge.CurrentCells : -1,
                Grenades = player != null && player.GrenadeSystem != null ? player.GrenadeSystem.CurrentGrenades : -1,
                EnemyCount = n,
                EnemyPositions = new Vector2[n],
                EnemyHp = new int[n]
            };
            for (int i = 0; i < n; i++)
            {
                var e = enemies[i];
                stamp.EnemyPositions[i] = e != null ? e.Position : Vector2.zero;
                stamp.EnemyHp[i] = e != null ? e.CurrentHp : 0;
            }
            return stamp;
        }

        public bool Same(ProtoLogicStamp other)
        {
            if (Position != other.Position || Hp != other.Hp || AmmoCells != other.AmmoCells || Grenades != other.Grenades || EnemyCount != other.EnemyCount)
                return false;
            for (int i = 0; i < EnemyCount; i++)
                if (EnemyPositions[i] != other.EnemyPositions[i] || EnemyHp[i] != other.EnemyHp[i]) return false;
            return true;
        }

        public string Diff(ProtoLogicStamp other)
        {
            var sb = new StringBuilder();
            if (Position != other.Position) sb.Append($" pos {Position}->{other.Position}");
            if (Hp != other.Hp) sb.Append($" hp {Hp}->{other.Hp}");
            if (AmmoCells != other.AmmoCells) sb.Append($" ammo {AmmoCells}->{other.AmmoCells}");
            if (Grenades != other.Grenades) sb.Append($" grenades {Grenades}->{other.Grenades}");
            if (EnemyCount != other.EnemyCount) sb.Append($" enemies {EnemyCount}->{other.EnemyCount}");
            int n = Mathf.Min(EnemyCount, other.EnemyCount);
            for (int i = 0; i < n; i++)
            {
                if (EnemyPositions[i] != other.EnemyPositions[i]) sb.Append($" e{i} pos {EnemyPositions[i]}->{other.EnemyPositions[i]}");
                if (EnemyHp[i] != other.EnemyHp[i]) sb.Append($" e{i} hp {EnemyHp[i]}->{other.EnemyHp[i]}");
            }
            return sb.Length == 0 ? "same" : sb.ToString();
        }
    }

    /// <summary>Activates one view root. Does not read or write gameplay state.</summary>
    public static class ProtoSkinSwitch
    {
        public static void Apply(GameObject pixelRoot, GameObject dioramaRoot, bool pixel)
        {
            if (pixelRoot != null) pixelRoot.SetActive(pixel);
            if (dioramaRoot != null) dioramaRoot.SetActive(!pixel);
        }
    }
}
