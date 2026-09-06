using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _00_Members.JYG._Scripts.Scene;
using _00_Members.JYG._Scripts.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _00_Members.JYG._Scripts
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : Singleton<GameManager>
    {
        private Coroutine _sceneLoadCoroutine;
        [SerializeField] private Transform effector;
        private readonly WaitForSeconds _waitForSeconds = new WaitForSeconds(0.2f);
        public Transform drone;
        
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this);
        }

        public void ChangeScene(string sceneName)
        {
            if (_sceneLoadCoroutine != null)
            {
                Debug.LogError("이미 Scene Changer가 활성화되었는데 다시 씬 변경을 시도했습니다.");
                return;
            }
            _sceneLoadCoroutine = StartCoroutine(SceneChange(sceneName));
        }
        
        public void SetEffector(Transform effector) => this.effector = effector;

        public IEnumerator SceneChange(string sceneName)
        {
            if (effector != null)
            {
                List<ISceneEffector> effectors = effector.GetComponentsInChildren<ISceneEffector>().ToList();
                if (effectors.Count != 0)
                {
                    foreach (ISceneEffector sceneEffector in effectors)
                    {
                        sceneEffector.ExecuteEffect();
                    }
                    while (effectors.Count > 0)
                    {
                        if (effectors[0].IsEnd)
                        {
                            effectors.RemoveAt(0);
                            continue;
                        }

                        yield return _waitForSeconds;
                    }
                }
            }
            _sceneLoadCoroutine = null;
            SceneManager.LoadScene(sceneName);
        }
    }
}
