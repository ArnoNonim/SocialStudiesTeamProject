using System.Collections.Generic;
using UnityEngine;

namespace _00_Members.JYG._Scripts.UISystem.Quest
{
    [CreateAssetMenu(fileName = "QuestDataList", menuName = "Quest/Quest data List")]
    public class QuestDataList : ScriptableObject
    {
        public List<QuestData> questDataList;
    }
}
