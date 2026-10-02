using UnityEngine;

namespace SpaceAttack
{
    [CreateAssetMenu(menuName = "Space Attack/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        public PlayerConfig player;
        public EnemyConfig redEnemy;
        public EnemyConfig greenEnemy;
        public EnemyConfig yellowEnemy;
        public WaveConfig waves;
        public ScoringConfig scoring;
        public FeedbackConfig feedback;
    }
}
