using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Dragablz;
using DynamicData;
using DynamicData.Binding;
using EyeAuras.Shared;
using EyeAuras.Shared.Services;
using EyeAuras.UI.Core.Services;
using EyeAuras.UI.Core.Utilities;
using EyeAuras.UI.MainWindow.Models;
using EyeAuras.UI.Overlay.ViewModels;
using EyeAuras.UI.Prism.Modularity;
using JetBrains.Annotations;
using log4net;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity;

namespace EyeAuras.UI.Core.Models
{
    internal sealed class OverlayAuraModelBase : AuraModelBase<OverlayAuraProperties>, IOverlayAuraModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayAuraModelBase));
        private static readonly TimeSpan ModelsReloadTimeout = TimeSpan.FromSeconds(1);
        private static int GlobalAuraIdx;

        private readonly IAuraRepository repository;
        private readonly IConfigSerializer configSerializer;
        private readonly IClock clock;
        private readonly string defaultAuraName;

        private ICloseController closeController;
        private bool isActive;
        private bool isEnabled = true;
        private string name;
        private WindowMatchParams targetWindow;
        private string uniqueId;
        private TimeSpan whileActiveActionsTimeout;
        private DateTime lastWhileActiveTimestamp;

        public OverlayAuraModelBase(
            [NotNull] ISharedContext sharedContext,
            [NotNull] IAuraRepository repository,
            [NotNull] IConfigSerializer configSerializer,
            [NotNull] IUniqueIdGenerator idGenerator,
            [NotNull] IClock clock,
            [NotNull] IFactory<IEyeOverlayViewModel, IOverlayWindowController, IAuraModelController> overlayViewModelFactory,
            [NotNull] IFactory<IOverlayWindowController, IWindowTracker> overlayWindowControllerFactory,
            [NotNull] IFactory<WindowTracker, IStringMatcher> windowTrackerFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            defaultAuraName = $"Aura #{Interlocked.Increment(ref GlobalAuraIdx)}";
            Name = defaultAuraName;
            Id = idGenerator.Next();
            using var sw = new BenchmarkTimer($"[{Name}({Id})] OverlayAuraModel initialization", Log, nameof(OverlayAuraModelBase));
            
            var auraTriggers = new ComplexAuraTrigger().AddTo(Anchors);
            Triggers = auraTriggers.Triggers;
            
            var onEnterActionsHolder = new ComplexAuraAction();
            OnEnterActions = onEnterActionsHolder.Actions;
            
            var onExitActionsHolder = new ComplexAuraAction();
            OnExitActions = onExitActionsHolder.Actions;
            
            var whileActiveActionsHolder = new ComplexAuraAction();
            WhileActiveActions = whileActiveActionsHolder.Actions;
            
            this.repository = repository;
            this.configSerializer = configSerializer;
            this.clock = clock;
            var matcher = new RegexStringMatcher().AddToWhitelist(".*");
            var windowTracker = windowTrackerFactory
                .Create(matcher)
                .AddTo(Anchors);

            var overlayController = overlayWindowControllerFactory
                .Create(windowTracker)
                .AddTo(Anchors);
            sw.Step($"Overlay controller created: {overlayController}");

            var overlayViewModel = overlayViewModelFactory
                .Create(overlayController, this)
                .AddTo(Anchors);
            sw.Step($"Overlay view model created: {overlayViewModel}");
            
            Overlay = overlayViewModel;
            Observable.Merge(
                    overlayViewModel.WhenValueChanged(x => x.AttachedWindow, false).ToUnit(),
                    overlayViewModel.WhenValueChanged(x => x.IsLocked, false).ToUnit(),
                    this.WhenValueChanged(x => x.IsActive, false).ToUnit())
                .StartWithDefault()
                .Select(
                    () => new
                    {
                        OverlayShouldBeShown = IsActive || !overlayViewModel.IsLocked,
                        WindowIsAttached = overlayViewModel.AttachedWindow != null
                    })
                .Subscribe(x => overlayController.IsEnabled = x.OverlayShouldBeShown && x.WindowIsAttached)
                .AddTo(Anchors);
            sw.Step($"Overlay view model initialized: {overlayViewModel}");

            Observable.CombineLatest(
                    auraTriggers.WhenAnyValue(x => x.IsActive),  
                    sharedContext.SystemTrigger.WhenValueChanged(x => x.IsActive))
                .DistinctUntilChanged()
                .Subscribe(x => IsActive = x.All(isActive => isActive), Log.HandleUiException)
                .AddTo(Anchors);

            var triggerActivates =
            auraTriggers.WhenAnyValue(x => x.IsActive)
                .WithPrevious((prev, curr) => new {prev, curr})
                .Where(x => x.prev == false && x.curr)
                .Where(x => Triggers.Count > 0);
            
            var triggerDeactivates = auraTriggers.WhenAnyValue(x => x.IsActive)
                .WithPrevious((prev, curr) => new {prev, curr})
                .Where(x => x.prev == true && x.curr == false)
                .Where(x => Triggers.Count > 0);
            
            triggerActivates
                .ObserveOn(bgScheduler)
                .Subscribe(ExecuteOnEnterActions, Log.HandleUiException)
                .AddTo(Anchors);
            triggerDeactivates
                .ObserveOn(bgScheduler)
                .Subscribe(ExecuteOnExitActions, Log.HandleUiException)
                .AddTo(Anchors);

            Observable.Merge(
                    triggerActivates.ToUnit(), 
                    triggerDeactivates.ToUnit(), 
                    this.WhenAnyValue(x => x.WhileActiveActionsTimeout).ToUnit())
                .Select(
                    x => IsActive && Triggers.Count > 0
                        ? Observable.Timer(DateTimeOffset.Now, TimeSpan.FromMilliseconds(50), bgScheduler).ToUnit()
                        : Observable.Empty(Unit.Default))
                .Switch()
                .Where(x => clock.Now - lastWhileActiveTimestamp > WhileActiveActionsTimeout)
                .ObserveOn(bgScheduler)
                .Subscribe(ExecuteWhileActiveActions, Log.HandleUiException)
                .AddTo(Anchors);
            
            this.repository.KnownEntities
                .ToObservableChangeSet()
                .SkipInitial()
                .Throttle(ModelsReloadTimeout, bgScheduler)
                .ObserveOn(uiScheduler)
                .Subscribe(
                    () =>
                    {
                        var properties = Properties;
                        ReloadCollections(properties);
                    })
                .AddTo(Anchors);
            
            var modelPropertiesToIgnore = new[]
            {
                nameof(IAuraTrigger.IsActive),
                nameof(IAuraTrigger.TriggerDescription),
                nameof(IAuraTrigger.TriggerName),
            }.ToImmutableHashSet();

            //FIXME Properties mechanism should have inverted logic - only important parameters must matter
            Observable.Merge(
                    this.WhenAnyProperty(x => x.Name, x => x.TargetWindow, x => x.IsEnabled).Select(x => $"[{Name}].{x.EventArgs.PropertyName} property changed"),
                    Overlay.WhenAnyProperty().Where(x => !modelPropertiesToIgnore.Contains(x.EventArgs.PropertyName)).Select(x => $"[{Name}].{nameof(Overlay)}.{x.EventArgs.PropertyName} property changed"),
                    Triggers.ToObservableChangeSet().Select(x => $"[{Name}({Id})] Trigger list changed, item count: {Triggers.Count}"),
                    Triggers.ToObservableChangeSet().WhenPropertyChanged().Where(x => !modelPropertiesToIgnore.Contains(x.EventArgs.PropertyName)).Select(x => $"[{Name}].{x.Sender}.{x.EventArgs.PropertyName} Trigger property changed"),
                    OnEnterActions.ToObservableChangeSet().Select(x => $"[{Name}({Id})] [OnEnter]  Action list changed, item count: {OnEnterActions.Count}"),
                    OnEnterActions.ToObservableChangeSet().WhenPropertyChanged().Where(x => !modelPropertiesToIgnore.Contains(x.EventArgs.PropertyName)).Select(x => $"[{Name}].{x.Sender}.{x.EventArgs.PropertyName} [OnEnter] Action property changed"),
                    OnExitActions.ToObservableChangeSet().Select(x => $"[{Name}({Id})] [OnExit]  Action list changed, item count: {OnEnterActions.Count}"),
                    OnExitActions.ToObservableChangeSet().WhenPropertyChanged().Where(x => !modelPropertiesToIgnore.Contains(x.EventArgs.PropertyName)).Select(x => $"[{Name}].{x.Sender}.{x.EventArgs.PropertyName} [OnExit] Action property changed"),
                    WhileActiveActions.ToObservableChangeSet().Select(x => $"[{Name}({Id})] [WhileActive] Action list changed, item count: {OnEnterActions.Count}"),
                    WhileActiveActions.ToObservableChangeSet().WhenPropertyChanged().Where(x => !modelPropertiesToIgnore.Contains(x.EventArgs.PropertyName)).Select(x => $"[{Name}].{x.Sender}.{x.EventArgs.PropertyName}  [WhileActive] Action property changed"))
                .Subscribe(reason => RaisePropertyChanged(nameof(Properties)))
                .AddTo(Anchors);
            sw.Step($"Overlay model properties initialized");

            overlayController.RegisterChild(overlayViewModel).AddTo(Anchors);
            sw.Step($"Overlay registration completed: {this}");
            
            auraTriggers.AddTo(Anchors);
            whileActiveActionsHolder.AddTo(Anchors);
            onEnterActionsHolder.AddTo(Anchors);
            onExitActionsHolder.AddTo(Anchors);
            Disposable.Create(() =>
            {
                Log.Debug(
                    $"Disposed Aura {Name}({Id}) (aka {defaultAuraName}), triggers: {Triggers.Count}, actions: {OnEnterActions.Count}");
            }).AddTo(Anchors);
        }

        private void ExecuteOnEnterActions()
        {
            Log.Debug($"[{Name}({Id})] Trigger state changed, executing OnEnter Actions");
            OnEnterActions.ForEach(action => action.Execute());
        }
        
        private void ExecuteOnExitActions()
        {
            Log.Debug($"[{Name}({Id})] Trigger state changed, executing OnExit Actions");
            OnExitActions.ForEach(action => action.Execute());
        }
        
        private void ExecuteWhileActiveActions()
        {
            foreach (var action in WhileActiveActions)
            {
                if (!IsActive)
                {
                    break;
                }
                action.Execute();
            }

            LastWhileActiveTimestamp = clock.Now;
        }
        
        public bool IsActive
        {
            get => isActive;
            private set => RaiseAndSetIfChanged(ref isActive, value);
        }

        public DateTime LastWhileActiveTimestamp
        {
            get => lastWhileActiveTimestamp;
            private set => this.RaiseAndSetIfChanged(ref lastWhileActiveTimestamp, value);
        }

        public WindowMatchParams TargetWindow
        {
            get => targetWindow;
            set => RaiseAndSetIfChanged(ref targetWindow, value);
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => RaiseAndSetIfChanged(ref isEnabled, value);
        }

        public string Id
        {
            get => uniqueId;
            private set => this.RaiseAndSetIfChanged(ref uniqueId, value);
        }

        public ObservableCollection<IAuraTrigger> Triggers { get; }

        public ObservableCollection<IAuraAction> OnEnterActions { get; }
        
        public ObservableCollection<IAuraAction> WhileActiveActions { get; }

        public ObservableCollection<IAuraAction> OnExitActions { get; }

        public TimeSpan WhileActiveActionsTimeout
        {
            get => whileActiveActionsTimeout;
            set => this.RaiseAndSetIfChanged(ref whileActiveActionsTimeout, value);
        }

        public IEyeOverlayViewModel Overlay { get; }

        public void SetCloseController(ICloseController closeController)
        {
            Guard.ArgumentNotNull(closeController, nameof(closeController));

            CloseController = closeController;
        }

        public ICloseController CloseController
        {
            get => closeController;
            private set => RaiseAndSetIfChanged(ref closeController, value);
        }

        public string Name
        {
            get => name;
            set => RaiseAndSetIfChanged(ref name, value);
        }

        private void ReloadCollections(OverlayAuraProperties source)
        {
            OnEnterActions.Clear();
            OnExitActions.Clear();
            WhileActiveActions.Clear();
            Triggers.Clear();
            
            source.TriggerProperties
                .Select(ToAuraProperties)
                .Where(ValidateProperty)
                .Select(x => repository.CreateModel<IAuraTrigger>(x))
                .ForEach(x => Triggers.Add(x));
            
            source.OnEnterActionProperties
                .Select(ToAuraProperties)
                .Where(ValidateProperty)
                .Select(x => repository.CreateModel<IAuraAction>(x))
                .ForEach(x => OnEnterActions.Add(x));
            
            source.OnExitActionProperties
                .Select(ToAuraProperties)
                .Where(ValidateProperty)
                .Select(x => repository.CreateModel<IAuraAction>(x))
                .ForEach(x => OnExitActions.Add(x));
            
            source.WhileActiveActionProperties
                .Select(ToAuraProperties)
                .Where(ValidateProperty)
                .Select(x => repository.CreateModel<IAuraAction>(x))
                .ForEach(x => WhileActiveActions.Add(x));
        }

        protected override void VisitLoad(OverlayAuraProperties source)
        {
            if (!string.IsNullOrEmpty(source.Id))
            {
                Id = source.Id;
            }
            if (!string.IsNullOrEmpty(source.Name))
            {
                Name = source.Name;
            }

            WhileActiveActionsTimeout = source.WhileActiveActionsTimeout;
            TargetWindow = source.WindowMatch;
            ReloadCollections(source);
            IsEnabled = source.IsEnabled;
            Overlay.ThumbnailOpacity = source.ThumbnailOpacity;
            Overlay.Region.SetValue(source.SourceRegionBounds);
            Overlay.IsClickThrough = source.IsClickThrough;
            Overlay.MaintainAspectRatio = source.MaintainAspectRatio;
            Overlay.BorderColor = source.BorderColor;
            Overlay.BorderThickness = source.BorderThickness;

            var bounds = source.OverlayBounds.ScaleToWpf();
            Overlay.Left = bounds.Left;
            Overlay.Top = bounds.Top;
            Overlay.Height = bounds.Height;
            Overlay.Width = bounds.Width;
        }

        protected override void VisitSave(OverlayAuraProperties properties)
        {
            properties.Name = Name;
            properties.TriggerProperties = Triggers.Select(x => x.Properties).Where(ValidateProperty).Select(ToMetadata).ToList();
            properties.OnEnterActionProperties = OnEnterActions.Select(x => x.Properties).Where(ValidateProperty).Select(ToMetadata).ToList();
            properties.WhileActiveActionProperties = WhileActiveActions.Select(x => x.Properties).Where(ValidateProperty).Select(ToMetadata).ToList();
            properties.OnExitActionProperties = OnExitActions.Select(x => x.Properties).Where(ValidateProperty).Select(ToMetadata).ToList();
            properties.WhileActiveActionsTimeout = WhileActiveActionsTimeout;
            properties.SourceRegionBounds = Overlay.Region.Bounds;
            properties.OverlayBounds = Overlay.NativeBounds;
            properties.WindowMatch = TargetWindow;
            properties.IsClickThrough = Overlay.IsClickThrough;
            properties.ThumbnailOpacity = Overlay.ThumbnailOpacity;
            properties.MaintainAspectRatio = Overlay.MaintainAspectRatio;
            properties.BorderColor = Overlay.BorderColor;
            properties.BorderThickness = Overlay.BorderThickness;
            properties.IsEnabled = IsEnabled;
            properties.Id = Id;
        }

        private IAuraProperties ToAuraProperties(PoeConfigMetadata<IAuraProperties> metadata)
        {
            if (metadata.Value == null)
            {
                Log.Warn($"Trying to re-serialize metadata type {metadata.TypeName} (v{metadata.Version}) {metadata.AssemblyName}...");
                var serialized = configSerializer.Serialize(metadata);
                if (string.IsNullOrEmpty(serialized))
                {
                    throw new ApplicationException($"Something went wrong when re-serializing metadata: {metadata}\n{metadata.ConfigValue}");
                }
                var deserialized = configSerializer.Deserialize<PoeConfigMetadata<IAuraProperties>>(serialized);
                if (deserialized.Value != null)
                {
                    Log.Debug($"Successfully restored type {metadata.TypeName} (v{metadata.Version}) {metadata.AssemblyName}: {deserialized.Value}");
                    metadata = deserialized;
                }
                else
                {
                    Log.Warn($"Failed to restore type {metadata.TypeName} (v{metadata.Version}) {metadata.AssemblyName}");
                }
            }
            
            if (metadata.Value == null)
            {
                
                return new ProxyAuraProperties(metadata);
            }

            return metadata.Value;
        }
        
        private PoeConfigMetadata<IAuraProperties> ToMetadata(IAuraProperties properties)
        {
            if (properties is ProxyAuraProperties proxyAuraProperties)
            {
                return proxyAuraProperties.Metadata;
            }
            
            return new PoeConfigMetadata<IAuraProperties>
            {
                AssemblyName = properties.GetType().Assembly.GetName().Name,
                TypeName = properties.GetType().FullName,
                Value = properties,
                Version = properties.Version,
            };
        }
        
        private bool ValidateProperty(IAuraProperties properties)
        {
            if (properties == null)
            {
                return false;
            }
            
            if (properties is EmptyAuraProperties)
            {
                Log.Warn($"[{Name}({Id})] {nameof(EmptyAuraProperties)} should never be used for Models Save/Load purposes - too generic");
                return false;
            }

            return true;
        }
    }
}