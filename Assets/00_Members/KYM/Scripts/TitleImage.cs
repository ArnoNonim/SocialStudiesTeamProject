using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _00_Members.KYM.Scripts
{
    public class TitleImage : MonoBehaviour
    {
        [Header("로고")]
        [SerializeField, Min(0f)] private float waitDelay;

        [Header("크레딧")]
        [SerializeField] private TMP_Text creditText;
        [SerializeField, TextArea] private string[] creditMessages;
        [SerializeField, Min(0f)] private float creditStartDelay = 0.8f;
        [SerializeField, Min(0f)] private float creditFadeDuration = 0.5f;
        [SerializeField, Min(0f)] private float creditDisplayDuration = 1.5f;

        [Header("다음 씬")]
        [SerializeField] private string titleSceneString;

        private Image _image;
        private Sequence _creditSequence;

        private void Awake()
        {
            _image = GetComponent<Image>();
            if (creditText != null)
            {
                creditText.alpha = 0f;
            }
        }

        private async void Start()
        {
            _image.color = new Color(1, 1, 1, 0);

            await Awaitable.WaitForSecondsAsync(waitDelay);

            _image.color = new Color(1, 1, 1, 1);

            await Awaitable.WaitForSecondsAsync(waitDelay);
            _image.color = new Color(1, 1, 1, 0);
            PlayCredits();
        }

        private void PlayCredits()
        {
            
            if (creditText == null || creditMessages == null || creditMessages.Length == 0)
            {
                LoadNextScene();
                return;
            }

            _creditSequence?.Kill();
            _creditSequence = DOTween.Sequence();
            _creditSequence.AppendInterval(creditStartDelay);

            foreach (string message in creditMessages)
            {
                string currentMessage = message;
                _creditSequence.AppendCallback(() => creditText.text = currentMessage);
                _creditSequence.Append(creditText.DOFade(1f, creditFadeDuration));
                _creditSequence.AppendInterval(creditDisplayDuration);
                _creditSequence.Append(creditText.DOFade(0f, creditFadeDuration));
            }

            _creditSequence.OnComplete(LoadNextScene);
        }

        private void LoadNextScene()
        {
            if (!string.IsNullOrWhiteSpace(titleSceneString))
            {
                SceneManager.LoadScene(titleSceneString);
            }
        }

        private void OnDestroy()
        {
            _creditSequence?.Kill();
        }
    }
}
