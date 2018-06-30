using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEditor;
using ModTool.Shared;
using ModTool.Shared.Editor;

//Note: ModTool uses an old version of Mono.Cecil in the editor
#pragma warning disable CS0618

namespace ModTool.Editor
{
    internal class ExporterCreator
    {
        /// <summary>
        /// Create a mod exporter package for this game.
        /// </summary>
        [MenuItem("Tools/ModTool/Create Exporter")]
        public static void CreateExporter()
        {
            CreateExporter(ExportPackageOptions.Interactive, Directory.GetCurrentDirectory());
        }

        /// <summary>
        /// Create a mod exporter package after building the game.
        /// </summary>
        [UnityEditor.Callbacks.PostProcessBuild]
        public static void CreateExporterPostBuild(BuildTarget target, string pathToBuiltProject)
        {
            pathToBuiltProject = Path.GetDirectoryName(pathToBuiltProject);

            CreateExporter(ExportPackageOptions.Default, pathToBuiltProject);
        }

        /// <summary>
        /// Disables the exporter Assembly after creating the package.
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        public static void DisableExporter()
        {
            string modToolDirectory = AssetUtility.GetModToolDirectory();
            string exporterPath = Path.Combine(modToolDirectory, Path.Combine("Editor", "ModTool.Exporting.Editor.dll"));
            SetPluginEnabled(exporterPath, false);
        }

        private static void CreateExporter(ExportPackageOptions exportPackageOptions, string path)
        {
            LogUtility.LogInfo("Creating Exporter");

            UpdateSettings();

            ModToolSettings modToolSettings = ModToolSettings.instance;
            CodeSettings codeSettings = CodeSettings.instance;

            string modToolDirectory = AssetUtility.GetModToolDirectory();
            string exporterPath = Path.Combine(modToolDirectory, Path.Combine("Editor", "ModTool.Exporting.Editor.dll"));
            string fileName = Path.Combine(path, Application.productName + " Mod Tools.unitypackage");

            List<string> assetPaths = new List<string>
            {
                AssetDatabase.GetAssetPath(modToolSettings),
                AssetDatabase.GetAssetPath(codeSettings),
                Path.Combine(modToolDirectory, Path.Combine("Editor", "ModTool.Exporting.Editor.dll")),
                Path.Combine(modToolDirectory, Path.Combine("Editor", "ModTool.Shared.Editor.dll")),
                Path.Combine(modToolDirectory, "ModTool.Shared.dll"),
                Path.Combine(modToolDirectory, "ModTool.Shared.xml"),
                Path.Combine(modToolDirectory, "ModTool.Interface.dll"),
                Path.Combine(modToolDirectory, "ModTool.Interface.xml"),
                Path.Combine(modToolDirectory, Path.Combine("Mono.Cecil", "Mono.Cecil.dll")),
                Path.Combine(modToolDirectory, Path.Combine("Mono.Cecil", "LICENSE.txt"))
            };

            List<string> assemblyPaths = GetApiAssemblyPaths(CodeSettings.apiAssemblies);
            AssetUtility.MoveAssets(assemblyPaths, modToolDirectory);
            assetPaths.AddRange(assemblyPaths);
            
            SetPluginEnabled(exporterPath, true);

            //TODO: ExportPackageOptions.IncludeLibraryAssets makes the package huge in Unity 2017.2
            AssetDatabase.ExportPackage(assetPaths.ToArray(), fileName, exportPackageOptions | ExportPackageOptions.IncludeLibraryAssets);
        }

        private static void SetPluginEnabled(string pluginPath, bool enabled)
        {
            PluginImporter pluginImporter = AssetImporter.GetAtPath(pluginPath) as PluginImporter;

            if (pluginImporter.GetCompatibleWithEditor() == enabled)
                return;

            pluginImporter.SetCompatibleWithEditor(enabled);
            pluginImporter.SaveAndReimport();
        }

        private static List<string> GetApiAssemblyPaths(List<string> apiAssemblies)
        {
            List<string> assemblyPaths = AssemblyUtility.GetAssemblies(Application.dataPath, AssemblyFilter.ApiAssemblies);

            for(int i = 0; i < assemblyPaths.Count; i++)            
                assemblyPaths[i] = AssetUtility.GetRelativePath(assemblyPaths[i]);
            
            return assemblyPaths;
        }   
        
        private static void UpdateSettings()
        {
            if (string.IsNullOrEmpty(ModToolSettings.productName) || ModToolSettings.productName != Application.productName)
                typeof(ModToolSettings).GetField("_productName", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ModToolSettings.instance, Application.productName);

            if (string.IsNullOrEmpty(ModToolSettings.unityVersion) || ModToolSettings.unityVersion != Application.unityVersion)            
                typeof(ModToolSettings).GetField("_unityVersion", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ModToolSettings.instance, Application.unityVersion);

            EditorUtility.SetDirty(ModToolSettings.instance);
        }
    }
}
