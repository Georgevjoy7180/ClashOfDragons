using UnityEngine;
using UnityEngine.InputSystem;
using ClashOfDragons.Combat;

namespace ClashOfDragons.Player
{
    /// <summary>
    /// Reads WASD movement and Q/E/R ability input from the player, and drives the dragon's
    /// Rigidbody + DragonCombat. Movement is world-space (W is always world +Z), matching the
    /// fixed top-down camera. This is only the WHEN — DragonCombat/abilities decide HOW each
    /// attack works.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(DragonCombat))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float rotationSpeed = 720f;

        private Rigidbody body;
        private DragonCombat combat;
        private Vector2 moveInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            combat = GetComponent<DragonCombat>();
        }

        private void Update()
        {
            if (combat.IsBusy)
            {
                // A multi-stage ability (Fly) owns the Rigidbody right now — ignore all input.
                moveInput = Vector2.zero;
                return;
            }

            moveInput = ReadMoveInput();
            ReadAbilityInput();
        }

        private void ReadAbilityInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                combat.TryUseAbility(AbilitySlot.Fire);
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                combat.TryUseAbility(AbilitySlot.Tail);
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                combat.TryUseAbility(AbilitySlot.Fly);
            }
        }

        private void FixedUpdate()
        {
            if (combat.IsBusy)
            {
                return;
            }

            Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
            direction = Vector3.ClampMagnitude(direction, 1f);

            ApplyMovement(direction);
            ApplyRotation(direction);
        }

        private static Vector2 ReadMoveInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            float horizontal = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float vertical = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            return new Vector2(horizontal, vertical);
        }

        private void ApplyMovement(Vector3 direction)
        {
            Vector3 targetVelocity = direction * moveSpeed;
            Vector3 velocity = body.linearVelocity;
            velocity.x = targetVelocity.x;
            velocity.z = targetVelocity.z;
            body.linearVelocity = velocity;
        }

        private void ApplyRotation(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }
}
