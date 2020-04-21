using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using ModTool.Shared;
using ModTool.Shared.Verification;
using ModTool.Interface;

namespace ModTool
{   
    /// <summary>
    /// Class that represents a Mod. 
    /// A Mod lets you load scenes, prefabs and Assemblies that have been exported with the game's generated ModTools.
    /// </summary>
    public class Mod : Resource 
    {
        /// <summary>
        /// Occurs when a ModScene has been loaded
        /// </summary>
        public event Action<ModScene> SceneLoaded;
        /// <summary>
        /// Occurs when a ModScene has been unloaded
        /// </summary>
        public event Action<ModScene> SceneUnloaded;
        /// <summary>
        /// Occurs when a ModScene has cancelled async loading.
        /// </summary>
        public event Action<ModScene> SceneLoadCancelled;
        
        /// <summary>
        /// Collection of names of Assemblies included in this Mod.
        /// </summary>
        public ReadOnlyCollection<string> assemblyNames { get; private set; }

        /// <summary>
        /// Collection of Mods that are in conflict with this Mod.
        /// </summary>
        public ReadOnlyCollection<Mod> conflictingMods { get; private set; }

        /// <summary>
        /// Collection of names of Scenes included in this Mod.
        /// </summary>
        public ReadOnlyCollection<string> sceneNames { get; private set; }

        /// <summary>
        /// Collection of paths of assets included in this Mod.
        /// </summary>
        public ReadOnlyCollection<string> assetPaths { get; private set; }

        /// <summary>
        /// Collection of ModScenes included in this Mod.
        /// </summary>
        public ReadOnlyCollection<ModScene> scenes { get; private set; }

        /// <summary>
        /// Collection of loaded prefabs included in this Mod. Only available when the mod is loaded.
        /// </summary>
        public ReadOnlyCollection<GameObject> prefabs { get; private set; }
                        
        /// <summary>
        /// This mod's ModInfo.
        /// </summary>
        public ModInfo modInfo { get; private set; }
        
        /// <summary>
        /// Types of content included in this Mod.
        /// </summary>
        public ModContent contentType { get; private set; }

        /// <summary>
        /// Is this Mod or any of its resources currently busy loading?
        /// </summary>
        public override bool isBusy
        {
            get
            {
                return base.isBusy || _scenes.Any(s => s.isBusy);
            }
        }

        /// <summary>
        /// Can this mod be loaded? False if a conflicting mod is loaded, if the mod is not enabled or if the mod is not valid
        /// </summary>
        public override bool canLoad
        {
            get
            {
                CheckResources();
                return !ConflictingModsLoaded() && isValid;
            }
        }

        /// <summary>
        /// Set the mod to be enabled or disabled
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
        /// Is the mod valid? A Mod becomes invalid when it is no longer being managed by the ModManager,
        /// when any of its resources is missing or can't be loaded.
        /// </summary>
        public bool isValid { get; private set; }

        /// <summary>
        /// The Mod's ContentHandler. Use for instantiating Objects and adding Components that have to be initialized for this mod, 
        /// or cleaned up after the mod is unloaded.
        /// </summary>
        public ContentHandler contentHandler { get; private set; }

        private List<string> assemblyFiles;

        private AssetBundleResource assetsResource;
        private AssetBundleResource scenesResource;
        private List<Assembly> assemblies;
        
        private List<string> _assemblyNames;
        private List<Mod> _conflictingMods;
        private List<ModScene> _scenes;
        private List<GameObject> _prefabs;
        
        private Dictionary<Type, object> allInstances;
                
        /// <summary>
        /// Initialize a new Mod with a path to a mod file.
        /// </summary>
        /// <param name="path">The path to a mod file</param>
        public Mod(string path) : base(Path.GetFileNameWithoutExtension(path))
        {
            modInfo = ModInfo.Load(path);

            contentType = modInfo.content;

            string modDirectory = Path.GetDirectoryName(path);
            string platformDirectory = Path.Combine(modDirectory, Application.platform.GetModPlatform().ToString());

            string assets = Path.Combine(platformDirectory, modInfo.name.ToLower() + ".assets");
            string scenes = Path.Combine(platformDirectory, modInfo.name.ToLower() + ".scenes");

            assemblyFiles = AssemblyUtility.GetAssemblies(modDirectory, AssemblyFilter.ModAssemblies);
            assetsResource = new AssetBundleResource(name + " assets", assets);
            scenesResource = new AssetBundleResource(name + " scenes", scenes);

            isValid = true;

            Initialize();

            VerifyAssemblies();

            CheckResources();
        }

