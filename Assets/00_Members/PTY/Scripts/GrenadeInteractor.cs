using _00_Members.JYG._Scripts.UISystem.CameraEffect;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Members.PTY.Scripts
{
    public class GrenadeInteractor : MonoBehaviour
    {
        // grenadePrefab/grenadePoolFolder/fxPrefab/fxPoolFolder는 GrenadePoolManager로 옮겨졌으므로
        // 여기서는 더 이상 들고 있지 않음. 씬에 GrenadePoolManager를 배치하고 거기서 할당할 것.
        [SerializeField] private Transform dropPos;
        [SerializeField] private AudioSource audioSource;

        public int grenadeAmount = 2;
        public bool isSuicideDrone;

        private bool _isExploded;

        // 기존 Awake()의 DropGrenade.Initialize(...) 호출은 삭제함.
        // 풀 초기화는 씬에 배치된 GrenadePoolManager.Awake()가 알아서 처리함.

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (grenadeAmount > 0 && !isSuicideDrone)
                    Throw(dropPos.position, Quaternion.identity);
                else if (isSuicideDrone && !_isExploded)
                    SelfDestruct();
            }
        }

        public void Throw(Vector3 pos, Quaternion rot)
        {
            GrenadePoolManager.Instance.SpawnGrenade(pos, rot);
            grenadeAmount--;
        }

        private void SelfDestruct()
        {
            _isExploded = true;

            var g = GrenadePoolManager.Instance.SpawnGrenade(transform.position, Quaternion.identity);
            g.Explode(transform.position); // 아밍 딜레이 무시하고 즉시 폭발

            Debug.Log("자폭");

            CamNoiseEffect.Instance.OnBlack += HandleCameraCutOut;
            CamNoiseEffect.Instance.TurnOff();
            audioSource.Stop();
        }

        private void HandleCameraCutOut()
        {
            CamNoiseEffect.Instance.OnBlack -= HandleCameraCutOut; // 한 번 쓰고 구독 해제
        }
    }
}