using UnityEngine;

namespace SpaceAttack
{
    [CreateAssetMenu(menuName = "Space Attack/Player Config")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [Min(0.1f)] public float moveSpeed = 10f;
        [Min(1)] public int maxHealth = 3;
        [Min(0f)] public float invulnerabilitySeconds = 1.2f;
        public WeaponConfig weapon;
    }
}
