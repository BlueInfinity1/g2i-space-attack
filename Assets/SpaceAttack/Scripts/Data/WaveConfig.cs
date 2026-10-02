using UnityEngine;

namespace SpaceAttack
{
    [CreateAssetMenu(menuName = "Space Attack/Wave Config")]
    public sealed class WaveConfig : ScriptableObject
    {
        [Min(0f)] public float attackStartDelaySeconds = 1.6f;
        [Min(0.05f)] public float attackIntervalSeconds = 0.95f;
        [Min(1)] public int maxConcurrentAttackers = 8;
        [Min(0.1f)] public float interWaveDelaySeconds = 1.8f;
        [Min(0f)] public float difficultyIncreasePerWave = 0.2f;
        [Min(1f)] public float maxDifficultyMultiplier = 3f;
        public float formationMinOffsetX = -2.5f;
        public float formationMaxOffsetX = 2.5f;
        [Min(0f)] public float formationSpeed = 0.4f;

        public float Difficulty(int stage) => Mathf.Min(1f + Mathf.Max(0, stage - 1) * difficultyIncreasePerWave,
            Mathf.Max(1f, maxDifficultyMultiplier));
        public float FormationSpeed(int stage) => Mathf.Max(0f, formationSpeed) * Difficulty(stage);

        public float AttackInterval(int stage)
        {
            return Mathf.Max(0.05f, attackIntervalSeconds / Difficulty(stage));
        }
    }
}
