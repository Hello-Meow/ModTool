using System.Collections.Generic;
using UnityEngine;

namespace ModTool.Shared
{
    /// <summary>
    /// Class for storing general ModTool settings.
    /// </summary>
    public class ModToolSettings : Singleton<ModToolSettings>
    {
        /// <summary>
        /// The product name for the project.
        /// </summary>
        public static string productName
        {
            get
            {
                return instance._productName;
            }
        }

        /// <summary>
        /// The unity version of the project.
        /// </summary>
        public static string unityVersion
        {
            get
            {
                return instance._unityVersion;
            }
        }
       
        /// <summary>
        /// The supported platforms for the project.
        /// </summary>
        public static ModPlatform supportedPlatforms
        {
            get
            {
                return instance._supportedPlatforms;
            }
        }

        /// <summary>
        /// The types of content that are supported for the project.
        /// </summary>
        public static ModContent supportedContent
        {
            get
            {
                return instance._supportedContent;
            }
        }

        /// <summary>
        /// ModTool's log level.
        /// </summary>
        public static LogLevel logLevel
        {
            get
            {
                return instance._logLevel;
            }
            set
            {
                instance._logLevel = value;
            }
        }

        /// <summary>
        /// List of assets that are shared with mods.
        /// </summary>
        public static List<string> sharedAssets
        {
            get
            {
                return instance._sharedAssets;
            }
        }


        /// <summary>
        /// List of packages that are shared with mods.
        /// </summary>
        public static List<string> sharedPackages
        {
            get
            {
                return instance._sharedPackages;
            }
        }
        
        [HideInInspector]
        [SerializeField]
        private string _productName;

        [HideInInspector]
        [SerializeField]
        private string _unityVersion;
        
        [HideInInspector]
        [SerializeField]
        private ModPlatform _supportedPlatforms = ModPlatform.Android | ModPlatform.Linux | ModPlatform.OSX | ModPlatform.Windows;

        [HideInInspector]
        [SerializeField]
        private ModContent _supportedContent = ModContent.Code | ModContent.Assets | ModContent.Scenes;

        [HideInInspector]
        [SerializeField]
        private LogLevel _logLevel = LogLevel.Info;

        [HideInInspector]
        [SerializeField]
        private List<string> _sharedAssets = new List<string>();

        [HideInInspector]
        [SerializeField]
        private List<string> _sharedPackages = new List<string>();

        void OnEnable()
        {
            if (string.IsNullOrEmpty(_productName))
                _productName = Application.productName;

            if (string.IsNullOrEmpty(_unityVersion))
                _unityVersion = Application.unityVersion;
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            GetInstance();
        }
    }
}
