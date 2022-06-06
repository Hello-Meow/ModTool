using System;
using System.Collections.Generic;
using ModTool.Shared;
using UnityEditor;

namespace ModTool.Editor.Exporting
{
    /// <summary>
    /// Extension methods for ModPlatform.
    /// </summary>
    public static class EditorModPlatformExtensions
    {
        /// <summary>
        /// Does this ModPlatform include the equivalent BuildTarget?
        /// </summary>
        /// <param name="self">A ModPlatform instance.</param>
        /// <param name="buildTarget">The BuildTarget to check.</param>
        /// <returns>True if the ModPlatform has the BuildTarget.</returns>
        public static bool HasBuildTarget(this ModPlatform self, BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                    if ((self & ModPlatform.Windows) == ModPlatform.Windows)
                        return true;
                    break;
                case BuildTarget.StandaloneLinuxUniversal:
                    if ((self & ModPlatform.Linux) == ModPlatform.Linux)
                        return true;
                    break;
                case BuildTarget.StandaloneOSX:
                    if ((self & ModPlatform.OSX) == ModPlatform.OSX)
                        return true;
                    break;
                case BuildTarget.Android:
                    if ((self & ModPlatform.Android) == ModPlatform.Android)
                        return true;
                    break;
            }

            return false;
        }

        /// <summary>
        /// Get the ModPlatform equivalent to this BuildTarget
        /// </summary>
        /// <param name="self">A BuildTarget instance.</param>
        /// <returns>The equivalent ModPlatform.</returns>
        public static ModPlatform GetModPlatform(this BuildTarget self)
        {
            switch (self)
            {
                case BuildTarget.StandaloneWindows:
                    return ModPlatform.Windows;
                case BuildTarget.StandaloneLinuxUniversal:
                    return ModPlatform.Linux;
                case BuildTarget.StandaloneOSX:
                    return ModPlatform.OSX;
                case BuildTarget.Android:
                    return ModPlatform.Android;
            }

            return 0;
        }

        /// <summary>
        /// Get a list of BuildTargets that are equivalent to this ModPlatform.
        /// </summary>
        /// <param name="self">A ModPlatform Instance.</param>
        /// <returns>A list with equivalent BuildTargets</returns>
        public static List<BuildTarget> GetBuildTargets(this ModPlatform self)
        {
            List<BuildTarget> runtimePlatforms = new List<BuildTarget>();

            var values = Enum.GetValues(typeof(BuildTarget));

            foreach (BuildTarget r in values)
            {
                if (self.HasBuildTarget(r))
                    runtimePlatforms.Add(r);
            }

            return runtimePlatforms;
        }
    }
}
