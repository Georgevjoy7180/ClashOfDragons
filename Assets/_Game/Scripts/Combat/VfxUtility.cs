using UnityEngine;

namespace ClashOfDragons.Combat
{
    /// <summary>
    /// Shared placeholder VFX: a short burst of colored particles, no texture/art dependency —
    /// this is what "the entire game must work with placeholders" looks like for particle effects
    /// specifically. Used by Fire/Tail/Fly so each ability's Activate() doesn't repeat the same
    /// ~20 lines of ParticleSystem setup. Swappable later: point each ability at a real
    /// ParticleSystem prefab instead of calling this, nothing else about the ability changes.
    /// </summary>
    internal static class VfxUtility
    {
        private static Shader cachedShader;

        public static void SpawnBurst(Vector3 position, Color color, float duration, int particleCount = 16, float speed = 4f, float size = 0.25f)
        {
            GameObject vfxObject = new GameObject("Vfx_Burst");
            vfxObject.transform.position = position;

            ParticleSystem system = vfxObject.AddComponent<ParticleSystem>();
            // A ParticleSystem starts playing the instant it's added to an active GameObject —
            // stop it before touching main.duration etc., which Unity refuses to change mid-play.
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = duration;
            main.startLifetime = duration;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particleCount) });

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            ParticleSystemRenderer renderer = vfxObject.GetComponent<ParticleSystemRenderer>();
            Shader shader = GetVfxShader();
            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = color;
                renderer.material = material;
            }
            // If no shader is available at all, the particles still play (they just render with
            // the renderer's default material) — a missing VFX shader should never be allowed to
            // throw and abort whatever gameplay code triggered this effect (e.g. FlyAttack's
            // coroutine, where an exception here would strand the dragon mid-air permanently).

            system.Play();
            Object.Destroy(vfxObject, duration + 0.5f);
        }

        /// <summary>
        /// "Universal Render Pipeline/Unlit" isn't referenced by any serialized Material asset in
        /// this project (only used here, via Shader.Find), so a build could strip it unless
        /// explicitly registered (see BuildScript.EnsureVfxShaderIncluded). Falls back to
        /// "Universal Render Pipeline/Lit" — which IS guaranteed present, since Mat_Ground/Mat_Wall/
        /// Mat_Player/Mat_Enemy all use it — rather than crash if that registration is ever missed.
        /// </summary>
        public static Shader GetVfxShader()
        {
            if (cachedShader != null)
            {
                return cachedShader;
            }

            cachedShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (cachedShader == null)
            {
                cachedShader = Shader.Find("Universal Render Pipeline/Lit");
            }
            return cachedShader;
        }
    }
}
