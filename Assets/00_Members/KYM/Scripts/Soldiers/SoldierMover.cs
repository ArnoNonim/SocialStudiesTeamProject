using KimLIb.ModuleSystems;
using UnityEngine;
using UnityEngine.AI;

namespace _00_Members.KYM.Scripts.Soldiers
{
    public class SoldierMover : MonoBehaviour, IModule
    {
        [SerializeField] private NavMeshAgent navAgent;

        private bool _initiallyEnabled;

        public NavMeshAgent NavAgent { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            NavAgent = navAgent != null
                ? navAgent
                : GetComponent<NavMeshAgent>() ?? owner.GetComponentInChildren<NavMeshAgent>(true);
            _initiallyEnabled = NavAgent != null && NavAgent.enabled;
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
            }
        }
    }
}
