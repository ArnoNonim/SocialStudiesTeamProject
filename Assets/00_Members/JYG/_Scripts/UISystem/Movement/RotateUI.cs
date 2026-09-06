using System;
using UnityEngine;

namespace _00_Members.JYG._Scripts.UISystem.Movement
{
    public class RotateUI : MonoBehaviour
    {
        public float spinSpeed = 0.5f;
        public float spinAngle = 60f;
        private void Update()
        {
            float zRotation = Mathf.Sin(Time.time * spinSpeed) * spinAngle;
            
            transform.localEulerAngles = new Vector3(0, 0, zRotation);
        }
    }
}
