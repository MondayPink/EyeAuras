using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Dragablz;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Core.Utilities;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI;
using Prism.Commands;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI.Core.ViewModels
{
    internal sealed class OverlayCoreEditorViewModel : AuraPropertiesEditorBase<OverlayAuraCore>
    {
        private readonly SerialDisposable activeSourceAnchors;

        public OverlayCoreEditorViewModel(
            [NotNull] IWindowSelectorService windowSelector
            )
        {
            activeSourceAnchors = new SerialDisposable().AddTo(Anchors);
            WindowSelector = windowSelector.AddTo(Anchors);

            this.WhenAnyValue(x => x.Source)
                .Subscribe(HandleSourceChange)
                .AddTo(Anchors);
        }

        public IWindowSelectorService WindowSelector { get; }

        private void HandleSourceChange()
         {
             var sourceAnchors = new CompositeDisposable().AssignTo(activeSourceAnchors);
 
             if (Source == null)
             {
                 return;
             }
 
             Disposable.Create(() =>
             {
                 Source = null;
             }).AddTo(sourceAnchors);
             
             Source.WhenAnyValue(x => x.TargetWindow).Subscribe(x => WindowSelector.TargetWindow = x).AddTo(sourceAnchors);
             WindowSelector.WhenAnyValue(x => x.TargetWindow).Subscribe(x => Source.TargetWindow = x).AddTo(sourceAnchors);

             Source.WhenAnyValue(x => x.Overlay)
                 .Select(x => x == null ? Observable.Empty<Unit>() : x.WhenAnyValue(y => y.AttachedWindow).ToUnit())
                 .Switch()
                 .Subscribe(() => WindowSelector.ActiveWindow = Source.Overlay.AttachedWindow)
                 .AddTo(Anchors);
             
             Source.WhenAnyValue(x => x.Overlay)
                 .Select(x => x == null ? Observable.Empty<Unit>() : WindowSelector.WhenAnyValue(y => y.ActiveWindow).ToUnit())
                 .Switch()
                 .Subscribe(() => Source.Overlay.AttachedWindow = WindowSelector.ActiveWindow)
                 .AddTo(Anchors);
         }
    }
}