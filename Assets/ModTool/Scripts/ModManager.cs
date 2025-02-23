using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using UnityEngine;
using ModTool.Shared;

namespace ModTool
{
    /// <summary>
    /// Provides functionality to find and keep track of Mods. 
    /// </summary>
    public static class ModManager
    {
        /// <summary>
        /// Occurs when the collection of Mods has changed.
        /// </summary>
        public static event Action ModsChanged;

        /// <summary>
        /// Occurs when a Mod has been found.
        /// </summary>
        public static event Action<Mod> ModFound;

        /// <summary>
        /// Occurs when a Mod has been removed. The Mod will be marked invalid.
        /// </summary>
        public static event Action<Mod> ModRemoved;

        /// <summary>
        /// Occurs when a Mod has been loaded
        /// </summary>
        public static event Action<Mod> ModLoaded;

        /// <summary>
        /// Occurs when a Mod has been Unloaded
        /// </summary>
        public static event Action<Mod> ModUnloaded;

        /// <summary>
        /// Default directory that will be searched for mods.
        /// </summary>
        public static string defaultSearchDirectory { get; private set; }

        /// <summary>
        /// All mods that have currently been found in all search directories.
        /// </summary>
        public static Mod[] mods => _mods.ToArray();

        private static object _lock = new object();

        private static Dictionary<string, Mod> modPaths;
        private static List<Mod> _mods;

        private static List<Mod> refreshQueue;

        private static List<ModSearchDirectory> searchDirectories;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            modPaths = new Dictionary<string, Mod>();
            _mods = new List<Mod>();

            refreshQueue = new List<Mod>();
            searchDirectories = new List<ModSearchDirectory>();

            if (Application.platform == RuntimePlatform.Android)
                defaultSearchDirectory = Path.Combine(Application.persistentDataPath, "Mods");       
            else
                defaultSearchDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Mods");

            if (!Directory.Exists(defaultSearchDirectory))
                Directory.CreateDirectory(defaultSearchDirectory);

            AddSearchDirectory(defaultSearchDirectory);

            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
        }

        /// <summary>
        /// Refresh all search directories and update any new, changed or removed Mods.
        /// </summary>
        public static void Refresh()
        {
            foreach (var searchDirectory in searchDirectories)
                searchDirectory.Refresh();
        }

        /// <summary>
        /// Add a directory that will be searched for Mods
        /// </summary>
        /// <param name="path">The path of the search directory.</param>
        public static void  AddSearchDirectory(string path)
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
        public static void RemoveSearchDirectory(string path)
        {
            ModSearchDirectory directory = searchDirectories.Find(s => s.path.NormalizedPath() == path.NormalizedPath());

            if (directory == null)
                return;

            directory.Dispose();

            searchDirectories.Remove(directory);
        }

        private static void OnModLoaded(Mod mod)
        {
            ModLoaded?.Invoke(mod);
        }

        private static void OnModUnloaded(Mod mod)
        {
            ModUnloaded?.Invoke(mod);

            if (refreshQueue.Remove(mod))
                OnModChanged(mod.modInfo.path);          
        }

        private static void OnModFound(string path)
        {
            ThreadPool.QueueUserWorkItem(o => AddMod(path));
        }

        private static void OnModRemoved(string path)
        {
            RemoveMod(path);
        }

        private static void OnModChanged(string path)
        {
            LogUtility.LogInfo("Mod refreshing: " + path);

            OnModRemoved(path);

            if(File.Exists(path))
                OnModFound(path);
        }

        private static void QueueModRefresh(Mod mod)
        {
            if (refreshQueue.Contains(mod))
                return;

            LogUtility.LogInfo("Mod refresh queued: " + mod.name);
            mod.SetInvalid();
            refreshQueue.Add(mod);
        }

        private static void AddMod(string path)
        {
            lock (_lock)
            {
                if (modPaths.ContainsKey(path))
                    return;
            }

            Mod mod = new Mod(path);

            lock(_lock)            
                modPaths.Add(path, mod);

            Dispatcher.Enqueue(() => AddMod(mod), true);
        }

        private static void AddMod(Mod mod)
        {
            mod.Loaded += OnModLoaded;
            mod.Unloaded += OnModUnloaded;

            mod.UpdateConflicts(_mods);
            foreach (Mod other in _mods)
                other.UpdateConflicts(mod);

            LogUtility.LogInfo("Mod found: " + mod.name + " - " + mod.contentType);
            _mods.Add(mod);

            ModFound?.Invoke(mod);
            ModsChanged?.Invoke();
        }

        private static void RemoveMod(string path)
        {
            lock (_lock)
            {
                Mod mod;

                if (modPaths.TryGetValue(path, out mod))
                {
                    if (mod.loadState != LoadState.Unloaded)
                    {
                        Dispatcher.Enqueue(() => QueueModRefresh(mod));
                        return;
                    }

                    modPaths.Remove(path);

                    Dispatcher.Enqueue(() => RemoveMod(mod), true);
                }
            }
        }

        private static void RemoveMod(Mod mod)
        {
            mod.Loaded -= OnModLoaded;
            mod.Unloaded -= OnModUnloaded;

            mod.SetInvalid();

            foreach (Mod other in _mods)
                other.UpdateConflicts(mod);

            LogUtility.LogInfo("Mod removed: " + mod.name);
            _mods.Remove(mod);

            ModRemoved?.Invoke(mod);
            ModsChanged?.Invoke();
        }

        private static void OnDomainUnload(object sender, EventArgs e)
        {
            foreach (var searchDirectory in searchDirectories)
                searchDirectory.Dispose();
        }
    }
}
