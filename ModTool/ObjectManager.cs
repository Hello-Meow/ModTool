using System.Collections.Generic;
using UnityEngine;

namespace ModTool
{
    /// <summary>
    /// The ObjectManager keeps track of the objects that are instantiated by Mods.
    /// </summary>
    public static class ObjectManager
    {
        private static Dictionary<string, HashSet<GameObject>> modObjects;

        static ObjectManager()
        { 
            modObjects = new Dictionary<string, HashSet<GameObject>>();
        }

        /// <summary>
        /// Instantiate an object and associate it with a mod.
        /// </summary>
        /// <param name="original">An existing object that you want to make a copy of.</param>
        /// <param name="modName">The name of the mod.</param>
        /// <returns>The instantiated copy.</returns>
        public static Object Instantiate(Object original, string modName)
        {
            Object obj = Object.Instantiate(original);

            RegisterObject(obj, modName);
            
            return obj;
        }

        /// <summary>
        /// Instantiate an object and associate it with a mod.
        /// </summary>
        /// <param name="original">An existing object that you want to make a copy of.</param>
        /// <param name="parent">Parent that will be assigned to the new object.</param>
        /// <param name="modName">The name of the mod.</param>
        /// <returns>The instantiated copy.</returns>
        public static Object Instantiate(Object original, Transform parent, string modName)
        {
            Object obj = Object.Instantiate(original, parent);

            RegisterObject(obj, modName);

            return obj;
        }

        /// <summary>
        /// Instantiate an object and associate it with a mod.
        /// </summary>
        /// <param name="original">An existing object that you want to make a copy of.</param>
        /// <param name="parent">Parent that will be assigned to the new object.</param>
        /// <param name="instantiateInWorldSpace">When you assign a parent Object, pass true to position the new object directly in world space. Pass false to set the Object’s position relative to its new parent.</param>
        /// <param name="modName">The name of the mod.</param>
        /// <returns>The instantiated copy.</returns>
        public static Object Instantiate(Object original, Transform parent, bool instantiateInWorldSpace, string modName)
        {
            Object obj = Object.Instantiate(original, parent, instantiateInWorldSpace);

            RegisterObject(obj, modName);

            return obj;
        }

        /// <summary>
        /// Instantiate an object and associate it with a mod.
        /// </summary>
        /// <param name="original">An existing object that you want to make a copy of.</param>
        /// <param name="position">Position for the new object.</param>
        /// <param name="rotation">Orientation of the new object.</param>
        /// <param name="modName">The name of the mod.</param>
        /// <returns>The instantiated copy.</returns>
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation, string modName)
        {
            Object obj = Object.Instantiate(original, position, rotation);

            RegisterObject(obj, modName);

            return obj;
        }

        /// <summary>
        /// Instantiate an object and associate it with a mod.
        /// </summary>
        /// <param name="original">An existing object that you want to make a copy of.</param>
        /// <param name="position">Position for the new object.</param>
        /// <param name="rotation">Orientation of the new object.</param>
        /// <param name="parent">Parent that will be assigned to the new object.</param>
        /// <param name="modName">The name of the mod.</param>
        /// <returns>The instantiated copy.</returns>
        public static Object Instantiate(Object original, Vector3 position, Quaternion rotation, Transform parent, string modName)
        {
            Object obj = Object.Instantiate(original, position, rotation, parent);

            RegisterObject(obj, modName);

            return obj;
        }

        /// <summary>
        /// Instantiate an object and associate it with a mod.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="original">An existing object that you want to make a copy of.</param>
        /// <param name="modName">The name of the mod.</param>
        /// <returns>The instantiated copy.</returns>
        public static T Instantiate<T>(T original, string modName) where T : Object
        {
            T obj = Object.Instantiate(original);

            RegisterObject(obj, modName);

            return obj;
        }

        /// <summary>
        /// Instantiate an object and associate it with a mod.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="original">An existing object that you want to make a copy of.</param>
        /// <param name="parent">Parent that will be assigned to the new object.</param>
        /// <param name="modName">The name of the mod.</param>
        /// <returns>The instantiated copy.</returns>
        public static T Instantiate<T>(T original, Transform parent, string modName) where T : Object
        {
            T obj = Object.Instantiate(original, parent);

            RegisterObject(obj, modName);

            return obj;
        }

