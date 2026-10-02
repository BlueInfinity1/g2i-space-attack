using System;
using UnityEngine;

namespace SpaceAttack
{
    public sealed class ShipHealth : MonoBehaviour
    {
        public int Current { get; private set; }
        public int Maximum { get; private set; }
        public bool IsInvulnerable => Time.time < invulnerableUntil;
        public float InvulnerabilityElapsed => Mathf.Max(0f, Time.time - (invulnerableUntil - invulnerabilitySeconds));
        public event Action Changed;
        public event Action Damaged;
        public event Action Died;
        private float invulnerabilitySeconds;
        private float invulnerableUntil;

        public void ResetHealth(int maximum, float invulnerability)
        {
            Maximum = Mathf.Max(1, maximum);
            Current = Maximum;
            invulnerabilitySeconds = Mathf.Max(0f, invulnerability);
            invulnerableUntil = float.NegativeInfinity;
            Changed?.Invoke();
        }

        public bool TakeDamage(int damage)
        {
            if (Current <= 0 || damage <= 0 || IsInvulnerable) return false;
            Current = Mathf.Max(0, Current - damage);
            invulnerableUntil = Time.time + invulnerabilitySeconds;
            Changed?.Invoke();
            Damaged?.Invoke();
            if (Current == 0) Died?.Invoke();
            return true;
        }
    }
}
