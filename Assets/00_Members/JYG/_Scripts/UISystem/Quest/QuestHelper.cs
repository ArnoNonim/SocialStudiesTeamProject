using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Members.JYG._Scripts.UISystem.Quest
{
    public class QuestHelper : MonoBehaviour
    {
        [SerializeField] private QuestData vKeyQuestData;
        [SerializeField] private QuestData spaceKeyQuestData;
        private void Update()
        {
            if (Keyboard.current.vKey.wasPressedThisFrame)
            {
                QuestManager.Instance.PlusGoal(vKeyQuestData, 1);
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                QuestManager.Instance.PlusGoal(spaceKeyQuestData, 1);
                
            }
        }
    }
}
