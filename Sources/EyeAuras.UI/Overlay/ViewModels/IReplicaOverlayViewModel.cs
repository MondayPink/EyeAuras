using System.Windows.Input;
using DynamicData.Annotations;
using EyeAuras.OnTopReplica;

namespace EyeAuras.UI.Overlay.ViewModels
{
    internal interface IReplicaOverlayViewModel : IEyeOverlayViewModel
    {
        WindowHandle AttachedWindow { get; set; }

        ThumbnailRegion Region { [NotNull] get; }

        ICommand ResetRegionCommand { [NotNull] get; }
    }
}