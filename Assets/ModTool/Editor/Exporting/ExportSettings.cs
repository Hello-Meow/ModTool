using UnityEngine;
using ModTool.Shared;

namespace ModTool.Editor.Exporting
{
    /// <summary>
    /// Stores the exporter's settings.
    /// </summary>
    public class ExportSettings : Singleton<ExportSettings>
    {
        /// <summary>
        /// The Mod's name.
        /// </summary>
        public static new string name
        {
            get
            {
                return instance._name;
            }
            set
            {
                instance._name = value;
            }
        }

        /// <summary>
        /// The Mod's author.
        /// </summary>
        public static string author
        {
            get
            {
                return instance._author;
            }
            set
            {
                instance._author = value;
            }
        }

        /// <summary>
        /// The Mod's description.
        /// </summary>
        public static string description
        {
            get
            {
                return instance._description;
            }
            set
            {
                instance._description = value;
            }
        }
                
        /// <summary>
        /// The Mod's version.
        /// </summary>
        public static string version
        {
            get
            {
                return instance._version;
            }
            set
            {
                instance._version = value;
            }
        }

        /// <summary>
        /// The selected platforms for which this mod will be exported.
        /// </summary>
        public static ModPlatform platforms
        {
            get
            {
                return instance._platforms;
            }
            set
            {
                instance._platforms = value;
            }
        }

        /// <summary>
        /// The selected content types that will be exported.
        /// </summary>
        public static ModContent content
        {
            get
            {
                return instance._content;
            }
            set
            {
                instance._content = value;
            }
        }
        
        /// <summary>
        /// The directory to which the Mod will be exported.
        /// </summary>
        public static string outputDirectory
        {
            get
            {
                return instance._outputDirectory;
            }
            set
            {
                instance._outputDirectory = value;
            }
        }

        [SerializeField]
        private string _name;

        [SerializeField]
        private string _author;

        [SerializeField]
        private string _description;

        [SerializeField]
        private string _version;

        [SerializeField]
        private ModPlatform _platforms = (ModPlatform)(-1);

        [SerializeField]
        private ModContent _content = (ModContent)(-1);

        [SerializeField]
        private string _outputDirectory;
    }
}
