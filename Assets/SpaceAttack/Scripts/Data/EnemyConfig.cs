using UnityEngine;

namespace SpaceAttack
{
    public enum EnemyKind { Red, Green, Yellow }

    [CreateAssetMenu(menuName = "Space Attack/Enemy Config")]
    public sealed class EnemyConfig : ScriptableObject
    {
        public EnemyKind kind;
        [Min(0.1f)] public float attackSpeed = 3.6f;
        [Range(5f, 80f)] public float attackAngleFromVerticalDegrees = 35f;
        [Min(0f)] public float directionChangeDelaySeconds = 0.8f;
        [Min(0.05f)] public float directionCheckIntervalSeconds = 0.4f;
        [Range(0f, 1f)] public float directionChangeInitialChance = 0.06f;
        [Min(0f)] public float directionChanceGrowthPerSecond = 0.06f;
        [Range(0f, 1f)] public float directionChangeMaximumChance = 0.4f;
        public WeaponConfig weapon;
        [Min(1)] public int contactDamage = 1;
        [Min(1)] public int formationScore = 30;
        public Color color = new Color(1f, 0.22f, 0.32f);
        public float DirectionChangeChance(float attackAge) => attackAge < directionChangeDelaySeconds ? 0f :
            Mathf.Clamp(directionChangeInitialChance + (attackAge - directionChangeDelaySeconds) *
                directionChanceGrowthPerSecond, 0f, Mathf.Clamp01(directionChangeMaximumChance));
        // Enemy health is deliberately not configurable: every enemy dies in one hit.
    }
}
