using System.Drawing;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using Color = System.Windows.Media.Color;

namespace EyeAuras.UI.Core.Models
{
    //FIXME This must be migrated to a new Core properties version, left for compatibility with older versions
    internal sealed class OverlayAuraProperties : AuraPropertiesBase, IOverlayCoreProperties
    {
        public static readonly OverlayAuraProperties Default = new OverlayAuraProperties
        {
            CoreProperties = new EmptyCoreProperties(),
            IsEnabled = true
        };

        public override int Version { get; set; } = 2;
        
        public WindowMatchParams WindowMatch { get; set; }
        public Rectangle OverlayBounds { get; set; }
        public Rectangle SourceRegionBounds { get; set; }
        public double BorderThickness { get; set; }
        public Color BorderColor { get; set; }
        public bool IsClickThrough { get; set; }
        public double ThumbnailOpacity { get; set; }
        public bool MaintainAspectRatio { get; set; }
    }
}