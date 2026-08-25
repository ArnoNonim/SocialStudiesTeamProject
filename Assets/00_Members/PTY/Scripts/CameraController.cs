using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Members.PTY.Scripts
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera dropCam;
        [SerializeField] private Transform dropPos;
        [SerializeField] private GameObject grenade;
        
        private Camera _mainCam;
        private bool _isChanging;

        private void Awake()
        {
            _mainCam = Camera.main;
            
            // 방어 코드: 인스펙터나 씬에서 카메라가 누락되었는지 확인
            if (_mainCam == null || dropCam == null)
            {
                Debug.LogError("카메라가 할당되지 않았습니다. 메인 카메라 태그나 인스펙터를 확인하세요.");
                return;
            }

            dropCam.enabled = false;
            _mainCam.enabled = true; // 시작 시 메인 카메라 확실히 켜기
        }
        
        private async void Update()
        {
            // 키보드 장치 연결 확인 및 입력 체크
            if (Keyboard.current != null && !_isChanging && Keyboard.current.vKey.wasPressedThisFrame)
            {
                await ChangeCam();
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                GameObject clone = Instantiate(grenade, dropPos);
                clone.SetActive(true);
            }
        }

        private async Task ChangeCam()
        {
            _isChanging = true;

            await Task.Delay(300);

            // 상태를 안전하게 반전시키는 올바른 로직
            // 현재 활성화 상태를 보관해두거나, 단순 토글 처리를 합니다.
            bool currentMainState = _mainCam.enabled;

            _mainCam.enabled = !currentMainState;
            dropCam.enabled = currentMainState; 

            _isChanging = false;
        }
    }
}