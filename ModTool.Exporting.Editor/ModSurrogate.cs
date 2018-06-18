using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEditor;
using ModTool.Interface;
using ModTool.Shared;
using ModTool.Shared.Editor;

namespace ModTool.Exporting.Editor
{
    internal class ModSurrogateInitializer
    {
        [RuntimeInitializeOnLoadMethod]
        private static void InitializeModSurrogate()
        {
            ExportSettings settings = ExportSettings.instance;
            List<GameObject> prefabs = new List<GameObject>();
            List<IResource> scenes = new List<IResource>();

            List<string> prefabPaths = AssetUtility.GetAssets("t:prefab");
            foreach (string prefabPath in prefabPaths)
            {
                prefabs.Add(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            }

            List<string> scenePaths = AssetUtility.GetAssets("t:scene");
            foreach (string scenePath in scenePaths)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                scenes.Add(new ModSceneSurrogate(sceneName));
            }

            ModSurrogate mod = new ModSurrogate(settings.name);

            ContentHandler contentHandler = new ContentHandler(mod, scenes.AsReadOnly(), prefabs.AsReadOnly());

            InitializeModHandlers(contentHandler);
        }

        private static void InitializeModHandlers(ContentHandler contentHandler)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(IModHandler).IsAssignableFrom(type))
                    {
                        if (type.IsAbstract)
                            continue;
                        if (!type.IsClass)
                            continue;

                        if (type.IsSubclassOf(typeof(Component)))
                        {
                            foreach(Component component in GetComponents(type))
                            {
                                ((IModHandler)component).OnLoaded(contentHandler);
                            }

                            continue;
                        }

                        try
                        {
                            IModHandler loadHandler = (IModHandler)Activator.CreateInstance(type);
                            loadHandler.OnLoaded(contentHandler);
                        }
                        catch (Exception e)
                        {
                            if (e is MissingMethodException)
                                LogUtility.LogWarning(e.Message);
                        }
                    }
                }
            }
        }

        private static Component[] GetComponents(Type componentType)
        {
            List<Component> components = new List<Component>();

            foreach (Component component in Resources.FindObjectsOfTypeAll(componentType))
            {
                if ((component.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
                    continue;
                if ((component.hideFlags & HideFlags.NotEditable) == HideFlags.NotEditable)
                    continue;
                if ((component.hideFlags & HideFlags.HideInHierarchy) == HideFlags.HideInHierarchy)
                    continue;

                components.Add(component);
            }

            return components.ToArray();
        }
    }

    /// <summary>
    /// A class that substitutes a Mod while testing the Mod in play-mode.
    /// </summary>
    internal class ModSurrogate : IResource
    {
        public string name { get; private set; }
        
        public ModSurrogate(string name)
        {
            this.name = name;
        }

        public void Load()
        {
            
        }

        public void LoadAsync()
        {
            
        }

        public void Unload()
        {
            
        }        
    }

    /// <summary>
    /// A class that substitutes a ModScene while testing the Mod in play mode.
    /// </summary>
    internal class ModSceneSurrogate : IResource
    {
        public string name
        {
            get; private set;
        }


        public ModSceneSurrogate(string name)
        {
            this.name = name;
           
        }

        public void Load()
        {
            
        }

        public void LoadAsync()
        {
            
        }

        public void Unload()
        {
           
        }
    }
}
