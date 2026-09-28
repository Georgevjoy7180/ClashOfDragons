using UnityEngine;

namespace ClashOfDragons.Cameras
{
    // Namespace is "Cameras" (plural), not "Camera" — a namespace segment named "Camera" would
    // shadow UnityEngine.Camera for every other file under the ClashOfDragons.* tree, since C#
    // resolves enclosing-namespace members before usings.
    /// <summary>
    /// Simple angled top-down camera: keeps a fixed offset (the same angle set up in Phase 2) and
    /// only ever translates to follow the midpoint between both dragons, in LateUpdate so it tracks
    /// after all movement for the frame has already happened. Deliberately not a complex system —
    /// no dynamic zoom, no rotation changes, just "stay above the midpoint."
    /// </summary>
    public class BattleCamera : MonoBehaviour
    {
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform enemyTarget;
        [SerializeField] private Vector3 offset = new Vector3(0f, 13f, -12f);
        [SerializeField] private float followSpeed = 5f;

        /// <summary>Wired up by ArenaBuilder (Editor-time), once both dragons exist.</summary>
        public void Initialize(Transform player, Transform enemy)
        {
            playerTarget = player;
            enemyTarget = enemy;
        }

        private void LateUpdate()
        {
            if (playerTarget == null || enemyTarget == null)
            {
                return;
            }

            Vector3 midpoint = (playerTarget.position + enemyTarget.position) * 0.5f;
            Vector3 desiredPosition = midpoint + offset;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        }
    }
}
