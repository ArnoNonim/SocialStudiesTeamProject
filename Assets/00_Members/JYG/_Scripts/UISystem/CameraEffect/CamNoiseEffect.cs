using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _00_Members.JYG._Scripts.UISystem.CameraEffect
{
    public class CamNoiseEffect : MonoBehaviour
    {
        public Material noiseMat;

        [Header("Transition Settings")]
        [SerializeField] private float duration = 0.5f;     // 올라가고 내려가는 시간
        [SerializeField] private float waitTime = 0.2f;     // 최고치에서 대기하는 시간
        [SerializeField] private float minTransition = 0.2f; // 최소 노이즈 값
        [SerializeField] private float maxTransition = 1.0f; // 최대 노이즈 값

        private static readonly int TransitionID = Shader.PropertyToID("_Transition");

        private RawImage _targetImage;
        private Material _instancedMaterial; // 개별 제어용 머티리얼 인스턴스
        private Coroutine _noiseCoroutine;

        public Action OnBlack;

        private void Awake()
        {
            _targetImage = GetComponent<RawImage>();

            // 원본 머티리얼이 설정되어 있다면 복사본(Instance)을 만들어 UI에 할당
            if (noiseMat != null)
            {
                _instancedMaterial = new Material(noiseMat);
                _targetImage.material = _instancedMaterial;
            }
            else if (_targetImage.material != null)
            {
                _instancedMaterial = _targetImage.material;
            }

            SetTransitionValue(minTransition);
        }

        [ContextMenu("DO Noise")]
        public void DoNoise()
        {
            if (_noiseCoroutine != null)
            {
                StopCoroutine(_noiseCoroutine);
            }

            _noiseCoroutine = StartCoroutine(Co_DoNoise());
        }

        private IEnumerator Co_DoNoise()
        {
            // 1. minTransition -> maxTransition (duration 동안)
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float currentVal = Mathf.Lerp(minTransition, maxTransition, elapsedTime / duration);
                SetTransitionValue(currentVal);
                yield return null;
            }
            SetTransitionValue(maxTransition);

            OnBlack?.Invoke();
            // 2. 대기
            yield return new WaitForSeconds(waitTime);

            // 3. maxTransition -> minTransition (duration 동안)
            elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float currentVal = Mathf.Lerp(maxTransition, minTransition, elapsedTime / duration);
                SetTransitionValue(currentVal);
                yield return null;
            }
            SetTransitionValue(minTransition);

            _noiseCoroutine = null;
        }

        private void SetTransitionValue(float value)
        {
            if (_instancedMaterial != null)
            {
                _instancedMaterial.SetFloat(TransitionID, value);
            }
        }

        private void OnDestroy()
        {
            // 동적 생성된 Material 인스턴스 메모리 해제
            if (_instancedMaterial != null)
            {
                Destroy(_instancedMaterial);
            }
        }
    }
}