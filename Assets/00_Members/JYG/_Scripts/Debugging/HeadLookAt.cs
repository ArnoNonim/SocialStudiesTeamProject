using UnityEngine;
using UnityEngine.InputSystem;

// 신규 Input System 네임스페이스 추가

namespace _00_Members.JYG._Scripts.Debugging
{
    public class HeadLookAt : MonoBehaviour
    {
        [Header("Sensitivity Settings")]
        public float mouseSensitivity = 15f; // New Input System은 감도 값을 10~20 정도로 낮게 설정하세요.

        [Header("Angle Limitations")]
        public float minVerticalAngle = -60f;
        public float maxVerticalAngle = 60f;
        public float minHorizontalAngle = -70f;
        public float maxHorizontalAngle = 70f;

        private float xRotation = 0f;
        private float yRotation = 0f;

        void Start()
        {
            // 마우스 커서 중앙 고정 및 숨김
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            // 1. Mouse.current 존재 여부 확인
            if (Mouse.current == null) return;

            // 2. New Input System 방식으로 마우스 Delta(델타) 값 가져오기
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
            float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

            // 3. 각도 계산 및 제한 (Clamp)
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

            yRotation += mouseX;
            yRotation = Mathf.Clamp(yRotation, minHorizontalAngle, maxHorizontalAngle);

            // 4. 머리 회전 적용
            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
    }
}