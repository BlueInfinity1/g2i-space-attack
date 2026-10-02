using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SpaceAttack.Tests
{
    public sealed class ReviewChangesTests
    {
        private GameController game;
        private FeedbackController feedback;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1f;
            Random.InitState(294);
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            game = Object.FindFirstObjectByType<GameController>();
            feedback = Object.FindFirstObjectByType<FeedbackController>();
            feedback.SetMuted(true);
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (game != null) game.SetSettingsOpen(false);
            Time.timeScale = 1f;
            yield return null;
        }

        private static Button Button(string name) => Object.FindObjectsByType<Button>(FindObjectsInactive.Include,
            FindObjectsSortMode.None).Single(b => b.name == name);

        [UnityTest] public IEnumerator SettingsPauseWorldAndPreserveIndependentPreferencesOnRestart()
        {
            game.StartRun();
            game.Player.TryHit(1);
            game.Player.Weapon.TryFire();
            yield return new WaitForFixedUpdate();
            var enemy = game.Waves.Enemies.First();
            var shot = Object.FindFirstObjectByType<Projectile>();
            Button("Settings Button").onClick.Invoke();
            // Wait until Update: Time.time returns fixed time inside a FixedUpdate continuation.
            yield return null;
            Vector2 enemyPosition = enemy.GetComponent<Rigidbody2D>().position;
            Vector2 shotPosition = shot.GetComponent<Rigidbody2D>().position;
            float invulnerabilityAge = game.Player.Health.InvulnerabilityElapsed;
            Assert.That(game.SettingsOpen, Is.True);
            Assert.That(game.Player.IsPlaying, Is.False);
            var shake = Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(s => s.name == "Screen shake Slider");
            shake.value = 0f;
            Assert.That(feedback.GetPreference(FeedbackSetting.Shake), Is.Zero);
            Assert.That(feedback.GetPreference(FeedbackSetting.Particles), Is.EqualTo(0.6f));
            Assert.That(game.Config.feedback.screenShakeIntensity, Is.EqualTo(0.2f));
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(enemy.GetComponent<Rigidbody2D>().position, Is.EqualTo(enemyPosition));
            Assert.That(shot.GetComponent<Rigidbody2D>().position, Is.EqualTo(shotPosition));
            Assert.That(game.Player.Health.InvulnerabilityElapsed, Is.EqualTo(invulnerabilityAge));
            Assert.That(game.Player.Weapon.TryFire(), Is.False);
            Button("Close Settings Button").onClick.Invoke();
            Assert.That(game.Player.IsPlaying, Is.True);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(shot.GetComponent<Rigidbody2D>().position.y, Is.GreaterThan(shotPosition.y));
            game.EndRun();
            game.StartRun();
            Assert.That(feedback.GetPreference(FeedbackSetting.Shake), Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest] public IEnumerator SettingsReturnToStartAndGameOverWithoutStartingARun()
        {
            game.SetSettingsOpen(true);
            game.StartRun();
            Assert.That(game.State, Is.EqualTo(GameState.StartScreen));
            game.SetSettingsOpen(false);
            Assert.That(game.State, Is.EqualTo(GameState.StartScreen));
            game.StartRun();
            game.EndRun();
            game.SetSettingsOpen(true);
            game.SetSettingsOpen(false);
            Assert.That(game.State, Is.EqualTo(GameState.GameOver));
            Assert.That(game.Player.IsPlaying, Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator EnemiesFireOnlyAfterBeginningADiagonalAttack()
        {
            game.StartRun();
            // Isolate enemy firing from the automatic attack selection, while enemies still update.
            game.Waves.enabled = false;
            int shots = 0;
            game.EnemyShotFired += () => shots++;
            foreach (var enemy in game.Waves.Enemies)
            {
                enemy.Weapon.ResetCooldown(); // Even a ready weapon must remain silent in formation.
                enemy.MoveInFormation(0.1f);
            }
            yield return new WaitForSeconds(0.15f);
            Assert.That(game.Waves.Enemies.All(e => e.State == EnemyState.InFormation), Is.True);
            Assert.That(shots, Is.Zero);
            Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None), Is.Empty);

            var attackers = new[] { EnemyKind.Red, EnemyKind.Green, EnemyKind.Yellow }
                .Select(kind => game.Waves.Enemies.First(e => e.Config.kind == kind)).ToArray();
            var startingY = attackers.Select(e => e.GetComponent<Rigidbody2D>().position.y).ToArray();
            foreach (var attacker in attackers) Assert.That(attacker.BeginAttack(1), Is.True);
            yield return new WaitForSeconds(0.1f);
            for (int i = 0; i < attackers.Length; i++)
                Assert.That(attackers[i].GetComponent<Rigidbody2D>().position.y, Is.LessThan(startingY[i]));
            Assert.That(shots, Is.EqualTo(3), "Only the three attacking enemies may fire.");
            Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)
                .Count(p => p.Owner == Team.Enemy), Is.EqualTo(3));
            game.Waves.StopRun();
        }

        [UnityTest] public IEnumerator EnemyProjectileTravelsDownAndDamagesPlayer()
        {
            game.StartRun();
            game.Waves.StopRun();
            var enemy = game.Waves.Enemies.First();
            enemy.transform.position = new Vector3(0, game.Playfield.playerY + 1.6f, 0);
            Physics2D.SyncTransforms();
            enemy.Weapon.ResetCooldown();
            Assert.That(enemy.Weapon.TryFire(), Is.True);
            var shot = Object.FindFirstObjectByType<Projectile>();
            Assert.That(shot.Owner, Is.EqualTo(Team.Enemy));
            Assert.That(shot.GetComponent<Rigidbody2D>().linearVelocity.y, Is.LessThan(0));
            yield return new WaitForSeconds(0.35f);
            Assert.That(game.Player.Health.Current, Is.EqualTo(2));
            Assert.That(game.Score, Is.Zero);
            Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None), Is.Empty);
        }

        [UnityTest] public IEnumerator EnemyCooldownDoesNotDependOnPreviousProjectileCleanup()
        {
            game.StartRun();
            game.Waves.StopRun();
            var enemy = game.Waves.Enemies.First();
            var weapon = Object.Instantiate(enemy.Config.weapon);
            weapon.fireIntervalSeconds = 0.2f;
            enemy.Weapon.Initialize(weapon, GameObject.Find("Projectiles").transform, Team.Enemy);
            Assert.That(enemy.Weapon.TryFire(), Is.True);
            game.ClearProjectiles();
            Assert.That(enemy.Weapon.TryFire(), Is.False);
            yield return new WaitForSeconds(0.21f);
            Assert.That(enemy.Weapon.TryFire(), Is.True);
            yield return new WaitForSeconds(0.21f);
            Assert.That(enemy.Weapon.TryFire(), Is.True);
            Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
            Object.Destroy(weapon);
        }

        [UnityTest] public IEnumerator RandomTurnsReverseOnlyHorizontalVelocity()
        {
            game.StartRun();
            game.Waves.StopRun();
            var original = game.Waves.Enemies.First();
            var enemy = Object.Instantiate(original);
            var type = Object.Instantiate(original.Config);
            type.directionChangeDelaySeconds = 0.05f;
            type.directionCheckIntervalSeconds = 0.5f;
            type.directionChangeInitialChance = type.directionChangeMaximumChance = 1f;
            type.directionChanceGrowthPerSecond = 0f;
            enemy.Initialize(type, game.Playfield, GameObject.Find("Projectiles").transform);
            enemy.BeginAttack(1);
            float downwardSpeed = enemy.Velocity.y;
            yield return new WaitForSeconds(0.1f);
            Assert.That(enemy.Velocity.x, Is.LessThan(0));
            Assert.That(enemy.Velocity.y, Is.EqualTo(downwardSpeed).Within(0.001f));
            Assert.That(enemy.Velocity.magnitude, Is.EqualTo(type.attackSpeed).Within(0.001f));
            enemy.DisposeWithoutScore();
            Object.Destroy(type);
        }

        [UnityTest] public IEnumerator FormationMovesTogetherAndReversesAtBothLimits()
        {
            var rootConfig = Object.Instantiate(game.Config);
            var waves = Object.Instantiate(game.Config.waves);
            rootConfig.waves = waves;
            waves.formationMinOffsetX = -0.04f;
            waves.formationMaxOffsetX = 0.04f;
            waves.attackStartDelaySeconds = 10f;
            game.Waves.Initialize(rootConfig, game.Playfield);
            game.StartRun();
            var pair = game.Waves.Enemies.Take(2).ToArray();
            Vector2 spacing = pair[1].GetComponent<Rigidbody2D>().position - pair[0].GetComponent<Rigidbody2D>().position;
            float lowest = 0f, highest = 0f;
            for (int i = 0; i < 28; i++)
            {
                yield return new WaitForFixedUpdate();
                float offset = game.Waves.FormationOffsetX;
                Assert.That(offset, Is.InRange(-0.0401f, 0.0401f));
                lowest = Mathf.Min(lowest, offset);
                highest = Mathf.Max(highest, offset);
                Vector2 actual = pair[1].GetComponent<Rigidbody2D>().position - pair[0].GetComponent<Rigidbody2D>().position;
                Assert.That(Vector2.Distance(actual, spacing), Is.LessThan(0.001f));
            }
            Assert.That(lowest, Is.LessThan(-0.03f));
            Assert.That(highest, Is.GreaterThan(0.03f));
            Object.Destroy(rootConfig);
            Object.Destroy(waves);
        }

        [UnityTest] public IEnumerator InvulnerabilityBlinksAlphaAndRestoresFullOpacity()
        {
            game.StartRun();
            game.Waves.StopRun();
            feedback.SetPreference(FeedbackSetting.HitFlash, 0f);
            var part = game.Player.GetComponentsInChildren<Renderer>().First(r => r.name == "Nose");
            var properties = new MaterialPropertyBlock();
            game.Player.TryHit(1);
            yield return new WaitForSeconds(0.04f);
            part.GetPropertyBlock(properties);
            Assert.That(properties.GetColor("_Color").a, Is.EqualTo(0.5f).Within(0.001f));
            yield return new WaitForSeconds(0.12f);
            part.GetPropertyBlock(properties);
            Assert.That(properties.GetColor("_Color").a, Is.EqualTo(1f).Within(0.001f));
            feedback.SetPreference(FeedbackSetting.InvulnerabilityBlink, 0f);
            yield return null;
            yield return null;
            part.GetPropertyBlock(properties);
            Assert.That(properties.GetColor("_Color").a, Is.EqualTo(0.5f).Within(0.001f));
            yield return new WaitForSeconds(1.2f);
            part.GetPropertyBlock(properties);
            Assert.That(properties.GetColor("_Color").a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(GameObject.Find("Invulnerability Shield"), Is.Null);
        }

        [UnityTest] public IEnumerator EnergyBarUsesRuntimeMaximumAndBoundaryDoesNotDriftWithStars()
        {
            game.StartRun();
            game.Waves.StopRun();
            feedback.SetPreference(FeedbackSetting.Shake, 0f);
            game.Player.Health.ResetHealth(6, 0f);
            game.Player.TryHit(2);
            var bar = GameObject.Find("Energy Fill").GetComponent<RectTransform>();
            Assert.That(bar.anchorMax.x, Is.EqualTo(4f / 6f).Within(0.001f));
            var line = GameObject.Find("Lower Boundary").transform;
            var stars = GameObject.Find("Primitive Starfield").transform;
            Vector3 linePosition = line.position, starPosition = stars.position;
            yield return new WaitForSeconds(0.3f);
            Assert.That(line.position, Is.EqualTo(linePosition));
            Assert.That(stars.position, Is.Not.EqualTo(starPosition));
        }
    }
}
