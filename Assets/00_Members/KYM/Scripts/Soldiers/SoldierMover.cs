using _00_Members.KYM.Scripts.Soldiers.Movement;
using KimLIb.ModuleSystems;
using UnityEngine;
using UnityEngine.AI;

namespace _00_Members.KYM.Scripts.Soldiers
{
    public class SoldierMover : MonoBehaviour, IModule, ISoldierNavigation
    {
        [SerializeField] private NavMeshAgent navAgent;

        private bool _initiallyEnabled;
        private bool _initialUpdatePosition;
        private bool _initialUpdateRotation;
        private float _initialSpeed;
        private Transform _ownerTransform;

        public NavMeshAgent NavAgent { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            _ownerTransform = owner.transform;
            NavAgent = navAgent != null
                ? navAgent
                : GetComponent<NavMeshAgent>() ?? owner.GetComponentInChildren<NavMeshAgent>(true);
            _initiallyEnabled = NavAgent != null && NavAgent.enabled;
            if (NavAgent != null)
            {
                _initialUpdatePosition = NavAgent.updatePosition;
                _initialUpdateRotation = NavAgent.updateRotation;
                _initialSpeed = NavAgent.speed;
            }
        }

        public bool BeginRootMotionNavigation()
        {
            if (NavAgent == null || !NavAgent.enabled || !NavAgent.isOnNavMesh)
            {
                return false;
            }

            NavAgent.updatePosition = false;
            NavAgent.updateRotation = false;
            NavAgent.speed = 0f;
            NavAgent.nextPosition = _ownerTransform.position;
            NavAgent.isStopped = false;
            return true;
        }

        public bool TrySetDestination(Vector3 destination)
        {
            if (NavAgent == null || !NavAgent.enabled || !NavAgent.isOnNavMesh)
            {
                return false;
            }

            NavAgent.isStopped = false;
            return NavAgent.SetDestination(destination);
        }

        public bool TryApplyRootMotion(Vector3 positionDelta, Quaternion rotationDelta)
        {
            if (NavAgent == null ||
                !NavAgent.enabled ||
                !NavAgent.isOnNavMesh ||
                _ownerTransform == null)
            {
                return false;
            }

            // NavMeshAgent.Move constrains animation-driven displacement to the
            // currently baked NavMesh instead of letting the root cross its edge.
            NavAgent.Move(positionDelta);
            _ownerTransform.position = NavAgent.nextPosition;
            _ownerTransform.rotation *= rotationDelta;
            return true;
        }

        public void StopPath()
        {
            if (NavAgent == null || !NavAgent.enabled || !NavAgent.isOnNavMesh)
            {
                return;
            }

            NavAgent.isStopped = true;
            NavAgent.ResetPath();
        }

        public void EndRootMotionNavigation()
        {
            StopPath();
            if (NavAgent == null || !NavAgent.enabled)
            {
                return;
            }

            if (NavAgent.isOnNavMesh)
            {
                NavAgent.Warp(_ownerTransform.position);
            }

            NavAgent.speed = _initialSpeed;
            NavAgent.updatePosition = _initialUpdatePosition;
            NavAgent.updateRotation = _initialUpdateRotation;
        }

        public void Stop()
        {
            if (NavAgent == null)
            {
                return;
            }

            if (NavAgent.isOnNavMesh)
            {
                NavAgent.ResetPath();
            }

            NavAgent.enabled = false;
        }

        public void ResetMovement(Vector3 worldPosition)
        {
            if (NavAgent == null)
            {
                return;
            }

            NavAgent.enabled = _initiallyEnabled;
            if (NavAgent.enabled && NavAgent.isOnNavMesh)
            {
                NavAgent.Warp(worldPosition);
                NavAgent.ResetPath();
                NavAgent.speed = _initialSpeed;
                NavAgent.updatePosition = _initialUpdatePosition;
                NavAgent.updateRotation = _initialUpdateRotation;
            }
        }

        private void LateUpdate()
        {
            if (NavAgent != null &&
                NavAgent.enabled &&
                NavAgent.isOnNavMesh &&
                !NavAgent.updatePosition &&
                _ownerTransform != null)
            {
                NavAgent.nextPosition = _ownerTransform.position;
            }
        }
    }
}
