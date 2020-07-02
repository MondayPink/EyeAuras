using System;
using System.IO;
using System.Linq;
using EyeAuras.Shared.Services;
using PoeShared.Scaffolding;
using Prism.Modularity;

namespace EyeAuras.UI.Core.Services
{
    internal sealed class AppModulePathResolver : IAppModulePathResolver
    {
        private readonly IModuleCatalog moduleCatalog;

        public AppModulePathResolver(IModuleCatalog moduleCatalog)
        {
            this.moduleCatalog = moduleCatalog;
        }

        public DirectoryInfo ResolvePath<T>() where T : IModule
        {
            var moduleType = typeof(T).AssemblyQualifiedName;
            var result = moduleCatalog.Modules.FirstOrDefault(x => x.ModuleType == moduleType);
            if (result == null)
            {
                throw new ApplicationException($"Failed to find module of type {moduleType} in module catalog: {moduleCatalog.Modules.Select(x => new { x.ModuleName, x.ModuleType, x.Ref }).DumpToTextRaw()}");
            }

            var moduleDirectoryPath = Path.GetDirectoryName(result.Ref);
            if (moduleDirectoryPath == null || !Directory.Exists(moduleDirectoryPath))
            {
                throw new ApplicationException($"Failed to resolve path for module of type {moduleType}: {result.DumpToTextRaw()}");
            }
            
            return new DirectoryInfo(moduleDirectoryPath);
        }
    }
}