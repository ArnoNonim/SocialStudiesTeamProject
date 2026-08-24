using KimLIb.ModuleSystems;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers
{
    public class RagdollController : MonoBehaviour, IModule
    {
        [SerializeField] private Transform ragdollRoot;
        [SerializeField] private Rigidbody headRigidbody;

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

            foreach (Rigidbody rigidbody in _ragdollRigidbodies)
            {
                rigidbody.isKinematic = !value;
            }

            foreach (Collider ragdollCollider in _ragdollColliders)
            {
                ragdollCollider.enabled = value;
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
