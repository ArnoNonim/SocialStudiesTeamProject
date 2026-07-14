using System;
using KimLIb.AnimatorSystems;
using UnityEngine;

namespace _00_Members.KYM.Scripts.Agents.FSM
{
    public enum SoldierAnimKey
    {
        Default,
        Variant01,
        Variant02,
        Wounded,
        Panic,
        Crying
    }

    [Serializable]
    public class SoldierAnimationData
    {
        public SoldierAnimKey key;
        public AnimParamSO animationState;
    }
    
    [CreateAssetMenu(fileName = "Command so", menuName = "KimSO/FSM/Command so", order = 0)]
    public class CommandSO : ScriptableObject
    {
        public int commandIndex;
        public string commandName;
        public string className;
    }
}