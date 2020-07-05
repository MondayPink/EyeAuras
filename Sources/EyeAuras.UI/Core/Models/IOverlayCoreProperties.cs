using System.Drawing;
using EyeAuras.Shared.Services;
using Color = System.Windows.Media.Color;

namespace EyeAuras.UI.Core.Models
{
    internal interface IOverlayCoreProperties : IAuraCoreProperties
    {
        Rectangle OverlayBounds { get; set; }

        double BorderThickness { get; set; }

        Color BorderColor { get; set; }

        bool IsClickThrough { get; set; }

        double ThumbnailOpacity { get; set; }

        bool MaintainAspectRatio { get; set; }
    }
}