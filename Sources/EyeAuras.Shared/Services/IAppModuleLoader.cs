using JetBrains.Annotations;

namespace EyeAuras.Shared.Services
{
    public interface IAppModuleLoader
    {
        void LoadModulesFromBytes([NotNull] byte[] assemblyBytes);
    }
}