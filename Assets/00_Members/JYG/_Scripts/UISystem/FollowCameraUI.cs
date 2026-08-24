using UnityEngine;

namespace _00_Members.JYG._Scripts.UISystem
{
    public class FollowCameraUI : MonoBehaviour
    {
        [Header("Target & Distance")]
        [Tooltip("비워두면 Main Camera를 자동으로 찾습니다.")]
        public Transform targetCamera;
        [Tooltip("카메라 전방으로 유지할 거리")]
        public float distanceFromCamera = 2.0f;

        [Header("Damping Settings")]
        [Tooltip("위치 이동 댐핑 시간 (작을수록 빠름)")]
        public float positionSmoothTime = 0.15f;
        [Tooltip("회전 댐핑 속도 (클스록 빠름)")]
        public float rotationSmoothSpeed = 10.0f;

        private Vector3 currentVelocity = Vector3.zero;

        void Start()
        {
            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }
        }

        void LateUpdate()
        {
            if (targetCamera == null) return;

            // 1. 카메라 회전을 반영한 목표 위치 계산 (Front Distance)
            Vector3 targetPosition = targetCamera.position + (targetCamera.forward * distanceFromCamera);

            // 2. 위치 Damping (SmoothDamp)
            transform.position = Vector3.SmoothDamp(
                transform.position, 
                targetPosition, 
                ref currentVelocity, 
                positionSmoothTime
            );

            // 3. 회전 Damping (Quaternion.Slerp)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetCamera.rotation, 
                Time.deltaTime * rotationSmoothSpeed
            );
        }
    }
}
