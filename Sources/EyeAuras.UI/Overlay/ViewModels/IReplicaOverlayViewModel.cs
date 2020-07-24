using System.Windows.Input;
using DynamicData.Annotations;
using EyeAuras.OnTopReplica;

namespace EyeAuras.UI.Overlay.ViewModels
{
    internal interface IReplicaOverlayViewModel : IEyeOverlayViewModel
    {
        IWindowHandle AttachedWindow { get; set; }

        ThumbnailRegion Region { [NotNull] get; }
    }
}