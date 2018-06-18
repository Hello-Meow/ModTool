using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using ModTool.Shared;

namespace ModTool
{   
    /// <summary> 
    /// Main class for finding mods
    /// </summary>
    public class ModManager : UnitySingleton<ModManager>
    {
        private object _lock = new object();

        /// <summary>
        /// Occurs when the collection of Mods has changed.
        /// </summary>
        public event Action ModsChanged;
        /// <summary>
        /// Occurs when a Mod has been found.
        /// </summary>
        public event Action<Mod> ModFound;
        /// <summary>
        /// Occurs when a Mod has been removed. The Mod will be marked invalid.
        /// </summary>
        public event Action<Mod> ModRemoved;
        /// <summary>
        /// Occurs when a Mod has been loaded
        /// </summary>
        public event Action<Mod> ModLoaded;
        /// <summary>
        /// Occurs when a Mod has been Unloaded
        /// </summary>
        public event Action<Mod> ModUnloaded;
        /// <summary>
        /// Occurs when a Mod has cancelled async loading
        /// </summary>
        public event Action<Mod> ModLoadCancelled;
        /// <summary>
        /// Occurs when a ModScene has been loaded
        /// </summary>
        public event Action<ModScene> SceneLoaded;
        /// <summary>
        /// Occurs when a ModScene has been unloaded
        /// </summary>
        public event Action<ModScene> SceneUnloaded;
        /// <summary>
        /// Occurs when a ModScene has cancelled async loading
        /// </summary>
        public event Action<ModScene> SceneLoadCancelled;

        /// <summary>
        /// Default directory that will be searched for mods.
        /// </summary>
        public string defaultSearchDirectory { get; private set; }

        /// <summary>
        /// The interval (in seconds) between refreshing Mod search directories. 
        /// Set to 0 to disable auto refreshing.
        /// </summary>
        public int refreshInterval
        {
            get
            {
                return _refreshInterval;
            }
            set
            {
                _refreshInterval = value;

                StopAllCoroutines();

                if (_refreshInterval < 1)
                    return;

                wait = new WaitForSeconds(_refreshInterval);
                StartCoroutine(AutoRefreshSearchDirectories());
            }
        }

        /// <summary>
        /// All mods that have been found in all search directories.
        /// </summary>
        public ReadOnlyCollection<Mod> mods { get; private set; }

        private Dispatcher dispatcher;

        private Dictionary<string, Mod> _modPaths;
        private List<Mod> _mods;
        private List<Mod> queuedRefreshMods;
        
        private List<ModSearchDirectory> searchDirectories;
        private int _refreshInterval;
        private WaitForSeconds wait;
        
        protected override void Awake()
        {
            base.Awake();

            LogUtility.logLevel = ModToolSettings.logLevel;

            dispatcher = Dispatcher.instance;

            _mods = new List<Mod>();
            _modPaths = new Dictionary<string, Mod>();
            queuedRefreshMods = new List<Mod>();
            searchDirectories = new List<ModSearchDirectory>();

            mods = _mods.AsReadOnly();
            
            defaultSearchDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Mods");

            if (Application.platform == RuntimePlatform.Android)
            {
                //Note: android needs managed assemblies copied from apk so verification can resolve assemblies.
                defaultSearchDirectory = Path.Combine(Application.persistentDataPath, "Mods");
                AndroidAssemblyHelper.CopyAssemblies();
            }

            if (!Directory.Exists(defaultSearchDirectory))
                Directory.CreateDirectory(defaultSearchDirectory);
            
            AddSearchDirectory(defaultSearchDirectory);            
        }

        private void OnModLoaded(Resource mod)
        {
            if (ModLoaded != null)
                ModLoaded.Invoke((Mod)mod);
        }

        private void OnModUnloaded(Resource mod)
        {
            Mod _mod = (Mod)mod;

            if (ModUnloaded != null)
                ModUnloaded.Invoke(_mod);

            if (queuedRefreshMods.Contains(_mod))
            {
                queuedRefreshMods.Remove(_mod);
                OnModChanged(_mod.modInfo.path);
            }
        }

        private void OnModLoadCancelled(Resource mod)
        {
            if (ModLoadCancelled != null)
                ModLoadCancelled.Invoke((Mod)mod);
        }

        private void OnSceneLoaded(ModScene scene)
        {
            if (SceneLoaded != null)
                SceneLoaded.Invoke(scene);
        }

        private void OnSceneUnloaded(ModScene scene)
        {
            if (SceneUnloaded != null)
                SceneUnloaded.Invoke(scene);
        }

        private void OnSceneLoadCancelled(ModScene scene)
        {
            if (SceneLoadCancelled != null)
            {
                SceneLoadCancelled.Invoke(scene);
            }
        }
        
