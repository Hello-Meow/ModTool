using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ModTool.Shared
{
    /// <summary>
    /// A singleton class for ScriptableObjecs, which stores itself as an asset in a Resources folder.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Singleton<T> : ScriptableObject where T : Singleton<T>
    {
        public static T instance
        {
            get
            {               
                GetInstance();                
                return _instance;
            }
        }

        private static T _instance;

        protected Singleton()
        {
            if (_instance == null)
                _instance = this as T;
        }
        
        protected static void GetInstance()
        {
            if (_instance != null)
                return;
            
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
            string modToolDirectory = Path.GetDirectoryName(typeof(ModInfo).Assembly.Location);
            string resourcesDirectory = Path.Combine(modToolDirectory, "Resources");

            resourcesDirectory = resourcesDirectory.Substring(Application.dataPath.Length - 6);

            if (Directory.Exists(resourcesDirectory))
                Directory.CreateDirectory(resourcesDirectory);

            string assetPath = Path.Combine(resourcesDirectory, typeof(T).Name + ".asset");

            Type type = Type.GetType("UnityEditor.AssetDatabase, UnityEditor");

            if (type == null)
                return;

            MethodInfo method = type.GetMethod("CreateAsset", BindingFlags.Public | BindingFlags.Static);

            method.Invoke(null, new object[] { _instance, assetPath });
        }
    }
}
