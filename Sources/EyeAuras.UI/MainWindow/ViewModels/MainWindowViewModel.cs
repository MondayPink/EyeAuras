using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.DefaultAuras.Triggers.HotkeyIsActive;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Models;
using EyeAuras.UI.Core.Services;
using EyeAuras.UI.Core.ViewModels;
using EyeAuras.UI.MainWindow.Models;
using EyeAuras.UI.MainWindow.Services;
using EyeAuras.UI.Prism.Modularity;
using EyeAuras.UI.Sharing.ViewModels;
using Force.DeepCloner;
using Humanizer;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.Squirrel.Updater;
using PoeShared.UI;
using PoeShared.UI.Hotkeys;
using PoeShared.UI.TreeView;
using PoeShared.Wpf.UI.Settings;
using ReactiveUI;
using Unity;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using IMainWindowBlocksRepository = EyeAuras.UI.MainWindow.Models.IMainWindowBlocksRepository;
using Size = System.Windows.Size;

namespace EyeAuras.UI.MainWindow.ViewModels
{
    internal class MainWindowViewModel : DisposableReactiveObject, IMainWindowViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MainWindowViewModel));
        private static readonly int UndoStackDepth = 10;

        private static readonly string ExplorerExecutablePath = Environment.ExpandEnvironmentVariables(@"%WINDIR%\explorer.exe");
        private static readonly TimeSpan ConfigSaveSamplingTimeout = TimeSpan.FromSeconds(5);

        private readonly IClipboardManager clipboardManager;
        private readonly IAuraSerializer auraSerializer;
        private readonly IConfigProvider<EyeAurasConfig> configProvider;
        private readonly IGlobalContext globalContext;
        private readonly ISubject<Unit> configUpdateSubject = new Subject<Unit>();

        private readonly IHotkeyConverter hotkeyConverter;
        private readonly IFactory<HotkeyIsActiveTrigger> hotkeyTriggerFactory;
        private readonly CircularBuffer<OverlayAuraProperties> recentlyClosedQueries = new CircularBuffer<OverlayAuraProperties>(UndoStackDepth);
        private readonly IRegionSelectorService regionSelectorService;
        private readonly IWindowViewController viewController;
        private readonly IAppArguments appArguments;

        private double height;
        private double left;
        private GridLength listWidth;
        private IAuraTabViewModel selectedAura;
        private double top;
        private double width;
        private WindowState windowState;
        private Visibility visibility;
        private bool showInTaskbar;
        private bool topmost;
        private bool isLoading = true;

        public MainWindowViewModel(
            [NotNull] IWindowViewController viewController,
            [NotNull] IGlobalContext globalContext,
            [NotNull] IAppArguments appArguments,
            [NotNull] IApplicationUpdaterViewModel appUpdater,
            [NotNull] IClipboardManager clipboardManager,
            [NotNull] IAuraSerializer auraSerializer,
            [NotNull] IGenericSettingsViewModel settingsViewModel,
            [NotNull] IMessageBoxViewModel messageBox,
            [NotNull] IHotkeyConverter hotkeyConverter,
            [NotNull] IFactory<HotkeyIsActiveTrigger> hotkeyTriggerFactory,
            [NotNull] IConfigProvider<EyeAurasConfig> configProvider,
            [NotNull] IConfigProvider rootConfigProvider,
            [NotNull] IPrismModuleStatusViewModel moduleStatus,
            [NotNull] IMainWindowBlocksRepository mainWindowBlocksRepository,
            [NotNull] IFactory<IRegionSelectorService> regionSelectorServiceFactory,
            [NotNull] EyeTreeViewAdapter treeViewAdapter,
            [NotNull] ExportMessageBoxViewModel exportMessageBox,
            [NotNull] ImportMessageBoxViewModel importMessageBox,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UIIdle)] IScheduler uiIdleScheduler)
        {
            TreeViewAdapter = treeViewAdapter;
            ExportMessageBox = exportMessageBox;
            ImportMessageBox = importMessageBox;
            using var unused = new OperationTimer(elapsed => Log.Debug($"{nameof(MainWindowViewModel)} initialization took {elapsed.TotalMilliseconds:F0}ms"));
            viewController
                .WhenRendered
                .Take(1)
                .Select(() => configProvider.ListenTo(y => y.StartMinimized))
                .Switch()
                .Take(1)
                .ObserveOn(uiScheduler)
                .Subscribe(
                    x =>
                    {
                        if (x)
                        {
                            Log.Debug($"StartMinimized option is active - minimizing window, current state: {WindowState}");
                            viewController.Hide();
                        }
                        else
                        {
                            Log.Debug($"StartMinimized option is not active - showing window as Normal, current state: {WindowState}");
                            viewController.Show();
                        }
                        
                    }, Log.HandleUiException)
                .AddTo(Anchors);
            
            Observable.Merge(
                    this.WhenAnyValue(x => x.WindowState).ToUnit(),
                    configProvider.ListenTo(x => x.MinimizeToTray).ToUnit())
                .ObserveOn(uiScheduler)
                .Subscribe(x => ShowInTaskbar = WindowState != WindowState.Minimized || !configProvider.ActualConfig.MinimizeToTray, Log.HandleUiException)
                .AddTo(Anchors);
            
            TabsList = globalContext.TabList;
            treeViewAdapter.WhenAnyValue(x => x.SelectedValue)
                .Subscribe(x => SelectedAura = x, Log.HandleUiException)
                .AddTo(Anchors);
            this.WhenAnyValue(x => x.SelectedAura)
                .Subscribe(x => treeViewAdapter.SelectedValue = x, Log.HandleUiException)
                .AddTo(Anchors);
            
            ModuleStatus = moduleStatus.AddTo(Anchors);
            var executingAssemblyName = Assembly.GetExecutingAssembly().GetName();
            Title = $"{(appArguments.IsDebugMode ? "[D]" : "")} {executingAssemblyName.Name} v{executingAssemblyName.Version}";
            Disposable.Create(() => Log.Info("Disposing Main view model")).AddTo(Anchors);

            ApplicationUpdater = appUpdater.AddTo(Anchors);
            MessageBox = messageBox.AddTo(Anchors);
            Settings = settingsViewModel.AddTo(Anchors);
            StatusBarItems = mainWindowBlocksRepository.StatusBarItems;

            this.viewController = viewController;
            this.appArguments = appArguments;
            this.configProvider = configProvider;
            this.globalContext = globalContext;
            this.regionSelectorService = regionSelectorServiceFactory.Create();
            this.clipboardManager = clipboardManager;
            this.auraSerializer = auraSerializer;
            this.hotkeyConverter = hotkeyConverter;
            this.hotkeyTriggerFactory = hotkeyTriggerFactory;

            ExitAppCommand = CommandWrapper.Create(
                () =>
                {
                    Log.Debug("Closing application");
                    configProvider.Save(configProvider.ActualConfig);
                    System.Windows.Application.Current.Shutdown();
                });
            ShowAppCommand = CommandWrapper.Create(ToggleAppVisibilityCommandExecuted);
            CreateNewTabCommand = CommandWrapper.Create(CreateNewTabCommandExecuted);
            CreateNewTabInDirectoryCommand = CommandWrapper.Create<object>(CreateNewTabInDirectoryCommandExecuted);
            CloseTabCommand = CommandWrapper
                .Create<IOverlayAuraTabViewModel>(CloseTabCommandExecuted, CloseTabCommandCanExecute)
                .RaiseCanExecuteChangedWhen(this.WhenAnyProperty(x => x.SelectedAura));

            DuplicateTabCommand = CommandWrapper
                .Create(DuplicateTabCommandExecuted, DuplicateTabCommandCanExecute)
                .RaiseCanExecuteChangedWhen(this.WhenAnyProperty(x => x.SelectedAura));
            CopyTabToClipboardCommand = CommandWrapper
                .Create<object>(CopyTabToClipboardExecuted);

            PasteTabCommand = CommandWrapper.Create(PasteTabCommandExecuted);
            UndoCloseTabCommand = CommandWrapper.Create(UndoCloseTabCommandExecuted, UndoCloseTabCommandCanExecute);
            OpenAppDataDirectoryCommand = CommandWrapper.Create(OpenAppDataDirectory);
            SelectRegionCommand = CommandWrapper.Create(SelectRegionCommandExecuted);
            CreateDirectoryCommand = CommandWrapper.Create<string>(path =>
            {
                var result = treeViewAdapter.AddDirectory(path);
                result.RenameCommand.Execute(null);
            });
            RemoveDirectoryCommand = CommandWrapper.Create<string>(path =>
            {
                var auras = treeViewAdapter.EnumerateAuras(path).ToArray();
                if (!auras.Any() || System.Windows.MessageBox.Show(
                    Application.Current.MainWindow,
                    $"Are you sure you want to remove folder {path} and {"aura".ToQuantity(auras.Length)} ?", "Confirmation",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    treeViewAdapter.RemoveDirectory(path);
                }
            });

            Observable.Merge(
                    this.WhenAnyProperty(x => x.Left, x => x.Top, x => x.Width, x => x.Height)
                        .Sample(ConfigSaveSamplingTimeout, bgScheduler)
                        .Select(x => $"[{x.Sender}] Main window property change: {x.EventArgs.PropertyName}"),
                    globalContext.AuraList.ToObservableChangeSet()
                        .Sample(ConfigSaveSamplingTimeout, bgScheduler)
                        .Select(x => "Tabs list change"),
                    globalContext.TabList.ToObservableChangeSet()
                        .WhenPropertyChanged(x => x.Properties)
                        .Sample(ConfigSaveSamplingTimeout, bgScheduler)
                        .Select(x => $"[{x.Sender}] Properties have changed"),
                    TreeViewAdapter.TreeViewItems.ToObservableChangeSet()
                        .Select(x => $"Aura list changed, item count: {TreeViewAdapter.TreeViewItems.Count}"),
                    TreeViewAdapter.TreeViewItems.ToObservableChangeSet()
                        .WhenPropertyChanged(nameof(EyeTreeItemViewModel.Name), nameof(ITreeViewItemViewModel.IsExpanded))
                        .Select(x => $"[{x.Sender}].{x.EventArgs.PropertyName} changed"))
                .Buffer(ConfigSaveSamplingTimeout, bgScheduler)
                .Where(x => x.Count > 0)
                .Subscribe(
                    reasons =>
                    {
                        const int maxReasonsToOutput = 50;
                        Log.Debug(
                            $"Config Save reasons({(reasons.Count <= maxReasonsToOutput ? $"{reasons.Count} items" : $"first {maxReasonsToOutput} of {reasons.Count} items")}):\r\n\t{reasons.Take(maxReasonsToOutput).JoinStrings("\r\n\t")}");
                        configUpdateSubject.OnNext(Unit.Default);
                    },
                    Log.HandleUiException)
                .AddTo(Anchors);

            GlobalHotkeyTrigger = CreateFreezeAurasTrigger();
            globalContext.SystemTrigger.Add(GlobalHotkeyTrigger);

            configProvider
                .WhenChanged
                .Subscribe(x => this.RaisePropertyChanged(nameof(ActualConfig)))
                .AddTo(Anchors);

            RegisterSelectRegionHotkey()
                .Where(isActive => isActive)
                .ObserveOn(uiScheduler)
                .Subscribe(isActive => SelectRegionCommandExecuted(), Log.HandleUiException)
                .AddTo(Anchors);

            RegisterGlobalUnlockHotkey()
                .ObserveOn(uiScheduler)
                .Subscribe(
                    unlock =>
                    {
                        var allOverlays = TabsList
                            .OfType<IOverlayAuraTabViewModel>()
                            .Select(x => x.Model)
                            .OfType<IOverlayAuraModel>()
                            .Select(x => x.Core)
                            .OfType<IOverlayAuraCore>()
                            .Where(x => x.Overlay != null)
                            .Select(y => y.Overlay);
                        if (unlock)
                        {
                            allOverlays
                                .Where(overlay => overlay.UnlockWindowCommand.CanExecute(null))
                                .ForEach(overlay => overlay.UnlockWindowCommand.Execute(null));
                        }
                        else
                        {
                            allOverlays
                                .Where(overlay => overlay.LockWindowCommand.CanExecute(null))
                                .ForEach(overlay => overlay.LockWindowCommand.Execute(null));
                        }
                    }, Log.HandleUiException)
                .AddTo(Anchors);

            LoadConfig();
            Log.Info($"Updating configuration format");
            rootConfigProvider.Save();

            if (globalContext.AuraList.Count == 0)
            {
                CreateNewTabCommand.Execute(null);
            }

            configUpdateSubject
                .Sample(ConfigSaveSamplingTimeout, bgScheduler)
                .Subscribe(SaveConfig, Log.HandleException)
                .AddTo(Anchors);

            globalContext
                .TabList
                .ToObservableChangeSet()
                .ObserveOn(uiScheduler)
                .SkipInitial()
                .OnItemAdded(x => SelectedAura = x)
                .Subscribe()
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.SelectedAura)
                .Subscribe(x => Log.Debug($"Selected tab: {x}"))
                .AddTo(Anchors);

            foreach (var aura in globalContext.TabList.Where(x => x.IsEnabled))
            {
                using var profiler = new BenchmarkTimer($"Preloading aura {aura}", Log);
            }

            uiIdleScheduler.Schedule(() =>
            {
                Log.Info($"Loading Auras...");
                SelectedAura = globalContext.TabList.LastOrDefault();
                GlobalHotkeyTrigger.TriggerValue = true;
                IsLoading = false;
                Log.Info($"Loading completed");
            }).AddTo(Anchors);
        }

        public EyeAurasConfig ActualConfig => configProvider.ActualConfig;

        public IPrismModuleStatusViewModel ModuleStatus { get; }

        public ReadOnlyObservableCollection<object> StatusBarItems { [NotNull] get; }

        public string Title { get; }

        public bool IsLoading
        {
            get => isLoading;
            set => RaiseAndSetIfChanged(ref isLoading, value);
        }

        public bool IsElevated => appArguments.IsElevated;

        public HotkeyIsActiveTrigger GlobalHotkeyTrigger { get; }

        public IApplicationUpdaterViewModel ApplicationUpdater { get; }

        public IMessageBoxViewModel MessageBox { get; }

        public ObservableCollection<IAuraTabViewModel> TabsList { get; }

        public CommandWrapper OpenAppDataDirectoryCommand { get; }
        
        public CommandWrapper ShowAppCommand { get; }
        
        public CommandWrapper ExitAppCommand { get; }
        
        public CommandWrapper CreateDirectoryCommand { get; }
        
        public CommandWrapper RemoveDirectoryCommand { get; }
        
        public IGenericSettingsViewModel Settings { get; }
        
        public EyeTreeViewAdapter TreeViewAdapter { get; }
        
        public ExportMessageBoxViewModel ExportMessageBox { get; }
        
        public ImportMessageBoxViewModel ImportMessageBox { get; }

        public Size MinSize { get; } = new Size(1150, 750);

        public IAuraTabViewModel SelectedAura
        {
            get => selectedAura;
            set => RaiseAndSetIfChanged(ref selectedAura, value);
        }

        public GridLength ListWidth
        {
            get => listWidth;
            set => RaiseAndSetIfChanged(ref listWidth, value);
        }

        public double Width
        {
            get => width;
            set => RaiseAndSetIfChanged(ref width, value);
        }

        public double Height
        {
            get => height;
            set => RaiseAndSetIfChanged(ref height, value);
        }

        public double Left
        {
            get => left;
            set => RaiseAndSetIfChanged(ref left, value);
        }

        public double Top
        {
            get => top;
            set => RaiseAndSetIfChanged(ref top, value);
        }

        public WindowState WindowState
        {
            get => windowState;
            set => RaiseAndSetIfChanged(ref windowState, value);
        }
        
        public bool ShowInTaskbar
        {
            get => showInTaskbar;
            set => this.RaiseAndSetIfChanged(ref showInTaskbar, value);
        }

        public Visibility Visibility
        {
            get => visibility;
            set => this.RaiseAndSetIfChanged(ref visibility, value);
        }

        public bool Topmost
        {
            get => topmost;
            set => this.RaiseAndSetIfChanged(ref topmost, value);
        }

        public CommandWrapper CreateNewTabCommand { get; }
        
        public CommandWrapper CreateNewTabInDirectoryCommand { get; }

        public CommandWrapper CloseTabCommand { get; }

        public CommandWrapper CopyTabToClipboardCommand { get; }

        public CommandWrapper DuplicateTabCommand { get; }

        public CommandWrapper UndoCloseTabCommand { get; }

        public CommandWrapper PasteTabCommand { get; }

        public CommandWrapper SelectRegionCommand { get; }
        
        public override void Dispose()
        {
            Log.Debug("Disposing viewmodel...");
            SaveConfig();

            foreach (var mainWindowTabViewModel in TabsList)
            {
                mainWindowTabViewModel.Dispose();
            }

            base.Dispose();

            Log.Debug("Viewmodel disposed");
        }


        private void ToggleAppVisibilityCommandExecuted()
        {
            if (Visibility != Visibility.Visible || WindowState == WindowState.Minimized)
            {
                SetAppVisibility(true);
            }
            else
            {
                SetAppVisibility(false);
            }
        }

        private void SetAppVisibility(bool isVisible)
        {
            if (isVisible)
            {
                viewController.Show();
            }
            else if (configProvider.ActualConfig.MinimizeToTray)
            {
                viewController.Hide();
            }
            else
            {
                viewController.Minimize();
            }
        }

        private HotkeyIsActiveTrigger CreateFreezeAurasTrigger()
        {
            var result = hotkeyTriggerFactory.Create();
            result.SuppressKey = true;
            result.TriggerValue = false;
            Observable.Merge(
                    configProvider.ListenTo(x => x.FreezeAurasHotkey).ToUnit(),
                    configProvider.ListenTo(x => x.FreezeAurasHotkeyMode).ToUnit())
                .Select(
                    () => new
                    {
                        FreezeAurasHotkey = hotkeyConverter.ConvertFromString(configProvider.ActualConfig.FreezeAurasHotkey),
                        configProvider.ActualConfig.FreezeAurasHotkeyMode
                    })
                .DistinctUntilChanged()
                .WithPrevious((prev, curr) => new {prev, curr})
                .Subscribe(
                    cfg =>
                    {
                        Log.Debug($"Setting new FreezeAurasHotkey configuration, {cfg.prev.DumpToTextRaw()} => {cfg.curr.DumpToTextRaw()}");
                        result.Hotkey = cfg.curr.FreezeAurasHotkey;
                        result.HotkeyMode = cfg.curr.FreezeAurasHotkeyMode;
                    },
                    Log.HandleException)
                .AddTo(Anchors);

            return result;
        }
        
        private IObservable<bool> RegisterGlobalUnlockHotkey()
        {
            var globalUnlockHotkeyTrigger = hotkeyTriggerFactory.Create().AddTo(Anchors);
            globalUnlockHotkeyTrigger.SuppressKey = true;
            globalUnlockHotkeyTrigger.HotkeyMode = HotkeyMode.Hold;

            Observable.Merge(
                    configProvider.ListenTo(x => x.UnlockAurasHotkey).ToUnit(),
                    configProvider.ListenTo(x => x.UnlockAurasHotkeyMode).ToUnit())
                .Select(
                    () => new
                    {
                        UnlockAurasHotkey = hotkeyConverter.ConvertFromString(configProvider.ActualConfig.UnlockAurasHotkey),
                        configProvider.ActualConfig.UnlockAurasHotkeyMode
                    })
                .DistinctUntilChanged()
                .WithPrevious((prev, curr) => new {prev, curr})
                .Subscribe(
                    cfg =>
                    {
                        Log.Debug($"Setting new UnlockAurasHotkey configuration, {cfg.prev.DumpToTextRaw()} => {cfg.curr.DumpToTextRaw()}");
                        globalUnlockHotkeyTrigger.Hotkey = cfg.curr.UnlockAurasHotkey;
                        globalUnlockHotkeyTrigger.HotkeyMode = cfg.curr.UnlockAurasHotkeyMode;
                    },
                    Log.HandleUiException)
                .AddTo(Anchors);

            return globalUnlockHotkeyTrigger.WhenAnyValue(x => x.IsActive).DistinctUntilChanged();
        }
        
        private IObservable<bool> RegisterSelectRegionHotkey()
        {
            var selectRegionHotkeyTrigger = hotkeyTriggerFactory.Create().AddTo(Anchors);
            selectRegionHotkeyTrigger.SuppressKey = true;
            selectRegionHotkeyTrigger.HotkeyMode = HotkeyMode.Hold;
            Observable.Merge(configProvider.ListenTo(x => x.RegionSelectHotkey).ToUnit())
                .Select(
                    () => new
                    {
                        RegionSelectHotkey = hotkeyConverter.ConvertFromString(configProvider.ActualConfig.RegionSelectHotkey)
                    })
                .DistinctUntilChanged()
                .WithPrevious((prev, curr) => new {prev, curr})
                .Subscribe(
                    cfg =>
                    {
                        Log.Debug($"Setting new {nameof(selectRegionHotkeyTrigger)} configuration, {cfg.prev.DumpToTextRaw()} => {cfg.curr.DumpToTextRaw()}");
                        selectRegionHotkeyTrigger.Hotkey = cfg.curr.RegionSelectHotkey;
                    },
                    Log.HandleException)
                .AddTo(Anchors);

            return selectRegionHotkeyTrigger.WhenAnyValue(x => x.IsActive).DistinctUntilChanged();
        }
        
        private async void SelectRegionCommandExecuted()
        {
            Log.Debug($"Requesting screen Region from {regionSelectorService}...");
            SetAppVisibility(false);
            var result = await regionSelectorService.SelectRegion();

            if (result?.IsValid ?? false)
            {
                Log.Debug($"ScreenRegion selection result: {result}");
                var newTabProperties = new OverlayAuraProperties
                {
                    Name = $"{result.Window.ProcessName} - {result.Window.Title}",
                    IsEnabled = true,
                    CoreProperties = new OverlayReplicaCoreProperties()
                    {
                        WindowMatch = new WindowMatchParams
                        {
                            Title = result.Window.Handle.ToHexadecimal(),
                        },
                        OverlayBounds = result.AbsoluteSelection,
                        SourceRegionBounds = result.Selection,
                        MaintainAspectRatio = true
                    }
                };
                Log.Info($"Quick-Creating new tab using {newTabProperties.DumpToTextRaw()} args...");
                AddNewCommandExecuted(newTabProperties, false); 
            }
            else
            {
                Log.Debug("ScreenRegion selection cancelled/failed");
            }
        }

        private async Task OpenAppDataDirectory()
        {
            await Task.Run(
                () =>
                {
                    Log.Debug($"Opening App directory: {appArguments.AppDataDirectory}");
                    if (!Directory.Exists(appArguments.AppDataDirectory))
                    {
                        Log.Debug($"App directory does not exist, creating dir: {appArguments.AppDataDirectory}");
                        Directory.CreateDirectory(appArguments.AppDataDirectory);
                    }

                    Process.Start(ExplorerExecutablePath, appArguments.AppDataDirectory);
                });
        }

        private void CreateNewTabCommandExecuted()
        {
            CreateAura(OverlayAuraProperties.Default);
        }

        private void CreateNewTabInDirectoryCommandExecuted(object parameter)
        {
            var path = parameter switch
            {
                HolderTreeViewItemViewModel treeItemForAura => treeItemForAura.Value.Path,
                IAuraTabViewModel auraTab => auraTab.Path,
                DirectoryTreeViewItemViewModel treeDirectory => treeDirectory.Path,
                var _ => throw new ArgumentOutOfRangeException(nameof(parameter), parameter,
                    $"Something went wrong - failed to copy parameter of type {parameter.GetType()}: {parameter}")
            };

            var properties = OverlayAuraProperties.Default.DeepClone();
            properties.Path = path;
            CreateAura(properties);
        }
        
        private bool CopyTabToClipboardCommandCanExecute(object parameter)
        {
            return parameter is IAuraTabViewModel || parameter is ITreeViewItemViewModel;
        }

        private void CopyTabToClipboardExecuted(object parameter)
        {
            if (!CopyTabToClipboardCommandCanExecute(parameter))
            {
                return;
            }

            Log.Debug($"Copying {parameter}...");

            var data = auraSerializer.Serialize(parameter);
            clipboardManager.SetText(data);
        }

        private bool UndoCloseTabCommandCanExecute()
        {
            return !recentlyClosedQueries.IsEmpty;
        }

        private void UndoCloseTabCommandExecuted()
        {
            if (!UndoCloseTabCommandCanExecute())
            {
                return;
            }

            var closedAuraProperties = recentlyClosedQueries.PopBack();
            CreateAura(closedAuraProperties);
            UndoCloseTabCommand.RaiseCanExecuteChanged();
        }

        private void PasteTabCommandExecuted()
        {
            var content = clipboardManager.GetText() ?? "";
            if (ImportMessageBox.DownloadCommand.CanExecute(content))
            {
                ImportMessageBox.DownloadCommand.Execute(content);
            }
        }

        private bool DuplicateTabCommandCanExecute()
        {
            return selectedAura != null;
        }

        private void DuplicateTabCommandExecuted()
        {
            Guard.ArgumentIsTrue(() => DuplicateTabCommandCanExecute());

            var cfg = selectedAura.Properties;
            CreateAura(cfg);
        }

        private bool CloseTabCommandCanExecute(IAuraTabViewModel tab)
        {
            return tab != null;
        }

        private void CloseTabCommandExecuted(IAuraTabViewModel tab)
        {
            using var sw = new BenchmarkTimer($"Close tab {tab.TabName}({tab.Id})", Log);
            Guard.ArgumentNotNull(tab, nameof(tab));

            Log.Debug($"Removing tab {tab}...");

            var tabIdx = TabsList.IndexOf(tab);
            if (tabIdx > 0)
            {
                var tabToSelect = TabsList[tabIdx - 1];
                Log.Debug($"Selecting neighbour tab {tabToSelect}...");
                SelectedAura = tabToSelect;
            }

            globalContext.TabList.Remove(tab);
            sw.Step($"Removed tab from AuraList (current count: {globalContext.AuraList.Count})");

            var cfg = tab.Properties;
            recentlyClosedQueries.PushBack(cfg);
            UndoCloseTabCommand.RaiseCanExecuteChanged();

            tab.Dispose();
            sw.Step($"Disposed tab {tab}");
        }

        private void SaveConfig()
        {
            using var profiler = new BenchmarkTimer("Config save routine", Log);
            var config = PrepareConfig();
            profiler.Step("Preparing config");
            configProvider.Save(config);
            profiler.Step("Saving config");
        }

        private EyeAurasConfig PrepareConfig()
        {
            var positionedItems = TabsList;
            Log.Debug($"Preparing config, tabs count: {positionedItems.Count}");
            var config = configProvider.ActualConfig.DeepClone();

            config.Auras = positionedItems.Select(x => x.Properties).ToArray();
            config.MainWindowBounds = new Rect(Left, Top, Width, Height);
            config.ListWidth = ListWidth.Value;
            config.Directories = TreeViewAdapter.EnumerateDirectories().EmptyIfNull().ToArray();
            return config;
        }

        private void LoadConfig()
        {
            using var unused = new BenchmarkTimer("Load config operation", Log);

            Log.Debug($"Loading config (provider: {configProvider})...");

            var config = configProvider.ActualConfig;

            Log.Debug($"Received configuration DTO:\r\n{config.DumpToText()}");
            
            var desktopHandle = UnsafeNative.GetDesktopWindow();
            var systemInformation = new
            {
                SystemInformation.MonitorCount,
                SystemInformation.VirtualScreen,
                MonitorBounds = UnsafeNative.GetMonitorBounds(desktopHandle),
                MonitorInfo = UnsafeNative.GetMonitorInfo(desktopHandle)
            };
            Log.Debug($"Current SystemInformation: {systemInformation.DumpToTextRaw()}");

            if (!config.MainWindowBounds.Size.IsNotEmpty() || UnsafeNative.IsOutOfBounds(config.MainWindowBounds, systemInformation.MonitorBounds))
            {
                var size = config.MainWindowBounds.Size.IsNotEmpty()
                    ? config.MainWindowBounds.Size
                    : MinSize;
                var screenCenter = UnsafeNative.GetPositionAtTheCenter(systemInformation.MonitorBounds, size);
                Log.Warn(
                    $"Main window is out of screen bounds(screen: {systemInformation.MonitorBounds}, main window bounds: {config.MainWindowBounds}, using size {size}), resetting Location to {screenCenter}");
                config.MainWindowBounds = new Rect(screenCenter, size);
            }

            foreach (var auraProperties in config.Auras.EmptyIfNull())
            {
                //FIXME Remove OverlayConfig v2 backward compatibility layer
                if (auraProperties.Version == 2 && auraProperties.CoreProperties == null)
                {
                    Log.Info($"[{auraProperties.Name}] Converting old config format to a new one");

                    if (auraProperties.WindowMatch.IsEmpty)
                    {
                        auraProperties.CoreProperties = new EmptyCoreProperties();
                    }
                    else
                    {
                        auraProperties.CoreProperties = new OverlayReplicaCoreProperties
                        {
                            BorderColor = auraProperties.BorderColor,
                            BorderThickness = auraProperties.BorderThickness,
                            OverlayBounds = auraProperties.OverlayBounds,
                            ThumbnailOpacity = auraProperties.ThumbnailOpacity,
                            WindowMatch = auraProperties.WindowMatch,
                            IsClickThrough = auraProperties.IsClickThrough,
                            MaintainAspectRatio = auraProperties.MaintainAspectRatio,
                            SourceRegionBounds = auraProperties.SourceRegionBounds,
                        };
                    }

                    auraProperties.WindowMatch = new WindowMatchParams();
                    auraProperties.BorderThickness = 0;
                    auraProperties.BorderColor = new Color();
                    auraProperties.ThumbnailOpacity = 0;
                    auraProperties.IsClickThrough = false;
                    auraProperties.MaintainAspectRatio = false;
                    auraProperties.SourceRegionBounds = Rectangle.Empty;
                    auraProperties.OverlayBounds = Rectangle.Empty;
                }
                
                CreateAura(auraProperties);
            }

            Left = config.MainWindowBounds.Left;
            Top = config.MainWindowBounds.Top;
            Width = config.MainWindowBounds.Width;
            Height = config.MainWindowBounds.Height;
            viewController.Topmost = false;

            ListWidth = config.ListWidth <= 0 || double.IsNaN(config.ListWidth)
                ? GridLength.Auto
                : new GridLength(config.ListWidth, GridUnitType.Pixel);

            Log.Debug($"Successfully loaded config, tabs count: {TabsList.Count}");
            TreeViewAdapter.SyncWith(globalContext.TabList, config.Directories.EmptyIfNull().ToArray());
        }

        private void AddNewCommandExecuted(OverlayAuraProperties tabProperties, bool rename = true)
        {
            try
            {
                var newTab = CreateAura(tabProperties);
                
                if (newTab is IOverlayAuraTabViewModel newOverlayTab)
                {
                    if (newOverlayTab.Model is IOverlayAuraModel newOverlayModel && newOverlayModel.Core is IOverlayAuraCore overlayCore && overlayCore.Overlay.UnlockWindowCommand.CanExecute(null))
                    {
                        overlayCore.Overlay.UnlockWindowCommand.Execute(null);
                    }

                    if (rename)
                    {
                        var treeItem = TreeViewAdapter.FindItemByTab(newOverlayTab);
                        if (treeItem != null && treeItem.RenameCommand.CanExecute(null))
                        {
                            treeItem.RenameCommand.Execute(null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.HandleUiException(e);
            }
        }
        
        public IAuraTabViewModel[] CreateAura(params OverlayAuraProperties[] properties)
        {
            return properties.Select(CreateAura).ToArray();
        }

        private IAuraTabViewModel CreateAura(OverlayAuraProperties tabProperties)
        {
            var auraViewModel = globalContext.CreateAura(tabProperties).Single();
            var auraCloseController = new CloseController<IAuraTabViewModel>(auraViewModel, () => CloseTabCommandExecuted(auraViewModel));
            auraViewModel.SetCloseController(auraCloseController);
            return auraViewModel;
        }
    }
}