using KimLIb.AnimatorSystems;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Soldiers.Fleeing
{
    internal interface IFleeAnimationPlayer
    {
        int Play(AnimParamSO animationState);
        bool IsFinished(int stateHash, float elapsed, float fallbackDuration);
    }

    internal sealed class FleeAnimationPlayer : IFleeAnimationPlayer
    {
        private readonly Animator _animator;
        private readonly float _transitionDuration;

        public FleeAnimationPlayer(Animator animator, float transitionDuration)
        {
            _animator = animator;
            _transitionDuration = transitionDuration;
        }

        public int Play(AnimParamSO animationState)
        {
            if (_animator == null || animationState == null ||
                string.IsNullOrWhiteSpace(animationState.ParamName))
            {
                return 0;
            }

            int stateHash = animationState.ParamHash;
            if (!_animator.HasState(0, stateHash))
            {
                string layerName = _animator.GetLayerName(0);
                stateHash = Animator.StringToHash($"{layerName}.{animationState.ParamName}");
                if (!_animator.HasState(0, stateHash))
                {
                    return 0;
                }
            }

            _animator.CrossFadeInFixedTime(stateHash, _transitionDuration);
            return stateHash;
        }

        public bool IsFinished(int stateHash, float elapsed, float fallbackDuration)
        {
            if (_animator != null && stateHash != 0)
            {
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                if (!_animator.IsInTransition(0) &&
                    stateInfo.fullPathHash == stateHash &&
                    stateInfo.normalizedTime >= 0.95f)
                {
                    return true;
                }
            }

            return elapsed >= fallbackDuration;
        }
    }
}
