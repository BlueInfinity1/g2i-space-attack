using UnityEngine;
using UnityEngine.UI;

namespace SpaceAttack
{
    public sealed class FeedbackController : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private Transform view;
        [SerializeField] private Transform effectsRoot;
        [SerializeField] private Transform starfield;
        [SerializeField] private EffectParticle sparkPrefab;
        [SerializeField] private Image screenFlash;
        [SerializeField] private Text comfortText;
        [SerializeField] private RectTransform scoreRoot;
        public bool ReducedEffects { get; private set; }
        public bool Muted { get; private set; }
        public event System.Action PreferencesChanged;
        private FeedbackPreferences preferences;
        private FeedbackConfig settings;
        private AudioSource source;
        private AudioClip shotSound, enemyShotSound, killSound, damageSound;
        private Vector3 cameraOrigin, starOrigin;
        private float shakeRemaining, flashRemaining;
        private bool particlesWereEnabled = true;
        private Renderer[] playerParts;
        private Color[] playerColors;
        private MaterialPropertyBlock properties;

        private void Awake()
        {
            properties = new MaterialPropertyBlock();
            settings = game.Config.feedback;
            preferences = new FeedbackPreferences(settings);
        }

        private void Start()
        {
            settings = game.Config.feedback;
            cameraOrigin = view.localPosition;
            starOrigin = starfield.localPosition;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            ApplyVolume();
            shotSound = Tone("Player laser", 0.11f, 1300f, 550f, 0.45f, true);
            enemyShotSound = Tone("Enemy laser", 0.16f, 650f, 180f, 0.3f, true);
            killSound = Tone("Enemy down", 0.1f, 340f, 90f, 0.45f);
            damageSound = Tone("Player damage", 0.18f, 125f, 35f, 0.5f);
            playerParts = game.Player.GetComponentsInChildren<Renderer>(true);
            playerColors = new Color[playerParts.Length];
            for (int i = 0; i < playerParts.Length; i++) playerColors[i] = playerParts[i].sharedMaterial.color;
            game.EnemyDefeated += OnEnemyDefeated;
            game.PlayerDamaged += OnPlayerDamaged;
            game.ShotFired += OnShot;
            game.EnemyShotFired += OnEnemyShot;
            game.StateChanged += OnStateChanged;
            UpdateComfortLabel();
        }

        private void Update()
        {
            if (settings == null) return;
            if (Input.GetKeyDown(KeyCode.F)) SetReducedEffects(!ReducedEffects);
            if (Input.GetKeyDown(KeyCode.M)) SetMuted(!Muted);
            ApplyVolume();
            bool particlesEnabled = GetPreference(FeedbackSetting.Particles) > 0f;
            if (!particlesEnabled && particlesWereEnabled) ClearSparks();
            particlesWereEnabled = particlesEnabled;
        }

        private void LateUpdate()
        {
            if (settings == null) return;
            shakeRemaining = Mathf.Max(0f, shakeRemaining - Time.deltaTime);
            flashRemaining = Mathf.Max(0f, flashRemaining - Time.deltaTime);
            float shake = shakeRemaining > 0f ? GetPreference(FeedbackSetting.Shake) * 0.18f *
                (shakeRemaining / Mathf.Max(0.001f, settings.screenShakeDurationSeconds)) : 0f;
            // Deterministic oscillation avoids modifying the random sequence used by gameplay.
            view.localPosition = cameraOrigin + new Vector3(Mathf.Sin(Time.time * 91f), Mathf.Cos(Time.time * 107f), 0) * shake;
            float flashAge = Mathf.Max(settings.hitFlashDurationSeconds, settings.screenFlashDurationSeconds) - flashRemaining;
            float shipFlash = settings.hitFlashDurationSeconds > 0f ?
                GetPreference(FeedbackSetting.HitFlash) * Mathf.Clamp01(1f - flashAge / settings.hitFlashDurationSeconds) : 0f;
            if (flashRemaining <= 0f) shipFlash = 0f;
            float alpha = 1f;
            if (game.State == GameState.Playing && game.Player.Health.IsInvulnerable)
            {
                float blink = GetPreference(FeedbackSetting.InvulnerabilityBlink);
                int phase = Mathf.FloorToInt(game.Player.Health.InvulnerabilityElapsed /
                    Mathf.Max(0.04f, settings.invulnerabilityBlinkIntervalSeconds));
                // When blinking is disabled, steady transparency still communicates invulnerability.
                alpha = blink <= 0f ? settings.invulnerabilityMinimumAlpha :
                    (phase % 2 == 0 ? Mathf.Lerp(1f, settings.invulnerabilityMinimumAlpha, blink) : 1f);
            }
            for (int i = 0; i < playerParts.Length; i++)
            {
                Color tint = Color.Lerp(playerColors[i], Color.white, shipFlash);
                tint.a = alpha;
                properties.SetColor("_Color", tint);
                playerParts[i].SetPropertyBlock(properties);
            }
            float overlay = flashRemaining > 0f && settings.screenFlashDurationSeconds > 0f ?
                GetPreference(FeedbackSetting.ScreenFlash) * Mathf.Clamp01(1f - flashAge / settings.screenFlashDurationSeconds) : 0f;
            screenFlash.color = new Color(1f, 0.15f, 0.2f, overlay * 0.3f);
            starfield.localPosition = starOrigin + Vector3.down *
                (Mathf.Sin(Time.time * 0.12f) * GetPreference(FeedbackSetting.BackgroundMotion) * 0.7f);
        }

