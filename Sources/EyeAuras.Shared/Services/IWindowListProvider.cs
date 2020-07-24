using System;
using System.Collections.ObjectModel;
using EyeAuras.OnTopReplica;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace EyeAuras.Shared.Services
{
    public interface IWindowListProvider : IDisposableReactiveObject
    {
        [CanBeNull]
        IWindowHandle ResolveByHandle(IntPtr hWnd);
        
        ReadOnlyObservableCollection<IWindowHandle> WindowList { [NotNull] get; }
    }
}