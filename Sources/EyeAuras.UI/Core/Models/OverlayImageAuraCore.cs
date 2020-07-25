using EyeAuras.UI.Overlay.ViewModels;
using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Prism;
using ReactiveUI;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using EyeAuras.Shared;
using log4net;
using PoeShared;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Core.Models
{
    internal sealed class OverlayImageAuraCore : OverlayAuraCore<OverlayImageCoreProperties>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayImageAuraCore));

        private readonly IFactory<IImageOverlayViewModel, IOverlayWindowController, IAuraModelController> overlayViewModelFactory;
        private string imageFilePath;
        private BitmapImage imageFile;
        private byte[] imageData;

        public OverlayImageAuraCore(
            [NotNull] IFactory<IImageOverlayViewModel, IOverlayWindowController, IAuraModelController> overlayViewModelFactory,
            [NotNull] IFactory<IOverlayWindowController, IWindowTracker> overlayWindowControllerFactory, 
            [NotNull] IFactory<WindowTracker, IStringMatcher> windowTrackerFactory) : base(overlayWindowControllerFactory, windowTrackerFactory)
        {
            this.overlayViewModelFactory = overlayViewModelFactory;

            this.WhenAnyValue(x => x.ImageFilePath)
                .SelectSafeOrDefault(x => string.IsNullOrEmpty(x) ? null : File.ReadAllBytes(x))
                .Subscribe(x => ImageData = x, Log.HandleUiException)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.ImageData)
                .SelectSafeOrDefault(LoadOrDefault)
                .Subscribe(x => ImageFile = x, Log.HandleUiException)
                .AddTo(Anchors);
        }

        protected override void VisitSave(OverlayImageCoreProperties properties)
        {
            base.VisitSave(properties);
            properties.ImageData = imageData;
            properties.ImageFilePath = null; // we're storing images inside configs now
        }

        protected override void VisitLoad(OverlayImageCoreProperties properties)
        {
            base.VisitLoad(properties);
            if (properties.ImageData != null)
            {
                ImageData = properties.ImageData;
            }
            
            if (!string.IsNullOrEmpty(properties.ImageFilePath))
            {
                ImageFilePath = properties.ImageFilePath;
            }
        }
        
        public new IImageOverlayViewModel Overlay => base.Overlay as IImageOverlayViewModel;

        public override string Name { get; } = "\uf03e Image";

        public override string Description { get; } = "Show Image overlay";

        public string ImageFilePath
        {
            get => imageFilePath;
            set => RaiseAndSetIfChanged(ref imageFilePath, value);
        }

        public BitmapImage ImageFile
        {
            get => imageFile;
            private set => RaiseAndSetIfChanged(ref imageFile, value);
        }

        [AuraProperty]
        public byte[] ImageData
        {
            get => imageData;
            private set => RaiseAndSetIfChanged(ref imageData, value);
        }

        public void ResetImage()
        {
            ImageFilePath = null;
            ImageData = null;
            ImageFile = null;
        }
        
        protected override IEyeOverlayViewModel CreateOverlay(IOverlayWindowController windowController, IAuraModelController modelController)
        {
            return overlayViewModelFactory.Create(windowController, modelController);
        }

        protected override void HandleInitialization()
        {
            base.HandleInitialization();
            
            this.WhenAnyValue(x => x.ImageFile)
                .Subscribe(x => Overlay.Content = x)
                .AddTo(Anchors);
        }
        
        private static BitmapImage LoadOrDefault(byte[] data)
        {
            try
            {
                if (data == null || data.Length == 0)
                {
                    return null;
                }
                var result = new BitmapImage();
                result.BeginInit();
                //FIXME Should probably be disposed 
                result.StreamSource = new MemoryStream(data);
                result.EndInit();
                return result;
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to load binary image data (length: {data?.Length ?? -1})", e);
                return null;
            }
        }
    }
}