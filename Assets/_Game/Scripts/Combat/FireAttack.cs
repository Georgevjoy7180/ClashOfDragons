using UnityEngine;

namespace ClashOfDragons.Combat
{
    /// <summary>
    /// Ranged attack: damages the target if it's within range and roughly in front of the dragon.
    /// VFX is a beam (LineRenderer, still a procedural placeholder — no art assets) plus a particle
    /// burst at the impact point from the shared VfxUtility.
    /// </summary>
    public class FireAttack : AbilityBase
    {
        [SerializeField] private float range = 7f;
        [SerializeField] private float halfConeAngle = 60f;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Color vfxColor = new Color(1f, 0.45f, 0.1f);
        [SerializeField] private float vfxDuration = 0.15f;

        public override AbilitySlot Slot => AbilitySlot.Fire;
        public float Range => range;

        private void Reset()
        {
            cooldown = 4f;
            damage = 20f;
        }

        protected override void Awake()
        {
            base.Awake();
            if (firePoint == null)
            {
                Transform found = transform.Find("FirePoint");
                firePoint = found != null ? found : transform;
            }
        }

        protected override void Activate(DragonHealth target)
        {
            if (target == null || target.IsDead)
            {
                return;
            }

            Vector3 origin = firePoint.position;
            Vector3 toTarget = target.transform.position - origin;
            toTarget.y = 0f;

            if (toTarget.magnitude > range)
            {
                return;
            }

            Vector3 forward = firePoint.forward;
            forward.y = 0f;

            if (Vector3.Angle(forward, toTarget) > halfConeAngle)
            {
                return;
            }

            target.TakeDamage(damage);
            SpawnFireVfx(origin, target.transform.position);
        }

        private void SpawnFireVfx(Vector3 from, Vector3 to)
        {
            GameObject beam = new GameObject("FireVfx_Beam");
            LineRenderer line = beam.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = 0.15f;
            line.endWidth = 0.05f;

            Shader shader = VfxUtility.GetVfxShader();
            if (shader != null)
            {
                line.material = new Material(shader);
                line.material.color = vfxColor;
            }

            Destroy(beam, vfxDuration);

            VfxUtility.SpawnBurst(to, vfxColor, vfxDuration);
        }
    }
}
