using System.Windows.Input;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared
{
    public interface IAuraViewModel : IDisposableReactiveObject
    {
        string Id { [NotNull] get; }
        
        string TabName { [NotNull] get; [NotNull] set; }
        
        string Path { [CanBeNull] get; [CanBeNull] set; }
        
        string FullPath { [NotNull] get; }
        
        bool IsSelected { get; set; }
        
        bool IsActive { get; }
        
        bool IsEnabled { get; }
        
        IAuraModel Model { [NotNull] get; }
        
        ICommand EnableCommand { [NotNull] get; }
        
        ICommand DisableCommand { [NotNull] get; }
    }
}