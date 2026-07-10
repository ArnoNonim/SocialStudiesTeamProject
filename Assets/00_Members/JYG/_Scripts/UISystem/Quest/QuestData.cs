using UnityEngine;

namespace _00_Members.JYG._Scripts.UISystem.Quest
{
    [CreateAssetMenu(fileName = "QuestData", menuName = "Quest/Quest data")]
    public class QuestData : ScriptableObject
    {
        public string content;
        public int goalCount;
    }
}
