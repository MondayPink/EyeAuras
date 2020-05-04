using System.Windows.Input;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;
using JetBrains.Annotations;
using PoeShared.Native;

namespace EyeAuras.UI.Core.ViewModels
{
    internal interface IAuraTabViewModel : IAuraViewModel
    {
        OverlayAuraProperties Properties { get; }

        void SetCloseController([NotNull] ICloseController closeController);
    }
}