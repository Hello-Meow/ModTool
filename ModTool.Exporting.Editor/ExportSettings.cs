using UnityEngine;
using ModTool.Shared;
using ModTool.Shared.Editor;

namespace ModTool.Exporting.Editor
{
    /// <summary>
    /// Stores the exporter's settings.
    /// </summary>
    public class ExportSettings : EditorScriptableSingleton<ExportSettings>
    {
        /// <summary>
        /// The Mod's name.
        /// </summary>
        public new string name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// The Mod's author.
        /// </summary>
        public string author
        {
            get
            {
                return _author;
            }
        }

        /// <summary>
        /// The Mod's description.
        /// </summary>
        public string description
        {
            get
            {
                return _description;
            }
        }
                
        /// <summary>
        /// The Mod's version.
        /// </summary>
        public string version
        {
            get
            {
                return _version;
            }
        }

        /// <summary>
        /// The selected platforms for which this mod will be exported.
        /// </summary>
        public ModPlatform platforms
        {
            get
            {
                return _platforms;
            }
        }

        /// <summary>
        /// The selected content types that will be exported.
        /// </summary>
        public ModContent content
        {
            get
            {
                return _content;
            }
        }
        
        /// <summary>
        /// The directory to which the Mod will be exported.
        /// </summary>
        public string outputDirectory
        {
            get
            {
                return _outputDirectory;
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
