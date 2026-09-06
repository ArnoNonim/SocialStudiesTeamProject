using _00_Members.KYM.Scripts.Humans;
using _00_Members.KYM.Scripts.Soldiers.Explosions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace _00_Members.KYM.Scripts.Test
{
    public class TestDeathManager : MonoBehaviour
    {
        [FormerlySerializedAs("targetSoldier")]
        [SerializeField] private AbstractHuman targetHuman;
        [SerializeField, Min(0f)] private float deathDistance;
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
            if (targetHuman == null || keyboard == null)
            {
                return;
            }

            if (keyboard[resetKey].wasPressedThisFrame)
            {
                targetHuman.Revive();
                return;
            }

            if (keyboard[deathKey].wasPressedThisFrame)
            {
                targetHuman.Die(deathDistance);
                return;
            }

            if (keyboard[explosionKey].wasPressedThisFrame)
            {
                Vector3 explosionCenter = targetHuman.transform.TransformPoint(localExplosionOffset);
                SoldierExplosionSystem.Detonate(explosionCenter, explosionProfile, gameObject);
                return;
            }

            if (keyboard[bombDropKey].wasPressedThisFrame && bombPrefab != null)
            {
                Vector3 spawnPosition = targetHuman.transform.TransformPoint(localBombSpawnOffset);
                Instantiate(bombPrefab, spawnPosition, Quaternion.identity);
            }
        }
    }
}
