using System;
using System.Reactive.Linq;
using DynamicData.Binding;
using EyeAuras.UI.Overlay.ViewModels;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace EyeAuras.UI.Core.Models
{
    internal abstract class OverlayAuraCore<T> : AuraCore<T>,IOverlayAuraCore where T : class, IOverlayCoreProperties, new()
    {
        public OverlayPositionAdapter PositionAdapter { get; }
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayAuraCore<>));

        private readonly IFactory<IOverlayWindowController, IWindowTracker> overlayWindowControllerFactory;
        private readonly IFactory<WindowTracker, IStringMatcher> windowTrackerFactory;
        
        private IEyeOverlayViewModel overlay;

        public OverlayAuraCore(
            [NotNull] IFactory<IOverlayWindowController, IWindowTracker> overlayWindowControllerFactory,
            [NotNull] IFactory<WindowTracker, IStringMatcher> windowTrackerFactory)
        {
            PositionAdapter = new OverlayPositionAdapter();
            this.overlayWindowControllerFactory = overlayWindowControllerFactory;
            this.windowTrackerFactory = windowTrackerFactory;

            Observable.CombineLatest(this.WhenAnyValue(x => x.ModelController), this.WhenAnyValue(x => x.Context), (controller, auraContext) => new { controller, auraContext })
                .Where(x => x.controller != null && x.auraContext != null)
                .Take(1)
                .Subscribe(HandleInitialization)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.Overlay)
                .Subscribe(x => PositionAdapter.Overlay = x)
                .AddTo(Anchors);
        }
        
        public IEyeOverlayViewModel Overlay
        {
            get => overlay;
            protected set => RaiseAndSetIfChanged(ref overlay, value);
        }

        protected abstract IEyeOverlayViewModel CreateOverlay(IOverlayWindowController windowController, IAuraModelController modelController);

        public void VisitSave(IOverlayCoreProperties properties)
        {
            if (Overlay != null)
            {
                properties.IsClickThrough = Overlay.IsClickThrough;
                properties.ThumbnailOpacity = Overlay.ThumbnailOpacity;
                properties.MaintainAspectRatio = Overlay.MaintainAspectRatio;
                properties.BorderColor = Overlay.BorderColor;
                properties.BorderThickness = Overlay.BorderThickness;
            }
            PositionAdapter.VisitSave(properties);
        }

        public void VisitLoad(IOverlayCoreProperties source)
        {
            if (Overlay != null)
            {
                Overlay.ThumbnailOpacity = source.ThumbnailOpacity;
                Overlay.IsClickThrough = source.IsClickThrough;
                Overlay.MaintainAspectRatio = source.MaintainAspectRatio;
                Overlay.BorderColor = source.BorderColor;
                Overlay.BorderThickness = source.BorderThickness;
            }
            PositionAdapter.VisitLoad(source);
        }

        protected virtual void HandleInitialization()
        {
            using var sw = new BenchmarkTimer($"[{this}] {Name} initialization", Log, nameof(OverlayAuraModel));

            var matcher = new RegexStringMatcher().AddToWhitelist(".*");
            var windowTracker = windowTrackerFactory
                .Create(matcher)
                .AddTo(Anchors);
            var overlayController = overlayWindowControllerFactory
                .Create(windowTracker)
                .AddTo(Anchors);
            sw.Step($"Overlay controller created: {overlayController}");

            Overlay = CreateOverlay(overlayController, ModelController).AddTo(Anchors);
            sw.Step($"Overlay view model created: {Overlay}");
            
            Observable.Merge(
                    this.WhenAnyValue(x => x.Overlay).OfType<IReplicaOverlayViewModel>().Select(x => x.WhenAnyValue(y => y.AttachedWindow)).Switch().ToUnit(),
                    Overlay.WhenValueChanged(x => x.IsLocked, false).ToUnit(),
                    Context.WhenValueChanged(x => x.IsActive, false).ToUnit())
                .StartWithDefault()
                .Select(
                    () => new
                    {
                        OverlayShouldBeShown = Context.IsActive || !Overlay.IsLocked,
                        WindowIsAttached = !(Overlay is IReplicaOverlayViewModel replicaOverlayViewModel) || replicaOverlayViewModel.AttachedWindow != null
                    })
                .Subscribe(x => overlayController.IsEnabled = x.OverlayShouldBeShown && x.WindowIsAttached, Log.HandleUiException)
                .AddTo(Anchors);
            sw.Step($"Overlay view model initialized: {Overlay}");
            
            overlayController.RegisterChild(Overlay).AddTo(Anchors);
            
            Observable.Merge(
                    Overlay.WhenAnyProperty().Select(x => $"[{Context.Name}].{nameof(Overlay)}.{x.EventArgs.PropertyName} property changed"))
                .Subscribe(reason => RaisePropertyChanged(nameof(Properties)))
                .AddTo(Anchors);
            sw.Step($"Overlay registration completed");
        }
    }
}