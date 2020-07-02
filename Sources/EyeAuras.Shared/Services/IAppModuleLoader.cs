using System.IO;
using JetBrains.Annotations;

namespace EyeAuras.Shared.Services
{
    public interface IAppModuleLoader
    {
        void LoadModules();

        [NotNull]
        DirectoryInfo UnzipCompressedModule(string moduleName, [NotNull] byte[] zipBytes);
        
        void LoadModulesFromBytes([NotNull] byte[] assemblyBytes, FileInfo moduleRef);
    }
}