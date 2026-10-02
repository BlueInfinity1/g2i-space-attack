using UnityEngine;

namespace SpaceAttack
{
    [CreateAssetMenu(menuName = "Space Attack/Scoring Config")]
    public sealed class ScoringConfig : ScriptableObject
    {
        [Min(1f)] public float attackMinMultiplier = 4f;
        [Min(1f)] public float attackMaxMultiplier = 10f;
        [Min(1)] public int scoreRoundingStep = 10;
    }
}
