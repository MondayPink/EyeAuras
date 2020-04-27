using System.Windows.Input;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;
using JetBrains.Annotations;
using PoeShared.Native;

namespace EyeAuras.UI.Core.ViewModels
{
    internal interface IAuraTabViewModel : IAuraViewModel
    {
        bool IsFlipped { get; set; }
        
        string DefaultTabName { [NotNull] get; }
        
        ICommand RenameCommand { [NotNull] get; }

        OverlayAuraProperties Properties { get; }

        void SetCloseController([NotNull] ICloseController closeController);
    }
}