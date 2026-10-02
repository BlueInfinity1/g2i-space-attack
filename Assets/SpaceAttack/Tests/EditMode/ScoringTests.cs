using NUnit.Framework;
using UnityEngine;

namespace SpaceAttack.Tests
{
    public sealed class ScoringTests
    {
        private ScoringConfig config;
        [SetUp] public void Setup() => config = ScriptableObject.CreateInstance<ScoringConfig>();
        [TearDown] public void Teardown() => Object.DestroyImmediate(config);

        [TestCase(30, 30)] [TestCase(40, 40)] [TestCase(60, 60)]
        public void FormationKillsUseBaseScoreAtAnyHeight(int baseScore, int expected)
        {
            Assert.That(ScoreCalculator.Calculate(baseScore, false, -20, 3, -6, config), Is.EqualTo(expected));
        }

        [TestCase(30, 3f, 120)] [TestCase(40, 3f, 160)] [TestCase(60, 3f, 240)]
        [TestCase(30, -1.5f, 210)] [TestCase(40, -1.5f, 280)] [TestCase(60, -1.5f, 420)]
        [TestCase(30, -6f, 300)] [TestCase(40, -6f, 400)] [TestCase(60, -6f, 600)]
        public void AttackScoresMatchApprovedTable(int baseScore, float height, int expected)
        {
            Assert.That(ScoreCalculator.Calculate(baseScore, true, height, 3, -6, config), Is.EqualTo(expected));
        }

        [Test] public void ScoreIsMonotonicAndClamped()
        {
            int previous = 0;
            for (float y = 10; y >= -15; y -= 0.1f)
            {
                int score = ScoreCalculator.Calculate(30, true, y, 3, -6, config);
                Assert.That(score, Is.InRange(120, 300));
                Assert.That(score, Is.GreaterThanOrEqualTo(previous));
                Assert.That(score % 10, Is.Zero);
                previous = score;
            }
        }

        [Test] public void MidpointRoundingRoundsUp()
        {
            Assert.That(ScoreCalculator.Calculate(30, true, 0.75f, 3, -6, config), Is.EqualTo(170));
        }

        [Test] public void DifficultyShortensIntervalsWithinCap()
        {
            var wave = ScriptableObject.CreateInstance<WaveConfig>();
            Assert.That(wave.AttackInterval(2), Is.LessThan(wave.AttackInterval(1)));
            Assert.That(wave.AttackInterval(1000), Is.EqualTo(wave.attackIntervalSeconds / wave.maxDifficultyMultiplier).Within(0.001f));
            Object.DestroyImmediate(wave);
        }

        [Test] public void OneHealthDiesOnceAndResetRestoresIt()
        {
            var ship = new GameObject("Health test");
            var health = ship.AddComponent<ShipHealth>();
            int deaths = 0;
            health.Died += () => deaths++;
            health.ResetHealth(1, 0);
            Assert.That(health.TakeDamage(1), Is.True);
            Assert.That(health.TakeDamage(1), Is.False);
            Assert.That(deaths, Is.EqualTo(1));
            health.ResetHealth(1, 0);
            Assert.That(health.Current, Is.EqualTo(1));
            Object.DestroyImmediate(ship);
        }
    }
}
