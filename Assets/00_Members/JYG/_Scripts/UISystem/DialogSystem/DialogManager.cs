using System;
using System.Collections;
using _00_Members.JYG._Scripts.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace _00_Members.JYG._Scripts.UISystem.DialogSystem
{
    public class DialogManager : MonoBehaviour
    {
        [Header("UI Component")]
        [SerializeField] private TextMeshProUGUI speakerText;
        [SerializeField] private TextMeshProUGUI contentText;

        [Header("Dialog Asset")]
        [SerializeField] private DialogData defaultDialogData; // 기본(Fallback) 대화 데이터

        [Header("Typewriter Settings")]
        [SerializeField] private float typingSpeed = 0.05f; // 글자당 출력 속도(초)

        private DialogData _activeData;
        private int _currentIndex = 0;
        private Coroutine _typingCoroutine;
        private bool _isTyping = false;

        private void Awake()
        {
            _activeData = DialogContainer.CurrentDialogData ??= defaultDialogData;
        }

        private void Start()
        {
            if (_activeData != null && _activeData.contents.Count > 0)
            {
                ShowDialog(_currentIndex);
            }
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                OnClickNext();
            }
        }

        /// <summary>
        /// 지정된 인덱스의 대화를 출력합니다.
        /// </summary>
        public void ShowDialog(int index)
        {
            if (index < 0 || index >= _activeData.contents.Count) return;

            _currentIndex = index;
            DialogText dialog = _activeData.contents[_currentIndex];

            if (speakerText != null)
            {
                speakerText.text = dialog.speakerName;
            }

            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
            }

            _typingCoroutine = StartCoroutine(Co_TypeText(dialog.content));
        }

        /// <summary>
        /// 화면 클릭 등 사용자 입력 시 호출하는 함수
        /// </summary>
        public void OnClickNext()
        {
            // 1. 글자가 출력 중인 경우 -> 타이핑을 즉시 완료하고 전체 문장 출력
            if (_isTyping)
            {
                CompleteTyping();
                return;
            }

            // 2. 출력이 끝난 상태인 경우 -> 다음 대화로 이동
            _currentIndex++;
            if (_currentIndex < _activeData.contents.Count)
            {
                ShowDialog(_currentIndex);
            }
            else
            {
                OnDialogEnd();
            }
        }

        private IEnumerator Co_TypeText(string fullText)
        {
            _isTyping = true;
            contentText.text = "";

            // string의 CharArray 순회 출력
            foreach (char letter in fullText.ToCharArray())
            {
                contentText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }

            _isTyping = false;
            _typingCoroutine = null;
        }

        private void CompleteTyping()
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }

            contentText.text = _activeData.contents[_currentIndex].content;
            _isTyping = false;
        }

        private void OnDialogEnd()
        {
            // 대화가 모두 완료되었을 때 실행할 로직 (UI 비활성화 등)
            gameObject.SetActive(false);
            SceneManager.LoadScene("Stage0" + PlayerPrefs.GetInt(SceneController.StageNumberKey));
        }

        [ContextMenu("Remove Data")]
        public void ResetLevelData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetInt(SceneController.StageNumberKey, 1);
        }
    }
}