using System.Collections;
using UnityEngine;

namespace _00_Members.PTY.Scripts
{
    public class ExplodeLight : MonoBehaviour
    {
        [SerializeField] private float stayTime = 0.02f;
        [SerializeField] private int lightAmount = 20;

        private Light _lgt;
        
        private void Awake()
        {
            _lgt = GetComponent<Light>();
        }

        public void Play()
        {
            StartCoroutine(Disappear());
        }

        private IEnumerator Disappear()
        {
            _lgt.intensity = lightAmount;
            yield return new WaitForSeconds(stayTime);
            _lgt.intensity = 0;
        }
    }
}
