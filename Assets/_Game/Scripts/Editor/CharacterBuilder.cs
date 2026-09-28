using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ClashOfDragons.Player;
using ClashOfDragons.Combat;
using ClashOfDragons.AI;

namespace ClashOfDragons.EditorTools
{
    /// <summary>
    /// Builds the placeholder dragon rigs (capsule visual + physics body + attack/ground reference
    /// points) using Unity's own APIs. See ArenaBuilder for why this runs as an Editor tool instead
    /// of hand-authored scene YAML. Safe to re-run.
    /// </summary>
    public static class CharacterBuilder
    {
        private const string PlayerMaterialPath = "Assets/_Game/Materials/Mat_Player.mat";
        private static readonly Color PlayerColor = new Color(0.2f, 0.4f, 0.9f);

        private const string EnemyMaterialPath = "Assets/_Game/Materials/Mat_Enemy.mat";
        private static readonly Color EnemyColor = new Color(0.85f, 0.2f, 0.2f);

        [MenuItem("Clash Of Dragons/Build Player (Phase 3)")]
        public static void BuildPlayerDragon()
        {
            GameObject player = BuildDragonRig("PlayerDragon", "PlayerSpawn", new Vector3(0f, 1f, -6f), PlayerMaterialPath, PlayerColor);
            if (player == null)
            {
                return;
            }

            if (player.GetComponent<PlayerController>() == null)
            {
                player.AddComponent<PlayerController>();
            }

            MarkDirtyAndLog("PlayerDragon");
        }

        [MenuItem("Clash Of Dragons/Build Enemy (Phase 4)")]
        public static void BuildEnemyDragon()
        {
            GameObject enemy = BuildDragonRig("EnemyDragon", "EnemySpawn", new Vector3(0f, 1f, 6f), EnemyMaterialPath, EnemyColor);
            if (enemy == null)
            {
                return;
            }

            // Faces the player's side of the arena by default; DragonAI drives rotation once it starts acting.
            enemy.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

            if (enemy.GetComponent<DragonAI>() == null)
            {
                enemy.AddComponent<DragonAI>();
            }

            MarkDirtyAndLog("EnemyDragon");
        }

        private static GameObject BuildDragonRig(string dragonName, string spawnName, Vector3 fallbackSpawn, string materialPath, Color color)
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "BattleScene")
            {
                Debug.LogError("Clash Of Dragons: Open Assets/_Game/Scenes/BattleScene.unity as the active scene before running this command.");
                return null;
            }

            Vector3 spawnPosition = FindSpawnPosition(spawnName, fallbackSpawn);
            Transform characters = EditorSceneUtility.GetOrCreateRoot("Characters");

            GameObject dragon = EditorSceneUtility.FindOrCreateChild(characters, dragonName);
            dragon.transform.position = spawnPosition;
            dragon.transform.rotation = Quaternion.identity;

            SetupPhysicsBody(dragon);
            SetupVisual(dragon.transform, materialPath, color);
            SetupPoint(dragon.transform, "FirePoint", new Vector3(0f, 1f, 1f));
            SetupPoint(dragon.transform, "GroundPoint", new Vector3(0f, -1f, 0f));

            if (dragon.GetComponent<DragonHealth>() == null)
            {
                dragon.AddComponent<DragonHealth>();
            }

            if (dragon.GetComponent<DragonCombat>() == null)
            {
                dragon.AddComponent<DragonCombat>();
            }

            if (dragon.GetComponent<FireAttack>() == null)
            {
                dragon.AddComponent<FireAttack>();
            }

            if (dragon.GetComponent<TailAttack>() == null)
            {
                dragon.AddComponent<TailAttack>();
            }

            if (dragon.GetComponent<FlyAttack>() == null)
            {
                dragon.AddComponent<FlyAttack>();
            }

            if (dragon.GetComponent<DragonVisualFeedback>() == null)
            {
                dragon.AddComponent<DragonVisualFeedback>();
            }

            LinkOpponents();

            return dragon;
        }

        /// <summary>
        /// Points each dragon's DragonCombat.Target at the other dragon, if both currently exist.
        /// Runs after every build so Player/Enemy can be built in either order.
        /// </summary>
        private static void LinkOpponents()
        {
            GameObject playerObject = GameObject.Find("PlayerDragon");
            GameObject enemyObject = GameObject.Find("EnemyDragon");
            if (playerObject == null || enemyObject == null)
            {
                return;
            }

            DragonCombat playerCombat = playerObject.GetComponent<DragonCombat>();
            DragonCombat enemyCombat = enemyObject.GetComponent<DragonCombat>();
            DragonHealth playerHealth = playerObject.GetComponent<DragonHealth>();
            DragonHealth enemyHealth = enemyObject.GetComponent<DragonHealth>();

            if (playerCombat != null && enemyHealth != null)
            {
                playerCombat.SetTarget(enemyHealth);
            }

            if (enemyCombat != null && playerHealth != null)
            {
                enemyCombat.SetTarget(playerHealth);
            }
        }

        private static void MarkDirtyAndLog(string dragonName)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"Clash Of Dragons: {dragonName} built. Press Ctrl+S to save BattleScene.");
        }

        private static Vector3 FindSpawnPosition(string spawnName, Vector3 fallback)
        {
            GameObject spawn = GameObject.Find(spawnName);
            return spawn != null ? spawn.transform.position : fallback;
        }

        private static void SetupPhysicsBody(GameObject go)
        {
            Rigidbody body = go.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = go.AddComponent<Rigidbody>();
            }
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            CapsuleCollider collider = go.GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = go.AddComponent<CapsuleCollider>();
            }
            collider.height = 2f;
            collider.radius = 0.5f;
            collider.center = Vector3.zero;
        }

        private static void SetupVisual(Transform parent, string materialPath, Color color)
        {
            Transform existing = parent.Find("Visual");
            GameObject visual;
            if (existing != null)
            {
                visual = existing.gameObject;
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Visual";
                Object.DestroyImmediate(visual.GetComponent<Collider>());
            }
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = Vector3.zero;

            Renderer renderer = visual.GetComponent<Renderer>();
            renderer.sharedMaterial = EditorSceneUtility.GetOrCreateMaterial(materialPath, color);
        }

        private static void SetupPoint(Transform parent, string name, Vector3 localPosition)
        {
            GameObject point = EditorSceneUtility.FindOrCreateChild(parent, name);
            point.transform.localPosition = localPosition;
        }
    }
}
