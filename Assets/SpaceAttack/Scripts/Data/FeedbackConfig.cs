using UnityEngine;

namespace SpaceAttack
{
    [CreateAssetMenu(menuName = "Space Attack/Feedback Config")]
    public sealed class FeedbackConfig : ScriptableObject
    {
        [Range(0f, 1f)] public float screenShakeIntensity = 0.2f;
        [Min(0f)] public float screenShakeDurationSeconds = 0.15f;
        [Range(0f, 1f)] public float hitFlashIntensity = 0.25f;
        [Min(0f)] public float hitFlashDurationSeconds = 0.1f;
        [Range(0f, 1f)] public float screenFlashIntensity = 0f;
        [Min(0f)] public float screenFlashDurationSeconds = 0.1f;
        [Range(0f, 1f)] public float explosionParticleAmount = 0.6f;
        [Range(0f, 1f)] public float backgroundMotionIntensity = 0.15f;
        [Range(0f, 1f)] public float invulnerabilityBlinkIntensity = 1f;
        [Range(0.04f, 0.5f)] public float invulnerabilityBlinkIntervalSeconds = 0.12f;
        [Range(0.1f, 1f)] public float invulnerabilityMinimumAlpha = 0.5f;
        [Range(0f, 1f)] public float masterVolume = 0.9f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
    }
}
