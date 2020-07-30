using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using EyeAuras.UI.Prism.Modularity;
using Force.DeepCloner;
using JetBrains.Annotations;
using log4net;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.Services;
using PoeShared.Squirrel.Updater;
using PoeShared.UI.Hotkeys;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI.MainWindow.ViewModels
{
    [UsedImplicitly]
    internal sealed class EyeAurasSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<EyeAurasConfig>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EyeAurasSettingsViewModel));

        private readonly IAppArguments appArguments;
        private readonly IHotkeyConverter hotkeyConverter;
        private readonly IConfigProvider<EyeAurasConfig> configProvider;
        private readonly IStartupManager startupManager;

        private HotkeyGesture freezeAurasHotkey;
        private HotkeyMode freezeAurasHotkeyMode;
        
        private HotkeyGesture unlockAurasHotkey;
        private HotkeyMode unlockAurasHotkeyMode;
        private HotkeyGesture selectRegionHotkey;
        private bool startMinimized;
        private bool minimizeToTray;

        public EyeAurasSettingsViewModel(
            [NotNull] IApplicationUpdaterViewModel appUpdater,
            [NotNull] IAppArguments appArguments,
            [NotNull] IHotkeyConverter hotkeyConverter,
            [NotNull] IFactory<IStartupManager, StartupManagerArgs> startupManagerFactory,
            [NotNull] IConfigProvider<EyeAurasConfig> configProvider)
        {
            var startupManagerArgs = new StartupManagerArgs
            {
                UniqueAppName = $"{appArguments.AppName}{(appArguments.IsDebugMode ? "-debug" : string.Empty)}",
                ExecutablePath = appUpdater.GetLatestExecutable().FullName,
                CommandLineArgs = appArguments.StartupArgs,
                AutostartFlag = appArguments.AutostartFlag
            };
            this.startupManager = startupManagerFactory.Create(startupManagerArgs);
            this.appArguments = appArguments;
            this.hotkeyConverter = hotkeyConverter;
            this.configProvider = configProvider;
            RunAtLoginToggleCommand = CommandWrapper.Create<bool>(RunAtLoginCommandExecuted);
        }

        public HotkeyGesture FreezeAurasHotkey
        {
            get => freezeAurasHotkey;
            set => RaiseAndSetIfChanged(ref freezeAurasHotkey, value);
        }

        public HotkeyMode FreezeAurasHotkeyMode
        {
            get => freezeAurasHotkeyMode;
            set => RaiseAndSetIfChanged(ref freezeAurasHotkeyMode, value);
        }
        
        public HotkeyGesture UnlockAurasHotkey
        {
            get => unlockAurasHotkey;
            set => RaiseAndSetIfChanged(ref unlockAurasHotkey, value);
        }

        public HotkeyGesture SelectRegionHotkey
        {
            get => selectRegionHotkey;
            set => this.RaiseAndSetIfChanged(ref selectRegionHotkey, value);
        }

        public HotkeyMode UnlockAurasHotkeyMode
        {
            get => unlockAurasHotkeyMode;
            set => RaiseAndSetIfChanged(ref unlockAurasHotkeyMode, value);
        }
        
        public CommandWrapper RunAtLoginToggleCommand { get; }

        public bool StartMinimized
        {
            get => startMinimized;
            set => this.RaiseAndSetIfChanged(ref startMinimized, value);
        }

        public bool MinimizeToTray
        {
            get => minimizeToTray;
            set => this.RaiseAndSetIfChanged(ref minimizeToTray, value);
        }
        
        public bool RunAtLogin => startupManager.IsRegistered;

        public string ModuleName { get; } = "EyeAuras Main Settings";
        
        public Task Load(EyeAurasConfig config)
        {
            FreezeAurasHotkey = hotkeyConverter.ConvertFromString(config.FreezeAurasHotkey);
            FreezeAurasHotkeyMode = config.FreezeAurasHotkeyMode;
            UnlockAurasHotkey = hotkeyConverter.ConvertFromString(config.UnlockAurasHotkey);
            UnlockAurasHotkeyMode = config.UnlockAurasHotkeyMode;
            SelectRegionHotkey = hotkeyConverter.ConvertFromString(config.RegionSelectHotkey);
            StartMinimized = config.StartMinimized;
            MinimizeToTray = config.MinimizeToTray;
            return Task.CompletedTask;
        }

        public EyeAurasConfig Save()
        {
            var updatedConfig = configProvider.ActualConfig.DeepClone();
            updatedConfig.FreezeAurasHotkey = FreezeAurasHotkey?.ToString();
            updatedConfig.FreezeAurasHotkeyMode = FreezeAurasHotkeyMode;
            updatedConfig.UnlockAurasHotkey = UnlockAurasHotkey?.ToString();
            updatedConfig.UnlockAurasHotkeyMode = UnlockAurasHotkeyMode;
            updatedConfig.RegionSelectHotkey = SelectRegionHotkey?.ToString();
            updatedConfig.StartMinimized = StartMinimized;
            updatedConfig.MinimizeToTray = MinimizeToTray;
            return updatedConfig;
        }
        
        private async Task RunAtLoginCommandExecuted(bool runAtLogin)
        {
            if (runAtLogin)
            {
                if (!startupManager.Register())
                {
                    Log.Warn("Failed to add application to Auto-start");
                }
                else
                {
                    Log.Info($"Application successfully added to Auto-start");
                }
            }
            else
            {
                if (!startupManager.Unregister())
                {
                    Log.Warn("Failed to remove application from Auto-start");
                }
                else
                {
                    Log.Info("Application successfully removed from Auto-start");
                }
            }
        }
    }
}