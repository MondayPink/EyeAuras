﻿using System.Windows;
using PoeShared.Scaffolding;
using ReactiveUI;
using System;
using System.Drawing;
using Size = System.Drawing.Size;

namespace EyeAuras.OnTopReplica
{
    public sealed class ThumbnailRegion : DisposableReactiveObject
    {
        private int regionHeight;
        private int regionWidth;
        private int regionX;
        private int regionY;

        private readonly ObservableAsPropertyHelper<Rectangle> bounds;

        /// <summary>
        ///     Creates a ThumbnailRegion from a bounds rectangle (in absolute terms).
        /// </summary>
        public ThumbnailRegion(Rectangle rectangle)
        {
            bounds = this.WhenAnyProperty(x => x.RegionX, x => x.RegionY, x => x.RegionHeight, x => x.RegionWidth)
                .Select(() => RegionWidth <= 0 || RegionHeight <= 0
                        ? Rectangle.Empty
                        : new Rectangle(RegionX, RegionY, RegionWidth, RegionHeight))
                .ToPropertyHelper(this, x => x.Bounds)
                .AddTo(Anchors);
            SetValue(rectangle);
        }

        public Rectangle Bounds => bounds.Value;

        public int RegionX
        {
            get => regionX;
            set => RaiseAndSetIfChanged(ref regionX, value);
        }

        public int RegionY
        {
            get => regionY;
            set => RaiseAndSetIfChanged(ref regionY, value);
        }

        public int RegionWidth
        {
            get => regionWidth;
            set => RaiseAndSetIfChanged(ref regionWidth, value);
        }

        public int RegionHeight
        {
            get => regionHeight;
            set => RaiseAndSetIfChanged(ref regionHeight, value);
        }

        public void SetValue(Rectangle rectangle)
        {
            if (rectangle == bounds.Value)
            {
                return;
            }
            var previousState = new { RegionX, RegionY, RegionHeight, RegionWidth };
            regionWidth = rectangle.Width;
            regionHeight = rectangle.Height;
            regionX = rectangle.X;
            regionY = rectangle.Y;

            this.RaiseIfChanged(nameof(RegionX), previousState.RegionX, RegionX);
            this.RaiseIfChanged(nameof(RegionY), previousState.RegionY, RegionY);
            this.RaiseIfChanged(nameof(RegionHeight), previousState.RegionHeight, RegionHeight);
            this.RaiseIfChanged(nameof(RegionWidth), previousState.RegionWidth, RegionWidth);
        }
        
        public override string ToString()
        {
            return $"Region({Bounds})";
        }
    }
}