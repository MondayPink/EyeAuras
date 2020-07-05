﻿using System;
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
            [NotNull] IFactory<IReplicaOverlayViewModel, IOverlayWindowController, IAuraModelController> overlayViewModelFactory,
            [NotNull] IFactory<IOverlayWindowController, IWindowTracker> overlayWindowControllerFactory, 
            [NotNull] IFactory<WindowTracker, IStringMatcher> windowTrackerFactory) : base(overlayWindowControllerFactory, windowTrackerFactory)
        {
            this.overlayViewModelFactory = overlayViewModelFactory;
        }

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
            if (Overlay is IReplicaOverlayViewModel replicaOverlay)
            {
                properties.SourceRegionBounds = replicaOverlay.Region.Bounds;
            }
            properties.WindowMatch = TargetWindow;
        }

        protected override void VisitLoad(OverlayReplicaCoreProperties source)
        {
            base.VisitLoad(source);
            if (Overlay is IReplicaOverlayViewModel replicaOverlay)
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