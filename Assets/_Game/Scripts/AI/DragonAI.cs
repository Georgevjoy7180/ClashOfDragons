using UnityEngine;
using ClashOfDragons.Combat;

namespace ClashOfDragons.AI
{
    public enum AiState
    {
        Idle,
        Chase,
        Attack
    }

    /// <summary>
    /// Simple finite-state-machine AI (Idle / Chase / Attack) that decides WHEN to move or attack.
    /// It calls the exact same DragonCombat.TryUseAbility() that PlayerController calls, so it runs
    /// on the exact same cooldowns — there's no separate "AI cooldown" to keep in sync.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(DragonCombat))]
    public class DragonAI : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 540f;
        [SerializeField] private float engageBuffer = 0.2f;

        private Rigidbody body;
        private DragonCombat combat;
        private FireAttack fireAttack;
        private TailAttack tailAttack;
        private FlyAttack flyAttack;

        public AiState CurrentState { get; private set; } = AiState.Idle;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            combat = GetComponent<DragonCombat>();
        }

        private void Start()
        {
            fireAttack = combat.GetAbility(AbilitySlot.Fire) as FireAttack;
            tailAttack = combat.GetAbility(AbilitySlot.Tail) as TailAttack;
            flyAttack = combat.GetAbility(AbilitySlot.Fly) as FlyAttack;
        }

        private void FixedUpdate()
        {
            if (combat.IsBusy)
            {
                return;
            }

            DragonHealth self = combat.Self;
            DragonHealth target = combat.Target;

            if (self == null || self.IsDead || target == null || target.IsDead)
            {
                CurrentState = AiState.Idle;
                Stop();
                return;
            }

            Decide(target);
        }

        private void Decide(DragonHealth target)
        {
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            // Fly Attack ready + suitable situation: only when not already in melee range,
            // otherwise it would fight Tail for the same close-range moment.
            if (flyAttack != null && flyAttack.IsReady && tailAttack != null && distance > tailAttack.Range + engageBuffer)
            {
                CurrentState = AiState.Attack;
                Stop();
                FaceTarget(toTarget);
                combat.TryUseAbility(AbilitySlot.Fly);
                return;
            }

            if (tailAttack != null && distance <= tailAttack.Range)
            {
                CurrentState = AiState.Attack;
                Stop();
                FaceTarget(toTarget);
                combat.TryUseAbility(AbilitySlot.Tail);
                return;
            }

            if (fireAttack != null && distance <= fireAttack.Range)
            {
                CurrentState = AiState.Attack;
                Stop();
                FaceTarget(toTarget);
                combat.TryUseAbility(AbilitySlot.Fire);
                return;
            }

            CurrentState = AiState.Chase;
            MoveTowards(toTarget);
        }

        private void MoveTowards(Vector3 toTarget)
        {
            Vector3 direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.zero;

            Vector3 velocity = body.linearVelocity;
            velocity.x = direction.x * moveSpeed;
            velocity.z = direction.z * moveSpeed;
            body.linearVelocity = velocity;

            FaceTarget(direction);
        }

        private void Stop()
        {
            Vector3 velocity = body.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            body.linearVelocity = velocity;
        }

        private void FaceTarget(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }
}
