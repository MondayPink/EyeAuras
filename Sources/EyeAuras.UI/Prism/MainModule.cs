﻿using EyeAuras.Interception.Prism;
using EyeAuras.Shared;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Core.ViewModels;
using EyeAuras.UI.MainWindow.ViewModels;
using EyeAuras.UI.Prism.Modularity;
using EyeAuras.UI.Triggers.AuraIsActive;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Squirrel.Prism;
using PoeShared.Wpf.Scaffolding;
using Prism.Ioc;
using Prism.Modularity;
using Unity;

namespace EyeAuras.UI.Prism
{
    internal sealed class MainModule : IModule
    {
        private readonly IUnityContainer container;

        public MainModule(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            container.AddNewExtension<Diagnostic>();
            container.AddNewExtension<UiRegistrations>();
            container.AddNewExtension<WpfCommonRegistrations>();
            container.AddNewExtension<UpdaterRegistrations>();
            container.AddNewExtension<NativeRegistrations>();
            container.AddNewExtension<CommonRegistrations>();
            container.AddNewExtension<InterceptionRegistrations>();
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<EyeAurasConfig, EyeAurasSettingsViewModel>();
            
            container.RegisterOverlayController();

            var auraManager = container.Resolve<IAuraRegistrator>();

            auraManager.Register<OverlayAuraPropertiesEditorViewModel, OverlayAuraModelBase>();
            
            auraManager.Register<AuraIsActiveTrigger>();
            auraManager.Register<AuraIsActiveTriggerEditor, AuraIsActiveTrigger>();
        }
    }
}