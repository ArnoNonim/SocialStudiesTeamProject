using _00_Members.KYM.Scripts.Soldiers;
using _00_Members.KYM.Scripts.Soldiers.DeathEvent;
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
            }
        }
    }
}
