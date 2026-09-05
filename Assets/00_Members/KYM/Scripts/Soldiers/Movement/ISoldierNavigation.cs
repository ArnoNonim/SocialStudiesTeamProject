using UnityEngine;
using UnityEngine.AI;

namespace _00_Members.KYM.Scripts.Soldiers.Movement
{
    public interface ISoldierNavigation
    {
        NavMeshAgent NavAgent { get; }
        bool BeginRootMotionNavigation();
        void EndRootMotionNavigation();
        bool TrySetDestination(Vector3 destination);
        bool TryApplyRootMotion(Vector3 positionDelta, Quaternion rotationDelta);
        void StopPath();
    }
}
