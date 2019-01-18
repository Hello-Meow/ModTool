using System;
using System.IO;
using System.Collections.Generic;
using Mono.Cecil;

namespace ModTool.Shared
{ 
    /// <summary>
    /// Filter mode for finding Assemblies.
    /// </summary>
    [Flags]
    public enum AssemblyFilter { ApiAssemblies = 1, ModToolAssemblies = 2, ModAssemblies = 4 }


    /// <summary>
    /// Utility for finding Assemblies.
    /// </summary>
    public class AssemblyUtility
    {
        /// <summary>
        /// Find dll files in a directory and its sub directories.
        /// </summary>
        /// <param name="path">The directory to search in.</param>
        /// <returns>A List of paths to found Assemblies.</returns>
        public static List<string> GetAssemblies(string path, AssemblyFilter assemblyFilter)
        {
            List<string> assemblies = new List<string>();

            GetAssemblies(assemblies, path, assemblyFilter);

            return assemblies;
        }

        public static void GetAssemblies(List<string> assemblies, string path, AssemblyFilter assemblyFilter)
        {
            var assemblyFiles = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);

            foreach (var assembly in assemblyFiles)
            {
                AssemblyDefinition assemblyDefinition;

                try
                {
                    assemblyDefinition = Mono.Cecil.AssemblyDefinition.ReadAssembly(assembly);
                }
                catch
                {
                    continue;
                }

                string name = assemblyDefinition.Name.Name;

                if (name == "ModTool" || name.StartsWith("ModTool."))
                {
                    if ((assemblyFilter & AssemblyFilter.ModToolAssemblies) != 0)
                        assemblies.Add(assembly);

                    continue;
                }
                
                if(name.Contains("Mono.Cecil"))
                {
                    if((assemblyFilter & AssemblyFilter.ModToolAssemblies) != 0)
                        assemblies.Add(assembly);

                    continue;
                }

                if(CodeSettings.apiAssemblies.Contains(name))
                {
                    if((assemblyFilter & AssemblyFilter.ApiAssemblies) != 0)
                        assemblies.Add(assembly);

                    continue;
                }

                if ((assemblyFilter & AssemblyFilter.ModAssemblies) != 0)
                    assemblies.Add(assembly);
            }
        }                 
    }    
}