        /// <summary>
        /// Instantiate an object and associate it with a mod.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="original">An existing object that you want to make a copy of.</param>
        /// <param name="parent">Parent that will be assigned to the new object.</param>
        /// <param name="worldPositionStays">When you assign a parent Object, pass true to position the new object directly in world space. Pass false to set the Object’s position relative to its new parent.</param>
        /// <param name="modName">The name of the mod.</param>
        /// <returns>The instantiated copy.</returns>
        public static T Instantiate<T>(T original, Transform parent, bool worldPositionStays, string modName) where T : Object
        {
            T obj = Object.Instantiate(original, parent, worldPositionStays);

            RegisterObject(obj, modName);

            return obj;
        }

        /// <summary>
        /// Instantiate an object and associate it with a mod.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="original">An existing object that you want to make a copy of.</param>
        /// <param name="position">Position for the new object.</param>
        /// <param name="rotation">Orientation of the new object.</param>
        /// <param name="modName">The name of the mod.</param>
        /// <returns>The instantiated copy.</returns>
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, string modName) where T : Object
        {
            T obj = Object.Instantiate(original, position, rotation);

            RegisterObject(obj, modName);

            return obj;
        }

        /// <summary>
        /// Instantiate an object and associate it with a mod.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="original">An existing object that you want to make a copy of.</param>
        /// <param name="position">Position for the new object.</param>
        /// <param name="rotation">Orientation of the new object.</param>
        /// <param name="parent">Parent that will be assigned to the new object.</param>
        /// <param name="modName">The name of the mod.</param>
        /// <returns></returns>
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent, string modName) where T : Object
        {
            T obj = Object.Instantiate(original, position, rotation, parent);

            RegisterObject(obj, modName);

            return obj;
        }

        /// <summary>
        /// Create a new GameObject and associate it with a mod.
        /// </summary>
        /// <param name="modName">The name of the mod.</param>
        /// <returns>The created GameObject.</returns>
        public static GameObject Create(string modName)
        {
            var obj = new GameObject();

            RegisterObject(obj, modName);

            return obj;
        }

        /// <summary>
        /// Create a new GameObject and associate it with a mod.
        /// </summary>
        /// <param name="name">The name of the GameObject.</param>
        /// <param name="modName">The name of the mod.</param>
        /// <returns>The created GameObject.</returns>
        public static GameObject Create(string name, string modName)
        {
            var obj = new GameObject(name);

            RegisterObject(obj, modName);

            return obj;
        }

        /// <summary>
        /// Create a new GameObject and associate it with a mod.
        /// </summary>
        /// <param name="name">The name of the GameObject.</param>
        /// <param name="modName">The name of the mod.</param>
        /// <param name="components">An array of component types to add to the GameObject.</param>
        /// <returns>The created GameObject.</returns>
        public static GameObject Create(string name, string modName, params System.Type[] components)
        {
            var obj = new GameObject(name, components);

            RegisterObject(obj, modName);

            return obj;
        }
        
        private static void RegisterObject(Object obj, string modName)
        {
            if (obj is GameObject)
                RegisterObject((GameObject)obj, modName);

            if (obj is Component)
                RegisterObject((Component)obj, modName);
        }

        private static void RegisterObject(Component component, string modName)
        {
            RegisterObject(component.gameObject, modName);
        }

        private static void RegisterObject(GameObject gameObject, string modName)
        {
            var modObject = gameObject.AddComponent<ModObject>();

            modObject.Initialize(modName);

            modObjects[modName].Add(gameObject);
        }

        private static void UnregisterObject(GameObject gameObject, string modName)
        {
            modObjects[modName].Remove(gameObject);
        }

        internal static void Initialize(string modName)
        {
            if (modObjects.ContainsKey(modName))
                return;

            modObjects.Add(modName, new HashSet<GameObject>());
        }

        internal static void Clear(string modName)
        {
            var objects = modObjects[modName];

            foreach (var obj in objects)
                Object.Destroy(obj.gameObject);
        }

        private class ModObject : MonoBehaviour
        {
            private string modName;

            public void Initialize(string modName)
            {
                this.modName = modName;
            }

            void OnDestroy()
            {
                UnregisterObject(gameObject, modName);
            }
        }
    }
}
