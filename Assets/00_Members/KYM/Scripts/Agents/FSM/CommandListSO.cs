using UnityEngine;

namespace _00_Members.KYM.Scripts.Agents.FSM
{
    [CreateAssetMenu(fileName = "Command so list", menuName = "KimSO/FSM/Command list", order = 1)]
    public class CommandListSO : ScriptableObject
    {
        public string commandEnumName;
        public CommandSO[] commandList;
    }
}