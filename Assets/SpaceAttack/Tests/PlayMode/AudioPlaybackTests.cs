using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpaceAttack.Tests
{
    public sealed class AudioPlaybackTests
    {
        [UnityTest] public IEnumerator FiringEventsProduceDistinctNonSilentAudioOutput()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var game = Object.FindFirstObjectByType<GameController>();
            var feedback = Object.FindFirstObjectByType<FeedbackController>();
            var source = feedback.GetComponent<AudioSource>();
            feedback.SetMuted(false);
            game.StartRun();
            game.Waves.StopRun();
            AudioListener.pause = false;
            AudioListener.volume = 1f;
            yield return new WaitForSecondsRealtime(0.15f);
            game.Player.Weapon.TryFire();
            var player = new System.Collections.Generic.List<float>();
            yield return Capture(source, player);
            Assert.That(player.Max(v => Mathf.Abs(v)), Is.GreaterThan(0.05f), "Player fire must reach Unity's audio output at a useful level.");
            game.ClearProjectiles();
            source.Stop();
            yield return new WaitForSecondsRealtime(0.15f);
            var enemy = game.Waves.Enemies.First();
            enemy.Weapon.ResetCooldown();
            enemy.Weapon.TryFire();
            var opponent = new System.Collections.Generic.List<float>();
            yield return Capture(source, opponent);
            Assert.That(opponent.Max(v => Mathf.Abs(v)), Is.GreaterThan(0.03f), "Enemy fire must reach Unity's audio output.");
            Assert.That(player.SequenceEqual(opponent), Is.False, "Player/enemy fire must have distinct signals.");
            string directory = Path.Combine(Application.dataPath, "../TestResults/Audio-Batch");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "levels.txt"),
                $"Unity AudioSource output peaks: player={player.Max(v => Mathf.Abs(v)):F4}, enemy={opponent.Max(v => Mathf.Abs(v)):F4}; shared gain={source.volume:F3}\n" +
                "Captured from actual firing events through PlayOneShot/GetOutputData. Hardware speaker loudness still requires the applicant's listening check.\n");
            feedback.SetMuted(true);
            Assert.That(source.volume, Is.Zero);
        }

        private static IEnumerator Capture(AudioSource source, System.Collections.Generic.List<float> output)
        {
            var buffer = new float[1024];
            double end = Time.realtimeSinceStartupAsDouble + 0.3;
            while (Time.realtimeSinceStartupAsDouble < end)
            {
                source.GetOutputData(buffer, 0);
                output.AddRange(buffer);
                yield return null;
            }
        }
    }
}
