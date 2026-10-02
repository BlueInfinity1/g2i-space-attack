using NUnit.Framework;
using UnityEngine;

namespace SpaceAttack.Tests
{
    public sealed class ConfigBehaviorTests
    {
        [Test] public void DirectionChangeChanceWaitsThenGrowsToACap()
        {
            var config = ScriptableObject.CreateInstance<EnemyConfig>();
            Assert.That(config.DirectionChangeChance(0.5f), Is.Zero);
            Assert.That(config.DirectionChangeChance(0.8f), Is.EqualTo(0.06f).Within(0.001f));
            Assert.That(config.DirectionChangeChance(2.8f), Is.EqualTo(0.18f).Within(0.001f));
            Assert.That(config.DirectionChangeChance(100f), Is.EqualTo(0.4f));
            Object.DestroyImmediate(config);
        }

        [Test] public void DifficultySpeedsFormationAndAttackCadenceWithinLimits()
        {
            var config = ScriptableObject.CreateInstance<WaveConfig>();
            Assert.That(config.FormationSpeed(2), Is.GreaterThan(config.FormationSpeed(1)));
            Assert.That(config.AttackInterval(2), Is.LessThan(config.AttackInterval(1)));
            Assert.That(config.FormationSpeed(1000), Is.EqualTo(1.2f).Within(0.001f));
            Object.DestroyImmediate(config);
        }
    }
}
