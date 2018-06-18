using System.Collections.Generic;
using Mono.Cecil;
using UnityEngine;
using System.IO;

namespace ModTool.Shared.Verification
{
    /// <summary>
    /// A class for verifying Assembly files based on a number of Restrictions.
    /// </summary>
    public class AssemblyVerifier
    {
        private static string persistentDataPath;
        private static RuntimePlatform platform;
        private static bool initialized;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            //Note: These can only be called on the main thread.
            persistentDataPath = Application.persistentDataPath;
            platform = Application.platform;
            
            initialized = true;
        }
        
        /// <summary>
        /// Verify a list of Assembly files. 
        /// </summary>
        /// <param name="assemblies">A collection of paths to Assemblies that will be verified.</param>
        /// <returns>False if an assembly has failed.</returns>
        public static bool VerifyAssemblies(IEnumerable<string> assemblies)
        {
            if (!initialized)
                Initialize();

            foreach (var path in assemblies)
            {                
                if (!VerifyAssembly(path))
                    return false;
            }

            return true;
        }

        private static bool VerifyAssembly(string path)
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path);

            foreach (ModuleDefinition module in assembly.Modules)
            {
                DefaultAssemblyResolver resolver = (DefaultAssemblyResolver)module.AssemblyResolver;

                resolver.AddSearchDirectory(Path.GetDirectoryName(path));

                AddSearchDirectories(resolver);

                if (!VerifyModule(module))
                    return false;
            }

            return true;
        }

        private static bool VerifyModule(ModuleDefinition module)
        {            
            foreach (TypeDefinition type in module.Types)
            {
                if (!VerifyType(type))
                    return false;
            }

            return true;
        }

        private static void AddSearchDirectories(BaseAssemblyResolver resolver)
        {
            //Add search directories based on platform
            if (platform == RuntimePlatform.WindowsEditor || platform == RuntimePlatform.OSXEditor)
            {
                resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(UnityEngine.Object).Assembly.Location));
                resolver.AddSearchDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Assets"));

                foreach (string directory in Directory.GetDirectories(Directory.GetCurrentDirectory(), "ModTool", SearchOption.AllDirectories))
                    resolver.AddSearchDirectory(directory);
                
                resolver.AddSearchDirectory(Directory.GetCurrentDirectory());
            }

            if (platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.LinuxPlayer || platform == RuntimePlatform.OSXPlayer)
            {
                foreach (string directory in Directory.GetDirectories(Directory.GetCurrentDirectory(), "Managed", SearchOption.AllDirectories))
                    resolver.AddSearchDirectory(directory);
                
                resolver.AddSearchDirectory(Directory.GetCurrentDirectory());  
            }

            //android - extracted assemblies from apk in persistentDatapath/Assemblies
            if (platform == RuntimePlatform.Android)
                resolver.AddSearchDirectory(Path.Combine(persistentDataPath, "Assemblies"));
        }
        
        private static bool VerifyType(TypeDefinition type)
        {
            foreach (var restriction in CodeSettings.inheritanceRestrictions)
                if (!restriction.Verify(type, CodeSettings.apiAssemblies))
                    return false;

            foreach (var member in type.Fields)
            {
                foreach (var restriction in CodeSettings.namespaceRestrictions)
                    if (!restriction.Verify(member, CodeSettings.apiAssemblies))
                        return false;

                foreach (var restriction in CodeSettings.typeRestrictions)
                    if (!restriction.Verify(member, CodeSettings.apiAssemblies))
                        return false;

                foreach (var restriction in CodeSettings.memberRestrictions)
                    if (!restriction.Verify(member, CodeSettings.apiAssemblies))
                        return false;
            }

            foreach (var member in type.Properties)
            {
                foreach (var restriction in CodeSettings.namespaceRestrictions)
                    if (!restriction.Verify(member, CodeSettings.apiAssemblies))
                        return false;

                foreach (var restriction in CodeSettings.typeRestrictions)
                    if (!restriction.Verify(member, CodeSettings.apiAssemblies))
                        return false;

                foreach (var restriction in CodeSettings.memberRestrictions)
                    if (!restriction.Verify(member, CodeSettings.apiAssemblies))
                        return false;
            }

            foreach (var member in type.Methods)
            {
                foreach (var restriction in CodeSettings.namespaceRestrictions)
                    if (!restriction.Verify(member, CodeSettings.apiAssemblies))
                        return false;

                foreach (var restriction in CodeSettings.typeRestrictions)
                    if (!restriction.Verify(member, CodeSettings.apiAssemblies))
                        return false;

                foreach (var restriction in CodeSettings.memberRestrictions)
                    if (!restriction.Verify(member, CodeSettings.apiAssemblies))
                        return false;
            }

            foreach(var nested in type.NestedTypes)
            {
                if(!VerifyType(nested))
                    return false;                
            }

            return true;
        } 
    }
}
