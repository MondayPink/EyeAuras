using System.Collections.ObjectModel;
using System.Linq;
using EyeAuras.Shared.Sharing.Services;
using log4net;
using PoeShared.Scaffolding;

namespace EyeAuras.UI.Sharing.Services
{
    internal sealed class ShareProviderRepository : IShareProviderRepository, IShareProviderRegistrator
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ShareProviderRepository));

        private readonly ObservableCollection<IShareProvider> shares;
        
        public ShareProviderRepository()
        {
            shares = new ObservableCollection<IShareProvider>();
            Providers = new ReadOnlyObservableCollection<IShareProvider>(shares);
        }

        public ReadOnlyObservableCollection<IShareProvider> Providers { get; }
        
        public void Register(IShareProvider shareProvider)
        {
            Log.Info($"Registering new Share provider {shareProvider.Name}, knownProviders: {shares.Select(x => x.Name).DumpToTextRaw()}");   
            shares.Add(shareProvider);
        }
    }
}