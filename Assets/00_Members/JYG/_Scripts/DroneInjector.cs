using System;
using UnityEngine;

namespace _00_Members.JYG._Scripts
{
    public class DroneInjector : MonoBehaviour
    {
        private void Awake()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.drone = transform;
        }
    }
}
