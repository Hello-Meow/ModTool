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
        /// Verify a list of assemblies.
        /// </summary>
        /// <param name="assemblies">A list of paths of assemblies.</param>
        /// <param name="messages">A list of messages of failed Restrictions.</param>
        public static void VerifyAssemblies(IEnumerable<string> assemblies, List<string> messages)
        {
            if (!initialized)
                Initialize();

            foreach (var path in assemblies)
                VerifyAssembly(path, messages);
        }        

        private static void VerifyAssembly(string path, List<string> messages)
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path);
            
            foreach(var module in assembly.Modules)
            {
                DefaultAssemblyResolver resolver = (DefaultAssemblyResolver)module.AssemblyResolver;

                resolver.AddSearchDirectory(Path.GetDirectoryName(path));

                AddSearchDirectories(resolver);

                VerifyModule(module, messages);
            }
        }                

        private static void VerifyModule(ModuleDefinition module, List<string> messages)
        {
            foreach (var type in module.Types)
                VerifyType(type, messages);
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

        private static void VerifyType(TypeDefinition type, List<string> messages)
        {
            foreach (var restriction in CodeSettings.inheritanceRestrictions)
                restriction.Verify(type, messages);                

            foreach (var member in type.Fields)
            {
                foreach (var restriction in CodeSettings.namespaceRestrictions)
                    restriction.Verify(member, messages);

                foreach (var restriction in CodeSettings.typeRestrictions)
                    restriction.Verify(member, messages);

                foreach (var restriction in CodeSettings.memberRestrictions)
                    restriction.Verify(member, messages);
            }

            foreach (var member in type.Properties)
            {
                foreach (var restriction in CodeSettings.namespaceRestrictions)
                    restriction.Verify(member, messages);

                foreach (var restriction in CodeSettings.typeRestrictions)
                    restriction.Verify(member, messages);

                foreach (var restriction in CodeSettings.memberRestrictions)
                    restriction.Verify(member, messages);
            }

            foreach (var member in type.Methods)
            {
                foreach (var restriction in CodeSettings.namespaceRestrictions)
                    restriction.Verify(member, messages);

                foreach (var restriction in CodeSettings.typeRestrictions)
                    restriction.Verify(member, messages);

                foreach (var restriction in CodeSettings.memberRestrictions)
                    restriction.Verify(member, messages);
            }

            foreach (var nested in type.NestedTypes)            
                VerifyType(nested, messages);
        }        
    }
}
