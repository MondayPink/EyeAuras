using EyeAuras.UI.Core.Models;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Sharing.ViewModels
{
    internal interface IAuraPreviewViewModel : IDisposableReactiveObject
    {
        OverlayAuraProperties[] Content { get; set; }
    }
}