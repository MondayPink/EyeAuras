using System.Collections.ObjectModel;
using System.Windows.Input;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;
using JetBrains.Annotations;
using PoeShared.Native;

namespace EyeAuras.UI.Core.ViewModels
{
    internal interface IOverlayAuraTabViewModel : IAuraTabViewModel
    {
        IPropertyEditorViewModel GeneralEditor { [NotNull] get; }
    }
}