using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Members.PTY.Scripts
{
    public class GrenadeDropper : MonoBehaviour
    {
        [SerializeField] private DropGrenade grenadePrefab;
        [SerializeField] private Transform grenadePoolFolder;
        [SerializeField] private GrenadeExplosionFX fxPrefab;
        [SerializeField] private Transform fxPoolFolder;
        [SerializeField] private Transform dropPos;

        public int grenadeAmount = 2;

        private void Awake()
        {
            DropGrenade.Initialize(grenadePrefab, grenadePoolFolder, fxPrefab, fxPoolFolder);
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame && grenadeAmount > 0)
            {
                Throw(dropPos.position, Quaternion.identity);
            }
        }
        
        public void Throw(Vector3 pos, Quaternion rot)
        {
            var g = DropGrenade.Spawn(pos, rot);
            grenadeAmount--;
        }
    }
}