using EyeAuras.CsScriptAuras.Actions.ExecuteScript;
using EyeAuras.Shared;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.Wpf.Scaffolding;
using Prism.Ioc;
using Unity;

namespace EyeAuras.CsScriptAuras.Prism
{
    [UsedImplicitly]
    public sealed class CsScriptAurasModule : DisposableReactiveObject, IDynamicModule
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CsScriptAurasModule));

        private readonly IUnityContainer container;

        public CsScriptAurasModule(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            container.RegisterOverlayController();

            var auraManager = container.Resolve<IAuraRegistrator>();

            auraManager.Register<ExecuteScriptAction>();
            auraManager.Register<ExecuteScriptActionEditor, ExecuteScriptAction>();
        }
    }
}