using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace ModTool.Shared.Editor
{
    /// <summary>
    /// A set of utilities for handling assets.
    /// </summary>
    public class AssetUtility
    {
        /// <summary>
        /// Finds and returns the directory where ModTool is located.
        /// </summary>
        /// <returns>The directory where ModTool is located.</returns>
        public static string GetModToolDirectory()
        {
            string location = typeof(ModInfo).Assembly.Location;

            string modToolDirectory = Path.GetDirectoryName(location);

            if (!Directory.Exists(modToolDirectory))
                modToolDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Assets");

            return GetRelativePath(modToolDirectory);
        }

        /// <summary>
        /// Get the relative path for an absolute path.
        /// </summary>
        /// <param name="path">The absolute path.</param>
        /// <returns>The relative path.</returns>
        public static string GetRelativePath(string path)
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            Uri pathUri = new Uri(path);

            if (!currentDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
                currentDirectory += Path.DirectorySeparatorChar;

            Uri directoryUri = new Uri(currentDirectory);

            string relativePath = Uri.UnescapeDataString(directoryUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));

            return relativePath;
        }

        /// <summary>
        /// Get all asset paths for assets that match the filter.
        /// </summary>
        /// <param name="filter">The filter string can contain search data for: names, asset labels and types (class names).</param>
        /// <returns>A list of asset paths</returns>
        public static List<string> GetAssets(string filter)
        {
            List<string> assetPaths = new List<string>();

            string[] assetGuids = AssetDatabase.FindAssets(filter);

            foreach (string guid in assetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (assetPath.Contains("ModTool"))
                    continue;

                if (assetPath.StartsWith("Packages"))
                    continue;

                //NOTE: AssetDatabase.FindAssets() can contain duplicates for some reason
                if (assetPaths.Contains(assetPath))
                    continue;

                assetPaths.Add(assetPath);
            }

            return assetPaths;
        }

        /// <summary>
        /// Move assets to a directory.
        /// </summary>
        /// <param name="assetPaths">A list of asset paths</param>
        /// <param name="targetDirectory">The directory to move all assets to.</param>
        public static void MoveAssets(List<string> assetPaths, string targetDirectory)
        {
            for (int i = 0; i < assetPaths.Count; i++)
            {
                string assetPath = assetPaths[i];

                if (Path.GetDirectoryName(assetPath) != targetDirectory)
                {
                    string assetName = Path.GetFileName(assetPath);
                    string newAssetPath = Path.Combine(targetDirectory, assetName);

                    AssetDatabase.MoveAsset(assetPath, newAssetPath);
                    assetPaths[i] = newAssetPath;
                }
            }
        }

        /// <summary>
        /// Create an asset for a ScriptableObject in a ModTool Resources directory.
        /// </summary>
        /// <param name="scriptableObject">A ScriptableObject instance.</param>
        public static void CreateAsset(ScriptableObject scriptableObject)
        {
            string resourcesParentDirectory = GetModToolDirectory();
            string resourcesDirectory = "";

            resourcesDirectory = Directory.GetDirectories(resourcesParentDirectory, "Resources", SearchOption.AllDirectories).FirstOrDefault();

            if (string.IsNullOrEmpty(resourcesDirectory))
            {
                resourcesDirectory = Path.Combine(resourcesParentDirectory, "Resources");
                Directory.CreateDirectory(resourcesDirectory);
            }

            string path = Path.Combine(resourcesDirectory, scriptableObject.GetType().Name + ".asset");

            AssetDatabase.CreateAsset(scriptableObject, path);
        }        
    }
}
