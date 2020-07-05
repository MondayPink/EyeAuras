using System.Drawing;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace EyeAuras.UI.Core.Models
{
    internal abstract class OverlayCoreProperties : IOverlayCoreProperties
    {
        public Rectangle OverlayBounds { get; set; } = new Rectangle(100,100,200,200);

        public double BorderThickness { get; set; }

        public Color BorderColor { get; set; } = Colors.AntiqueWhite;

        public bool IsClickThrough { get; set; }

        public double ThumbnailOpacity { get; set; } = 1;

        public bool MaintainAspectRatio { get; set; } = true;
        
        public abstract int Version { get; set; }
    }
}