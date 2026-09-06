using System;
using UnityEngine;

namespace _00_Members.JYG._Scripts.UISystem
{
    public class ScannerUI : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private GameObject scannerSquare;
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private float distance;
        private Transform _drone;
        
        private void Start()
        {
            _drone = GameManager.Instance.drone;
            if (_drone == null)
            {
                
                Debug.LogWarning("Drone이 GameManager에 정의되지 않았습니다. 비활성화 처리합니다. : " + gameObject.name);
                enabled = false;
            }
            else
                transform.parent = null;
        }
/*          //원래 벽이나 사물에 막히면 안나오게 하려고 했지만, 맵 에셋이 너무 복잡한 관계로 해당 기능은 없애기로 결정
        private void FixedUpdate()
        {
            CheckDroneRaycast();
        }
*/
        private void CheckDroneRaycast()
        {
            Ray ray = new Ray(transform.position, _drone.transform.position - transform.position);
            if (Physics.Raycast(ray, out RaycastHit hit, distance, targetMask))
            {
                if (hit.transform == _drone)
                {
                    if (!scannerSquare.activeSelf)
                    {
                        scannerSquare.SetActive(true);
                    }
                    return;
                }
            }
            scannerSquare.SetActive(false);
        }

        private void LateUpdate()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;
            transform.position = followTarget.position;
            transform.up = mainCam.transform.up;
            transform.forward = mainCam.transform.forward;
        }
    }
}
