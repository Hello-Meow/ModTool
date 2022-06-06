using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using ModTool.Shared;
using ModTool.Interface;

namespace ModTool
{
    /// <summary> 
    /// A Mod lets you load scenes, assets and code that have been exported with the game's Mod exporter.
    /// </summary>
    public class Mod : Resource<Mod>
    {
        /// <summary>
        /// This mod's ModInfo.
        /// </summary>
        public ModInfo modInfo { get; private set; }

        /// <summary>
        /// Types of content included in this Mod.
        /// </summary>
        public ModContent contentType { get; private set; }

        /// <summary>
        /// Collection of Mods that are in conflict with this Mod.
        /// </summary>
        public IReadOnlyList<Mod> conflictingMods { get; private set; }

        /// <summary>
        /// Collection of names of Assemblies included in this Mod.
        /// </summary>
        public IReadOnlyList<string> assemblyNames { get; private set; }

        /// <summary>
        /// Collection of names of Scenes included in this Mod.
        /// </summary>
        public IReadOnlyList<string> sceneNames { get; private set; }

        /// <summary>
        /// Collection of paths of assets included in this Mod.
        /// </summary>
        public IReadOnlyList<string> assetPaths { get; private set; }

        /// <summary>
        /// Collection of ModScenes included in this Mod.
        /// </summary>
        public IReadOnlyList<ModScene> scenes { get; private set; }

        /// <summary>
        /// Collection of loaded prefabs included in this Mod when the mod is loaded.
        /// </summary>
        public IReadOnlyList<GameObject> prefabs { get; private set; }

        /// <summary>
        /// Is the mod valid? A Mod becomes invalid when it is removed from the ModManager,
        /// when any of its resources can't be loaded or are missing.
        /// </summary>
        public bool isValid { get; private set; }

        /// <summary>
        /// Keeps track of which Mods to enable or disable. 
        /// This property does not affect what you can do with a Mod; a Mod that is not enabled can still be loaded.
        /// </summary>
        public bool isEnabled
        {
            get
            {
                return modInfo.isEnabled;
            }
            set
            {
                modInfo.isEnabled = value;
                modInfo.Save();
            }
        }

        /// <summary>
        /// Can this mod be loaded? False if a conflicting mod is loaded or if the mod is not valid
        /// </summary>
        public override bool canLoad
        {
            get
            {
                CheckResources();
                return isValid && !ConflictingModsLoaded();
            }
        }

        private AssetBundleResource assetsResource;
        private AssetBundleResource scenesResource;

        private AssemblyResource assemblyResource;

        private List<Mod> _conflictingMods;
        private List<ModScene> _scenes;
        private List<GameObject> _prefabs;

        private Dictionary<Type, object> allInstances;

        /// <summary>
        /// Initialize a new Mod with a ModInfo file path.
        /// </summary>
        /// <param name="path">The path to a ModInfo file</param>
        public Mod(string path) : base(Path.GetFileNameWithoutExtension(path))
        {
            try
            {
                modInfo = ModInfo.Load(path);
                contentType = modInfo.content;

                isValid = true;

                GetResources();
                CheckResources();
                Initialize();
            }
            catch(Exception e)
            {
                AddError(e.ToString());
            }

            foreach (var error in errors)
                LogUtility.LogWarning(error);
        }

        private void Initialize()
        {
            allInstances = new Dictionary<Type, object>();

            _conflictingMods = new List<Mod>();
            _prefabs = new List<GameObject>();
            _scenes = new List<ModScene>();

            conflictingMods = _conflictingMods.AsReadOnly();
            prefabs = _prefabs.AsReadOnly();
            scenes = _scenes.AsReadOnly();

            foreach (string sceneName in sceneNames)
                _scenes.Add(new ModScene(sceneName, this));
        }

        private void GetResources()
        {
            string modDirectory = Path.GetDirectoryName(modInfo.path);
            string platformDirectory = Path.Combine(modDirectory, Application.platform.GetModPlatform().ToString());

            assemblyResource = new AssemblyResource(name + " Assemblies", modDirectory);

            assetsResource = new AssetBundleResource(name + " Assets", Path.Combine(platformDirectory, name + ".assets"));
            scenesResource = new AssetBundleResource(name + " Scenes", Path.Combine(platformDirectory, name + ".scenes"));            

            assetPaths = assetsResource.assetPaths;
            sceneNames = scenesResource.assetPaths;
            assemblyNames = assemblyResource.assemblyNames;
        }

        private void CheckResources()
        {
            if (!isValid)
            {
                foreach (var message in errors)
                    LogUtility.LogWarning(message);

                return;
            }

            if (!modInfo.platforms.HasRuntimePlatform(Application.platform))
                AddError("Platform not supported for Mod: " + name);

            if ((contentType & ModContent.Code) == ModContent.Code && !assemblyResource.canLoad)
                AddErrors(assemblyResource.errors);

            if ((contentType & ModContent.Assets) == ModContent.Assets && !assetsResource.canLoad)
                AddErrors(assetsResource.errors);

            if ((contentType & ModContent.Scenes) == ModContent.Scenes && !scenesResource.canLoad)
                AddErrors(scenesResource.errors);

            if (errors.Count > 0)
                isValid = false;
        }

