using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceAttack.Editor
{
    public static class ProjectValidation
    {
        [MenuItem("Space Attack/Open Game Scene")]
        public static void OpenForReview()
        {
            EditorSceneManager.OpenScene("Assets/SpaceAttack/Scenes/Game.unity");
            EditorApplication.delayCall += () =>
            {
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.in2DMode = true;
                    SceneView.lastActiveSceneView.LookAt(Vector3.zero, Quaternion.identity, 10f, true, true);
                }
                EditorApplication.ExecuteMenuItem("Window/General/Game");
            };
        }

        [MenuItem("Space Attack/Validate Scene")]
        public static void Validate()
        {
            var scene = EditorSceneManager.OpenScene("Assets/SpaceAttack/Scenes/Game.unity");
            var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            foreach (Transform item in objects)
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) > 0)
                    throw new Exception("Missing script on " + item.name);
            var slots = objects.Select(t => t.GetComponent<FormationSlot>()).Where(s => s != null).ToArray();
            Require(slots.Length == 41, "Expected 41 formation slots");
            Require(slots.Count(s => s.kind == EnemyKind.Red) == 32, "Expected 32 red enemies");
            Require(slots.Count(s => s.kind == EnemyKind.Green) == 7, "Expected seven green enemies");
            Require(slots.Count(s => s.kind == EnemyKind.Yellow) == 2, "Expected two yellow enemies");
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/SpaceAttack/Settings/Game.asset");
            Require(config != null && config.player != null && config.player.weapon != null &&
                config.redEnemy != null && config.greenEnemy != null && config.yellowEnemy != null &&
                config.scoring != null && config.waves != null && config.feedback != null, "Missing config reference");
            Require(config.redEnemy.weapon != null && config.greenEnemy.weapon != null && config.yellowEnemy.weapon != null,
                "Missing enemy weapon config");
            Require(config.player.weapon != config.redEnemy.weapon, "Player and enemy weapons must tune independently");
            Require(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SpaceAttack/Prefabs/Enemy.prefab").GetComponent<EnemyController>() != null, "Enemy prefab missing controller");
            Require(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SpaceAttack/Prefabs/Projectile.prefab").GetComponent<Projectile>() != null, "Projectile prefab missing controller");
            Require(objects.Any(t => t.GetComponent<SettingsView>() != null), "Settings view missing");
            Require(objects.Count(t => t.GetComponent<UnityEngine.UI.Slider>() != null) == 8, "Expected eight independent settings");
            Require(objects.Single(t => t.name == "Lower Boundary").parent.name == "Playfield Border", "Boundary must not drift with stars");
            foreach (Renderer renderer in objects.Select(t => t.GetComponent<Renderer>()).Where(r => r != null))
                Require(renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null &&
                    renderer.sharedMaterial.shader.name == "SpaceAttack/FlatColor", "Missing/custom shader failed on " + renderer.name);
            Debug.Log("SPACE_ATTACK_VALIDATION_OK: 41 slots, three types, serialized scene/prefabs/config references valid. No build created.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
