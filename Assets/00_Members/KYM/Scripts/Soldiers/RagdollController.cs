using KimLIb.ModuleSystems;
using _00_Members.KYM.Scripts.Soldiers.Explosions;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers
{
    public class RagdollController : MonoBehaviour, IModule
    {
        [SerializeField] private Transform ragdollRoot;
        [SerializeField] private Rigidbody headRigidbody;
        [SerializeField, Min(0f)] private float settlingSpeed = 0.35f;

        private Rigidbody[] _ragdollRigidbodies;
        private Collider[] _ragdollColliders;
        private Transform[] _poseTransforms;
        private Vector3[] _initialLocalPositions;
        private Quaternion[] _initialLocalRotations;
        private Animator _animator;

        public bool IsRagdollActive { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            _animator = owner.GetComponentInChildren<Animator>(true);
            ragdollRoot = ragdollRoot != null
                ? ragdollRoot
                : _animator != null ? _animator.transform : owner.transform;

            _ragdollRigidbodies = ragdollRoot.GetComponentsInChildren<Rigidbody>(true);
            _ragdollColliders = ragdollRoot.GetComponentsInChildren<Collider>(true);
            _poseTransforms = ragdollRoot.GetComponentsInChildren<Transform>(true);
            _initialLocalPositions = new Vector3[_poseTransforms.Length];
            _initialLocalRotations = new Quaternion[_poseTransforms.Length];

            for (int i = 0; i < _poseTransforms.Length; i++)
            {
                _initialLocalPositions[i] = _poseTransforms[i].localPosition;
                _initialLocalRotations[i] = _poseTransforms[i].localRotation;
            }

            if (headRigidbody == null && _animator != null && _animator.isHuman)
            {
                Transform headBone = _animator.GetBoneTransform(HumanBodyBones.Head);
                if (headBone != null)
                {
                    headRigidbody = headBone.GetComponent<Rigidbody>();
                }
            }

            SetRagdollActive(false);
        }

        public void EnableRagdoll(Vector3 hitPoint, Vector3 forceDirection, float force)
        {
            if (IsRagdollActive)
            {
                return;
            }

            if (_animator != null)
            {
                _animator.enabled = false;
            }

            Physics.SyncTransforms();
            SetRagdollActive(true);

            Rigidbody targetRigidbody = GetClosestRigidbody(hitPoint);
            if (targetRigidbody != null && forceDirection.sqrMagnitude > 0f && force > 0f)
            {
                targetRigidbody.AddForceAtPosition(
                    forceDirection.normalized * force,
                    hitPoint,
                    ForceMode.Impulse);
            }
        }

        public void EnableExplosionRagdoll(ExplosionContext context)
        {
            if (IsRagdollActive)
            {
                return;
            }

            if (_animator != null)
            {
                _animator.enabled = false;
            }

            Physics.SyncTransforms();
            SetRagdollActive(true);
            foreach (Rigidbody rigidbody in _ragdollRigidbodies)
            {
                rigidbody.maxAngularVelocity = Mathf.Min(rigidbody.maxAngularVelocity, 9f);
                rigidbody.angularDamping = Mathf.Max(rigidbody.angularDamping, 0.45f);
                rigidbody.AddExplosionForce(
                    context.Force * context.Exposure,
                    context.Center,
                    context.Radius,
                    context.UpwardModifier,
                    ForceMode.Impulse);
            }
        }

        public void DisableHeadPhysics()
        {
            if (headRigidbody == null)
            {
                return;
            }

            foreach (Collider headCollider in headRigidbody.GetComponents<Collider>())
            {
                headCollider.enabled = false;
            }
        }

        public void ResetRagdoll()
        {
            foreach (Rigidbody rigidbody in _ragdollRigidbodies)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }

            SetRagdollActive(false);

            for (int i = 0; i < _poseTransforms.Length; i++)
            {
                _poseTransforms[i].localPosition = _initialLocalPositions[i];
                _poseTransforms[i].localRotation = _initialLocalRotations[i];
            }
        }

        private void SetRagdollActive(bool value)
        {
            IsRagdollActive = value;

            foreach (Collider ragdollCollider in _ragdollColliders)
            {
                ragdollCollider.enabled = value;
            }

            foreach (Rigidbody rigidbody in _ragdollRigidbodies)
            {
                if (!value)
                {
                    rigidbody.linearVelocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                    rigidbody.useGravity = false;
                    rigidbody.detectCollisions = false;
                    rigidbody.isKinematic = true;
                    continue;
                }

                rigidbody.isKinematic = false;
                rigidbody.detectCollisions = true;
                rigidbody.useGravity = true;

                if (settlingSpeed > 0f && rigidbody.linearVelocity.y > -settlingSpeed)
                {
                    Vector3 velocity = rigidbody.linearVelocity;
                    velocity.y = -settlingSpeed;
                    rigidbody.linearVelocity = velocity;
                }

                rigidbody.WakeUp();
            }
        }

        private Rigidbody GetClosestRigidbody(Vector3 point)
        {
            Rigidbody closest = null;
            float minSqrDistance = float.MaxValue;

            foreach (Rigidbody rigidbody in _ragdollRigidbodies)
            {
                float sqrDistance = Vector3.SqrMagnitude(rigidbody.position - point);
                if (sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    closest = rigidbody;
                }
            }

            return closest;
        }
    }
}