        public float GetPreference(FeedbackSetting setting) => ReducedEffects && (int)setting < (int)FeedbackSetting.MasterVolume ?
            0f : preferences[setting];

        public void SetPreference(FeedbackSetting setting, float value)
        {
            if (ReducedEffects && (int)setting < (int)FeedbackSetting.MasterVolume)
            {
                for (int i = 0; i < (int)FeedbackSetting.MasterVolume; i++) preferences[(FeedbackSetting)i] = 0f;
                ReducedEffects = false;
            }
            preferences[setting] = value;
            if (setting == FeedbackSetting.Particles && value <= 0f) ClearSparks();
            ApplyVolume();
            UpdateComfortLabel();
            PreferencesChanged?.Invoke();
        }

        public void ResetPreferences()
        {
            preferences = new FeedbackPreferences(settings);
            ReducedEffects = false;
            SetMuted(false);
            ApplyVolume();
            PreferencesChanged?.Invoke();
        }

        private void ApplyVolume()
        {
            if (source != null) source.volume = Muted ? 0f :
                GetPreference(FeedbackSetting.MasterVolume) * GetPreference(FeedbackSetting.SfxVolume);
        }

        public void SetReducedEffects(bool reduced)
        {
            ReducedEffects = reduced;
            if (reduced)
            {
                shakeRemaining = flashRemaining = 0f;
                ClearSparks();
            }
            UpdateComfortLabel();
            PreferencesChanged?.Invoke();
        }

        public void SetMuted(bool muted)
        {
            Muted = muted;
            if (source != null && muted) { source.Stop(); source.volume = 0f; }
            ApplyVolume();
            UpdateComfortLabel();
        }

        private void OnShot() => Play(shotSound);
        private void OnEnemyShot() => Play(enemyShotSound);
        private void ClearSparks()
        {
            foreach (EffectParticle spark in effectsRoot.GetComponentsInChildren<EffectParticle>())
                Destroy(spark.gameObject);
        }
        private void Play(AudioClip clip)
        {
            ApplyVolume();
            if (clip != null && source.volume > 0f) source.PlayOneShot(clip);
        }

        private void OnEnemyDefeated(Vector2 position, Color color, int score)
        {
            Play(killSound);
            int count = Mathf.RoundToInt(GetPreference(FeedbackSetting.Particles) * 8);
            for (int i = 0; i < count; i++)
            {
                float angle = (i * 2.399963f + score * 0.01f);
                var spark = Instantiate(sparkPrefab, new Vector3(position.x, position.y, -0.3f), Quaternion.Euler(0, 0, i * 40), effectsRoot);
                spark.Initialize(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (0.8f + i * 0.12f), color, 0.3f + i * 0.018f);
            }
            ShowScore(position + Vector2.up * 0.28f, score);
        }

        public FloatingScore ShowScore(Vector2 position, int score)
        {
            var popup = new GameObject("Score +" + score, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Text), typeof(Outline), typeof(FloatingScore));
            popup.transform.SetParent(scoreRoot, false);
            var floating = popup.GetComponent<FloatingScore>();
            floating.Initialize(score, position, view.GetComponent<Camera>(), scoreRoot.GetComponentInParent<Canvas>());
            return floating;
        }

        private void OnPlayerDamaged()
        {
            Play(damageSound);
            if (ReducedEffects) return;
            shakeRemaining = settings.screenShakeDurationSeconds;
            flashRemaining = Mathf.Max(settings.hitFlashDurationSeconds, settings.screenFlashDurationSeconds);
        }

        private void OnStateChanged(GameState state)
        {
            shakeRemaining = flashRemaining = 0f;
            if (state != GameState.Playing) return;
            for (int i = effectsRoot.childCount - 1; i >= 0; i--) Destroy(effectsRoot.GetChild(i).gameObject);
            for (int i = scoreRoot.childCount - 1; i >= 0; i--) Destroy(scoreRoot.GetChild(i).gameObject);
        }

        private void UpdateComfortLabel()
        {
            comfortText.text = $"F  REDUCED EFFECTS: {(ReducedEffects ? "ON" : "OFF")}     /     M  AUDIO: {(Muted ? "MUTED" : "ON")}";
        }

        private static AudioClip Tone(string name, float duration, float fromHz, float toHz, float amplitude, bool harmonics = false)
        {
            const int rate = 22050;
            float[] samples = new float[Mathf.CeilToInt(duration * rate)];
            float phase = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)samples.Length;
                phase += 2f * Mathf.PI * Mathf.Lerp(fromHz, toHz, t) / rate;
                float wave = harmonics ? (Mathf.Sin(phase) + 0.22f * Mathf.Sin(2f * phase) +
                    0.12f * Mathf.Sin(3f * phase)) / 1.34f : Mathf.Sin(phase);
                samples[i] = wave * amplitude * (1f - t) * Mathf.Min(1f, t * 25f);
            }
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.EnemyDefeated -= OnEnemyDefeated;
                game.PlayerDamaged -= OnPlayerDamaged;
                game.ShotFired -= OnShot;
                game.EnemyShotFired -= OnEnemyShot;
                game.StateChanged -= OnStateChanged;
            }
            if (settings != null && view != null) view.localPosition = cameraOrigin;
            if (shotSound != null) Destroy(shotSound);
            if (enemyShotSound != null) Destroy(enemyShotSound);
            if (killSound != null) Destroy(killSound);
            if (damageSound != null) Destroy(damageSound);
        }
    }
}
