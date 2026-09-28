using UnityEngine;

namespace ClashOfDragons.Combat
{
    /// <summary>
    /// Short-range melee attack: damages and knocks back any target within range, regardless of
    /// facing (unlike FireAttack's frontal cone — a tail swipe hits whatever is close).
    /// </summary>
    public class TailAttack : AbilityBase
    {
        [SerializeField] private float range = 2.5f;
        [SerializeField] private float knockbackForce = 6f;
        [SerializeField] private Color vfxColor = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private float vfxDuration = 0.12f;

        public override AbilitySlot Slot => AbilitySlot.Tail;
        public float Range => range;

        private void Reset()
        {
            cooldown = 3f;
            damage = 25f;
        }

        protected override void Activate(DragonHealth target)
        {
            if (target == null || target.IsDead)
            {
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude > range)
            {
                return;
            }

            target.TakeDamage(damage);
            ApplyKnockback(target, toTarget);
            SpawnHitVfx(target.transform.position);
        }

        private void ApplyKnockback(DragonHealth target, Vector3 toTarget)
        {
            Rigidbody targetBody = target.GetComponent<Rigidbody>();
            // A kinematic Rigidbody (target is mid-Fly-Attack, see FlyAttack.FlySequence) can't take
            // a velocity push — it's under scripted position control for the duration. Damage still
            // applies above; only the physics knockback is skipped here.
            if (targetBody == null || targetBody.isKinematic || knockbackForce <= 0f || toTarget.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 direction = toTarget.normalized;
            Vector3 velocity = targetBody.linearVelocity;
            velocity.x = direction.x * knockbackForce;
            velocity.z = direction.z * knockbackForce;
            targetBody.linearVelocity = velocity;
        }

        private void SpawnHitVfx(Vector3 position)
        {
            VfxUtility.SpawnBurst(position + Vector3.up, vfxColor, vfxDuration);
        }
    }
}
