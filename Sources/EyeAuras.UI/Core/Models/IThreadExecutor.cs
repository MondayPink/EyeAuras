using System;
using System.Reactive;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.Models
{
    internal interface IThreadExecutor : IDisposableReactiveObject
    {
        bool IsActive { get; set; }
        TimeSpan WhileActivePeriod { get; set; }
        IObservable<Unit> WhenActivated { get; }
        IObservable<Unit> WhileActive { get; }
        IObservable<Unit> WhenDeactivated { get; }
    }
}