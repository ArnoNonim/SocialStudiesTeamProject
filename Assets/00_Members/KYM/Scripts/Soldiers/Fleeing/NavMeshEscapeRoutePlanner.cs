using _00_Members.KYM.Scripts.Soldiers.Movement;
using UnityEngine;
using UnityEngine.AI;

namespace _00_Members.KYM.Scripts.Soldiers.Fleeing
{
    internal interface IEscapeRoutePlanner
    {
        bool TrySetEscapeRoute(
            ISoldierNavigation navigation,
            Vector3 soldierPosition,
            Vector3 soldierForward,
            Vector3 threatPosition);
    }

    internal sealed class NavMeshEscapeRoutePlanner : IEscapeRoutePlanner
    {
        private readonly float _fleeDistance;
        private readonly float _routeVariation;
        private readonly float _sampleRadius;

        public NavMeshEscapeRoutePlanner(
            float fleeDistance,
            float routeVariation,
            float sampleRadius)
        {
            _fleeDistance = fleeDistance;
            _routeVariation = routeVariation;
            _sampleRadius = sampleRadius;
        }

        public bool TrySetEscapeRoute(
            ISoldierNavigation navigation,
            Vector3 soldierPosition,
            Vector3 soldierForward,
            Vector3 threatPosition)
        {
            NavMeshAgent agent = navigation?.NavAgent;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return false;
            }

            Vector3 away = soldierPosition - threatPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
            {
                away = -soldierForward;
            }

            away.Normalize();
            float[] angles =
            {
                0f,
                _routeVariation,
                -_routeVariation,
                _routeVariation * 0.55f,
                -_routeVariation * 0.55f
            };

            float bestScore = float.NegativeInfinity;
            Vector3 bestDestination = soldierPosition;
            bool foundRoute = false;

            foreach (float angle in angles)
            {
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * away;
                Vector3 candidate = soldierPosition + direction * _fleeDistance;
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, _sampleRadius, agent.areaMask))
                {
                    continue;
                }

                NavMeshPath path = new NavMeshPath();
                if (!NavMesh.CalculatePath(soldierPosition, hit.position, agent.areaMask, path) ||
                    path.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                float threatSeparation = HorizontalDistance(hit.position, threatPosition);
                float routeLength = CalculatePathLength(path);
                float detour = Mathf.Max(0f, routeLength - _fleeDistance);
                float score = threatSeparation - detour * 0.2f - Mathf.Abs(angle) * 0.002f;
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestDestination = hit.position;
                foundRoute = true;
            }

            return foundRoute && navigation.TrySetDestination(bestDestination);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static float CalculatePathLength(NavMeshPath path)
        {
            float length = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }

            return length;
        }
    }
}
