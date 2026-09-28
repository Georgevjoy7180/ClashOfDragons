using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ClashOfDragons.EditorTools
{
    /// <summary>
    /// Lighting + post-processing polish pass. No art assets needed — this tunes the existing
    /// Directional Light and ambient settings, and builds a URP Global Volume (a Volume Profile
    /// asset with Bloom/Vignette/Tonemapping/Color Adjustments overrides), all through Unity's own
    /// APIs. Safe to re-run.
    /// </summary>
    public static class PolishBuilder
    {
        private const string VolumeProfilePath = "Assets/_Game/ScriptableObjects/PostProcessProfile.asset";

        [MenuItem("Clash Of Dragons/Build Lighting Polish (Phase 19)")]
        public static void BuildPolish()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "BattleScene")
            {
                Debug.LogError("Clash Of Dragons: Open Assets/_Game/Scenes/BattleScene.unity as the active scene before running this command.");
                return;
            }

            if (!TuneLight())
            {
                return;
            }

            TuneAmbient();
            BuildPostProcessVolume();
            EnablePostProcessingOnCamera();

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("Clash Of Dragons: Lighting polish applied. Press Ctrl+S to save BattleScene.");
        }

        private static bool TuneLight()
        {
            Light light = Object.FindAnyObjectByType<Light>();
            if (light == null)
            {
                Debug.LogError("Clash Of Dragons: No light found — run 'Build Arena (Phase 2)' first.");
                return false;
            }

            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            return true;
        }

        private static void TuneAmbient()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.5f, 0.6f);
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.32f, 0.35f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.15f, 0.15f);
        }

        private static void BuildPostProcessVolume()
        {
            GameObject volumeObject = EditorSceneUtility.GetOrCreateRoot("Global Volume").gameObject;

            Volume volume = volumeObject.GetComponent<Volume>();
            if (volume == null)
            {
                volume = volumeObject.AddComponent<Volume>();
            }
            volume.isGlobal = true;
            volume.priority = 0f;

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }
            volume.sharedProfile = profile;

            ConfigureBloom(profile);
            ConfigureVignette(profile);
            ConfigureTonemapping(profile);
            ConfigureColorAdjustments(profile);

            EditorUtility.SetDirty(profile);
        }

        private static void ConfigureBloom(VolumeProfile profile)
        {
            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1.1f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.3f;
        }

        private static void ConfigureVignette(VolumeProfile profile)
        {
            if (!profile.TryGet(out Vignette vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }
            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.25f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.6f;
        }

        private static void ConfigureTonemapping(VolumeProfile profile)
        {
            if (!profile.TryGet(out Tonemapping tonemapping))
            {
                tonemapping = profile.Add<Tonemapping>(true);
            }
            tonemapping.active = true;
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.Neutral;
        }

        private static void ConfigureColorAdjustments(VolumeProfile profile)
        {
            if (!profile.TryGet(out ColorAdjustments colorAdjustments))
            {
                colorAdjustments = profile.Add<ColorAdjustments>(true);
            }
            colorAdjustments.active = true;
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = 8f;
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.contrast.value = 6f;
        }

        private static void EnablePostProcessingOnCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
        }
    }
}
