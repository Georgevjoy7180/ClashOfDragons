using UnityEngine;
using UnityEngine.UI;
using ClashOfDragons.Combat;

namespace ClashOfDragons.UI
{
    /// <summary>
    /// Drives one health bar's fill amount from a DragonHealth's OnHealthChanged event — no
    /// per-frame polling, the bar only updates on the frame health actually changes.
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private DragonHealth target;
        [SerializeField] private Image fillImage;

        /// <summary>Wired up by HudBuilder (Editor-time) once the dragon it tracks exists.</summary>
        public void Initialize(DragonHealth dragonHealth, Image fill)
        {
            target = dragonHealth;
            fillImage = fill;
        }

        private void OnEnable()
        {
            if (target == null)
            {
                return;
            }

            target.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(target.CurrentHealth, target.MaxHealth);
        }

        private void OnDisable()
        {
            if (target == null)
            {
                return;
            }

            target.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (fillImage == null || max <= 0f)
            {
                return;
            }

            fillImage.fillAmount = Mathf.Clamp01(current / max);
        }
    }
}
