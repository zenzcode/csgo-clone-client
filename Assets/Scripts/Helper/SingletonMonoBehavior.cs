using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Helper
{
    public class SingletonMonoBehavior<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = this as T;
            }
            else
            {
                Debug.LogWarning($"Found Duplicate of singleton {name}");
                Destroy(gameObject);
            }
        }
    }
}