using System;
using UnityEngine;

namespace SpaceAttack
{
    public enum EnemyState { InFormation, Attacking, Resolved }

    public readonly struct EnemyResolution
    {
        public readonly EnemyController Enemy;
        public readonly EnemyConfig Config;
        public readonly Vector2 Position;
        public readonly bool WasAttacking;
        public readonly bool PlayerKill;
        public EnemyResolution(EnemyController enemy, bool playerKill)
        {
            Enemy = enemy;
            Config = enemy.Config;
            Position = enemy.transform.position;
            WasAttacking = enemy.State == EnemyState.Attacking;
            PlayerKill = playerKill;
        }
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(ShipHealth), typeof(Weapon))]
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField] private Renderer[] coloredParts;
        public EnemyConfig Config { get; private set; }
        public EnemyState State { get; private set; }
        public Vector2 Velocity => body == null ? Vector2.zero : body.linearVelocity;
        public event Action<EnemyResolution> Resolved;
        public event Action ShotFired;
        public Weapon Weapon { get; private set; }
        private Rigidbody2D body;
        private Collider2D hitbox;
        private ShipHealth health;
        private PlayfieldBounds bounds;
        private bool initialized;
        private bool simulationEnabled;
        private Vector2 formationPosition;
        private float attackAge;
        private float nextDirectionCheck;

        public void Initialize(EnemyConfig config, PlayfieldBounds playfield, Transform projectiles = null)
        {
            Config = config;
            bounds = playfield;
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<Collider2D>();
            health = GetComponent<ShipHealth>();
            health.ResetHealth(1, 0f);
            health.Died += OnKilled;
            Weapon = GetComponent<Weapon>();
            if (config.weapon != null)
                Weapon.Initialize(config.weapon, projectiles, Team.Enemy,
                    UnityEngine.Random.Range(1f, Mathf.Max(1f, config.weapon.fireIntervalSeconds)));
            Weapon.Fired += OnShot;
            formationPosition = body.position;
            State = EnemyState.InFormation;
            body.linearVelocity = Vector2.zero;
            simulationEnabled = true;
            initialized = true;
            var properties = new MaterialPropertyBlock();
            properties.SetColor("_Color", config.color);
            foreach (Renderer part in coloredParts) part.SetPropertyBlock(properties);
        }

        public bool BeginAttack(int horizontalSign)
        {
            if (!initialized || !simulationEnabled || State != EnemyState.InFormation) return false;
            float angle = Mathf.Clamp(Config.attackAngleFromVerticalDegrees, 5f, 80f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2((horizontalSign < 0 ? -1f : 1f) * Mathf.Sin(angle), -Mathf.Cos(angle));
            State = EnemyState.Attacking;
            attackAge = 0f;
            nextDirectionCheck = Mathf.Max(0f, Config.directionChangeDelaySeconds);
            body.linearVelocity = direction.normalized * Mathf.Max(0.1f, Config.attackSpeed);
            return true;
        }

        public bool HitByPlayer()
        {
            if (!initialized || !simulationEnabled || State == EnemyState.Resolved) return false;
            return health.TakeDamage(1);
        }

        private void OnKilled() => Resolve(true);
        private void OnShot() => ShotFired?.Invoke();

        public void MoveInFormation(float offset)
        {
            if (initialized && simulationEnabled && State == EnemyState.InFormation)
                body.MovePosition(formationPosition + Vector2.right * offset);
        }

        private void Update()
        {
            if (!initialized || !simulationEnabled || State != EnemyState.Attacking || Time.timeScale <= 0f) return;
            Weapon.TryFire();
        }

        private void FixedUpdate()
        {
            if (!initialized || !simulationEnabled || State != EnemyState.Attacking) return;
            if (bounds.IsOutside(body.position)) { Resolve(false); return; }
            attackAge += Time.fixedDeltaTime;
            if (attackAge < nextDirectionCheck) return;
            nextDirectionCheck = attackAge + Mathf.Max(0.05f, Config.directionCheckIntervalSeconds);
            if (UnityEngine.Random.value < Config.DirectionChangeChance(attackAge))
                body.linearVelocity = new Vector2(-body.linearVelocity.x, body.linearVelocity.y);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!initialized || !simulationEnabled || State == EnemyState.Resolved) return;
            if (other.TryGetComponent(out PlayerController player) && player.IsPlaying)
            {
                player.TryHit(Config.contactDamage);
                Resolve(false);
            }
        }

        private void Resolve(bool playerKill)
        {
            if (State == EnemyState.Resolved) return;
            var resolution = new EnemyResolution(this, playerKill);
            State = EnemyState.Resolved;
            hitbox.enabled = false;
            body.simulated = false;
            Resolved?.Invoke(resolution);
            Destroy(gameObject);
        }

        public void SetSimulation(bool enabled)
        {
            simulationEnabled = enabled;
            if (body != null) body.simulated = enabled;
        }

        public void DisposeWithoutScore()
        {
            State = EnemyState.Resolved;
            if (hitbox != null) hitbox.enabled = false;
            if (body != null) body.simulated = false;
            Resolved = null;
            ShotFired = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= OnKilled;
            if (Weapon != null) Weapon.Fired -= OnShot;
        }
    }
}