        private void Initialize()
        {
            allInstances = new Dictionary<Type, object>();
            assemblies = new List<Assembly>();
            _prefabs = new List<GameObject>();
            _scenes = new List<ModScene>();
            _conflictingMods = new List<Mod>();
            _assemblyNames = new List<string>();

            prefabs = _prefabs.AsReadOnly();
            scenes = _scenes.AsReadOnly();
            conflictingMods = _conflictingMods.AsReadOnly();
            assemblyNames = _assemblyNames.AsReadOnly();

            assetPaths = assetsResource.assetPaths;
            sceneNames = scenesResource.assetPaths;

            assetsResource.Loaded += OnAssetsResourceLoaded;
            scenesResource.Loaded += OnScenesResourceLoaded;

            foreach (string sceneName in sceneNames)
            {
                ModScene modScene = new ModScene(sceneName, this);

                modScene.Loaded += OnSceneLoaded;
                modScene.Unloaded += OnSceneUnloaded;
                modScene.LoadCancelled += OnSceneLoadCancelled;

                _scenes.Add(modScene);
            }

            foreach (string assembly in assemblyFiles)
                _assemblyNames.Add(Path.GetFileName(assembly));

            contentHandler = new ContentHandler(this, _scenes.Cast<IResource>().ToList().AsReadOnly(), prefabs);
        }

        private void CheckResources()
        {
            if(!modInfo.platforms.HasRuntimePlatform(Application.platform))
            {
                isValid = false;
                LogUtility.LogWarning("Platform not supported for Mod: " + name);

                return;
            }

            if ((contentType & ModContent.Assets) == ModContent.Assets && !assetsResource.canLoad)
            {
                isValid = false;
                LogUtility.LogWarning("Assets assetbundle missing for Mod: " + name);
            }

            if ((contentType & ModContent.Scenes) == ModContent.Scenes && !scenesResource.canLoad)
            {
                isValid = false;
                LogUtility.LogWarning("Scenes assetbundle missing for Mod: " + name);
            }

            if ((contentType & ModContent.Code) == ModContent.Code && assemblyFiles.Count == 0)
            {
                isValid = false;
                LogUtility.LogWarning("Assemblies missing for Mod: " + name);
            }

            foreach (string path in assemblyFiles)
            {
                if(!File.Exists(path))
                {
                    isValid = false;
                    LogUtility.LogWarning(path + " missing for Mod: " + name);
                }
            }            
        }

        private void VerifyAssemblies()
        {
            List<string> messages = new List<string>();

            AssemblyVerifier.VerifyAssemblies(assemblyFiles, messages);

            if(messages.Count > 0)
            {
                SetInvalid();

                LogUtility.LogWarning("Incompatible assemblies found for Mod: " + name);

                foreach (var message in messages)
                    LogUtility.LogWarning(message);
            }          
        }

        private void LoadAssemblies()
        {
            foreach (string path in assemblyFiles)
            {
                if (!File.Exists(path))
                    continue;

                try
                {
                    Assembly assembly = Assembly.Load(File.ReadAllBytes(path));
                    assembly.GetTypes();
                    assemblies.Add(assembly);
                }
                catch (Exception e)
                {
                    LogUtility.LogException(e);
                    SetInvalid();
                    Unload();
                }
            }
        }
                        
        private void OnAssetsResourceLoaded(Resource resource)
        {
            try
            {
                if (assetsResource.assetBundle == null)
                    throw new Exception("Could not load assets.");
                
                GameObject[] prefabs = assetsResource.assetBundle.LoadAllAssets<GameObject>();
                _prefabs.AddRange(prefabs);
            }
            catch (Exception e)
            {
                LogUtility.LogException(e);
                SetInvalid();
                Unload();
            }
        }

        private void OnScenesResourceLoaded(Resource resource)
        {
            if (scenesResource.assetBundle == null)
            {
                LogUtility.LogError("Could not load scenes.");
                SetInvalid();
                Unload();
            }
        }

        protected override IEnumerator LoadResources()
        {
            LogUtility.LogInfo("Loading Mod: " + name);
            
            LoadAssemblies();

            assetsResource.Load();            

            scenesResource.Load();            
            
            yield break;
        }        
                
        protected override IEnumerator LoadResourcesAsync()
        {
            LogUtility.LogInfo("Async loading Mod: " + name);

            LoadAssemblies();

            assetsResource.LoadAsync();

            scenesResource.LoadAsync();
            
            yield return UpdateProgress(assetsResource, scenesResource);
        }

        private IEnumerator UpdateProgress(params Resource[] resources)
        {
            if (resources == null || resources.Length == 0)
                yield break;

            var loadingResources = resources.Where(r => r.canLoad);

            int count = loadingResources.Count();

            while (true)
            {
                bool isDone = true;
                float progress = 0;

                foreach (var resource in loadingResources)
                {
                    isDone = isDone && resource.loadState == ResourceLoadState.Loaded;
                    progress += resource.loadProgress;
                }

                loadProgress = progress / count;

                if (isDone)
                    yield break;

                yield return null;
            }
        }
        
        protected override void PreUnLoadResources()
        {
            contentHandler.Clear();

            _scenes.ForEach(s => s.Unload());
            
            foreach (IModHandler loader in GetInstances<IModHandler>())
            {
                loader.OnUnloaded();
            }
        }
                
