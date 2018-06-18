using System;
using UnityEngine;

namespace ModTool
{
    /// <summary>
    /// A generic singleton class for MonoBehaviours
    /// </summary>
    /// <typeparam name="T">The singleton's Type.</typeparam>
    public class UnitySingleton<T> : MonoBehaviour where T : Component
    {
        private static T _instance;

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static T instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject();
                        obj.name = typeof(T).Name;
                        _instance = obj.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(this.gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if(_instance == this)
                _instance = null;
        }
    }
}
