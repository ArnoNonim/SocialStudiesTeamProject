using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Members.JYG._Scripts.UISystem.Quest
{
    public class QuestBlock : MonoBehaviour, IQuestBlock
    {
        public TextMeshProUGUI questText;
        private int goal = 0;
        private int maxGoal;
        private string mainText;
        public void InitializeText(string text, int questGoal)
        {
            questText.text = text;
            mainText = text;
            maxGoal = questGoal;
            if (questGoal <= 1) return;
            questText.text += $"\n ({goal}/{questGoal})";
        }

        public void GoalPlus(int count)
        {
            goal += count;
            if (goal >= maxGoal)
            {
                questText.text = mainText; 
                questText.color = Color.green;
            }
            else
                questText.text = $"{mainText}\n ({goal}/{maxGoal})";
        }

        private void Update()
        {
            if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                GoalPlus(1);
            }
        }
    }
}
