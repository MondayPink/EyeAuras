using System.Drawing;
using EyeAuras.OnTopReplica;
using PoeShared.Native;
using PoeShared.Scaffolding;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace EyeAuras.UI.Core.Models
{
    internal sealed class OverlayPositionAdapter : DisposableReactiveObject
    {
        private RegionAnchor overlayAnchor;
        private IOverlayViewModel overlay;

        public OverlayPositionAdapter()
        {
            this.WhenAnyValue(x => x.Overlay)
                .Select(x => x == null ? Observable.Empty<Rectangle>() : AbsoluteBounds.WhenAnyValue(y => y.Bounds))
                .Switch()
                .Where(x => Overlay.NativeBounds != x)
                .Select(x => x.ScaleToWpf())
                .Subscribe(x =>
                {
                    Overlay.Left = x.Left;
                    Overlay.Top = x.Top;
                    Overlay.Width = x.Width;
                    Overlay.Height = x.Height;
                })
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.Overlay)
                .Select(x => x == null ? Observable.Empty<Rectangle>() : Overlay.WhenAnyValue(y => y.NativeBounds))
                .Switch()
                .Where(x => x != AbsoluteBounds.Bounds)
                .Subscribe(x =>
                {
                    AbsoluteBounds.SetValue(x);
                })
                .AddTo(Anchors);

            AbsoluteBounds.WhenAnyValue(x => x.Bounds)
                .Select(x =>
                {
                    var expectedBounds = RecalculateBounds(RelativeBounds.Bounds, overlayAnchor);

                    var location = x.Location;
                    if (location != expectedBounds.Location)
                    {
                        Console.WriteLine($"Location changed: {new Point(expectedBounds.X - location.X, expectedBounds.Y - location.Y)}");
                    }

                    var size = x.Size;
                    return new Rectangle(location, size);
                })
                .Subscribe(x => RelativeBounds.SetValue(x))
                .AddTo(Anchors);

            Observable.CombineLatest(RelativeBounds.WhenAnyValue(x => x.Bounds), this.WhenAnyValue(x => x.OverlayAnchor), (rectangle, anchor) => new { rectangle, anchor })
                .DistinctUntilChanged()
                .Select(x => new { Actual = x, Calculated = RecalculateBounds(x.rectangle, x.anchor) })
                .Subscribe(x => AbsoluteBounds.SetValue(x.Calculated))
                .AddTo(Anchors);
        }
        
        public ThumbnailRegion AbsoluteBounds { get; } = new ThumbnailRegion(Rectangle.Empty);
        
        public ThumbnailRegion RelativeBounds { get; } = new ThumbnailRegion(Rectangle.Empty);

        public RegionAnchor OverlayAnchor
        {
            get => overlayAnchor;
            set => RaiseAndSetIfChanged(ref overlayAnchor, value);
        }

        public IOverlayViewModel Overlay
        {
            get => overlay;
            set => RaiseAndSetIfChanged(ref overlay, value);
        }
        
        public void VisitSave(IOverlayCoreProperties properties)
        {
            if (Overlay == null)
            {
                return;
            }
            
            properties.OverlayBounds = RelativeBounds.Bounds;
        }

        public void VisitLoad(IOverlayCoreProperties source)
        {
            if (Overlay == null)
            {
                return;
            }

            RelativeBounds.SetValue(source.OverlayBounds);
        }

        private static Rectangle RecalculateBounds(Rectangle bounds, RegionAnchor anchor)
        {
            var result = bounds;
            switch (anchor)
            {
                case RegionAnchor.TopLeft:
                    break;
                case RegionAnchor.TopRight:
                    result.Offset(-bounds.Width, 0);
                    break;
                case RegionAnchor.BottomRight:
                    result.Offset(-bounds.Width, -bounds.Height);
                    break;
                case RegionAnchor.BottomLeft:
                    result.Offset(0, -bounds.Height);
                    break;
                case RegionAnchor.Center:
                    result.Offset(-bounds.Width / 2, -bounds.Height / 2);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null);
            }

            return result;
        }
    }
}