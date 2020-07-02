using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using log4net;

namespace EyeAuras.UI.Prism.Modularity
{
    internal sealed class MemoryAssemblyLoadContext : AssemblyLoadContext
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MemoryAssemblyLoadContext));
        
        private readonly AssemblyDependencyResolver resolver;
        
        public MemoryAssemblyLoadContext(string contextName, string assemblyPath) : base(contextName)
        {
            resolver = new AssemblyDependencyResolver(assemblyPath);
            this.Resolving += OnResolving;
        }
        
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            Log.Debug($"[{Name}] Loading assembly {assemblyName}");
            
            var defaultAssembly = AssemblyLoadContext.Default.Assemblies.Any(x => x.FullName == assemblyName.FullName);
            
            if (defaultAssembly)
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            }
            
            var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath == null)
            {
                Log.Warn($"[{Name}] Failed to resolve assembly name to assembly path {assemblyName}");
                return null;
            }
            var result = base.LoadFromAssemblyPath(assemblyPath);
            Log.Debug($"[{Name}] Loaded assembly {assemblyName}: {result?.FullName}");
            return result;
        }

        private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            Log.Debug($"[{Name}] Resolving assembly {assemblyName}");
            return null;
        }
    }
}