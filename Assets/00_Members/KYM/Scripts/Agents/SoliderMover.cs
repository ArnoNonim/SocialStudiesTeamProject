using KimLIb.ModuleSystems;
using UnityEngine;
using UnityEngine.AI;

namespace _00_Members.KYM.Scripts.Agents
{
    public class SoldierMover : MonoBehaviour, IModule
    {
        [SerializeField] private float walkNavigationSpeed = 1.8f;
        [SerializeField] private float runNavigationSpeed = 4.5f;
        [SerializeField] private float rotationSpeed = 540f;
        [SerializeField] private float arrivalDistance = 0.15f;
        
        [SerializeField] private float fleeDistance = 12f;
        [SerializeField] private float navMeshSearchRadius = 4f;

        private ModuleOwner _owner;
        private NavMeshAgent _agent;
        private Animator animator;
        private Transform lookTarget;
        private Vector3 rootMotionVelocity;

        public Vector3 Velocity => rootMotionVelocity;
        public bool IsMoving => _agent != null && _agent.isOnNavMesh && !_agent.isStopped && _agent.hasPath;
        public bool IsRunning { get; private set; }

        public bool HasArrived =>
            _agent != null &&
            _agent.isOnNavMesh &&
            (_agent.isStopped ||
             (!_agent.pathPending &&
              _agent.hasPath &&
              _agent.remainingDistance <= _agent.stoppingDistance + arrivalDistance));

        public void Initialize(ModuleOwner moduleOwner)
        {
            _owner = moduleOwner;
            _agent = _owner.GetComponent<NavMeshAgent>();
            animator = _owner.GetComponentInChildren<Animator>();

            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _agent.nextPosition = _owner.transform.position;
        }

        private void Update()
        {
            rootMotionVelocity = Vector3.zero;

            if (_agent == null || !_agent.isOnNavMesh)
                return;

            _agent.nextPosition = _owner.transform.position;

            if (lookTarget != null)
            {
                RotateTowards(lookTarget.position);
                return;
            }

            if (IsMoving)
                RotateTowards(_agent.steeringTarget);
        }

        public bool MoveTo(Vector3 destination, bool run = false)
        {
            if (_agent == null || !_agent.isOnNavMesh)
                return false;

            lookTarget = null;
            IsRunning = run;
            _agent.speed = run ? runNavigationSpeed : walkNavigationSpeed;
            _agent.isStopped = false;

            return _agent.SetDestination(destination);
        }

        public void Stop()
        {
            lookTarget = null;
            IsRunning = false;

            if (_agent == null || !_agent.isOnNavMesh)
                return;

            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.nextPosition = _owner.transform.position;
        }

        public void LookAt(Transform target)
        {
            Stop();
            lookTarget = target;
        }

        public bool FleeFrom(Vector3 threatPosition)
        {
            Vector3 direction = _owner.transform.position - threatPosition;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.01f)
                direction = -_owner.transform.forward;

            Vector3 candidate = _owner.transform.position + direction.normalized * fleeDistance;

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

        internal void ApplyRootMotion(Vector3 deltaPosition)
        {
            if (!IsMoving || Time.deltaTime <= 0f)
            {
                rootMotionVelocity = Vector3.zero;
                return;
            }

            Vector3 motion = deltaPosition;
            motion.y = 0f;

            _owner.transform.position += motion;
            _agent.nextPosition = _owner.transform.position;
            rootMotionVelocity = motion / Time.deltaTime;

            if (HasArrived)
                Stop();
        }

        private void RotateTowards(Vector3 position)
        {
            Vector3 direction = position - _owner.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _owner.transform.rotation = Quaternion.RotateTowards(
                _owner.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }
    }
}
