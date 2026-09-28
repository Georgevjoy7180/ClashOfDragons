using System.Collections;
using UnityEngine;

namespace ClashOfDragons.Combat
{
    /// <summary>
    /// Multi-stage gap-closer: Takeoff -> Rise -> Move/Attack -> Deal Damage -> Land.
    /// Movement is fully scripted (Rigidbody goes kinematic for the duration) so the arc doesn't
    /// fight normal physics. Sets IsBusy for the whole sequence — PlayerController checks that and
    /// stops driving input, which is how "disable normal movement during the fly attack" happens.
    /// </summary>
    public class FlyAttack : AbilityBase
    {
        [SerializeField] private float riseHeight = 3f;
        [SerializeField] private float landingDistance = 1.5f;
        [SerializeField] private float damageRadius = 3f;
        [SerializeField] private float takeoffDuration = 0.15f;
        [SerializeField] private float riseDuration = 0.25f;
        [SerializeField] private float attackMoveDuration = 0.35f;
        [SerializeField] private float landDuration = 0.25f;
        [SerializeField] private Color vfxColor = new Color(0.9f, 0.2f, 0.9f);
        [SerializeField] private float vfxDuration = 0.2f;

        private Rigidbody body;

        public override AbilitySlot Slot => AbilitySlot.Fly;

        private void Reset()
        {
            cooldown = 8f;
            damage = 35f;
        }

        protected override void Awake()
        {
            base.Awake();
            body = GetComponent<Rigidbody>();
        }

        protected override void Activate(DragonHealth target)
        {
            if (target == null || target.IsDead)
            {
                return;
            }

            StartCoroutine(FlySequence(target));
        }

        private IEnumerator FlySequence(DragonHealth target)
        {
            IsBusy = true;
            body.isKinematic = true;

            Vector3 startPosition = transform.position;
            Vector3 landingPosition = ComputeLandingPosition(startPosition, target.transform.position);
            Vector3 risenStart = startPosition + Vector3.up * riseHeight;
            Vector3 risenLanding = landingPosition + Vector3.up * riseHeight;

            FaceTowards(landingPosition - startPosition);

            // Takeoff: brief anticipation before leaving the ground.
            yield return new WaitForSeconds(takeoffDuration);

            // Rise: lift straight up.
            yield return MoveOverTime(startPosition, risenStart, riseDuration);

            // Move/attack: cross the arena to just beside the target, still elevated.
            yield return MoveOverTime(risenStart, risenLanding, attackMoveDuration);

            // Deal damage: instantaneous hit on arrival.
            TryDealDamage(target);

            // Land: descend back to the ground.
            yield return MoveOverTime(risenLanding, landingPosition, landDuration);

            body.isKinematic = false;
            IsBusy = false;
        }

        private Vector3 ComputeLandingPosition(Vector3 startPosition, Vector3 targetPosition)
        {
            Vector3 approachDirection = startPosition - targetPosition;
            approachDirection.y = 0f;
            approachDirection = approachDirection.sqrMagnitude > 0.0001f ? approachDirection.normalized : Vector3.back;

            Vector3 landingPosition = targetPosition + approachDirection * landingDistance;
            landingPosition.y = startPosition.y;
            return landingPosition;
        }

        private void FaceTowards(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void TryDealDamage(DragonHealth target)
        {
            if (target.IsDead)
            {
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude > damageRadius)
            {
                return;
            }

            target.TakeDamage(damage);
            SpawnImpactVfx(target.transform.position);
        }

        private void SpawnImpactVfx(Vector3 position)
        {
            // Bigger, faster burst than Fire/Tail — Fly is the highest-damage, most dramatic hit.
            VfxUtility.SpawnBurst(position + Vector3.up, vfxColor, vfxDuration, particleCount: 24, speed: 6f, size: 0.35f);
        }

        private IEnumerator MoveOverTime(Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                body.MovePosition(Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            body.MovePosition(to);
        }
    }
}
