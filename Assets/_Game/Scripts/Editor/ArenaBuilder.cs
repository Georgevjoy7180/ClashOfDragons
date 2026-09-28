using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ClashOfDragons.Cameras;

namespace ClashOfDragons.EditorTools
{
    /// <summary>
    /// One-time scene setup tool. Builds the arena (ground, walls, spawn points, light, camera)
    /// using Unity's own APIs so the scene file is written safely by the Editor, not hand-authored.
    /// Safe to re-run: it updates existing objects instead of duplicating them.
    /// </summary>
    public static class ArenaBuilder
    {
        private const float ArenaHalfSize = 9f;
        private const float WallHeight = 3f;
        private const float WallThickness = 1f;

        private const string GroundMaterialPath = "Assets/_Game/Materials/Mat_Ground.mat";
        private const string WallMaterialPath = "Assets/_Game/Materials/Mat_Wall.mat";

        [MenuItem("Clash Of Dragons/Build Arena (Phase 2)")]
        public static void BuildArena()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "BattleScene")
            {
                Debug.LogError("Clash Of Dragons: Open Assets/_Game/Scenes/BattleScene.unity as the active scene before running this command.");
                return;
            }

            Transform environment = EditorSceneUtility.GetOrCreateRoot("Environment");

            CreateGround(environment);
            CreateWall(environment, "Wall_North", new Vector3(0f, WallHeight * 0.5f, ArenaHalfSize + WallThickness * 0.5f), new Vector3(ArenaHalfSize * 2f + WallThickness, WallHeight, WallThickness));
            CreateWall(environment, "Wall_South", new Vector3(0f, WallHeight * 0.5f, -(ArenaHalfSize + WallThickness * 0.5f)), new Vector3(ArenaHalfSize * 2f + WallThickness, WallHeight, WallThickness));
            CreateWall(environment, "Wall_East", new Vector3(ArenaHalfSize + WallThickness * 0.5f, WallHeight * 0.5f, 0f), new Vector3(WallThickness, WallHeight, ArenaHalfSize * 2f + WallThickness));
            CreateWall(environment, "Wall_West", new Vector3(-(ArenaHalfSize + WallThickness * 0.5f), WallHeight * 0.5f, 0f), new Vector3(WallThickness, WallHeight, ArenaHalfSize * 2f + WallThickness));

            CreateSpawnPoint("PlayerSpawn", new Vector3(0f, 1f, -6f));
            CreateSpawnPoint("EnemySpawn", new Vector3(0f, 1f, 6f));

            SetupLight();
            SetupCamera();

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("Clash Of Dragons: Arena built. Press Ctrl+S to save BattleScene.");
        }

        private static void CreateGround(Transform parent)
        {
            GameObject ground = FindOrCreatePrimitive(parent, "Ground", PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(ArenaHalfSize * 2f, 1f, ArenaHalfSize * 2f);
            ApplyMaterial(ground, EditorSceneUtility.GetOrCreateMaterial(GroundMaterialPath, new Color(0.35f, 0.4f, 0.3f)));
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = FindOrCreatePrimitive(parent, name, PrimitiveType.Cube);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            ApplyMaterial(wall, EditorSceneUtility.GetOrCreateMaterial(WallMaterialPath, new Color(0.3f, 0.3f, 0.32f)));
        }

        private static GameObject FindOrCreatePrimitive(Transform parent, string name, PrimitiveType type)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void ApplyMaterial(GameObject go, Material material)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void CreateSpawnPoint(string name, Vector3 position)
        {
            GameObject spawn = GameObject.Find(name);
            if (spawn == null)
            {
                spawn = new GameObject(name);
            }
            spawn.transform.position = position;
        }

        private static void SetupLight()
        {
            Light light = Object.FindAnyObjectByType<Light>();
            GameObject lightObject = light != null ? light.gameObject : new GameObject("Directional Light");
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }
            light.type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void SetupCamera()
        {
            Camera camera = Camera.main;
            GameObject cameraObject;
            if (camera == null)
            {
                cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<AudioListener>();
            }
            else
            {
                cameraObject = camera.gameObject;
            }

            cameraObject.transform.position = new Vector3(0f, 13f, -12f);
            cameraObject.transform.LookAt(new Vector3(0f, 1f, 0f));

            WireBattleCamera(cameraObject);
        }

        private static void WireBattleCamera(GameObject cameraObject)
        {
            BattleCamera battleCamera = cameraObject.GetComponent<BattleCamera>();
            if (battleCamera == null)
            {
                battleCamera = cameraObject.AddComponent<BattleCamera>();
            }

            GameObject player = GameObject.Find("PlayerDragon");
            GameObject enemy = GameObject.Find("EnemyDragon");

            battleCamera.Initialize(
                player != null ? player.transform : null,
                enemy != null ? enemy.transform : null);
        }
    }
}
