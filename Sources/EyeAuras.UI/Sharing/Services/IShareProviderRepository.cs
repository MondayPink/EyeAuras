using System.Collections.ObjectModel;

namespace EyeAuras.UI.Sharing.Services
{
    internal interface IShareProviderRepository
    {
        ReadOnlyObservableCollection<IShareProvider> Providers { get; }
    }
}