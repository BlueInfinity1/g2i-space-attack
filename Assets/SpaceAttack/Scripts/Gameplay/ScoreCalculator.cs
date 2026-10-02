using UnityEngine;

namespace SpaceAttack
{
    public static class ScoreCalculator
    {
        public static int Calculate(int baseScore, bool attacking, float enemyY,
            float formationBottomY, float dangerLineY, ScoringConfig config)
        {
            if (!attacking) return baseScore;
            float span = formationBottomY - dangerLineY;
            float progress = span > 0f ? Mathf.Clamp01((formationBottomY - enemyY) / span) : 0f;
            float minimum = Mathf.Max(1f, config.attackMinMultiplier);
            float multiplier = Mathf.Lerp(minimum, Mathf.Max(minimum, config.attackMaxMultiplier), progress);
            int step = Mathf.Max(1, config.scoreRoundingStep);
            return step * Mathf.FloorToInt(baseScore * multiplier / step + 0.5f);
        }
    }
}
