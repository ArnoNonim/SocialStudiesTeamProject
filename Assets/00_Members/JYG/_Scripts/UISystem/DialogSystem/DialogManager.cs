using System;
using UnityEngine;

namespace _00_Members.JYG._Scripts.UISystem.DialogSystem
{
    public class DialogManager : MonoBehaviour
    {
        [SerializeField] private DialogData dialogData; //SerializeField로 한 이유는, Dialog가 정상적으로 오지 않았을 때 기본 메시지를 출력하기 위함이다.

        private void Awake()
        {
            dialogData = DialogContainer.CurrentDialogData;
        }
    }
}
