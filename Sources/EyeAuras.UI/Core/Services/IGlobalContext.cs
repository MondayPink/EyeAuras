using System.Collections.ObjectModel;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Core.ViewModels;

namespace EyeAuras.UI.Core.Services
{
    internal interface IGlobalContext : ISharedContext
    {
        IComplexAuraTrigger SystemTrigger { get; }
        
        ObservableCollection<IAuraTabViewModel> TabList { get; }

        IAuraTabViewModel[] CreateAura(params OverlayAuraProperties[] properties);
    }
}