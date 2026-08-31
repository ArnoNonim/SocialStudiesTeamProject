using System;
using _00_Members.JYG._Scripts.UISystem.Quest;
using TMPro;
using UnityEngine;

namespace _00_Members.JYG._Scripts.Scene
{
    public class SceneController : MonoBehaviour
    {
        public SceneData sceneData;
        public TextMeshProUGUI clearText;

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
            if (QuestManager.Instance.IsCompleteAll())
            {
                GameManager.Instance.ChangeScene(sceneData.nextScene);
                clearText.enabled = true;
            }
        }
    }
}
