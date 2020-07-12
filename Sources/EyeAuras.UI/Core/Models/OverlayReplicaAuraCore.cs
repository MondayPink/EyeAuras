using System;
using System.Reactive;
using System.Reactive.Linq;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Overlay.ViewModels;
using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace EyeAuras.UI.Core.Models
{
    internal sealed class OverlayReplicaAuraCore : OverlayAuraCore<OverlayReplicaCoreProperties>
    {
        private WindowMatchParams targetWindow;

        private readonly IFactory<IReplicaOverlayViewModel, IOverlayWindowController, IAuraModelController>
            overlayViewModelFactory;

        public OverlayReplicaAuraCore(
            [NotNull] IWindowSelectorService windowSelector,
            [NotNull] IFactory<IReplicaOverlayViewModel, IOverlayWindowController, IAuraModelController> overlayViewModelFactory,
            [NotNull] IFactory<IOverlayWindowController, IWindowTracker> overlayWindowControllerFactory, 
            [NotNull] IFactory<WindowTracker, IStringMatcher> windowTrackerFactory) : base(overlayWindowControllerFactory, windowTrackerFactory)
        {
            WindowSelector = windowSelector.AddTo(Anchors);
            this.overlayViewModelFactory = overlayViewModelFactory;
            
            this.WhenAnyValue(x => x.TargetWindow).Subscribe(x => WindowSelector.TargetWindow = x).AddTo(Anchors);
            WindowSelector.WhenAnyValue(x => x.TargetWindow).Subscribe(x => this.TargetWindow = x).AddTo(Anchors);
            
            this.WhenAnyValue(x => x.Overlay)
                .OfType<IReplicaOverlayViewModel>()
                .Select(x => x == null ? Observable.Empty<Unit>() : x.WhenAnyValue(y => y.AttachedWindow).Skip(1).ToUnit())
                .Switch()
                .Select(x => this.Overlay)
                .OfType<IReplicaOverlayViewModel>()
                .Subscribe(x => WindowSelector.ActiveWindow = x.AttachedWindow)
                .AddTo(Anchors);

            Observable.Merge(
                    this.WhenAnyValue(x => x.Overlay).ToUnit(),
                    WindowSelector.WhenAnyValue(x => x.ActiveWindow).ToUnit())
                .Select(x => this.Overlay)
                .OfType<IReplicaOverlayViewModel>()
                .Subscribe(x => x.AttachedWindow = WindowSelector.ActiveWindow)
                .AddTo(Anchors);
        }
        
        public IWindowSelectorService WindowSelector { get; }

        public WindowMatchParams TargetWindow
        {
            get => targetWindow;
            set => RaiseAndSetIfChanged(ref targetWindow, value);
        }
        
        public new IReplicaOverlayViewModel Overlay
        {
            get => base.Overlay as IReplicaOverlayViewModel;
            private set => base.Overlay = value;
        }
        
        public override string Name { get; } = "Replica";
        
        public override string Description { get; } = "Show selected window Replica";
        
        protected override void VisitSave(OverlayReplicaCoreProperties properties)
        {
            base.VisitSave(properties);
            var replicaOverlay = Overlay;
            if (replicaOverlay != null)
            {
                properties.SourceRegionBounds = replicaOverlay.Region.Bounds;
            }
            properties.WindowMatch = TargetWindow;
        }

        protected override void VisitLoad(OverlayReplicaCoreProperties source)
        {
            base.VisitLoad(source);
            var replicaOverlay = Overlay;
            if (replicaOverlay != null)
            {
                replicaOverlay.Region.SetValue(source.SourceRegionBounds);
            }
            TargetWindow = source.WindowMatch;
        }

        protected override IEyeOverlayViewModel CreateOverlay(IOverlayWindowController windowController, IAuraModelController modelController)
        {
            return overlayViewModelFactory.Create(windowController, modelController);
        }

        protected override void HandleInitialization()
        {
            base.HandleInitialization();
            Observable.Merge(
                    this.WhenAnyProperty(x => x.TargetWindow).Select(x => $"[{Context?.Name}].{x.EventArgs.PropertyName} property changed"))
                .Subscribe(reason => RaisePropertyChanged(nameof(Properties)))
                .AddTo(Anchors);
        }
    }
}