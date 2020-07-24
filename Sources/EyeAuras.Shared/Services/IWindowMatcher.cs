using EyeAuras.OnTopReplica;
using JetBrains.Annotations;

namespace EyeAuras.Shared.Services
{
    public interface IWindowMatcher
    {
        bool IsMatch([NotNull] IWindowHandle window, WindowMatchParams matchParams);
    }
}