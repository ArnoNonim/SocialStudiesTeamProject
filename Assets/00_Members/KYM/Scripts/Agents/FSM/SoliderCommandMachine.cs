using System;
using System.Collections.Generic;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Agents.FSM
{
    public class SoliderCommandMachine
    {
        public AbstractSoldierCommand CurrentState { get; private set; }
        private Dictionary<int, AbstractSoldierCommand> _stateDict;

        public SoliderCommandMachine(SoldierActor agent, CommandSO[] stateList)
        {
            _stateDict = new Dictionary<int, AbstractSoldierCommand>();
            foreach (CommandSO stateData in stateList)
            {
                Type type = Type.GetType(stateData.className);
                Debug.Assert(type != null, $"찾고자 하는 타입이 없습니다. : {stateData.className}");
                AbstractSoldierCommand state = (AbstractSoldierCommand)Activator.CreateInstance(type, agent);
                _stateDict.Add(stateData.commandIndex, state);
            }
        }

        public void ChangeState(int newStateIndex)
        {
            CurrentState?.Exit();
            AbstractSoldierCommand newState = _stateDict.GetValueOrDefault(newStateIndex);
            Debug.Assert(newState != null, $"new State is null {newStateIndex}");
            
            CurrentState = newState;
            CurrentState.Enter();
        }
        
        public void UpdateMachine() => CurrentState?.Update();

        public AbstractSoldierCommand GetCurrentState() => CurrentState;
        public AbstractSoldierCommand GetState(int stateIndex)
        {
            return _stateDict.GetValueOrDefault(stateIndex);
        }
    }
}