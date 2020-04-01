using EyeAuras.OnTopReplica;
using JetBrains.Annotations;

namespace EyeAuras.Shared.Services
{
    public interface IWindowMatcher
    {
        bool IsMatch([NotNull] WindowHandle window, WindowMatchParams matchParams);
    }
}