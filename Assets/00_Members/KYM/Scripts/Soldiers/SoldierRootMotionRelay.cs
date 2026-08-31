using _00_Members.KYM.Scripts.Soldiers.Movement;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers
{
    [RequireComponent(typeof(Animator))]
    public sealed class SoldierRootMotionRelay : MonoBehaviour, IModule
    {
        [SerializeField] private bool applyPosition = true;
        [Tooltip("Keep disabled while navigation owns the actor's facing direction.")]
        [SerializeField] private bool applyRotation;

        private Animator _animator;
        private Transform _ownerRoot;
        private ISoldierNavigation _navigation;

        public bool IsApplying { get; private set; }
        public Vector3 LastDeltaPosition { get; private set; }
        public Quaternion LastDeltaRotation { get; private set; } = Quaternion.identity;

        public void Initialize(ModuleOwner owner)
        {
            _animator = GetComponent<Animator>();
            _ownerRoot = owner.transform;
            _navigation = owner.GetModule<ISoldierNavigation>();
            IsApplying = false;
        }

        public void SetApplying(bool value)
        {
            IsApplying = value;
            LastDeltaPosition = Vector3.zero;
            LastDeltaRotation = Quaternion.identity;
        }

        private void OnAnimatorMove()
        {
            if (_animator == null || _ownerRoot == null || !IsApplying)
            {
                return;
            }

            LastDeltaPosition = _animator.deltaPosition;
            LastDeltaRotation = _animator.deltaRotation;

            Vector3 positionDelta = applyPosition ? LastDeltaPosition : Vector3.zero;
            Quaternion rotationDelta = applyRotation ? LastDeltaRotation : Quaternion.identity;
            if (_navigation != null && _navigation.TryApplyRootMotion(positionDelta, rotationDelta))
            {
                return;
            }

            // Keep the relay usable on actors without a navigation module.
            if (applyPosition)
            {
                _ownerRoot.position += LastDeltaPosition;
            }

            if (applyRotation)
            {
                _ownerRoot.rotation *= LastDeltaRotation;
            }
        }

        private void OnDisable()
        {
            SetApplying(false);
        }
    }
}