        private void SafeInvoke(Action action)
        {
            try
            {
                action();
            }
            catch(Exception e)
            {
                LogUtility.LogException(e);
            }
        }
        
        protected override IEnumerator LoadResources()
        {
            assemblyResource.Load();

            while (assemblyResource.loadState == LoadState.Loading)
                yield return null;

            assetsResource.Load();
            scenesResource.Load();

            yield return UpdateProgress(assetsResource, scenesResource);

            ObjectManager.Initialize(name);

            if (assetsResource.loadState == LoadState.Loaded)
                _prefabs.AddRange(assetsResource.assetBundle.LoadAllAssets<GameObject>());

            foreach (IModHandler handler in GetInstances<IModHandler>())
                SafeInvoke(() => handler.OnLoaded());
        }

        private IEnumerator UpdateProgress(params Resource[] resources)
        {
            if (resources == null || resources.Length == 0)
                yield break;

            var loadingResources = resources.Where(r => r.canLoad);

            int count = loadingResources.Count();

            while (loadingResources.Any(r => r.loadState == LoadState.Loading))
            {
                progress = 0;

                foreach (var resource in loadingResources)
                    progress += resource.progress;

                progress /= count;

                yield return null;
            }
        }

        protected override IEnumerator UnloadResources()
        {
            foreach (IModHandler handler in GetInstances<IModHandler>())
                SafeInvoke(() => handler.OnUnloaded());

            _scenes.ForEach(s => s.Unload());
            
            while (scenes.Any(s => s.loadState != LoadState.Unloaded))
                yield return null;

            ObjectManager.Clear(name);

            allInstances.Clear();            
            _prefabs.Clear();

            assemblyResource.Unload();

            assetsResource.Unload();
            scenesResource.Unload();

            while (scenesResource.loadState != LoadState.Unloaded && assetsResource.loadState != LoadState.Unloaded)
                yield return null;

            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Update this Mod's conflicting Mods with the supplied Mod
        /// </summary>
        /// <param name="other">Another Mod</param>
        public void UpdateConflicts(Mod other)
        {
            if (other == this || !isValid)
                return;

            if (!other.isValid)
            {               
                _conflictingMods.Remove(other);

                return;
            }

            foreach (string assemblyName in assemblyNames)
            {
                foreach (string otherAssemblyName in other.assemblyNames)
                {
                    if (assemblyName == otherAssemblyName)
                    {
                        LogUtility.LogWarning("Assembly " + other.name + "/" + otherAssemblyName + " conflicting with " + name + "/" + assemblyName);

                        if (!_conflictingMods.Contains(other))
                        {
                            _conflictingMods.Add(other);
                            return;
                        }
                    }
                }
            }

            foreach (string sceneName in sceneNames)
            {
                foreach (string otherSceneName in other.sceneNames)
                {
                    if (sceneName == otherSceneName)
                    {
                        LogUtility.LogWarning("Scene " + other.name + "/" + otherSceneName + " conflicting with " + name + "/" + sceneName);

                        if (!_conflictingMods.Contains(other))
                        {
                            _conflictingMods.Add(other);
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update this Mod's conflicting Mods with the supplied Mods
        /// </summary>
        /// <param name="mods">A collection of Mods</param>
        public void UpdateConflicts(IEnumerable<Mod> mods)
        {
            foreach (Mod mod in mods)
            {
                UpdateConflicts(mod);
            }
        }

        /// <summary>
        /// Is another conflicting Mod loaded?
        /// </summary>
        /// <returns>True if another conflicting mod is loaded</returns>
        public bool ConflictingModsLoaded()
        {
            return _conflictingMods.Any(m => m.loadState != LoadState.Unloaded);
        }

        /// <summary>
        /// Is another conflicting Mod enabled?
        /// </summary>
        /// <returns>True if another conflicting mod is enabled</returns>
        public bool ConflictingModsEnabled()
        {
            return _conflictingMods.Any(m => m.isEnabled);
        }

        /// <summary>
        /// Invalidate the mod
        /// </summary>
        public void SetInvalid()
        {
            isValid = false;
        }

        /// <summary>
        /// Get an asset with name.
        /// </summary>
        /// <param name="name">The asset's name.</param>
        /// <returns>The asset if it has been found. Null otherwise</returns>
        public UnityEngine.Object GetAsset(string name)
        {
            if (assetsResource.loadState == LoadState.Loaded)
                return assetsResource.assetBundle.LoadAsset(name);

            return null;
        }

        /// <summary>
        /// Get an asset with name of a certain Type.
        /// </summary>
        /// <param name="name">The asset's name.</param>
        /// <typeparam name="T">The asset Type.</typeparam>
        /// <returns>The asset if it has been found. Null otherwise</returns>
        public T GetAsset<T>(string name) where T : UnityEngine.Object
        {
            if (assetsResource.loadState == LoadState.Loaded)
                return assetsResource.assetBundle.LoadAsset<T>(name);

            return null;
        }

        /// <summary>
        /// Get all assets of a certain Type.
        /// </summary>
        /// <typeparam name="T">The asset Type.</typeparam>
        /// <returns>AssetBundleRequest that can be used to get the asset.</returns>
        public T[] GetAssets<T>() where T : UnityEngine.Object
        {
            if (assetsResource.loadState == LoadState.Loaded)
                return assetsResource.assetBundle.LoadAllAssets<T>();

            return new T[0];
        }

        /// <summary>
        /// Get an asset with name of a certain Type.
        /// </summary>
        /// <param name="name">The asset's name.</param>
        /// <typeparam name="T">The asset's Type</typeparam>
        /// <returns>AssetBundleRequest that can be used to get the asset.</returns>
        public AssetBundleRequest GetAssetAsync<T>(string name) where T : UnityEngine.Object
        {
            if (assetsResource.loadState == LoadState.Loaded)
                return assetsResource.assetBundle.LoadAssetAsync<T>(name);

            return null;
        }

        /// <summary>
        /// Get all assets of a certain Type.
        /// </summary>
        /// <typeparam name="T">The asset Type.</typeparam>
        /// <returns>AssetBundleRequest that can be used to get the assets.</returns>
        public AssetBundleRequest GetAssetsAsync<T>() where T : UnityEngine.Object
        {
            if (assetsResource.loadState == LoadState.Loaded)
                return assetsResource.assetBundle.LoadAllAssetsAsync<T>();

            return null;
        }

        /// <summary>
        /// Get all Components of type T in all prefabs
        /// </summary>
        /// <typeparam name="T">The Component that will be looked for.</typeparam>
        /// <returns>An array of found Components of Type T.</returns>
        public T[] GetComponentsInPrefabs<T>()
        {
            List<T> components = new List<T>();

            foreach (GameObject prefab in prefabs)
            {
                components.AddRange(prefab.GetComponentsInChildren<T>());
            }

            return components.ToArray();
        }

        /// <summary>
        /// Get all Components of type T in all prefabs.
        /// </summary>
        /// <typeparam name="T">The Component that will be looked for.</typeparam>
        /// <param name="components">A List that will be populated with the found Components.</param>
        public void GetComponentsInPrefabs<T>(List<T> components)
        {
            foreach (GameObject prefab in prefabs)
                prefab.GetComponentsInChildren(components);
        }

        /// <summary>
        /// Get all Components of type T in all loaded ModScenes.
        /// </summary>
        /// <typeparam name="T">The Component that will be looked for.</typeparam>
        /// <returns>An array of found Components of Type T.</returns>
        public T[] GetComponentsInScenes<T>()
        {
            List<T> components = new List<T>();

            foreach (ModScene scene in _scenes)
            {
                components.AddRange(scene.GetComponentsInScene<T>());
            }

            return components.ToArray();
        }

        /// <summary>
        /// Get all Components of type T in all loaded ModScenes.
        /// </summary>
        /// <typeparam name="T">The Component that will be looked for.</typeparam>
        /// <param name="components">A List that will be populated with the found Components.</param>
        public void GetComponentsInScenes<T>(List<T> components)
        {
            foreach (ModScene scene in _scenes)
                scene.GetComponentsInScene(components);
        }

        /// <summary>
        /// Get instances of all non-UnityEngine.Object Types included in the Mod that implement or derive from Type T.
        /// Reuses existing instances and creates new instances for Types that have no instance yet.
        /// </summary>
        /// <typeparam name="T">The Type that will be looked for</typeparam>
        /// <param name="args">Optional arguments for the Type's constructor</param>
        /// <returns>A List of Instances of Types that implement or derive from Type T</returns>
        public T[] GetInstances<T>(params object[] args)
        {
            List<T> instances = new List<T>();

            foreach (Assembly assembly in assemblyResource.assemblies)
            {
                try
                {
                    instances.AddRange(GetInstances<T>(assembly, args));
                }
                catch (Exception e)
                {
                    LogUtility.LogException(e);
                }
            }

            return instances.ToArray();
        }

        private T[] GetInstances<T>(Assembly assembly, params object[] args)
        {
            List<T> instances = new List<T>();

            foreach (Type type in assembly.GetTypes())
            {
                if (!typeof(T).IsAssignableFrom(type))
                    continue;

                if (!type.IsClass || type.IsAbstract)
                    continue;

                if (type.IsSubclassOf(typeof(UnityEngine.Object)))
                    continue;

                object foundInstance;

                if (allInstances.TryGetValue(type, out foundInstance))
                {
                    instances.Add((T)foundInstance);
                    continue;
                }

                try
                {
                    T instance = (T)Activator.CreateInstance(type, args);
                    instances.Add(instance);
                    allInstances.Add(type, instance);
                }
                catch (Exception e)
                {
                    if (e is MissingMethodException)
                        LogUtility.LogWarning(e.Message);
                    else
                        LogUtility.LogException(e);
                }
            }

            return instances.ToArray();
        }
    }
}
