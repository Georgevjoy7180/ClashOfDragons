using System.Collections.Generic;
using UnityEngine;

namespace ClashOfDragons.Combat
{
    /// <summary>
    /// Central combat hub shared by PlayerController and DragonAI. Neither caller needs to know
    /// which concrete abilities exist — it just asks DragonCombat to use a given AbilitySlot, and
    /// DragonCombat looks up whichever AbilityBase component fills that slot.
    /// </summary>
    [RequireComponent(typeof(DragonHealth))]
    public class DragonCombat : MonoBehaviour
    {
        [SerializeField] private DragonHealth target;

        private readonly Dictionary<AbilitySlot, AbilityBase> abilities = new Dictionary<AbilitySlot, AbilityBase>();

        public DragonHealth Self { get; private set; }
        public DragonHealth Target => target;

        public bool IsBusy
        {
            get
            {
                foreach (AbilityBase ability in abilities.Values)
                {
                    if (ability.IsBusy)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private void Awake()
        {
            Self = GetComponent<DragonHealth>();
            foreach (AbilityBase ability in GetComponents<AbilityBase>())
            {
                abilities[ability.Slot] = ability;
            }
        }

        /// <summary>Wired up by CharacterBuilder (Editor-time) once both dragons exist in the scene.</summary>
        public void SetTarget(DragonHealth newTarget)
        {
            target = newTarget;
        }

        public bool TryUseAbility(AbilitySlot slot)
        {
            if (IsBusy || Target == null || Target.IsDead || !abilities.TryGetValue(slot, out AbilityBase ability))
            {
                return false;
            }

            return ability.TryActivate(Target);
        }

        public AbilityBase GetAbility(AbilitySlot slot)
        {
            return abilities.TryGetValue(slot, out AbilityBase ability) ? ability : null;
        }

        [ContextMenu("Debug/Log Combat State")]
        private void DebugLogState()
        {
            string targetName = Target != null ? Target.name : "null";
            Debug.Log($"{name}: Target={targetName}, AbilitiesFound={abilities.Count}");
        }
    }
}
