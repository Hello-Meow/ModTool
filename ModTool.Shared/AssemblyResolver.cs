using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace ModTool.Shared
{
    public class AssemblyResolver : DefaultAssemblyResolver
    {
        private Dictionary<string, AssemblyDefinition> cache;

        private ReaderParameters parameters;

        public AssemblyResolver()
        {
            cache = new Dictionary<string, AssemblyDefinition>();

            parameters = new ReaderParameters()
            {
                AssemblyResolver = this,
                InMemory = true
            };
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            AssemblyDefinition assemblyDefinition;

            if (cache.TryGetValue(name.Name, out assemblyDefinition))
                return assemblyDefinition;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if(assembly.GetName().Name == name.Name)
                {
                    string location = assembly.Location;

                    assemblyDefinition = GetAssembly(location, parameters);

                    cache.Add(name.Name, assemblyDefinition);

                    return assemblyDefinition;
                }
            }

            return base.Resolve(name);
        }

        protected virtual AssemblyDefinition GetAssembly(string location, ReaderParameters parameters)
        {
            return ModuleDefinition.ReadModule(location, parameters).Assembly;
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var assembly in cache.Values)
                assembly.Dispose();

            cache.Clear();

            base.Dispose(disposing);
        }
    }
}
