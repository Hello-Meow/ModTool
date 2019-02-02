using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using ModTool.Shared;
using ModTool.Shared.Verification;
using ModTool.Shared.Editor;
using Mono.Cecil;
using System.Text.RegularExpressions;

namespace ModTool.Exporting.Editor
{
    public abstract class ExportStep
    {
        protected static readonly string assetsDirectory = "Assets";
        protected static readonly string modToolDirectory = AssetUtility.GetModToolDirectory();
        protected static readonly string assemblyDirectory = Path.Combine("Library", "ScriptAssemblies");
        protected static readonly string tempAssemblyDirectory = Path.Combine("Temp", "ScriptAssemblies");
        protected static readonly string dllPath = Path.Combine(modToolDirectory, "ModTool.Interface.dll");

        protected static readonly string[] scriptAssemblies =
        {
            "Assembly-CSharp.dll",
            "Assembly-Csharp-firstpass.dll",
            "Assembly-UnityScript.dll",
            "Assembly-UnityScript-firstpass.dll"
            //"Assembly-Boo.dll",
            //"Assembly-Boo-firstpass.dll"
        };
        
        public bool waitForAssemblyReload { get; private set; }

        public abstract string message { get; }

        internal abstract void Execute(ExportSettings settings, ExportData data);

        protected void ForceAssemblyReload()
        {
            waitForAssemblyReload = true;
            AssetDatabase.ImportAsset(dllPath, ImportAssetOptions.ForceUpdate);
        }        
    }

    public class StartExport : ExportStep
    {
        public override string message { get { return "Starting Export"; } }

        internal override void Execute(ExportSettings settings, ExportData data)
        {
            data.loadedScene = SceneManager.GetActiveScene().path;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())            
                throw new Exception("Cancelled by user");

            data.prefix = settings.name + "-";
                        
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    public class Verify : ExportStep
    {
        public override string message { get { return "Verifying Project"; } }

        internal override void Execute(ExportSettings settings, ExportData data)
        {
            CheckSerializationMode();
            VerifyProject();
            VerifySettings(settings);
        }

        private void CheckSerializationMode()
        {
            if (EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                LogUtility.LogInfo("Changed serialization mode from " + EditorSettings.serializationMode + " to Force Text");
                EditorSettings.serializationMode = SerializationMode.ForceText;
            }
        }

        private void VerifyProject()
        {
            if (!string.IsNullOrEmpty(ModToolSettings.unityVersion) && Application.unityVersion != ModToolSettings.unityVersion)
                throw new Exception("Mods for " + ModToolSettings.productName + " can only be exported with Unity " + ModToolSettings.unityVersion);
            
            if (Application.isPlaying)
                throw new Exception("Unable to export mod in play mode");

            if (!VerifyAssemblies())
                throw new Exception("Incompatible scripts or assemblies found");           
        }

        private void VerifySettings(ExportSettings settings)
        {
            if (string.IsNullOrEmpty(settings.name))
                throw new Exception("Mod has no name");            

            if (string.IsNullOrEmpty(settings.outputDirectory))            
                throw new Exception("No output directory set");             

            if (!Directory.Exists(settings.outputDirectory))            
                throw new Exception("Output directory " + settings.outputDirectory + " does not exist");
            
            if (settings.platforms == 0)            
                throw new Exception("No platforms selected");            

            if (settings.content == 0)            
                throw new Exception("No content selected");
        }

        private static bool VerifyAssemblies()
        {
            List<string> assemblies = AssemblyUtility.GetAssemblies(assetsDirectory, AssemblyFilter.ModAssemblies);

            foreach (string scriptAssembly in scriptAssemblies)
            {
                string scriptAssemblyFile = Path.Combine(assemblyDirectory, scriptAssembly);

                if (File.Exists(scriptAssemblyFile))                
                    assemblies.Add(scriptAssemblyFile);                
            }

            return AssemblyVerifier.VerifyAssemblies(assemblies);
        }

        [MenuItem("Tools/ModTool/Verify")]
        public static void VerifyScriptsMenuItem()
        {
            if (VerifyAssemblies())
                LogUtility.LogInfo("Scripts Verified!");
            else
                LogUtility.LogWarning("Scripts Not verified!");
        }
    }

    public class GetContent : ExportStep
    {
        public override string message { get { return "Finding Content"; } }

        internal override void Execute(ExportSettings settings, ExportData data)
        {            
            data.assemblies = GetAssemblies();
            data.assets = GetAssets("t:prefab t:scriptableobject");
            data.scenes = GetAssets("t:scene");
            data.scripts = GetAssets("t:monoscript");
            
            ModContent content = settings.content;

            if (data.assets.Count == 0)
                content &= ~ModContent.Assets;
            if (data.scenes.Count == 0)
                content &= ~ModContent.Scenes;
            if (data.assemblies.Count == 0 && data.scripts.Count == 0)
                content &= ~ModContent.Code;
            
            data.content = content;
        }

        private List<Asset> GetAssets(string filter)
        {
            List<Asset> assets = new List<Asset>();

            foreach (string path in AssetUtility.GetAssets(filter))
                assets.Add(new Asset(path));
            
            return assets;
        }

