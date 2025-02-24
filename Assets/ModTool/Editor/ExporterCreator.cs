using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEditor;
using ModTool.Shared;
using UnityEditorInternal;
using System;

namespace ModTool.Editor
{
    internal class ExporterCreator
    {
        /// <summary>
        /// Create a mod exporter package for this game.
        /// </summary>
        [MenuItem("Tools/ModTool/Create Exporter", priority = 1)]
        public static void CreateExporter()
        {
            CreateExporter(Directory.GetCurrentDirectory(), true);
        }

        /// <summary>
        /// Create a mod exporter package after building the game.
        /// </summary>
        [UnityEditor.Callbacks.PostProcessBuild]
        public static void CreateExporterPostBuild(BuildTarget target, string pathToBuiltProject)
        {
            pathToBuiltProject = Path.GetDirectoryName(pathToBuiltProject);

            CreateExporter(pathToBuiltProject);
        }

        private static void CreateExporter(string path, bool revealPackage = false)
        {
            LogUtility.LogInfo("Creating Mod Exporter for " + ModToolSettings.productName);

            UpdateSettings();

            ModToolSettings modToolSettings = ModToolSettings.instance;
            CodeSettings codeSettings = CodeSettings.instance;

            string modToolDirectory = GetModToolDirectory();
            string exporterPath = Path.Combine(modToolDirectory, Path.Combine("Editor", Path.Combine("Exporting", "ModTool.Editor.Exporting.asmdef")));


            string fileName = Path.Combine(path, Application.productName + " Mod Tools.unitypackage");
            string projectSettingsDirectory = "ProjectSettings";

            List<string> assetPaths = new List<string>
            {
                AssetDatabase.GetAssetPath(modToolSettings),
                AssetDatabase.GetAssetPath(codeSettings),
                Path.Combine(projectSettingsDirectory, "EditorBuildSettings.asset"),
                Path.Combine(projectSettingsDirectory, "InputManager.asset"),
                Path.Combine(projectSettingsDirectory, "TagManager.asset"),
                Path.Combine(projectSettingsDirectory, "Physics2DSettings.asset"),
                Path.Combine(projectSettingsDirectory, "DynamicsManager.asset"),                
                //TODO: include ProjectSettings.asset?
            };

            string[] folders = new string[]
            {
                Path.Combine(modToolDirectory, Path.Combine("Scripts", "Shared")),
                Path.Combine(modToolDirectory, Path.Combine("Scripts", "Interface")),                
                Path.Combine(modToolDirectory, Path.Combine("Scripts", "Mono.Cecil")),
                Path.Combine(modToolDirectory, Path.Combine("Editor", "Exporting")),
                Path.Combine(modToolDirectory, "Resources"),
            };

            foreach (var folder in folders)
                assetPaths.AddRange(GetAssetPaths(folder));

            assetPaths.AddRange(ModToolSettings.sharedAssets);

            //Note: Only enable exporter assembly definition for the exporter package.
            SetAssemblyDefinitionEnabled(exporterPath, true);

            AssetDatabase.ExportPackage(assetPaths.ToArray(), fileName);

            SetAssemblyDefinitionEnabled(exporterPath, false);

            if (revealPackage)
                EditorUtility.RevealInFinder(fileName);
        }

        private static string[] GetAssetPaths(string path, string filter = "*")
        {
            string[] guids = AssetDatabase.FindAssets(filter, new string[] { path });

            string[] assetPaths = new string[guids.Length];

            for (int i = 0; i < guids.Length; i++)
                assetPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);

            return assetPaths;
        }

        private static void SetAssemblyDefinitionEnabled(string path, bool enabled)
        {
            var asmDef = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);

            AssemblyData assemblyData = JsonUtility.FromJson<AssemblyData>(asmDef.text);

            List<string> excludedPlatforms = new List<string>(assemblyData.excludePlatforms);

            if (enabled)
                excludedPlatforms.Remove("Editor");
            else if (!excludedPlatforms.Contains("Editor"))
                excludedPlatforms.Add("Editor");

            assemblyData.excludePlatforms = excludedPlatforms.ToArray();

            File.WriteAllText(path, JsonUtility.ToJson(assemblyData, true));

            var asset = AssetImporter.GetAtPath(path);

            asset.SaveAndReimport();
        }

        private static void UpdateSettings()
        {
            if (string.IsNullOrEmpty(ModToolSettings.productName) || ModToolSettings.productName != Application.productName)
                typeof(ModToolSettings).GetField("_productName", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ModToolSettings.instance, Application.productName);

            if (string.IsNullOrEmpty(ModToolSettings.unityVersion) || ModToolSettings.unityVersion != Application.unityVersion)
                typeof(ModToolSettings).GetField("_unityVersion", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ModToolSettings.instance, Application.unityVersion);

            EditorUtility.SetDirty(ModToolSettings.instance);
        }

        private static string GetModToolDirectory()
        {    
            var guid = AssetDatabase.FindAssets("t:folder ModTool");

            string modToolDirectory = AssetDatabase.GUIDToAssetPath(guid[0]);

            return modToolDirectory;
        }

        [Serializable]
        private class AssemblyData
        {
            public string name;
            public string[] references;
            public string[] includePlatforms;
            public string[] excludePlatforms;
        }
    }
}
