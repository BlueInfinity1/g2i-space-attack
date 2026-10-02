using UnityEngine;

namespace SpaceAttack
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Projectile : MonoBehaviour
    {
        private Rigidbody2D body;
        private Collider2D hitbox;
        private Team team;
        private int damage;
        private float remainingLife;
        private bool consumed;
        private bool initialized;
        private Vector2 velocity;
        public Team Owner => team;

        public void Initialize(WeaponConfig config, Team owner)
        {
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<Collider2D>();
            team = owner;
            gameObject.layer = owner == Team.Player ? 10 : 11;
            damage = Mathf.Max(1, config.projectileDamage);
            remainingLife = config.projectileLifetimeSeconds;
            velocity = (owner == Team.Player ? Vector2.up : Vector2.down) * config.projectileSpeed;
            if (owner == Team.Enemy)
            {
                var tint = new MaterialPropertyBlock();
                tint.SetColor("_Color", new Color(1f, 0.5f, 0.25f));
                foreach (Renderer part in GetComponentsInChildren<Renderer>()) part.SetPropertyBlock(tint);
            }
            body.linearVelocity = velocity;
            initialized = true;
        }

        private void Update()
        {
            if (!initialized) return;
            remainingLife -= Time.deltaTime;
            if (remainingLife <= 0f || Mathf.Abs(transform.position.y) > 9f) Consume();
        }

        private void FixedUpdate()
        {
            if (!initialized || consumed) return;
            // Sweep each physics step so fast, narrow shots cannot pass through an enemy.
            int mask = 1 << (team == Team.Player ? 9 : 8);
            RaycastHit2D hit = Physics2D.CircleCast(body.position, 0.075f, velocity.normalized,
                velocity.magnitude * Time.fixedDeltaTime, mask);
            if (hit.collider != null) TryHit(hit.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (initialized) TryHit(other);
        }

        private void TryHit(Collider2D other)
        {
            if (consumed) return;
            if (team == Team.Player && other.TryGetComponent(out EnemyController enemy))
            {
                if (enemy.HitByPlayer()) Consume();
            }
            else if (team == Team.Enemy && other.TryGetComponent(out PlayerController player))
            {
                player.TryHit(damage);
                Consume();
            }
        }

        private void Consume()
        {
            if (consumed) return;
            consumed = true;
            if (hitbox != null) hitbox.enabled = false;
            if (body != null) body.simulated = false;
            Destroy(gameObject);
        }
    }
}
