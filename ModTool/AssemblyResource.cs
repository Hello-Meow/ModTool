using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using UnityEngine;
using ModTool.Shared;
using ModTool.Shared.Verification;

namespace ModTool
{
    internal class AssemblyResource : Resource<AssemblyResource>
    {
        public IReadOnlyList<string> assemblyNames { get; private set; }

        public IReadOnlyList<Assembly> assemblies { get; private set; }

        public override bool canLoad => errors.Count == 0;

        private List<string> assemblyPaths;

        private List<Assembly> _assemblies;

        public AssemblyResource(string name, string path) : base(name)
        {
            assemblyPaths = AssemblyUtility.GetAssemblies(path);

            if (assemblyPaths.Count == 0)
                AddError(name + " - Could not find assemblies.");

            var assemblyNames = new List<string>();

            foreach (var assemblyPath in assemblyPaths)
                assemblyNames.Add(Path.GetFileNameWithoutExtension(assemblyPath));

            this.assemblyNames = assemblyNames.AsReadOnly();

            _assemblies = new List<Assembly>();
            assemblies = _assemblies.AsReadOnly();

            VerifyAssemblies();
        }

        private void VerifyAssemblies()
        {
            AssemblyResolver assemblyResolver;

            if (Application.platform == RuntimePlatform.Android)
                assemblyResolver = new AndroidAssemblyResolver();
            else
                assemblyResolver = new AssemblyResolver();
            
            var messages = new List<string>();

            using (var assemblyVerifier = new AssemblyVerifier(assemblyResolver))
                assemblyVerifier.VerifyAssemblies(assemblyPaths, messages);

            foreach (var message in messages)
                AddError(message);
        }

        private async Task<List<Assembly>> LoadAssemblies()
        {
            var assemblies = new List<Assembly>();

            try
            {
                foreach (var path in assemblyPaths)
                    assemblies.Add(await LoadAssembly(path));

                //Note: catch TypeLoadExceptions 
                foreach (var assembly in assemblies)
                    assembly.GetTypes();
            }
            catch(Exception e)
            {
                LogUtility.LogException(e);               
            }

            return assemblies;
        }

        private async Task<Assembly> LoadAssembly(string path)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(path);

            var assembly = await Task.Run(() => SearchAppDomain(assemblyName));

            if (assembly != null)
                return assembly;

            return Assembly.Load(File.ReadAllBytes(path));
        }

        private Assembly SearchAppDomain(string assemblyName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == assemblyName)
                    return assembly;
            }

            return null;
        }

        protected override IEnumerator LoadResources()
        {
            var task = LoadAssemblies();

            while (!task.IsCompleted)
                yield return null;

            _assemblies.AddRange(task.Result);         
        }

        protected override IEnumerator UnloadResources()
        {
            _assemblies.Clear();

            yield break;
        }
    }
}
