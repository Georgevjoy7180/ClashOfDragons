using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace ClashOfDragons.EditorTools
{
    /// <summary>
    /// One-click Windows build. Also forces Build Settings to contain only BattleScene (at index 0)
    /// — the project's original template SampleScene would otherwise still be scene 0, which would
    /// make the standalone build boot into the wrong scene.
    /// </summary>
    public static class BuildScript
    {
        private const string BattleScenePath = "Assets/_Game/Scenes/BattleScene.unity";
        private const string BuildFolder = "Builds/Windows";
        private const string BuildExeName = "ClashOfDragons.exe";

        [MenuItem("Clash Of Dragons/Build Windows (Phase 20)")]
        public static void BuildWindows()
        {
            if (!File.Exists(BattleScenePath))
            {
                Debug.LogError($"Clash Of Dragons: BattleScene not found at {BattleScenePath}. Complete earlier phases first.");
                return;
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BattleScenePath, true)
            };

            // Without this, the standalone build pauses (or barely ticks) whenever the game
            // window loses focus — a coroutine like FlyAttack's sequence would appear to "freeze"
            // mid-air if the player alt-tabs or clicks away while it's running.
            PlayerSettings.runInBackground = true;

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(projectRoot, BuildFolder);
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, BuildExeName);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { BattleScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            // No serialized Material asset ever references "Universal Render Pipeline/Unlit" —
            // it's only ever built at runtime via Shader.Find() inside VfxUtility/FireAttack for
            // the placeholder hit-effect materials. Without this, Unity's build-time shader
            // stripping drops it, Shader.Find returns null in the build, and `new Material(null)`
            // throws — which, when that happens mid-coroutine inside FlyAttack's sequence, kills
            // the coroutine permanently and strands the dragon mid-air.
            EnsureVfxShaderIncluded();

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Clash Of Dragons: Build succeeded — {summary.totalSize / (1024 * 1024)} MB at {outputPath}");
            }
            else
            {
                Debug.LogError($"Clash Of Dragons: Build {summary.result} ({summary.totalErrors} errors). Check the Console above for details.");
            }
        }

        /// <summary>
        /// Adds the shader to ProjectSettings/GraphicsSettings.asset's "Always Included Shaders"
        /// list via SerializedObject — the version-independent way to do this from a script
        /// (there's no simple helper method for it across all Editor versions). Note: this asset
        /// lives outside Assets/, so AssetDatabase can't load it — GraphicsSettings.GetGraphicsSettings()
        /// is the correct accessor.
        /// </summary>
        private static void EnsureVfxShaderIncluded()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogWarning("Clash Of Dragons: Could not find 'Universal Render Pipeline/Unlit' shader to register for the build.");
                return;
            }

            UnityEngine.Object graphicsSettings = GraphicsSettings.GetGraphicsSettings();
            if (graphicsSettings == null)
            {
                Debug.LogWarning("Clash Of Dragons: Could not access GraphicsSettings.");
                return;
            }

            SerializedObject serializedSettings = new SerializedObject(graphicsSettings);
            SerializedProperty alwaysIncluded = serializedSettings.FindProperty("m_AlwaysIncludedShaders");
            if (alwaysIncluded == null)
            {
                Debug.LogWarning("Clash Of Dragons: GraphicsSettings.asset has no 'm_AlwaysIncludedShaders' property.");
                return;
            }

            for (int i = 0; i < alwaysIncluded.arraySize; i++)
            {
                if (alwaysIncluded.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                {
                    return; // already registered
                }
            }

            alwaysIncluded.InsertArrayElementAtIndex(alwaysIncluded.arraySize);
            alwaysIncluded.GetArrayElementAtIndex(alwaysIncluded.arraySize - 1).objectReferenceValue = shader;
            serializedSettings.ApplyModifiedProperties();
        }
    }
}
