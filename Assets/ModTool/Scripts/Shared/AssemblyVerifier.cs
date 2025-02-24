using System.Collections.Generic;
using Mono.Cecil;
using System;

namespace ModTool.Shared.Verification
{
    /// <summary>
    /// A class for verifying Assembly files based on a number of Restrictions.
    /// </summary>
    public class AssemblyVerifier : IDisposable
    {
        private IAssemblyResolver assemblyResolver;

        private ReaderParameters parameters;

        public AssemblyVerifier()
        {
            assemblyResolver = new AssemblyResolver();

            Initialize();
        }
        
        /// <summary>
        /// Initialize an AssemblyVerifier with a specified IAssemblyResolver.
        /// </summary>
        /// <param name="assemblyResolver"></param>
        public AssemblyVerifier(IAssemblyResolver assemblyResolver)
        {
            this.assemblyResolver = assemblyResolver;

            Initialize();
        }

        private void Initialize()
        {
            parameters = new ReaderParameters()
            {
                AssemblyResolver = assemblyResolver,
                InMemory = true
            };
        }

        /// <summary>
        /// Verify a collection of assemblies.
        /// </summary>
        /// <param name="assemblies">A list of assembly file paths.</param>
        /// <param name="messages">List of messages from failed restrictions.</param>
        public void VerifyAssemblies(IEnumerable<string> assemblies, List<string> messages)
        {
            foreach (var path in assemblies)
                VerifyAssembly(path, messages);
        }

        /// <summary>
        /// Verify an assembly.
        /// </summary>
        /// <param name="path">The file path of the assembly.</param>
        /// <param name="messages">List of messages from failed restrictions.</param>
        public void VerifyAssembly(string path, List<string> messages)
        {
            try
            {
                using (var assembly = AssemblyDefinition.ReadAssembly(path, parameters))
                    VerifyModule(assembly.MainModule, messages);
            }
            catch (Exception e)
            {
                messages.Add(e.ToString());
            }
        }

        public void Dispose()
        {
            assemblyResolver.Dispose();
        }

        private static void VerifyModule(ModuleDefinition module, List<string> messages)
        {
            foreach (var type in module.Types)
                VerifyType(type, messages);
        }

        private static void VerifyType(TypeDefinition type, List<string> messages)
        {
            foreach (var restriction in CodeSettings.inheritanceRestrictions)
                restriction.Verify(type, messages);

            foreach (var member in type.Fields)
                VerifyMember(member, messages);

            foreach (var member in type.Properties)
                VerifyMember(member, messages);

            foreach (var member in type.Methods)
                VerifyMember(member, messages);

            foreach (var nested in type.NestedTypes)
                VerifyType(nested, messages);
        }

        private static void VerifyMember(MemberReference member, List<string> messages)
        {
            foreach (var restriction in CodeSettings.namespaceRestrictions)
                restriction.Verify(member, messages);

            foreach (var restriction in CodeSettings.typeRestrictions)
                restriction.Verify(member, messages);

            foreach (var restriction in CodeSettings.memberRestrictions)
                restriction.Verify(member, messages);
        }
    }
}
