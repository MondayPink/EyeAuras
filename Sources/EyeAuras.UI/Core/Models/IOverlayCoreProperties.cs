using System.Drawing;
using EyeAuras.Shared.Services;
using Color = System.Windows.Media.Color;

namespace EyeAuras.UI.Core.Models
{
    internal interface IOverlayCoreProperties : IAuraCoreProperties
    {
        WindowMatchParams WindowMatch { get; set; }

        Rectangle OverlayBounds { get; set; }

        Rectangle SourceRegionBounds { get; set; }

        double BorderThickness { get; set; }

        Color BorderColor { get; set; }

        bool IsClickThrough { get; set; }

        double ThumbnailOpacity { get; set; }

        bool MaintainAspectRatio { get; set; }
    }
}