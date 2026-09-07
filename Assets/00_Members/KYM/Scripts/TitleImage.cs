using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Members.KYM.Scripts
{
    public class TitleImage : MonoBehaviour
    {
        [SerializeField] private float duration;
        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private async void Start()
        {
            _image.color = new Color(1, 1, 1, 0);
            
            await Awaitable.WaitForSecondsAsync(1f);
            
            _image.DOFade(1, duration).SetEase(Ease.InCubic);
        }
    }
}
