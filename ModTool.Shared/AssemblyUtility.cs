using System;
using System.IO;
using System.Collections.Generic;
using Mono.Cecil;

using System.Reflection;

namespace ModTool.Shared
{ 
    /// <summary>
    /// Utility for finding Assemblies.
    /// </summary>
    public class AssemblyUtility
    {
        public static List<string> GetAssemblies(string path, Func<string, bool> filter = null)
        {
            List<string> assemblies = new List<string>();

            GetAssemblies(assemblies, path, filter);

            return assemblies;
        }

        public static void GetAssemblies(List<string> assemblies, string path, Func<string, bool> filter = null)
        {
            var assemblyFiles = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);

            foreach (var assembly in assemblyFiles)
            {
                AssemblyName assemblyName;

                try
                {
                    assemblyName = AssemblyName.GetAssemblyName(assembly);                    
                }
                catch (Exception e)
                {
                    LogUtility.LogDebug(e.Message);
                    continue;
                }

                string name = assemblyName.Name;

                if (name == "ModTool" || name.StartsWith("ModTool."))                 
                    continue;

                if (IsShared(assembly))                
                    continue;                

                if (assembly.Contains("Editor"))
                    continue;

                if(filter != null && !filter(assembly))
                    continue;

                assemblies.Add(assembly);
            }
        }

        /// <summary>
        /// Is an assembly shared with the mod exporter?
        /// </summary>
        /// <param name="path">The assembly's file path.</param>
        /// <returns>True if an assembly is shared with the mod exporter.</returns>
        public static bool IsShared(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);

            foreach (string sharedAsset in ModToolSettings.sharedAssets)
            {
                if (!sharedAsset.EndsWith(".asmdef") && !sharedAsset.EndsWith(".dll"))
                    continue;

                if (Path.GetFileNameWithoutExtension(sharedAsset) == name)
                    return true;
            }

            return false;
        }
    }    
}
