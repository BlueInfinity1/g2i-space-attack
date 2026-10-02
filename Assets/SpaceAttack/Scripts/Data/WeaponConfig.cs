using UnityEngine;

namespace SpaceAttack
{
    [CreateAssetMenu(menuName = "Space Attack/Weapon Config")]
    public sealed class WeaponConfig : ScriptableObject
    {
        [Min(0.02f)] public float fireIntervalSeconds = 0.18f;
        [Min(0.1f)] public float projectileSpeed = 24f;
        [Min(1)] public int projectileDamage = 1;
        [Min(0.1f)] public float projectileLifetimeSeconds = 1.3f;
    }
}
