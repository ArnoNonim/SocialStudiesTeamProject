using UnityEngine;

namespace _00_Members.JYG._Scripts.UISystem.CameraEffect
{
    public class Effector : MonoBehaviour   //Effect들의 parent가 달고있는 스크립트
    {
        private void Start()
        {
            if(GameManager.Instance != null)
                GameManager.Instance.SetEffector(transform);
        }
    }
}
