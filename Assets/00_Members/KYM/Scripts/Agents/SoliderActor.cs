using _00_Members.KYM.Scripts.Agents.FSM;
using KimLIb.ModuleSystems;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Agents
{

    public class SoldierActor : ModuleOwner
    {
        [SerializeField] private bool isEnemy = true;
        [SerializeField] private CommandListSO commandList;
        private SoliderCommandMachine _commandMachine;

        protected override void Awake()
        {
            base.Awake();
            _commandMachine = new SoliderCommandMachine(this, commandList.commandList);
        }

        private void Update()
        {
            _commandMachine.UpdateMachine();
        }
        public void ChangeCommand(SoldierCommandEnum command)
            => _commandMachine.ChangeState((int)command);
    }
}