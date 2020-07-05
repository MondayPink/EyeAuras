using EyeAuras.UI.Overlay.ViewModels;
using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Prism;
using ReactiveUI;
using System;
using System.Windows.Media.Imaging;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.Models
{
    internal sealed class OverlayImageAuraCore : OverlayAuraCore<OverlayImageCoreProperties>
    {
        private readonly IFactory<IImageOverlayViewModel, IOverlayWindowController, IAuraModelController> overlayViewModelFactory;

        public OverlayImageAuraCore(
            [NotNull] IFactory<IImageOverlayViewModel, IOverlayWindowController, IAuraModelController> overlayViewModelFactory,
            [NotNull] IFactory<IOverlayWindowController, IWindowTracker> overlayWindowControllerFactory, 
            [NotNull] IFactory<WindowTracker, IStringMatcher> windowTrackerFactory) : base(overlayWindowControllerFactory, windowTrackerFactory)
        {
            this.overlayViewModelFactory = overlayViewModelFactory;
        }

        protected override void VisitSave(OverlayImageCoreProperties properties)
        {
            base.VisitSave(properties);
            properties.ImageFilePath = ImageFilePath;
        }

        protected override void VisitLoad(OverlayImageCoreProperties source)
        {
            base.VisitLoad(source);
            ImageFilePath = source.ImageFilePath;
        }
        
        public new IImageOverlayViewModel Overlay
        {
            get => base.Overlay as IImageOverlayViewModel;
            private set => base.Overlay = value;
        }

        public override string Name { get; } = "Image";

        public override string Description { get; } = "Show Image overlay";

        private string imageFilePath;

        public string ImageFilePath
        {
            get => imageFilePath;
            set => RaiseAndSetIfChanged(ref imageFilePath, value);
        }
        
        protected override IEyeOverlayViewModel CreateOverlay(IOverlayWindowController windowController, IAuraModelController modelController)
        {
            return overlayViewModelFactory.Create(windowController, modelController);
        }

        protected override void HandleInitialization()
        {
            base.HandleInitialization();
            
            this.WhenAnyValue(x => x.ImageFilePath)
                .SelectSafeOrDefault(x => new BitmapImage(new Uri(x)))
                .Subscribe(x => Overlay.Content = x)
                .AddTo(Anchors);
        }
    }
}