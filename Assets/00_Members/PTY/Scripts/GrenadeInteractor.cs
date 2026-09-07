using _00_Members.JYG._Scripts.UISystem.CameraEffect;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Members.PTY.Scripts
{
    public class GrenadeInteractor : MonoBehaviour
    {
        [SerializeField] private DropGrenade grenadePrefab;
        [SerializeField] private Transform grenadePoolFolder;
        [SerializeField] private GrenadeExplosionFX fxPrefab;
        [SerializeField] private Transform fxPoolFolder;
        [SerializeField] private Transform dropPos;
        [SerializeField] private AudioSource audioSource;

        public int grenadeAmount = 2;
        public bool isSuicideDrone;

        private bool _isExploded;   
        
        private void Awake()
        {
            DropGrenade.Initialize(grenadePrefab, grenadePoolFolder, fxPrefab, fxPoolFolder);
        }

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
            var g = DropGrenade.Spawn(pos, rot);
            grenadeAmount--;
        }
        
        private void SelfDestruct()
        {
            _isExploded = true;

            var g = DropGrenade.Spawn(transform.position, Quaternion.identity);
            g.Explode(transform.position);

            Debug.Log("자폭");

            CamNoiseEffect.Instance.OnBlack += HandleCameraCutOut;
            CamNoiseEffect.Instance.TurnOff();
            audioSource.Stop();
        }

        private void HandleCameraCutOut()
        {
            CamNoiseEffect.Instance.OnBlack -= HandleCameraCutOut; // 한 번 쓰고 구독 해제 (안 하면 다음 드론도 이 델리게이트에 계속 쌓임)
        }
    }
}