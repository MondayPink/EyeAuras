using System.IO;
using JetBrains.Annotations;
using Prism.Modularity;

namespace EyeAuras.Shared.Services
{
    public interface IAppModulePathResolver
    {
        [NotNull]
        DirectoryInfo ResolvePath<T>() where T : IModule;
    }
}