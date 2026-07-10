using System;
using _00_Members.JYG._Scripts.UISystem.Quest;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _00_Members.JYG._Scripts.UISystem.Box
{
    public class Textbox : MonoBehaviour
    {
        public TextMeshProUGUI textBox;
        public GameObject board;
        public QuestDataList list;
        private int idx = 0;

        private void Awake()
        {
            
            textBox.text = list.questDataList[idx].content;
        }

        public void NextText()
        {
            idx++;
            if (idx >= list.questDataList.Count)
            {
                DisableBox();
                return;
            }
            textBox.text = list.questDataList[idx].content;
        }

        private void DisableBox()
        {
            board.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                NextText();
            }
        }
    }
}
