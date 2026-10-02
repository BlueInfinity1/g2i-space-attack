using System;
using UnityEngine;

namespace SpaceAttack
{
    public enum Team { Player, Enemy }

    public sealed class Weapon : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform muzzle;
        public event Action Fired;
        private WeaponConfig config;
        private Transform projectileRoot;
        private Team team;
        private float nextShotTime;

        public void Initialize(WeaponConfig settings, Transform root, Team owner, float initialDelay = 0f)
        {
            config = settings;
            projectileRoot = root;
            team = owner;
            ResetCooldown(initialDelay);
        }

        public void ResetCooldown(float delay = 0f) => nextShotTime = Time.time + Mathf.Max(0f, delay);

        public bool TryFire()
        {
            if (config == null || Time.timeScale <= 0f || Time.time < nextShotTime) return false;
            nextShotTime = Time.time + Mathf.Max(0.02f, config.fireIntervalSeconds);
            Projectile shot = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity, projectileRoot);
            shot.Initialize(config, team);
            Fired?.Invoke();
            return true;
        }
    }
}
