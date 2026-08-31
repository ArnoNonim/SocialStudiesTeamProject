using DroneController;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers.Fleeing
{
    internal readonly struct FleeThreatObservation
    {
        public FleeThreatObservation(Vector3 position, float distance, float approachSpeed)
        {
            Position = position;
            Distance = distance;
            ApproachSpeed = approachSpeed;
        }

        public Vector3 Position { get; }
        public float Distance { get; }
        public float ApproachSpeed { get; }
    }

    internal interface IFleeThreatSensor
    {
        bool TryObserve(
            Vector3 observerPosition,
            float deltaTime,
            float currentTime,
            out FleeThreatObservation observation);
    }

    internal sealed class FleeThreatSensor : IFleeThreatSensor
    {
        private readonly Transform _override;
        private Transform _threat;
        private float _previousDistance;
        private float _nextSearchTime;
        private bool _hasDistanceSample;

        public FleeThreatSensor(Transform threatOverride)
        {
            _override = threatOverride;
            _threat = threatOverride;
        }

        public bool TryObserve(
            Vector3 observerPosition,
            float deltaTime,
            float currentTime,
            out FleeThreatObservation observation)
        {
            AcquireThreat(currentTime);
            if (_threat == null)
            {
                observation = default;
                return false;
            }

            float distance = GetPerceivedDistance(observerPosition, _threat.position);
            float approachSpeed = _hasDistanceSample && deltaTime > 0f
                ? (_previousDistance - distance) / deltaTime
                : 0f;

            _previousDistance = distance;
            _hasDistanceSample = true;
            observation = new FleeThreatObservation(_threat.position, distance, approachSpeed);
            return true;
        }

        private void AcquireThreat(float currentTime)
        {
            if (_override != null)
            {
                _threat = _override;
                return;
            }

            if (_threat != null || currentTime < _nextSearchTime)
            {
                return;
            }

            _nextSearchTime = currentTime + 1f;
            DroneMovement drone = Object.FindFirstObjectByType<DroneMovement>();
            if (drone != null)
            {
                SetThreat(drone.transform);
                return;
            }

            TestDroneFollower testDrone = Object.FindFirstObjectByType<TestDroneFollower>();
            if (testDrone != null)
            {
                SetThreat(testDrone.ThreatTransform);
            }
        }

        private void SetThreat(Transform threat)
        {
            _threat = threat;
            _hasDistanceSample = false;
        }

        private static float GetPerceivedDistance(Vector3 observerPosition, Vector3 threatPosition)
        {
            Vector3 offset = threatPosition - observerPosition;
            float vertical = offset.y * 0.35f;
            offset.y = 0f;
            return Mathf.Sqrt(offset.sqrMagnitude + vertical * vertical);
        }
    }
}
