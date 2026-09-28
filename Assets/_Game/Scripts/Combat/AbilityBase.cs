using System;
using UnityEngine;

namespace ClashOfDragons.Combat
{
    public enum AbilitySlot
    {
        Fire,
        Tail,
        Fly
    }

    /// <summary>
    /// Common cooldown + activation contract for every dragon ability. DragonCombat discovers all
    /// AbilityBase components on a dragon automatically (GetComponents), so a new ability only has
    /// to exist as a component here — no other script needs to change to support it.
    /// </summary>
    [RequireComponent(typeof(DragonHealth))]
    public abstract class AbilityBase : MonoBehaviour
    {
        [SerializeField] protected float cooldown = 1f;
        [SerializeField] protected float damage = 10f;

        private float cooldownTimer;

        public abstract AbilitySlot Slot { get; }
        public float Cooldown => cooldown;
        public float CooldownRemaining => Mathf.Max(cooldownTimer, 0f);
        public bool IsReady => cooldownTimer <= 0f;

        /// <summary>True while a multi-stage ability (e.g. Fly) is mid-sequence. DragonCombat ORs
        /// this across all abilities so PlayerController/DragonAI can suspend normal movement and
        /// other abilities without knowing which ability is actually running.</summary>
        public bool IsBusy { get; protected set; }

        protected DragonHealth Self { get; private set; }

        /// <summary>Fires every time this ability successfully activates — the animation/VFX hook
        /// (e.g. Phase 16's Animator can subscribe SetTrigger("Fire") here) without Activate() itself
        /// needing to know anything about animation.</summary>
        public event Action OnActivated;

        protected virtual void Awake()
        {
            Self = GetComponent<DragonHealth>();
        }

        protected virtual void Update()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
            }
        }

        public bool TryActivate(DragonHealth target)
        {
            if (!IsReady || Self == null || Self.IsDead)
            {
                return false;
            }

            cooldownTimer = cooldown;
            Activate(target);
            OnActivated?.Invoke();
            return true;
        }

        protected abstract void Activate(DragonHealth target);
    }
}
