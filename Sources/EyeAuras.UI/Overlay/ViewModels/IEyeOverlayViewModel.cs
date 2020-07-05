using System.Windows.Media;
using PoeShared.Native;

namespace EyeAuras.UI.Overlay.ViewModels
{
    internal interface IEyeOverlayViewModel : IOverlayViewModel
    {
        Color BorderColor { get; set; }
        
        double BorderThickness { get; set; }

        string OverlayName { get; }

        bool IsClickThrough { get; set; }

        double ThumbnailOpacity { get; set; }
        
        bool MaintainAspectRatio { get; set; }

        void ScaleOverlay(double scaleRatio);
    }
}