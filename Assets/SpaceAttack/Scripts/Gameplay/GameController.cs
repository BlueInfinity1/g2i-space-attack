using System;
using UnityEngine;

namespace SpaceAttack
{
    public enum GameState { StartScreen, Playing, GameOver }

    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private PlayerController player;
        [SerializeField] private WaveDirector waves;
        [SerializeField] private PlayfieldBounds playfield;
        [SerializeField] private HUDView hud;
        [SerializeField] private Transform projectilesRoot;
        public GameState State { get; private set; }
        public int Score { get; private set; }
        public int BestScore { get; private set; }
        public int Kills { get; private set; }
        public int Escaped { get; private set; }
        public GameConfig Config => config;
        public PlayerController Player => player;
        public WaveDirector Waves => waves;
        public PlayfieldBounds Playfield => playfield;
        public event Action<Vector2, Color, int> EnemyDefeated;
        public event Action PlayerDamaged;
        public event Action ShotFired;
        public event Action EnemyShotFired;
        public event Action<GameState> StateChanged;
        public bool SettingsOpen { get; private set; }
        private float timeScaleBeforeSettings = 1f;

        private void Start()
        {
            Application.targetFrameRate = 120;
            player.Initialize(config.player, playfield, projectilesRoot);
            waves.Initialize(config, playfield);
            player.Health.Changed += Refresh;
            player.Health.Damaged += OnPlayerDamaged;
            player.Health.Died += EndRun;
            player.Weapon.Fired += OnShotFired;
            waves.EnemyResolved += OnEnemyResolved;
            waves.StageStarted += OnStageStarted;
            waves.StageCleared += OnStageCleared;
            waves.EnemyShotFired += OnEnemyShotFired;
            hud.Bind(this);
            State = GameState.StartScreen;
            hud.ShowStart();
            Refresh();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) SetSettingsOpen(!SettingsOpen);
            if (SettingsOpen) return;
            if (State != GameState.Playing && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                StartRun();
            else if (State == GameState.GameOver && Input.GetKeyDown(KeyCode.R))
                StartRun();
        }

        public void StartRun()
        {
            if (State == GameState.Playing || SettingsOpen) return;
            ClearProjectiles();
            Score = 0;
            Kills = 0;
            Escaped = 0;
            player.ResetPlayer();
            State = GameState.Playing;
            player.SetPlaying(true);
            hud.ShowRun();
            waves.BeginRun();
            Refresh();
            StateChanged?.Invoke(State);
        }

        private void OnEnemyResolved(EnemyResolution resolution)
        {
            if (State != GameState.Playing) return;
            if (resolution.PlayerKill)
            {
                int points = ScoreCalculator.Calculate(resolution.Config.formationScore, resolution.WasAttacking,
                    resolution.Position.y, waves.FormationBottomY, playfield.dangerLineY, config.scoring);
                Score += points;
                BestScore = Mathf.Max(Score, BestScore);
                Kills++;
                EnemyDefeated?.Invoke(resolution.Position, resolution.Config.color, points);
            }
            else Escaped++;
            Refresh();
        }

        private void OnStageStarted(int stage)
        {
            ClearProjectiles();
            hud.ClearAnnouncement();
            Refresh();
        }

        private void OnStageCleared() => hud.Announce("SECTOR CLEAR   /   NEXT FORMATION INCOMING", config.waves.interWaveDelaySeconds);
        private void OnShotFired() => ShotFired?.Invoke();
        private void OnEnemyShotFired() => EnemyShotFired?.Invoke();
        private void OnPlayerDamaged() => PlayerDamaged?.Invoke();

        public void EndRun()
        {
            if (State != GameState.Playing) return;
            State = GameState.GameOver;
            player.SetPlaying(false);
            waves.StopRun();
            ClearProjectiles();
            hud.ShowGameOver(this);
            Refresh();
            StateChanged?.Invoke(State);
        }

        public void ClearProjectiles()
        {
            for (int i = projectilesRoot.childCount - 1; i >= 0; i--)
            {
                GameObject shot = projectilesRoot.GetChild(i).gameObject;
                shot.SetActive(false);
                Destroy(shot);
            }
        }

        private void Refresh() => hud.Refresh(this);

        public void ToggleSettings() => SetSettingsOpen(!SettingsOpen);

        public void SetSettingsOpen(bool open)
        {
            if (SettingsOpen == open) return;
            SettingsOpen = open;
            if (open)
            {
                timeScaleBeforeSettings = Time.timeScale;
                Time.timeScale = 0f;
                player.SetPlaying(false);
            }
            else
            {
                Time.timeScale = timeScaleBeforeSettings;
                player.SetPlaying(State == GameState.Playing);
            }
            hud.ShowSettings(open);
        }

        private void OnDisable()
        {
            if (SettingsOpen) Time.timeScale = timeScaleBeforeSettings;
            SettingsOpen = false;
        }

        private void OnDestroy()
        {
            if (player != null && player.Health != null)
            {
                player.Health.Changed -= Refresh;
                player.Health.Damaged -= OnPlayerDamaged;
                player.Health.Died -= EndRun;
                player.Weapon.Fired -= OnShotFired;
            }
            if (waves != null)
            {
                waves.EnemyResolved -= OnEnemyResolved;
                waves.StageStarted -= OnStageStarted;
                waves.StageCleared -= OnStageCleared;
                waves.EnemyShotFired -= OnEnemyShotFired;
            }
        }
    }
}
