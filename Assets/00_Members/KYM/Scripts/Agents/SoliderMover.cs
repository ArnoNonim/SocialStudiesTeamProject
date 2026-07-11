using UnityEngine;
using UnityEngine.AI;

namespace _00_Members.KYM.Scripts.Agents
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class SoldierMover : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 1.8f;
        [SerializeField] private float runSpeed = 4.5f;
        [SerializeField] private float rotationSpeed = 540f;
        [SerializeField] private float arrivalDistance = 0.15f;

        [Header("Flee")]
        [SerializeField] private float fleeDistance = 12f;
        [SerializeField] private float navMeshSearchRadius = 4f;

        private NavMeshAgent agent;
        private Transform lookTarget;

        public Vector3 Velocity => agent.velocity;

        public bool HasArrived =>
            agent.isOnNavMesh &&
            !agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance + arrivalDistance;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
        }

        private void Update()
        {
            if (lookTarget != null)
                RotateTowards(lookTarget.position);
            else if (agent.velocity.sqrMagnitude > 0.01f)
                RotateTowards(transform.position + agent.velocity);
        }

        public bool MoveTo(Vector3 destination, bool run = false)
        {
            if (!agent.isOnNavMesh)
                return false;

            lookTarget = null;
            agent.speed = run ? runSpeed : walkSpeed;
            agent.isStopped = false;

            return agent.SetDestination(destination);
        }

        public void Stop()
        {
            lookTarget = null;

            if (!agent.isOnNavMesh)
                return;

            agent.isStopped = true;
            agent.ResetPath();
        }

        public void LookAt(Transform target)
        {
            Stop();
            lookTarget = target;
        }

        public bool FleeFrom(Vector3 threatPosition)
        {
            Vector3 direction = transform.position - threatPosition;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.01f)
                direction = -transform.forward;

            Vector3 candidate = transform.position +
                                direction.normalized * fleeDistance;

            if (!NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    navMeshSearchRadius,
                    NavMesh.AllAreas))
            {
                return false;
            }

            return MoveTo(hit.position, true);
        }

        private void RotateTowards(Vector3 position)
        {
            Vector3 direction = position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }
    }
}