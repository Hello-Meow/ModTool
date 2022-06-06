using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ModTool
{
    /// <summary>
    /// Represents a directory that is monitored for Mods.
    /// </summary>
    internal class ModSearchDirectory : IDisposable
    {
        /// <summary>
        /// Occurs when a new Mod has been found.
        /// </summary>
        public event Action<string> ModFound;
        /// <summary>
        /// Occurs when a Mod has been removed.
        /// </summary>
        public event Action<string> ModRemoved;
        /// <summary>
        /// Occurs when a change to a Mod's directory has been detected.
        /// </summary>
        public event Action<string> ModChanged;
        /// <summary>
        /// Occurs when any change was detected for any Mod in this search directory.
        /// </summary>
        public event Action ModsChanged;

        /// <summary>
        /// This ModSearchDirectory's path.
        /// </summary>
        public string path { get; private set; }

        private Dictionary<string, long> _modPaths;

        private Thread backgroundRefresh;
        private AutoResetEvent refreshEvent;
        private bool disposed;

        /// <summary>
        /// Initialize a new ModSearchDirectory with a path.
        /// </summary>
        /// <param name="path">The path to the search directory.</param>
        public ModSearchDirectory(string path)
        {
            this.path = Path.GetFullPath(path);

            if (!Directory.Exists(this.path))            
                throw new DirectoryNotFoundException(this.path);            

            _modPaths = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            refreshEvent = new AutoResetEvent(false);

            backgroundRefresh = new Thread(BackgroundRefresh);
            backgroundRefresh.Start();
        }

        /// <summary>
        /// Refresh the collection of mod paths. Remove all missing paths and add all new paths.
        /// </summary>
        public void Refresh()
        {
            refreshEvent.Set();
        }

        private void BackgroundRefresh()
        {
            Thread.CurrentThread.IsBackground = true;

            refreshEvent.WaitOne();

            while(!disposed)
            {
                DoRefresh();

                refreshEvent.WaitOne();
            }
        }

        private void DoRefresh()
        {
            bool changed = false;            

            string[] modInfoPaths = GetModInfoPaths();

            foreach (string path in _modPaths.Keys.ToArray())
            {
                if (!modInfoPaths.Contains(path))
                {
                    changed = true;
                    RemoveModPath(path);
                    continue;
                }

                DirectoryInfo modDirectory = new DirectoryInfo(Path.GetDirectoryName(path));

                long currentTicks = DateTime.Now.Ticks;
                long lastWriteTime = _modPaths[path];

                if (modDirectory.LastWriteTime.Ticks > lastWriteTime)
                {
                    changed = true;
                    _modPaths[path] = currentTicks;
                    UpdateModPath(path);
                    continue;
                }

                foreach (DirectoryInfo directory in modDirectory.GetDirectories("*", SearchOption.AllDirectories))
                {
                    if (directory.LastWriteTime.Ticks > lastWriteTime)
                    {
                        changed = true;
                        _modPaths[path] = currentTicks;
                        UpdateModPath(path);
                        break;
                    }
                }

                foreach (FileInfo file in modDirectory.GetFiles("*", SearchOption.AllDirectories))
                {
                    if (file.Extension == ".info")
                        continue;

                    if (file.LastWriteTime.Ticks > lastWriteTime)
                    {
                        changed = true;
                        _modPaths[path] = currentTicks;
                        UpdateModPath(path);
                        break;
                    }
                }
            }

            foreach (string path in modInfoPaths)
            {
                if (!_modPaths.ContainsKey(path))
                {
                    changed = true;
                    AddModPath(path);
                }
            }

            if (changed)
                ModsChanged?.Invoke();
        }

        private void AddModPath(string path)
        {
            if (_modPaths.ContainsKey(path))
                return;

            _modPaths.Add(path, DateTime.Now.Ticks);

            ModFound?.Invoke(path);
        }

        private void RemoveModPath(string path)
        {
            if (!_modPaths.ContainsKey(path))
                return;

            _modPaths.Remove(path);
            ModRemoved?.Invoke(path);
        }

        private void UpdateModPath(string path)
        {
            if (!File.Exists(path))
            {
                RemoveModPath(path);
                return;
            }

            ModChanged?.Invoke(path);
        }
                
        private string[] GetModInfoPaths()
        {
            return Directory.GetFiles(path, "*.info", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Releases all resources used by the ModSearchDirectory.
        /// </summary>
        public void Dispose()
        {
            ModFound = null;
            ModRemoved = null;
            ModChanged = null;

            disposed = true;
            refreshEvent.Set();
            backgroundRefresh.Join();

            refreshEvent.Dispose();
        }
    }
}
