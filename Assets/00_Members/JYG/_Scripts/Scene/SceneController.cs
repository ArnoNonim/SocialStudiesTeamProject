using System;
using _00_Members.JYG._Scripts.UISystem.DialogSystem;
using _00_Members.JYG._Scripts.UISystem.Quest;
using TMPro;
using UnityEngine;

namespace _00_Members.JYG._Scripts.Scene
{
    public class SceneController : MonoBehaviour
    {
        public SceneData sceneData;
        public TextMeshProUGUI clearText;
        private bool _isActivated = false;
        public static string StageNumberKey = "StageNumber";

        private void Start()
        {
            foreach(QuestData data in sceneData.quests)
                QuestManager.Instance.RegistrationQuest(data);

            QuestManager.Instance.OnPlusGoal += HandleMoveToNextScene;
        }

        private void OnDestroy()
        {
            if(QuestManager.Instance != null)
                QuestManager.Instance.OnPlusGoal -= HandleMoveToNextScene;
        }

        private void HandleMoveToNextScene()
        {
            if (!_isActivated && QuestManager.Instance.IsCompleteAll())
            {
                _isActivated = true;
                PlayerPrefs.SetInt(StageNumberKey, PlayerPrefs.GetInt(StageNumberKey, 0) + 1);
                GameManager.Instance.ChangeScene(sceneData.nextScene);
                DialogContainer.CurrentDialogData = sceneData.dialogData;
                clearText.enabled = true;
            }
        }
    }
}
