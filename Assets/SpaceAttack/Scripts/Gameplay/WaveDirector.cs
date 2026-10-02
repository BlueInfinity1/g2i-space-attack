using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceAttack
{
    public sealed class WaveDirector : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyPrefab;
        [SerializeField] private Transform enemiesRoot;
        [SerializeField] private Transform projectilesRoot;
        [SerializeField] private FormationSlot[] slots;
        public int Stage { get; private set; }
        public int Remaining => live.Count;
        public float FormationBottomY { get; private set; }
        public IReadOnlyCollection<EnemyController> Enemies => live;
        public event Action<EnemyResolution> EnemyResolved;
        public event Action<int> StageStarted;
        public event Action StageCleared;
        public event Action EnemyShotFired;
        public float FormationOffsetX { get; private set; }
        private readonly HashSet<EnemyController> live = new HashSet<EnemyController>();
        private readonly List<EnemyController> candidates = new List<EnemyController>();
        private GameConfig config;
        private PlayfieldBounds bounds;
        private bool running;
        private float attackTimer;
        private float stageTimer;
        private float formationTravel, minOffset, maxOffset;

        public void Initialize(GameConfig settings, PlayfieldBounds playfield)
        {
            config = settings;
            bounds = playfield;
            FormationBottomY = float.PositiveInfinity;
            foreach (FormationSlot slot in slots) FormationBottomY = Mathf.Min(FormationBottomY, slot.transform.position.y);
            float left = float.PositiveInfinity, right = float.NegativeInfinity;
            foreach (FormationSlot slot in slots)
            {
                left = Mathf.Min(left, slot.transform.position.x);
                right = Mathf.Max(right, slot.transform.position.x);
            }
            float allowedMin = -bounds.halfWidth + 0.5f - left;
            float allowedMax = bounds.halfWidth - 0.5f - right;
            minOffset = Mathf.Clamp(Mathf.Min(config.waves.formationMinOffsetX, config.waves.formationMaxOffsetX), allowedMin, allowedMax);
            maxOffset = Mathf.Clamp(Mathf.Max(config.waves.formationMinOffsetX, config.waves.formationMaxOffsetX), minOffset, allowedMax);
        }

        public void BeginRun()
        {
            Clear();
            Stage = 0;
            running = true;
            BeginStage();
        }

        public void StopRun()
        {
            running = false;
            foreach (EnemyController enemy in live) enemy.SetSimulation(false);
        }

        public void Clear()
        {
            running = false;
            foreach (EnemyController enemy in live)
            {
                if (enemy == null) continue;
                enemy.Resolved -= HandleResolution;
                enemy.ShotFired -= OnEnemyShot;
                enemy.DisposeWithoutScore();
            }
            live.Clear();
            candidates.Clear();
        }

        private void BeginStage()
        {
            Stage++;
            FormationOffsetX = Mathf.Clamp(0f, minOffset, maxOffset);
            formationTravel = FormationOffsetX - minOffset;
            foreach (FormationSlot slot in slots)
            {
                EnemyConfig type = slot.kind == EnemyKind.Yellow ? config.yellowEnemy :
                    slot.kind == EnemyKind.Green ? config.greenEnemy : config.redEnemy;
                EnemyController enemy = Instantiate(enemyPrefab, slot.transform.position, Quaternion.identity, enemiesRoot);
                enemy.name = $"{type.kind} - {slot.name}";
                enemy.Initialize(type, bounds, projectilesRoot);
                enemy.Resolved += HandleResolution;
                enemy.ShotFired += OnEnemyShot;
                enemy.MoveInFormation(FormationOffsetX);
                live.Add(enemy);
            }
            attackTimer = config.waves.attackStartDelaySeconds;
            stageTimer = config.waves.interWaveDelaySeconds;
            StageStarted?.Invoke(Stage);
        }

        private void Update()
        {
            if (!running || Time.timeScale <= 0f) return;
            if (live.Count == 0)
            {
                stageTimer -= Time.deltaTime;
                if (stageTimer <= 0f) BeginStage();
                return;
            }
            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f) return;
            candidates.Clear();
            int attackers = 0;
            foreach (EnemyController enemy in live)
            {
                if (enemy.State == EnemyState.InFormation) candidates.Add(enemy);
                else if (enemy.State == EnemyState.Attacking) attackers++;
            }
            if (attackers >= config.waves.maxConcurrentAttackers || candidates.Count == 0) return;
            EnemyController selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            // Favor inward diagonals, ensuring ships cross the active playfield.
            int sign = Mathf.Abs(selected.transform.position.x) < 0.1f ?
                (UnityEngine.Random.value < 0.5f ? -1 : 1) : (selected.transform.position.x < 0f ? 1 : -1);
            selected.BeginAttack(sign);
            attackTimer = config.waves.AttackInterval(Stage);
        }

        private void HandleResolution(EnemyResolution resolution)
        {
            if (!live.Remove(resolution.Enemy)) return;
            resolution.Enemy.Resolved -= HandleResolution;
            resolution.Enemy.ShotFired -= OnEnemyShot;
            EnemyResolved?.Invoke(resolution);
            if (live.Count == 0 && running) StageCleared?.Invoke();
        }

        private void OnEnemyShot() => EnemyShotFired?.Invoke();

        private void FixedUpdate()
        {
            if (!running || live.Count == 0) return;
            formationTravel += config.waves.FormationSpeed(Stage) * Time.fixedDeltaTime;
            FormationOffsetX = maxOffset > minOffset ? minOffset + Mathf.PingPong(formationTravel, maxOffset - minOffset) : minOffset;
            foreach (EnemyController enemy in live) enemy.MoveInFormation(FormationOffsetX);
        }

        private void OnDestroy() => Clear();
    }
}
