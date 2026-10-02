#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SpaceAttack.Tests
{
    public sealed class PlaythroughReviewTests
    {
        private int titlePixelCount;
        [UnityTest] public IEnumerator PlayAndCaptureTheNormalGameView()
        {
            UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Game");
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var game = Object.FindFirstObjectByType<GameController>();
            var feedback = Object.FindFirstObjectByType<FeedbackController>();
            feedback.SetMuted(true);
            Random.InitState(270);
            game.StartRun();
            game.Player.enabled = false; // Drive movement/fire through their real gameplay methods.
            int kills = 0, shots = 0, hits = 0, restarts = 0, peakBullets = 0, highestStage = 1;
            game.EnemyDefeated += (position, color, score) => kills++;
            game.EnemyShotFired += () => shots++;
            game.PlayerDamaged += () => hits++;
            float nextCapture = 4f;
            for (float elapsed = 0f; elapsed < 32f; elapsed += Time.fixedDeltaTime)
            {
                if (game.State == GameState.GameOver) { game.StartRun(); restarts++; }
                float target = Mathf.PingPong(elapsed * 7f, 16f) - 8f;
                float movement = Mathf.Clamp((target - game.Player.transform.position.x) /
                    (game.Config.player.moveSpeed * Time.fixedDeltaTime), -1f, 1f);
                game.Player.Move(movement, Time.fixedDeltaTime);
                game.Player.Weapon.TryFire();
                highestStage = Mathf.Max(highestStage, game.Waves.Stage);
                peakBullets = Mathf.Max(peakBullets, Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length);
                if (elapsed >= nextCapture)
                {
                    yield return Capture("play-" + Mathf.RoundToInt(nextCapture));
                    nextCapture += 8f;
                }
                yield return new WaitForFixedUpdate();
            }
            game.SetSettingsOpen(true);
            yield return Capture("settings-live");
            game.SetSettingsOpen(false);
            Assert.That(kills, Is.GreaterThan(0));
            Assert.That(shots, Is.GreaterThan(0));
            Assert.That(peakBullets, Is.LessThan(100), "Fixed firing intervals must keep projectile population bounded.");
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "../TestResults/Live-Review"));
            File.WriteAllText(Path.Combine(Application.dataPath, "../TestResults/Live-Review/session.txt"),
                $"32 seconds of scripted Editor play: kills={kills}, enemy shots={shots}, damage events={hits}, restarts={restarts}, highest stage={highestStage}, peak live bullets={peakBullets}.\n" +
                "Captured the normal Game view with Screen Space Overlay UI. Scripted play does not replace the applicant's keyboard/balance and speaker listening review.\n");
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();
            string directory = Path.Combine(Application.dataPath, "../TestResults/Live-Review");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), frame.EncodeToPNG());
            if (name.StartsWith("play-"))
            {
                // Check the saved image data, independently of any screenshot preview scaling.
                Text title = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Single(t => t.name == "Wordmark");
                var corners = new Vector3[4];
                title.rectTransform.GetWorldCorners(corners);
                Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
                Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
                int count = 0;
                for (int y = Mathf.Max(0, Mathf.CeilToInt(min.y)); y < Mathf.Min(frame.height, max.y); y++)
                    for (int x = Mathf.Max(0, Mathf.CeilToInt(min.x)); x < Mathf.Min(frame.width, max.x); x++)
                    {
                        Color pixel = frame.GetPixel(x, y);
                        if (pixel.g > 0.75f && pixel.b > 0.8f && pixel.r < 0.4f) count++;
                    }
                Assert.That(count, Is.GreaterThan(0), "The title must render in the captured Game view.");
                if (titlePixelCount == 0) titlePixelCount = count;
                Assert.That(count, Is.EqualTo(titlePixelCount).Within(titlePixelCount * 0.03f),
                    "Static HUD text must remain fully rendered through kills, damage and restarts.");
                File.AppendAllText(Path.Combine(directory, "title-pixels.txt"), name + ": " + count + " cyan title pixels\n");
            }
            Object.Destroy(frame);
        }
    }
}
#endif
