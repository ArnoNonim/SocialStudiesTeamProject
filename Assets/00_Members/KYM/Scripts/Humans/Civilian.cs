using _00_Members.KYM.Scripts.Soldiers;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Humans
{
    public class Civilian : AbstractHuman
    {
        private RagdollController _ragdoll;
        private Animator _animator;
        private Collider[] _rootColliders;
        private bool[] _initialColliderStates;
        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;

        protected override void Awake()
        {
            base.Awake();
            _ragdoll = GetModule<RagdollController>();
            _animator = GetComponentInChildren<Animator>(true);
            _rootColliders = GetComponents<Collider>();
            _initialColliderStates = new bool[_rootColliders.Length];
            for (int i = 0; i < _rootColliders.Length; i++)
            {
                _initialColliderStates[i] = _rootColliders[i].enabled;
            }

            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
        }

        protected override void HandleFatalDamage(HumanDamage damage)
        {
            foreach (Collider rootCollider in _rootColliders)
            {
                rootCollider.enabled = false;
            }

            if (_ragdoll != null)
            {
                _ragdoll.EnableRagdoll(damage.HitPoint, damage.Direction, damage.Force);
            }
            else if (_animator != null)
            {
                _animator.enabled = false;
            }
        }

        public override void Revive()
        {
            transform.SetLocalPositionAndRotation(_initialLocalPosition, _initialLocalRotation);
            _ragdoll?.ResetRagdoll();

            if (_animator != null)
            {
                _animator.enabled = true;
                _animator.Rebind();
                _animator.Update(0f);
            }

            for (int i = 0; i < _rootColliders.Length; i++)
            {
                _rootColliders[i].enabled = _initialColliderStates[i];
            }

            base.Revive();
        }
    }
}
