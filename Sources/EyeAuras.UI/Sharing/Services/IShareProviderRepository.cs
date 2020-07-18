using System.Collections.ObjectModel;
using EyeAuras.Shared.Sharing.Services;

namespace EyeAuras.UI.Sharing.Services
{
    internal interface IShareProviderRepository
    {
        ReadOnlyObservableCollection<IShareProvider> Providers { get; }
    }
}