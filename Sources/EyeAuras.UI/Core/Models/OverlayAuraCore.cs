﻿using System;
using System.Reactive.Linq;
using DynamicData.Binding;
using EyeAuras.UI.Core.Services;
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
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayAuraCore<>));

        private readonly IGlobalContext globalContext;
        private readonly IFactory<IOverlayWindowController, IWindowTracker> overlayWindowControllerFactory;
        private readonly IFactory<WindowTracker, IStringMatcher> windowTrackerFactory;
        
        private IEyeOverlayViewModel overlay;

        public OverlayAuraCore(
            [NotNull] IGlobalContext globalContext,
            [NotNull] IFactory<IOverlayWindowController, IWindowTracker> overlayWindowControllerFactory,
            [NotNull] IFactory<WindowTracker, IStringMatcher> windowTrackerFactory)
        {
            this.globalContext = globalContext;
            this.overlayWindowControllerFactory = overlayWindowControllerFactory;
            this.windowTrackerFactory = windowTrackerFactory;

            Observable.CombineLatest(this.WhenAnyValue(x => x.ModelController), this.WhenAnyValue(x => x.Context), (controller, auraContext) => new { controller, auraContext })
                .Where(x => x.controller != null && x.auraContext != null)
                .Take(1)
                .Subscribe(HandleInitialization)
                .AddTo(Anchors);
        }
        
        public IEyeOverlayViewModel Overlay
        {
            get => overlay;
            protected set => RaiseAndSetIfChanged(ref overlay, value);
        }

        protected abstract IEyeOverlayViewModel CreateOverlay(IOverlayWindowController windowController, IAuraModelController modelController);

        protected void VisitSave(IOverlayCoreProperties properties)
        {
            if (Overlay != null)
            {
                properties.OverlayBounds = Overlay.NativeBounds;
                properties.IsClickThrough = Overlay.IsClickThrough;
                properties.ThumbnailOpacity = Overlay.ThumbnailOpacity;
                properties.MaintainAspectRatio = Overlay.MaintainAspectRatio;
                properties.BorderColor = Overlay.BorderColor;
                properties.BorderThickness = Overlay.BorderThickness;
            }
        }

        protected void VisitLoad(IOverlayCoreProperties source)
        {
            if (Overlay != null)
            {
                Overlay.ThumbnailOpacity = source.ThumbnailOpacity;
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
        }

        protected virtual void HandleInitialization()
        {
            using var sw = new BenchmarkTimer($"[{this}] {Name} initialization", Log, nameof(OverlayAuraModel));

            var matcher = new FakeStringMatcher(true);
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
                    this.WhenAnyValue(x => x.Overlay.IsInitialized).ToUnit(),
                    Overlay.WhenValueChanged(x => x.IsLocked, false).ToUnit(),
                    globalContext.WhenValueChanged(x => x.OverlaysAreEnabled, false).ToUnit(),
                    Context.WhenValueChanged(x => x.IsActive, false).ToUnit())
                .StartWithDefault()
                .Select(
                    () => new
                    {
                        OverlayShouldBeShown = Context.IsActive || !Overlay.IsLocked,
                        OverlayIsInitialized = Overlay.IsInitialized,
                        globalContext.OverlaysAreEnabled
                    })
                .Subscribe(x => overlayController.IsEnabled = x.OverlaysAreEnabled && x.OverlayShouldBeShown && x.OverlayIsInitialized, Log.HandleUiException)
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