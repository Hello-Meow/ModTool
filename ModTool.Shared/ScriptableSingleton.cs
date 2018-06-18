using System;
using System.Reflection;
using UnityEngine;

namespace ModTool.Shared
{
    public abstract class ScriptableSingleton<T> : ScriptableObject where T : ScriptableSingleton<T>
    {
        public static T instance
        {
            get
            {
                if (_instance == null)
                    GetInstance();
                
                return _instance;
            }
        }

        private static T _instance;

        protected ScriptableSingleton()
        {
            if (_instance == null)
                _instance = this as T;
        }

        void OnEnable()
        {
            if (_instance == null)
                _instance = this as T;
        }

        protected static void GetInstance()
        {
            if (_instance == null)
                _instance = Resources.Load<T>(typeof(T).Name);

            if (_instance == null)
            {
                _instance = CreateInstance<T>();
                
                if (Application.isEditor)
                    CreateAsset();
            }
        }
        
        private static void CreateAsset()
        {
            Type type = Type.GetType("ModTool.Shared.Editor.AssetUtility, ModTool.Shared.Editor");
            MethodInfo method = type.GetMethod("CreateAsset", BindingFlags.Public | BindingFlags.Static);

            method.Invoke(null, new object[] { _instance });
        }
    }
}
