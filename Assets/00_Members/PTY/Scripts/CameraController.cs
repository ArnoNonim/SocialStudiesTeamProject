// CameraController.cs
using UnityEngine;
using UnityEngine.Serialization;

namespace _00_Members.PTY.Scripts
{
    public class CameraController : MonoBehaviour
    {
        [FormerlySerializedAs("_playerInput")]
        [Header("Input SO")]
        [SerializeField] private SO.PlayerInputSO playerInput;

        [FormerlySerializedAs("_moveSpeed")]
        [Header("이동 설정")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float moveLerpSpeed = 8f;      // 이동 러프 속도
        [SerializeField] private float moveInertiaDecay = 6f;   // 관성 감쇠 속도

        [Header("회전 설정")]
        [SerializeField] private float lookSensitivity = 0.15f;
        [SerializeField] private float lookLerpSpeed = 12f;     // 회전 러프 속도
        [SerializeField] private float lookInertiaDecay = 8f;   // 회전 관성 감쇠 속도
        [SerializeField] private float pitchClamp = 80f;        // 상하 각도 제한

        // 이동
        private Vector2 _rawMoveInput;
        private Vector3 _targetVelocity;
        private Vector3 _currentVelocity;

        // 회전
        private Vector2 _rawLookInput;
        private Vector2 _lookVelocity;      // 회전 관성
        private float _currentYaw;
        private float _currentPitch;
        private float _targetYaw;
        private float _targetPitch;

        private void OnEnable()
        {
            playerInput.OnMovement += HandleMovement;
            playerInput.OnLookAction += HandleLook;
        }

        private void OnDisable()
        {
            playerInput.OnMovement -= HandleMovement;
            playerInput.OnLookAction -= HandleLook;
        }

        private void Start()
        {
            _currentYaw = transform.eulerAngles.y;
            _currentPitch = transform.eulerAngles.x;
            _targetYaw = _currentYaw;
            _targetPitch = _currentPitch;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            UpdateMovement();
            UpdateRotation();
        }

        private void HandleMovement(Vector2 input)
        {
            _rawMoveInput = input;
        }

        private void HandleLook(Vector2 input)
        {
            _rawLookInput = input;
        }

        private void UpdateMovement()
        {
            // 입력 기반 목표 속도 계산
            Vector3 inputDir = new Vector3(_rawMoveInput.x, 0f, _rawMoveInput.y);
            Vector3 worldDir = transform.TransformDirection(inputDir);  // 카메라 방향 기준
            Vector3 desiredVelocity = worldDir * moveSpeed;

            // 관성: 입력 없으면 서서히 감쇠
            if (_rawMoveInput.sqrMagnitude > 0.01f)
                _targetVelocity = desiredVelocity;
            else
                _targetVelocity = Vector3.Lerp(_targetVelocity, Vector3.zero, moveInertiaDecay * Time.deltaTime);

            // 러프로 부드럽게 현재 속도에 적용
            _currentVelocity = Vector3.Lerp(_currentVelocity, _targetVelocity, moveLerpSpeed * Time.deltaTime);
            transform.position += _currentVelocity * Time.deltaTime;
        }

        private void UpdateRotation()
        {
            // 마우스 델타 -> 회전 관성에 누적
            _lookVelocity += _rawLookInput * lookSensitivity;

            // 관성 감쇠
            _lookVelocity = Vector2.Lerp(_lookVelocity, Vector2.zero, lookInertiaDecay * Time.deltaTime);

            // 목표 각도 누적
            _targetYaw += _lookVelocity.x;
            _targetPitch -= _lookVelocity.y;
            _targetPitch = Mathf.Clamp(_targetPitch, -pitchClamp, pitchClamp);

            // 러프로 현재 각도 추적
            _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, lookLerpSpeed * Time.deltaTime);
            _currentPitch = Mathf.LerpAngle(_currentPitch, _targetPitch, lookLerpSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
        }
    }
}