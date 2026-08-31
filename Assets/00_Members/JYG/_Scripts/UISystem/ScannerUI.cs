using System;
using UnityEngine;

namespace _00_Members.JYG._Scripts.UISystem
{
    public class ScannerUI : MonoBehaviour
    {
        private Camera _mainCam;
        [SerializeField] private GameObject scannerSquare;
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private float distance;

        private void Awake()
        {
            _mainCam = Camera.main;
            
            if(_mainCam == null)
                Debug.LogWarning("Billboard가 MainCamera를 들고오지 못했습니다. : " + gameObject.name);
        }

        private void FixedUpdate()
        {
            CheckDroneRaycast();
        }

        private void CheckDroneRaycast()
        {
            Ray ray = new Ray(transform.position, GameManager.Instance.drone.transform.position - transform.position);
            if (Physics.Raycast(ray, out RaycastHit hit, distance, targetMask))
            {
                if (hit.transform == GameManager.Instance.drone)
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
            transform.up = _mainCam.transform.up;
            transform.forward = _mainCam.transform.forward;
        }
    }
}