        protected override void UnloadResources()
        {
            LogUtility.LogInfo("Unloading Mod: " + name);

            allInstances.Clear();
            assemblies.Clear();
            _prefabs.Clear();

            assetsResource.Unload();
            scenesResource.Unload();

            Resources.UnloadUnusedAssets();
            GC.Collect();
        }
                
        private void OnSceneLoaded(Resource scene) 
        {            
            SceneLoaded?.Invoke((ModScene)scene);
        }

        private void OnSceneLoadCancelled(Resource scene)
        {            
            SceneLoadCancelled?.Invoke((ModScene)scene);

            if (!_scenes.Any(s => s.isBusy))
                End();
        }

        private void OnSceneUnloaded(Resource scene)
        {
            SceneUnloaded?.Invoke((ModScene)scene);
            
            if (!_scenes.Any(s => s.isBusy))
                End();
        }
                
        protected override void OnLoadResumed()
        {
            //resume scene loading
            foreach (ModScene scene in _scenes)
            {
                if (scene.loadState == ResourceLoadState.Cancelling)
                    scene.Load();
            }

            base.OnLoadResumed();
        }
                
        protected override void OnLoaded()
        {
            foreach (IModHandler loader in GetInstances<IModHandler>())
                loader.OnLoaded(contentHandler);

            base.OnLoaded();
        }               
        
        /// <summary>
        /// Update this Mod's conflicting Mods with the supplied Mod
        /// </summary>
        /// <param name="other">Another Mod</param>
        public void UpdateConflicts(Mod other)
        {
            if (other == this || !isValid)
                return;
            
            if(!other.isValid)
            {
                if (_conflictingMods.Contains(other))
                    _conflictingMods.Remove(other);

                return;
            }

            foreach (string assemblyName in _assemblyNames)
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
            foreach(Mod mod in mods)
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
            return _conflictingMods.Any(m => m.loadState != ResourceLoadState.Unloaded);
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
            if (assetsResource.loadState == ResourceLoadState.Loaded)
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
            if (assetsResource.loadState == ResourceLoadState.Loaded)
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
            if (assetsResource.loadState == ResourceLoadState.Loaded)
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
            if (assetsResource.loadState == ResourceLoadState.Loaded)
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
            if (assetsResource.loadState == ResourceLoadState.Loaded)
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
            
            foreach(GameObject prefab in prefabs)
            {
                components.AddRange(prefab.GetComponentsInChildren<T>());
            }

            return components.ToArray();
        }
        
        /// <summary>
        /// Get all Components of type T in all loaded ModScenes.
        /// </summary>
        /// <typeparam name="T">The Component that will be looked for.</typeparam>
        /// <returns>An array of found Components of Type T.</returns>
        public T[] GetComponentsInScenes<T>()
        {
            if (!typeof(T).IsSubclassOf(typeof(Component)))
                throw new ArgumentException(typeof(T).Name + " is not a component.");

            List<T> components = new List<T>();

            foreach(ModScene scene in _scenes)
            {
                components.AddRange(scene.GetComponentsInScene<T>());
            }

            return components.ToArray();
        }
        
        /// <summary>
        /// Get instances of all Types included in the Mod that implement or derive from Type T.
        /// Reuses existing instances and creates new instances for Types that have no instance yet.
        /// Does not instantiate Components; returns all active instances of the Component instead.
        /// </summary>
        /// <typeparam name="T">The Type that will be looked for</typeparam>
        /// <param name="args">Optional arguments for the Type's constructor</param>
        /// <returns>A List of Instances of Types that implement or derive from Type T</returns>
        public T[] GetInstances<T>(params object[] args)
        {
            List<T> instances = new List<T>();

            if (loadState != ResourceLoadState.Loaded)
                return instances.ToArray();
            
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    instances.AddRange(GetInstances<T>(assembly, args));
                }
                catch(Exception e)
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

                if (type.IsAbstract)
                    continue;

                if (!type.IsClass)
                    continue;

                object foundInstance;
                if (allInstances.TryGetValue(type, out foundInstance))
                {
                    //LogUtility.Log("existing instance of " + typeof(T).Name + " found: " + type.Name);
                    instances.Add((T)foundInstance);
                    continue;
                }

                if (type.IsSubclassOf(typeof(Component)))
                {
                    foreach (Component component in GetComponents(type))
                    {
                        instances.Add((T)(object)component);
                    }
                    continue;
                }

                try
                {
                    T instance = (T)Activator.CreateInstance(type, args);
                    instances.Add(instance);
                    allInstances.Add(type, instance);
                }
                catch(Exception e)
                {
                    if (e is MissingMethodException)
                        LogUtility.LogWarning(e.Message);
                    else
                        LogUtility.LogException(e);
                }
            }

            return instances.ToArray();
        }
                
        private static Component[] GetComponents(Type componentType)
        {
            List<Component> components = new List<Component>();
            
            for(int i = 0; i < SceneManager.sceneCount; i++)
            {
                components.AddRange(SceneManager.GetSceneAt(i).GetComponentsInScene(componentType));
            }

            return components.ToArray();
        }
    }
}
