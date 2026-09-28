using System.Collections;
using UnityEngine;

namespace ClashOfDragons.Combat
{
    /// <summary>
    /// Placeholder-friendly "animation" layer. There's no skeletal dragon model yet (Phase 15 was
    /// deliberately skipped), so instead of an Animator Controller this drives simple procedural
    /// feedback on the Visual capsule: a punch-scale when an ability activates, a color flash when
    /// health drops. It listens to the exact same hooks a real Animator would (AbilityBase.OnActivated,
    /// DragonHealth.OnHealthChanged) — swapping in a real rig later means replacing this component,
    /// not touching any combat code.
    /// </summary>
    [RequireComponent(typeof(DragonHealth))]
    public class DragonVisualFeedback : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private float attackPunchScale = 1.25f;
        [SerializeField] private float attackPunchDuration = 0.15f;
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private float hitFlashDuration = 0.15f;

        private DragonHealth health;
        private Material visualMaterial;
        private Color baseColor;
        private Vector3 baseScale;
        private Coroutine punchRoutine;
        private Coroutine flashRoutine;

        // -1 sentinel: skip reacting to the very first OnHealthChanged callback (Awake/OnEnable
        // ordering between components isn't guaranteed, so we don't trust an initial "previous"
        // read from DragonHealth directly — we just ignore whichever value arrives first).
        private float previousHealth = -1f;

        private void Awake()
        {
            health = GetComponent<DragonHealth>();

            if (visual == null)
            {
                Transform found = transform.Find("Visual");
                visual = found != null ? found : transform;
            }

            baseScale = visual.localScale;

            Renderer visualRenderer = visual.GetComponent<Renderer>();
            if (visualRenderer != null)
            {
                visualMaterial = visualRenderer.material; // instanced copy — never mutates the shared .mat asset
                baseColor = visualMaterial.color;
            }
        }

        private void OnEnable()
        {
            health.OnHealthChanged += HandleHealthChanged;

            foreach (AbilityBase ability in GetComponents<AbilityBase>())
            {
                ability.OnActivated += HandleAbilityActivated;
            }
        }

        private void OnDisable()
        {
            health.OnHealthChanged -= HandleHealthChanged;

            foreach (AbilityBase ability in GetComponents<AbilityBase>())
            {
                ability.OnActivated -= HandleAbilityActivated;
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (previousHealth >= 0f && current < previousHealth)
            {
                PlayHitFlash();
            }
            previousHealth = current;
        }

        private void HandleAbilityActivated()
        {
            PlayAttackPunch();
        }

        private void PlayAttackPunch()
        {
            if (punchRoutine != null)
            {
                StopCoroutine(punchRoutine);
                visual.localScale = baseScale;
            }
            punchRoutine = StartCoroutine(PunchRoutine());
        }

        private void PlayHitFlash()
        {
            if (visualMaterial == null)
            {
                return;
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator PunchRoutine()
        {
            float half = attackPunchDuration * 0.5f;
            yield return ScaleOverTime(baseScale, baseScale * attackPunchScale, half);
            yield return ScaleOverTime(visual.localScale, baseScale, half);
            punchRoutine = null;
        }

        private IEnumerator ScaleOverTime(Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                visual.localScale = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            visual.localScale = to;
        }

        private IEnumerator FlashRoutine()
        {
            visualMaterial.color = hitFlashColor;
            yield return new WaitForSeconds(hitFlashDuration);
            visualMaterial.color = baseColor;
            flashRoutine = null;
        }
    }
}
