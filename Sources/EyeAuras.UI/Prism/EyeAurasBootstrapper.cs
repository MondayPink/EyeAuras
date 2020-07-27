using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using EyeAuras.DefaultAuras.Prism;
using EyeAuras.Shared.Services;
using EyeAuras.UI.MainWindow.ViewModels;
using EyeAuras.UI.Prism.Modularity;
using EyeAuras.UI.SplashWindow.Services;
using log4net;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Prism;
using Prism.Modularity;
using Prism.Unity;
using Unity;
using Unity.Lifetime;
using Unity.Resolution;

namespace EyeAuras.UI.Prism
{
    internal sealed class EyeAurasBootstrapper : UnityBootstrapper
    {
        private readonly IUnityContainer unityContainer;
        private static readonly ILog Log = LogManager.GetLogger(typeof(EyeAurasBootstrapper));

        private readonly CompositeDisposable anchors = new CompositeDisposable();

        public EyeAurasBootstrapper(IUnityContainer unityContainer)
        {
            this.unityContainer = unityContainer;
            Log.Debug($"Initializing EyeAuras bootstrapper");
        }

        protected override DependencyObject CreateShell()
        {
            Log.Info("Creating shell...");
            return Container.Resolve<MainWindow.Views.MainWindow>();
        }

        protected override IUnityContainer CreateContainer()
        {
            return unityContainer;
        }

        protected override void InitializeShell()
        {
            Log.Info("Initializing shell...");
            base.InitializeShell();

            var sw = Stopwatch.StartNew();

            var window = (Window) Shell;

            var splashWindow = new SplashWindowDisplayer(window);

            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => window.ContentRendered += h, h => window.ContentRendered -= h)
                .Take(1)
                .Subscribe(
                    () =>
                    {
                        Log.Debug("Window rendered");
                        Application.Current.MainWindow = window;
                        splashWindow.Close();
                        Log.Info($"Window initialization(frame + content) has taken {sw.ElapsedMilliseconds}ms");
                    })
                .AddTo(anchors);

            Observable
                .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => window.Loaded += h, h => window.Loaded -= h)
                .Take(1)
                .Subscribe(
                    () =>
                    {
                        Log.Debug("Window loaded");
                        Log.Info($"Window frame(without content) initialization has taken {sw.ElapsedMilliseconds}ms");
                    })
                .AddTo(anchors);
            
            Log.Info("Loading main window...");
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Application.Current.MainWindow = window;

            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            splashWindow.Show();
        }

        protected override IModuleCatalog CreateModuleCatalog()
        {
            Log.Info($"Loading application modules...");
            return new SharedModuleCatalog();
        }

        protected override void ConfigureContainer()
        {
            Log.Debug($"Configuring {Container}, catalog: {ModuleCatalog} (type: {ModuleCatalog.GetType()})");
            base.ConfigureContainer();
            if (ModuleCatalog is IAppModuleLoader appModuleLoader)
            {
                Container.RegisterInstance(appModuleLoader, new ContainerControlledLifetimeManager());
            }
            else
            {
                throw new ApplicationException($"ModuleCatalog must be of type {nameof(IAppModuleLoader)}, got {ModuleCatalog}");
            }
        }

        protected override void ConfigureModuleCatalog()
        {
            Log.Debug($"Configuring ModuleCatalog");
            var mainModule = typeof(MainModule);
            
            ModuleCatalog.AddModule(
                new ModuleInfo
                {
                    ModuleName = mainModule.Name,
                    ModuleType = mainModule.AssemblyQualifiedName,
                });

            var updaterModule = typeof(UpdaterModule);
            ModuleCatalog.AddModule(
                new ModuleInfo
                {
                    ModuleName = updaterModule.Name,
                    ModuleType = updaterModule.AssemblyQualifiedName,
                });
        }

        public override void Run(bool runWithDefaultConfiguration)
        {
            base.Run(runWithDefaultConfiguration);

            var moduleManager = Container.Resolve<IModuleManager>();
            Observable
                .FromEventPattern<LoadModuleCompletedEventArgs>(h => moduleManager.LoadModuleCompleted += h, h => moduleManager.LoadModuleCompleted -= h)
                .Select(x => x.EventArgs)
                .Subscribe(
                    evt =>
                    {
                        if (evt.Error != null)
                        {
                            Log.Error($"[#{evt.ModuleInfo.ModuleName}] Error during loading occured, isHandled: {evt.IsErrorHandled}", evt.Error);
                        }

                        Log.Info($"[#{evt.ModuleInfo.ModuleName}] Module loaded");
                    })
                .AddTo(anchors);

            var moduleCatalog = Container.Resolve<IModuleCatalog>();
            var modules = moduleCatalog.Modules.ToArray();
            Log.Debug(
                $"Modules list:\n\t{modules.Select(x => new {x.ModuleName, x.ModuleType, x.State, x.InitializationMode, x.DependsOn}).DumpToTable()}");

            var window = (Window) Shell;
            
            var sw = Stopwatch.StartNew();
            Log.Info("Initializing Main window...");
            var viewController = new WindowViewController(window);
            var viewModelFactory = Container.Resolve<IFactory<IMainWindowViewModel, IWindowViewController>>();
            var viewModel = viewModelFactory.Create(viewController).AddTo(anchors);
            window.DataContext = viewModel;
            sw.Stop();
            Log.Info($"Main window view model took {sw.ElapsedMilliseconds}ms...");
        }

        public void Dispose()
        {
            Log.Info("Disposing Main bootstrapper...");
            anchors.Dispose();
            Log.Info("Disposed Main bootstrapper...");
        }
    }
}