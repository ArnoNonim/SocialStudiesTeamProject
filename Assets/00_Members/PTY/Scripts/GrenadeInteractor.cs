using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Members.PTY.Scripts
{
    public class GrenadeInteractor : MonoBehaviour
    {
        [SerializeField] private DropGrenade grenadePrefab;
        [SerializeField] private Transform grenadePoolFolder;
        [SerializeField] private GrenadeExplosionFX fxPrefab;
        [SerializeField] private Transform fxPoolFolder;
        [SerializeField] private Transform dropPos;

        public int grenadeAmount = 2;
        public bool isSuicideDrone;

        private bool _isExploded;   
        
        private void Awake()
        {
            DropGrenade.Initialize(grenadePrefab, grenadePoolFolder, fxPrefab, fxPoolFolder);
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if(grenadeAmount > 0 && !isSuicideDrone)
                    Throw(dropPos.position, Quaternion.identity);
                else if (isSuicideDrone && !_isExploded)
                {
                    Debug.Log("자폭");
                    _isExploded = true;
                }
            }
        }
        
        public void Throw(Vector3 pos, Quaternion rot)
        {
            var g = DropGrenade.Spawn(pos, rot);
            grenadeAmount--;
        }
    }
}