        /// <summary>
        /// Add a directory that will be searched for Mods
        /// </summary>
        /// <param name="path">The path of the search directory.</param>
        public void AddSearchDirectory(string path)
        {
            if (searchDirectories.Any(s => s.path.NormalizedPath() == path.NormalizedPath()))
                return;

            ModSearchDirectory directory = new ModSearchDirectory(path);

            directory.ModFound += OnModFound;
            directory.ModRemoved += OnModRemoved;
            directory.ModChanged += OnModChanged;

            searchDirectories.Add(directory);

            directory.Refresh();
        }

        /// <summary>
        /// Remove a directory that will be searched for mods
        /// </summary>
        /// <param name="path">The path of the search directory.</param>
        public void RemoveSearchDirectory(string path)
        {
            ModSearchDirectory directory = searchDirectories.Find(s => s.path.NormalizedPath() == path.NormalizedPath());

            if (directory == null)
                return;

            directory.Dispose();

            searchDirectories.Remove(directory);
        }

        /// <summary>
        /// Refresh all search directories and update any new, changed or removed Mods.
        /// </summary>
        public void RefreshSearchDirectories()
        {
            foreach (ModSearchDirectory searchDirectory in searchDirectories)
                searchDirectory.Refresh();
        }

        IEnumerator AutoRefreshSearchDirectories()
        {
            while (true)
            {
                RefreshSearchDirectories();
                yield return wait;
            }
        }

        private void OnModFound(string path)
        {
            //AddMod(path);
            ThreadPool.QueueUserWorkItem(o => AddMod(path));
        }

        private void OnModRemoved(string path)
        {
            RemoveMod(path);
        }

        private void OnModChanged(string path)
        {
            RefreshMod(path);
        }

        private void RefreshMod(string path)
        {
            LogUtility.LogInfo("Mod refreshing: " + path);
            OnModRemoved(path);
            OnModFound(path);
        }

        private void QueueModRefresh(Mod mod)
        {
            if (queuedRefreshMods.Contains(mod))
                return;

            LogUtility.LogInfo("Mod refresh queued: " + mod.name);
            mod.SetInvalid();
            queuedRefreshMods.Add(mod);
        }

        private void AddMod(string path)
        {
            lock (_lock)
            {
                if (_modPaths.ContainsKey(path))
                    return;
            }

            Mod mod = new Mod(path);
            
            lock (_lock)
            {
                _modPaths.Add(path, mod);
            }

            dispatcher.Enqueue(() => AddMod(mod), true);
        }

        private void AddMod(Mod mod)
        {
            mod.Loaded += OnModLoaded;
            mod.Unloaded += OnModUnloaded;
            mod.LoadCancelled += OnModLoadCancelled;
            mod.SceneLoaded += OnSceneLoaded;
            mod.SceneUnloaded += OnSceneUnloaded;
            mod.SceneLoadCancelled += OnSceneLoadCancelled;

            mod.UpdateConflicts(_mods);
            foreach (Mod other in _mods)
                other.UpdateConflicts(mod);

            LogUtility.LogInfo("Mod found: " + mod.name + " - " + mod.contentType);
            _mods.Add(mod);

            ModFound?.Invoke(mod);
            ModsChanged?.Invoke();
        }

        private void RemoveMod(string path)
        {
            lock (_lock)
            {
                Mod mod;

                if (_modPaths.TryGetValue(path, out mod))
                {
                    if (mod.loadState != ResourceLoadState.Unloaded)
                    {
                        dispatcher.Enqueue(() => QueueModRefresh(mod));
                        return;
                    }

                    _modPaths.Remove(path);

                    dispatcher.Enqueue(() => RemoveMod(mod), true);
                }
            }
        }
        
        private void RemoveMod(Mod mod)
        {
            mod.Loaded -= OnModLoaded;
            mod.Unloaded -= OnModUnloaded;
            mod.LoadCancelled -= OnModLoadCancelled;
            mod.SceneLoaded -= OnSceneLoaded;
            mod.SceneUnloaded -= OnSceneUnloaded;
            mod.SceneLoadCancelled -= OnSceneLoadCancelled;
            mod.SetInvalid();

            foreach (Mod other in _mods)
                other.UpdateConflicts(mod);

            LogUtility.LogInfo("Mod removed: " + mod.name);
            _mods.Remove(mod);

            ModRemoved?.Invoke(mod);
            ModsChanged?.Invoke();
        }
                
        protected override void OnDestroy()
        {
            queuedRefreshMods.Clear();

            foreach (Mod mod in _mods)
            {
                mod.Unload();
                mod.SetInvalid();
            }

            foreach (ModSearchDirectory searchDirectory in searchDirectories)
            {
                searchDirectory.Dispose();
            }

            base.OnDestroy();
        }
    }
}
