using EyeAuras.UI.Overlay.ViewModels;
using JetBrains.Annotations;

namespace EyeAuras.UI.Core.Models
{
    internal interface IOverlayAuraCore
    {
        IEyeOverlayViewModel Overlay { [CanBeNull] get; }
    }
}