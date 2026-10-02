#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SpaceAttack.Tests
{
    public sealed class VisualCaptureTests
    {
        [UnityTest] public IEnumerator RenderEditorReviewScreens()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Visual review requires a graphics-enabled Unity Editor run.");
            UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Game");
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var game = Object.FindFirstObjectByType<GameController>();
            var camera = Object.FindFirstObjectByType<Camera>();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            Object.FindFirstObjectByType<FeedbackController>().SetMuted(true);
            yield return Capture("start", camera, canvas, 1280, 900);
            Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(b => b.name == "Start Button").onClick.Invoke();
            Assert.That(game.State, Is.EqualTo(GameState.Playing));
            yield return null;
            yield return Capture("gameplay", camera, canvas, 1280, 900);
            game.SetSettingsOpen(true);
            yield return Capture("settings", camera, canvas, 1280, 900);
            yield return Capture("settings-small", camera, canvas, 960, 720);
            game.SetSettingsOpen(false);
            EnemyController attacker = game.Waves.Enemies.First(e => e.Config.kind == EnemyKind.Yellow);
            attacker.BeginAttack(1);
            attacker.transform.position = new Vector3(1.5f, -3.2f, 0);
            Physics2D.SyncTransforms();
            attacker.HitByPlayer();
            yield return null;
            yield return Capture("score-feedback", camera, canvas, 1280, 900);
            // Keep time paused while capturing the damage cue and proportional energy bar.
            game.Player.TryHit(1);
            Time.timeScale = 0f;
            yield return Capture("damage", camera, canvas, 1280, 900);
            Time.timeScale = 1f;
            game.Player.Health.ResetHealth(1, 0f);
            game.Player.TryHit(game.Player.Health.Current);
            Assert.That(game.State, Is.EqualTo(GameState.GameOver));
            yield return Capture("game-over", camera, canvas, 1280, 900);
            Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(b => b.name == "Restart Button").onClick.Invoke();
            Assert.That(game.State, Is.EqualTo(GameState.Playing));
            yield return Capture("gameplay-small", camera, canvas, 960, 720);
            game.Waves.StopRun();
            foreach (EnemyKind kind in new[] { EnemyKind.Red, EnemyKind.Green, EnemyKind.Yellow })
            {
                var enemy = game.Waves.Enemies.First(e => e.Config.kind == kind);
                Object.FindFirstObjectByType<FeedbackController>().ShowScore(enemy.transform.position, enemy.Config.formationScore);
            }
            Time.timeScale = 0f;
            yield return Capture("score-contrast", camera, canvas, 1280, 900);
            Time.timeScale = 1f;
        }

        private static IEnumerator Capture(string name, Camera camera, Canvas canvas, int width, int height)
        {
            string directory = Path.Combine(Application.dataPath, "../TestResults/Review-Batch");
            Directory.CreateDirectory(directory);
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            yield return null;
            Canvas.ForceUpdateCanvases();
            // Read the completed frame after the camera target and canvas size have updated.
            yield return new WaitForEndOfFrame();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), texture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Object.Destroy(texture);
            target.Release();
            Object.Destroy(target);
        }
    }
}
#endif
