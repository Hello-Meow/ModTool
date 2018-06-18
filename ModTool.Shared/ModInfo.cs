using System;
using System.IO;
using UnityEngine;

namespace ModTool.Shared
{
    /// <summary>
    /// Class that stores a Mod's name, author, description, version, path and supported platforms.
    /// </summary>
    [Serializable]
    public class ModInfo
    {
        /// <summary>
        /// Name
        /// </summary>
        public string name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// Supported platforms for this mod.
        /// </summary>
        public ModPlatform platforms
        {
            get
            {
                return _platforms;
            }
        }
        
        /// <summary>
        /// The Mod's available content types.
        /// </summary>
        public ModContent content
        {
            get
            {
                return _content;
            }
        }

        /// <summary>
        /// Mod author.
        /// </summary>
        public string author
        {
            get
            {
                return _author;
            }
        }

        /// <summary>
        /// Mod description.
        /// </summary>
        public string description
        {
            get
            {
                return _description;
            }
        }

        /// <summary>
        /// Mod version.
        /// </summary>
        public string version
        {
            get
            {
                return _version;
            }
        }

        /// <summary>
        /// The version of Unity that was used to export this mod.
        /// </summary>
        public string unityVersion
        {
            get
            {
                return _unityVersion;
            }
        }
        
        /// <summary>
        /// Should this mod be enabled.
        /// </summary>
        public bool isEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                _isEnabled = value;
            }
        }

        /// <summary>
        /// Location of mod
        /// </summary>
        public string path { get; private set; }

        [SerializeField]
        private string _name;

        [SerializeField]
        private string _author;

        [SerializeField]
        private string _description;

        [SerializeField]
        private string _version;

        [SerializeField]
        private string _unityVersion;

        [SerializeField]
        private ModPlatform _platforms;

        [SerializeField]
        private ModContent _content;

        [SerializeField]
        private bool _isEnabled;

        /// <summary>
        /// Initialize a new ModInfo.
        /// </summary>
        /// <param name="name">The Mod's name.</param>
        /// <param name="author">The Mod's author.</param>
        /// <param name="description">The Mod's description.</param>
        /// <param name="platforms">The Mod's supported platforms.</param>
        /// <param name="content">The Mod's available content types.</param>
        /// <param name="version">The Mod's version</param>
        /// <param name="unityVersion"> The version of Unity that the Mod was exported with.</param>
        public ModInfo(
            string name,
            string author,
            string description,
            string version,
            string unityVersion,
            ModPlatform platforms,
            ModContent content)
        {
            _author = author;
            _description = description;
            _name = name;
            _platforms = platforms;
            _content = content;
            _version = version;
            _unityVersion = unityVersion;

            isEnabled = false;
        }
        
        /// <summary>
        /// Save this ModInfo.
        /// </summary>
        public void Save()
        {
            if (!string.IsNullOrEmpty(path))
                Save(path, this);
        }

        /// <summary>
        /// Save a ModInfo.
        /// </summary>
        /// <param name="path">The path to save the ModInfo to.</param>
        /// <param name="modInfo">The ModInfo to save.</param>
        public static void Save(string path, ModInfo modInfo)
        {
            string json = JsonUtility.ToJson(modInfo, true);

            File.WriteAllText(path, json);         
        }

        /// <summary>
        /// Load a ModInfo.
        /// </summary>
        /// <param name="path">The path to load the ModInfo from.</param>
        /// <returns>The loaded Modinfo, if succeeded. Null otherwise.</returns>
        public static ModInfo Load(string path)
        {
            path = Path.GetFullPath(path);

            if (File.Exists(path))
            {   
                try
                {
                    string json = File.ReadAllText(path);

                    ModInfo modInfo = JsonUtility.FromJson<ModInfo>(json);

                    modInfo.path = path;

                    return modInfo;
                }
                catch(Exception e)
                {
                    LogUtility.LogWarning("There was an issue while loading the ModInfo from " + path + " - " + e.Message);                    
                }
            }

            return null;
        }        
    }        
}
