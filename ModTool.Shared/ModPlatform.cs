using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModTool.Shared
{
    /// <summary>
    /// Represents a platform or a combination of platforms.
    /// </summary>
    [Flags]
    [Serializable]
    public enum ModPlatform { Windows = 1, Linux = 2, OSX = 4, Android = 8 }

    /// <summary>
    /// Extension methods for ModPlatform.
    /// </summary>
    public static class ModPlatformExtensions
    {
        /// <summary>
        /// Does this ModPlatform include the equivalent RuntimePlatform?
        /// </summary>
        /// <param name="self">A ModPlatform instance.</param>
        /// <param name="runtimePlatform">A RuntimePlatform.</param>
        /// <returns>True if the ModPlatform includes the equivalent RuntimePlatform.</returns>
        public static bool HasRuntimePlatform(this ModPlatform self, RuntimePlatform runtimePlatform)
        {
            switch (runtimePlatform)
            {
                case RuntimePlatform.WindowsPlayer:
                    if ((self & ModPlatform.Windows) == ModPlatform.Windows)
                        return true;
                    break;
                case RuntimePlatform.WindowsEditor:
                    if ((self & ModPlatform.Windows) == ModPlatform.Windows)
                        return true;
                    break;
                case RuntimePlatform.LinuxPlayer:
                    if ((self & ModPlatform.Linux) == ModPlatform.Linux)
                        return true;
                    break;
                case RuntimePlatform.OSXPlayer:
                    if ((self & ModPlatform.OSX) == ModPlatform.OSX)
                        return true;
                    break;
                case RuntimePlatform.OSXEditor:
                    if ((self & ModPlatform.OSX) == ModPlatform.OSX)
                        return true;
                    break;
                case RuntimePlatform.Android:
                    if ((self & ModPlatform.Android) == ModPlatform.Android)
                        return true;
                    break;
            }

            return false;
        }
        
        /// <summary>
        /// Get the equivalent ModPlatform for this RuntimePlatform.
        /// </summary>
        /// <param name="self">A RuntimePlatform.</param>
        /// <returns>The equivalent ModPlatform.</returns>
        public static ModPlatform GetModPlatform(this RuntimePlatform self)
        {
            switch(self)
            {
                case RuntimePlatform.WindowsPlayer:
                    return ModPlatform.Windows;
                case RuntimePlatform.WindowsEditor:
                    return ModPlatform.Windows;
                case RuntimePlatform.LinuxPlayer:
                    return ModPlatform.Linux;
                case RuntimePlatform.OSXPlayer:
                    return ModPlatform.OSX;
                case RuntimePlatform.OSXEditor:
                    return ModPlatform.OSX;
                case RuntimePlatform.Android:
                    return ModPlatform.Android;                    
            }

            return 0;
        }

        /// <summary>
        /// Get a list of the equivalent RuntimePlatforms for this ModPlatform
        /// </summary>
        /// <param name="self">A ModPlatform instance.</param>
        /// <returns>A List of equivalent RuntimePlatforms.</returns>
        public static List<RuntimePlatform> GetRuntimePlatforms(this ModPlatform self)
        {
            List<RuntimePlatform> runtimePlatforms = new List<RuntimePlatform>();

            var values = Enum.GetValues(typeof(RuntimePlatform));

            foreach (RuntimePlatform r in values)
            {
                if (self.HasRuntimePlatform(r))
                    runtimePlatforms.Add(r);
            }

            return runtimePlatforms;
        }
    }
}
