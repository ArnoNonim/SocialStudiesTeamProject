using _00_Members.KYM.Scripts.Soldiers;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Humans.Effects
{
    public sealed class CorpseSettlingModule : MonoBehaviour, IModule
    {
        [SerializeField, Min(0f)] private float minimumSimulationTime = 2.5f;
        [SerializeField, Min(0.1f)] private float quietTimeRequired = 1.15f;
        [SerializeField, Min(0.01f)] private float linearSpeedThreshold = 0.12f;
        [SerializeField, Min(0.01f)] private float angularSpeedThreshold = 0.55f;
        [SerializeField, Min(0f)] private float dampingRampDuration = 3f;
        [SerializeField, Min(0f)] private float settledLinearDamping = 2.5f;
        [SerializeField, Min(0f)] private float settledAngularDamping = 4f;

        private AbstractHuman _human;
        private RagdollController _ragdoll;
        private Rigidbody[] _bodies;
        private float[] _initialLinearDamping;
        private float[] _initialAngularDamping;
        private bool _monitoring;
        private bool _settled;
        private float _deathTime;
        private float _quietSince = -1f;

        public bool IsSettled => _settled;

        public void Initialize(ModuleOwner owner)
        {
            _human = owner as AbstractHuman;
            _ragdoll = owner.GetModule<RagdollController>();
            _bodies = owner.GetComponentsInChildren<Rigidbody>(true);
            _initialLinearDamping = new float[_bodies.Length];
            _initialAngularDamping = new float[_bodies.Length];
            for (int i = 0; i < _bodies.Length; i++)
            {
                _initialLinearDamping[i] = _bodies[i].linearDamping;
                _initialAngularDamping[i] = _bodies[i].angularDamping;
            }

            if (_human == null)
            {
                enabled = false;
                return;
            }

            _human.Died += OnDied;
            _human.Revived += OnRevived;
        }

        private void OnDied()
        {
            RestoreBodyDamping();
            _monitoring = true;
            _settled = false;
            _deathTime = Time.time;
            _quietSince = -1f;
        }

        private void FixedUpdate()
        {
            if (!_monitoring || _settled)
            {
                return;
            }

            if (_ragdoll != null && !_ragdoll.IsRagdollActive)
            {
                return;
            }

            float elapsed = Time.time - _deathTime;
            if (elapsed < minimumSimulationTime)
            {
                return;
            }

            ApplyDampingRamp(elapsed - minimumSimulationTime);
            if (!AreBodiesQuiet())
            {
                _quietSince = -1f;
                return;
            }

            if (_quietSince < 0f)
            {
                _quietSince = Time.time;
                return;
            }

            if (Time.time - _quietSince >= quietTimeRequired)
            {
                SettleBodies();
            }
        }

        private bool AreBodiesQuiet()
        {
            bool foundDynamicBody = false;
            float linearThresholdSqr = linearSpeedThreshold * linearSpeedThreshold;
            float angularThresholdSqr = angularSpeedThreshold * angularSpeedThreshold;

            foreach (Rigidbody body in _bodies)
            {
                if (body == null || body.isKinematic || !body.detectCollisions)
                {
                    continue;
                }

                foundDynamicBody = true;
                if (body.linearVelocity.sqrMagnitude > linearThresholdSqr ||
                    body.angularVelocity.sqrMagnitude > angularThresholdSqr)
                {
                    return false;
                }
            }

            return foundDynamicBody;
        }

        private void ApplyDampingRamp(float elapsed)
        {
            float t = dampingRampDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / dampingRampDuration);
            for (int i = 0; i < _bodies.Length; i++)
            {
                Rigidbody body = _bodies[i];
                if (body == null || body.isKinematic)
                {
                    continue;
                }

                body.linearDamping = Mathf.Lerp(
                    _initialLinearDamping[i],
                    Mathf.Max(_initialLinearDamping[i], settledLinearDamping),
                    t);
                body.angularDamping = Mathf.Lerp(
                    _initialAngularDamping[i],
                    Mathf.Max(_initialAngularDamping[i], settledAngularDamping),
                    t);
            }
        }

        private void SettleBodies()
        {
            foreach (Rigidbody body in _bodies)
            {
                if (body == null || body.isKinematic || !body.detectCollisions)
                {
                    continue;
                }

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.Sleep();
                body.isKinematic = true;
            }

            _settled = true;
            _monitoring = false;
        }

        private void OnRevived()
        {
            _monitoring = false;
            _settled = false;
            _quietSince = -1f;
            RestoreBodyDamping();
        }

        private void RestoreBodyDamping()
        {
            for (int i = 0; i < _bodies.Length; i++)
            {
                if (_bodies[i] == null)
                {
                    continue;
                }

                _bodies[i].linearDamping = _initialLinearDamping[i];
                _bodies[i].angularDamping = _initialAngularDamping[i];
            }
        }

        private void OnDestroy()
        {
            if (_human == null)
            {
                return;
            }

            _human.Died -= OnDied;
            _human.Revived -= OnRevived;
        }
    }
}
