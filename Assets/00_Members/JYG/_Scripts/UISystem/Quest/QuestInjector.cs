using System;
using UnityEngine;

namespace _00_Members.JYG._Scripts.UISystem.Quest
{
    public class QuestInjector : MonoBehaviour
    {
        public QuestDataList questDataList;

        private void Awake()
        {
            int idx = 0;
            foreach (IQuestBlock questBlock in GetComponentsInChildren<IQuestBlock>())
            {
                QuestData data = questDataList.questDataList[idx];
                questBlock.InitializeText(data.content, data.goalCount);
                idx++;
            }
        }
    }
}
