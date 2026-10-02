using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpaceAttack.Tests
{
    public sealed class GameplayTests
    {
        private GameController game;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            game = Object.FindFirstObjectByType<GameController>();
        }

        [UnityTest] public IEnumerator StartCreatesCompleteFormationAndCorrectTypes()
        {
            Assert.That(game.State, Is.EqualTo(GameState.StartScreen));
            game.StartRun();
            Assert.That(game.Waves.Remaining, Is.EqualTo(41));
            Assert.That(game.Waves.Enemies.Count(e => e.Config.kind == EnemyKind.Red), Is.EqualTo(32));
            Assert.That(game.Waves.Enemies.Count(e => e.Config.kind == EnemyKind.Green), Is.EqualTo(7));
            Assert.That(game.Waves.Enemies.Count(e => e.Config.kind == EnemyKind.Yellow), Is.EqualTo(2));
            Assert.That(game.Waves.Enemies.All(e => e.State == EnemyState.InFormation), Is.True);
            yield return null;
        }

        [UnityTest] public IEnumerator EveryTypeDiesInOneHitWithExactlyOneScoreAward()
        {
            game.StartRun();
            int expected = 0;
            foreach (var kind in new[] { EnemyKind.Red, EnemyKind.Green, EnemyKind.Yellow })
            {
                EnemyController enemy = game.Waves.Enemies.First(e => e.Config.kind == kind);
                expected += enemy.Config.formationScore;
                Assert.That(enemy.HitByPlayer(), Is.True);
                Assert.That(enemy.HitByPlayer(), Is.False);
                Assert.That(game.Score, Is.EqualTo(expected));
            }
            Assert.That(game.Kills, Is.EqualTo(3));
            yield return null;
        }

        [UnityTest] public IEnumerator AttackVelocityIsDiagonalAndConstantInBothDirections()
        {
            game.StartRun();
            var enemies = game.Waves.Enemies.Take(2).ToArray();
            enemies[0].BeginAttack(-1);
            enemies[1].BeginAttack(1);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(enemies[0].Velocity.x, Is.LessThan(0));
            Assert.That(enemies[1].Velocity.x, Is.GreaterThan(0));
            foreach (var enemy in enemies)
            {
                Assert.That(enemy.Velocity.y, Is.LessThan(0));
                Assert.That(enemy.Velocity.magnitude, Is.EqualTo(enemy.Config.attackSpeed).Within(0.001f));
                Assert.That(enemy.HitByPlayer(), Is.True);
            }
        }

        [UnityTest] public IEnumerator EscapedAttackerIsRemovedWithoutPoints()
        {
            game.StartRun();
            var enemy = game.Waves.Enemies.First();
            enemy.BeginAttack(1);
            enemy.GetComponent<Rigidbody2D>().position = new Vector2(0, -20);
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(game.Waves.Remaining, Is.EqualTo(40));
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.Escaped, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator ARealProjectileHitsAndIsConsumed()
        {
            game.StartRun();
            Assert.That(game.Player.Weapon.TryFire(), Is.True);
            Assert.That(game.Player.Weapon.TryFire(), Is.False, "Weapon cooldown must prevent duplicate shots.");
            yield return new WaitForSeconds(0.6f);
            Assert.That(game.Kills, Is.EqualTo(1));
            Assert.That(game.Score, Is.EqualTo(30));
            Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None), Is.Empty);
        }

        [UnityTest] public IEnumerator ClearingAStageCreatesTheNextFullFormation()
        {
            game.StartRun();
            foreach (var enemy in game.Waves.Enemies.ToArray()) enemy.HitByPlayer();
            Assert.That(game.Score, Is.EqualTo(1360));
            Assert.That(game.Waves.Remaining, Is.Zero);
            yield return new WaitForSeconds(game.Config.waves.interWaveDelaySeconds + 0.1f);
            Assert.That(game.Waves.Stage, Is.EqualTo(2));
            Assert.That(game.Waves.Remaining, Is.EqualTo(41));
        }

        [UnityTest] public IEnumerator PlayerInvulnerabilityGameOverAndRepeatedRestartsAreClean()
        {
            game.StartRun();
            Assert.That(game.Player.TryHit(1), Is.True);
            Assert.That(game.Player.TryHit(1), Is.False);
            Assert.That(game.Player.Health.Current, Is.EqualTo(2));
            game.Player.Health.ResetHealth(1, 0);
            game.Player.TryHit(1);
            Assert.That(game.State, Is.EqualTo(GameState.GameOver));
            Assert.That(game.Player.IsPlaying, Is.False);
            for (int i = 0; i < 3; i++)
            {
                game.StartRun();
                yield return null;
                Assert.That(game.Player.Health.Current, Is.EqualTo(3));
                Assert.That(game.Score, Is.Zero);
                Assert.That(game.Waves.Stage, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Length, Is.EqualTo(41));
                game.Waves.Enemies.First(e => e.Config.kind == EnemyKind.Red).HitByPlayer();
                Assert.That(game.Score, Is.EqualTo(30));
                game.EndRun();
            }
        }

        [UnityTest] public IEnumerator PlayerMovementStaysWithinBounds()
        {
            game.StartRun();
            game.Waves.StopRun();
            // Drive the real movement/physics path without hardware keyboard input.
            game.Player.enabled = false;
            for (int i = 0; i < 30; i++)
            {
                game.Player.Move(1, 0.05f);
                yield return new WaitForFixedUpdate();
            }
            yield return new WaitForFixedUpdate();
            Assert.That(game.Player.transform.position.x, Is.EqualTo(game.Playfield.halfWidth).Within(0.02f));
            for (int i = 0; i < 50; i++)
            {
                game.Player.Move(-1, 0.05f);
                yield return new WaitForFixedUpdate();
            }
            yield return new WaitForFixedUpdate();
            Assert.That(game.Player.transform.position.x, Is.EqualTo(-game.Playfield.halfWidth).Within(0.02f));
            Assert.That(game.Player.transform.position.y, Is.EqualTo(game.Playfield.playerY).Within(0.001f));
        }

        [UnityTest] public IEnumerator ComfortPreferencesSurviveRestartWithoutEditingConfig()
        {
            var feedback = Object.FindFirstObjectByType<FeedbackController>();
            float shakeSetting = game.Config.feedback.screenShakeIntensity;
            feedback.SetReducedEffects(true);
            feedback.SetMuted(true);
            game.StartRun();
            game.Waves.Enemies.First().HitByPlayer();
            yield return null;
            Assert.That(Object.FindObjectsByType<EffectParticle>(FindObjectsSortMode.None), Is.Empty);
            game.EndRun();
            game.StartRun();
            Assert.That(feedback.ReducedEffects && feedback.Muted, Is.True);
            Assert.That(game.Config.feedback.screenShakeIntensity, Is.EqualTo(shakeSetting));
        }
    }
}
