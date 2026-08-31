using System;
using System.Collections.Generic;
using _00_Members.JYG._Scripts.Util;
using Unity.Mathematics;
using UnityEngine;

namespace _00_Members.JYG._Scripts.UISystem.Quest
{
    public class QuestManager : Singleton<QuestManager>
    {
        private readonly Dictionary<QuestData, QuestBlock> _questBlocks = new Dictionary<QuestData, QuestBlock>();
        
        [SerializeField] private GameObject questField;
        [SerializeField] private GameObject questBlock;

        public event Action OnPlusGoal;
        public void RegistrationQuest(QuestData questData)
        {
            if (questField != null && questBlock != null)
            {
                if (_questBlocks.ContainsKey(questData))
                {
                    Debug.LogError("이미 등록된 퀘스트입니다. 자동으로 거부합니다."); //담당 조윤규. 수정 필요 시 갠디
                    return;
                }
                QuestBlock block =
                    Instantiate(questBlock, Vector3.zero, quaternion.identity, questField.transform)
                        .GetComponent<QuestBlock>();
                block.transform.localPosition = Vector3.zero;
                block.transform.localRotation = Quaternion.identity;
                block.InitializeText(questData.content, questData.goalCount);
                
                _questBlocks.Add(questData, block);
            }
        }

        public void PlusGoal(QuestData questData, int goalCount)
        {
            QuestBlock block = GetQuestBlock(questData);
            if (block == null) return;
            
            block.GoalPlus(goalCount);
            OnPlusGoal?.Invoke();
        }

        private QuestBlock GetQuestBlock(QuestData questData)
        {
            if (!_questBlocks.TryGetValue(questData, out QuestBlock block))
            {
                Debug.Log($"QuestData : {questData} is not found. -QuestManager- Please RegistrationQuest");
                return null;
            }
            
            return block;
        }

        public bool IsCompleteAll()
        {
            foreach(QuestBlock block in _questBlocks.Values)
                if (!block.IsComplete())
                    return false;
            return true;
        }
    }
}
