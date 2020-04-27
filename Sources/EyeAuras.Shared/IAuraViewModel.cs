using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared
{
    public interface IAuraViewModel : IDisposableReactiveObject
    {
        string Id { [NotNull] get; }
        
        string TabName { [NotNull] get; }
        
        bool IsSelected { get; set; }
        
        bool IsActive { get; }
        
        bool IsEnabled { get; set; }
    }
}