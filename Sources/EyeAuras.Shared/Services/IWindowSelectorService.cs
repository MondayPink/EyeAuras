using EyeAuras.OnTopReplica;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared.Services
{
    public interface IWindowSelectorService : IDisposableReactiveObject
    {
        IWindowHandle ActiveWindow { [CanBeNull] get; [CanBeNull] set; }
        
        IWindowHandle[] MatchingWindowList { [NotNull] get; }
        
        WindowMatchParams TargetWindow { get; set; }
    }
}