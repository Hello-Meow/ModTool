using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;

namespace ModTool
{
    /// <summary>
    /// Extensions for the Scene class.
    /// </summary>
    internal static class SceneExtensions
    {
        /// <summary>
        /// Get a Component of Type T in this Scene. Returns the first found Component.
        /// </summary>
        /// <typeparam name="T">A Type that derives from Component</typeparam>
        /// <param name="self">A Scene instance.</param>
        /// <returns>A Component of Type T or null if none is found.</returns>
        public static T GetComponentInScene<T>(this Scene self)
        {
            if (!self.isLoaded || !self.IsValid())
            {
                return default(T);
            }

            foreach (GameObject go in self.GetRootGameObjects())
            {
                T component = go.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return default(T);
        }
               
        /// <summary>
        /// Get all components of Type T in this Scene.
        /// </summary>
        /// <typeparam name="T">A Type that derives from Component.</typeparam>
        /// <param name="self">A Scene instance.</param>
        /// <returns>An array of found Components of Type T.</returns>
        public static T[] GetComponentsInScene<T>(this Scene self)
        {
            List<T> components = new List<T>();

            if (!self.isLoaded || !self.IsValid())
            {
                return components.ToArray();
            }
            
            foreach (GameObject go in self.GetRootGameObjects())
            {
                components.AddRange(go.GetComponentsInChildren<T>(true));
            }

            return components.ToArray();
        }

        /// <summary>
        /// Get all Components of type componentType in this Scene.
        /// </summary>
        /// <typeparam name="T">A Type that derives from Component</typeparam>
        /// <param name="self">A Scene instance.</param>
        /// <param name="components">A List to populate with the found Components.</param>
        public static void GetComponentsInScene<T>(this Scene self, List<T> components)
        {
            if (!self.isLoaded || !self.IsValid())
                return;
            
            foreach (GameObject go in self.GetRootGameObjects())
            {
                components.AddRange(go.GetComponentsInChildren<T>(true));
            }
        }                
    }
}