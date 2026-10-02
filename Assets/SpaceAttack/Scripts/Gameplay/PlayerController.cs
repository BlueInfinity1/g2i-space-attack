using UnityEngine;

namespace SpaceAttack
{
    [RequireComponent(typeof(Rigidbody2D), typeof(ShipHealth), typeof(Weapon))]
    public sealed class PlayerController : MonoBehaviour
    {
        public ShipHealth Health { get; private set; }
        public Weapon Weapon { get; private set; }
        public bool IsPlaying { get; private set; }
        private Rigidbody2D body;
        private PlayerConfig config;
        private PlayfieldBounds bounds;
        private float input;

        public void Initialize(PlayerConfig settings, PlayfieldBounds playfield, Transform projectiles)
        {
            Health = GetComponent<ShipHealth>();
            Weapon = GetComponent<Weapon>();
            body = GetComponent<Rigidbody2D>();
            config = settings;
            bounds = playfield;
            Weapon.Initialize(config.weapon, projectiles, Team.Player);
            ResetPlayer();
            SetPlaying(false);
        }

        public void ResetPlayer()
        {
            body.position = new Vector2(0f, bounds.playerY);
            body.linearVelocity = Vector2.zero;
            input = 0f;
            Health.ResetHealth(config.maxHealth, config.invulnerabilitySeconds);
            Weapon.ResetCooldown();
        }

        public void SetPlaying(bool playing)
        {
            IsPlaying = playing;
            if (body != null) body.linearVelocity = Vector2.zero;
            if (!playing) input = 0f;
        }

        public bool TryHit(int damage) => IsPlaying && Health.TakeDamage(damage);

        private void Update()
        {
            if (Health == null) return;
            if (!IsPlaying) return;
            input = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            if (Input.GetKey(KeyCode.Space)) Weapon.TryFire();
        }

        private void FixedUpdate()
        {
            if (IsPlaying) Move(input, Time.fixedDeltaTime);
        }

        public void Move(float direction, float deltaTime)
        {
            Vector2 destination = body.position + Vector2.right * Mathf.Clamp(direction, -1f, 1f) *
                config.moveSpeed * deltaTime;
            body.MovePosition(bounds.ClampPlayer(destination));
        }
    }
}
