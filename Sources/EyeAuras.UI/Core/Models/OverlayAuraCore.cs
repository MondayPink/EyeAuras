using System;
using System.Reactive.Linq;
using DynamicData.Binding;
using EyeAuras.Shared.Services;
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
    internal sealed class OverlayAuraCore : AuraCore<OverlayCoreProperties>
    {
        [NotNull] private readonly IFactory<IEyeOverlayViewModel, IOverlayWindowController, IAuraModelController> overlayViewModelFactory;
        [NotNull] private readonly IFactory<IOverlayWindowController, IWindowTracker> overlayWindowControllerFactory;
        [NotNull] private readonly IFactory<WindowTracker, IStringMatcher> windowTrackerFactory;
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayAuraCore));
        
        private WindowMatchParams targetWindow;
        private IEyeOverlayViewModel overlay;

        public OverlayAuraCore(
            [NotNull] IFactory<IEyeOverlayViewModel, IOverlayWindowController, IAuraModelController> overlayViewModelFactory,
            [NotNull] IFactory<IOverlayWindowController, IWindowTracker> overlayWindowControllerFactory,
            [NotNull] IFactory<WindowTracker, IStringMatcher> windowTrackerFactory)
        {
            this.overlayViewModelFactory = overlayViewModelFactory;
            this.overlayWindowControllerFactory = overlayWindowControllerFactory;
            this.windowTrackerFactory = windowTrackerFactory;

            Observable.CombineLatest(this.WhenAnyValue(x => x.ModelController), this.WhenAnyValue(x => x.Context), (controller, auraContext) => new { controller, auraContext })
                .Where(x => x.controller != null && x.auraContext != null)
                .Take(1)
                .Subscribe(HandleInitialization)
                .AddTo(Anchors);
        }
        
        public override string Name { get; } = "Replica";
        
        public override string Description { get; } = "Show selected window Replica";
        
        public WindowMatchParams TargetWindow
        {
            get => targetWindow;
            set => RaiseAndSetIfChanged(ref targetWindow, value);
        }
        
        public IEyeOverlayViewModel Overlay
        {
            get => overlay;
            private set => RaiseAndSetIfChanged(ref overlay, value);
        }

        protected override void VisitSave(OverlayCoreProperties properties)
        {
            if (Overlay != null)
            {
                properties.SourceRegionBounds = Overlay.Region.Bounds;
                properties.OverlayBounds = Overlay.NativeBounds;
                properties.IsClickThrough = Overlay.IsClickThrough;
                properties.ThumbnailOpacity = Overlay.ThumbnailOpacity;
                properties.MaintainAspectRatio = Overlay.MaintainAspectRatio;
                properties.BorderColor = Overlay.BorderColor;
                properties.BorderThickness = Overlay.BorderThickness;
            }
            properties.WindowMatch = TargetWindow;
        }

        protected override void VisitLoad(OverlayCoreProperties source)
        {
            if (Overlay != null)
            {
                Overlay.ThumbnailOpacity = source.ThumbnailOpacity;
                Overlay.Region.SetValue(source.SourceRegionBounds);
                Overlay.IsClickThrough = source.IsClickThrough;
                Overlay.MaintainAspectRatio = source.MaintainAspectRatio;
                Overlay.BorderColor = source.BorderColor;
                Overlay.BorderThickness = source.BorderThickness;
            
                var bounds = source.OverlayBounds.ScaleToWpf();
                Overlay.Left = bounds.Left;
                Overlay.Top = bounds.Top;
                Overlay.Height = bounds.Height;
                Overlay.Width = bounds.Width;
            }
            TargetWindow = source.WindowMatch;
        }

        private void HandleInitialization()
        {
            using var sw = new BenchmarkTimer($"[{this}] {nameof(OverlayAuraCore)} initialization", Log, nameof(OverlayAuraModelBase));

            var matcher = new RegexStringMatcher().AddToWhitelist(".*");
            var windowTracker = windowTrackerFactory
                .Create(matcher)
                .AddTo(Anchors);
            var overlayController = overlayWindowControllerFactory
                .Create(windowTracker)
                .AddTo(Anchors);
            sw.Step($"Overlay controller created: {overlayController}");

            var overlayViewModel = overlayViewModelFactory
                .Create(overlayController, ModelController)
                .AddTo(Anchors);
            Overlay = overlayViewModel;
            sw.Step($"Overlay view model created: {overlayViewModel}");
            
            Observable.Merge(
                    overlayViewModel.WhenValueChanged(x => x.AttachedWindow, false).ToUnit(),
                    overlayViewModel.WhenValueChanged(x => x.IsLocked, false).ToUnit(),
                    Context.WhenValueChanged(x => x.IsActive, false).ToUnit())
                .StartWithDefault()
                .Select(
                    () => new
                    {
                        OverlayShouldBeShown = Context.IsActive || !overlayViewModel.IsLocked,
                        WindowIsAttached = overlayViewModel.AttachedWindow != null
                    })
                .Subscribe(x => overlayController.IsEnabled = x.OverlayShouldBeShown && x.WindowIsAttached, Log.HandleUiException)
                .AddTo(Anchors);
            sw.Step($"Overlay view model initialized: {overlayViewModel}");
            
            overlayController.RegisterChild(overlayViewModel).AddTo(Anchors);
            
            Observable.Merge(
                    this.WhenAnyProperty(x => x.TargetWindow).Select(x => $"[{Context.Name}].{x.EventArgs.PropertyName} property changed"),
                    Overlay.WhenAnyProperty().Select(x => $"[{Context.Name}].{nameof(Overlay)}.{x.EventArgs.PropertyName} property changed"))
                .Subscribe(reason => RaisePropertyChanged(nameof(Properties)))
                .AddTo(Anchors);
            sw.Step($"Overlay registration completed");
        }
    }
}