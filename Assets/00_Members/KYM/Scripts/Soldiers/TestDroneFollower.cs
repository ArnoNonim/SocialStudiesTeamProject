using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers
{
    public sealed class TestDroneFollower : MonoBehaviour
    {
        [SerializeField] private Soldier target;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float followHeight = 2.5f;
        [SerializeField, Min(0f)] private float stoppingDistance = 0.75f;
        [SerializeField, Min(0f)] private float turnSpeed = 240f;

        public Transform ThreatTransform => transform;

        private void Awake()
        {
            if (target == null)
            {
                target = FindFirstObjectByType<Soldier>();
            }
        }

        private void Update()
        {
            if (target == null || target.IsDead)
            {
                return;
            }

            Vector3 destination = target.transform.position + Vector3.up * followHeight;
            Vector3 toTarget = destination - transform.position;
            float distance = toTarget.magnitude;
            if (distance <= stoppingDistance)
            {
                return;
            }

            float travelDistance = Mathf.Min(
                moveSpeed * Time.deltaTime,
                distance - stoppingDistance);
            transform.position += toTarget.normalized * travelDistance;

            Vector3 horizontalDirection = toTarget;
            horizontalDirection.y = 0f;
            if (horizontalDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(
                    horizontalDirection.normalized,
                    Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (target == null)
            {
                return;
            }

            Vector3 destination = target.transform.position + Vector3.up * followHeight;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, destination);
            Gizmos.DrawWireSphere(destination, stoppingDistance);
        }
    }
}
