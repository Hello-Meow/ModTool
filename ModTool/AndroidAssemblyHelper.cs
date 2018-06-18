using System.IO;
using UnityEngine;

namespace ModTool
{
    /// <summary>
    /// A class for extracting Assemblies from the game's APK.
    /// </summary>
    internal class AndroidAssemblyHelper
    {
        /// <summary>
        /// Copy the game's Assemblies from the APK to the game's persistent datapath.
        /// </summary>
        public static void CopyAssemblies()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                string assemblyFolderPath = Path.Combine(Application.persistentDataPath, "Assemblies");

                if (!Directory.Exists(assemblyFolderPath))
                    Directory.CreateDirectory(assemblyFolderPath);

                AndroidJavaClass assemblyHelper = new AndroidJavaClass("hellomeow.assemblyhelper.AssemblyHelper");
                assemblyHelper.CallStatic<bool>("CopyAssemblies", assemblyFolderPath, GetAndroidActivity());
            }
        }
                
        private static AndroidJavaObject GetAndroidActivity()
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            return currentActivity;
        }
    }
}
