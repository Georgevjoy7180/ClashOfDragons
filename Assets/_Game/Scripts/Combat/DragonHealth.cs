using System;
using UnityEngine;

namespace ClashOfDragons.Combat
{
    /// <summary>
    /// Shared health/damage system used by both the player and AI dragons — the bottom of the
    /// combat hierarchy for both PlayerController and DragonAI.
    /// </summary>
    public class DragonHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void Die()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            CurrentHealth = 0f;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            OnDeath?.Invoke();
        }

        // Editor-only hooks so health can be tested in isolation before the ability system (Phase 6+) exists.
        [ContextMenu("Debug/Take 20 Damage")]
        private void DebugTakeDamage()
        {
            TakeDamage(20f);
            Debug.Log($"{name}: {CurrentHealth}/{MaxHealth} HP (IsDead={IsDead})");
        }

        [ContextMenu("Debug/Heal 20")]
        private void DebugHeal()
        {
            Heal(20f);
            Debug.Log($"{name}: {CurrentHealth}/{MaxHealth} HP (IsDead={IsDead})");
        }

        [ContextMenu("Debug/Kill")]
        private void DebugKill()
        {
            Die();
            Debug.Log($"{name}: {CurrentHealth}/{MaxHealth} HP (IsDead={IsDead})");
        }
    }
}
