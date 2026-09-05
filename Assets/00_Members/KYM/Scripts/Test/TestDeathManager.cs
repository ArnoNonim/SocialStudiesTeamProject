using _00_Members.KYM.Scripts.Soldiers;
using _00_Members.KYM.Scripts.Soldiers.DeathEvent;
using _00_Members.KYM.Scripts.Soldiers.Explosions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Members.KYM.Scripts.Test
{
    public class TestDeathManager : MonoBehaviour
    {
        [SerializeField] private Soldier targetSoldier;
        [SerializeField] private DeathType deathType = DeathType.Ragdoll;
        [SerializeField] private Key deathKey = Key.K;
        [SerializeField] private Key resetKey = Key.R;

        [Header("Explosion Test")]
        [SerializeField] private Key explosionKey = Key.B;
        [SerializeField] private Vector3 localExplosionOffset = new Vector3(0f, 0f, -2f);
        [SerializeField] private ExplosionProfile explosionProfile = new ExplosionProfile();

        [Header("Bomb Drop Test")]
        [SerializeField] private Key bombDropKey = Key.V;
        [SerializeField] private GameObject bombPrefab;
        [SerializeField] private Vector3 localBombSpawnOffset = new Vector3(0f, 10f, -2f);

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (targetSoldier == null || keyboard == null)
            {
                return;
            }

            if (keyboard[resetKey].wasPressedThisFrame)
            {
                targetSoldier.Revive();
                return;
            }

            if (keyboard[deathKey].wasPressedThisFrame)
            {
                targetSoldier.Die(deathType);
                return;
            }

            if (keyboard[explosionKey].wasPressedThisFrame)
            {
                Vector3 explosionCenter = targetSoldier.transform.TransformPoint(localExplosionOffset);
                SoldierExplosionSystem.Detonate(explosionCenter, explosionProfile, gameObject);
                return;
            }

            if (keyboard[bombDropKey].wasPressedThisFrame && bombPrefab != null)
            {
                Vector3 spawnPosition = targetSoldier.transform.TransformPoint(localBombSpawnOffset);
                Instantiate(bombPrefab, spawnPosition, Quaternion.identity);
            }
        }
    }
}
