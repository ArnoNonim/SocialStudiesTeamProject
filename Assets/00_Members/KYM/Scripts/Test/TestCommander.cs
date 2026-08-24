using _00_Members.KYM.Scripts.Agents;
using _00_Members.KYM.Scripts.Agents.FSM;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Test
{
    public class TestCommander : MonoBehaviour
    {
        [SerializeField] private SoldierActor soldierActor;
        [SerializeField] private SoldierCommandEnum currentCommand;

        [ContextMenu("인보크 커멘드")]
        public void InvokeCommand()
        {
            soldierActor.ChangeCommand(currentCommand);
        }
    }
}
