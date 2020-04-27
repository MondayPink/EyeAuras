using System.Collections.ObjectModel;
using EyeAuras.Shared;
using EyeAuras.UI.Core.ViewModels;

namespace EyeAuras.UI.Core.Services
{
    internal interface IEyeAuraSharedContext : ISharedContext
    {
        IComplexAuraTrigger SystemTrigger { get; }
        
        ObservableCollection<IAuraTabViewModel> TabList { get; }
    }
}