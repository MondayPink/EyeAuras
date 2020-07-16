using EyeAuras.Controls.ViewModels;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.Shared.Sharing.Services;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Core.Services;
using EyeAuras.UI.Core.ViewModels;
using EyeAuras.UI.MainWindow.Models;
using EyeAuras.UI.MainWindow.ViewModels;
using EyeAuras.UI.Overlay.ViewModels;
using EyeAuras.UI.Prism.Modularity;
using EyeAuras.UI.RegionSelector.Services;
using EyeAuras.UI.RegionSelector.ViewModels;
using EyeAuras.UI.Sharing.Services;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using Unity;
using Unity.Extension;
using IMainWindowBlocksRepository = EyeAuras.UI.MainWindow.Models.IMainWindowBlocksRepository;

namespace EyeAuras.UI.Prism
{
    internal sealed class UiRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<AuraRepository>(typeof(IAuraRepository), typeof(IAuraRegistrator))
                .RegisterSingleton<MainWindowBlocksService>(typeof(IMainWindowBlocksRepository), typeof(Shared.Services.IMainWindowBlocksRepository))
                .RegisterSingleton<ShareProviderRepository>(typeof(IShareProviderRegistrator), typeof(IShareProviderRepository))
                .RegisterSingleton<IWindowListProvider, WindowListProvider>()
                .RegisterSingleton<SharedContext>(typeof(IGlobalContext), typeof(ISharedContext))
                .RegisterSingleton<IRegionSelectorService, RegionSelectorService>()
                .RegisterSingleton<IUniqueIdGenerator, UniqueIdGenerator>()
                .RegisterSingleton<IWindowMatcher, WindowMatcher>()
                .RegisterSingleton<IPrismModuleStatusViewModel, PrismModuleStatusViewModel>()
                .RegisterSingleton<IAppModulePathResolver, AppModulePathResolver>()
                .RegisterSingleton<IAuraSerializer, AuraSerializer>()
                .RegisterSingleton<IMainWindowViewModel, MainWindowViewModel>();

            Container
                .RegisterType<ISelectionAdornerViewModel, SelectionAdornerViewModel>()
                .RegisterType<IWindowSelectorService, WindowSelectorService>()
                .RegisterType<IMessageBoxViewModel, MessageBoxViewModel>()
                .RegisterType<IOverlayAuraModel, OverlayAuraModel>()
                .RegisterType<IRegionSelectorViewModel, RegionSelectorViewModel>()
                .RegisterType<IPropertyEditorViewModel, PropertyEditorViewModel>()
                .RegisterType<IOverlayAuraTabViewModel, OverlayAuraTabViewModel>()
                .RegisterType<IImageOverlayViewModel, ImageOverlayViewModel>()
                .RegisterType<IReplicaOverlayViewModel, ReplicaOverlayViewModel>();

            Container.RegisterSingleton<IConfigProvider, ConfigProviderFromFile>();
        }
    }
}