using KimLIb.ModuleSystems;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Agents
{
    public sealed class SoldierRenderer : MonoBehaviour, IModule
    {
        private SoldierMover _mover;
        public Animator Animator { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            _mover = owner.GetModule<SoldierMover>();
            Animator = GetComponent<Animator>();
            Animator.applyRootMotion = true;
        }

        private void OnAnimatorMove()
        {
            if (_mover == null || Animator == null)
                return;

            _mover.ApplyRootMotion(Animator.deltaPosition);
        }
        
        public void PlayClip(int clipHash, float crossFadeDuration = 0.2f, int layerIndex = 0, float normalizedTime = 0)
        {
            Animator.CrossFadeInFixedTime(clipHash, crossFadeDuration, layerIndex, normalizedTime);
        }
        
    }
}
