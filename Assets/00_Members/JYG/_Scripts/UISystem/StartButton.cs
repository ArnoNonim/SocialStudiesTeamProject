using System;
using _00_Members.JYG._Scripts.Scene;
using _00_Members.JYG._Scripts.UISystem.DialogSystem;
using UnityEngine;

namespace _00_Members.JYG._Scripts.UISystem
{
    public class StartButton : MonoBehaviour
    {
        [SerializeField] private DialogData firstDialog;
        [SerializeField] private SceneData sceneData;

        private void Awake()
        {
            DialogContainer.CurrentDialogData = firstDialog;
        }

        public void OnStart()
        {
            PlayerPrefs.SetInt(SceneController.StageNumberKey, 1);
            GameManager.Instance.ChangeScene("Dialog");
        }
    }
}