        private List<Asset> GetAssemblies()
        {            
            List<Asset> assemblies = new List<Asset>();

            foreach (string path in AssemblyUtility.GetAssemblies(assetsDirectory, AssemblyFilter.ModAssemblies))
            {
                Asset assembly = new Asset(path);
                assembly.Move(modToolDirectory);
                assemblies.Add(assembly);
            }                        

            return assemblies;
        }          
    }

    public class CreateBackup : ExportStep
    {
        public override string message { get { return "Creating Backup"; } }

        internal override void Execute(ExportSettings settings, ExportData data)
        {
            AssetDatabase.SaveAssets();

            if (Directory.Exists(Asset.backupDirectory))
                Directory.Delete(Asset.backupDirectory, true);

            Directory.CreateDirectory(Asset.backupDirectory);

            if (Directory.Exists(tempAssemblyDirectory))
                Directory.Delete(tempAssemblyDirectory, true);
            
            Directory.CreateDirectory(tempAssemblyDirectory);
            
            foreach (Asset asset in data.assets)
                asset.Backup();

            foreach (Asset scene in data.scenes)
                scene.Backup();

            foreach (Asset script in data.scripts)
                script.Backup();

            foreach (string path in Directory.GetFiles(assemblyDirectory))
                File.Copy(path, Path.Combine(tempAssemblyDirectory, Path.GetFileName(path)));
        }        
    }
    
    public class ImportScripts : ExportStep
    {
        public override string message { get { return "Importing Script Assemblies"; } }

        internal override void Execute(ExportSettings settings, ExportData data)
        {
            if((data.content & ModContent.Code) != ModContent.Code)
                return;

            foreach (Asset script in data.scripts)
                script.Delete();

            string prefix = data.prefix.Replace(" ", "");

            if (!string.IsNullOrEmpty(settings.version))
                prefix += settings.version.Replace(" ", "") + "-";

            List<string> searchDirectories = GetSearchDirectories();

            foreach (string scriptAssembly in scriptAssemblies)
            {
                string scriptAssemblyPath = Path.Combine(tempAssemblyDirectory, scriptAssembly);

                if (!File.Exists(scriptAssemblyPath))
                    continue;

                AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(scriptAssemblyPath);
                AssemblyNameDefinition assemblyName = assembly.Name;

                DefaultAssemblyResolver resolver = (DefaultAssemblyResolver)assembly.MainModule.AssemblyResolver;

                foreach (string searchDirectory in searchDirectories)
                    resolver.AddSearchDirectory(searchDirectory);

                assemblyName.Name = prefix + assemblyName.Name;

                foreach (var reference in assembly.MainModule.AssemblyReferences)
                {
                    if (reference.Name.Contains("firstpass"))
                        reference.Name = prefix + reference.Name;
                }
                                
                scriptAssemblyPath = Path.Combine(modToolDirectory, assemblyName.Name + ".dll");                

                assembly.Write(scriptAssemblyPath);

                data.scriptAssemblies.Add(new Asset(scriptAssemblyPath));
            }

            if (data.scriptAssemblies.Count > 0)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
                ForceAssemblyReload();
            }            
        }

        private static List<string> GetSearchDirectories()
        {
            List<string> searchDirectories = new List<string>()
            {
                Path.GetDirectoryName(typeof(UnityEngine.Object).Assembly.Location),
                assetsDirectory
            };

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.GetName().Name == "netstandard")
                    searchDirectories.Add(Path.GetDirectoryName(a.Location));
            }

