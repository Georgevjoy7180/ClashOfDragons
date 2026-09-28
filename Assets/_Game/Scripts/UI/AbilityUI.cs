using UnityEngine;
using UnityEngine.UI;
using ClashOfDragons.Combat;

namespace ClashOfDragons.UI
{
    /// <summary>
    /// Drives one ability slot's radial cooldown overlay + countdown text. Cooldown is a continuous
    /// timer (not a discrete event like health), so this polls AbilityBase.CooldownRemaining every
    /// frame — that's the one legitimate case for polling instead of an event.
    /// </summary>
    public class AbilityUI : MonoBehaviour
    {
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private Text cooldownText;

        // Must be [SerializeField] — a plain private field gets wiped to null on every script
        // recompile (see GameManager.cs for the full explanation), which would silently freeze
        // this slot's cooldown display after any future code change.
        [SerializeField] private AbilityBase ability;

        /// <summary>Wired up by HudBuilder (Editor-time).</summary>
        public void Initialize(AbilityBase abilityToTrack, Image overlay, Text text)
        {
            ability = abilityToTrack;
            cooldownOverlay = overlay;
            cooldownText = text;
        }

        private void Update()
        {
            if (ability == null)
            {
                return;
            }

            bool isOnCooldown = ability.CooldownRemaining > 0f;

            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = ability.Cooldown > 0f
                    ? Mathf.Clamp01(ability.CooldownRemaining / ability.Cooldown)
                    : 0f;
            }

            if (cooldownText != null)
            {
                cooldownText.enabled = isOnCooldown;
                if (isOnCooldown)
                {
                    cooldownText.text = ability.CooldownRemaining.ToString("F1");
                }
            }
        }
    }
}
