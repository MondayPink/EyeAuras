using System.Windows.Media.Imaging;

namespace EyeAuras.UI.Overlay.ViewModels
{
    internal interface IImageOverlayViewModel : IEyeOverlayViewModel
    {
        BitmapSource Content { get; set; }
    }
}