using _00_Members.JYG._Scripts.UISystem.DialogSystem;
using _00_Members.JYG._Scripts.UISystem.Quest;
using UnityEngine;

namespace _00_Members.JYG._Scripts.Scene
{
    [CreateAssetMenu(fileName = "Scene data", menuName = "SceneData", order = 0)]
    public class SceneData : ScriptableObject
    {
        public string nextScene;   //다음 씬으로 이동하기 위한 씬 이름
        public QuestData[] quests; //Initialize에서 퀘스트 리스트에 쫙 넣어줄 퀘스트들 목록
        public DialogData dialogData;
    }
}