            return searchDirectories;
        }
    }

    public class UpdateAssets : ExportStep
    {
        public override string message { get { return "Updating Assets"; } }

        internal override void Execute(ExportSettings settings, ExportData data)
        {
            var allAssets = data.assets.Concat(data.scenes);
            UpdateReferences(allAssets, data.scriptAssemblies);
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);

            if ((data.content & ModContent.Assets) == ModContent.Assets)
            {
                foreach (Asset asset in data.assets)
                    asset.SetAssetBundle(settings.name, "assets");
            }

            if ((data.content & ModContent.Scenes) == ModContent.Scenes)
            {
                foreach (Asset scene in data.scenes)
                {
                    scene.name = data.prefix + scene.name;
                    scene.SetAssetBundle(settings.name, "scenes");
                }
            }
        }

        private static void UpdateReferences(IEnumerable<Asset> assets, IEnumerable<Asset> scriptAssemblies)
        {
            foreach(Asset scriptAssembly in scriptAssemblies)
                UpdateReferences(assets, scriptAssembly);            
        }

        private static void UpdateReferences(IEnumerable<Asset> assets, Asset scriptAssembly)
        {
            string assemblyGuid = AssetDatabase.AssetPathToGUID(scriptAssembly.assetPath);
            ModuleDefinition module = ModuleDefinition.ReadModule(scriptAssembly.assetPath);
            
            foreach (Asset asset in assets)
                UpdateReferences(asset, assemblyGuid, module.Types);
        }

        private static void UpdateReferences(Asset asset, string assemblyGuid, IEnumerable<TypeDefinition> types)
        {
            string[] lines = File.ReadAllLines(asset.assetPath);

            for (int i = 0; i < lines.Length; i++)
            {
                //Note: Line references script file - 11500000 is Unity's YAML class ID for MonoScript
                if (lines[i].Contains("11500000"))
                    lines[i] = UpdateReference(lines[i], assemblyGuid, types);                
            }

            File.WriteAllLines(asset.assetPath, lines);
        }

        private static string UpdateReference(string line, string assemblyGuid, IEnumerable<TypeDefinition> types)
        {
            string guid = GetGuid(line);
            string scriptPath = AssetDatabase.GUIDToAssetPath(guid);
            string scriptName = Path.GetFileNameWithoutExtension(scriptPath);

            foreach (TypeDefinition type in types)
            {
                //script's type found, replace reference
                if (type.Name == scriptName)
                {
                    string fileID = GetTypeID(type.Namespace, type.Name).ToString();
                    line = line.Replace("11500000", fileID);
                    return line.Replace(guid, assemblyGuid);
                }
            }

            return line;
        }

        private static string GetGuid(string line)
        {
            string[] properties = Regex.Split(line, ", ");

            foreach (string property in properties)
            {
                if (property.Contains("guid: "))
                    return property.Remove(0, 6);
            }

            return "";
        }
        
        private static int GetTypeID(TypeDefinition type)
        {
            return GetTypeID(type.Namespace, type.Name);
        }

        private static int GetTypeID(string nameSpace, string typeName)
        {
            string toBeHashed = "s\0\0\0" + nameSpace + typeName;

            using (MD4 hash = new MD4())
            {
                byte[] hashed = hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(toBeHashed));

                int result = 0;

                for (int i = 3; i >= 0; --i)
                {
                    result <<= 8;
                    result |= hashed[i];
                }

                return result;
            }
        }    
    }

    public class Export : ExportStep
    {
        public override string message { get { return "Exporting Files"; } }

        private string tempModDirectory;
        private string modDirectory;

        internal override void Execute(ExportSettings settings, ExportData data)
        {
            tempModDirectory = Path.Combine("Temp", settings.name);
            modDirectory = Path.Combine(settings.outputDirectory, settings.name);

            if (Directory.Exists(tempModDirectory))
                Directory.Delete(tempModDirectory, true);

            Directory.CreateDirectory(tempModDirectory);

            foreach (Asset assembly in data.assemblies)
                assembly.Copy(tempModDirectory);

            foreach (Asset assembly in data.scriptAssemblies)
                assembly.Copy(tempModDirectory);

            ModPlatform platforms = settings.platforms;

            BuildAssetBundles(platforms);

            ModInfo modInfo = new ModInfo(
                settings.name,
                settings.author,
                settings.description,
                settings.version,
                Application.unityVersion,
                platforms,
                data.content);

            ModInfo.Save(Path.Combine(tempModDirectory, settings.name + ".info"), modInfo);

            CopyToOutput();

            if (data.scriptAssemblies.Count > 0)
                ForceAssemblyReload();
        }

        private void BuildAssetBundles(ModPlatform platforms)
        {
            List<BuildTarget> buildTargets = platforms.GetBuildTargets();

            foreach (BuildTarget buildTarget in buildTargets)
            {
                string platformSubdirectory = Path.Combine(tempModDirectory, buildTarget.GetModPlatform().ToString());
                Directory.CreateDirectory(platformSubdirectory);
                BuildPipeline.BuildAssetBundles(platformSubdirectory, BuildAssetBundleOptions.None, buildTarget);
            }            
        }

        private void CopyToOutput()
        {
            try
            {
                if (Directory.Exists(modDirectory))
                    Directory.Delete(modDirectory, true);

                CopyAll(tempModDirectory, modDirectory);

                LogUtility.LogInfo("Export complete");
            }
            catch (Exception e)
            {
                LogUtility.LogWarning("There was an issue while copying the mod to the output folder. " + e.Message);
            }
        }

        private static void CopyAll(string sourceDirectory, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);

            foreach (string file in Directory.GetFiles(sourceDirectory))
            {
                string fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine(targetDirectory, fileName), true);
            }

            foreach (string subDirectory in Directory.GetDirectories(sourceDirectory))
            {
                string targetSubDirectory = Path.Combine(targetDirectory, Path.GetFileName(subDirectory));
                CopyAll(subDirectory, targetSubDirectory);
            }
        }
    }
    
    public class RestoreProject : ExportStep
    {
        public override string message { get { return "Restoring Project"; } }

        internal override void Execute(ExportSettings settings, ExportData data)
        {
            foreach (Asset scriptAssembly in data.scriptAssemblies)
                scriptAssembly.Delete();

            foreach (Asset asset in data.assets)
                asset.Restore();

            foreach (Asset scene in data.scenes)
                scene.Restore();

            foreach (Asset script in data.scripts)
                script.Restore();

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            
            if(!string.IsNullOrEmpty(data.loadedScene))
                EditorSceneManager.OpenScene(data.loadedScene);
        }
    }     
}
