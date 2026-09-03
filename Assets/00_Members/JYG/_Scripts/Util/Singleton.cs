using System;
using UnityEngine;

namespace _00_Members.JYG._Scripts.Util
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if(Instance == null)
            {
                Instance = this as T;
            }
            else
            {
            	Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if(Instance == this)
            {
                Instance = null;
            }
        }
    }
}