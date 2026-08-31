using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace _00_Members.JYG._Scripts.Scene
{
    public class ScreenFadeEffector : MonoBehaviour, ISceneEffector
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 1f;
        private bool _isEnd = false;
        public void ExecuteEffect()
        {
            canvasGroup.DOFade(1, fadeDuration).SetEase(Ease.InBack)
                .OnComplete(() => _isEnd = true);
        }

        public bool IsEnd => _isEnd;
    }
}
