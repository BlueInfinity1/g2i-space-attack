using UnityEngine;

namespace SpaceAttack
{
    public sealed class EffectParticle : MonoBehaviour
    {
        private Vector3 velocity;
        private float lifetime;
        private float remaining;
        private Color tint;
        private Renderer visual;
        private MaterialPropertyBlock properties;

        private void Awake() => properties = new MaterialPropertyBlock();

        public void Initialize(Vector3 motion, Color color, float duration)
        {
            velocity = motion;
            tint = color;
            remaining = lifetime = duration;
            visual = GetComponent<Renderer>();
            properties.SetColor("_Color", tint);
            visual.SetPropertyBlock(properties);
        }

        private void Update()
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f) { Destroy(gameObject); return; }
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(0f, 0f, 160f * Time.deltaTime);
            tint.a = Mathf.Clamp01(remaining / lifetime);
            properties.SetColor("_Color", tint);
            visual.SetPropertyBlock(properties);
        }
    }
}
