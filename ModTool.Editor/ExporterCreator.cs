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
            LogUtility.LogInfo("Creating Exporter");

            UpdateSettings();

            ModToolSettings modToolSettings = ModToolSettings.instance;
            CodeSettings codeSettings = CodeSettings.instance;

            string modToolDirectory = AssetUtility.GetModToolDirectory();
            string exporterPath = Path.Combine(modToolDirectory, Path.Combine("Editor", "ModTool.Exporting.Editor.dll"));
            string fileName = Path.Combine(path, Application.productName + " Mod Tools.unitypackage");
            string projectSettingsDirectory = "ProjectSettings";

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
                Path.Combine(modToolDirectory, Path.Combine("Mono.Cecil", "LICENSE.txt")),
                Path.Combine(projectSettingsDirectory, "InputManager.asset"),
                Path.Combine(projectSettingsDirectory, "TagManager.asset"),
                Path.Combine(projectSettingsDirectory, "Physics2DSettings.asset"),
                Path.Combine(projectSettingsDirectory, "DynamicsManager.asset")
            };

            SetPluginEnabled(exporterPath, true);

            List<string> assemblyPaths = GetApiAssemblies(CodeSettings.apiAssemblies);
            
            assetPaths.AddRange(assemblyPaths);

            AssetDatabase.ExportPackage(assetPaths.ToArray(), fileName);

            foreach (string assemblyPath in assemblyPaths)
                AssetDatabase.DeleteAsset(assemblyPath);

            SetPluginEnabled(exporterPath, false);

            if(revealPackage)
                EditorUtility.RevealInFinder(fileName);
        }

        private static void SetPluginEnabled(string pluginPath, bool enabled)
        {
            PluginImporter pluginImporter = AssetImporter.GetAtPath(pluginPath) as PluginImporter;

            if (pluginImporter.GetCompatibleWithEditor() == enabled)
                return;

            pluginImporter.SetCompatibleWithEditor(enabled);
            pluginImporter.SaveAndReimport();
        }

        private static List<string> GetApiAssemblies(List<string> apiAssemblies)
        {
            List<string> assemblyPaths = AssemblyUtility.GetAssemblies(Directory.GetCurrentDirectory(), AssemblyFilter.ApiAssemblies);
            
            string modToolDirectory = AssetUtility.GetModToolDirectory();

            for (int i = 0; i < assemblyPaths.Count; i++)
            {
                string path = assemblyPaths[i];
                string fileName = Path.GetFileName(path);
                string newPath = Path.Combine(modToolDirectory, fileName);

                File.Copy(path, newPath);
                AssetDatabase.ImportAsset(newPath);

                assemblyPaths[i] = newPath;
            }

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
