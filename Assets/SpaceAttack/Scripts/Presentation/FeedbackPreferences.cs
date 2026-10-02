using UnityEngine;

namespace SpaceAttack
{
    public enum FeedbackSetting { Shake, HitFlash, ScreenFlash, Particles, BackgroundMotion, InvulnerabilityBlink, MasterVolume, SfxVolume }

    // Per-session values: adjusting the menu never writes to shared config assets.
    public sealed class FeedbackPreferences
    {
        private readonly float[] values = new float[8];
        public float this[FeedbackSetting setting]
        {
            get => values[(int)setting];
            set => values[(int)setting] = Mathf.Clamp01(value);
        }

        public FeedbackPreferences(FeedbackConfig config)
        {
            this[FeedbackSetting.Shake] = config.screenShakeIntensity;
            this[FeedbackSetting.HitFlash] = config.hitFlashIntensity;
            this[FeedbackSetting.ScreenFlash] = config.screenFlashIntensity;
            this[FeedbackSetting.Particles] = config.explosionParticleAmount;
            this[FeedbackSetting.BackgroundMotion] = config.backgroundMotionIntensity;
            this[FeedbackSetting.InvulnerabilityBlink] = config.invulnerabilityBlinkIntensity;
            this[FeedbackSetting.MasterVolume] = config.masterVolume;
            this[FeedbackSetting.SfxVolume] = config.sfxVolume;
        }
    }
}
