using UnityEngine;
using HillNGun.Core;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "enemy_", menuName = "HillNGun/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string nameKey;
        public EnemyType enemyType;

        [Header("Stats")]
        public int maxHp = 600;
        public float hitRadius = 0.5f;
        public float moveSpeed = 2f;

        [Header("Behavior")]
        public float warningTime = 0.5f;
        public float attackInterval = 1.5f;
        public int attackDamage = 120;

        [Header("Attack - Ranged")]
        public bool isRanged = true;
        public float attackRange = 10f;
        public float bulletSpeed = 8f;
        public float bulletRadius = 0.15f;
        public int projectilesPerShot = 1;
        public float spreadDeg = 0f;

        [Header("Attack - Thrown (Dynamite)")]
        public bool throwsExplosive = false;
        public float explosiveRadius = 2f;
        public int explosiveDamage = 450;
        public float explosiveFlightTime = 0.8f;

        [Header("Movement")]
        [Header("Bullet dodge (t48u)")]
        [Tooltip("Reaction to incoming player bullets: chance / delay / lookahead / margin / cooldown")]
        public HillNGun.Combat.DodgeTuning bulletDodge = new HillNGun.Combat.DodgeTuning { chance = 0f, delayMin = 0.15f, delayMax = 0.3f, lookahead = 0.55f, margin = 0.15f, cooldown = 3f };
        [Tooltip("Side-step distance (tiles) and speed (tiles/s)")]
        public float dodgeStepDistance = 1.2f;
        public float dodgeStepSpeed = 7f;

        public bool movesBetweenRows = false;
        public float rowChangeInterval = 5f;

        [Header("Scoring")]
        public int gaugeValue = 1;
        public int scoreValue = 100;

        [Header("Visual")]
        public Color baseColor = new Color(0.6f, 0.4f, 0.3f);
        public float scale = 1f;

        [Header("On Defeat")]
        public bool fleesOnDefeat = true;
        public float fleeSpeed = 5f;
    }
}
