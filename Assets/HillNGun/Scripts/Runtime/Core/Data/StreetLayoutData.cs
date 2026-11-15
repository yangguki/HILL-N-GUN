using UnityEngine;
using HillNGun.Core;

namespace HillNGun.Data
{
    /// <summary>One FreeMove PvE enemy entrance painted into the street plate (door, alley, street end, fence, window).</summary>
    [System.Serializable]
    public class StageSpawnPoint
    {
        public string id;
        public SpawnPointType type = SpawnPointType.StorefrontDoor;
        [Tooltip("Screen side the enemy enters from (waves use at least two sides)")]
        public SpawnSide side = SpawnSide.Left;
        [Tooltip("Logic position (actor centre) where the entrance ends and the enemy becomes active")]
        public Vector2 position;
        [Tooltip("Logic position the entrance path starts from (doorway / alley mouth / off-screen / behind the fence / window sill)")]
        public Vector2 entranceStart;
        [Tooltip("Facing / walk-out direction")]
        public Vector2 direction = Vector2.down;
        [Tooltip("Telegraph before the enemy is visible ('!' + shake at the door / window). 0 = none")]
        public float telegraphTime = 0f;
        [Tooltip("Entrance path duration (walk out / climb / rise). The enemy ignores hits until it ends")]
        public float entranceTime = 0.5f;
        [Tooltip("Where the telegraph marker is drawn (logic). Zero = entranceStart")]
        public Vector2 telegraphPosition;

        public Vector2 TelegraphPos => telegraphPosition != Vector2.zero ? telegraphPosition : entranceStart;
        public bool IsSniperPost => type == SpawnPointType.BalconySniper;
    }

    /// <summary>
    /// FreeMove street layout for one painted BG composition (all four time-of-day plates share it): walkable area,
    /// enemy post zone and spawn points. Logic tiles, actor-centre coordinates (the painted foot line is
    /// <see cref="footOffset"/> below the centre). Referenced by StageVisualData.streetLayout; authored by FreeMoveSetup.
    /// </summary>
    [CreateAssetMenu(fileName = "street_layout_", menuName = "HillNGun/Data/Street Layout")]
    public class StreetLayoutData : ScriptableObject
    {
        public string id;
        [Tooltip("Walkable polygon (CCW, simple), actor-centre coordinates. Top = back fence/gate line, bottom = front fence, sides = boardwalk/street edges")]
        public Vector2[] walkPolygon;
        [Tooltip("Painted foot line is this far below the actor centre (documentation + authoring; collision uses centres)")]
        public float footOffset = 0.8f;
        [Tooltip("Enemy posts are picked inside this rect (clipped by the walk polygon)")]
        public Rect enemyPostZone = new Rect(1f, 8.6f, 22f, 5f);
        [Tooltip("Keep enemy posts this far from the walk polygon edge")]
        public float enemyPostEdgeMargin = 0.8f;
        public StageSpawnPoint[] spawnPoints;
        [Tooltip("Boss walk-in path: start (behind the gate) → battle position (BossData.battlePosition)")]
        public Vector2 bossEntryStart = new Vector2(11.6f, 19.5f);

        public StageSpawnPoint Find(string pointId)
        {
            if (spawnPoints == null || string.IsNullOrEmpty(pointId)) return null;
            foreach (var p in spawnPoints) if (p != null && p.id == pointId) return p;
            return null;
        }

        public int IndexOf(string pointId)
        {
            if (spawnPoints == null) return -1;
            for (int i = 0; i < spawnPoints.Length; i++) if (spawnPoints[i] != null && spawnPoints[i].id == pointId) return i;
            return -1;
        }
    }
}
