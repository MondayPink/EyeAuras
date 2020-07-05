using System;
using System.Drawing;
using System.Windows.Media;
using EyeAuras.Shared.Services;
using Color = System.Windows.Media.Color;

namespace EyeAuras.UI.Core.Models
{
    internal sealed class OverlayCoreProperties : IOverlayCoreProperties
    {
        public WindowMatchParams WindowMatch { get; set; }

        public Rectangle OverlayBounds { get; set; }

        public Rectangle SourceRegionBounds { get; set; }

        public double BorderThickness { get; set; }

        public Color BorderColor { get; set; } = Colors.AntiqueWhite;

        public bool IsClickThrough { get; set; }

        public double ThumbnailOpacity { get; set; } = 1;

        public bool MaintainAspectRatio { get; set; } = true;
        
        public int Version { get; set; } = 1;
    }
}