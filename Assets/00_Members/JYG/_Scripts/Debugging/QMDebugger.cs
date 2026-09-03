using System;
using _00_Members.JYG._Scripts.UISystem.Quest;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Members.JYG._Scripts.Debugging
{
    public class QMDebugger : MonoBehaviour // QuestManager Debugger
    {
        [SerializeField] private QuestData qData;
        [SerializeField] private QuestData qData2;

        private void Awake()
        {
            QuestManager.Instance.RegistrationQuest(qData);
            QuestManager.Instance.RegistrationQuest(qData2);
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                QuestManager.Instance.PlusGoal(qData, 1);
            }
            if (Keyboard.current.vKey.wasPressedThisFrame)
            {
                QuestManager.Instance.PlusGoal(qData2, 1);
            }
        }
    }
}
