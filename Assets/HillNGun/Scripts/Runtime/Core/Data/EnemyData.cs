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
        public int projectilesPerShot = 1;
        public float spreadDeg = 0f;

        [Header("Attack - Thrown (Dynamite)")]
        public bool throwsExplosive = false;
        public float explosiveRadius = 2f;
        public int explosiveDamage = 450;
        public float explosiveFlightTime = 0.8f;

        [Header("Movement")]
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
