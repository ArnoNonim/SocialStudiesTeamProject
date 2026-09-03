using System;
using System.Collections.Generic;
using UnityEngine;

namespace _00_Members.JYG._Scripts.UISystem.DialogSystem
{
    [CreateAssetMenu(fileName = "new Dialog data", menuName = "Dialog Data")]
    public class DialogData : ScriptableObject
    {
        public List<DialogText> contents = new List<DialogText>();
    }

    [Serializable]
    public class DialogText
    {
        public string speakerName = "익명";
        [TextArea] public string content;
    }
}
