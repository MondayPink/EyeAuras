using System.Drawing;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using EyeAuras.UI.Core.Models;
using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Prism;
using ReactiveUI;
using Unity;
using System;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Overlay.ViewModels
{
    internal sealed class ImageOverlayViewModel : EyeOverlayViewModel, IImageOverlayViewModel
    {
        public ImageOverlayViewModel(
            [NotNull] IAuraModelController auraModelController, 
            [NotNull] [Dependency(WellKnownWindows.AllWindows)] IWindowTracker mainWindowTracker,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler) : base(mainWindowTracker, auraModelController, uiScheduler)
        {
            this.WhenAnyValue(x => x.Content)
                .Select(x => x == null ? Size.Empty : new Size(x.PixelWidth, x.PixelHeight))
                .ObserveOn(uiScheduler)
                .Subscribe(x => ThumbnailSize = x)
                .AddTo(Anchors);
        }

        private BitmapSource content;

        public BitmapSource Content
        {
            get => content;
            set => RaiseAndSetIfChanged(ref content, value);
        }
    }
}