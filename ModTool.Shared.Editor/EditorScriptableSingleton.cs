using System;
using UnityEngine;

namespace ModTool.Shared.Editor
{
    public abstract class EditorScriptableSingleton<T> : ScriptableObject where T : EditorScriptableSingleton<T>
    {
        //Note: Unity versions 5.6 and earlier fail to load ScriptableObject assets for Types that are defined in an editor assembly 
        //and derive from a Type defined in a non-editor assembly.

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

        protected EditorScriptableSingleton()
        {
            if (_instance == null)
                _instance = this as T;
        }

        void OnEnable()
        {
            if (_instance == null)
                _instance = this as T;
        }

        private static void GetInstance()
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
            AssetUtility.CreateAsset(_instance);
        }
    }
}
