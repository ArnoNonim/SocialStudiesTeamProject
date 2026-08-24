using System;
using _00_Members.JYG._Scripts.UISystem.Quest;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Members.JYG._Scripts.Debugging
{
    public class QMDebugger : MonoBehaviour // QuestManager Debugger
    {
        [SerializeField] private QuestData qData;
        private void Update()
        {
            if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                QuestManager.Instance.RegistrationQuest(qData);
            }

            if (Keyboard.current.lKey.wasPressedThisFrame)
            {
                QuestManager.Instance.PlusGoal(qData, 1);
            }
        }
    }
}
